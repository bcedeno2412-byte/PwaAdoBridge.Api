using System;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.ProjectServer.Client;
using Microsoft.SharePoint.Client;

namespace PwaAdoBridge.Api.Auth
{
    public class AuthenticationManager : IDisposable
    {
        private readonly string _tenantId;
        private readonly string _clientId;

        public AuthenticationManager(string tenantId, string clientId)
        {
            _tenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        }

        /// <summary>
        /// Get ProjectContext for Project Online using username/password authentication
        /// </summary>
        public ProjectContext GetContext(Uri web, string userPrincipalName, SecureString userPassword)
        {
            if (web == null) throw new ArgumentNullException(nameof(web));
            if (string.IsNullOrWhiteSpace(userPrincipalName)) throw new ArgumentNullException(nameof(userPrincipalName));
            if (userPassword == null) throw new ArgumentNullException(nameof(userPassword));

            var context = new ProjectContext(web.AbsoluteUri);

            // Add access token to each request
            context.ExecutingWebRequest += (sender, e) =>
            {
                string accessToken = EnsureAccessTokenAsync(
                    new Uri($"{web.Scheme}://{web.DnsSafeHost}"),
                    userPrincipalName,
                    userPassword
                ).GetAwaiter().GetResult();

                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken;
            };

            return context;
        }

        /// <summary>
        /// Get ClientContext for SharePoint using username/password authentication and client ID
        /// </summary>
        public ClientContext GetContext(Uri web, string userPrincipalName, SecureString userPassword, string clientId)
        {
            if (web == null) throw new ArgumentNullException(nameof(web));
            if (string.IsNullOrWhiteSpace(userPrincipalName)) throw new ArgumentNullException(nameof(userPrincipalName));
            if (userPassword == null) throw new ArgumentNullException(nameof(userPassword));
            if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentNullException(nameof(clientId));

            var context = new ProjectContext(web.AbsoluteUri);

            // Add access token to each request
            context.ExecutingWebRequest += (sender, e) =>
            {
                string accessToken = EnsureAccessTokenAsync(
                    new Uri($"{web.Scheme}://{web.DnsSafeHost}"),
                    userPrincipalName,
                    new NetworkCredential(string.Empty, userPassword).Password,
                    clientId
                ).GetAwaiter().GetResult();

                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken;
            };

            return context; 
        }

        /// <summary>
        /// Acquire access token for a given resource using username/password
        /// </summary>
        private async Task<string> EnsureAccessTokenAsync(
            Uri resource,
            string userPrincipalName,
            SecureString userPassword)
        {
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            var authority = $"https://login.microsoftonline.com/{_tenantId}";

            var app = PublicClientApplicationBuilder
                .Create(_clientId)
                .WithAuthority(authority)
                .Build();

            var scopes = new[] { $"{resource.Scheme}://{resource.Host}/.default" };

            var result = await app
                .AcquireTokenByUsernamePassword(scopes, userPrincipalName, userPassword)
                .ExecuteAsync()
                .ConfigureAwait(false);

            return result.AccessToken;
        }

        /// <summary>
        /// Acquire access token for a resource using plain password string
        /// </summary>
        private async Task<string> EnsureAccessTokenAsync(
            Uri resource,
            string userPrincipalName,
            string userPassword,
            string clientId)
        {
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            if (string.IsNullOrWhiteSpace(userPrincipalName)) throw new ArgumentNullException(nameof(userPrincipalName));
            if (string.IsNullOrWhiteSpace(userPassword)) throw new ArgumentNullException(nameof(userPassword));
            if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentNullException(nameof(clientId));

            // Convert plain password to SecureString
            var securePassword = new SecureString();
            foreach (char c in userPassword)
            {
                securePassword.AppendChar(c);
            }
            securePassword.MakeReadOnly();

            return await EnsureAccessTokenAsync(resource, userPrincipalName, securePassword);
        }

        public void Dispose()
        {
            // Nothing to dispose for now
        }
    }
}
