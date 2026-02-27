// Стартираме с показване на всички файлове веднага щом страницата се зареди
document.addEventListener("DOMContentLoaded", () => {
    filterFiles(null, 'all');
});

// Филтриране на файловете според техния тип (напр. 'image', 'document' и т.н.)
function filterFiles(e, type) {
    if (e) {
        // Управляваме визуално активния бутон за филтриране
        document.querySelectorAll('.filter-btn,.nav-item').forEach(x => x.classList.remove('active'));
        e.currentTarget.classList.add('active');
    }

    let visible = 0;

    // Скриваме или показваме файловете на база техния data-type атрибут
    document.querySelectorAll('.file-item').forEach(item => {
        const show = type === 'all' || item.dataset.type === type;
        item.style.display = show ? 'block' : 'none';
        if (show) visible++;
    });

    // Ако няма файлове за показване, извеждаме "празно състояние" (Empty State)
    const empty = document.getElementById('empty-state');
    if (empty) empty.style.display = visible ? 'none' : 'block';
}

// Търсене в реално време (Live Search) по име на файл
function searchFiles(text) {
    text = text.toLowerCase();
    document.querySelectorAll('.file-item').forEach(item => {
        const name = item.querySelector('h3').innerText.toLowerCase();
        item.style.display = name.includes(text) ? 'block' : 'none';
    });
}

// Използваме обект Set за съхранение на уникални ID-та на избраните файлове
const selection = new Set();

// Превключвател за избор/отмяна на избора на единичен файл
function toggleSelect(el, id) {
    el.classList.toggle('checked');
    const fileItem = document.getElementById(`file-${id}`);

    if (el.classList.contains('checked')) {
        selection.add(id);
        if (fileItem) fileItem.classList.add('selected');
    } else {
        selection.delete(id);
        if (fileItem) fileItem.classList.remove('selected');
    }

    updateSelectionUI(); // Обновяваме брояча и лентата с инструменти
}

// Селектиране на всички видими файлове едновременно
function selectAllFiles() {
    // Вземаме само файловете, които не са скрити от филтъра или търсачката
    const allVisibleFiles = document.querySelectorAll('.file-item:not([style*="display: none"])');
    allVisibleFiles.forEach(item => {
        const id = item.id.replace('file-', '');
        const checkCircle = item.querySelector('.check-circle');

        if (!selection.has(id)) {
            selection.add(id);
            if (checkCircle) checkCircle.classList.add('checked');
            item.classList.add('selected');
        }
    });
    updateSelectionUI();
}

// Изчистване на цялата селекция
function clearSelection() {
    selection.clear();
    document.querySelectorAll('.check-circle').forEach(el => el.classList.remove('checked'));
    document.querySelectorAll('.file-item').forEach(el => el.classList.remove('selected'));
    updateSelectionUI();
}

// Динамично обновяване на лентата за групови действия (Bulk Actions UI)
function updateSelectionUI() {
    const bulkBar = document.getElementById('bulkBar');
    const selectionTools = document.getElementById('selection-tools');
    const countDisplay = document.getElementById('selectedCountDisplay');
    const bulkCountDisplay = document.getElementById('bulkCount');

    const count = selection.size;
    const text = `${count} selected`;

    if (count > 0) {
        // Показваме инструментите, ако има поне един избран файл
        if (bulkBar) bulkBar.classList.add('active');
        if (selectionTools) selectionTools.style.display = 'block';

        if (countDisplay) countDisplay.innerText = text;
        if (bulkCountDisplay) bulkCountDisplay.innerText = text;
    } else {
        // Скриваме инструментите, ако селекцията е празна
        if (bulkBar) bulkBar.classList.remove('active');
        if (selectionTools) selectionTools.style.display = 'none';

        if (countDisplay) countDisplay.innerText = "0 selected";
        if (bulkCountDisplay) bulkCountDisplay.innerText = "0 Selected";
    }
}

// Зареждане на списък с приятели чрез AJAX (без презареждане на страницата)
async function showFriends(event, element) {
    event.preventDefault();
    const url = element.getAttribute('href');

    try {
        const response = await fetch(url);
        const html = await response.text();

        const container = document.getElementById('modalContainer');
        container.innerHTML = html;
        container.style.display = 'flex';

        // Скриваме страничната лента за по-добър фокус върху модала
        const sidebar = document.querySelector('.sidebar');
        if (sidebar) sidebar.style.display = 'none';
    } catch (err) {
        console.error("Error loading friends:", err);
    }
}

// Зареждане на модален прозорец за споделяне на файл
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

// Функции за затваряне на модалните прозорци
function closeShareModal() {
    const container = document.getElementById('shareContainer');

    if (container) {
        container.style.display = 'none';
        container.innerHTML = ''; // Изчистваме DOM-а
    }

    const sidebar = document.querySelector('.sidebar');
    if (sidebar) {
        sidebar.style.display = 'flex'; // Връщаме страничната лента
    }
}

function closeFriendsTab() {
    const container = document.getElementById('modalContainer');
    container.style.display = 'none';
    container.innerHTML = '';

    const sidebar = document.querySelector('.sidebar');
    if (sidebar) sidebar.style.display = 'flex';
}

// Управление на табове (Tabs) в интерфейса
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

