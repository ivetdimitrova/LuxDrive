document.addEventListener("DOMContentLoaded", function () {
    // Проверяваме кой таб трябва да е активен първоначално (вземаме го от атрибут в HTML)
    var container = document.querySelector('.settings-container');
    var activeTabName = container ? container.getAttribute('data-active-tab') : 'profile';

    // Автоматично кликваме върху бутона на активния таб, за да го отворим
    var btn = document.getElementById('btn-' + activeTabName);
    if (btn) btn.click();

    // Логика за автоматично скриване на известия (Toast notifications) след 5 секунди
    var toastContainer = document.getElementById('force-toast-container');
    if (toastContainer) {
        setTimeout(function () {
            toastContainer.style.opacity = '0'; // Правим го прозрачен
            // След като изчезне визуално, го премахваме напълно от лейаута
            setTimeout(() => { toastContainer.style.display = 'none'; }, 500);
        }, 5000);
    }

    // Функция, която позволява въвеждане само на букви (кирилица и латиница) и тирета
    function restrictToLetters(e) {
        e.target.value = e.target.value.replace(/[^a-zA-Zа-яА-Я\s\-]/g, '');
    }

    // Прилагаме филтъра за букви върху имената
    ['firstNameInput', 'lastNameInput', 'newCardName'].forEach(id => {
        var el = document.getElementById(id);
        if (el) el.addEventListener('input', restrictToLetters);
    });

    // Валидация за телефон – позволява само цифри и символа "+"
    var phoneInput = document.getElementById('phoneInput');
    if (phoneInput) {
        phoneInput.addEventListener('input', function (e) {
            e.target.value = e.target.value.replace(/[^0-9\+]/g, '');
        });
    }

    // Форматиране на номера на картата – добавя интервал на всеки 4 цифри
    var cardNumber = document.getElementById('cardNumber');
    if (cardNumber) {
        cardNumber.addEventListener('input', function (e) {
            let value = e.target.value.replace(/\D/g, ''); // Махаме всичко освен цифрите
            e.target.value = value.replace(/(.{4})/g, '$1 ').trim(); // Групираме по 4
        });
    }

    // Форматиране на датата на изтичане на картата (ММ/ГГ)
    var cardExpiry = document.getElementById('cardExpiry');
    if (cardExpiry) {
        cardExpiry.addEventListener('input', function (e) {
            let value = e.target.value.replace(/\D/g, '');
            if (value.length >= 2) {
                value = value.substring(0, 2) + '/' + value.substring(2, 4);
            }
            e.target.value = value;
        });
    }
    // Валидация за CVC кода (обикновено 3 цифри на гърба на картата)
    var cardCvc = document.getElementById('cardCvc');
    if (cardCvc) {
        cardCvc.addEventListener('input', function (e) {
            // Оставяме само цифрите и ограничаваме дължината до 3 символа
            e.target.value = e.target.value.replace(/\D/g, '').substring(0, 3);
        });
    }
});

// Функция за превключване между табовете (Профил, Сигурност, Плащания)
function openTab(evt, tabName) {
    var i, tabcontent, tablinks;
    // Скриваме съдържанието на всички табове
    tabcontent = document.getElementsByClassName("tab-content");
    for (i = 0; i < tabcontent.length; i++) {
        tabcontent[i].style.display = "none";
    }
    // Премахваме класа 'active' от всички бутони
    tablinks = document.getElementsByClassName("tab-btn");
    for (i = 0; i < tablinks.length; i++) {
        tablinks[i].classList.remove("active");
    }
    // Показваме само избрания таб и маркираме неговия бутон като активен
    document.getElementById(tabName).style.display = "block";
    evt.currentTarget.classList.add("active");
}

// Функция за визуализиране (preview) на качената профилна снимка
function handleImageUpload(event) {
    const file = event.target.files[0];
    if (file) {
        const reader = new FileReader();
        reader.onload = function (e) {
            const preview = document.getElementById('profile-preview');
            // Правим плавен ефект при смяна на снимката
            preview.style.opacity = '0';
            setTimeout(() => {
                preview.src = e.target.result;
                preview.style.opacity = '1';
                // Отбелязваме, че снимката НЕ е маркирана за изтриване
                document.getElementById('removePhotoFlag').value = "false";
            }, 300);
        }
        reader.readAsDataURL(file);
    }
}

// Функция за премахване на снимката и връщане на картинка по подразбиране
function markForRemoval() {
    const preview = document.getElementById('profile-preview');
    preview.style.opacity = '0';
    setTimeout(() => {
        preview.src = '/images/default-avatar.png'; // Път към аватара по подразбиране
        preview.style.opacity = '1';
        // Маркираме флаг за сървъра, че снимката трябва да бъде изтрита от базата данни
        document.getElementById('removePhotoFlag').value = "true";
        var fileInput = document.getElementById('imageInput');
        if (fileInput) fileInput.value = ""; // Изчистваме избрания файл
    }, 300);
}