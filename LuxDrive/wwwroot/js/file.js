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

async function showFriends(event, element) {
    event.preventDefault();

    const url = element.getAttribute('href');

    try {
        const response = await fetch(url);
        const html = await response.text();

        const container = document.getElementById('modalContainer');
        container.innerHTML = html;
        container.style.display = 'flex';

        const sidebar = document.querySelector('.sidebar');
        if (sidebar) sidebar.style.display = 'none';
    } catch (err) {
        console.error("Грешка при зареждане на приятели:", err);
    }
}

function closeFriendsTab() {
    const container = document.getElementById('modalContainer');
    container.style.display = 'none';
    container.innerHTML = '';


    const sidebar = document.querySelector('.sidebar');
    if (sidebar) sidebar.style.display = 'flex';
}

    function openTab(tabName) {
        const allTabs = document.querySelectorAll('.tab-content');
        allTabs.forEach(t => {
            t.style.display = 'none';
        });

        const targetTab = document.getElementById(tabName);

        if (targetTab) {
            targetTab.style.display = 'block';
        } else {
            console.error("Не е намерен елемент с ID: " + tabName);
        }

        document.querySelectorAll('.tab-link').forEach(btn => btn.classList.remove('active'));
        const activeBtn = document.querySelector(`button[onclick*="${tabName}"]`);
        if (activeBtn) activeBtn.classList.add('active');
}

async function showShareList(event, element) {
    event.preventDefault();

    const url = element.getAttribute('href');

    try {
        const response = await fetch(url);
        const html = await response.text();

        const container = document.getElementById('shareModal');
        container.innerHTML = html;
        container.style.display = 'flex';

        const sidebar = document.querySelector('.sidebar');
        if (sidebar) sidebar.style.display = 'none';
    } catch (err) {
        console.error("Грешка при зареждане на приятели:", err);
    }
}