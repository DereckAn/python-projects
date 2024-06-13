const fs = require("fs");

function alignMarkdownTable(table) {
  const lines = table.trim().split("\n");
  const headerSepIndex = 1; // line with dashes and colons
  const columns = lines
    .map((line) => line.split("|").map((col) => col.trim()))
    .map((row) => row.filter((col) => col !== ""));

  const maxWidths = columns[0].map((_, colIndex) => {
    const columnData = columns
      .filter((_, index) => index !== headerSepIndex) // Exclude headerSepIndex row
      .map((row) => (row[colIndex] ? row[colIndex].length : 0));

    return Math.max(...columnData);
  });

  const formatRow = (row, i) => {
    if (i === headerSepIndex) {
      return row
        .map((col, i) => {
          const leftAlign = col.startsWith(":");
          const rightAlign = col.endsWith(":");
          const dash = "-".repeat(maxWidths[i] + 1);
          if (leftAlign && rightAlign) return `:${dash}:`;
          if (leftAlign) return `:${dash}`;
          if (rightAlign) return `${dash}:`;
          return dash;
        })
        .join(" | ");
    } else {
      return row.map((col, i) => col.padEnd(maxWidths[i])).join(" | ");
    }
  };

  const formattedLines = columns.map(formatRow);

  var finishedLines = formattedLines
    .map((line, i) => (i === headerSepIndex ? `|${line}|` : `| ${line} |`))
    .join("\n");
  const linesArray = finishedLines.split("\n");
  linesArray[headerSepIndex] = linesArray[headerSepIndex]
    .replaceAll(" :", ":")
    .replaceAll(" |", "|");
  finishedLines = linesArray.join("\n");

  return finishedLines;
}

fs.readFile("table.txt", "utf8", (err, data) => {
  if (err) {
    console.error(err);
    return;
  }
  const formattedTable = alignMarkdownTable(data);
  fs.writeFile("table.txt", formattedTable, (err) => {
    if (err) {
      console.error(err);
      return;
    }
    console.log("Table Formatted written to table.txt");
  });
});