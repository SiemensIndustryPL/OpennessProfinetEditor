using Siemens.Engineering.HW;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetEditor
{
    public class IoSystemLevel
    {
        private IoSystem ioSystem;

        public List<NetworkDeviceItem> IoDevices { get; } = new List<NetworkDeviceItem>();
        public string IoSystemName { get; }
        public SubnetLevel SubnetLevel { get; }


        public NetworkDeviceItem IoController { get; private set; }


        private IoSystemLevel(IoSystem iosystem, SubnetLevel subnetLevel)
        {
            this.ioSystem = iosystem;
            IoSystemName = this.ioSystem.Name;
            SubnetLevel = subnetLevel;
        }
        public static async Task<IoSystemLevel> Create(IoSystem iosystem, SubnetLevel subnetLevel)
        {
            var ret = new IoSystemLevel(iosystem, subnetLevel);
            await ret.PopulateIoSystemLvl().ConfigureAwait(false);

            return ret;
        }

        private async Task PopulateIoSystemLvl()
        {
            DeviceItem controller = (DeviceItem)ioSystem.Parent.Parent.Parent;
            var ioDeviceTasks = new List<Task<NetworkDeviceItem>>();

            var IoControllerTask = NetworkDeviceItem.Create(controller, NetworkDeviceItemLevel.IoSystem, 
                NetworkDeviceItemWorkMode.IoController, SubnetLevel, this);

            foreach (var connector in ioSystem.ConnectedIoDevices)
            {
                DeviceItem di = (DeviceItem)connector.Parent.Parent;
                ioDeviceTasks.Add(NetworkDeviceItem.Create(di, NetworkDeviceItemLevel.IoSystem, 
                    NetworkDeviceItemWorkMode.IoDevice, SubnetLevel, this));
                
                //if (ioDevice != null) IoDevices.Add(ioDevice);
            }

            IoController = await IoControllerTask.ConfigureAwait(false);
            NetworkDeviceItem[] ioDevices = await Task.WhenAll<NetworkDeviceItem>(ioDeviceTasks).ConfigureAwait(false);
            IoDevices.AddRange(ioDevices.Where(iod => iod != null));
            //IoDevices.RemoveAll((iod) => iod == null);
        }

        public List<Device> GetDevices()
        {
            List<Device> devices = new List<Device>();

            devices.Add(IoController.Device());

            foreach (NetworkDeviceItem deviceItem in IoDevices)
            {
                devices.Add(deviceItem.Device());
            }

            return devices;

        }
    }
}
