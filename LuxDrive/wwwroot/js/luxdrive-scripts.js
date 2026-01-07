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
        switch (type) {
            case 'image': emptyText.innerText = "No photos uploaded"; break;
            case 'video': emptyText.innerText = "No videos uploaded"; break;
            case 'document': emptyText.innerText = "No documents uploaded"; break;
            case 'archive': emptyText.innerText = "No archives uploaded"; break;
            default: emptyText.innerText = "No files uploaded"; break;
        }
    } else {
        emptyState.style.display = 'none';
    }
}

function searchFiles(txt) {
    txt = txt.toLowerCase();
    document.querySelectorAll('.file-item').forEach(item => { item.style.display = item.querySelector('h3').innerText.toLowerCase().includes(txt) ? 'block' : 'none'; });
}

const selection = new Set();
function toggleSelect(el, id) {
    el.classList.toggle('checked');
    if (el.classList.contains('checked')) selection.add(id); else selection.delete(id);
    const bar = document.getElementById('bulkBar');
    if (selection.size > 0) { bar.classList.add('active'); document.getElementById('bulkCount').innerText = selection.size + " Selected"; } else { bar.classList.remove('active'); }
}

function deleteFile(id) { if (confirm("Delete?")) { document.getElementById('delId').value = id; document.getElementById('delForm').submit(); } }

async function bulkDelete() {
    if (!confirm(`Delete ${selection.size} files?`)) return;
    await fetch('/file/DeleteMultiple', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(Array.from(selection)) });
    location.reload();
}

function bulkDownload() {
    if (selection.size === 0) return;

    if (confirm(`Download ${selection.size} files?`)) {
        selection.forEach(id => {
            const item = document.getElementById(`file-${id}`);
            if (item) {
                const url = item.getAttribute('data-download-url');
                if (url) {
                    const a = document.createElement('a');
                    a.href = url;
                    a.download = ''; 
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                }
            }
        });
    }
}

let isBulk = false;
function openShare(id) { isBulk = false; document.getElementById('shareId').value = id; document.getElementById('shareModal').style.display = 'block'; loadShareList(); }
function bulkShare() { if (!selection.size) return; isBulk = true; document.getElementById('shareModal').style.display = 'block'; loadShareList(); }
function closeShare() { document.getElementById('shareModal').style.display = 'none'; }

async function loadShareList() {
    const sel = document.getElementById('shareSelect'); sel.innerHTML = '<option>Loading...</option>';
    const r = await fetch('/api/friends/list'); const d = await r.json();
    sel.innerHTML = d.map(f => `<option value="${f.id}">${f.username}</option>`).join('');
}

