function toggleMenu(button) {
    const card = button.closest('.lux-card');
    const menu = card.querySelector('.dropdown-menu');

    const isVisible = menu.classList.contains('show');

    document.querySelectorAll('.dropdown-menu.show')
        .forEach(m => m.classList.remove('show'));

    if (!isVisible) {
        menu.classList.add('show');
    }
}

document.addEventListener('click', function (e) {
    if (!e.target.closest('.lux-card')) {
        document.querySelectorAll('.dropdown-menu.show')
            .forEach(m => m.classList.remove('show'));
    }
});

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

function deleteFile(id) {
    if (!confirm('Наистина ли искаш да изтриеш този файл?')) {
        return;
    }

    const form = document.getElementById('deleteForm');
    document.getElementById('deleteFileId').value = id;

    form.submit();
}
