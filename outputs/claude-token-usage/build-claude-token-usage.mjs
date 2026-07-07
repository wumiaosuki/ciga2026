import fs from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = "/Users/wepie/ciga2026/outputs/claude-token-usage";
const logRoot = path.join(os.homedir(), ".claude", "projects");
const outputPath = path.join(outputDir, "claude_token_usage_by_project_date.xlsx");

const tokenFields = [
  "input_tokens",
  "output_tokens",
  "cache_creation_input_tokens",
  "cache_read_input_tokens",
];

function excelCol(index) {
  let n = index + 1;
  let s = "";
  while (n > 0) {
    const r = (n - 1) % 26;
    s = String.fromCharCode(65 + r) + s;
    n = Math.floor((n - 1) / 26);
  }
  return s;
}

function escapeFormulaString(value) {
  return String(value).replaceAll('"', '""');
}

function dateObject(isoDate) {
  return new Date(`${isoDate}T00:00:00Z`);
}

async function walkJsonlFiles(dir) {
  const out = [];
  async function walk(current) {
    const entries = await fs.readdir(current, { withFileTypes: true });
    for (const entry of entries) {
      const full = path.join(current, entry.name);
      if (entry.isDirectory()) {
        await walk(full);
      } else if (entry.isFile() && entry.name.endsWith(".jsonl")) {
        out.push(full);
      }
    }
  }
  await walk(dir);
  out.sort();
  return out;
}

function addCounter(target, record) {
  target.requests += 1;
  target.input += record.input_tokens;
  target.output += record.output_tokens;
  target.cacheCreate += record.cache_creation_input_tokens;
  target.cacheRead += record.cache_read_input_tokens;
  target.withoutCacheRead += record.input_tokens + record.output_tokens + record.cache_creation_input_tokens;
  target.total += record.input_tokens + record.output_tokens + record.cache_creation_input_tokens + record.cache_read_input_tokens;
}

function newCounter() {
  return {
    requests: 0,
    input: 0,
    output: 0,
    cacheCreate: 0,
    cacheRead: 0,
    withoutCacheRead: 0,
    total: 0,
  };
}

async function loadUsageRecords() {
  const files = await walkJsonlFiles(logRoot);
  const seen = new Map();
  let rawUsageRows = 0;
  let parseErrors = 0;

  for (const file of files) {
    const text = await fs.readFile(file, "utf8");
    const lines = text.split(/\r?\n/);
    for (let i = 0; i < lines.length; i += 1) {
      const line = lines[i].trim();
      if (!line) {
        continue;
      }

      let obj;
      try {
        obj = JSON.parse(line);
      } catch {
        parseErrors += 1;
        continue;
      }

      const message = obj?.message;
      const usage = message?.usage;
      if (!message || !usage) {
        continue;
      }

      rawUsageRows += 1;
      const requestId = obj.requestId;
      const messageId = message.id;
      const key = requestId
        ? `request:${requestId}`
        : messageId
          ? `message:${messageId}`
          : `line:${file}:${i + 1}`;

      const values = Object.fromEntries(tokenFields.map((field) => [field, Number(usage[field] || 0)]));
      const total = Object.values(values).reduce((sum, value) => sum + value, 0);
      const record = {
        key,
        cwd: obj.cwd || `[missing cwd] ${path.dirname(file)}`,
        date: String(obj.timestamp || "").slice(0, 10),
        sessionId: obj.sessionId || path.basename(file, ".jsonl"),
        model: message.model || "<unknown>",
        entrypoint: obj.entrypoint || "<unknown>",
        ...values,
      };

      const previous = seen.get(key);
      if (!previous || total > tokenFields.reduce((sum, field) => sum + previous[field], 0)) {
        seen.set(key, record);
      }
    }
  }

  return { files, records: [...seen.values()].filter((record) => record.date), rawUsageRows, parseErrors };
}

