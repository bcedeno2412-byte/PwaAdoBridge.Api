using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ProjectServer.Client;
using PwaAdoBridge.Api.Auth;
using PwaAdoBridge.Api.Dtos;

namespace PwaAdoBridge.Api.Services
{
    /// <summary>
    /// Client service for interacting with Project Online (PWA)
    /// </summary>
    public class PwaClient
    {
        private readonly string _pwaUrl;
        private readonly string _tenantId;
        private readonly string _clientId;
        private readonly string _username;
        private readonly string _password;
        private readonly string _sharePointUrl;
        private readonly ILogger<PwaClient> _logger;

        public PwaClient(IConfiguration config, ILogger<PwaClient> logger)
        {
            _pwaUrl        = config["Pwa:Url"]           ?? throw new ArgumentNullException("Pwa:Url");
            _tenantId      = config["Pwa:TenantId"]      ?? throw new ArgumentNullException("Pwa:TenantId");
            _clientId      = config["Pwa:ClientId"]      ?? throw new ArgumentNullException("Pwa:ClientId");
            _username      = config["Pwa:Username"]      ?? throw new ArgumentNullException("Pwa:Username");
            _password      = config["Pwa:Password"]      ?? throw new ArgumentNullException("Pwa:Password");
            _sharePointUrl = config["Pwa:SharepointUrl"] ?? throw new ArgumentNullException("Pwa:SharepointUrl");

            _logger = logger;
        }

        /// <summary>
        /// Converts plain string password to SecureString
        /// </summary>
        private SecureString ToSecureString(string password)
        {
            var secure = new SecureString();
            foreach (char c in password)
                secure.AppendChar(c);
            secure.MakeReadOnly();
            return secure;
        }

        /// <summary>
        /// Creates a ProjectContext for connecting to Project Online
        /// </summary>
        private ProjectContext CreateProjectContext()
        {
            var securePw = ToSecureString(_password);
            var context = ProjectServices.GetConnectionProjectContext(
                _pwaUrl,
                _username,
                securePw,
                _clientId,
                _tenantId,
                _sharePointUrl
            );

            if (context == null)
            {
                _logger.LogError("Failed to create ProjectContext from ProjectServices.GetConnectionProjectContext");
                throw new InvalidOperationException("Could not create ProjectContext for Project Online.");
            }

            return context;
        }

        /// <summary>
        /// Retrieves all published projects from Project Online
        /// </summary>
        public async Task<IReadOnlyList<PublishedProject>> GetProjectsAsync(
            CancellationToken cancellationToken = default)
        {
            using var projContext = CreateProjectContext();

            projContext.Load(projContext.Projects);
            await projContext.ExecuteQueryAsync();

            _logger.LogInformation("Loaded {Count} projects from Project Online", projContext.Projects.Count);

            return projContext.Projects.ToList();
        }

        /// <summary>
        /// Retrieves a specific project and its tasks as a DTO
        /// </summary>
        public async Task<PwaProjectDto?> GetProjectWithTasksAsDtoAsync(
            Guid projectUid,
            CancellationToken cancellationToken = default)
        {
            using var projContext = CreateProjectContext();

            var proj = projContext.Projects.GetByGuid(projectUid);

            projContext.Load(
                proj,
                p => p.Id,
                p => p.Name,
                p => p.StartDate,
                p => p.FinishDate,
                p => p.Tasks
            );
            projContext.Load(proj.Tasks);
            await projContext.ExecuteQueryAsync();

            if (proj.ServerObjectIsNull == true)
                return null;

            // Map Project and Tasks to DTO
            return new PwaProjectDto
            {
                ProjectUid  = proj.Id.ToString(),
                ProjectName = proj.Name,
                StartDate   = proj.StartDate,
                FinishDate  = proj.FinishDate,
                Tasks       = proj.Tasks
                                .Select(t => new PwaTaskDto
                                {
                                    TaskUid    = t.Id.ToString(),
                                    TaskName   = t.Name,
                                    StartDate  = t.Start,
                                    FinishDate = t.Finish
                                })
                                .ToList()
            };
        }
    }

    /// <summary>
    /// PWA authentication options for DI configuration
    /// </summary>
    public class PwaAuthOptions
    {
        public string TenantId    { get; set; } = default!;
        public string ClientId    { get; set; } = default!;
        public string PwaResource { get; set; } = default!;
        public string Username    { get; set; } = default!;
        public string Password    { get; set; } = default!;
    }
}
