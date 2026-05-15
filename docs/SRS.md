# Software Requirements Specification (SRS)
## Project: University Course Scheduling System

### 1. Functional Requirements

| ID | Description | Priority |
| :--- | :--- | :--- |
| **REQ-01** | Users must be able to register an account with a university email. | High |
| **REQ-02** | Users must be able to login and receive a secure JWT token. | High |
| **REQ-03** | Students must be able to view available course sections. | High |
| **REQ-04** | Students must be able to enroll in a section if capacity allows. | Medium |
| **REQ-05** | The system must prevent duplicate enrollment in the same section. | Medium |
| **REQ-06** | The system must verify mandatory prerequisites before enrollment. | High |
| **REQ-07** | Users must be able to update their profile and change passwords. | Low |
| **REQ-08** | Admins must be able to manage the course catalog (Create, Read, Update, Delete courses). | High |
| **REQ-09** | Admins must be able to manage university classrooms (Create, Read, Update, Delete rooms). | High |
| **REQ-10** | Admins must be able to manage university departments. | Medium |
| **REQ-11** | Admins must be able to run an automated scheduling engine to generate draft sections. | High |
| **REQ-12** | Admins must be able to publish draft schedules to make them live for student enrollment. | High |
| **REQ-13** | Instructors must be able to view their assigned class rosters. | Medium |
| **REQ-14** | The system must detect and prevent scheduling conflicts for instructors and rooms. | High |

### 2. Non-Functional Requirements

| ID | Description |
| :--- | :--- |
| **NFR-01** | **Security:** Passwords must be hashed using BCrypt. |
| **NFR-02** | **Performance:** Authentication response must be under 500ms. |
| **NFR-03** | **Scalability:** System must handle concurrent enrollment requests. |
| **NFR-04** | **Usability:** Interface must follow the semantic academic design system. |
| **NFR-05** | **Availability:** System must provide role-based access control (RBAC) to ensure feature security. |
