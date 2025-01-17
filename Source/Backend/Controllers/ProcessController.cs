using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using VitalService.Data;
using VitalService.Dtos;
using VitalService.Stores;

namespace VitalService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public partial class ProcessController : Controller
    {
        private ManagedProcessStore AffinityStore { get; }
        public MachineDataStore MachineDataStore { get; }
        public ProfileStore ProfileStore { get; }
        private ILogger<ProcessController> Logger { get; }

        public ProcessController(ManagedProcessStore affinityStore, MachineDataStore machineDataStore, ProfileStore profileStore, ILogger<ProcessController> logger)
        {
            AffinityStore = affinityStore;
            MachineDataStore = machineDataStore;
            ProfileStore = profileStore;
            Logger = logger;
        }
        [HttpGet]
        public async Task<ActionResult<GetAllResponse>> GetAsync()
        {
            var managedApps = await AffinityStore.GetAsync();
            return Ok(new GetManagedResponse
            {
                AffinityModels = managedApps.Select(e => e.ToDto())?.ToArray() ?? Array.Empty<ManagedModelDto>()
            });
        }
        // GET: api/<Affinity>
        [Route("Managed")]
        [HttpGet]
        public async Task<ActionResult<GetManagedResponse>> GetManagedAsync()
        {
            Logger.LogInformation("GetManaged");

            var managedApps = await AffinityStore.GetAsync();
            return Ok(new GetManagedResponse
            {
                AffinityModels = managedApps.Select(e => e.ToDto())?.ToArray() ?? Array.Empty<ManagedModelDto>()
            });
        }
        [Route("RunningProcesses")]
        [HttpGet()]
        public ActionResult<GetRunningProcessesResponse> GetRunningProcesses()
        {
            var processes = MachineDataStore.GetRunningProcesses();
            var response = new GetRunningProcessesResponse { ProcessView = processes };

            return Ok(response);
        }

        [Route("ProcessesToAdd")]
        [HttpGet()]
        public ActionResult<GetProcessesToAddResponse> GetProcesses()
        {
            Process[] processes;

            try
            {
                // for some reason this could fail. so we'll reattempt it if it does.
                processes = Process.GetProcesses();
            }
            catch (Exception)
            {
                Thread.Sleep(500);
                processes = Process.GetProcesses();
            }

            var processToReturn = new List<ProcessToAddDto>(processes.Length);
            foreach (var process in processes)
            {
                try
                {
                    _ = process.ProcessorAffinity; // used to check if we can modify Affinity. if we cant an exception will be thrown.
                    var p = new ProcessToAddDto
                    {
                        ProcessName = process.ProcessName
                    };

                    TryGetFileName(process, out var fileName);
                    p.ExecutionPath = fileName;

                    p.Pid = process.Id;
                    p.CanModify = true;

                    processToReturn.Add(p);

                }
                catch (Win32Exception exception) when (exception.Message == "Access is denied.")
                {
                    TryGetFileName(process, out var fileName);

                    try
                    {
                        processToReturn.Add(new ProcessToAddDto
                        {
                            ProcessName = process.ProcessName,
                            ExecutionPath = fileName,
                            Pid = process.Id,
                            CanModify = false
                        });
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                }
                catch (InvalidOperationException exception) when (exception.Message.StartsWith("Cannot process request because the process") && exception.Message.EndsWith("has exited."))
                {
                    // ignore
                }

            }

            return Ok(new GetProcessesToAddResponse
            {
                Processes = processToReturn.OrderBy(x => x.ProcessName),
            });
        }

        private static bool TryGetFileName(Process process, out string? fileName)
        {
            try
            {
                fileName = process.MainModule?.FileName;
                return true;
            }
            catch { }
            fileName = null;
            return false;
        }



        [HttpPost("kill/{id}")]
        public ActionResult KillProcessTree(int id)
        {
            var process = Process.GetProcessById(id);
            process.Kill(true);
            return Ok();
        }

        [HttpPost("openpath/{id}")]
        public ActionResult OpenProcessLocation(int id)
        {
            var process = Process.GetProcessById(id);

            if (process is null)
            {
                return NotFound();
            }
            else
            {
                var filePath = process.MainModule?.FileName;
                var directory = Path.GetDirectoryName(filePath);
                if (Directory.Exists(directory))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
#pragma warning disable SCS0001 // Potential Command Injection vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        Arguments = directory,
#pragma warning restore SCS0001 // Potential Command Injection vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        FileName = "explorer.exe"
                    };
                    Process.Start(startInfo);
                };
            }
            return Ok();
        }

        [HttpPost("openproperties/{id}")]
        public ActionResult OpenProcessProperties(int id)
        {
            var process = Process.GetProcessById(id);

            if (process is null)
            {
                return NotFound();
            }
            else
            {
                var filePath = process.MainModule?.FileName;
                if (System.IO.File.Exists(filePath))
                    Utilities.FileProperties.Open(filePath);
            }
            return Ok();
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                //else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                //{
                //    Process.Start("xdg-open", url);
                //}
                //else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                //{
                //    Process.Start("open", url);
                //}
                else
                {
                    throw;
                }
            }
        }
    }
}