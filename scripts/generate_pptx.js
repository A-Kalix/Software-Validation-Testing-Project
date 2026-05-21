const pptxgen = require("pptxgenjs");
const fs = require("fs");

let pres = new pptxgen();
pres.layout = 'LAYOUT_WIDE';
pres.author = 'Antigravity AI';
pres.title = 'University Scheduling System Progress Report';

const IMG_CAMPUS = "/Users/linanashbat/.gemini/antigravity/brain/51bec40f-f330-4007-924c-3635827d2836/university_campus_minimalist_1778829593084.png";
const IMG_ARCH = "/Users/linanashbat/.gemini/antigravity/brain/51bec40f-f330-4007-924c-3635827d2836/system_architecture_abstract_1778829607426.png";
const IMG_VALID = "/Users/linanashbat/.gemini/antigravity/brain/51bec40f-f330-4007-924c-3635827d2836/academic_validation_minimalist_1778829619880.png";

// Define Master Slides
pres.defineSlideMaster({
  title: 'TITLE_SLIDE',
  background: { color: '1E293B' },
  objects: [
    { rect: { x: 0, y: 0, w: '100%', h: 0.1, fill: { color: '0EA5E9' } } },
    { text: { text: 'UNIVERSITY SCHEDULING SYSTEM', options: { x: 0.5, y: 4.5, w: 12, fontSize: 44, color: 'FFFFFF', bold: true, align: 'left', charSpacing: 2 } } },
    { text: { text: 'PROGRESS & VALIDATION REPORT', options: { x: 0.5, y: 5.2, w: 12, fontSize: 24, color: '94A3B8', align: 'left', italic: true } } }
  ]
});

pres.defineSlideMaster({
  title: 'CONTENT_SLIDE',
  background: { color: 'F8FAFC' },
  objects: [
    { rect: { x: 0, y: 0, w: '100%', h: 0.6, fill: { color: '1E293B' } } },
    { placeholder: { options: { name: 'title', type: 'title', x: 0.5, y: 0.1, w: 9, h: 0.4, fontSize: 28, color: 'FFFFFF', bold: true, align: 'left', margin: 0 } } },
    { rect: { x: 0, y: 7.2, w: '100%', h: 0.3, fill: { color: 'F1F5F9' } } },
    { text: { text: 'Project Validation Report 2026', options: { x: 0.5, y: 7.22, w: 5, fontSize: 10, color: '64748B' } } }
  ]
});

// Helper for card-style content
const addCard = (slide, x, y, w, h, title, lines) => {
  slide.addShape(pres.shapes.RECTANGLE, { x, y, w, h, fill: { color: 'FFFFFF' }, line: { color: 'E2E8F0', width: 1 }, shadow: { type: 'outer', blur: 4, offset: 2, color: '000000', opacity: 0.05 } });
  slide.addText(title, { x: x+0.2, y: y+0.2, w: w-0.4, fontSize: 18, color: '0EA5E9', bold: true });
  slide.addText(lines.map(l => ({ text: l, options: { bullet: true, breakLine: true } })), { x: x+0.2, y: y+0.6, w: w-0.4, h: h-0.8, fontSize: 12, color: '334155' });
};

// Slide 1: Title
let s1 = pres.addSlide({ masterName: 'TITLE_SLIDE' });
s1.addImage({ path: IMG_CAMPUS, x: 0, y: 0, w: 13.3, h: 4.2, sizing: { type: 'cover' } });

// Slide 2: Project Milestones
let s2 = pres.addSlide({ masterName: 'CONTENT_SLIDE' });
s2.addText('Project Milestones & Current State', { placeholder: 'title' });
addCard(s2, 0.5, 1.0, 6.0, 2.8, 'Infrastructure Complete', [
  'Production-hardened .NET 8 Web API backend',
  'Semantic React frontend with centralized design system',
  'SQLite database with fully synchronized migrations',
  'Robust JWT-based Role-Based Access Control (RBAC)'
]);
addCard(s2, 6.8, 1.0, 6.0, 2.8, 'Feature Parity Achieved', [
  'Admin: Full Course, Room, and Department CRUD',
  'Instructor: Dedicated Class Rosters & Scheduling',
  'Student: Real-time Section Enrollment & Scheduling',
  'Automated Scheduling Engine & Publishing Pipeline'
]);

