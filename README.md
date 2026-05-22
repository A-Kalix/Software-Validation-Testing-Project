# University Course Scheduling & Management System

A production-grade, highly automated academic management system for course catalogs, class sections, student registrations, and automated scheduling conflicts analysis.

This repository features **100% passing tests** (103/103) across Entity Framework Core integration tests and Selenium End-to-End (E2E) UI tests, coupled with a complete **SonarQube Quality Gate** configuration.

---

## Technical Stack

- **Backend:** .NET 8.0 Web API, Entity Framework Core (EF Core)
- **Database:** SQL Server 2022 (Linux containerized)
- **Frontend:** React, Vite, CSS (Custom Academic Layout System), Axios, React Router v6
- **Test Automation:** xUnit, Selenium WebDriver (Headless/Interactive Chrome), Coverlet
- **Infrastructure & CI:** Docker Compose, SonarQube Scanner, GitHub Actions

---

## Core Infrastructure Layout

- `/Backend`: Contains the ASP.NET Core source code, controllers, DTO definitions, and EF migrations.
- `/Backend.Tests`: 100 backend integration and unit tests validating enrollment boundaries, prerequisites, and scheduling constraints.
- `/Backend.Tests.UI`: Selenium E2E tests validating the student login, dashboard layouts, and page routing.
- `/Frontend`: Standard Vite + React codebase containing academic dashboards and Master Schedule conflict grids.
- `/docs`: Verification and validation documentation, including [SRS.md](./docs/SRS.md) and [testing_report.md](./docs/testing_report.md).
- `/scripts`: Automated helper scripts to run Selenium UI tests and local SonarQube analyses.

---

## Setup & Execution Guide

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Node.js (v18 or higher)](https://nodejs.org/)

---

### Step 1: Clone and Configure Environment

1. Clone the repository and navigate to the project root.
2. Create your local environment configuration file from the template:
   ```bash
   cp .env.example .env
   ```
3. Open `.env` and verify the values. By default, it is pre-configured with secure credentials:
   - `MSSQL_SA_PASSWORD=ChangeMePassword!`
   - `JWT_SECRET=ChangeMeSecretKeyForUniversitySchedulerSystem2026!` (satisfies the strict 256-bit signature validation)
   - `CONNECTION_STR=Server=localhost,1433;Database=UniversityScheduler;User Id=sa;Password=ChangeMePassword!;TrustServerCertificate=True`

---

### Step 2: Start SQL Server Database

Launch the SQL Server container in the background:
```bash
docker compose up -d db
```
*Note: If you have pre-existing volume data with an outdated password, clean it fresh using `docker compose down -v` and rebuild.*

---

### Step 3: Run the Backend API

Start the backend application. The system has **self-healing migrations** built into `Program.cs` that will automatically create the database, execute EF migrations, and run the `DbSeeder` dynamically on startup!

```bash
cd Backend
dotnet restore
dotnet run
```
- The backend will begin listening at: `http://localhost:5005`
- The interactive Swagger documentation will be accessible at: `http://localhost:5005/swagger`

---

### Step 4: Run the Frontend UI

Open a new terminal window, install dependencies, and launch the Vite dev server:

```bash
cd Frontend
npm install
npm run dev
```
- The React application will start in standard port: `http://localhost:5173`
- Open your browser to log in using the pre-seeded credentials:
  - **Admin:** `admin@university.edu` / `Demo123!`
  - **Instructor:** `teacher@university.edu` / `Demo123!`
  - **Student:** `student@university.edu` / `Demo123!`

---

## Running the Automated Test Suites

### 1. Unit & Integration Test Suite
Execute the 100 backend integration tests (which utilize a isolated in-memory DB provider to execute in under 2 seconds):
```bash
dotnet test Backend.Tests/Backend.Tests.csproj
```

### 2. Selenium End-to-End UI Test Suite
Ensure that the SQL Server, Backend API, and Vite Frontend are fully operational, then run the UI test suite:
```bash
dotnet test Backend.Tests.UI/Backend.Tests.UI.csproj
```

#### Running Headless vs. Interactive mode
By default, the Selenium tests run in **headless** mode (suitable for CI execution). If you want to watch the automated browser open and execute the steps interactively, set the following environment variable:
```bash
export UI_TEST_HEADLESS=false
dotnet test Backend.Tests.UI/Backend.Tests.UI.csproj
```

#### E2E Orchestrator Script
Alternatively, you can run the all-in-one execution script. It spins up standard Docker db container, starts backend + frontend services in background, waits for ports to open, executes Selenium, and teardown services automatically:
```bash
chmod +x scripts/run-selenium-ui.sh
./scripts/run-selenium-ui.sh
```

---

## Running Local SonarQube Scanner

1. Spin up a local SonarQube Community container:
   ```bash
   docker run -d --name sonarqube -p 9000:9000 -e SONAR_ES_BOOTSTRAP_CHECKS_DISABLE=true sonarqube:community
   ```
2. Open `http://localhost:9000`, log in (default: `admin`/`admin`), set a new password, create a project named `software-validation-testing-project`, and generate a token.
3. Configure the token in your shell environment:
   ```bash
   export SONAR_TOKEN="<your_generated_token>"
   export SONAR_HOST_URL="http://localhost:9000"
   ```
4. Run the pre-configured Sonar analysis orchestrator script:
   ```bash
   chmod +x scripts/run-sonar.sh
   ./scripts/run-sonar.sh
   ```
5. Open your SonarQube dashboard to view complete analysis metrics, bugs, vulnerabilities, code smells, and statement coverages.
