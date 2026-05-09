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

### 3. Frontend Initialization
```bash
cd Frontend
npm install
npm run dev
```
The application will be accessible at http://localhost:5173.

## Database Documentation
Detailed entity relationships and field definitions are documented in [Backend/ER_Diagram.md](./Backend/ER_Diagram.md).