function aggregate(records) {
  const byProject = new Map();
  const byProjectDate = new Map();
  const byDate = new Map();

  for (const record of records) {
    const project = record.cwd;
    const projectDateKey = `${project}\u0000${record.date}`;

    if (!byProject.has(project)) {
      byProject.set(project, { project, dates: new Set(), sessions: new Set(), models: new Map(), ...newCounter() });
    }
    const projectBucket = byProject.get(project);
    addCounter(projectBucket, record);
    projectBucket.dates.add(record.date);
    projectBucket.sessions.add(record.sessionId);
    projectBucket.models.set(record.model, (projectBucket.models.get(record.model) || 0) + (
      record.input_tokens + record.output_tokens + record.cache_creation_input_tokens + record.cache_read_input_tokens
    ));

    if (!byProjectDate.has(projectDateKey)) {
      byProjectDate.set(projectDateKey, {
        project,
        date: record.date,
        sessions: new Set(),
        models: new Map(),
        ...newCounter(),
      });
    }
    const projectDateBucket = byProjectDate.get(projectDateKey);
    addCounter(projectDateBucket, record);
    projectDateBucket.sessions.add(record.sessionId);
    projectDateBucket.models.set(record.model, (projectDateBucket.models.get(record.model) || 0) + (
      record.input_tokens + record.output_tokens + record.cache_creation_input_tokens + record.cache_read_input_tokens
    ));

    if (!byDate.has(record.date)) {
      byDate.set(record.date, { date: record.date, sessions: new Set(), ...newCounter() });
    }
    const dateBucket = byDate.get(record.date);
    addCounter(dateBucket, record);
    dateBucket.sessions.add(record.sessionId);
  }

  const projectRows = [...byProject.values()].map((row) => ({
    ...row,
    sessionCount: row.sessions.size,
    dateRange: `${[...row.dates].sort()[0]} ~ ${[...row.dates].sort().at(-1)}`,
    primaryModel: [...row.models.entries()].sort((a, b) => b[1] - a[1])[0]?.[0] || "",
  })).sort((a, b) => b.total - a.total);

  const projectDateRows = [...byProjectDate.values()].map((row) => ({
    ...row,
    sessionCount: row.sessions.size,
    primaryModel: [...row.models.entries()].sort((a, b) => b[1] - a[1])[0]?.[0] || "",
  })).sort((a, b) => a.date.localeCompare(b.date) || b.total - a.total || a.project.localeCompare(b.project));

  const dateRows = [...byDate.values()].map((row) => ({
    ...row,
    sessionCount: row.sessions.size,
  })).sort((a, b) => a.date.localeCompare(b.date));

  return { projectRows, projectDateRows, dateRows };
}

function setHeader(range) {
  range.format = {
    fill: "#1F4E78",
    font: { bold: true, color: "#FFFFFF" },
    wrapText: true,
    horizontalAlignment: "center",
    verticalAlignment: "center",
  };
}

function setTitle(range) {
  range.format = {
    fill: "#EAF2F8",
    font: { bold: true, color: "#17365D", size: 16 },
    verticalAlignment: "center",
  };
}

function setNumericFormat(sheet, rangeAddress) {
  sheet.getRange(rangeAddress).format.numberFormat = "#,##0";
}

function formatTable(sheet, headerRange, dataRange) {
  setHeader(sheet.getRange(headerRange));
  sheet.getRange(dataRange).format.borders = {
    insideHorizontal: { style: "thin", color: "#E5E7EB" },
    top: { style: "thin", color: "#CBD5E1" },
    bottom: { style: "thin", color: "#CBD5E1" },
  };
}

const { files, records, rawUsageRows, parseErrors } = await loadUsageRecords();
const { projectRows, projectDateRows, dateRows } = aggregate(records);

const workbook = Workbook.create();
const summary = workbook.worksheets.add("Summary");
const projects = workbook.worksheets.add("Project Summary");
const detail = workbook.worksheets.add("Project Date Detail");
const pivotTotal = workbook.worksheets.add("Date x Project Total");
const pivotNoCache = workbook.worksheets.add("Date x Project No Cache");

