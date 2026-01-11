document.addEventListener("DOMContentLoaded", function () {
    filterFiles(null, 'all');
});

function filterFiles(e, type) {
    if (e) {
        document.querySelectorAll('.filter-btn, .nav-item').forEach(b => b.classList.remove('active'));
        e.currentTarget.classList.add('active');
    }

    let visibleCount = 0;
    const items = document.querySelectorAll('.file-item');

    items.forEach(item => {
        const match = (type === 'all' || item.dataset.type === type);
        if (match) {
            item.style.display = 'block';
            visibleCount++;
        } else {
            item.style.display = 'none';
        }
    });

    const emptyState = document.getElementById('empty-state');
    const emptyText = document.getElementById('empty-text');

    if (visibleCount === 0) {
        emptyState.style.display = 'block';
        if (emptyText) {
            switch (type) {
                case 'image': emptyText.innerText = "No photos uploaded"; break;
                case 'video': emptyText.innerText = "No videos uploaded"; break;
                case 'document': emptyText.innerText = "No documents uploaded"; break;
                case 'archive': emptyText.innerText = "No archives uploaded"; break;
                default: emptyText.innerText = "No files uploaded"; break;
            }
        }
    } else {
        if (emptyState) emptyState.style.display = 'none';
    }
}

function searchFiles(txt) {
    txt = txt.toLowerCase();
    document.querySelectorAll('.file-item').forEach(item => {
        const fileName = item.querySelector('h3').innerText.toLowerCase();
        item.style.display = fileName.includes(txt) ? 'block' : 'none';
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

    const bar = document.getElementById('bulkBar');
    const countEl = document.getElementById('bulkCount');

    if (selection.size > 0) {
        bar.classList.add('active');
        countEl.innerText = selection.size + " Selected";
    } else {
        bar.classList.remove('active');
    }
}

function deleteFile(id) {
    if (confirm("Are you sure you want to delete this file?")) {
        document.getElementById('delId').value = id;
        document.getElementById('delForm').submit();
    }
}

async function bulkDelete() {
    if (!confirm(`Delete ${selection.size} files?`)) return;
    try {
        const response = await fetch('/file/DeleteMultiple', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(Array.from(selection))
        });
        if (response.ok) location.reload();
    } catch (e) { console.error("Bulk delete failed", e); }
}

function bulkDownload() {
    if (selection.size === 0) return;
    selection.forEach(id => {
        const item = document.getElementById(`file-${id}`);
        if (item) {
            const url = item.getAttribute('data-download-url');
            const a = document.createElement('a');
            a.href = url;
            a.download = '';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
        }
    });
}

let isBulk = false;

function openShare(id) {
    isBulk = false;
    document.getElementById('shareId').value = id;
    document.getElementById('shareModal').style.display = 'block';
    loadShareList();
}

function bulkShare() {
    if (!selection.size) return;
    isBulk = true;
    document.getElementById('shareModal').style.display = 'block';
    loadShareList();
}

function closeShare() {
    document.getElementById('shareModal').style.display = 'none';
}

async function loadShareList() {
    const sel = document.getElementById('shareSelect');
    sel.innerHTML = '<option value="" disabled selected>Loading friends...</option>';
    try {
        const r = await fetch('/api/friends/list');
        const d = await r.json();
        if (d.length === 0) {
            sel.innerHTML = '<option value="" disabled>No friends found</option>';
            return;
        }
        let options = '<option value="" disabled selected>-- Select a Friend --</option>';
        options += d.map(f => `<option value="${f.id}">${f.firstName ? f.firstName + ' ' + f.lastName : f.username}</option>`).join('');
        sel.innerHTML = options;
    } catch (e) {
        sel.innerHTML = '<option value="" disabled>Error loading friends</option>';
    }
}

async function confirmShare() {
    const rid = document.getElementById('shareSelect').value;
    const shareIdInput = document.getElementById('shareId');

    if (!rid || rid === "") {
        alert("Please select a friend first.");
        return;
    }

    try {
        let response;
        if (isBulk) {
            response = await fetch(`/file/ShareMultiple?receiverId=${rid}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(Array.from(selection))
            });
        } else {
            response = await fetch(`/file/share?fileId=${shareIdInput.value}&receiverId=${rid}`, { method: 'POST' });
        }

        if (response.ok) {
            alert("Shared successfully!");
            closeShare();

            shareIdInput.value = "";
            if (isBulk) {
                selection.clear();
                document.getElementById('bulkBar').classList.remove('active');
                document.querySelectorAll('.check-circle').forEach(c => c.classList.remove('checked'));
                isBulk = false;
            }
        } else {
            alert("Error: Could not share file.");
        }
    } catch (e) {
        alert("Server error.");
    }
}

function openHub() {
    document.getElementById('socialHub').style.display = 'block';
    setTab('connections');
}

function closeHub() {
    document.getElementById('socialHub').style.display = 'none';
}

function setTab(name) {
    const views = ['view-connections', 'view-add', 'view-requests'];
    views.forEach(v => document.getElementById(v).style.display = v.includes(name) ? 'block' : 'none');

    document.querySelectorAll('.tab-link').forEach(t => t.classList.remove('active'));

    if (name === 'connections') { loadConnections(); document.querySelectorAll('.tab-link')[0].classList.add('active'); }
    if (name === 'add') { loadSent(); document.querySelectorAll('.tab-link')[1].classList.add('active'); }
    if (name === 'requests') { loadReqs(); document.querySelectorAll('.tab-link')[2].classList.add('active'); }
}

async function loadConnections() {
    const c = document.getElementById('connectionsList');
    try {
        const r = await fetch('/api/friends/list');
        const d = await r.json();
        if (!d.length) {
            c.innerHTML = '<p style="text-align:center;color:#666;padding:20px;">No friends added yet.</p>';
            return;
        }
        c.innerHTML = d.map(f => `
            <div class="connection-card">
                <div class="user-avatar">${f.username[0]}</div>
                <div class="user-details">
                    <span class="user-name">${f.firstName ? f.firstName + ' ' + f.lastName : f.username}</span>
                    <span class="user-email">${f.email}</span>
                </div>
                <button class="tool-btn del" onclick="removeFriend('${f.id}')"><i class="fas fa-user-minus"></i></button>
            </div>`).join('');
    } catch (e) { c.innerHTML = 'Error loading list.'; }
}

async function sendInvite() {
    const emailInput = document.getElementById('inviteEmail');
    const email = emailInput.value;
    if (!email) return alert("Please enter an email.");
    try {
        const s = await fetch(`/api/friends/search?email=${encodeURIComponent(email)}`);
        if (!s.ok) return alert("User not found.");
        const u = await s.json();
        const response = await fetch(`/api/friends/request?receiverId=${u.id}`, { method: 'POST' });
        if (response.ok) {
            alert("Friend request sent!");
            emailInput.value = '';
            loadSent();
        }
    } catch (e) { alert("Error sending request."); }
}

async function loadReqs() {
    const c = document.getElementById('requestsList');
    const r = await fetch('/api/friends/pending');
    const d = await r.json();
    if (!d.length) { c.innerHTML = '<p style="text-align:center;color:#666;padding:20px;">No incoming requests.</p>'; return; }
    c.innerHTML = d.map(q => `
        <div class="req-card">
            <div class="user-avatar">${q.senderName ? q.senderName[0] : '?'}</div>
            <div style="margin-left:10px;">${q.senderName}</div>
            <div class="req-actions">
                <button class="btn-yes" onclick="accept('${q.id}')">Accept</button>
                <button class="btn-no" onclick="reject('${q.id}')">Reject</button>
            </div>
        </div>`).join('');
}

async function loadSent() {
    const c = document.getElementById('sentList');
    const r = await fetch('/api/friends/sent');
    const d = await r.json();
    c.innerHTML = d.length ? d.map(r => `
        <div style="display:flex; justify-content:space-between; padding:12px; border-bottom:1px solid #222;">
            <span>${r.receiverName}</span><span style="color:var(--gold)">Pending</span>
        </div>`).join('') : '<p style="text-align:center;color:#555;padding:10px;">No sent requests.</p>';
}

async function removeFriend(id) { if (confirm("Remove this friend?")) { await fetch(`/api/friends/remove?friendId=${id}`, { method: 'POST' }); loadConnections(); } }
async function accept(id) { await fetch(`/api/friends/accept?requestId=${id}`, { method: 'POST' }); loadReqs(); }
async function reject(id) { if (confirm("Reject this request?")) { await fetch(`/api/friends/reject?requestId=${id}`, { method: 'POST' }); loadReqs(); } }

async function renameFile(id, currentName) {
    const newName = prompt("Enter new filename:", currentName);
    if (newName && newName.trim() !== "" && newName !== currentName) {
        const response = await fetch(`/File/Rename?id=${id}&newName=${encodeURIComponent(newName)}`, { method: 'POST' });
        if (response.ok) location.reload();
    }
}