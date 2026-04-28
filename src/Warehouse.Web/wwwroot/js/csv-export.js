// Client-side CSV export from a table by id
document.getElementById('csv-export')?.addEventListener('click', () => {
  const table = document.getElementById('stock-table');
  if (!table) return;

  const rows = [];
  // headers
  const headers = [...table.querySelectorAll('thead th')].map(th => th.textContent.trim());
  rows.push(headers);
  // body rows
  table.querySelectorAll('tbody tr').forEach(tr => {
    const cells = [...tr.querySelectorAll('td')].map(td => td.textContent.trim().replace(/\s+/g, ' '));
    if (cells.length === headers.length) rows.push(cells);
  });

  const csv = rows
    .map(r => r.map(cell => {
      const escaped = cell.replace(/"/g, '""');
      return /[",\n]/.test(escaped) ? `"${escaped}"` : escaped;
    }).join(','))
    .join('\n');

  const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `stock-report-${new Date().toISOString().slice(0,10)}.csv`;
  a.click();
  URL.revokeObjectURL(url);
});
