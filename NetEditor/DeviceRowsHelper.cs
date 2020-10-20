using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace NetEditor
{
    static class DeviceRowsHelper
    {
        public static Dictionary<int, NetworkDeviceItem> DeviceByRowID { get; } = new Dictionary<int, NetworkDeviceItem>();

        public static DataGridViewRow InitializeRow(int Id, NetworkDeviceItem netDeviceItem, IoSystemLevel ioSystemLevel, DataGridView dgvWithExpectedStyle)
        {
            DeviceRowsHelper.DeviceByRowID.Add(Id, netDeviceItem);

            DataGridViewRow row = new DataGridViewRow();
            row.CreateCells(dgvWithExpectedStyle);
            row.SetValues(CreateRecord(Id, netDeviceItem, ioSystemLevel));

            return row;
        }
        
        public static void MarkRepeatingIPs(DataGridViewRowCollection rows, int IPColumnInd, int ModeColumnInd)
        {
            foreach (DataGridViewRow row in rows)
            {
                DataGridViewCell cell = row.Cells[IPColumnInd];
                if (!string.IsNullOrEmpty(cell.Value.ToString()))
                {
                    cell.Style.BackColor = System.Drawing.SystemColors.Control;
                }
                else
                {
                    cell.Style.BackColor = System.Drawing.SystemColors.ControlLight;
                }
            }

            foreach (int index in DeviceRowsHelper.FindRepeatingIPs(rows, IPColumnInd, ModeColumnInd))
            {
                rows[index].Cells[IPColumnInd].Style.BackColor = System.Drawing.Color.Pink;
            }
        }

        public static void DisableCell(DataGridViewCell cell)
        {
            cell.ReadOnly = true;
            cell.Style.BackColor = System.Drawing.SystemColors.ControlLight;
            cell.Style.ForeColor = System.Drawing.SystemColors.GrayText;
        }

        public static void DisableCells(params DataGridViewCell[] cells)
        {
            foreach (var cell in cells)
            {
                DisableCell(cell);
            }
        }

        public static void PadCell(DataGridViewCell cell, int padding)
        {
            cell.Style.Padding = new Padding(padding, 0, 0, 0);

        }

        private static List<int> FindRepeatingIPs(DataGridViewRowCollection rows, int IPColumnInd, int ModeColumnInd)
        {
            Dictionary<string, int> existingIPs = new Dictionary<string, int>();
            List<int> indicesWithRepetingIPs = new List<int>();

            foreach (DataGridViewRow row in rows)
            {
                object obj = row.Cells[IPColumnInd].Value;
                if (obj == null) continue;
                string deviceIP = obj.ToString();
                if (existingIPs.ContainsKey(deviceIP))
                {
                    string firstMode = rows[existingIPs[deviceIP]].Cells[ModeColumnInd].Value.ToString();
                    string secondMode = row.Cells[ModeColumnInd].Value.ToString();
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
        private static string[] CreateRecord(int Id, NetworkDeviceItem netDeviceItem, IoSystemLevel ioSystemLevel)
        {
            //IoSystemLevel ioSystemLevel_proper = netDeviceItem.IoSystemLevel; // work on that

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
    }
}