// Slide 3: Development Collaboration History (ACCURATE)
let s3 = pres.addSlide({ masterName: 'CONTENT_SLIDE' });
s3.addText('Development Collaboration History', { placeholder: 'title' });
s3.addTable([
  ['Contributor', 'Primary Role / Actual Work', 'Project Impact'],
  ['Fulya H. Aykıt', 'Project Admin', 'Administrative orchestration and team coordination'],
  ['Ahmed Khalil', 'Technical Lead', 'Core architecture, CI/CD, and Backend/Frontend integration'],
  ['Al-Hakeem', 'Backend Developer', 'Enrollment controller, DTO architecture, and Identity logic'],
  ['Serenat Varol', 'Backend Developer', 'Department and Classroom management implementation'],
  ['Kübra Laçin', 'UI & Documentation', 'Initial frontend scaffolding and technical documentation'],
  ['mirsat', 'Integration Support', 'Merge coordination and branch maintenance (mirsad branch)'],
  ['Tolga Topçu', 'Technical Support', 'Infrastructure oversight and technical verification support']
], { x: 0.5, y: 1.0, w: 12.3, colW: [2.5, 4.5, 5.3], fill: { color: 'FFFFFF' }, border: { pt: 1, color: 'E2E8F0' }, fontSize: 11 });

// Slide 4: System Architecture
let s4 = pres.addSlide({ masterName: 'CONTENT_SLIDE' });
s4.addText('Architectural Repository Structure', { placeholder: 'title' });
s4.addImage({ path: IMG_ARCH, x: 8.5, y: 1.0, w: 4.3, h: 5.8, sizing: { type: 'cover' }, rounding: true });
s4.addText([
  { text: 'Backend Layer (.NET 8)\n', options: { bold: true, color: '0EA5E9', fontSize: 16 } },
  { text: '   /Controllers: Business logic & RBAC enforcement\n', options: { fontSize: 12 } },
  { text: '   /Models: Entity definitions (User, Course, Section, Room)\n', options: { fontSize: 12 } },
  { text: '   /Data: DbContext & Migration history\n\n', options: { fontSize: 12 } },
  { text: 'Frontend Layer (React)\n', options: { bold: true, color: '0EA5E9', fontSize: 16 } },
  { text: '   /services: Axios clients & interceptors\n', options: { fontSize: 12 } },
  { text: '   /pages: Role-specific portals (Login, Dashboard, Catalog)\n', options: { fontSize: 12 } },
  { text: '   /components: Reusable semantic UI elements\n\n', options: { fontSize: 12 } },
  { text: 'Testing Suites\n', options: { bold: true, color: '0EA5E9', fontSize: 16 } },
  { text: '   /Backend.Tests: 81 Unit/Integration tests\n', options: { fontSize: 12 } },
  { text: '   /Backend.Tests.UI: 20 Selenium E2E tests', options: { fontSize: 12 } }
], { x: 0.5, y: 1.0, w: 7.5, h: 5.5, valign: 'top' });

// Slide 5: Deep Traceability: Enrollment Suite
let s5 = pres.addSlide({ masterName: 'CONTENT_SLIDE' });
s5.addText('Deep Traceability: Enrollment Controller', { placeholder: 'title' });
s5.addTable([
  ['Test Case', 'Test File Line', 'Target Method', 'Target Lines'],
  ['Enroll_ReturnsCreated_WhenValid', 'L121', 'Enroll', '78-132'],
  ['Enroll_ReturnsConflict_Duplicate', 'L141', 'Enroll', '94-100'],
  ['Enroll_ReturnsBadRequest_Draft', 'L166', 'Enroll', '90-91'],
  ['Enroll_Succeeds_Capacity_BVA', 'L185', 'Enroll', '103-105'],
  ['Enroll_Unprocessable_PrereqMissing', 'L243', 'Enroll', '108-116'],
  ['GetAll_Student_Ownership', 'L342', 'GetAll', '42-46'],
  ['UpdateStatus_Valid_EP', 'L402', 'UpdateStatus', '135-161'],
  ['Delete_ReturnsNoContent_Admin', 'L500', 'Delete', '165-173']
], { x: 0.5, y: 1.0, w: 12.3, colW: [3.5, 2, 3, 3.8], fill: { color: 'FFFFFF' }, border: { pt: 1, color: 'E2E8F0' }, fontSize: 11 });

// Slide 6: Deep Traceability: Scheduling Suite
let s6 = pres.addSlide({ masterName: 'CONTENT_SLIDE' });
s6.addText('Deep Traceability: Scheduling & Orchestration', { placeholder: 'title' });
s6.addTable([
  ['Test Case', 'Test File Line', 'Target Method', 'Target Lines'],
  ['RunScheduler_ReturnsOk', 'L100', 'RunAutoScheduler', '27-121'],
  ['RunScheduler_PreservesPublished', 'L121', 'RunAutoScheduler', '50-52'],
  ['RunScheduler_ClearsDrafts', 'L151', 'RunAutoScheduler', '30-35'],
  ['Publish_ReturnsOk_SetsIsPublished', 'L240', 'PublishSchedule', '124-146'],
  ['GetStatus_ReturnsDraft_State', 'L284', 'GetSemesterStatus', '160-167'],
  ['GetStatus_ReturnsPartial_Mixed', 'L326', 'GetSemesterStatus', '160-167'],
  ['GetStatus_IgnoresOtherSemesters', 'L360', 'GetSemesterStatus', '151-153']
], { x: 0.5, y: 1.0, w: 12.3, colW: [3.5, 2, 3, 3.8], fill: { color: 'FFFFFF' }, border: { pt: 1, color: 'E2E8F0' }, fontSize: 11 });

