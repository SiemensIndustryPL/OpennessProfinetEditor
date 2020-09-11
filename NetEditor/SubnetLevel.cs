using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetEditor
{
    public class SubnetLevel
    {
        private Subnet subnet;
        public string SubnetName
        {
            get { return subnet.Name; }
        }

        public List<IoSystemLevel> IoSystems { get; private set; } = new List<IoSystemLevel>();

        public List<NetworkDeviceItem> SubnetLvlDevItems { get; private set; } = new List<NetworkDeviceItem>();

        public SubnetLevel(Subnet subnet)
        {
            this.subnet = subnet;
        }

        public static async Task<SubnetLevel> Create(Subnet subnet)
        {
            var ret = new SubnetLevel(subnet);

            ret.SubnetLvlDevItems = await ret.FindDevItemsWithoutIoSystem(); // create subnet builder so it can be multitasked
            
            var ioSystemTasks = new List<Task<IoSystemLevel>>();

            foreach (IoSystem anyIoSystem in subnet.IoSystems)
            {
                // maybe check IoSystem type instead of node type? // should work
                NetworkInterface controllerNetInterface = (NetworkInterface)anyIoSystem.Parent.Parent;
                string ioSystemType = controllerNetInterface.GetAttribute("InterfaceType").ToString();
                if (!ioSystemType.Equals("Ethernet")) continue;
                ioSystemTasks.Add(IoSystemLevel.Create(anyIoSystem, ret)); // this waits unnecessarily, bc foreach is not async
            }
            ret.IoSystems.AddRange(await Task.WhenAll(ioSystemTasks).ConfigureAwait(false));

            return ret;
        }

        //private static async 

        public List<Device> GetDevices()
        {
            List<Device> devices = new List<Device>();

            IoSystems.ForEach(ios => devices.AddRange(ios.GetDevices()));
            SubnetLvlDevItems.ForEach(di => devices.Add(di.Device()));

            return devices;
        }

        private async Task<List<NetworkDeviceItem>> FindDevItemsWithoutIoSystem()
        {
            List<NetworkDeviceItem> connectedToSubnetButNotIoSystem = new List<NetworkDeviceItem>();
            var subnetDeviceTasks = new List<Task<NetworkDeviceItem>>();

            foreach (Node node in subnet.Nodes)
            {
                NetworkInterface ni = (NetworkInterface)node.Parent;
                if (IsInIoSystem(ni)) continue;
                //string nodeType = node.GetAttribute("NodeType").ToString();
                //if (!nodeType.Equals("Ethernet")) continue;
                subnetDeviceTasks.Add(NetworkDeviceItem.Create((DeviceItem)ni.Parent, NetworkDeviceItemLevel.IoSystem,
                    NetworkDeviceItemWorkMode.None, this, null)); 
            }
            connectedToSubnetButNotIoSystem.AddRange(await Task.WhenAll<NetworkDeviceItem>(subnetDeviceTasks).ConfigureAwait(false));
            connectedToSubnetButNotIoSystem.RemoveAll((iod) => iod == null);

            return connectedToSubnetButNotIoSystem;
        }

        private static bool IsInIoSystem(NetworkInterface ni)
        {
            return !((ni.IoConnectors.Count == 0 && ni.IoControllers.Count == 0) || // not an IoDevice nor IoController
                (ni.IoConnectors.Count != 0 && ni.IoConnectors[0].ConnectedToIoSystem == null) || //not connected IoDevice
                (ni.IoControllers.Count != 0 && ni.IoControllers[0].IoSystem == null)); //not connected IoController
        } 
    }
}
