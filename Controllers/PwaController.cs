using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PwaAdoBridge.Api.Dtos;
using PwaAdoBridge.Api.Services;

namespace PwaAdoBridge.Api.Controllers
{
    [ApiController]
    [Route("api/pwa")]
    public class PwaController : ControllerBase
    {
        private readonly PwaClient _pwaClient;
        private readonly PwaToAdoSyncService _syncService;
        private readonly ILogger<PwaController> _logger;

        public PwaController(
            PwaClient pwaClient,
            PwaToAdoSyncService syncService,
            ILogger<PwaController> logger)
        {
            _pwaClient = pwaClient;
            _syncService = syncService;
            _logger = logger;
        }

      

        /// <summary>
        /// Returns all projects from Project Online.
        /// </summary>
        [HttpGet("projects")]
        public async Task<ActionResult<IEnumerable<object>>> GetProjects(CancellationToken cancellationToken)
        {
            var projects = await _pwaClient.GetProjectsAsync(cancellationToken);

            var result = projects.Select(p => new
            {
                Id = p.Id,
                Name = p.Name,
                Start = p.StartDate,
                Finish = p.FinishDate
            });

            return Ok(result);
        }

        /// <summary>
        /// Returns a specific project along with its tasks by GUID.
        /// </summary>
        [HttpGet("projects/{projectUid:guid}")]
        public async Task<ActionResult<PwaProjectDto>> GetProjectWithTasks(
            Guid projectUid,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetProjectWithTasks called for {ProjectUid}", projectUid);

            var dto = await _pwaClient.GetProjectWithTasksAsDtoAsync(projectUid, cancellationToken);

            if (dto == null)
            {
                _logger.LogWarning("Project {ProjectUid} not found in Project Online", projectUid);
                return NotFound($"Project {projectUid} not found in Project Online.");
            }

            return Ok(dto);
        }

 

        /// <summary>
        /// Synchronizes a project to Azure DevOps using its GUID.
        /// </summary>
        [HttpPost("projects/{projectUid:guid}/sync")]
        public async Task<ActionResult<SyncResult>> SyncProjectToDevOps(
            Guid projectUid,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("SyncProjectToDevOps called for {ProjectUid}", projectUid);

            var dto = await _pwaClient.GetProjectWithTasksAsDtoAsync(projectUid, cancellationToken);

            if (dto == null)
                return Ok(CreateErrorResult($"Project {projectUid} not found in Project Online."));

            var validationErrors = ValidateProjectAndTasks(dto);
            if (validationErrors.Count > 0)
                return Ok(CreateValidationFailedResult(validationErrors));

            return await ExecuteSync(dto, projectUid.ToString());
        }

        /// <summary>
        /// DTO for receiving project name in the sync-by-name endpoint.
        /// </summary>
        public class SyncByNameRequest
        {
            public string ProjectName { get; set; } = string.Empty;
        }

        /// <summary>
        /// Synchronizes a project to Azure DevOps using its name.
        /// </summary>
        [HttpPost("projects/sync-by-name")]
        public async Task<ActionResult<SyncResult>> SyncProjectByName(
            [FromBody] SyncByNameRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProjectName))
                return BadRequest("ProjectName is required.");

            _logger.LogInformation("SyncProjectByName called for project name {ProjectName}", request.ProjectName);

            var projects = await _pwaClient.GetProjectsAsync(cancellationToken);
            var matches = projects
                .Where(p => string.Equals(p.Name, request.ProjectName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
                return Ok(CreateErrorResult($"No Project Online project was found with the name '{request.ProjectName}'."));

            if (matches.Count > 1)
                return Ok(CreateErrorResult("Multiple Project Online projects share that name. Please specify a unique one."));

            var dto = await _pwaClient.GetProjectWithTasksAsDtoAsync(matches[0].Id, cancellationToken);
            if (dto == null)
                return Ok(CreateErrorResult("The project was found but its tasks could not be loaded."));

            var validationErrors = ValidateProjectAndTasks(dto);
            if (validationErrors.Count > 0)
                return Ok(CreateValidationFailedResult(validationErrors));

            return await ExecuteSync(dto, matches[0].Name);
        }


        /// <summary>
        /// Validates project and task dates and names.
        /// </summary>
        private List<string> ValidateProjectAndTasks(PwaProjectDto dto)
        {
            var errors = new List<string>();

            if (dto.StartDate.HasValue && dto.FinishDate.HasValue && dto.StartDate > dto.FinishDate)
                errors.Add("Project start date must be earlier than or equal to the project finish date.");

            if (dto.Tasks != null)
            {
                foreach (var task in dto.Tasks)
                {
                    if (string.IsNullOrWhiteSpace(task.TaskName))
                        errors.Add("TaskName is required for all tasks.");

                    if (task.StartDate.HasValue && task.FinishDate.HasValue && task.StartDate > task.FinishDate)
                        errors.Add($"Start date must be earlier than or equal to the finish date for task '{task.TaskName}'.");
                }
            }

            return errors;
        }

        /// <summary>
        /// Creates a validation failed result object.
        /// </summary>
        private SyncResult CreateValidationFailedResult(List<string> validationErrors)
        {
            return new SyncResult
            {
                Success = false,
                ErrorCode = "ValidationFailed",
                ProjectsProcessed = 0,
                WorkItemsCreated = 0,
                WorkItemsUpdated = 0,
                Errors = 1,
                ValidationErrors = validationErrors,
                Message = string.Join(" ", validationErrors)
            };
        }

        /// <summary>
        /// Creates a generic error result object.
        /// </summary>
        private SyncResult CreateErrorResult(string message)
        {
            return new SyncResult
            {
                Success = false,
                ProjectsProcessed = 0,
                WorkItemsCreated = 0,
                WorkItemsUpdated = 0,
                Errors = 1,
                Message = message
            };
        }

        /// <summary>
        /// Executes the synchronization and handles logging and errors.
        /// </summary>
        private async Task<SyncResult> ExecuteSync(PwaProjectDto dto, string projectIdentifier)
        {
            var result = new SyncResult();

            try
            {
                await _syncService.SyncProjectsAndTasksAsync(new[] { dto });

                result.Success = true;
                result.ProjectsProcessed = 1;
                result.WorkItemsCreated = (dto.Tasks?.Count ?? 0) + 1; // epic + tasks
                result.WorkItemsUpdated = 0;
                result.Errors = 0;
                result.Message = "Sync completed.";

                _logger.LogInformation("Successfully synced project {Project} to DevOps. {@Result}", projectIdentifier, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing project {Project} from PWA to DevOps", projectIdentifier);

                result.Success = false;
                result.ProjectsProcessed = 1;
                result.WorkItemsCreated = 0;
                result.WorkItemsUpdated = 0;
                result.Errors = 1;
                result.Message = "Unexpected error syncing project to Azure DevOps.";
            }

            return result;
        }
    }
}
