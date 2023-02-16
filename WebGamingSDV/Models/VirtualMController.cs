using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Compute.Models;
using WebGamingSDV.Data;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System.Xml.Linq;
using System.Diagnostics;
using Microsoft.Azure.Management.Compute.Fluent;

namespace WebGamingSDV.Models
{
    public class VirtualMController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VirtualMController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: VirtualM
        public async Task<IActionResult> Index()
        {
              return _context.VirtualMs != null ? 
                          View(await _context.VirtualMs.ToListAsync()) :
                          Problem("Entity set 'ApplicationDbContext.VirtualM'  is null.");
        }

        // GET: VirtualM/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.VirtualMs == null)
            {
                return NotFound();
            }

            var VirtualM = await _context.VirtualMs
                .FirstOrDefaultAsync(m => m.name == id);
            if (VirtualM == null)
            {
                return NotFound();
            }

            return View(VirtualM);
        }

        // GET: VirtualM/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: VirtualM/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]     
        public async Task<IActionResult> Create([Bind("name,publicIp,login,password")] VirtualM VirtualM)
        {
            // Connection to Azure
            ArmClient client = new ArmClient(new DefaultAzureCredential());
            SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();
            ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();

            // With the collection, we can create a new resource group with an specific name

            string resourceGroupName = "rg-gaming-" + GetUserName();
            Console.WriteLine(resourceGroupName);
            AzureLocation location = AzureLocation.NorthEurope;
            ResourceGroupData resourceGroupData = new ResourceGroupData(location);
            ArmOperation<ResourceGroupResource> operation = await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, resourceGroupData);
            ResourceGroupResource resourceGroup = operation.Value;
            VirtualMachineCollection vms = resourceGroup.GetVirtualMachines();
            NetworkInterfaceCollection nics = resourceGroup.GetNetworkInterfaces();
            VirtualNetworkCollection vns = resourceGroup.GetVirtualNetworks();
            PublicIPAddressCollection publicIps = resourceGroup.GetPublicIPAddresses();

            //if (ModelState.IsValid)
            //{

            // Create a new VM from the formular
            string virtualNetworkName = "virtualMachineNetwork";
            string networkInterfaceName = "virtualMachineInterface";
            string vmName = VirtualM.login + "VM";
            string computeName = VirtualM.login + "PC";
            string login = VirtualM.login;
            string pwd = VirtualM.password;
            VirtualM.name = vmName;

            // Create the public ip adress
            PublicIPAddressResource ipResource = InitIp(resourceGroup);

            // Create virtual network
            VirtualNetworkResource vnetResrouce = vns.CreateOrUpdate(WaitUntil.Completed, virtualNetworkName,
            new VirtualNetworkData()
            {
                Location = AzureLocation.NorthEurope,
                Subnets =
                {
                new SubnetData()
                {
                    Name = "vmSubNet",
                    AddressPrefix = "10.0.0.0/24"
                }
                },
                AddressPrefixes =
                {
                "10.0.0.0/16"
                },
            }).Value;

            // Create network interface
            NetworkInterfaceResource nicResource = nics.CreateOrUpdate(WaitUntil.Completed, networkInterfaceName,
            new NetworkInterfaceData()
            {
                Location = AzureLocation.NorthEurope,
                IPConfigurations =
                {
                new NetworkInterfaceIPConfigurationData()
                {
                    Name = "Primary",
                    Primary = true,
                    Subnet = new SubnetData() { Id = vnetResrouce?.Data.Subnets.First().Id },
                    PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    PublicIPAddress = new PublicIPAddressData() { Id = ipResource ?.Data.Id }
                }
                }
            }
            ).Value;

            // Create VM
            VirtualMachineResource vmResource = vms.CreateOrUpdate(WaitUntil.Completed, vmName,
            new VirtualMachineData(AzureLocation.NorthEurope)
            {
                HardwareProfile = new VirtualMachineHardwareProfile()
                {
                    VmSize = VirtualMachineSizeType.StandardB2S
                },
                OSProfile = new VirtualMachineOSProfile()
                {
                    ComputerName = computeName,
                    AdminUsername = login,
                    AdminPassword = pwd,
                    WindowsConfiguration = new WindowsConfiguration
                    {
                        ProvisionVmAgent = true,
                    }
                },
                StorageProfile = new VirtualMachineStorageProfile()
                {
                    OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage),
                    ImageReference = new ImageReference()
                    {
                        Offer = "windows-10",
                        Publisher = "MicrosoftWindowsDesktop",
                        Sku = "19h2-pro-g2",
                        Version = "latest"
                    }
                },
                NetworkProfile = new VirtualMachineNetworkProfile()
                {
                    NetworkInterfaces =
                    {
                    new VirtualMachineNetworkInterfaceReference()
                    {
                        Id = nicResource.Id
                    }
                    }
                },
            }).Value;

            PublicIPAddressResource getIPResource = InitIp(resourceGroup);
            VirtualM.publicIp = getIPResource.Data.IPAddress;

            // Ajout de la VM crée a la base de données
            _context.Add(VirtualM);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
            //}
            //return View(VirtualM);
        }

        // Get IP Address
        private PublicIPAddressResource InitIp(ResourceGroupResource resourceGroup)
        {
            var publicIps = resourceGroup.GetPublicIPAddresses();
            var ipResource = publicIps.CreateOrUpdate(
                WaitUntil.Completed,
                "publicIpName",
                new PublicIPAddressData()
                {
                    PublicIPAddressVersion = NetworkIPVersion.IPv4,
                    PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    Location = AzureLocation.NorthEurope
                }).Value;

            return ipResource;
        }

        // GET: VirtualM/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.VirtualMs == null)
            {
                return NotFound();
            }

            var VirtualM = await _context.VirtualMs.FindAsync(id);
            
            if (VirtualM == null)
            {
                return NotFound();
            }
            
            return View(VirtualM);
        }

        private string GetUserName()
        {
            string user = string.Empty;
            if (HttpContext.User.Identity != null &&
                HttpContext.User.Identity.Name != null)
            {
                user = HttpContext.User.Identity.Name;
                user = user.Split("@")[0].Replace(".", "");
            }
            return user;
        }

        // POST: VirtualM/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("name,publicIp,login,password")] VirtualM VirtualM)
        {
            if (id != VirtualM.name)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(VirtualM);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VirtualMExists(VirtualM.name))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(VirtualM);
        }

        // GET: VirtualM/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.VirtualMs == null)
            {
                return NotFound();
            }

            var VirtualM = await _context.VirtualMs
                .FirstOrDefaultAsync(m => m.name == id);

            if (VirtualM == null)
            {
                return NotFound();
            }

            return View(VirtualM);
        }

        // POST: VirtualM/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (_context.VirtualMs == null)
            {
                return Problem("Entity set 'ApplicationDbContext.VirtualM'  is null.");
            }
            var VirtualM = await _context.VirtualMs.FindAsync(id);
            Console.WriteLine(id);
            Console.WriteLine(VirtualM);
            if (VirtualM != null)
            {

                ArmClient client = new ArmClient(new DefaultAzureCredential());
                SubscriptionResource sub = await client.GetDefaultSubscriptionAsync();
                ResourceGroupCollection resourceGroups = sub.GetResourceGroups();

                await foreach (ResourceGroupResource resourceGroup in resourceGroups)
                {
                    Console.WriteLine("Resource group et data.name " + resourceGroup.Data.Name);
                    string resourceGroupName = resourceGroup.Data.Name;
                    ResourceGroupResource resourceGroupToDelete = await resourceGroups.GetAsync(resourceGroupName);
                    await foreach (VirtualMachineResource vm in resourceGroupToDelete.GetVirtualMachines().GetAllAsync())
                    {
                        Console.WriteLine("VM name " + vm.Id.Name);
                        string vmName = vm.Id.Name;
                        if (vmName == VirtualM.name)
                        {
                            await resourceGroupToDelete.DeleteAsync(WaitUntil.Completed);
                        }
                    }
                }

                _context.VirtualMs.Remove(VirtualM);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: VirtualM/Shutoff
        public async Task<IActionResult> PowerOff(string id)
        {
            if (id == null || _context.VirtualMs == null)
            {
                return NotFound();
            }

            var VirtualM = await _context.VirtualMs
                .FirstOrDefaultAsync(m => m.name == id);

            if (VirtualM == null)
            {
                return NotFound();
            }

            return View(VirtualM);
        }

        // POST: VirtualM/Shutoff/5
        [HttpPost, ActionName("PowerOff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PowerOffConfirmed(string id)
        {
            if (_context.VirtualMs == null)
            {
                return Problem("Entity set 'ApplicationDbContext.VirtualM'  is null.");
            }
            var VirtualM = await _context.VirtualMs.FindAsync(id);
            Console.WriteLine("id is " + id);
            Console.WriteLine("VirtualM is " + VirtualM);
            if (VirtualM != null)
            {
                // Ne passe pas la dedans
                Console.WriteLine("salutTestVirtualM");

                ArmClient client = new ArmClient(new DefaultAzureCredential());
                SubscriptionResource sub = await client.GetDefaultSubscriptionAsync();
                ResourceGroupCollection resourceGroups = sub.GetResourceGroups();

                await foreach (ResourceGroupResource resourceGroup in resourceGroups)
                {
                    string resourceGroupName = resourceGroup.Data.Name;
                    Console.WriteLine(resourceGroupName);
                    ResourceGroupResource resourceGroupToDelete = await resourceGroups.GetAsync(resourceGroupName);
                    await foreach (VirtualMachineResource vm in resourceGroupToDelete.GetVirtualMachines().GetAllAsync())
                    {
                        string vmName = vm.Id.Name;
                        Console.WriteLine(vmName);
                        if (vmName == VirtualM.name)
                        {
                            Console.Write(vmName + " " + resourceGroupName);
                            await vm.PowerOffAsync(WaitUntil.Completed);
                        }
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: VirtualM/Connect
        public async Task<IActionResult> Connect(string id)
        {
            if (id == null || _context.VirtualMs == null)
            {
                return NotFound();
            }

            var VirtualM = await _context.VirtualMs
                .FirstOrDefaultAsync(m => m.name == id);

            if (VirtualM == null)
            {
                return NotFound();
            }

            return View(VirtualM);
        }

        // POST: VirtualM/ConnectToRDD
        [HttpPost, ActionName("Connect")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConnectToVMWithRDP(string id)
        {
            var VirtualM = await _context.VirtualMs.FindAsync(id);

            if (VirtualM != null)
            {
                var hostname = VirtualM.publicIp;
                Process.Start("mstsc", $"/v:{hostname}");
            }

            return View(VirtualM);

        }

        private bool VirtualMExists(string id)
        {
          return (_context.VirtualMs?.Any(e => e.name == id)).GetValueOrDefault();
        }
    }
}
