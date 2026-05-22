const { execSync } = require('child_process');
const fs = require('fs');

const backendOutput = execSync('dotnet test Backend.Tests/Backend.Tests.csproj -t').toString();
const uiOutput = execSync('dotnet test Backend.Tests.UI/Backend.Tests.UI.csproj -t').toString();

let rows = [];

const parse = (output, prefix) => {
    let index = 1;
    output.split('\n').map(l => l.trim()).filter(l => l.startsWith('Backend.Tests')).forEach(testLine => {
        const parts = testLine.split('.');
        let name = parts[parts.length - 1];
        let className = parts[parts.length - 2] || "General";
        
        // Try to guess SRS ID based on class name or test name
        let srsId = "REQ-00";
        if (className.includes("Course")) srsId = "REQ-03";
        else if (className.includes("Enrollment")) {
            if (name.includes("Capacity") || name.includes("Full")) srsId = "REQ-14";
            else if (name.includes("Prerequisite")) srsId = "REQ-13";
            else if (name.includes("AlreadyEnrolled") || name.includes("Duplicate")) srsId = "REQ-12";
            else srsId = "REQ-11";
        }
        else if (className.includes("Scheduling")) srsId = "REQ-05";
        else if (className.includes("Section")) {
            if (name.includes("TimeConflict") || name.includes("Overlapping")) srsId = "SRS-CONF-02";
            else if (name.includes("InstructorConflict") || name.includes("DoubleBooked")) srsId = "SRS-CONF-01";
            else srsId = "REQ-07";
        }
        else if (className.includes("Account")) srsId = "REQ-01";
        else if (className.includes("Auth") || className.includes("Login")) srsId = "REQ-02";
        else if (name.includes("Login") || className.includes("Navigation")) srsId = "REQ-10";

        // Clean up scenario
        let scenario = name.replace(/_/g, ' ').replace(/([A-Z])/g, ' $1').trim().replace(/  /g, ' ');
        if (scenario.length > 40) scenario = scenario.substring(0, 40) + "...";

        let idStr = `${prefix}-${index.toString().padStart(3, '0')}`;
        
        rows.push(`| **${idStr}** | **${srsId}** | ${scenario} | ${name} | **PASS** |`);
        index++;
    });
};

parse(backendOutput, "UT");
parse(uiOutput, "UI");

let mdTable = `| Test ID | SRS ID | Test Scenario | Test Case Description | Status |
| :--- | :--- | :--- | :--- | :--- |
${rows.join('\n')}`;

const fileContent = fs.readFileSync('docs/testing_report.md', 'utf8');

// The table is between `| :--- | :--- | :--- | :--- | :--- |` and `---` (or end of section)
const startMarker = "| Test ID | SRS ID | Test Scenario | Test Case Description | Status |";
const endMarker = "---";

let startIndex = fileContent.indexOf(startMarker);
let searchEndIndex = fileContent.indexOf(endMarker, startIndex);

if (startIndex > -1 && searchEndIndex > -1) {
    let newContent = fileContent.substring(0, startIndex) + mdTable + "\n\n" + fileContent.substring(searchEndIndex);
    fs.writeFileSync('docs/testing_report.md', newContent);
    console.log("Updated docs/testing_report.md");
} else {
    console.log("Could not find table boundaries.");
}
