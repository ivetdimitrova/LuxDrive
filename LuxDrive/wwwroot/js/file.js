document.addEventListener("DOMContentLoaded", () => {
    filterFiles(null, 'all');
});

function filterFiles(e, type) {
    if (e) {
        document.querySelectorAll('.filter-btn,.nav-item').forEach(x => x.classList.remove('active'));
        e.currentTarget.classList.add('active');
    }

    let visible = 0;

    document.querySelectorAll('.file-item').forEach(item => {
        const show = type === 'all' || item.dataset.type === type;
        item.style.display = show ? 'block' : 'none';
        if (show) visible++;
    });

    const empty = document.getElementById('empty-state');
    if (empty) empty.style.display = visible ? 'none' : 'block';
}

function searchFiles(text) {
    text = text.toLowerCase();
    document.querySelectorAll('.file-item').forEach(item => {
        const name = item.querySelector('h3').innerText.toLowerCase();
        item.style.display = name.includes(text) ? 'block' : 'none';
    });
}

const selection = new Set();

function toggleSelect(el, id) {
    el.classList.toggle('checked');

    if (el.classList.contains('checked')) {
        selection.add(id);
    } else {
        selection.delete(id);
    }

    updateSelectionUI();
}

function updateSelectionUI() {
    const bar = document.getElementById('bulkBar');
    const count = document.getElementById('bulkCount');

    if (selection.size > 0) {
        bar.classList.add('active');
        count.innerText = `${selection.size} Selected`;
    } else {
        bar.classList.remove('active');
    }
}

function openHub() {
    document.getElementById('socialHub')?.classList.add('active');
}

function closeHub() {
    document.getElementById('socialHub')?.classList.remove('active');
}
