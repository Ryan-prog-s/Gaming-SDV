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
using Gaming.Tools;

namespace WebGamingSDV.Models
{
    [TypeFilter(typeof(AuthorizationFilter))]
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
            return _context.VirtualMs != null
                ? View(await _context.VirtualMs.Where(vm => vm.name == GetUserName()).ToListAsync())
                : Problem("Entity set 'ApplicationDbContext.VirtualMs'  is null.");
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
        public async Task<IActionResult> Create([Bind("login,password")] VirtualM VirtualM)
        {
            ModelState.Remove(nameof(VirtualM.name));
            ModelState.Remove(nameof(VirtualM.publicIp));

            if (ModelState.IsValid)
            {
                AzureTools azureTools = new(GetUserName());

                ResourceGroupResource resourceGroup = await azureTools.GetResourceGroupAsync();

                azureTools.CreateVirtualMachine(resourceGroup, VirtualM.login, VirtualM.password);

                VirtualM.name = GetUserName();
                VirtualM.publicIp = await azureTools.GetIpAdress("ip-"+VirtualM.login);

                _context.Add(VirtualM);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(VirtualM);
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
            if (VirtualM != null)
            {

                _context.VirtualMs.Remove(VirtualM);

                AzureTools azureTools = new(GetUserName());
                ResourceGroupResource resourceGroup = await azureTools.GetResourceGroupAsync();

                VirtualMachineResource vm = await resourceGroup.GetVirtualMachines().GetAsync("vm-" + VirtualM.login);
                NetworkInterfaceResource nic = await resourceGroup.GetNetworkInterfaces().GetAsync("nic-" + VirtualM.login);
                VirtualNetworkResource vnet = await resourceGroup.GetVirtualNetworks().GetAsync("vnet-" + VirtualM.login);
                PublicIPAddressResource publicIp = await resourceGroup.GetPublicIPAddresses().GetAsync("ip-" + VirtualM.login);

                await vm.DeleteAsync(WaitUntil.Completed, forceDeletion: true);
                await nic.DeleteAsync(WaitUntil.Completed);
                await vnet.DeleteAsync(WaitUntil.Completed);
                await publicIp.DeleteAsync(WaitUntil.Completed);

                // TO DO : delete disk

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
            if (VirtualM != null)
            {
                AzureTools azureTools = new(GetUserName());
                azureTools.VmPowerOffsync();
            }

            return RedirectToAction(nameof(Index));
        }


        // GET: VirtualM/Shuton
        public async Task<IActionResult> PowerOn(string id)
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

        // POST: VirtualM/Shuton/5
        [HttpPost, ActionName("PowerOn")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PowerOnConfirmed(string id)
        {
            if (_context.VirtualMs == null)
            {
                return Problem("Entity set 'ApplicationDbContext.VirtualM'  is null.");
            }
            var VirtualM = await _context.VirtualMs.FindAsync(id);
            if (VirtualM != null)
            {
                AzureTools azureTools = new(GetUserName());
                azureTools.VmPowerOnAsync();
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

        // POST: VirtualM/ConnectToRDP
        [HttpPost, ActionName("Connect")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConnectToVMWithRDP(string id)
        {
            var VirtualM = await _context.VirtualMs.FindAsync(id);

            if (VirtualM != null)
            {
                var hostname = VirtualM.publicIp;
                Process.Start("mstsc", $"/v:{hostname}");
                //Process.Start("mstsc", $"/v:{hostname} /d:cmd /c start shell:AppsFolder\\Microsoft.MicrosoftSolitaireCollection_8wekyb3d8bbwe!App");
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

        private bool VirtualMExists(string id)
        {
          return (_context.VirtualMs?.Any(e => e.name == id)).GetValueOrDefault();
        }
    }
}
