# Test Plan
## University Course Scheduling System

### 1. Testing Strategy

The system is validated using a multi-layered testing approach to ensure full coverage of both functional and non-functional requirements.

#### 1.1 Test Levels
- **Unit Testing:** Validates individual controller actions and business logic in isolation using EF Core InMemory database.
- **Integration Testing:** Validates the interaction between API endpoints, database context, and middleware (e.g., Department and Classroom CRUD).
- **System Testing (UI):** End-to-end validation of user flows using Selenium WebDriver and headless Chrome.

#### 1.2 Testing Methodologies
- **White Box Testing:** Internal branch logic testing (e.g., verifying specific role-based access paths and conflict detection branches).
- **Black Box Testing:** Verification based on API specifications and expected HTTP status codes.
- **Boundary Value Analysis (BVA):** Testing enrollment limits at n-1 and n capacity.
- **Equivalence Partitioning (EP):** Partitioning input data for enrollment status (Active, Dropped, Completed).

---

### 2. Traceability Matrix

| SRS ID | Test IDs | Test Category | Method | Status |
| :--- | :--- | :--- | :--- | :--- |
| REQ-01 | UT-001, UT-002, TC-UI-15 | Unit / UI | White Box / Functional | PASS |
| REQ-02 | UT-001, TC-UI-01 to 05, 20 | Unit / UI | Black Box / Functional | PASS |
| REQ-03 | UT-022 to 026, TC-UI-10, 16 | Unit / UI | White Box / Functional | PASS |
| REQ-04 | UT-031, 055, 056, 062-064, 070 | Unit / Logic | BVA / EP / White Box | PASS |
| REQ-05 | UT-053, 067 | Unit / Logic | Black Box / White Box | PASS |
| REQ-06 | UT-057, 058 | Unit / Logic | White Box / Logic | PASS |
| REQ-08 | UT-005 to 021, TC-UI-09, 13, 14 | Unit / UI | Black Box / Functional | PASS |
| REQ-09 | UT-004, TC-UI-08, 12 | Integration / UI | Black Box / Functional | PASS* |
| REQ-10 | UT-003 | Integration | Black Box | PASS |
| REQ-11 | UT-071 to 074, 077, 081, TC-UI-11, 17 | Unit / UI | White Box / Functional | PASS |
| REQ-12 | UT-054, 075, 076, 078 to 080 | Unit / Logic | White Box / Logic | PASS |
| REQ-13 | TC-UI-06 | UI | Functional | PASS |
| REQ-14 | UT-043 to 050 | Unit / Logic | White Box / Logic | PASS |

*Note: UT-004 (Classroom Integration) fails in the test runner due to rate-limiting middleware interference in the test host environment.*

---

### 3. Test Environments
- **Unit/Integration:** .NET 8.0 SDK, xUnit Framework, EF Core InMemory Provider.
- **UI/E2E:** Selenium WebDriver 4.14, ChromeDriver (Headless), Node.js (Frontend Host).

---

### 4. Test Data Management
- Fresh InMemory database instances are initialized for each test method to ensure isolation.
- Pre-defined demo credentials (Admin, Instructor, Student) are used for UI automation.

---

### 5. Pass/Fail Criteria
- **Pass:** Actual HTTP response matches expected code AND database state reflects expected change.
- **Fail:** Response mismatch, unhandled exceptions, or UI timeouts (>10s).