for (const sheet of [summary, projects, detail, pivotTotal, pivotNoCache]) {
  sheet.showGridLines = false;
}

const detailRows = projectDateRows.map((row) => [
  dateObject(row.date),
  row.project,
  row.requests,
  row.sessionCount,
  row.input,
  row.output,
  row.cacheCreate,
  row.cacheRead,
  null,
  null,
  row.primaryModel,
]);
const detailHeaders = [
  "Date",
  "Project cwd",
  "Requests",
  "Sessions",
  "Input",
  "Output",
  "Cache Create",
  "Cache Read",
  "Without Cache Read",
  "Total",
  "Primary Model",
];
detail.getRangeByIndexes(0, 0, 1, detailHeaders.length).values = [detailHeaders];
if (detailRows.length > 0) {
  detail.getRangeByIndexes(1, 0, detailRows.length, detailHeaders.length).values = detailRows;
  detail.getRange(`I2`).formulas = [["=E2+F2+G2"]];
  detail.getRange(`I2:I${detailRows.length + 1}`).fillDown();
  detail.getRange(`J2`).formulas = [["=I2+H2"]];
  detail.getRange(`J2:J${detailRows.length + 1}`).fillDown();
}
detail.freezePanes.freezeRows(1);
detail.freezePanes.freezeColumns(2);
formatTable(detail, `A1:K1`, `A1:K${detailRows.length + 1}`);
detail.getRange(`A2:A${detailRows.length + 1}`).format.numberFormat = "yyyy-mm-dd";
setNumericFormat(detail, `C2:J${detailRows.length + 1}`);
detail.getRange("A:A").format.columnWidth = 13;
detail.getRange("B:B").format.columnWidth = 72;
detail.getRange("C:J").format.columnWidth = 15;
detail.getRange("K:K").format.columnWidth = 22;

const projectHeaders = [
  "Project cwd",
  "Date Range",
  "Requests",
  "Sessions",
  "Input",
  "Output",
  "Cache Create",
  "Cache Read",
  "Without Cache Read",
  "Total",
  "Primary Model",
];
const projectMatrix = projectRows.map((row) => [
  row.project,
  row.dateRange,
  row.requests,
  row.sessionCount,
  row.input,
  row.output,
  row.cacheCreate,
  row.cacheRead,
  row.withoutCacheRead,
  row.total,
  row.primaryModel,
]);
projects.getRangeByIndexes(0, 0, 1, projectHeaders.length).values = [projectHeaders];
projects.getRangeByIndexes(1, 0, projectMatrix.length, projectHeaders.length).values = projectMatrix;
projects.freezePanes.freezeRows(1);
projects.freezePanes.freezeColumns(1);
formatTable(projects, `A1:K1`, `A1:K${projectMatrix.length + 1}`);
setNumericFormat(projects, `C2:J${projectMatrix.length + 1}`);
projects.getRange("A:A").format.columnWidth = 78;
projects.getRange("B:B").format.columnWidth = 24;
projects.getRange("C:J").format.columnWidth = 15;
projects.getRange("K:K").format.columnWidth = 22;

const projectNames = projectRows.map((row) => row.project);
const dates = dateRows.map((row) => row.date);
const detailEnd = detailRows.length + 1;

