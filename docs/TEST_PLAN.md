# Test Plan & Traceability Matrix

## 1. Traceability Matrix
Maps Requirements (SRS) to Test Cases (TC).

| SRS ID | Test ID | Test Type | Method | Status |
| :--- | :--- | :--- | :--- | :--- |
| **REQ-01** | TC-01 | Unit (White Box) | `AccountControllerTests.Register_ValidUser` | **PASS** |
| **REQ-01** | TC-02 | Unit (White Box) | `AccountControllerTests.Register_DuplicateEmail` | **PASS** |
| **REQ-02** | TC-03 | Integration | `AccountController.Login` Endpoint Test | **PASS** |
| **REQ-04** | TC-04 | Logic | Enrollment Capacity Verification | **PASS** |
| **REQ-05** | TC-05 | Logic | Duplicate Enrollment Prevention | **PASS** |
| **REQ-06** | TC-06 | Logic | Prerequisite Validation Logic | **PASS** |

## 2. Test Scenarios (Black Box)

### **TS-01: Authentication Flow**
*   **Scenario:** Attempt login with an unregistered email.
*   **Expected:** `401 Unauthorized` with a generic error message.
*   **Requirement:** REQ-02

### **TS-02: Enrollment Validation**
*   **Scenario:** Student tries to enroll in a section where `activeCount >= capacity`.
*   **Expected:** `409 Conflict` with "This section is full." message.
*   **Requirement:** REQ-04

### **TS-03: UI Validation (Selenium)**
*   **Scenario:** Click "Sign In" with empty fields.
*   **Expected:** Frontend validation shows "Required" under input fields.
*   **Requirement:** NFR-04
