# PwaAdoBridge API

![.NET](https://img.shields.io/badge/.NET-10-blue)
![C#](https://img.shields.io/badge/C%23-Backend-green)
![Architecture](https://img.shields.io/badge/Architecture-Clean-orange)

## 📌 Overview

PwaAdoBridge API is a backend service that synchronizes Microsoft Project Online (PWA) projects and tasks with Azure DevOps work items.

This project demonstrates clean architecture principles, structured DTO modeling, external service integration, and professional API documentation using Swagger.

> ⚠️ This is a backend-only API. There is no frontend UI included.

---

## 🛠️ Tech Stack

- .NET 10
- C#
- ASP.NET Core Web API
- Swagger (OpenAPI)
- Azure DevOps REST API
- Microsoft Project Online (PWA)
- MSAL Authentication

---

## 🏗️ Architecture

The project follows a layered and maintainable structure:

- Controllers → Handle HTTP requests
- Services → Business logic & external integrations
- DTOs → Structured data contracts
- Options → Configuration binding (Azure DevOps settings)
- Auth → Token acquisition using MSAL

Designed to be consumed by other applications or enterprise systems.

---

## 🚀 How to Run Locally

1. Clone the repository:

```bash
git clone https://github.com/bcedeno2412-byte/PwaAdoBridge.Api.git
```

2. Navigate into the project:

```bash
cd PwaAdoBridge.Api
```

3. Run the API:

```bash
dotnet run
```

4. Open Swagger in your browser:

```
http://localhost:{PORT}/swagger
```

(Use the port shown in your terminal output.)

---

## 📬 Available Endpoint

### POST `/api/devops-test/demo`

Creates demo PWA projects and synchronizes them into Azure DevOps as work items.

---

## 📦 Example Response (SyncResult)

```json
{
  "success": true,
  "message": "Synchronization completed",
  "errorCode": null,
  "validationErrors": [],
  "projectsProcessed": 1,
  "workItemsCreated": 3,
  "workItemsUpdated": 0,
  "errors": 0
}
```

---

## 📦 Example Payload (PwaProjectDto)

```json
{
  "projectUid": "12345",
  "projectName": "Demo Project",
  "startDate": "2026-02-01T00:00:00Z",
  "finishDate": "2026-02-10T00:00:00Z",
  "tasks": [
    {
      "taskUid": "task-1",
      "taskName": "Planning",
      "startDate": "2026-02-01T00:00:00Z",
      "finishDate": "2026-02-03T00:00:00Z"
    }
  ]
}
```

---

## 📖 Swagger Documentation

Swagger UI is enabled in development mode.

It provides:

- Full endpoint documentation
- Schema definitions (DTOs like `SyncResult`)
- Interactive request testing
- Structured response modeling

Access via:

```
/swagger
```

---

## 🔐 Configuration

Sensitive credentials must be configured inside:

```
appsettings.json
```

Example configuration includes:

- Azure DevOps Organization URL
- Project Name
- Personal Access Token
- Tenant ID / Client ID (for MSAL authentication)

⚠️ Never commit real credentials to GitHub.

---

## 🎯 Purpose of This Project

This project demonstrates:

- Enterprise backend API design
- Integration with Microsoft services
- Clean separation of concerns
- Structured error handling & result modeling
- Professional API documentation

---

## 👨‍💻 Author

Bryan Cedeno  
Backend Developer  
Costa Rica

---

## License

This project is **MIT licensed**. See [LICENSE](LICENSE) for details.
