using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEditor
{
    class EngineeringCompare
    {
        public static void Sort(List<NetworkDeviceItem> sortedList, SortBy sortBy)
        {
            switch (sortBy)
            {
                // sorting by Name and Subnet is the same
                case SortBy.Name:
                    sortedList.Sort(CompareByDeviceName);
                    break;
                case SortBy.NameRev:
                    sortedList.Sort(CompareByDeviceNameRev);
                    break;
                case SortBy.Subnet:
                    sortedList.Sort(CompareByDeviceName);
                    break;
                case SortBy.SubnetRev:
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

        public static void Sort(List<SubnetLevel> sortedList, SortBy sortBy)
        {
            switch (sortBy)
            {
                // sorting by Name and Subnet is the same
                case SortBy.Name:
                    sortedList.Sort(CompareByName);
                    break;
                case SortBy.NameRev:
                    sortedList.Sort(CompareByNameRev);
                    break;
                case SortBy.Subnet:
                    sortedList.Sort(CompareByName);
                    break;
                case SortBy.SubnetRev:
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

        public static void Sort(List<IoSystemLevel> sortedList, SortBy sortBy)
        {
            switch (sortBy)
            {
                // sorting by Name and Subnet is the same
                case SortBy.Name:
                    sortedList.Sort(CompareByControllerName);
                    break;
                case SortBy.NameRev:
                    sortedList.Sort(CompareByControllerNameRev);
                    break;
                case SortBy.Subnet:
                    sortedList.Sort(CompareByControllerName);
                    break;
                case SortBy.SubnetRev:
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
            return -1 * CompareByName(x, y);
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
    }
}
