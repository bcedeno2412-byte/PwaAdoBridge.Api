using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PwaAdoBridge.Api.Dtos;
using PwaAdoBridge.Api.Services;

namespace PwaAdoBridge.Api.Controllers
{
    [ApiController]
    [Route("api/sync")]
    public class SyncController : ControllerBase
    {
        private readonly PwaToAdoSyncService _syncService;
        private readonly PwaClient _pwaClient;
        private readonly ILogger<SyncController> _logger;

        public SyncController(
            PwaToAdoSyncService syncService,
            PwaClient pwaClient,
            ILogger<SyncController> logger)
        {
            _syncService = syncService;
            _pwaClient = pwaClient;
            _logger = logger;
        }

        /// <summary>
        /// Synchronizes a project to Azure DevOps.
        /// Supports two modes: PwaProjectToDevOps or DevOpsOnly.
        /// </summary>
        [HttpPost("project")]
        public async Task<ActionResult<SyncResult>> SyncProject([FromBody] PwaProjectDto project)
        {
            // Validate payload
            if (project == null)
                return Ok(CreateErrorResult("Invalid project payload.", "InvalidPayload"));

            // Default mode for backward compatibility
            project.Mode = string.IsNullOrWhiteSpace(project.Mode)
                ? "PwaProjectToDevOps"
                : project.Mode.Trim();

            // Validate project name and mode
            var validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(project.ProjectName))
                validationErrors.Add("ProjectName is required.");

            if (project.Mode != "PwaProjectToDevOps" && project.Mode != "DevOpsOnly")
                validationErrors.Add("Mode must be 'PwaProjectToDevOps' or 'DevOpsOnly' if provided.");

            if (validationErrors.Count > 0)
                return Ok(CreateValidationFailedResult(validationErrors));

            // If mode is PWA → DevOps, fetch Project Online data
            if (project.Mode == "PwaProjectToDevOps")
            {
                var fetchResult = await FetchProjectOnlineData(project);
                if (fetchResult != null)
                    return Ok(fetchResult); // Return error if fetch failed
            }
            else if (project.Mode == "DevOpsOnly")
            {
                if (string.IsNullOrWhiteSpace(project.ProjectUid))
                    project.ProjectUid = Guid.NewGuid().ToString();
            }

            // Execute sync
            return await ExecuteSync(project);
        }

        // ---------------------------------------------
        // Helper Methods
        // ---------------------------------------------

        /// <summary>
        /// Fetches project information from Project Online for PwaProjectToDevOps mode.
        /// Returns a SyncResult if there is an error, otherwise null.
        /// </summary>
        private async Task<SyncResult?> FetchProjectOnlineData(PwaProjectDto project)
        {
            try
            {
                var pwaProjects = await _pwaClient.GetProjectsAsync(default);
                var matched = pwaProjects.FirstOrDefault(p =>
                    string.Equals(p.Name, project.ProjectName, StringComparison.OrdinalIgnoreCase));

                if (matched == null)
                    return CreateErrorResult($"No Project Online project was found with the name '{project.ProjectName}'.", "ProjectNotFound");

                // Fill missing info if not provided
                project.ProjectUid ??= matched.Id.ToString();
                project.StartDate ??= matched.StartDate;
                project.FinishDate ??= matched.FinishDate;

                return null;
            }
            catch (AuthenticationException ex)
            {
                _logger.LogError(ex, "Authentication error while querying Project Online for project {ProjectName}", project.ProjectName);
                return CreateErrorResult("An authentication error occurred while contacting Project Online.", "AuthFailed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while querying Project Online for project {ProjectName}", project.ProjectName);
                return CreateErrorResult("Unexpected error while validating the Project Online project name.", "UnexpectedError");
            }
        }

        /// <summary>
        /// Executes the synchronization via the sync service with error handling.
        /// </summary>
        private async Task<SyncResult> ExecuteSync(PwaProjectDto project)
        {
            try
            {
                _logger.LogInformation(
                    "Starting DevOps sync for project {ProjectName} ({ProjectUid}) in mode {Mode}",
                    project.ProjectName,
                    project.ProjectUid,
                    project.Mode);

                var result = await _syncService.SyncProjectsAndTasksAsync(new[] { project });

                // Ensure success flag
                if (result.Errors == 0 && !result.Success)
                    result.Success = true;

                if (string.IsNullOrWhiteSpace(result.Message))
                    result.Message = result.Errors == 0 ? "Sync completed." : "Sync completed with some errors.";

                _logger.LogInformation("DevOps sync completed for {ProjectUid}. {@Result}", project.ProjectUid, result);
                return result;
            }
            catch (AuthenticationException ex)
            {
                _logger.LogError(ex, "Authentication error while syncing project {ProjectUid} to Azure DevOps", project.ProjectUid);
                return CreateErrorResult("An authentication error occurred while contacting Azure DevOps.", "AuthFailed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error syncing project {ProjectUid} to Azure DevOps", project.ProjectUid);
                return CreateErrorResult("Unexpected error syncing project to Azure DevOps.", "UnexpectedError");
            }
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
                Errors = validationErrors.Count,
                ValidationErrors = validationErrors,
                Message = string.Join(" ", validationErrors)
            };
        }

        /// <summary>
        /// Creates a generic error result object.
        /// </summary>
        private SyncResult CreateErrorResult(string message, string errorCode)
        {
            return new SyncResult
            {
                Success = false,
                ErrorCode = errorCode,
                ProjectsProcessed = 0,
                WorkItemsCreated = 0,
                WorkItemsUpdated = 0,
                Errors = 1,
                Message = message
            };
        }
    }
}
