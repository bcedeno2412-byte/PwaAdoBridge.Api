using System;
using System.Security;
using Microsoft.Identity.Client;
using Microsoft.ProjectServer.Client;

namespace PwaAdoBridge.Api.Services
{
    // Static service to handle Project Online connections
    public static class ProjectServices
    {
        /// <summary>
        /// Gets a ProjectContext object for connecting to Project Online using username/password.
        /// </summary>
        /// <param name="url">Project Online site URL</param>
        /// <param name="username">User's username</param>
        /// <param name="secureString">User's password as SecureString</param>
        /// <param name="appId">Azure AD App ID (Base64 encoded)</param>
        /// <param name="appTenantId">Azure AD Tenant ID (Base64 encoded)</param>
        /// <param name="sharepointUrl">SharePoint site URL for token scope</param>
        /// <returns>ProjectContext object if successful; otherwise null</returns>
        public static ProjectContext GetConnectionProjectContext(
            string url,
            string username,
            SecureString secureString,
            string appId,
            string appTenantId,
            string sharepointUrl)
        {
            Console.WriteLine("Get project context");

            // Validate URL format
            if (!string.IsNullOrWhiteSpace(url) &&
                (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    // Build scope for authentication
                    string scope = sharepointUrl.TrimEnd('/') + "/Project.Read";

                    // Configure PublicClientApplication for Azure AD
                    var pcaConfig = PublicClientApplicationBuilder
                        .Create(SettingsService.Base64Decode(appId))
                        .WithTenantId(SettingsService.Base64Decode(appTenantId));

                    var pca = pcaConfig.Build();

                    // Acquire token with username/password
                    var tokenResult = pca
                        .AcquireTokenByUsernamePassword(new[] { scope }, username, secureString)
                        .ExecuteAsync()
                        .Result;

                    // Create ProjectContext and attach authentication header
                    var projectContext = new ProjectContext(url);
                    projectContext.ExecutingWebRequest += (s, e) =>
                    {
                        e.WebRequestExecutor.RequestHeaders["Authorization"] =
                            "Bearer " + tokenResult.AccessToken;
                    };

                    return projectContext;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to get ProjectContext: {ex}");
                }
            }

            // Return null if URL invalid or connection fails
            return null;
        }
    }
}
