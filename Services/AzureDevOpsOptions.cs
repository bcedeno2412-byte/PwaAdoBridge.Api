namespace PwaAdoBridge.Api.Options
{
    // Options class to hold Azure DevOps configuration values
    // These values are loaded from appsettings.json or environment variables
    public class AzureDevOpsOptions
    {
        // Base URL of the Azure DevOps organization
        public string OrganizationUrl { get; set; } = string.Empty;

        // Name of the Azure DevOps project where work items will be created
        public string ProjectName { get; set; } = string.Empty;

        // Personal Access Token (PAT) for authentication with Azure DevOps
        public string PersonalAccessToken { get; set; } = string.Empty;
    }
}
