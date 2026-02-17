# PwaAdoBridge

A backend .NET API that syncs **Project Online (PWA)** projects and tasks with **Azure DevOps**. This project is designed as a **pure backend service** that can be deployed to Azure App Services and consumed by external clients like chatbots or other systems via HTTP requests.

---

## Table of Contents

* [Overview](#overview)
* [Features](#features)
* [Getting Started](#getting-started)
* [Configuration](#configuration)
* [Usage](#usage)
* [Project Structure](#project-structure)
* [Contributing](#contributing)
* [License](#license)

---

## Overview

This API provides endpoints to:

* Retrieve projects from **Project Online**.
* Sync projects and tasks from **Project Online** to **Azure DevOps**.
* Validate project and task data before syncing.

It does **not** include a frontend or Swagger UI—this is purely a backend service meant to be integrated with other applications, such as chatbots, automation scripts, or webhooks.

---

## Features

* Retrieve all PWA projects or a single project with tasks.
* Sync projects and tasks to Azure DevOps with **epic/task hierarchy**.
* Validate dates and required fields before syncing.
* Logging of all sync operations and errors for monitoring.
* Supports two sync modes:

  * `PwaProjectToDevOps` (default)
  * `DevOpsOnly`

---

## Getting Started

### Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download)
* Access to **Project Online (PWA)**
* Access to **Azure DevOps** with a **Personal Access Token**

### Installation

1. Clone the repository:

```bash
git clone https://github.com/<your-username>/PwaAdoBridge.git
cd PwaAdoBridge
```

2. Restore dependencies:

```bash
dotnet restore
```

3. Build the project:

```bash
dotnet build
```

4. Run locally (for testing):

```bash
dotnet run
```

---

## Configuration

The application uses **appsettings.json** or environment variables for configuration:

```json
"Pwa": {
  "Url": "<PWA site URL>",
  "TenantId": "<Azure AD tenant ID>",
  "ClientId": "<Azure AD app client ID>",
  "Username": "<PWA username>",
  "Password": "<PWA password>",
  "SharepointUrl": "<SharePoint site URL>"
},
"AzureDevOps": {
  "OrganizationUrl": "<Azure DevOps organization URL>",
  "ProjectName": "<DevOps project name>",
  "PersonalAccessToken": "<PAT token>"
}
```

Make sure credentials and tokens are **never committed** to GitHub. Use environment variables or GitHub Secrets for deployment.

---

## Usage

The API exposes endpoints like:

* `GET /api/pwa/projects` – List all PWA projects.
* `GET /api/pwa/projects/{projectUid}` – Get a project with its tasks.
* `POST /api/pwa/projects/{projectUid}/sync` – Sync a PWA project to DevOps.
* `POST /api/sync/project` – Sync project data from JSON payload (used by external apps like chatbots).

Example JSON payload for `POST /api/sync/project`:

```json
{
  "ProjectUid": "1234-5678-90AB-CDEF",
  "ProjectName": "My PWA Project",
  "StartDate": "2026-01-01T00:00:00Z",
  "FinishDate": "2026-01-31T00:00:00Z",
  "Tasks": [
    {
      "TaskUid": "ABCD-1234",
      "TaskName": "Task 1",
      "StartDate": "2026-01-01T00:00:00Z",
      "FinishDate": "2026-01-05T00:00:00Z"
    }
  ]
}
```

---

## Project Structure

* `Controllers/` – API endpoints.
* `Services/` – Business logic and integration with PWA and DevOps.
* `Dtos/` – Data transfer objects for projects and tasks.
* `Auth/` – Authentication logic for Project Online.
* `Options/` – Configuration classes for DevOps and PWA.

---

## Contributing

* Fork the repository and create a new branch for features/bugs.
* Follow clean code and logging practices.
* Submit pull requests with clear descriptions.

---

## License

This project is **MIT licensed**. See [LICENSE](LICENSE) for details.
