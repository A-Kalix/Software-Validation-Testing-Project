# Software Requirements Specification (SRS)
## Project: University Course Scheduling System
**Version:** 3.0 (Pragmatic & Hardened) | **Team:** A-Kalix

---

### 1. Functional Requirements

The system handles three primary roles: **Admin**, **Instructor**, and **Student**. Features are secured via role-based access control (RBAC).

| ID | Category | Requirement Description | Priority |
| :--- | :--- | :--- | :--- |
| **REQ-01** | Account | Users must be able to register and manage accounts with verified university emails. | High |
| **REQ-02** | Auth | Users must be able to authenticate securely and obtain a JWT token for RBAC validation. | High |
| **REQ-03** | Course | Admins must be able to manage the Course Catalog (Create, Read, Update, Delete courses). | High |
| **REQ-04** | Room | Admins must be able to manage Classrooms (Create, Read, Update, Delete rooms, including capacity limits). | High |
| **REQ-05** | Schedule | Admins must be able to run an automated scheduling engine to generate draft course sections. | High |
| **REQ-06** | Schedule | Admins must be able to view conflicts (Instructor double-bookings, Room double-bookings, over-capacity). | High |
| **REQ-07** | Schedule | Admins must be able to publish draft sections to make them active for enrollment. | High |
| **REQ-08** | Availability | Instructors must be able to declare daily availability slots and preferences (Preferred, Neutral, Busy). | High |
| **REQ-09** | Availability | The scheduling engine must respect instructor availability preferences when positioning sections. | Medium |
| **REQ-10** | Enrollment | Students must be able to search and view all published course sections. | High |
| **REQ-11** | Enrollment | Students must be able to enroll in active sections if seat capacity is available. | High |
| **REQ-12** | Enrollment | The system must prevent a student from duplicate enrollment in the same section. | High |
| **REQ-13** | Enrollment | The system must enforce mandatory course prerequisites before allowing registration. | High |
| **REQ-14** | Capacity | The system must prevent student enrollment from exceeding classroom/section seat capacity. | High |

---

### 2. Conflict Detection & Validation Specifications

To ensure the integrity of the master academic schedule, the system must enforce strict conflict analysis rules.

#### A. Instructor Double-Booking (SRS-CONF-01)
- **Rule:** An instructor cannot be assigned to two different sections that overlap in days and times.
- **Validation:** Master Schedule UI must scan all active/draft sections and highlight overlapping slots assigned to the same lecturer with an **Instructor Overlap** alert on the Admin panel.

#### B. Classroom Double-Booking (SRS-CONF-02)
- **Rule:** A physical classroom cannot host two different course sections simultaneously.
- **Validation:** The scheduling engine and manual section creation forms must block overlapping room allocations and raise a **Room Overlap** warning on the Master Schedule.

#### C. Classroom Over-Capacity (SRS-CONF-03)
- **Rule:** Active enrollments in a section must never exceed the room's physical seating capacity.
- **Validation:** 
  - Real-world "average" target utilization should fall between 30% and 90% under normal operations.
  - If a section's enrollment strictly exceeds its capacity (e.g. 8 students enrolled in a room of capacity 5), a **Capacity Warning** badge must display adjacent to the section on the Admin Master Schedule page.

---

### 3. Non-Functional Requirements

| ID | Category | Specification |
| :--- | :--- | :--- |
| **NFR-01** | **Security** | All passwords must be hashed securely using `BCrypt.Net`. JWT secret keys must be at least 256 bits (32 bytes) to satisfy strict signature verification. |
| **NFR-02** | **Performance** | API response times for standard queries (Catalog search, schedule viewing) must be under 300ms. |
| **NFR-03** | **Scalability** | The database seeder must scale the student body (300+ students) and sections (55+ sections) to validate utilization graphs realistically. |
| **NFR-04** | **Usability** | The user interface must support responsive navigation with a clean academic dashboard shell. |
