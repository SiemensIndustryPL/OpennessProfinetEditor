using Siemens.Engineering;
using Siemens.Engineering.HW;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetEditor
{
    public class ProjectLevel
    {
        public List<SubnetLevel> Subnets { get; private set; } = new List<SubnetLevel>();
        public List<NetworkDeviceItem> UnusedDeviceItems { get; private set; }
        public List<Device> UnusedDevices { get; private set; } 

        public ProjectLevel() { }

        public static async Task<ProjectLevel> Create(Project project)
        {
            var ret = new ProjectLevel();
            List<Device> usedDevices = new List<Device>();
            var subnetTasks = new List<Task>();

            foreach (Subnet subnet in project.Subnets) //foreach is not async
            {
                subnetTasks.Add(AddSubnetAndUsedDevices(ret, subnet, usedDevices));
            }

            await Task.WhenAll(subnetTasks).ConfigureAwait(false);


            ret.UnusedDevices = ret.GetAllProjectDevices(project).Except(usedDevices).ToList();
            ret.UnusedDeviceItems = await ret.GetUnusedDeviceItems().ConfigureAwait(false);

            return ret;
        }

        public static async Task AddSubnetAndUsedDevices(ProjectLevel pl, Subnet subnet, List<Device> usedDevices)
        {
            SubnetLevel subnetlevel = await SubnetLevel.Create(subnet).ConfigureAwait(false);
            pl.Subnets.Add(subnetlevel);
            usedDevices.AddRange(subnetlevel.GetDevices());
        }


        private List<Device> GetAllProjectDevices(Project project)
        {
            List<Device> devices = new List<Device>();
            // Add all project devices

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            devices.AddRange(project.Devices);

            foreach (DeviceUserGroup group in project.DeviceGroups)
            {
                devices.AddRange(GetAllGroupedDevices(group));
            }

            devices.AddRange(project.UngroupedDevicesGroup.Devices);

            return devices;
        }

        private List<Device> GetAllGroupedDevices(DeviceUserGroup group)
        {
            List<Device> devices = new List<Device>();

            devices.AddRange(group.Devices);

            foreach (DeviceUserGroup subgroup in group.Groups)
            {
                List<Device> subGroupDevices = GetAllGroupedDevices(subgroup);
                devices.AddRange(subGroupDevices);
            }
            return devices;
        }

        private async Task<List<NetworkDeviceItem>> GetUnusedDeviceItems()
        {
            var validDeviceItems = new List<NetworkDeviceItem>();
            var validDeviceItemTasks = new List<Task<NetworkDeviceItem>>();

            UnusedDevices.ForEach(ud =>
            {
                foreach (DeviceItem deviceItem in ud.DeviceItems)
                {
                    foreach (DeviceItem couldBeInterfaceItem in deviceItem.DeviceItems) // going one level deep
                    {
                        //NetworkDeviceItem ndi;
                        validDeviceItemTasks.Add(NetworkDeviceItem.Create(couldBeInterfaceItem, NetworkDeviceItemLevel.Project,
                            NetworkDeviceItemWorkMode.None, null, null));
                    }
                }
            });
            validDeviceItems.AddRange(await Task.WhenAll<NetworkDeviceItem>(validDeviceItemTasks).ConfigureAwait(false));
            validDeviceItems.RemoveAll((vdi) => vdi == null);
            return validDeviceItems;
        }
    }
}
