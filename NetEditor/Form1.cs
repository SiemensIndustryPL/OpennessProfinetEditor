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
        
        private bool deviceTableAutoEdit = true;
        private ExclusiveAccess exclusiveAccess;
        private Transaction transaction;
        // it is needed because disposed access and transaction are not null, so it's hard to check for them.
        private bool accessAvailable = false;
        private bool transactionOpen = false;
        private SortBy lastSortingOrder = SortBy.Name;

        private static class ID
        {
            private static int nextID = 0;

            public static int NextID 
            {
                get 
                {
                    nextID++;
                    return nextID;
                }
            }

            public static void Clear()
            {
                nextID = 0;
            }

        }


        public TiaPortal MyTiaPortal { get; set; }
        public Project MyProject { get; set; }

        string version = "0.5";

        public Form1()
        {
            
            AppDomain CurrentDomain = AppDomain.CurrentDomain;
            CurrentDomain.AssemblyResolve += new ResolveEventHandler(MyResolver);
            InitializeComponent();

            // Double buffering can make DGV slow in remote desktop
            if (!System.Windows.Forms.SystemInformation.TerminalServerSession)
            {
                Type dgvType = deviceTable.GetType();
                PropertyInfo pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                pi.SetValue(deviceTable, true, null);
            }
            this.Text = $"Net Editor {version}";

            TextWriterTraceListener myListener = new TextWriterTraceListener(System.Console.Out);
            Debug.Listeners.Add(myListener);
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
            txt_Search.Enabled = false;

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

            PopulateDeviceTable(lastSortingOrder, txt_Search.Text);

            FillStatusBar();

            this.dgv_PnSubnet.HeaderCell.SortGlyphDirection = SortOrder.Descending;
            this.dgv_DeviceName.HeaderCell.SortGlyphDirection = SortOrder.Descending;
            this.dgv_IpAddress.HeaderCell.SortGlyphDirection = SortOrder.Descending;

            btn_ClearChanges.Enabled = true;
            btn_Disconnect.Enabled = true;
            btn_CommitChanges.Enabled = true;
            btn_ExportToCSV.Enabled = true;
            txt_Search.Enabled = true;
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

        private void PopulateDeviceTable(SortBy sortingOrder, string searchText)
        {
            if (projectLevel == null)
            {
                Debug.WriteLine("Nothing to populate device table with.");
                return;
            }

            deviceTableAutoEdit = true;
            deviceTable.Rows.Clear();
            ID.Clear();
            DeviceRowsHelper.DeviceByRowID.Clear();

            if (String.IsNullOrWhiteSpace(searchText) | searchText.Length <= 2)
            {
                deviceTable.Rows.AddRange(GetMatchingRows(sortingOrder).ToArray());
            }
            else
            {
                deviceTable.Rows.AddRange(GetMatchingRows(sortingOrder, searchText).ToArray());
            }
            deviceTableAutoEdit = false;

            DeviceRowsHelper.MarkRepeatingIPs(deviceTable.Rows, dgv_IpAddress.Index, dgv_Mode.Index);
        }

        private List<DataGridViewRow> GetMatchingRows(SortBy sortingOrder, string searchText)
        {
            List<DataGridViewRow> rows = new List<DataGridViewRow>();
            List<SubnetLevel> subnets = projectLevel.Subnets;

            EngineeringCompare.Sort(subnets, sortingOrder);
            subnets.ForEach(sn =>
            {
                EngineeringCompare.Sort(sn.IoSystems, sortingOrder);
                sn.IoSystems.ForEach(ioSystemLvl =>
                {
                    List<DataGridViewRow> ioDevicesRows = new List<DataGridViewRow>();
                    DataGridViewRow ioControllerRow = IoControllerRow(ioSystemLvl);

                    EngineeringCompare.Sort(ioSystemLvl.IoDevices, sortingOrder);
                    ioSystemLvl.IoDevices.ForEach(iod =>
                    {
                        DataGridViewRow ioDeviceRow = IoDeviceRow(iod, ioSystemLvl);
                        if (ContainsSearchText(ioDeviceRow, searchText))
                        {
                            ioDevicesRows.Add(ioDeviceRow);
                        }  
                    });

                    if (ContainsSearchText(ioControllerRow, searchText) | ioDevicesRows.Count != 0)
                    {
                        rows.Add(ioControllerRow);
                        rows.AddRange(ioDevicesRows);
                    }
                });

                EngineeringCompare.Sort(sn.SubnetLvlDevItems, sortingOrder);
                sn.SubnetLvlDevItems.ForEach(sdi =>
                {
                    DataGridViewRow subnetDeviceRow = SubnetDeviceRow(sdi);
                    if (ContainsSearchText(subnetDeviceRow, searchText))
                    {
                        rows.Add(subnetDeviceRow);
                    }
                });
            });

            EngineeringCompare.Sort(projectLevel.UnusedDeviceItems, sortingOrder);
            projectLevel.UnusedDeviceItems.ForEach(udi =>
            {
                DataGridViewRow unusedDeviceRow = UnusedDeviceRow(udi);
                if (ContainsSearchText(unusedDeviceRow, searchText))
                {
                    rows.Add(unusedDeviceRow);
                }
            });

            return rows;
        }

        private List<DataGridViewRow> GetMatchingRows(SortBy sortingOrder)
        {
            List<DataGridViewRow> rows = new List<DataGridViewRow>();
            List<SubnetLevel> subnets = projectLevel.Subnets;

            EngineeringCompare.Sort(subnets, sortingOrder);
            subnets.ForEach(sn =>
            {
                EngineeringCompare.Sort(sn.IoSystems, sortingOrder);
                sn.IoSystems.ForEach(ioSystemLvl =>
                {
                    rows.Add(IoControllerRow(ioSystemLvl));

                    EngineeringCompare.Sort(ioSystemLvl.IoDevices, sortingOrder);
                    ioSystemLvl.IoDevices.ForEach(iod =>
                    {
                        rows.Add(IoDeviceRow(iod, ioSystemLvl));
                    });
                });

                EngineeringCompare.Sort(sn.SubnetLvlDevItems, sortingOrder);
                sn.SubnetLvlDevItems.ForEach(sdi =>
                {
                    rows.Add(SubnetDeviceRow(sdi));
                });
            });

            EngineeringCompare.Sort(projectLevel.UnusedDeviceItems, sortingOrder);
            projectLevel.UnusedDeviceItems.ForEach(udi =>
            {
                rows.Add(UnusedDeviceRow(udi));
            });

            return rows;
        }

        private bool ContainsSearchText(DataGridViewRow row, string searchText)
        {
            bool containsSearchText = false;

            foreach (DataGridViewCell cell in row.Cells)
            {
                if (cell.Value.ToString().Contains(searchText))
                {
                    cell.Style.BackColor = System.Drawing.Color.Yellow;
                    containsSearchText = true;
                }
            }

            return containsSearchText;
        }

        private void FillStatusBar()
        {
            int allDevices = projectLevel.CountIoControllers + projectLevel.CountInSubnets + 
                             projectLevel.CountIoDevices + projectLevel.CountUnused;

            txt_StatusBar.Text = $"{allDevices} devices total ({projectLevel.CountIoControllers} IO controllers, " +
                                 $"{projectLevel.CountIoDevices} IO devices, {projectLevel.CountInSubnets} other in subnets, " +
                                 $"{projectLevel.CountUnused} unused)";
        }

        #region Events
        async void deviceTable_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (deviceTableAutoEdit) return;

            deviceTableAutoEdit = true;

            DataGridViewRow editedRow = deviceTable.Rows[e.RowIndex];
            DataGridViewCell editedCell = editedRow.Cells[e.ColumnIndex];
            string newValue = editedCell.Value?.ToString() ?? "";

            int id = int.Parse(editedRow.Cells[dgv_Id.Index].Value.ToString());
            NetworkDeviceItem netDeviceItem = DeviceRowsHelper.DeviceByRowID[id];

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
            deviceTableAutoEdit = false;

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

            DeviceRowsHelper.MarkRepeatingIPs(deviceTable.Rows, dgv_IpAddress.Index, dgv_Mode.Index);
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

            deviceTableAutoEdit = true;
            foreach (DataGridViewRow row in deviceTable.Rows)
            {
                if (row.Equals(editedRow)) continue; // it may be slow, look out

                string rowDeviceName = row.Cells[dgv_DeviceName.Index].Value.ToString();
                if (rowDeviceName != oldName) continue;

                deviceKey = int.Parse(row.Cells[dgv_Id.Index].Value.ToString());
                NetworkDeviceItem modifiedDI = DeviceRowsHelper.DeviceByRowID[deviceKey];

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
            deviceTableAutoEdit = false;
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
                return;
            }
            catch (EngineeringNotSupportedException ex)
            {
                editedCell.Value = oldMask;

                txt_Status.AppendText(ex.Message + Environment.NewLine);
                Debug.WriteLine(ex.Message);
                return;
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
                row.Cells[dgv_Mask.Index].Value = DeviceRowsHelper.DeviceByRowID[deviceKey].UpdateAddressMask();
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
                    subnetCell.Value = await DeviceRowsHelper.DeviceByRowID[deviceKey].UpdatePnSubnetName();
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
                    ioSystemCell.Value = await DeviceRowsHelper.DeviceByRowID[deviceKey].UpdateIoSystemName();
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
            txt_Search.Enabled = false;
        }

        private void DeviceTable_OnCellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            // End of edition on each click on column of checkbox
            if (e.ColumnIndex == dgv_autoGeneratePnName.Index && e.RowIndex != -1)
            {
                deviceTable.EndEdit();
            }
        }

        private void DeviceTable_Sort(object sender, DataGridViewCellMouseEventArgs e)
        {

            if (e.ColumnIndex == dgv_DeviceName.Index)
            {
                if (lastSortingOrder == SortBy.Name)
                {
                    PopulateDeviceTable(SortBy.NameRev, txt_Search.Text);
                    lastSortingOrder = SortBy.NameRev;
                    dgv_DeviceName.HeaderCell.SortGlyphDirection = SortOrder.Ascending;
                }
                else
                { 
                    PopulateDeviceTable(SortBy.Name, txt_Search.Text);
                    lastSortingOrder = SortBy.Name;
                    dgv_DeviceName.HeaderCell.SortGlyphDirection = SortOrder.Descending;
                }
            } 
            else if (e.ColumnIndex == dgv_IpAddress.Index)
            {
                if (lastSortingOrder == SortBy.IpAddress)
                {
                    PopulateDeviceTable(SortBy.IpAddressRev, txt_Search.Text);
                    lastSortingOrder = SortBy.IpAddressRev;
                    dgv_IpAddress.HeaderCell.SortGlyphDirection = SortOrder.Ascending;
                }
                else
                {
                    PopulateDeviceTable(SortBy.IpAddress, txt_Search.Text);
                    lastSortingOrder = SortBy.IpAddress;
                    dgv_IpAddress.HeaderCell.SortGlyphDirection = SortOrder.Descending;
                }
            }
            else if (e.ColumnIndex == dgv_PnSubnet.Index)
            {
                if (lastSortingOrder == SortBy.Subnet)
                {
                    PopulateDeviceTable(SortBy.SubnetRev, txt_Search.Text);
                    lastSortingOrder = SortBy.SubnetRev;
                    dgv_PnSubnet.HeaderCell.SortGlyphDirection = SortOrder.Ascending;
                }
                else
                {
                    PopulateDeviceTable(SortBy.Subnet, txt_Search.Text);
                    lastSortingOrder = SortBy.Subnet;
                    dgv_PnSubnet.HeaderCell.SortGlyphDirection = SortOrder.Descending;
                }
            }
        }

        private void ExportToCSV(object sender, EventArgs e)
        {
            CSVExporter.Export(projectLevel);
        }
        #endregion

        private DataGridViewRow IoControllerRow(IoSystemLevel ioSystem)
        {
            var ioControllerRow = DeviceRowsHelper.InitializeRow(ID.NextID, ioSystem.IoController, ioSystem, deviceTable);
            var cells = ioControllerRow.Cells;
            var deviceNameCell = cells[dgv_DeviceName.Index];

            DeviceRowsHelper.PadCell(deviceNameCell, 0);
            DeviceRowsHelper.DisableCell(cells[dgv_PnNumber.Index]);

            if (!ioSystem.IoController.UseRouter) DeviceRowsHelper.DisableCell(cells[dgv_RouterAddress.Index]);

            return ioControllerRow;
        }

        private DataGridViewRow IoDeviceRow(NetworkDeviceItem ioDevice, IoSystemLevel ioSystem)
        {
            var ioDeviceRow = DeviceRowsHelper.InitializeRow(ID.NextID, ioDevice, ioSystem, deviceTable);
            var cells = ioDeviceRow.Cells;
            var deviceNameCell = cells[dgv_DeviceName.Index];

            DeviceRowsHelper.PadCell(deviceNameCell, 25);
            DeviceRowsHelper.DisableCells(cells[dgv_IoSystem.Index], cells[dgv_PnSubnet.Index],
                                          cells[dgv_RouterAddress.Index], cells[dgv_Mask.Index]);

            if (string.IsNullOrEmpty(cells[dgv_IpAddress.Index].Value?.ToString())) // empty in future?
            {
                DeviceRowsHelper.DisableCell(cells[dgv_IpAddress.Index]);
            }

            return ioDeviceRow;
        }

        private DataGridViewRow SubnetDeviceRow(NetworkDeviceItem subnetDevice)
        {
            var subnetDeviceRow = DeviceRowsHelper.InitializeRow(ID.NextID, subnetDevice, null, deviceTable);
            var cells = subnetDeviceRow.Cells;
            var deviceNameCell = cells[dgv_DeviceName.Index];

            DeviceRowsHelper.PadCell(deviceNameCell, 12);
            DeviceRowsHelper.DisableCells(cells[dgv_IoSystem.Index], cells[dgv_PnSubnet.Index],
                                          cells[dgv_RouterAddress.Index], cells[dgv_Mask.Index],
                                          cells[dgv_PnNumber.Index]);

            return subnetDeviceRow;
        }

        private DataGridViewRow UnusedDeviceRow(NetworkDeviceItem unusedDevice)
        {
            var unusedDeviceRow = DeviceRowsHelper.InitializeRow(ID.NextID, unusedDevice, null, deviceTable);
            var cells = unusedDeviceRow.Cells;
            var deviceNameCell = cells[dgv_DeviceName.Index];

            DeviceRowsHelper.PadCell(deviceNameCell, 5);
            DeviceRowsHelper.DisableCells(cells[dgv_IoSystem.Index], cells[dgv_PnSubnet.Index],
                                          cells[dgv_RouterAddress.Index], cells[dgv_Mask.Index],
                                          cells[dgv_PnNumber.Index]);

            return unusedDeviceRow;
        }

        private void txt_StatusBar_Click(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void DisplayOnlyFiltered(object sender, System.EventArgs e)
        {
            PopulateDeviceTable(lastSortingOrder, txt_Search.Text);
        }
    }

    enum SortBy
    {
        Name,
        NameRev,
        IpAddress,
        IpAddressRev,
        Subnet,
        SubnetRev
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
