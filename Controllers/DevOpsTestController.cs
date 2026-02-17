using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PwaAdoBridge.Api.Dtos;
using PwaAdoBridge.Api.Services;

namespace PwaAdoBridge.Api.Controllers
{
    [ApiController]
    [Route("api/devops-test")]
    public class DevOpsTestController : ControllerBase
    {
        private readonly PwaToAdoSyncService _syncService;

        public DevOpsTestController(PwaToAdoSyncService syncService)
        {
            _syncService = syncService;
        }

        [HttpPost("demo")]
        public async Task<IActionResult> DemoSync()
        {

            var demoProjects = new List<PwaProjectDto>
            {
                new()
                {
                    ProjectUid  = Guid.NewGuid().ToString(),
                    ProjectName = "Demo PWA Project",
                    StartDate   = DateTime.UtcNow,
                    FinishDate  = DateTime.UtcNow.AddDays(10),

                    Tasks = new List<PwaTaskDto>
                    {
                        new()
                        {
                            TaskUid    = Guid.NewGuid().ToString(),
                            TaskName   = "Initial planning",
                            StartDate  = DateTime.UtcNow,
                            FinishDate = DateTime.UtcNow.AddDays(3)
                        },
                        new()
                        {
                            TaskUid    = Guid.NewGuid().ToString(),
                            TaskName   = "Development work",
                            StartDate  = DateTime.UtcNow.AddDays(3),
                            FinishDate = DateTime.UtcNow.AddDays(10)
                        }
                    }
                }
            };

            await _syncService.SyncProjectsAndTasksAsync(demoProjects);

            return Ok(new { message = "Demo projects and tasks created in Azure DevOps." });
        }
    }
}