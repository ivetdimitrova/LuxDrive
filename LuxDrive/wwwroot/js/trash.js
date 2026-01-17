let selectedIds = new Set();

function toggleSelect(circle, id) {
    const card = document.getElementById(`file-${id}`);
    if (selectedIds.has(id)) {
        selectedIds.delete(id);
        circle.classList.remove('checked');
        card.classList.remove('selected');
    } else {
        selectedIds.add(id);
        circle.classList.add('checked');
        card.classList.add('selected');
    }
    const bar = document.getElementById('bulkBar');
    bar.style.display = selectedIds.size > 0 ? 'flex' : 'none';
    document.getElementById('bulkCount').innerText = selectedIds.size + " SELECTED";
}

async function submitBulk(action) {
    if (selectedIds.size === 0) return;
    if (action.includes('Permanent') && !confirm("Permanently delete selected items?")) return;

    const idsArray = Array.from(selectedIds);
    const response = await fetch(`/File/${action}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: JSON.stringify(idsArray)
    });

    if (response.ok) {
        window.location.reload();
    } else {
        alert("Error processing request.");
    }
}