using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PwaAdoBridge.Api.Dtos;
using PwaAdoBridge.Api.Options;

namespace PwaAdoBridge.Api.Services
{
    /// <summary>
    /// Client to interact with Azure DevOps Work Items (Epics and Tasks) for PWA projects.
    /// </summary>
    public class AzureDevOpsClient
    {
        private readonly HttpClient _httpClient;
        private readonly AzureDevOpsOptions _options;
        private readonly ILogger<AzureDevOpsClient> _logger;

        // Options for JSON serialization consistent with Azure DevOps API
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public AzureDevOpsClient(
            HttpClient httpClient,
            IOptions<AzureDevOpsOptions> options,
            ILogger<AzureDevOpsClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;

            ValidateConfiguration();
            ConfigureHttpClient();
        }

        /// <summary>
        /// Ensure required configuration values are provided
        /// </summary>
        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_options.OrganizationUrl))
                throw new InvalidOperationException("AzureDevOps:OrganizationUrl is not configured.");

            if (string.IsNullOrWhiteSpace(_options.PersonalAccessToken))
                throw new InvalidOperationException("AzureDevOps:PersonalAccessToken is not configured.");
        }

        /// <summary>
        /// Configure the HttpClient with base address and authentication header
        /// </summary>
        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_options.OrganizationUrl.TrimEnd('/') + "/");
            var patBytes = Encoding.ASCII.GetBytes($":{_options.PersonalAccessToken}");
            var patBase64 = Convert.ToBase64String(patBytes);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", patBase64);
        }

        /// <summary>
        /// Create an Epic in DevOps for a given PWA project
        /// </summary>
        public async Task<int> CreateEpicForPwaProjectAsync(PwaProjectDto project)
        {
            var uri = $"{_options.ProjectName}/_apis/wit/workitems/$Epic?api-version=7.1";

            var patch = new[]
            {
                new { op = "add", path = "/fields/System.Title", value = project.ProjectName },
                new
                {
                    op = "add",
                    path = "/fields/System.Description",
                    value = $"Imported from Project Online. ProjectUid: {project.ProjectUid}"
                }
            };

            return await SendPatchRequestAsync(uri, patch, "Epic", project.ProjectName);
        }

        /// <summary>
        /// Search for an existing Epic by project name in DevOps
        /// </summary>
        public async Task<int?> FindEpicByProjectNameAsync(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
                return null;

            var wiqlUri = $"{_options.ProjectName}/_apis/wit/wiql?api-version=7.1";
            var safeTitle = projectName.Replace("'", "''");

            var wiqlPayload = new
            {
                query = $@"
                    SELECT [System.Id]
                    FROM WorkItems
                    WHERE
                        [System.TeamProject] = '{_options.ProjectName}'
                        AND [System.WorkItemType] = 'Epic'
                        AND [System.Title] = '{safeTitle}'
                "
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, wiqlUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(wiqlPayload, JsonOptions), Encoding.UTF8, "application/json")
            };

            _logger.LogInformation("Searching existing Epic in DevOps by name {ProjectName}", projectName);
            using var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to query Epics by name. Status {StatusCode}. Body: {Body}",
                    response.StatusCode,
                    body);
                return null;
            }

            try
            {
                var wiqlResult = JsonSerializer.Deserialize<WiqlResult>(body, JsonOptions);
                if (wiqlResult?.WorkItems == null || wiqlResult.WorkItems.Length == 0)
                    return null;

                var id = wiqlResult.WorkItems[0].Id;
                _logger.LogInformation("Found existing Epic {EpicId} for project name {ProjectName}", id, projectName);
                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing WIQL response when searching Epic by name {ProjectName}", projectName);
                return null;
            }
        }

        /// <summary>
        /// Return existing Epic ID or create a new one if it doesn't exist
        /// </summary>
        public async Task<int> GetOrCreateEpicForPwaProjectAsync(PwaProjectDto project)
        {
            var existingId = await FindEpicByProjectNameAsync(project.ProjectName);
            return existingId ?? await CreateEpicForPwaProjectAsync(project);
        }

        /// <summary>
        /// Create a Task in DevOps for a PWA task under a parent Epic
        /// </summary>
        public async Task<int> CreateTaskForPwaTaskAsync(PwaTaskDto task, int parentEpicId)
        {
            var uri = $"{_options.ProjectName}/_apis/wit/workitems/$Task?api-version=7.1";
            var patch = BuildTaskPatch(task, parentEpicId);
            return await SendPatchRequestAsync(uri, patch, "Task", task.TaskName);
        }

        private List<object> BuildTaskPatch(PwaTaskDto task, int parentEpicId)
        {
            var parentUrl = $"{_options.OrganizationUrl.TrimEnd('/')}/_apis/wit/workitems/{parentEpicId}";

            var patch = new List<object>
            {
                new { op = "add", path = "/fields/System.Title", value = task.TaskName },
                new
                {
                    op = "add",
                    path = "/fields/System.Description",
                    value = $"Imported from Project Online. TaskUid: {task.TaskUid}"
                },
                new
                {
                    op = "add",
                    path = "/relations/-",
                    value = new
                    {
                        rel = "System.LinkTypes.Hierarchy-Reverse",
                        url = parentUrl,
                        attributes = new { comment = "Imported from Project Online" }
                    }
                }
            };

            if (task.StartDate.HasValue)
                patch.Add(new { op = "add", path = "/fields/Microsoft.VSTS.Scheduling.StartDate", value = task.StartDate.Value.ToString("o") });

            if (task.FinishDate.HasValue)
                patch.Add(new { op = "add", path = "/fields/Microsoft.VSTS.Scheduling.DueDate", value = task.FinishDate.Value.ToString("o") });

            return patch;
        }

        private async Task<int> SendPatchRequestAsync(string uri, object patchObject, string workItemType, string title)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(JsonSerializer.Serialize(patchObject, JsonOptions), Encoding.UTF8, "application/json-patch+json")
            };

            _logger.LogInformation("Creating {WorkItemType} in DevOps: {Title}", workItemType, title);

            using var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create {WorkItemType}. Status {StatusCode}. Body: {Body}", workItemType, response.StatusCode, body);
                throw new InvalidOperationException($"Failed to create {workItemType}: {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("id").GetInt32();
        }

        // Internal classes for deserializing WIQL results
        private sealed class WiqlResult
        {
            public WorkItemRef[] WorkItems { get; set; } = Array.Empty<WorkItemRef>();
        }

        private sealed class WorkItemRef
        {
            public int Id { get; set; }
            public string? Url { get; set; }
        }
    }
}