async function confirmShare() {
    const rid = document.getElementById('shareSelect').value;
    if (isBulk) await fetch(`/file/ShareMultiple?receiverId=${rid}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(Array.from(selection)) });
    else await fetch(`/file/share?fileId=${document.getElementById('shareId').value}&receiverId=${rid}`, { method: 'POST' });
    alert("Shared!"); closeShare(); location.reload();
}

function openHub() { document.getElementById('socialHub').style.display = 'block'; setTab('connections'); }
function closeHub() { document.getElementById('socialHub').style.display = 'none'; }

function setTab(name) {
    document.getElementById('view-connections').style.display = name === 'connections' ? 'block' : 'none';
    document.getElementById('view-add').style.display = name === 'add' ? 'block' : 'none';
    document.getElementById('view-requests').style.display = name === 'requests' ? 'block' : 'none';
    document.querySelectorAll('.tab-link').forEach(t => t.classList.remove('active'));
    if (name === 'connections') { document.querySelectorAll('.tab-link')[0].classList.add('active'); loadConnections(); }
    if (name === 'add') { document.querySelectorAll('.tab-link')[1].classList.add('active'); loadSent(); }
    if (name === 'requests') { document.querySelectorAll('.tab-link')[2].classList.add('active'); loadReqs(); }
}

async function loadConnections() {
    const c = document.getElementById('connectionsList');
    c.innerHTML = '<div style="text-align:center; color:#666;">Loading...</div>';
    try {
        const r = await fetch('/api/friends/list');
        const d = await r.json();

        if (!d.length) {
            c.innerHTML = `
            <div style="text-align:center; padding:40px 20px; color:#666;">
                <i class="fas fa-user-plus" style="font-size:3rem; margin-bottom:15px; opacity:0.3; color:var(--gold);"></i>
                <p style="margin:0; font-size: 0.9rem;">No friends yet. Add someone!</p>
            </div>`;
            return;
        }

        c.innerHTML = d.map(f => `<div class="connection-card"><div class="user-avatar">${f.username[0]}<div class="status-dot"></div></div><div class="user-details"><span class="user-name">${f.username}</span><span class="user-email">${f.email}</span></div><div class="card-tools"><button class="tool-btn del" onclick="removeFriend('${f.id}')"><i class="fas fa-user-minus"></i></button></div></div>`).join('');
    } catch (e) { c.innerHTML = 'Error.'; }
}

async function loadReqs() {
    const c = document.getElementById('requestsList');
    const r = await fetch('/api/friends/pending');
    const d = await r.json();

    if (!d.length) {
        c.innerHTML = `
        <div style="text-align:center; padding:30px 20px; color:#555;">
            <i class="fas fa-inbox" style="font-size:2.5rem; margin-bottom:15px; opacity:0.3;"></i>
            <p style="margin:0; font-size: 0.9rem;">There are no incoming friend requests.</p>
        </div>`;
        return;
    }

    c.innerHTML = d.map(q => `
        <div class="req-card">
            <div class="user-avatar" style="width:35px; height:35px; font-size:0.9rem;">${q.senderName[0]}</div>
            <div style="margin-left:10px; color:#fff;">${q.senderName}</div>
            <div class="req-actions">
                <button class="btn-yes" onclick="accept('${q.id}')">Accept</button>
                <button class="btn-no" onclick="reject('${q.id}')">Remove</button>
            </div>
        </div>`).join('');
}

async function loadSent() {
    const c = document.getElementById('sentList');
    const r = await fetch('/api/friends/sent');
    const d = await r.json();

    c.innerHTML = d.length ? d.map(r => `<div style="padding:10px; border-bottom:1px solid #222; display:flex; justify-content:space-between;"><span style="color:#aaa">${r.receiverName}</span><span style="color:var(--gold); font-size:0.8rem;">Pending</span></div>`).join('') :
        `<div style="text-align:center; padding:20px; color:#555;">
        <i class="fas fa-paper-plane" style="font-size:1.5rem; margin-bottom:10px; opacity:0.4; display:block;"></i>
        <span style="font-size: 0.85rem;">There are no pending acceptance requests submitted.</span>
    </div>`;
}

async function sendInvite() {
    const email = document.getElementById('inviteEmail').value;
    if (!email) return;
    const s = await fetch(`/api/friends/search?email=${encodeURIComponent(email)}`);
    if (!s.ok) return alert("User not found.");
    const u = await s.json();
    await fetch(`/api/friends/request?receiverId=${u.id}`, { method: 'POST' });
    alert("Invite Sent!"); document.getElementById('inviteEmail').value = ''; loadSent();
}

async function removeFriend(id) { if (confirm("Remove connection?")) { await fetch(`/api/friends/remove?friendId=${id}`, { method: 'POST' }); loadConnections(); } }
async function accept(id) { await fetch(`/api/friends/accept?requestId=${id}`, { method: 'POST' }); loadReqs(); }

async function reject(id) {
    if (confirm("Reject friend request?")) {
        try {
            const response = await fetch(`/api/friends/reject?requestId=${id}`, { method: 'POST' });
            if (response.ok) {
                loadReqs();
            } else {
                alert("Could not remove request.");
            }
        } catch (e) {
            console.error(e);
        }
    }
}

async function renameFile(id, currentName) {
    const newName = prompt("Enter new name:", currentName);
    if (newName && newName.trim() !== "" && newName !== currentName) {
        try {
            const response = await fetch(`/File/Rename?id=${id}&newName=${encodeURIComponent(newName)}`, {
                method: 'POST'
            });
            if (response.ok) {
                location.reload();
            } else {
                alert("Failed to rename file.");
            }
        } catch (e) {
            console.error(e);
            alert("Error occurred.");
        }
    }
}