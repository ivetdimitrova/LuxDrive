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
        console.error("Error loading friends:", err);
    }
}

async function showShare(event, element) {
    event.preventDefault();

 
    const url = element.action;

  
    const formData = new FormData(element);

    try {
       
        const response = await fetch(url, {
            method: 'POST',
            body: formData
        });

        const html = await response.text();

        const container = document.getElementById('shareContainer');
        container.innerHTML = html;
        container.style.display = 'flex';

        const sidebar = document.querySelector('.sidebar');
        if (sidebar) sidebar.style.display = 'none';
    } catch (err) {
        console.error("Error loading share:", err);
    }
}

function closeShareModal() {
    const container = document.getElementById('shareContainer');
  
    if (container) {
        container.style.display = 'none';
        container.innerHTML = '';
    }

    const sidebar = document.querySelector('.sidebar');
    if (sidebar) {
        sidebar.style.display = 'flex';
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
            console.error("No element found with ID: " + tabName);
        }

        document.querySelectorAll('.tab-link').forEach(btn => btn.classList.remove('active'));
        const activeBtn = document.querySelector(`button[onclick*="${tabName}"]`);
        if (activeBtn) activeBtn.classList.add('active');
}
async function downloadSelected() {
    const ids = Array.from(selection);

    if (ids.length === 0) return;

    if (ids.length === 1) {
        window.location.href = `/File/Download/${ids[0]}`;
        return;
    }

    try {
        const response = await fetch('/File/DownloadMultiple', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(ids)
        });

        if (response.ok) {
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = "LuxDrive_Files.zip";
            document.body.appendChild(a);
            a.click();
            a.remove();
        } else {
            alert('Download error.');
        }
    } catch (error) {
        console.error('Download error:', error);
    }
}
async function bulkDownload() {
    const ids = Array.from(selection); 
    if (ids.length === 0) return;

    if (ids.length === 1) {
        window.location.href = `/File/Download?id=${ids[0]}`;
        return;
    }

    const response = await fetch('/File/DownloadMultiple', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(ids)
    });

    if (response.ok) {
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = "LuxDrive_Archive.zip";
        a.click();
    }
}
async function renameFile(id, oldName) {
    const newName = prompt("Enter a new file name:", oldName);

    if (newName === null || newName.trim() === "" || newName === oldName) {
        return;
    }

    try {
        const formData = new FormData();
        formData.append('id', id);
        formData.append('newName', newName.trim());

        const response = await fetch('/File/Rename', {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            const fileItem = document.getElementById(`file-${id}`);
            if (fileItem) {
                const h3 = fileItem.querySelector('h3');
                if (h3) h3.innerText = newName.trim();

                const renameBtn = fileItem.querySelector('button[title="Rename"]');
                if (renameBtn) {
                    renameBtn.setAttribute('onclick', `renameFile('${id}', '${newName.trim()}')`);
                }
            }
            alert("File renamed successfully.");
        } else {
            const errorText = await response.text();
            alert("Rename error: " + (errorText || "A problem occurred."));
        }
    } catch (error) {
        console.error("Rename error:", error);
        alert("Server connection error.");
    }
}
async function deleteFile(id) {
    if (!confirm("Are you sure you want to move this file to the trash?")) {
        return;
    }

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        const formData = new FormData();
        formData.append('id', id);
        formData.append('__RequestVerificationToken', token);

        const response = await fetch('/File/Delete', {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            const fileItem = document.getElementById(`file-${id}`);
            if (fileItem) {
                fileItem.style.opacity = '0';
                fileItem.style.transform = 'scale(0.9)';
                setTimeout(() => location.reload(), 300);
            } else {
                location.reload();
            }
        } else {
            alert("Error deleting file.");
        }
    } catch (error) {
        console.error("Delete error:", error);
        alert("There was a problem connecting to the server..");
    }
}
async function bulkDelete() {
    const ids = Array.from(selection); 

    if (ids.length === 0) return;

    if (!confirm(`Are you sure you want to delete ${ids.length} selected files?`)) {
        return;
    }

    try {
        const response = await fetch('/File/DeleteMultiple', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
               
            },
            body: JSON.stringify(ids)
        });

        if (response.ok) {
            alert("Files moved to the trash.");
            location.reload();
        } else {
            alert("Bulk delete error.");
        }
    } catch (error) {
        console.error("Bulk delete error:", error);
    }
}

function showAlert(message) {
    var element = document.getElementById("customAlert");
    if (element) {
        
        element.classList.remove("custom-alert-hidden");
        element.classList.add("custom-alert-visible"); 

      
        setTimeout(function () {
            element.classList.add("custom-alert-hidden");
            element.classList.remove("custom-alert-visible");
        }, 1000);
    }
}