# University Course Scheduling System

A professional management system for university academic catalogs, course sections, and student registrations.

## Project Status

The project has completed its initialization phase. The following infrastructure is now in place:

- **Database:** Relational schema defined and initial migration generated.
- **Backend:** .NET 8 API initialized with CORS policies and core Data Transfer Objects (DTOs).
- **Frontend:** React + Vite architecture established with a centralized API client and standardized folder structure.
- **Environment:** Docker configuration ready for local SQL Server deployment.

The project is currently ready for functional development of API controllers and frontend views.

## Technology Stack

- **Backend:** .NET 8 Web API, Entity Framework Core
- **Database:** SQL Server 2022
- **Frontend:** React, Vite, Tailwind CSS, Axios, React Router
- **Infrastructure:** Docker Compose

## Project Structure

- **/Backend:** Contains the .NET API source code, database models, and migrations.
- **/Frontend:** Contains the React application and UI components.
- **/Backend/ER_Diagram.md:** Technical documentation of the database schema and relationships.

## Setup Instructions

### Prerequisites
- Docker Desktop
- .NET 8 SDK
- Node.js (v18 or higher)

### 1. Database Initialization
Start the SQL Server container:
```bash
docker-compose up -d
```

### 2. Backend Initialization
```bash
cd Backend
dotnet restore
dotnet run
```
The API documentation will be available at http://localhost:5000/swagger.

### 4. Frontend Initialization
```bash
cd Frontend
npm install
npm run dev
```
The application will be accessible at http://localhost:5173.

### 5. SonarQube Analysis
1. Start SonarQube locally:
```bash
docker run -d --name sonarqube \
  -p 9000:9000 \
  -e SONAR_ES_BOOTSTRAP_CHECKS_DISABLE=true \
  sonarqube:community
```
2. Create a Sonar project and generate a token.
3. Copy `.env.example` to `.env` and update the values for your local environment:
```bash
cp .env.example .env
```
4. Set `SONAR_TOKEN` and `SONAR_HOST_URL` in your shell (or add them as GitHub repository secrets if you want CI to run Sonar):
```bash
export SONAR_TOKEN="<your_token>"
export SONAR_HOST_URL="https://your-sonar-host.example"
```
5. Run the helper script from the repo root (or let CI invoke it):
```bash
chmod +x scripts/run-sonar.sh
./scripts/run-sonar.sh
```

CI / GitHub Actions
-------------------
To run Sonar analysis in CI you must add the following repository secrets:

- `SONAR_TOKEN` — the token created for your Sonar project
- `SONAR_HOST_URL` — SonarQube or SonarCloud URL (e.g. `https://sonarcloud.io`)
- `MSSQL_SA_PASSWORD` — SQL Server SA password used by Docker in CI
- `UI_TEST_USERNAME` and `UI_TEST_PASSWORD` — credentials used by Selenium UI tests

The workflow `.github/workflows/main.yml` includes a `sonar-analysis` job that will run the scanner, collect coverage for both backend and UI tests, and upload results to Sonar.

## Selenium UI Testing
The repo already includes Selenium UI tests under `Backend.Tests.UI`. The helper script now starts the database container, backend, and frontend before executing the UI tests.

```bash
chmod +x scripts/run-selenium-ui.sh
./scripts/run-selenium-ui.sh
```

If you need to run with browser UI visible, set:
```bash
export UI_TEST_HEADLESS=false
./scripts/run-selenium-ui.sh
```

If you want to only execute the UI tests without the helper script, make sure Docker, the backend, and the frontend are running and then run:
```bash
dotnet test Backend.Tests.UI/Backend.Tests.UI.csproj
```

## Database Documentation
Detailed entity relationships and field definitions are documented in [Backend/ER_Diagram.md](./Backend/ER_Diagram.md).
