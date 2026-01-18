let selectedIds = new Set();

function updateTrashUI() {
    const tools = document.getElementById('selection-tools');
    const countDisplay = document.getElementById('selectedCountDisplay');
    const bar = document.getElementById('bulkBar');
    const bulkCount = document.getElementById('bulkCount');

    if (selectedIds.size > 0) {
        if (tools) tools.style.display = 'block';
        if (countDisplay) countDisplay.innerText = `${selectedIds.size} selected`;

        if (bar) bar.style.display = 'flex';
        if (bulkCount) bulkCount.innerText = `${selectedIds.size} SELECTED`;
    } else {
        if (tools) tools.style.display = 'none';
        if (bar) bar.style.display = 'none';
    }
}

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
    updateTrashUI();
}

function selectAllTrash() {
    const allCircles = document.querySelectorAll('.check-circle');
    allCircles.forEach(circle => {
        const id = circle.getAttribute('onclick').match(/'([^']+)'/)[1];
        const card = document.getElementById(`file-${id}`);

        selectedIds.add(id);
        circle.classList.add('checked');
        card.classList.add('selected');
    });
    updateTrashUI();
}

function deselectAllTrash() {
    selectedIds.clear();
    document.querySelectorAll('.check-circle').forEach(c => c.classList.remove('checked'));
    document.querySelectorAll('.file-item').forEach(f => f.classList.remove('selected'));
    updateTrashUI();
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