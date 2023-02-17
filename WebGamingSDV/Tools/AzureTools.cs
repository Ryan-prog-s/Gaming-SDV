using Azure.Core;
using Azure;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure.Identity;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using NuGet.Protocol.Plugins;

namespace WebGamingSDV.Models
{
    public class AzureTools
    {

        private SubscriptionResource subscription;
        private string resourceName;


        public AzureTools(string resourceName)
        {

            // Connection to Azure
            string tenantID = "b7b023b8-7c32-4c02-92a6-c8cdaa1d189c";
            string clientID = "239cafd2-9307-47f9-81ec-c61aa2b4f36b";
            string clientSecret = "0fZ8Q~5jYzIyiACWaEhQJrQIR0wJnbpb3o~V1a_O";

            ArmClient client = new ArmClient(new ClientSecretCredential(tenantID, clientID, clientSecret));

            subscription = client.GetDefaultSubscription();

            this.resourceName = resourceName;
        }

        public async Task<ResourceGroupResource> GetResourceGroupAsync()
        {

            ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();

            // With the collection, we can create a new resource group with an specific name
            string resourceGroupName = $"rg-{resourceName}";

            // Check if resource exist or not
            bool exists = await resourceGroups.ExistsAsync(resourceGroupName);

            // If not exists
            if (!exists)
            {
                // Set location
                var location = AzureLocation.FranceCentral;
                var resourceGroupData = new ResourceGroupData(location);

                // Create Resource group
                await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, resourceGroupData);
            }

            // Return new resource group
            return await resourceGroups.GetAsync(resourceGroupName);
        }

        private PublicIPAddressResource CreatePublicIp(ResourceGroupResource resourceGroup)
        {
            string ipName = $"ip-{resourceName}";
            PublicIPAddressCollection publicIps = resourceGroup.GetPublicIPAddresses();

            //Create public ip
            return publicIps.CreateOrUpdate(
                WaitUntil.Completed,
                ipName,
                new PublicIPAddressData()
                {
                    PublicIPAddressVersion = NetworkIPVersion.IPv4,
                    PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    Location = AzureLocation.FranceCentral
                }).Value;
        }

        private VirtualNetworkResource CreateVnet(ResourceGroupResource resourceGroup)
        {
            string vnetName = $"vnet-{resourceName}";
            VirtualNetworkCollection vnc = resourceGroup.GetVirtualNetworks();

            // Create vnetResource
            return vnc.CreateOrUpdate(
                WaitUntil.Completed,
                vnetName,
                new VirtualNetworkData()
                {
                    Location = AzureLocation.FranceCentral,
                    Subnets =
                    {
                    new SubnetData()
                    {
                        Name = "VMsubnet",
                        AddressPrefix = "10.0.0.0/24"
                    }
                    },
                    AddressPrefixes =
                    {
                    "10.0.0.0/16"
                    },
                }).Value;

        }

        private NetworkInterfaceResource CreateNetworkInterface(VirtualNetworkResource vnet, PublicIPAddressResource ipAddress, ResourceGroupResource resourceGroup)
        {
            string nicName = $"nic-{resourceName}";
            NetworkInterfaceCollection nics = resourceGroup.GetNetworkInterfaces();

            //Create network interface
            return nics.CreateOrUpdate(
                WaitUntil.Completed,
                nicName,
                new NetworkInterfaceData()
                {
                    Location = AzureLocation.FranceCentral,
                    IPConfigurations =
                    {
                    new NetworkInterfaceIPConfigurationData()
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new SubnetData() { Id = vnet?.Data.Subnets.First().Id },
                        PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddressData() { Id = ipAddress?.Data.Id }
                    }
                    }
                }).Value;
        }

        public VirtualMachineResource CreateVirtualMachine(ResourceGroupResource resourceGroup, string adminUsername, string adminPassword)
        {

            var ip = CreatePublicIp(resourceGroup);
            var vnet = CreateVnet(resourceGroup);
            var interfaceNetwork = CreateNetworkInterface(vnet, ip, resourceGroup);

            VirtualMachineCollection vms = resourceGroup.GetVirtualMachines();

            //Virtual machine
            return vms.CreateOrUpdate(
                WaitUntil.Completed,
                $"vm-{resourceName}",
                new VirtualMachineData(AzureLocation.FranceCentral)
                {
                    HardwareProfile = new VirtualMachineHardwareProfile()
                    {
                        VmSize = VirtualMachineSizeType.StandardB2S
                    },
                    OSProfile = new VirtualMachineOSProfile()
                    {
                        ComputerName = $"vm-{resourceName}",
                        AdminUsername = adminUsername,
                        AdminPassword = adminPassword,
                        WindowsConfiguration = new WindowsConfiguration
                        {
                            ProvisionVmAgent = true,
                        }
                    },
                    StorageProfile = new VirtualMachineStorageProfile()
                    {
                        OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                        {
                            DeleteOption = DiskDeleteOptionType.Delete,
                        },
                        ImageReference = new ImageReference()
                        {
                            Offer = "windows-10",
                            Publisher = "MicrosoftWindowsDesktop",
                            Sku = "19h2-pro-g2",
                            Version = "latest",
                        }
                    },
                    NetworkProfile = new VirtualMachineNetworkProfile()
                    {
                        NetworkInterfaces =
                        {
                        new VirtualMachineNetworkInterfaceReference()
                        {
                            Id = interfaceNetwork.Id
                        }
                        }
                    },
                }).Value;
        }

        public async void VmPowerOnAsync()
        {
            ResourceGroupResource resourceGroup = await GetResourceGroupAsync();

            var vms = resourceGroup.GetVirtualMachines();
            await vms.First().PowerOnAsync(WaitUntil.Completed);
        }

        public async void VmPowerOffsync()
        {
            ResourceGroupResource resourceGroup = await GetResourceGroupAsync();

            var vms = resourceGroup.GetVirtualMachines();
            await vms.First().PowerOffAsync(WaitUntil.Completed);
        }

        public async Task<string> GetIpAdress(string ipName)
        {
            ResourceGroupResource resourceGroup = await GetResourceGroupAsync();

            var publicIPAddresses = resourceGroup.GetPublicIPAddresses();
            PublicIPAddressResource publicIPAddress = publicIPAddresses.Get(ipName);

            return publicIPAddress.Data.IPAddress;
        }
    }
}
