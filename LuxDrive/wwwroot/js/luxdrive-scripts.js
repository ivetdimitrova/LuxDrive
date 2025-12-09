// Показване / скриване на менюто с трите точки
function toggleMenu(button) {
    const card = button.closest('.lux-card');
    const menu = card.querySelector('.dropdown-menu');

    const isVisible = menu.classList.contains('show');

    // Скрий всички отворени менюта
    document.querySelectorAll('.dropdown-menu.show')
        .forEach(m => m.classList.remove('show'));

    // Ако текущото беше скрито – покажи го
    if (!isVisible) {
        menu.classList.add('show');
    }
}

// Затваряне на всички менюта при клик извън картата
document.addEventListener('click', function (e) {
    if (!e.target.closest('.lux-card')) {
        document.querySelectorAll('.dropdown-menu.show')
            .forEach(m => m.classList.remove('show'));
    }
});

// Преименуване на файл – prompt + POST към File/Rename
function renameFile(id, currentName) {
    const newName = prompt('Ново име на файла:', currentName);

    if (!newName || newName.trim() === '' || newName.trim() === currentName) {
        return;
    }

    const form = document.getElementById('renameForm');
    document.getElementById('renameFileId').value = id;
    document.getElementById('renameFileNewName').value = newName.trim();

    form.submit();
}

// Изтриване на файл – confirm + POST към File/Delete
function deleteFile(id) {
    if (!confirm('Наистина ли искаш да изтриеш този файл?')) {
        return;
    }

    const form = document.getElementById('deleteForm');
    document.getElementById('deleteFileId').value = id;

    form.submit();
}
