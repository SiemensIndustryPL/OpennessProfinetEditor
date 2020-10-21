using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;


namespace NetEditor
{
    static class CSVExporter
    {
        public static void Export(ProjectLevel projectLevel)
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
    }
}