// Групово изтегляне на файлове (генериране на ZIP архив)
async function downloadSelected() {
    const ids = Array.from(selection);

    if (ids.length === 0) return;

    // Ако е само един файл, използваме стандартния контролер за сваляне
    if (ids.length === 1) {
        window.location.href = `/File/Download/${ids[0]}`;
        return;
    }

    try {
        // Изпращаме масив от ID-та към сървъра, който ще върне ZIP файл
        const response = await fetch('/File/DownloadMultiple', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(ids)
        });

        if (response.ok) {
            // Конвертираме отговора в Blob (двоични данни) и инициираме сваляне
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

// Алтернативна функция за групово изтегляне с изчистване на селекцията след успех
async function bulkDownload() {
    const ids = Array.from(selection);
    if (ids.length === 0) return;

    const clearSelectionUI = () => {
        selection.clear();
        document.querySelectorAll('.check-circle.checked').forEach(el => {
            el.classList.remove('checked');
        });
        updateSelectionUI();
    };

    if (ids.length === 1) {
        window.location.href = `/File/Download?id=${ids[0]}`;
        clearSelectionUI();
        return;
    }

    try {
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

            clearSelectionUI();
        } else {
            alert("An error occurred during bulk download..");
        }
    } catch (error) {
        console.error("Download error:", error);
    }
}

// Преименуване на файл с проверка за невалидни данни
async function renameFile(id, oldName) {
    const newName = prompt("Enter a new file name:", oldName);

    if (newName === null) {
        return; // Потребителят е натиснал Cancel
    }

    const trimmedName = newName.trim();

    // Базови валидации
    if (trimmedName === "") {
        alert("File name cannot be empty.");
        return;
    }

    if (trimmedName.length > 100) {
        alert("File name is too long. Please use up to 100 characters.");
        return;
    }

    if (trimmedName === oldName.trim()) {
        return; // Няма промяна в името
    }

    try {
        const formData = new FormData();
        formData.append('id', id);
        formData.append('newName', trimmedName);

        const response = await fetch('/File/Rename', {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            // Обновяваме името в DOM-а директно, без да презареждаме страницата
            const fileItem = document.getElementById(`file-${id}`);
            if (fileItem) {
                const h3 = fileItem.querySelector('h3');
                if (h3) h3.innerText = trimmedName;

                const renameBtn = fileItem.querySelector('button[title="Rename"]');
                if (renameBtn) {
                    renameBtn.setAttribute('onclick', `renameFile('${id}', '${trimmedName}')`);
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

// Изтриване на единичен файл с CSRF защита
async function deleteFile(id) {
    if (!confirm("Are you sure you want to move this file to the trash?")) {
        return;
    }

    try {
        // Вземаме Anti-Forgery Token-а за сигурност на POST заявката
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        const formData = new FormData();
        formData.append('id', id);
        formData.append('__RequestVerificationToken', token);

        const response = await fetch('/File/Delete', {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            // Плавно скриване на файла преди презареждане
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

// Групово изтриване на файлове
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

// Подготовка на интерфейса за групово споделяне на файлове
async function bulkShare() {
    const ids = Array.from(selection);
    if (ids.length === 0) return;

    try {
        const formData = new FormData();
        formData.append("fileId", ids[0]); // Пращаме първото ID, за да заредим логиката в бекенда

        const response = await fetch('/api/friends/load-share-list', {
            method: 'POST',
            body: formData
        });

        const html = await response.text();

        const container = document.getElementById('shareContainer');
        container.innerHTML = html;
        container.style.display = 'flex';

        const sidebar = document.querySelector('.sidebar');
        if (sidebar) sidebar.style.display = 'none';

        // Маркираме модала, че се намира в режим "Групово споделяне"
        container.dataset.mode = 'bulk';
    } catch (err) {
        console.error("Error loading sharing modal:", err);
    }
}

// Логика за обработка на формата за споделяне (Единично и Групово)
async function handleShareSubmit(event, form) {
    event.preventDefault();

    const container = document.getElementById('shareContainer');
    const isBulkMode = container.dataset.mode === 'bulk';

    const receiverElement = form.querySelector('[name="ReceiverId"]');
    const receiverId = receiverElement ? receiverElement.value : null;

    if (!receiverId || receiverId.trim() === "") {
        alert("Please select a friend from the list before sharing.");
        return;
    }

    if (isBulkMode) {
        // Логика за споделяне на множество файлове
        const ids = Array.from(selection);

        if (ids.length === 0) {
            alert("Please select at least one file to share.");
            return;
        }

        try {
            const response = await fetch(`/File/ShareMultiple?receiverId=${receiverId}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(ids)
            });

            if (response.ok) {
                // Изчистваме селекцията при успех
                selection.clear();
                updateSelectionUI();
                closeShareModal();

                document.querySelectorAll('.check-circle.checked').forEach(el => {
                    el.classList.remove('checked');
                });

                container.dataset.mode = ''; // Ресетваме режима на модала
                alert("Files were shared successfully!");
            } else {
                alert("An error occurred while mass sharing.");
            }
        } catch (error) {
            console.error("Error sending files:", error);
            alert("Server connection error.");
        }
    }
    else {
        // Ако не е в Bulk режим, изпращаме стандартната форма
        form.submit();
    }
}