function buildPivot(sheet, valueColumn, title) {
  sheet.getRange("A1").values = [[title]];
  sheet.getRange(`A1:${excelCol(projectNames.length + 1)}1`).merge();
  setTitle(sheet.getRange(`A1:${excelCol(projectNames.length + 1)}1`));
  sheet.getRange("A3").values = [["Date"]];
  sheet.getRangeByIndexes(2, 1, 1, projectNames.length).values = [projectNames];
  sheet.getRangeByIndexes(3, 0, dates.length, 1).values = dates.map((date) => [dateObject(date)]);
  for (let row = 0; row < dates.length; row += 1) {
    const excelRow = row + 4;
    const formulas = projectNames.map((project, col) => {
      const excelColName = excelCol(col + 1);
      return `=SUMIFS('Project Date Detail'!$${valueColumn}$2:$${valueColumn}$${detailEnd},'Project Date Detail'!$A$2:$A$${detailEnd},$A${excelRow},'Project Date Detail'!$B$2:$B$${detailEnd},${excelColName}$3)`;
    });
    sheet.getRangeByIndexes(row + 3, 1, 1, projectNames.length).formulas = [formulas];
  }
  sheet.getRange(`A3:${excelCol(projectNames.length)}3`).format = {
    fill: "#1F4E78",
    font: { bold: true, color: "#FFFFFF" },
    wrapText: true,
    verticalAlignment: "center",
  };
  sheet.getRange(`A4:A${dates.length + 3}`).format.numberFormat = "yyyy-mm-dd";
  sheet.getRangeByIndexes(3, 1, dates.length, projectNames.length).format.numberFormat = "#,##0";
  sheet.freezePanes.freezeRows(3);
  sheet.freezePanes.freezeColumns(1);
  sheet.getRange("A:A").format.columnWidth = 13;
  sheet.getRangeByIndexes(0, 1, dates.length + 3, projectNames.length).format.columnWidth = 18;
  sheet.getRange(`A3:${excelCol(projectNames.length)}${dates.length + 3}`).format.borders = {
    insideHorizontal: { style: "thin", color: "#E5E7EB" },
    insideVertical: { style: "thin", color: "#E5E7EB" },
    top: { style: "thin", color: "#CBD5E1" },
    bottom: { style: "thin", color: "#CBD5E1" },
  };
}

buildPivot(pivotTotal, "J", "Daily Tokens by Project (Including Cache Read)");
buildPivot(pivotNoCache, "I", "Daily Tokens by Project (Without Cache Read)");

summary.getRange("A1").values = [["Claude Token Usage by Project and Date"]];
summary.getRange("A1:H1").merge();
setTitle(summary.getRange("A1:H1"));
summary.getRange("A3:B10").values = [
  ["Log root", logRoot],
  ["Generated at", new Date().toISOString().replace("T", " ").slice(0, 19)],
  ["JSONL files", files.length],
  ["Raw usage rows", rawUsageRows],
  ["Unique usage records", records.length],
  ["Parse errors", parseErrors],
  ["Projects / cwd", projectRows.length],
  ["Date range", `${dates[0] || ""} ~ ${dates.at(-1) || ""}`],
];
summary.getRange("B4").format.numberFormat = "@";
summary.getRange("B5:B9").format.numberFormat = "#,##0";
summary.getRange("B3").format.columnWidth = 82;
summary.getRange("A3:A10").format = { fill: "#EEF2F7", font: { bold: true } };
summary.getRange("B4:B10").format.horizontalAlignment = "right";
summary.getRange("B4:B8").format.horizontalAlignment = "right";
summary.getRange("B4:B8").format.numberFormat = "#,##0";
summary.getRange("B5:B9").format.font = { bold: true };
summary.getRange("B5:B9").format.fill = "#F8FAFC";
summary.getRange("B5:B9").format.borders = { preset: "outside", style: "thin", color: "#CBD5E1" };
summary.getRange("B5:B9").format.numberFormat = "#,##0";
summary.getRange("B5:B9").format.horizontalAlignment = "right";
summary.getRange("B5:B9").format.verticalAlignment = "center";
summary.getRange("B5:B9").format.wrapText = false;
summary.getRange("B3").format.horizontalAlignment = "left";
summary.getRange("B4").format.horizontalAlignment = "left";
summary.getRange("B10").format.horizontalAlignment = "left";
summary.getRange("B5:B9").format.numberFormat = "#,##0";
summary.getRange("B5:B9").format.rowHeight = 22;
summary.getRange("A12:B18").values = [
  ["Metric", "Value"],
  ["Input", null],
  ["Output", null],
  ["Cache Create", null],
  ["Cache Read", null],
  ["Without Cache Read", null],
  ["Total", null],
];
summary.getRange("B13:B18").formulas = [
  [`=SUM('Project Date Detail'!E2:E${detailEnd})`],
  [`=SUM('Project Date Detail'!F2:F${detailEnd})`],
  [`=SUM('Project Date Detail'!G2:G${detailEnd})`],
  [`=SUM('Project Date Detail'!H2:H${detailEnd})`],
  [`=SUM('Project Date Detail'!I2:I${detailEnd})`],
  [`=SUM('Project Date Detail'!J2:J${detailEnd})`],
];
setHeader(summary.getRange("A12:B12"));
summary.getRange("A13:A18").format = { fill: "#EEF2F7", font: { bold: true } };
summary.getRange("B13:B18").format.numberFormat = "#,##0";
summary.getRange("B13:B18").format = { fill: "#F8FAFC", font: { bold: true } };
summary.getRange("A12:B18").format.borders = {
  insideHorizontal: { style: "thin", color: "#E5E7EB" },
  top: { style: "thin", color: "#CBD5E1" },
  bottom: { style: "thin", color: "#CBD5E1" },
};

