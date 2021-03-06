﻿using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NetEditor
{
    public class NetworkDeviceItem
    {
        //public Node Node { get; private set; }
        private Node node;
        private DeviceItem deviceItem;
        private NetworkDeviceItemLevel itemLevel;


        // maybe create setters instead of "private set" and "update*" ? to be seen
        public string IpSelectedBy { get; private set; } = null;
        public string IpAddress { get; private set; } = null;
        public string AddressMask { get; private set; } = null;
        public string PnDeviceName { get; private set; } = null;
        public string PnSubnetName { get; private set; } = null;
        public string IoSystemName { get; private set; } = null;
        public string HMName { get; private set; } = null;
        public string InterfaceOperatingMode { get; private set; } = null;
        public string PnDeviceNumber { get; private set; } = null;
        public string RouterAddress { get; private set; } = null;

        public bool UseRouter { get; private set; } = false;
        public bool IpSelectedByProject { get; private set; } = false;
        public bool autoGeneratePnName { get; private set; } = false;

        public NetworkDeviceItemWorkMode workMode { get; }
        public IoSystemLevel IoSystemLevel { get; } = null;
        public SubnetLevel SubnetLevel { get; } = null;

        public static async Task<NetworkDeviceItem> Create(DeviceItem di, NetworkDeviceItemLevel ndil,  //ndil will be unnecessary now when ndi references levels
            NetworkDeviceItemWorkMode workMode, SubnetLevel subnetLevel, IoSystemLevel ioSystemLevel)
        {
            NetworkInterface ni = ((IEngineeringServiceProvider)di).GetService<NetworkInterface>();
            if (ni == null) return null;

            Node node = ni.Nodes[0];

            // This hack is for distinguishing between HMI and Ethernet Commissioning device.
            // The proper way would be to find some attribute that differentiates them but I could not find any
            // You could check for existence of any profinet attribute but GetAttributeInfos is **HEAVY**
            if (ni.IoConnectors.Count == 0 && ni.IoControllers.Count == 0)
            {
                try
                {
                    node.GetAttribute("PnDeviceNameAutoGeneration");
                }
                catch
                {
                    return null;
                }
            }

            string nodeType = node.GetAttribute("NodeType").ToString();
            if (!nodeType.Equals("Ethernet")) return null;

            var ndi = new NetworkDeviceItem(di, node, ndil, workMode, subnetLevel, ioSystemLevel);
            await ndi.UpdateAllParameters().ConfigureAwait(false);

            return ndi;
        }
        private NetworkDeviceItem(DeviceItem di, Node node, NetworkDeviceItemLevel ndil, NetworkDeviceItemWorkMode workMode,
            SubnetLevel subnetLevel, IoSystemLevel ioSystemLevel)
        {
            this.itemLevel = ndil;
            this.deviceItem = di;
            this.node = node;
            this.IoSystemLevel = ioSystemLevel;
            this.SubnetLevel = subnetLevel;
            this.workMode = workMode;
        }

        public async Task UpdateAllParameters()
        {
            var getAllNodeRelatedParametersTask = getAllNodeRelatedParameters(); // node
            var UpdatePnSubnetNameTask = UpdatePnSubnetName(); // subnet
            var UpdateHMNameTask = UpdateHMName(); // complicated
            var UpdateItfOperatingModeTask = UpdateItfOperatingMode(); // deviceitem
            var UpdatePnNumberTask = UpdatePnNumber(); // connector/controller
            var UpdateIoSystemNameTask = UpdateIoSystemName(); // this is weird and should be changed
            // ndi creation should include a reference to it's subnet.

            await Task.WhenAll(getAllNodeRelatedParametersTask, UpdatePnSubnetNameTask, UpdateHMNameTask,
                               UpdateItfOperatingModeTask, UpdatePnNumberTask, UpdateIoSystemNameTask
                               ).ConfigureAwait(false);
            
        }

        public Device Device()
        {
            var parent = deviceItem.Parent;
            while (parent != null && !(parent is Device))
            {
                parent = parent.Parent;
            }

            return parent as Device; //can be null, lookout
        }

        private async Task getAllNodeRelatedParameters()
        {
            IpSelectedBy = node.GetAttribute("IpProtocolSelection").ToString();
            IpSelectedByProject = IpSelectedBy.Equals("Project");

            if (IpSelectedByProject)
            {
                string[] nodeParams = new string[] { "Address", "SubnetMask", "PnDeviceName", "UseRouter", "PnDeviceNameAutoGeneration" };
                IList<object> nodeValues = await Task.Run(() => node.GetAttributes(nodeParams)).ConfigureAwait(false);

                this.IpAddress = nodeValues[0].ToString();
                this.AddressMask = nodeValues[1].ToString();
                this.PnDeviceName = nodeValues[2].ToString();
                this.UseRouter = (bool)nodeValues[3];
                this.autoGeneratePnName = (bool)nodeValues[4];

                if (UseRouter) this.RouterAddress = node.GetAttribute("RouterAddress").ToString();
            }
            else if (itemLevel != NetworkDeviceItemLevel.Project)
            {
                // ipaddress and addressmask are null
                this.PnDeviceName = await Task.Run(() => node.GetAttribute("PnDeviceName").ToString()).ConfigureAwait(false);
            } // otherwise any of those is not set

        }

        #region API Calls for update

        public string UpdateIPAddress()
        {
            if (IpSelectedByProject) IpAddress = node.GetAttribute("Address").ToString();
            return IpAddress;
        }

        public string UpdateAddressMask()
        {
            if (IpSelectedByProject) AddressMask = node.GetAttribute("SubnetMask").ToString();
            return AddressMask;
        }

        public string UpdateRouterAddress()
        {
            if (UseRouter) RouterAddress = node.GetAttribute("RouterAddress").ToString();
            return RouterAddress;
        }

        public async Task<string> UpdateHMName()
        {
            // would be useful to move to separate method but HMI handling makes it tricky.
            DeviceItem parent = (DeviceItem)deviceItem.Parent;
            DeviceItem grandpa;
            HMName = null;

            while (!(parent.Classification == DeviceItemClassifications.HM || parent.Classification == DeviceItemClassifications.CPU))
            {
                grandpa = parent.Parent as DeviceItem;

                // Handling for HMI. NW structure is different compared to other devices.
                // Get name if Device Level is reached
                if (grandpa == null)
                {
                    HMName = ((Device)parent.Parent).Name; //god knows if it works, a second earlier it was null, wth
                    return HMName;
                }
                parent = grandpa;

            }
            HMName = parent.Name;
            return HMName;
        }

        public string UpdatePnDeviceName()
        {
            this.PnDeviceName = null;
            // PN device name can also be set at device level, check for that
            // it assumes that device not in subnet cannot have pn name whichc is not exactly true I think but good enough for now
            if (itemLevel != NetworkDeviceItemLevel.Project)
            {
                this.PnDeviceName = node.GetAttribute("PnDeviceName").ToString();
            }
            return this.PnDeviceName;
        }

        public async Task<string> UpdateItfOperatingMode()
        {
            
            this.InterfaceOperatingMode = 
                await Task.Run(() => deviceItem.GetAttribute("InterfaceOperatingMode").ToString()).ConfigureAwait(false);
            
            // deviceItem.GetAttribute("InterfaceOperatingMode").ToString();


            return this.InterfaceOperatingMode;
        }

        public async Task<string> UpdatePnSubnetName()
        {
            this.PnSubnetName = null;

            if (itemLevel != NetworkDeviceItemLevel.Project)
            {
                await Task.Run(() => this.PnSubnetName = node.ConnectedSubnet.Name).ConfigureAwait(false);
            }

            return this.PnSubnetName;
        }

        public async Task<string> UpdateIoSystemName()
        {
            // at some point it will be wiser to add a reference to subnet in Network Device Item constructor
            // then this functions will become super simple and always right.
            // this is good for now.
            IoSystemName = null;

            // this will not work correctly if it needs second ioconnector or something.
            if (itemLevel != NetworkDeviceItemLevel.IoSystem) return this.IoSystemName; //null

            await Task.Run(() =>
            {
                NetworkInterface itf = ((IEngineeringServiceProvider)deviceItem).GetService<NetworkInterface>();

                if (itf.IoConnectors.Count != 0)
                {
                    IoSystem ioSystem = itf.IoConnectors[0].ConnectedToIoSystem;
                    // the fact that I need that ?? means some devices classified as ioSystem level are not in iosystem.
                    // most likely it's broken cause I hardcoded 0. I will think a bit more about it.
                    this.IoSystemName = ioSystem?.Name;// ?? null;
                }
                else if (itf.IoControllers.Count == 1)
                {
                    IoSystem ioSystem = itf.IoControllers[0].IoSystem;
                    this.IoSystemName = ioSystem?.Name;// ?? null;
                }
            });

            return IoSystemName;
        }

        public bool UpdatePnDeviceNameAutoGeneration()
        {
            //this.autoGeneratePnName = false;
            this.autoGeneratePnName = (bool)node.GetAttribute("PnDeviceNameAutoGeneration");

            return this.autoGeneratePnName;
        }

        public async Task<string> UpdatePnNumber()
        {
            this.PnDeviceNumber = null;
            if (itemLevel == NetworkDeviceItemLevel.Project) return this.PnDeviceNumber; //null

            await Task.Run(() =>
            {
                NetworkInterface itf = ((IEngineeringServiceProvider)deviceItem).GetService<NetworkInterface>();

                // this will not work correctly if it needs second ioconnector or something.
                // also, GetAttributeInfos is really heavy
                if (itf.IoControllers.Count == 1)
                {
                    this.PnDeviceNumber = itf.IoControllers[0].GetAttribute("PnDeviceNumber").ToString();
                }
                else if(itf.IoConnectors.Count != 0 && itf.IoConnectors[0].GetAttributeInfos().Any((x) => x.Name == "PnDeviceNumber"))
                {
                    this.PnDeviceNumber = itf.IoConnectors[0].GetAttribute("PnDeviceNumber").ToString();
                }

            }).ConfigureAwait(false);

            return this.PnDeviceNumber;
        }
        #endregion

        #region API Calls for change

        public async Task ChangeIPAddress(string newAddress)
        {
            if (IpSelectedByProject)
            {
                IPAddress ip;
                bool addressIsValid = IPAddress.TryParse(newAddress, out ip);

                if (addressIsValid)
                {
                    await Task.Run(() =>
                    {
                        node.SetAttribute("Address", ip.ToString());
                        this.IpAddress = node.GetAttribute("Address").ToString();
                    }).ConfigureAwait(false);
                }
                else
                {
                    throw new ArgumentException("Provided input is not a valid IP address.");
                }
            }
            else
            {
                throw new EngineeringNotSupportedException("Changing IP address is not supported for this IP protocol selection mode. " +
                    "You can change it in TIA Portal.");
            }
        }

        public async Task ChangeAddressMask(string newMask)
        {
            //AddressMask = null;

            if (!IpSelectedByProject) throw new EngineeringNotSupportedException(
                                                "Changing address mask is not supported for this IP selection mode."
                                                + " You can change it in TIA Portal.");
            
            IPAddress mask;
            bool addressIsValid = IPAddress.TryParse(newMask, out mask);
            if (!addressIsValid) throw new ArgumentException($"{newMask} is not a valid mask.");

            string[] parts = mask.ToString().Split('.');
            bool allowedByTIAP = !parts.Any((p) => !(p.Equals("0") || p.Equals("255")));
            if (!allowedByTIAP) throw new ArgumentException($"{mask} is not a mask allowed by TIA Portal.");

            await Task.Run(() => node.SetAttribute("SubnetMask", mask.ToString())).ConfigureAwait(false);    
            AddressMask = await Task.Run(() => node.GetAttribute("SubnetMask").ToString()).ConfigureAwait(false);
        }

        public void ChangeRouterAddress(string newAddress)
        {
            this.RouterAddress = null;
            if (UseRouter)
            {
                IPAddress ip;
                bool addressIsValid = IPAddress.TryParse(newAddress, out ip);

                if (addressIsValid)
                {
                    node.SetAttribute("RouterAddress", ip.ToString());
                    this.RouterAddress = node.GetAttribute("RouterAddress").ToString();
                }
                else
                {
                    throw new ArgumentException("Provided input is not a valid IP address.");
                }
            }
            else
            {
                throw new EngineeringNotSupportedException("This device does not use router. You can change it in TIA Portal.");
            }

        }

        public async Task ChangeName(string newName)
        {
            await Task.Run(() =>
            {
                DeviceItem parent = (DeviceItem)deviceItem.Parent;
                DeviceItem grandpa;
                while (!(parent.Classification == DeviceItemClassifications.HM || parent.Classification == DeviceItemClassifications.CPU))
                {
                    grandpa = parent.Parent as DeviceItem;

                    // Handling for HMI. NW structure is different compared to other devices.
                    // Get name if Device Level is reached
                    if (grandpa == null)
                    { // can't remember if it was broken or I did goofed up
                        return; //in future, write some kind of exception here or sth
                    }
                    parent = grandpa;
                }

                // because of the way transactions work, it cannot cause any exceptions for the program to work
                // the best solution would be to check here for all the names in project
                // in a static dictionary of DeviceItem, string name
                // for now, devicetable logic will take care of this.
                try
                {
                    parent.SetAttribute("Name", newName);
                }
                catch (EngineeringTargetInvocationException)
                {
                    parent.SetAttribute("Name", newName + "_1");
                }

                HMName = parent.Name;
            }).ConfigureAwait(false);

            if (autoGeneratePnName) UpdatePnDeviceName();
        }

        public async Task ChangePnDeviceName(string newPnName)
        {
            this.PnDeviceName = null;
            // I think it makes sense that you cannot change PN name of device not in PN net.
            // I may be wrong and it might be useful.
            if (itemLevel == NetworkDeviceItemLevel.Project) return;

            await Task.Run(() =>
            {
                if (String.IsNullOrEmpty(newPnName))
                {
                    node.SetAttribute("PnDeviceNameAutoGeneration", true);
                }
                else
                {
                    node.SetAttribute("PnDeviceNameAutoGeneration", false);
                    node.SetAttribute("PnDeviceName", newPnName);
                }

                this.PnDeviceName = node.GetAttribute("PnDeviceName").ToString();
                this.autoGeneratePnName = (bool)node.GetAttribute("PnDeviceNameAutoGeneration");
            });
        }
        
        public void ChangePnDeviceNameAutoGeneration(bool newAuto)
        {
            node.SetAttribute("PnDeviceNameAutoGeneration", newAuto);
            this.autoGeneratePnName = (bool)node.GetAttribute("PnDeviceNameAutoGeneration");
        }

        public void ChangePnNumber(int newPnNumber)
        {
            this.PnDeviceNumber = null;
            // this will not work correctly if it needs second ioconnector or something.
            NetworkInterface itf = ((IEngineeringServiceProvider)deviceItem).GetService<NetworkInterface>();
            if (itemLevel == NetworkDeviceItemLevel.Project) return;

            if (itf.IoConnectors.Count != 0 && itf.IoConnectors[0].GetAttributeInfos().Any((x) => x.Name == "PnDeviceNumber"))
            {
                itf.IoConnectors[0].SetAttribute("PnDeviceNumber", newPnNumber);
                this.PnDeviceNumber = itf.IoConnectors[0].GetAttribute("PnDeviceNumber").ToString();
            }
        }

        public void ChangeSubnetName(string newSubnetName) //TODO: await; exception for why it wasn't changed
        {
            this.PnSubnetName = null;
            if (itemLevel != NetworkDeviceItemLevel.Project && !String.IsNullOrEmpty(newSubnetName))
            {
                node.ConnectedSubnet.SetAttribute("Name", newSubnetName);
                this.PnSubnetName = node.ConnectedSubnet.GetAttribute("Name").ToString();
            }
        }

        public void ChangeIoSystemName(string newIoSystemName)
        {
            this.IoSystemName = null;
            NetworkInterface itf = ((IEngineeringServiceProvider)deviceItem).GetService<NetworkInterface>();
            if (itemLevel != NetworkDeviceItemLevel.IoSystem) return;

            IoSystem ioSystem;
            if (itf.IoConnectors.Count != 0)
            {
                ioSystem = itf.IoConnectors[0].ConnectedToIoSystem;
            }
            else if (itf.IoControllers.Count == 1)
            {
                ioSystem = itf.IoControllers[0].IoSystem;
            }
            else return;

            if (ioSystem != null)
            {
                ioSystem.SetAttribute("Name", newIoSystemName);
                this.IoSystemName = ioSystem.GetAttribute("Name").ToString();
            }
        }
        #endregion
    }

    public enum NetworkDeviceItemLevel
    {
        Project,
        Subnet,
        IoSystem
    }

    public enum NetworkDeviceItemWorkMode
    {
        IoController,
        IoDevice,
        None
    }

    [Serializable]
    public class DeviceItemNotValidException : Exception
    {
        public DeviceItemNotValidException()
        {
        }

        public DeviceItemNotValidException(string message)
            : base(message)
        {
        }

        public DeviceItemNotValidException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected DeviceItemNotValidException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }
    }
}
