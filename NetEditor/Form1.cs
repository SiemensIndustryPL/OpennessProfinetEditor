using Microsoft.Win32;
using Siemens.Engineering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private bool accessAvailable = false;
        private bool transactionOpen = false;

        public TiaPortal MyTiaPortal { get; set; }
        public Project MyProject { get; set; }

        string version = "0.3 "; //add space after number

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
                PropertyInfo pi = dgvType.GetProperty("DoubleBuffered",
                  BindingFlags.Instance | BindingFlags.NonPublic);
                pi.SetValue(deviceTable, true, null);
            }
            this.Text = "Net Editor " + this.version;
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

        private async Task DeployEditor()
        {
            btn_ClearChanges.Enabled = false;
            btn_Disconnect.Enabled = false;
            btn_CommitChanges.Enabled = false;
            btn_ExportToCSV.Enabled = false;
            // null projectlevel here?
            await ConnectToTiaPortalProject(Dispatcher.CurrentDispatcher).ConfigureAwait(true);

            if (projectLevel == null) return;
            txt_Status.AppendText("Connected to TIA Portal project: " + MyProject.Path.ToString() + Environment.NewLine);
            this.Text = "Net Editor " + version + MyProject.Path.ToString();
            PopulateDeviceTable();
            MarkRowsWithRepeatingIPs();
           
            btn_ClearChanges.Enabled = true;
            btn_Disconnect.Enabled = true;
            btn_CommitChanges.Enabled = true;
            btn_ExportToCSV.Enabled = true;
        }

        private async Task ConnectToTiaPortalProject(Dispatcher dispatcher)
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

                            if (MyTiaPortal.Projects.Count <= 0)
                            {
                                dispatcher.Invoke(() =>
                                {
                                    txt_Status.AppendText("No TIA Portal Project was found!" + Environment.NewLine);
                                    btn_Connect.Enabled = true;
                                });
                                return;
                            }

                            MyProject = MyTiaPortal.Projects[0];
                            break;
                        case 0:
                            dispatcher.Invoke(() =>
                            {
                                txt_Status.AppendText("No running instance of TIA Portal was found!" + Environment.NewLine);
                                btn_Connect.Enabled = true;
                            });
                            return;
                        default:
                            dispatcher.Invoke(() =>
                            {
                                txt_Status.AppendText("More than one running instance of TIA Portal was found!" + Environment.NewLine);
                                btn_Connect.Enabled = true;
                            });
                            return;
                    }
                    exclusiveAccess = MyTiaPortal.ExclusiveAccess("My Activity");
                    accessAvailable = true;
                }

                if (!transactionOpen)
                {
                    transaction = exclusiveAccess.Transaction(MyProject, "No changes commited.");
                    transactionOpen = true;
                }
            }).ConfigureAwait(true);

            if (MyProject == null) return;
            rowDevices.Clear();
            projectLevel = await ProjectLevel.Create(MyProject);
        }

        private void PopulateDeviceTable()
        {
            dataTableAutoEdit = true;

            deviceTable.Rows.Clear();

            List<IoSystemLevel> AllIoSystems = new List<IoSystemLevel>();
            List<DataGridViewRow> rows = new List<DataGridViewRow>();
            int id;

            List<SubnetLevel> subnets = projectLevel.Subnets;
            //subnets.Sort()
            //List.com
            projectLevel.Subnets.Sort(CompareByName);

            subnets.ForEach(sn =>
            {
                //AllIoSystems.AddRange(sn.IoSystems);
                sn.IoSystems.Sort(CompareByControllerName);
                sn.IoSystems.ForEach(ioSystemLvl =>
                {
                    id = rows.Count + 1;
                    rows.Add(CreateTableRecord(id, ioSystemLvl.IoController, ioSystemLvl));
                    
                    DataGridViewCellCollection cells = rows[rows.Count - 1].Cells;
                    cells[dgv_DeviceName.Index].Style.Padding = new Padding(0, 0, 0, 0);
                    DisableCell(cells[dgv_PnNumber.Index]);
                    if (! ioSystemLvl.IoController.UseRouter) DisableCell(cells[dgv_RouterAddress.Index]);

                    ioSystemLvl.IoDevices.Sort(CompareByHMName);
                    ioSystemLvl.IoDevices.ForEach(iod =>
                    {
                        id = rows.Count + 1;
                        rows.Add(CreateTableRecord(id, iod, ioSystemLvl));
                        // padding on name column emulates some kind of tree structure
                        cells = rows[rows.Count - 1].Cells;
                        cells[dgv_DeviceName.Index].Style.Padding = new Padding(25, 0, 0, 0);

                        DisableCells(cells[dgv_IoSystem.Index], cells[dgv_PnSubnet.Index], 
                            cells[dgv_RouterAddress.Index], cells[dgv_Mask.Index]);
                        
                        if (string.IsNullOrEmpty(cells[dgv_IpAddress.Index].Value?.ToString())) // empty in future?
                        {
                            DisableCell(cells[dgv_IpAddress.Index]);
                        }
                    });
                });
                //rows.Sort()
                sn.SubnetLvlDevItems.Sort(CompareByHMName);
                sn.SubnetLvlDevItems.ForEach(sdi =>
                {
                    id = rows.Count + 1;
                    rows.Add(CreateTableRecord(id, sdi, null));

                    DataGridViewCellCollection cells = rows[rows.Count - 1].Cells;
                    cells[dgv_DeviceName.Index].Style.Padding = new Padding(12, 0, 0, 0);

                    DisableCells(cells[dgv_IoSystem.Index], cells[dgv_PnSubnet.Index],
                        cells[dgv_RouterAddress.Index], cells[dgv_Mask.Index], cells[dgv_PnNumber.Index]);
                });
            });

            projectLevel.UnusedDeviceItems.Sort(CompareByHMName);
            projectLevel.UnusedDeviceItems.ForEach(udi =>
            {
                rows.Add(CreateTableRecord(rows.Count + 1, udi, null));
                
                DataGridViewCellCollection cells = rows[rows.Count - 1].Cells;
                cells[dgv_DeviceName.Index].Style.Padding = new Padding(5, 0, 0, 0);

                DisableCells(cells[dgv_IoSystem.Index], cells[dgv_PnSubnet.Index],
                        cells[dgv_RouterAddress.Index], cells[dgv_Mask.Index], cells[dgv_PnNumber.Index]);
            });

            deviceTable.Rows.AddRange(rows.ToArray());
            //deviceTable.Rows.

            dataTableAutoEdit = false;
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
            string oldName = netDeviceItem.HMName;

            if (e.ColumnIndex == dgv_IpAddress.Index)
            {
                await EditIPAddressCell(netDeviceItem, editedCell, newValue).ConfigureAwait(true);
            }
            else if (e.ColumnIndex == dgv_DeviceName.Index)
            {
                await EditDeviceNameCell(netDeviceItem, editedRow, editedCell, newValue);
            }
            else if (e.ColumnIndex == dgv_Mask.Index)
            {
                await EditMaskCell(netDeviceItem, editedCell, newValue);
            }
            else if (e.ColumnIndex == dgv_PnDeviceName.Index)
            {
                netDeviceItem.ChangePnDeviceName(newValue);
                editedCell.Value = netDeviceItem.UpdatePnDeviceName();
                editedRow.Cells[dgv_autoGeneratePnName.Index].Value = netDeviceItem.UpdatePnDeviceNameAutoGeneration();
            }
            else if (e.ColumnIndex == dgv_PnSubnet.Index)
            {
                netDeviceItem.ChangeSubnetName(newValue);
                // some info for user could be useful if it doesn't take already taken name
                string notConnected = "[Not connected]";
                int deviceKey;
                foreach (DataGridViewRow row in deviceTable.Rows)
                {
                    deviceKey = int.Parse(row.Cells[dgv_Id.Index].Value.ToString());
                    row.Cells[dgv_PnSubnet.Index].Value = await rowDevices[deviceKey].UpdatePnSubnetName() ?? notConnected;
                    //if (row.Cells[dgv_PnSubnet.Index].Value == notConnected) row.Cells[dgv_PnSubnet.Index].ReadOnly = true;
                }
            }
            else if (e.ColumnIndex == dgv_IoSystem.Index)
            {
                netDeviceItem.ChangeIoSystemName(newValue);

                string notConnected = "[Not connected]";
                int deviceKey;
                foreach (DataGridViewRow row in deviceTable.Rows)
                {
                    deviceKey = int.Parse(row.Cells[dgv_Id.Index].Value.ToString());
                    row.Cells[dgv_IoSystem.Index].Value = rowDevices[deviceKey].UpdateIoSystemName() ?? notConnected;
                }
            }
            else if (e.ColumnIndex == dgv_autoGeneratePnName.Index)
            {
                bool auto = Boolean.Parse(newValue);
                netDeviceItem.ChangePnDeviceNameAutoGeneration(auto);
                editedCell.Value = netDeviceItem.UpdatePnDeviceNameAutoGeneration();
                editedRow.Cells[dgv_PnDeviceName.Index].Value = netDeviceItem.UpdatePnDeviceName();
            }
            else if (e.ColumnIndex == dgv_PnNumber.Index)
            {
                int pnNumber;
                if (int.TryParse(newValue, out pnNumber)) netDeviceItem.ChangePnNumber(pnNumber);

                editedCell.Value = await netDeviceItem.UpdatePnNumber();
            }
            else if (e.ColumnIndex == dgv_RouterAddress.Index)
            {
                try
                {
                    netDeviceItem.ChangeRouterAddress(newValue);
                    editedCell.Value = netDeviceItem.UpdateRouterAddress();
                    if (editedCell.Value.ToString() == newValue) txt_Status.AppendText($"new address: {newValue}{Environment.NewLine}");
                }
                catch (EngineeringNotSupportedException ex)
                {
                    txt_Status.AppendText(ex.Message + Environment.NewLine);
                    Debug.WriteLine(ex.Message);

                    editedCell.Value = netDeviceItem.UpdateRouterAddress();
                }
            }
            dataTableAutoEdit = false;

        }

        async Task EditIPAddressCell(NetworkDeviceItem editedNDI, DataGridViewCell editedCell, string proposedAddress)
        {
            string oldAddress = editedNDI.IpAddress;
            Task task = null;
            try
            {
                task = editedNDI.ChangeIPAddress(proposedAddress);
                await task;

                string newAddress = editedNDI.IpAddress;
                editedCell.Value = newAddress;
                if (editedNDI.IpAddress == proposedAddress)
                {
                    txt_Status.AppendText($"Device {editedNDI.HMName} changed IP address from: {oldAddress} to: {newAddress}." +
                        $"{Environment.NewLine}");
                }

                MarkRowsWithRepeatingIPs();
            }
            catch (AggregateException ae)
            {
                ae.Handle(ex =>
                {
                    if (ex is EngineeringNotSupportedException)
                    {
                        editedCell.Value = oldAddress;

                        txt_Status.AppendText(ex.Message + Environment.NewLine);
                        Debug.WriteLine(ex.Message);
                    }
                    return ex is EngineeringNotSupportedException;
                });

            }
        }

        async Task EditDeviceNameCell(NetworkDeviceItem editedNDI, DataGridViewRow editedRow, DataGridViewCell editedCell, string proposedName)
        {
            string oldName = editedNDI.HMName;
            string oldPnName = editedNDI.PnDeviceName;

            if (oldName == proposedName) return; // place for sanity check

            await editedNDI.ChangeName(proposedName).ConfigureAwait(true);
            string newName = editedNDI.HMName;
            editedCell.Value = newName;
            txt_Status.AppendText($"Device {oldName} changed name to: {newName}.{Environment.NewLine}");

            if (editedNDI.autoGeneratePnName)
            {
                string newPnName = editedNDI.PnDeviceName;

                editedRow.Cells[dgv_PnDeviceName.Index].Value = newPnName;
                txt_Status.AppendText($"Device {newName} changed Profinet device name " +
                    $"from: {oldPnName} to: {newPnName}.{Environment.NewLine}");
            }

            int deviceKey;
            foreach (DataGridViewRow row in deviceTable.Rows)
            {
                string rowDeviceName = row.Cells[dgv_DeviceName.Index].Value.ToString();
                if (rowDeviceName != oldName) continue;

                deviceKey = int.Parse(row.Cells[dgv_Id.Index].Value.ToString());
                NetworkDeviceItem modifiedDI = rowDevices[deviceKey];

                row.Cells[dgv_DeviceName.Index].Value = await modifiedDI.UpdateHMName().ConfigureAwait(true);

                if (modifiedDI.autoGeneratePnName)
                {
                    string oldPnNameM = modifiedDI.PnDeviceName;
                    string newPnNameM = modifiedDI.UpdatePnDeviceName();

                    // update for now, bc structure logic doesn't care about updating device names. That will change.
                    row.Cells[dgv_PnDeviceName.Index].Value = newPnNameM;
                    txt_Status.AppendText($"Device {newName} changed Profinet device name " +
                    $"from: {oldPnNameM} to: {newPnNameM}.{Environment.NewLine}");
                }
            }
        }

        async Task EditMaskCell(NetworkDeviceItem editedNDI, DataGridViewCell editedCell, string proposedMask)
        {
            string oldMask = editedNDI.AddressMask;
            try
            {
                await editedNDI.ChangeAddressMask(proposedMask);
            }
            catch (AggregateException ae)
            {
                ae.Handle(ex =>
                {
                    if (ex is EngineeringTargetInvocationException)
                    {
                        txt_Status.AppendText(ex.Message + Environment.NewLine);
                        Debug.WriteLine(ex.Message);

                        editedCell.Value = editedNDI.AddressMask;
                    }
                    return ex is EngineeringTargetInvocationException;
                });


            }

            string newMask = editedNDI.AddressMask;
            editedCell.Value = newMask;
            txt_Status.AppendText($"Device {editedNDI.HMName} changed address mask from: {oldMask} to: {newMask}." +
                $"{Environment.NewLine}");

            // only masks of devices in the same subnet (or iosystem) have changed so they should be filtered
            // actually, not really sure about that, some devices work both as IoControllers and IoDevices
            // in different IoSystems, so lets update everything to make sure.
            foreach (DataGridViewRow row in deviceTable.Rows)
            {
                int deviceKey = int.Parse(row.Cells[dgv_Id.Index].Value.ToString());
                row.Cells[dgv_Mask.Index].Value = rowDevices[deviceKey].UpdateAddressMask();
            }

        }

        private async void btn_ConnectTIA(object sender, EventArgs e)
        {
            btn_Connect.Enabled = false;
            this.progressBar.Style = ProgressBarStyle.Marquee;
            await DeployEditor();
            this.progressBar.Style = ProgressBarStyle.Continuous;
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
            //Maybe just copy the table?
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

        private void DisableCells(params DataGridViewCell[] cells)
        {
            foreach (var cell in cells)
            {
                DisableCell(cell);
            }
        }
        #endregion

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

        private static int CompareByControllerName(IoSystemLevel x, IoSystemLevel y)
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
                    return x.IoController.HMName.CompareTo(y.IoController.HMName);
                    //int retval = x.Length.CompareTo(y.Length);
                }
            }
        }

        private static int CompareByHMName(NetworkDeviceItem x, NetworkDeviceItem y)
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
