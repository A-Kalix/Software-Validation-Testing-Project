const pptxgen = require("pptxgenjs");
let pres = new pptxgen();
let slide = pres.addSlide();
let tableData = [["Header 1", "Header 2"]];
for (let i = 0; i < 50; i++) {
  tableData.push([`Row ${i}`, `Value ${i}`]);
}
slide.addTable(tableData, { x: 0.5, y: 1.0, w: 9, autoPage: true });
pres.writeFile({ fileName: "scratch.pptx" }).then(() => console.log("Done"));
