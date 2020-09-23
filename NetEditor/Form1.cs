using Microsoft.Win32;
using Siemens.Engineering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace NetEditor
{
    public partial class Form1 : Form
    {
        private static TiaPortalProcess _tiaProcess;
        private ProjectLevel projectLevel;
        private Dictionary<int, NetworkDeviceItem> rowDevices = new Dictionary<int, NetworkDeviceItem>();
        private bool dataTableAutoEdit = true;
        private ExclusiveAccess exclusiveAccess;
        private Transaction transaction;
        // it is needed because disposed access and transaction are not null, so it's hard to check for them.
        private bool accessAvailable = false;
        private bool transactionOpen = false;
        private SortBy lastSortingOrder = SortBy.Name;

        public TiaPortal MyTiaPortal { get; set; }
        public Project MyProject { get; set; }

        string version = "0.4";

        public Form1()
        {
            
            AppDomain CurrentDomain = AppDomain.CurrentDomain;
            CurrentDomain.AssemblyResolve += new ResolveEventHandler(MyResolver);
            InitializeComponent();
            //LoadOpennessAssembly();


            // Double buffering can make DGV slow in remote desktop
            if (!System.Windows.Forms.SystemInformation.TerminalServerSession)
            {
                Type dgvType = deviceTable.GetType();
                PropertyInfo pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                pi.SetValue(deviceTable, true, null);
            }
            this.Text = $"Net Editor {version}";
        }

        private Assembly MyResolver(object sender, ResolveEventArgs args)
        {
            int index = args.Name.IndexOf(',');
            if (index == -1)
            {
                return null;
            } 
            string name = args.Name.Substring(0, index);

            RegistryKey filePathReg = Registry.LocalMachine.OpenSubKey(
    "SOFTWARE\\Siemens\\Automation\\Openness\\16.0\\PublicAPI\\16.0.0.0");
            /*
            if (radio160.Checked)
            {
                filePathReg = Registry.LocalMachine.OpenSubKey(
    "SOFTWARE\\Siemens\\Automation\\Openness\\16.0\\PublicAPI\\16.0.0.0");
            } else
            {
                filePathReg = Registry.LocalMachine.OpenSubKey(
    "SOFTWARE\\Siemens\\Automation\\Openness\\15.1\\PublicAPI\\15.1.0.0");
            }
            */
            
            if (filePathReg == null) return null;

            object oRegKeyValue = filePathReg.GetValue(name);
            if (oRegKeyValue != null)
            {
                string filePath = oRegKeyValue.ToString();

                string path = filePath;
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    return Assembly.LoadFrom(fullPath);
                }
            }

            return null;
        }

        private void LoadOpennessAssembly()
        {
            RegistryKey filePathReg = Registry.LocalMachine.OpenSubKey(
                "SOFTWARE\\Siemens\\Automation\\Openness\\16.0\\PublicAPI\\16.0.0.0");

            if (filePathReg == null) return;

            object oRegKeyValue = filePathReg.GetValue("Siemens.Engineering");
            if (oRegKeyValue != null)
            {
                string filePath = oRegKeyValue.ToString();

                string path = filePath;
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    Assembly.LoadFrom(fullPath);
                }
            }
        }

        private async Task InitializeEditor()
        {
            btn_ClearChanges.Enabled = false;
            btn_Disconnect.Enabled = false;
            btn_CommitChanges.Enabled = false;
            btn_ExportToCSV.Enabled = false;

            try
            {
                await InitializeConnectionWithTiaPortalProject().ConfigureAwait(true);
            }
            catch (EngineeringTargetInvocationException ex)
            {
                txt_Status.AppendText(ex.Message + Environment.NewLine);
                btn_Connect.Enabled = true;
                return;
            }

            projectLevel = await ProjectLevel.Create(MyProject);

            txt_Status.AppendText($"Connected to TIA Portal project: {MyProject.Path}{Environment.NewLine}");
            this.Text = $"Net Editor {version}: {MyProject.Path}";

            PopulateDeviceTable(lastSortingOrder);
           
            btn_ClearChanges.Enabled = true;
            btn_Disconnect.Enabled = true;
            btn_CommitChanges.Enabled = true;
            btn_ExportToCSV.Enabled = true;
        }

        private async Task InitializeConnectionWithTiaPortalProject()
        {
            await Task.Run(() =>
            {
                if (!accessAvailable)
                {
                    IList<TiaPortalProcess> processes = TiaPortal.GetProcesses();

                    switch (processes.Count)
                    {
                        case 1:
                            _tiaProcess = processes[0];
                            MyTiaPortal = _tiaProcess.Attach();
                            break;
                        case 0:
                            throw new EngineeringTargetInvocationException(
                                "No running instance of TIA Portal was found!");
                        default:
                            throw new EngineeringTargetInvocationException(
                                "More than one running instance of TIA Portal was found!");
                    }

                    if (MyTiaPortal.Projects.Count <= 0)
                    {
                        throw new EngineeringTargetInvocationException(
                            "No open TIA Portal Project was found!");
                    }

                    MyProject = MyTiaPortal.Projects[0];

                    exclusiveAccess = MyTiaPortal.ExclusiveAccess("My Activity");
                    accessAvailable = true;
                }

                if (!transactionOpen)
                {
                    transaction = exclusiveAccess.Transaction(MyProject, "No changes commited.");
                    transactionOpen = true;
                }
            }).ConfigureAwait(true);
        }

        private void PopulateDeviceTable(SortBy sortingOrder)
        {
            dataTableAutoEdit = true;
            deviceTable.Rows.Clear();
            rowDevices.Clear();

            List<DataGridViewRow> rows = new List<DataGridViewRow>();
            List<SubnetLevel> subnets = projectLevel.Subnets;

            Sort(subnets, sortingOrder);
            subnets.ForEach(sn =>
            {
                Sort(sn.IoSystems, sortingOrder);
                sn.IoSystems.ForEach(ioSystemLvl =>
                {
                    AddIoControllerRow(rows, ioSystemLvl);

                    Sort(ioSystemLvl.IoDevices, sortingOrder);
                    ioSystemLvl.IoDevices.ForEach(iod =>
                    {
                        AddIoDeviceRow(rows, iod, ioSystemLvl);
                    });
                });

                Sort(sn.SubnetLvlDevItems, sortingOrder);
                sn.SubnetLvlDevItems.ForEach(sdi =>
                {
                    AddSubnetDeviceRow(rows, sdi);
                });
            });

            Sort(projectLevel.UnusedDeviceItems, sortingOrder);
            projectLevel.UnusedDeviceItems.ForEach(udi =>
            {
                AddUnusedDeviceRow(rows, udi);
            });


            deviceTable.Rows.AddRange(rows.ToArray());
            dataTableAutoEdit = false;

            MarkRowsWithRepeatingIPs();
        }

        #region Events
        async void deviceTable_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (dataTableAutoEdit) return;

            dataTableAutoEdit = true;

            DataGridViewRow editedRow = deviceTable.Rows[e.RowIndex];
            DataGridViewCell editedCell = editedRow.Cells[e.ColumnIndex];
            string newValue = editedCell.Value?.ToString() ?? "";

            int id = int.Parse(editedRow.Cells[dgv_Id.Index].Value.ToString());
            NetworkDeviceItem netDeviceItem = rowDevices[id];

            if (e.ColumnIndex == dgv_IpAddress.Index) 
            {
                await EditIPAddressCell(netDeviceItem, editedCell, newValue).ConfigureAwait(true);
            }
            else if (e.ColumnIndex == dgv_DeviceName.Index)
            {
                await EditDeviceNameCell(netDeviceItem, editedRow, editedCell, newValue).ConfigureAwait(true);
            }
            else if (e.ColumnIndex == dgv_Mask.Index)
            {
                await EditMaskCell(netDeviceItem, editedCell, newValue).ConfigureAwait(true);
            }
            else if (e.ColumnIndex == dgv_PnDeviceName.Index)
            {
                await EditPnDeviceNameCell(netDeviceItem, editedRow, editedCell, newValue).ConfigureAwait(true);
            }
            else if (e.ColumnIndex == dgv_PnSubnet.Index)
            {
                await EditSubnetNameCell(netDeviceItem, editedCell, newValue).ConfigureAwait(true);
            }
            else if (e.ColumnIndex == dgv_IoSystem.Index)
            {
                await EditIoSystemNameCell(netDeviceItem, editedCell, newValue).ConfigureAwait(true);
            }
            else if (e.ColumnIndex == dgv_autoGeneratePnName.Index)
            {
                await EditAutoGenerateCell(netDeviceItem, editedRow, editedCell, newValue).ConfigureAwait(true);
            }
            else if (e.ColumnIndex == dgv_PnNumber.Index)
            {
                await EditPnNumberCell(netDeviceItem, editedCell, newValue).ConfigureAwait(true);
            }
            else if (e.ColumnIndex == dgv_RouterAddress.Index)
            {
                await EditRouterAddressCell(netDeviceItem, editedCell, newValue).ConfigureAwait(true);
            }
            dataTableAutoEdit = false;

        }

        async Task EditIPAddressCell(NetworkDeviceItem editedNDI, DataGridViewCell editedCell, string proposedAddress)
        {
            string oldAddress = editedNDI.IpAddress;
            Task task = editedNDI.ChangeIPAddress(proposedAddress);

            try
            {
                await task.ConfigureAwait(true);
            }
            catch (ArgumentException ex)
            {
                editedCell.Value = oldAddress;
                txt_Status.AppendText(ex.Message + Environment.NewLine);
            }
            catch (EngineeringNotSupportedException ex)
            {
                editedCell.Value = oldAddress;

                txt_Status.AppendText(ex.Message + Environment.NewLine);
                Debug.WriteLine(ex.Message);
            }

            string newAddress = editedNDI.IpAddress;
            editedCell.Value = newAddress;
            if (editedNDI.IpAddress == proposedAddress)
            {
                txt_Status.AppendText($"Device \"{editedNDI.HMName}\" changed IP address from: {oldAddress} to: {newAddress}." +
                    $"{Environment.NewLine}");
            }

            MarkRowsWithRepeatingIPs();
        }

        async Task EditDeviceNameCell(NetworkDeviceItem editedNDI, DataGridViewRow editedRow, 
                                      DataGridViewCell editedCell, string proposedName)
        {
            string oldName = editedNDI.HMName;
            string oldPnName = editedNDI.PnDeviceName;

            if (oldName == proposedName) return;
            if (String.IsNullOrWhiteSpace(proposedName))
            {
                editedCell.Value = oldName;
                txt_Status.AppendText($"Device name cannot be empty.{Environment.NewLine}");
                return;
            }
            foreach (DataGridViewRow row in deviceTable.Rows)
            {
                if (row.Equals(editedRow)) continue;
                string deviceName = row.Cells[dgv_DeviceName.Index].Value.ToString();
                if (deviceName.Equals(proposedName, StringComparison.Ordinal)) 
                {
                    editedCell.Value = oldName;
                    txt_Status.AppendText($"Chosen device name is already taken.{Environment.NewLine}");
                    return;
                }
            }

            await editedNDI.ChangeName(proposedName).ConfigureAwait(true);
            string newName = editedNDI.HMName;
            editedCell.Value = newName;
            txt_Status.AppendText($"Device \"{oldName}\" changed name to: \"{newName}\".{Environment.NewLine}");

            if (editedNDI.autoGeneratePnName)
            {
                string newPnName = editedNDI.PnDeviceName;

                editedRow.Cells[dgv_PnDeviceName.Index].Value = newPnName;
                txt_Status.AppendText($"Device \"{newName}\" changed Profinet device name from: " +
                    $"\"{oldPnName}\" to: \"{newPnName}\".{Environment.NewLine}");
            }

            int deviceKey;

            dataTableAutoEdit = true;
            foreach (DataGridViewRow row in deviceTable.Rows)
            {
                if (row.Equals(editedRow)) continue; // it may be slow, look out

                string rowDeviceName = row.Cells[dgv_DeviceName.Index].Value.ToString();
                if (rowDeviceName != oldName) continue;

                deviceKey = int.Parse(row.Cells[dgv_Id.Index].Value.ToString());
                NetworkDeviceItem modifiedDI = rowDevices[deviceKey];

                row.Cells[dgv_DeviceName.Index].Value = await modifiedDI.UpdateHMName().ConfigureAwait(true);

                if (modifiedDI.autoGeneratePnName)
                {
                    string oldPnNameM = modifiedDI.PnDeviceName;
                    string newPnNameM = modifiedDI.UpdatePnDeviceName();

                    // update for now, bc structure logic doesn't care about updating device names. 
                    // That will change.
                    row.Cells[dgv_PnDeviceName.Index].Value = newPnNameM;
                    txt_Status.AppendText($"Device \"{newName}\" changed Profinet device name from: " +
                                          $"\"{oldPnNameM}\" to: \"{newPnNameM}\".{Environment.NewLine}");
                }
            }
            dataTableAutoEdit = false;
        }

        async Task EditMaskCell(NetworkDeviceItem editedNDI, DataGridViewCell editedCell, string proposedMask)
        {
            string oldMask = editedNDI.AddressMask;
            Task task = editedNDI.ChangeAddressMask(proposedMask);

            try
            {
                await task.ConfigureAwait(true);
            }
            catch (ArgumentException ex)
            {
                editedCell.Value = oldMask;
                txt_Status.AppendText(ex.Message + Environment.NewLine);
            }
            catch (EngineeringNotSupportedException ex)
            {
                editedCell.Value = oldMask;

                txt_Status.AppendText(ex.Message + Environment.NewLine);
                Debug.WriteLine(ex.Message);
            }

            string newMask = editedNDI.AddressMask;
            editedCell.Value = newMask;
            txt_Status.AppendText($"Device \"{editedNDI.HMName}\" changed address mask from: " +
                                  $"{oldMask} to: {newMask}.{Environment.NewLine}");

            // only masks of devices in the same subnet (or iosystem) have changed so they should be filtered
            // actually, not really sure about that, some devices work both as IoControllers and IoDevices
            // in different IoSystems, so lets update everything to make sure.
            foreach (DataGridViewRow row in deviceTable.Rows)
            {
                // this is not valid, bc index value is already a string
                //int deviceKey = (int)row.Cells[dgv_Id.Index].Value;

                int deviceKey = int.Parse(row.Cells[dgv_Id.Index].Value.ToString());
                row.Cells[dgv_Mask.Index].Value = rowDevices[deviceKey].UpdateAddressMask();
            }

        }

        async Task EditPnDeviceNameCell(NetworkDeviceItem editedNDI, DataGridViewRow editedRow, 
                                        DataGridViewCell editedCell, string proposedPnName)
        {
            if (String.IsNullOrWhiteSpace(proposedPnName)) return;

            string oldPnName = editedNDI.PnDeviceName;

            // bad idea, bc it changes autogeneration, even if it's the same name
            //if (oldPnName.Equals(proposedPnName, StringComparison.Ordinal)) return;
            //if (oldPnName.Equals(newPnName, StringComparison.Ordinal)) return;

            await editedNDI.ChangePnDeviceName(proposedPnName);
            string newPnName = editedNDI.PnDeviceName;

            editedCell.Value = newPnName;
            DataGridViewCell autoGenPnNameCell = editedRow.Cells[dgv_autoGeneratePnName.Index];
            autoGenPnNameCell.Value = editedNDI.autoGeneratePnName;

            txt_Status.AppendText($"Device \"{editedNDI.HMName}\" changed Profinet device name from: " +
                                  $"\"{oldPnName}\" to: \"{newPnName}\".{Environment.NewLine}");
        }

        async Task EditSubnetNameCell(NetworkDeviceItem editedNDI, DataGridViewCell editedCell, 
                                      string proposedSubnetName)
        {
            string oldName = editedNDI.PnSubnetName;
            editedNDI.ChangeSubnetName(proposedSubnetName); 
            string newName = editedNDI.PnSubnetName;

            if (oldName.Equals(newName, StringComparison.Ordinal))
            {
                txt_Status.AppendText($"The name of {oldName} subnet was not changed.{Environment.NewLine}");
                return;
            }

            editedCell.Value = newName;

            foreach (DataGridViewRow row in deviceTable.Rows)
            {
                DataGridViewCell subnetCell = row.Cells[dgv_PnSubnet.Index];
                string cellSubnetName = subnetCell.Value.ToString();

                if (cellSubnetName.Equals(oldName))
                {
                    DataGridViewCell idCell = row.Cells[dgv_Id.Index];
                    int deviceKey = int.Parse(idCell.Value.ToString());
                    subnetCell.Value = await rowDevices[deviceKey].UpdatePnSubnetName();
                }
            }

            txt_Status.AppendText($"Subnet {oldName} changed name to {newName}.{Environment.NewLine}");
        }

        async Task EditIoSystemNameCell(NetworkDeviceItem editedNDI, DataGridViewCell editedCell,
                                        string proposedIoSystemName)
        {
            string oldName = editedNDI.IoSystemName;
            editedNDI.ChangeIoSystemName(proposedIoSystemName);
            string newName = editedNDI.IoSystemName;

            if (oldName.Equals(newName, StringComparison.Ordinal))
            {
                txt_Status.AppendText($"The name of \"{oldName}\" IO system was not changed.{Environment.NewLine}");
                return;
            }

            editedCell.Value = newName;

            foreach (DataGridViewRow row in deviceTable.Rows)
            {
                DataGridViewCell ioSystemCell = row.Cells[dgv_IoSystem.Index];
                string cellIoSystemName = ioSystemCell.Value.ToString();

                if (!cellIoSystemName.Equals("[Not connected]"))
                {
                    DataGridViewCell idCell = row.Cells[dgv_Id.Index];
                    int deviceKey = int.Parse(idCell.Value.ToString());
                    ioSystemCell.Value = await rowDevices[deviceKey].UpdateIoSystemName();
                }
            }

            txt_Status.AppendText($"IO system \"{oldName}\" changed name to \"{newName}\".{Environment.NewLine}");
        }

        async Task EditAutoGenerateCell(NetworkDeviceItem editedNDI, DataGridViewRow editedRow,
                                DataGridViewCell editedCell, string proposedValue)
        {
            bool proposedBool = Boolean.Parse(proposedValue);
            editedNDI.ChangePnDeviceNameAutoGeneration(proposedBool);
            bool isAutogenerated = editedNDI.autoGeneratePnName;
            editedCell.Value = isAutogenerated;
            editedRow.Cells[dgv_PnDeviceName.Index].Value = editedNDI.UpdatePnDeviceName();

            if (isAutogenerated)
            {
                txt_Status.AppendText($"Profinet name of device \"{editedNDI.HMName}\" is autogenerated from " +
                                      $"its device name.{Environment.NewLine}");
            }
            else
            {
                txt_Status.AppendText($"Profinet name of device \"{editedNDI.HMName}\" is not autogenerated." +
                                     $"{Environment.NewLine}");
            }
        }

        async Task EditPnNumberCell(NetworkDeviceItem editedNDI,
                        DataGridViewCell editedCell, string proposedNumber)
        {
            string oldNumber = editedNDI.PnDeviceNumber;
            int pnNumber;
            if (int.TryParse(proposedNumber, out pnNumber)) editedNDI.ChangePnNumber(pnNumber);

            string newNumber = editedNDI.PnDeviceNumber;
            editedCell.Value = newNumber;

            txt_Status.AppendText($"Device \"{editedNDI.HMName}\" changed Profinet number from: " +
                      $"\"{oldNumber}\" to: \"{newNumber}\".{Environment.NewLine}");
        }

        async Task EditRouterAddressCell(NetworkDeviceItem editedNDI,
                                        DataGridViewCell editedCell, string proposedAddress)
        {
            string oldAddress = editedNDI.RouterAddress;

            try
            {
                editedNDI.ChangeRouterAddress(proposedAddress);
            }
            catch (EngineeringNotSupportedException ex)
            {
                txt_Status.AppendText(ex.Message + Environment.NewLine);
                Debug.WriteLine(ex.Message);

                editedCell.Value = oldAddress;
            }

            string newAddress = editedNDI.RouterAddress;
            if (!oldAddress.Equals(newAddress, StringComparison.Ordinal))
            {
                editedCell.Value = newAddress;
                txt_Status.AppendText($"Device {editedNDI.HMName} changed router address from: {oldAddress} to: {newAddress}." +
                $"{Environment.NewLine}");
            }
            
        }

        private async void btn_ConnectTIA(object sender, EventArgs e)
        {
            btn_Connect.Enabled = false;
            progressBar.Style = ProgressBarStyle.Marquee;
            await InitializeEditor();
            progressBar.Style = ProgressBarStyle.Continuous;
            if (projectLevel != null) btn_Connect.Text = "Refresh";
            btn_Connect.Enabled = true;
        }

        void CommitChanges(object sender, EventArgs e) 
        {
            transaction.CommitOnDispose();
            transaction.Dispose();
            transactionOpen = false;
            txt_Status.Text = "Earlier changes were commited." + Environment.NewLine;

            btn_ClearChanges.Enabled = false;
            btn_CommitChanges.Enabled = false;
        }

        void ClearChanges(object sender, EventArgs e)
        {
            transaction.Dispose();
            transactionOpen = false;
            txt_Status.Text = "Earlier changes were not applied." + Environment.NewLine;

            btn_ClearChanges.Enabled = false;
            btn_CommitChanges.Enabled = false;
        }

        void DisconnectFromProject(object sender, EventArgs e)
        {
            transaction.Dispose();
            exclusiveAccess.Dispose();
            accessAvailable = false;
            transactionOpen = false;
            deviceTable.Rows.Clear();
            txt_Status.Text = "Disconnected. Any changes made after the last commit were not applied." + Environment.NewLine;

            btn_Connect.Text = "Connect";
            btn_ClearChanges.Enabled = false;
            btn_CommitChanges.Enabled = false;
            btn_Disconnect.Enabled = false;
        }

        private void DeviceTable_OnCellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            // End of edition on each click on column of checkbox
            if (e.ColumnIndex == dgv_autoGeneratePnName.Index && e.RowIndex != -1)
            {
                deviceTable.EndEdit();
            }
        }
        
        private void DGV_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            if (e.Column.Index == dgv_DeviceName.Index)
            {
                int id1 = int.Parse(deviceTable.Rows[e.RowIndex1].Cells[dgv_Id.Index].Value.ToString());
                int id2 = int.Parse(deviceTable.Rows[e.RowIndex2].Cells[dgv_Id.Index].Value.ToString());

                NetworkDeviceItem ndi1 = rowDevices[id1];
                NetworkDeviceItem ndi2 = rowDevices[id2];
                string name1 = e.CellValue1.ToString();
                string name2 = e.CellValue2.ToString();

                if (ndi1.SubnetLevel == null && ndi2.SubnetLevel == null) name1.CompareTo(name2);
                else if (ndi1.SubnetLevel == null) e.SortResult = 1;
                else if (ndi2.SubnetLevel == null) e.SortResult = -1;

                else if (ndi1.SubnetLevel != ndi2.SubnetLevel) //both subnets exist and...
                {
                    e.SortResult = ndi1.SubnetLevel.SubnetName.CompareTo(ndi2.SubnetLevel.SubnetName);
                }
                else // the same subnet
                {
                    if (ndi1.IoSystemLevel == null && ndi2.IoSystemLevel == null) name1.CompareTo(name2);
                    else if (ndi1.IoSystemLevel == null) e.SortResult = 1;
                    else if (ndi2.IoSystemLevel == null) e.SortResult = -1;

                    else if (ndi1.IoSystemLevel != ndi2.IoSystemLevel) //both iosystems exist and...
                    {
                        string s1 = ndi1.IoSystemLevel.IoController.HMName + '.' + ndi1.IoSystemLevel.IoSystemName;
                        string s2 = ndi2.IoSystemLevel.IoController.HMName + '.' + ndi2.IoSystemLevel.IoSystemName;
                        e.SortResult = s1.CompareTo(s2);
                    }
                    else // the same iosystem
                    {
                        if (ndi1.workMode == NetworkDeviceItemWorkMode.IoController) e.SortResult = -1;
                        else if (ndi2.workMode == NetworkDeviceItemWorkMode.IoController) e.SortResult = 1;
                        else e.SortResult = name1.CompareTo(name2);
                    }
                }
            } 
            else if (e.Column.Index == dgv_Id.Index)
            {
                int i1 = int.Parse(e.CellValue1.ToString());
                int i2 = int.Parse(e.CellValue2.ToString());
                e.SortResult = i1 - i2;
            }
           

            e.Handled = true;
        }

        private void DeviceTable_Sort(object sender, DataGridViewCellMouseEventArgs e)
        {

            if (e.ColumnIndex == dgv_DeviceName.Index)
            {
                if (lastSortingOrder == SortBy.Name)
                {
                    PopulateDeviceTable(SortBy.NameRev);
                    lastSortingOrder = SortBy.NameRev;
                    dgv_DeviceName.HeaderCell.SortGlyphDirection = SortOrder.Ascending;
                }
                else
                { 
                    PopulateDeviceTable(SortBy.Name);
                    lastSortingOrder = SortBy.Name;
                    dgv_DeviceName.HeaderCell.SortGlyphDirection = SortOrder.Descending;
                }
            } 
            else if (e.ColumnIndex == dgv_IpAddress.Index)
            {
                if (lastSortingOrder == SortBy.IpAddress)
                {
                    PopulateDeviceTable(SortBy.IpAddressRev);
                    lastSortingOrder = SortBy.IpAddressRev;
                    dgv_IpAddress.HeaderCell.SortGlyphDirection = SortOrder.Ascending;
                }
                else
                {
                    PopulateDeviceTable(SortBy.IpAddress);
                    lastSortingOrder = SortBy.IpAddress;
                    dgv_IpAddress.HeaderCell.SortGlyphDirection = SortOrder.Descending;
                }
            }
        }
        #endregion

        #region records and CSV
        private DataGridViewRow CreateTableRecord(int Id, NetworkDeviceItem netDeviceItem, IoSystemLevel ioSystemLevel)
        {
            // it would be easier to return a whole row
            rowDevices.Add(Id, netDeviceItem);

            DataGridViewRow row = new DataGridViewRow();
            row.CreateCells(deviceTable);
            row.SetValues(CreateRecord(Id, netDeviceItem, ioSystemLevel));
 
            return row;
        }

        private static string[] CreateRecord(int Id, NetworkDeviceItem netDeviceItem, IoSystemLevel ioSystemLevel)
        {
            string deviceName = netDeviceItem.HMName;
            string ipAddress = netDeviceItem.IpAddress ?? "";
            string addressMask = netDeviceItem.AddressMask ?? "";
            string pnDeviceName = netDeviceItem.PnDeviceName ?? "";
            string interfaceOperatingMode = netDeviceItem.InterfaceOperatingMode;
            string subnetName = netDeviceItem.PnSubnetName ?? "[Not connected]";
            string ioSystemName = ioSystemLevel?.IoSystemName ?? "[Not connected]";
            string pnDeviceNumber = netDeviceItem.PnDeviceNumber ?? "[N/A]";
            string routerAddress = netDeviceItem.RouterAddress ?? "[Not used]";
            bool autoPnName = netDeviceItem.autoGeneratePnName;

            string[] record = new string[]
            {
                Id.ToString(),
                interfaceOperatingMode,
                deviceName,
                autoPnName.ToString(),
                pnDeviceName,
                pnDeviceNumber,
                ipAddress,
                addressMask,
                routerAddress,
                subnetName,
                ioSystemName,
            };

            return record;
        }

        private static string CreateCSVRecord(NetworkDeviceItem netDeviceItem, IoSystemLevel ioSystemLevel)
        {
            string deviceName = netDeviceItem.HMName;
            string ipAddress = netDeviceItem.IpAddress;
            string addressMask = netDeviceItem.AddressMask;
            string pnDeviceName = netDeviceItem.PnDeviceName;
            string interfaceOperatingMode = netDeviceItem.InterfaceOperatingMode;
            string subnetName = netDeviceItem.PnSubnetName ?? "[Not connected]";
            string ioSystemName = ioSystemLevel?.IoSystemName ?? "[Not connected]";
            string pnDeviceNumber = netDeviceItem.PnDeviceNumber ?? "[N/A]";
            string routerAddress = netDeviceItem.RouterAddress ?? "[Not used]";
            bool autoPnName = netDeviceItem.autoGeneratePnName;

            string record = "" +
                interfaceOperatingMode + ";" +
                deviceName + ";" +
                autoPnName.ToString() + ";" +
                pnDeviceName + ";" +
                pnDeviceNumber + ";" +
                ipAddress + ";" +
                addressMask + ";" +
                routerAddress + ";" +
                subnetName + ";" +
                ioSystemName;

            return record;
        }

        void ExportToCSV(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog1 = new SaveFileDialog())
            {
                saveFileDialog1.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
                saveFileDialog1.FilterIndex = 0;
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;
                
                using (System.IO.StreamWriter file = new StreamWriter(saveFileDialog1.OpenFile()))
                {
                    file.WriteLine("Interface Mode;Device Name;PN Device Name;Auto PN Name;PN Number;IP Address;Address Mask;" +
                        "Router Address;Subnet Name;IO System Name");
                    

                    projectLevel.Subnets.ForEach(sn =>
                    {
                        sn.IoSystems.ForEach(ios =>
                        {
                            file.WriteLine(CreateCSVRecord(ios.IoController, ios));
                            ios.IoDevices.ForEach(iod => file.WriteLine(CreateCSVRecord(iod, ios)));
                        });

                        sn.SubnetLvlDevItems.ForEach(sdi => file.WriteLine(CreateCSVRecord(sdi, null)));
                    });

                    projectLevel.UnusedDeviceItems.ForEach(udi =>
                    {
                        file.WriteLine(CreateCSVRecord(udi, null));
                    });
                }
                
            }
        }
        #endregion

        #region Table stylizing
        List<int> RowsWithRepeatingIPs()
        {
            // can I just take this info from openness??
            Dictionary<string, int> existingIPs = new Dictionary<string, int>();
            List<int> indicesWithRepetingIPs = new List<int>();

            foreach (DataGridViewRow row in deviceTable.Rows)
            {
                object obj = row.Cells[dgv_IpAddress.Index].Value;
                if (obj == null) continue;
                string deviceIP = obj.ToString();
                if (existingIPs.ContainsKey(deviceIP))
                {
                    string firstMode = deviceTable.Rows[existingIPs[deviceIP]].Cells[dgv_Mode.Index].Value.ToString();
                    string secondMode = row.Cells[dgv_Mode.Index].Value.ToString();
                    string mode = "IoController, IoDevice";
                    if (mode == firstMode && firstMode == secondMode) continue;

                    indicesWithRepetingIPs.Add(existingIPs[deviceIP]);
                    indicesWithRepetingIPs.Add(row.Index);
                }
                else
                {
                    existingIPs.Add(deviceIP, row.Index);
                }
            }

            return indicesWithRepetingIPs;
        }

        void MarkRowsWithRepeatingIPs()
        {
            // reset this style somewhere 
            foreach (DataGridViewRow row in deviceTable.Rows)
            {
                DataGridViewCell cell = row.Cells[dgv_IpAddress.Index];
                if (!string.IsNullOrEmpty(cell.Value.ToString()))
                {
                    cell.Style.BackColor = System.Drawing.SystemColors.Control;
                }
                else
                {
                    cell.Style.BackColor = System.Drawing.SystemColors.ControlLight;
                }
            }

            foreach (int index in RowsWithRepeatingIPs())
            {
                deviceTable.Rows[index].Cells[dgv_IpAddress.Index].Style.BackColor = System.Drawing.Color.Pink;
            }
        }

        private static void DisableCell(DataGridViewCell cell)
        {
            cell.ReadOnly = true;
            cell.Style.BackColor = System.Drawing.SystemColors.ControlLight;
            cell.Style.ForeColor = System.Drawing.SystemColors.GrayText;
        }

        private static void DisableCells(params DataGridViewCell[] cells)
        {
            foreach (var cell in cells)
            {
                DisableCell(cell);
            }
        }

        void AddIoControllerRow(List<DataGridViewRow> rows, IoSystemLevel ioSystem)
        {
            int id = rows.Count + 1;
            rows.Add(CreateTableRecord(id, ioSystem.IoController, ioSystem));

            DataGridViewCellCollection cells = rows[rows.Count - 1].Cells;
            cells[dgv_DeviceName.Index].Style.Padding = new Padding(0, 0, 0, 0);
            DisableCell(cells[dgv_PnNumber.Index]);
            if (!ioSystem.IoController.UseRouter) DisableCell(cells[dgv_RouterAddress.Index]);
        }

        void AddIoDeviceRow(List<DataGridViewRow> rows, NetworkDeviceItem ioDevice, IoSystemLevel ioSystem)
        {
            int id = rows.Count + 1;
            rows.Add(CreateTableRecord(id, ioDevice, ioSystem));


            DataGridViewCellCollection cells = rows[rows.Count - 1].Cells;
            // padding on name column emulates some kind of tree structure
            cells[dgv_DeviceName.Index].Style.Padding = new Padding(25, 0, 0, 0);

            DisableCells(cells[dgv_IoSystem.Index], cells[dgv_PnSubnet.Index],
                cells[dgv_RouterAddress.Index], cells[dgv_Mask.Index]);

            if (string.IsNullOrEmpty(cells[dgv_IpAddress.Index].Value?.ToString())) // empty in future?
            {
                DisableCell(cells[dgv_IpAddress.Index]);
            }
        }

        void AddSubnetDeviceRow(List<DataGridViewRow> rows, NetworkDeviceItem subnetDevice)
        {
            int id = rows.Count + 1;
            rows.Add(CreateTableRecord(id, subnetDevice, null));

            DataGridViewCellCollection cells = rows[rows.Count - 1].Cells;
            cells[dgv_DeviceName.Index].Style.Padding = new Padding(12, 0, 0, 0);

            DisableCells(cells[dgv_IoSystem.Index], cells[dgv_PnSubnet.Index],
                cells[dgv_RouterAddress.Index], cells[dgv_Mask.Index], cells[dgv_PnNumber.Index]);
        }

        void AddUnusedDeviceRow(List<DataGridViewRow> rows, NetworkDeviceItem unusedDevice)
        {
            int id = rows.Count + 1;
            rows.Add(CreateTableRecord(id, unusedDevice, null));

            DataGridViewCellCollection cells = rows[rows.Count - 1].Cells;
            cells[dgv_DeviceName.Index].Style.Padding = new Padding(5, 0, 0, 0);

            DisableCells(cells[dgv_IoSystem.Index], cells[dgv_PnSubnet.Index],
                    cells[dgv_RouterAddress.Index], cells[dgv_Mask.Index], cells[dgv_PnNumber.Index]);
        }

        #endregion

        #region Sorting
        private static void Sort(List<NetworkDeviceItem> sortedList, SortBy sortBy)
        {
            switch (sortBy)
            {
                case SortBy.Name:
                    sortedList.Sort(CompareByDeviceName);
                    break;
                case SortBy.NameRev:
                    sortedList.Sort(CompareByDeviceNameRev);
                    break;
                case SortBy.IpAddress:
                    sortedList.Sort(CompareByDeviceIpAddress);
                    break;
                case SortBy.IpAddressRev:
                    sortedList.Sort(CompareByDeviceIpAddressRev);
                    break;
                default:
                    break;
            }
        }

        private static void Sort(List<SubnetLevel> sortedList, SortBy sortBy)
        {
            switch (sortBy)
            {
                case SortBy.Name:
                    sortedList.Sort(CompareByName);
                    break;
                case SortBy.NameRev:
                    sortedList.Sort(CompareByNameRev);
                    break;
                case SortBy.IpAddress:
                    sortedList.Sort(CompareByName);
                    break;
                case SortBy.IpAddressRev:
                    sortedList.Sort(CompareByNameRev);
                    break;
                default:
                    break;
            }
        }

        private static void Sort(List<IoSystemLevel> sortedList, SortBy sortBy)
        {
            switch (sortBy)
            {
                case SortBy.Name:
                    sortedList.Sort(CompareByControllerName);
                    break;
                case SortBy.NameRev:
                    sortedList.Sort(CompareByControllerNameRev);
                    break;
                case SortBy.IpAddress:
                    sortedList.Sort(CompareByControllerIpAddress);
                    break;
                case SortBy.IpAddressRev:
                    sortedList.Sort(CompareByControllerIpAddressRev);
                    break;
                default:
                    break;
            }
        }
        
        private static int CompareByName(SubnetLevel x, SubnetLevel y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal.
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater.
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the
                    // lengths of the two strings.
                    return x.SubnetName.CompareTo(y.SubnetName);

                }
            }
        }

        private static int CompareByNameRev(SubnetLevel x, SubnetLevel y)
        {
            return -1* CompareByName(x, y);
        }

        private static int CompareByControllerName(IoSystemLevel x, IoSystemLevel y)
        {
            if (x == null)
            {
                if (y == null) return 0;
                else return -1;
            }
            else
            {
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the
                    // lengths of the two strings.
                    return x.IoController.HMName.CompareTo(y.IoController.HMName);
                    //int retval = x.Length.CompareTo(y.Length);
                }
            }
        }

        private static int CompareByControllerNameRev(IoSystemLevel x, IoSystemLevel y)
        {
            return -1 * CompareByControllerName(x, y);
        }

        private static int CompareByDeviceName(NetworkDeviceItem x, NetworkDeviceItem y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {                   
                    return -1; // y is greater.
                }
            }
            else
            {
                // If x is not null...
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the
                    // lengths of the two strings.
                    return x.HMName.CompareTo(y.HMName);
                    //int retval = x.Length.CompareTo(y.Length);
                }
            }
        }

        private static int CompareByDeviceNameRev(NetworkDeviceItem x, NetworkDeviceItem y)
        {
            return -1 * CompareByDeviceName(x, y);
        }

        private static int CompareByControllerIpAddress(IoSystemLevel x, IoSystemLevel y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {
                    return -1; // y is greater.
                }
            }
            else
            {
                // If x is not null...
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                { // x and y exist but check if they have ip addresses
                    if (String.IsNullOrEmpty(x.IoController.IpAddress))
                    {
                        if (String.IsNullOrEmpty(y.IoController.IpAddress))
                        {
                            return 0;
                        }
                        else
                        {
                            return -1; // y.address is greater.
                        }
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(y.IoController.IpAddress))
                        {
                            return 1;
                        }
                        else
                        { // they both have ip addresses
                            return x.IoController.IpAddress.CompareTo(y.IoController.IpAddress);
                        }
                    }
                }
            }
        }

        private static int CompareByControllerIpAddressRev(IoSystemLevel x, IoSystemLevel y)
        {
            return -1 * CompareByControllerIpAddress(x, y);
        }

        private static int CompareByDeviceIpAddress(NetworkDeviceItem x, NetworkDeviceItem y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {
                    return -1; // y is greater.
                }
            }
            else
            {
                // If x is not null...
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                { // x and y exist but check if they have ip addresses
                    if (String.IsNullOrEmpty(x.IpAddress))
                    {
                        if (String.IsNullOrEmpty(y.IpAddress))
                        {
                            return 0;
                        }
                        else
                        {
                            return -1; // y.address is greater.
                        }
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(y.IpAddress))
                        {
                            return 1;
                        }
                        else
                        { // they both have ip addresses
                            return x.IpAddress.CompareTo(y.IpAddress);
                        }
                    }
                }
            }
        }

        private static int CompareByDeviceIpAddressRev(NetworkDeviceItem x, NetworkDeviceItem y)
        {
            return -1 * CompareByDeviceIpAddress(x, y);
        }

        private enum SortBy
        {
            Name,
            NameRev,
            IpAddress,
            IpAddressRev
        }

        #endregion

    }

    // I use this to set the clock to highest frequency so Openness API calls are faster. I think it works
    public static class WinApi
    {
        /// <summary>TimeBeginPeriod(). See the Windows API documentation for details.</summary>

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1401:PInvokesShouldNotBeVisible"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressUnmanagedCodeSecurity]
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]

        public static extern uint TimeBeginPeriod(uint uMilliseconds);

        /// <summary>TimeEndPeriod(). See the Windows API documentation for details.</summary>

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1401:PInvokesShouldNotBeVisible"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressUnmanagedCodeSecurity]
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]

        public static extern uint TimeEndPeriod(uint uMilliseconds);
    }

}