// Slide 7: Selenium UI Testing Details
let s7 = pres.addSlide({ masterName: 'CONTENT_SLIDE' });
s7.addText('Selenium Automated E2E Testing', { placeholder: 'title' });
addCard(s7, 0.5, 1.0, 4.0, 4.0, 'Auth & Navigation', [
  'TC_UI_01: Admin Login Redirect',
  'TC_UI_06: Role-Based Sidebar Visibility',
  'TC_UI_19: Protected Route Guard',
  'TC_UI_05: Session Logout Persistence'
]);
addCard(s7, 4.7, 1.0, 4.0, 4.0, 'Administrative CRUD', [
  'TC_UI_12: Add Room Modal Flow',
  'TC_UI_14: Add Course Modal Interaction',
  'TC_UI_08: Navigation to Room Mgmt',
  'TC_UI_09: Navigation to Catalog'
]);
addCard(s7, 8.9, 1.0, 3.9, 4.0, 'Orchestration UI', [
  'TC_UI_17: Master Schedule Run Button',
  'TC_UI_18: Sidebar Collapse Animation',
  'TC_UI_03: Credential Card Auto-fill',
  'TC_UI_20: Password Visibility Toggle'
]);

// Slide 8: Continuous Integration & Actions
let s8 = pres.addSlide({ masterName: 'CONTENT_SLIDE' });
s8.addText('Continuous Integration Workflow', { placeholder: 'title' });
addCard(s8, 0.5, 1.0, 4.0, 5.5, 'Backend Pipeline', [
  'dotnet restore',
  'dotnet build',
  'dotnet test --filter "Category!=UI"',
  'Artifact: Test results export'
]);
addCard(s8, 4.7, 1.0, 4.0, 5.5, 'Frontend Pipeline', [
  'npm install',
  'npm run build',
  'Static analysis via PostCSS',
  'Production bundle verification'
]);
addCard(s8, 8.9, 1.0, 3.9, 5.5, 'Automation Triggers', [
  'Push to main',
  'Pull Request created',
  'Scheduled nightly validation'
]);

// Slide 9: Requirements Traceability
let s9 = pres.addSlide({ masterName: 'CONTENT_SLIDE' });
s9.addText('SRS Traceability Summary', { placeholder: 'title' });
s9.addTable([
  ['SRS ID', 'Description', 'Test Coverage', 'Status'],
  ['REQ-01', 'Registration', 'UT-001, UI-TC-15', '100%'],
  ['REQ-02', 'Auth / Login', 'UT-001, UI-TC-01, 05, 20', '100%'],
  ['REQ-04', 'Enrollment Logic', 'UT-055, 056, 066', '100%'],
  ['REQ-08', 'Course Catalog', 'UT-005, UI-TC-09, 13', '100%'],
  ['REQ-11', 'Auto-Scheduling', 'UT-071, UI-TC-17', '100%'],
  ['REQ-12', 'Publishing Pipeline', 'UT-076, 080', '100%']
], { x: 0.5, y: 1.0, w: 12.3, colW: [2, 4, 4, 2.3], fill: { color: 'FFFFFF' }, border: { pt: 1, color: 'E2E8F0' }, fontSize: 11 });

// Slide 10: Conclusion & Repository
let s10 = pres.addSlide({ masterName: 'TITLE_SLIDE' });
s10.addText('FINAL SUMMARY', { x: 0.5, y: 4.5, w: 12, fontSize: 32, color: '0EA5E9', bold: true, align: 'center' });
s10.addText('The University Scheduling System is production-hardened with 101 tests, full RBAC security, and a complete administrative orchestrator.', { x: 1, y: 5.2, w: 11, fontSize: 16, color: 'FFFFFF', align: 'center' });
s10.addText('REPOSITORY LINK', { x: 0.5, y: 6.0, w: 12, fontSize: 20, color: '94A3B8', bold: true, align: 'center' });
s10.addText('https://github.com/A-Khalix/Software-Validation-Testing-Project', { x: 0.5, y: 6.5, w: 12, fontSize: 16, color: '0EA5E9', align: 'center', underline: true });

pres.writeFile({ fileName: "University_Scheduling_System_Progress_REAL.pptx" });