summary.getRange("D12:H12").values = [["Top Projects", "Requests", "Without Cache Read", "Total", "Primary Model"]];
const topRows = projectRows.slice(0, 10).map((row) => [
  row.project,
  row.requests,
  row.withoutCacheRead,
  row.total,
  row.primaryModel,
]);
summary.getRangeByIndexes(12, 3, topRows.length, 5).values = topRows;
setHeader(summary.getRange("D12:H12"));
summary.getRange(`D13:H${topRows.length + 12}`).format.borders = {
  insideHorizontal: { style: "thin", color: "#E5E7EB" },
  top: { style: "thin", color: "#CBD5E1" },
  bottom: { style: "thin", color: "#CBD5E1" },
};
summary.getRange(`E13:G${topRows.length + 12}`).format.numberFormat = "#,##0";
summary.getRange("D:D").format.columnWidth = 70;
summary.getRange("E:G").format.columnWidth = 16;
summary.getRange("H:H").format.columnWidth = 22;
summary.getRange("A:A").format.columnWidth = 22;
summary.getRange("B:B").format.columnWidth = 34;
summary.freezePanes.freezeRows(12);

const summaryInspect = await workbook.inspect({
  kind: "table",
  sheetId: "Summary",
  range: "A1:H22",
  include: "values,formulas",
  tableMaxRows: 24,
  tableMaxCols: 8,
  maxChars: 6000,
});
console.log(summaryInspect.ndjson);

const errorScan = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 300 },
  summary: "final formula error scan",
  maxChars: 4000,
});
console.log(errorScan.ndjson);

const previewSheets = [
  ["summary_preview.png", "Summary"],
  ["project_summary_preview.png", "Project Summary"],
  ["project_date_detail_preview.png", "Project Date Detail"],
  ["pivot_total_preview.png", "Date x Project Total"],
  ["pivot_no_cache_preview.png", "Date x Project No Cache"],
];
for (const [fileName, sheetName] of previewSheets) {
  const preview = await workbook.render({
    sheetName,
    autoCrop: "all",
    scale: 1,
    format: "png",
  });
  await fs.writeFile(path.join(outputDir, fileName), new Uint8Array(await preview.arrayBuffer()));
}

await fs.mkdir(outputDir, { recursive: true });
const xlsx = await SpreadsheetFile.exportXlsx(workbook);
await xlsx.save(outputPath);

console.log(JSON.stringify({
  outputPath,
  logRoot,
  files: files.length,
  rawUsageRows,
  uniqueUsageRecords: records.length,
  projects: projectRows.length,
  projectDateRows: projectDateRows.length,
  dateRows: dateRows.length,
}));
