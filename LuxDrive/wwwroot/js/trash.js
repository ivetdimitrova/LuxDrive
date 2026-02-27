// Използваме Set, за да съхраняваме уникални ID-та на избраните файлове
let selectedIds = new Set();

// Функция за обновяване на интерфейса (UI) според броя избрани елементи
function updateTrashUI() {
    const tools = document.getElementById('selection-tools');
    const countDisplay = document.getElementById('selectedCountDisplay');
    const bar = document.getElementById('bulkBar');
    const bulkCount = document.getElementById('bulkCount');

    if (selectedIds.size > 0) {
        // Ако има избрани елементи, показваме инструментите за изтриване/възстановяване
        if (tools) tools.style.display = 'block';
        if (countDisplay) countDisplay.innerText = `${selectedIds.size} selected`;

        // Показваме лентата за групови действия
        if (bar) bar.style.display = 'flex';
        if (bulkCount) bulkCount.innerText = `${selectedIds.size} SELECTED`;
    } else {
        // Ако нищо не е избрано, скриваме лентата и инструментите
        if (tools) tools.style.display = 'none';
        if (bar) bar.style.display = 'none';
    }
}

// Функция за избиране или отмаркиране на конкретен файл
function toggleSelect(circle, id) {
    const card = document.getElementById(`file-${id}`);
    if (selectedIds.has(id)) {
        // Ако вече е избран, го премахваме от списъка и махаме стиловете
        selectedIds.delete(id);
        circle.classList.remove('checked');
        card.classList.remove('selected');
    } else {
        // Ако не е избран, го добавяме и маркираме визуално
        selectedIds.add(id);
        circle.classList.add('checked');
        card.classList.add('selected');
    }
    updateTrashUI(); // Обновяваме брояча на екрана
}

// Функция за избиране на всички файлове в кошчето наведнъж
function selectAllTrash() {
    const allCircles = document.querySelectorAll('.check-circle');
    allCircles.forEach(circle => {
        // Извличаме ID-то на файла от атрибута 'onclick' чрез регулярен израз
        const match = circle.getAttribute('onclick').match(/'([^']+)'/);
        if (match) {
            const id = match[1];
            const card = document.getElementById(`file-${id}`);
            selectedIds.add(id); // Добавяме в списъка
            circle.classList.add('checked');
            if (card) card.classList.add('selected');
        }
    });
    updateTrashUI();
}

// Функция за изчистване на целия избор
function deselectAllTrash() {
    selectedIds.clear(); // Изпразваме Set-а с ID-та
    document.querySelectorAll('.check-circle').forEach(c => c.classList.remove('checked'));
    document.querySelectorAll('.file-item').forEach(f => f.classList.remove('selected'));
    updateTrashUI();
}

// Функция за изпращане на избраните файлове към сървъра (за изтриване или възстановяване)
function submitBulk(action) {
    if (selectedIds.size === 0) return; // Ако няма нищо избрано, не правим нищо

    // Ако действието включва изтриване, искаме потвърждение от потребителя
    if (action.includes('Delete') && !confirm("Permanently delete selected items?")) return;

    const form = document.getElementById('bulkActionForm');
    const container = document.getElementById('hiddenInputsContainer');

    container.innerHTML = ''; // Изчистваме старите скрити полета
    form.action = `/File/${action}`; // Променяме адреса на формата спрямо действието

    // За всяко избрано ID създаваме скрито input поле, за да го изпратим към сървъра
    selectedIds.forEach(id => {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = 'ids'; // Сървърът ще получи масив от ID-та
        input.value = id;
        container.appendChild(input);
    });

    form.submit(); // Изпращаме формата
}