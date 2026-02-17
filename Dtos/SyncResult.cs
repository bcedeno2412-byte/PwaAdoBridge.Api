using System.Collections.Generic;

namespace PwaAdoBridge.Api.Dtos
{
    // Result of the synchronization between PWA and Azure DevOps
    public class SyncResult
    {
        // Indicates whether the synchronization was successful
        public bool Success { get; set; } = false;

        // Descriptive message of the operation
        public string? Message { get; set; }

        // Specific error code (optional)
        public string? ErrorCode { get; set; }

        // List of validation errors, if any
        public List<string> ValidationErrors { get; set; } = new();

        // Number of projects processed during this synchronization
        public int ProjectsProcessed { get; set; }

        // Number of work items created in DevOps
        public int WorkItemsCreated { get; set; }

        // Number of work items updated in DevOps
        public int WorkItemsUpdated { get; set; }

        // Total number of errors that occurred during synchronization
        public int Errors { get; set; }
    }
}
