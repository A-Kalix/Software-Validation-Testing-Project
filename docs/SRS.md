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

### 2. Non-Functional Requirements

| ID | Description |
| :--- | :--- |
| **NFR-01** | **Security:** Passwords must be hashed using BCrypt. |
| **NFR-02** | **Performance:** Authentication response must be under 500ms. |
| **NFR-03** | **Scalability:** System must handle concurrent enrollment requests. |
| **NFR-04** | **Usability:** Interface must follow the semantic academic design system. |
