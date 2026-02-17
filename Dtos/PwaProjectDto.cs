using System;
using System.Collections.Generic;

namespace PwaAdoBridge.Api.Dtos
{
    /// <summary>
    /// Data Transfer Object representing a PWA project
    /// </summary>
    public class PwaProjectDto
    {
        /// <summary>
        /// Unique identifier of the project
        /// </summary>
        public required string ProjectUid { get; set; }

        /// <summary>
        /// Name of the project
        /// </summary>
        public required string ProjectName { get; set; }

        /// <summary>
        /// Optional start date of the project
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Optional finish date of the project
        /// </summary>
        public DateTime? FinishDate { get; set; }

        /// <summary>
        /// List of tasks associated with the project
        /// </summary>
        public List<PwaTaskDto> Tasks { get; set; } = new();

        /// <summary>
        /// Optional mode for sync: "PwaProjectToDevOps" or "DevOpsOnly"
        /// </summary>
        public string? Mode { get; set; }
    }

    /// <summary>
    /// Data Transfer Object representing a PWA task
    /// </summary>
    public class PwaTaskDto
    {
        /// <summary>
        /// Unique identifier of the task
        /// </summary>
        public required string TaskUid { get; set; }

        /// <summary>
        /// Name of the task
        /// </summary>
        public required string TaskName { get; set; }

        /// <summary>
        /// Optional start date of the task
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Optional finish date of the task
        /// </summary>
        public DateTime? FinishDate { get; set; }
    }
}
