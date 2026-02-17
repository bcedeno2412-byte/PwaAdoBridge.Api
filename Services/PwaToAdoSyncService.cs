using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PwaAdoBridge.Api.Dtos;

namespace PwaAdoBridge.Api.Services
{
    /// <summary>
    /// Service responsible for syncing PWA projects and tasks to Azure DevOps
    /// </summary>
    public class PwaToAdoSyncService
    {
        private readonly AzureDevOpsClient _devOpsClient;
        private readonly ILogger<PwaToAdoSyncService> _logger;

        public PwaToAdoSyncService(
            AzureDevOpsClient devOpsClient,
            ILogger<PwaToAdoSyncService> logger)
        {
            _devOpsClient = devOpsClient;
            _logger = logger;
        }

        /// <summary>
        /// Sync multiple PWA projects to Azure DevOps
        /// </summary>
        public async Task<SyncResult> SyncProjectsAndTasksAsync(IEnumerable<PwaProjectDto> projects)
        {
            if (projects == null)
                throw new ArgumentNullException(nameof(projects));

            var result = new SyncResult();

            foreach (var project in projects)
            {
                await SyncSingleProjectAsync(project, result);
            }

            return result;
        }

        /// <summary>
        /// Sync a single PWA project
        /// </summary>
        private async Task SyncSingleProjectAsync(PwaProjectDto project, SyncResult result)
        {
            result.ProjectsProcessed++;

            try
            {
                _logger.LogInformation(
                    "Syncing PWA project {ProjectName} ({ProjectUid})",
                    project.ProjectName,
                    project.ProjectUid);

                // Create or get Epic in DevOps
                var epicId = await _devOpsClient.GetOrCreateEpicForPwaProjectAsync(project);

                if (epicId <= 0)
                {
                    result.Errors++;
                    _logger.LogError(
                        "AzureDevOpsClient returned invalid epic id {EpicId} for project {ProjectName}",
                        epicId,
                        project.ProjectName);
                    return;
                }

                result.WorkItemsCreated++; // Epic created

                if (project.Tasks == null || project.Tasks.Count == 0)
                {
                    _logger.LogInformation(
                        "PWA project {ProjectName} has no tasks to sync.",
                        project.ProjectName);
                    return;
                }

                // Sync project tasks
                await SyncTasksAsync(project, epicId, result);
            }
            catch (Exception ex)
            {
                result.Errors++;
                _logger.LogError(
                    ex,
                    "Error syncing PWA project {ProjectName} ({ProjectUid})",
                    project.ProjectName,
                    project.ProjectUid);
            }
        }

        /// <summary>
        /// Sync tasks of a PWA project to Azure DevOps under the specified Epic
        /// </summary>
        private async Task SyncTasksAsync(PwaProjectDto project, int epicId, SyncResult result)
        {
            if (project.Tasks == null)
                return;

            foreach (var task in project.Tasks)
            {
                try
                {
                    await _devOpsClient.CreateTaskForPwaTaskAsync(task, epicId);
                    result.WorkItemsCreated++;
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    _logger.LogError(
                        ex,
                        "Error creating DevOps Task for PWA task {TaskName} ({TaskUid}) in project {ProjectName}",
                        task.TaskName,
                        task.TaskUid,
                        project.ProjectName);
                }
            }
        }
    }
}
