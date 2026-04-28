// Dynamic add/remove rows in the movement form
(function () {
  const body = document.getElementById('items-body');
  const tpl  = document.getElementById('row-template');
  let nextIndex = body.querySelectorAll('.item-row').length;

  document.getElementById('add-row').addEventListener('click', () => {
    const html = tpl.innerHTML.replace(/__I__/g, nextIndex++);
    const tmp = document.createElement('tbody');
    tmp.innerHTML = html.trim();
    body.appendChild(tmp.firstElementChild);
  });

  body.addEventListener('click', (e) => {
    if (!e.target.classList.contains('remove-row')) return;
    if (body.querySelectorAll('.item-row').length === 1) return; // keep at least one
    e.target.closest('tr').remove();
    // Reindex remaining rows
    body.querySelectorAll('.item-row').forEach((row, i) => {
      row.querySelectorAll('[name]').forEach(el => {
        el.name = el.name.replace(/Items\[\d+\]/, `Items[${i}]`);
      });
    });
    nextIndex = body.querySelectorAll('.item-row').length;
  });
})();
