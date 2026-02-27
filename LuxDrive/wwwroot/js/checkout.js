// При изпращане на формата правим следното (събитие 'submit'):
document.getElementById('payment-form').addEventListener('submit', function (e) {
    const form = e.target; // Вземаме самата форма, която е изпратена

    // Ако на страницата има заредена jQuery библиотека, правим проверка
    if (typeof jQuery !== 'undefined') {
        // Проверяваме дали формата е валидна според правилата на jQuery Validation
        if (!jQuery(form).valid()) {
            e.preventDefault(); // Спираме изпращането, ако има грешни полета
            return;
        }
    }

    // Вземаме бутона за плащане, за да променим външния му вид
    const btn = document.getElementById('pay-btn');
    // Слагаме му "спинър" (въртящо се кръгче) и текст, за да знае потребителят, че се чака
    btn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...';
    // Правим го полупрозрачен и му спираме кликовете, за да не се натисне два пъти
    btn.style.opacity = '0.7';
    btn.style.pointerEvents = 'none';
});

// Логика за автоматично форматиране на номера на картата
document.getElementById('cardNumber').addEventListener('input', function (e) {
    // Махаме всичко, което НЕ е цифра (букви, символи и т.н.)
    let value = e.target.value.replace(/\D/g, '');

    let formattedValue = '';
    // Минаваме през цифрите една по една
    for (let i = 0; i < value.length; i++) {
        // На всеки 4 цифри добавяме интервал за по-лесно четене
        if (i > 0 && i % 4 === 0) {
            formattedValue += ' ';
        }
        formattedValue += value[i];
    }
    // Заместваме написаното в полето с форматирания текст
    e.target.value = formattedValue;
});

// Логика за датата на изтичане (ММ/ГГ)
document.getElementById('expiry').addEventListener('input', function (e) {
    // Оставяме само цифрите
    let value = e.target.value.replace(/\D/g, '');

    // Ако имаме поне 2 цифри (месеца), добавяме наклонената черта автоматично
    if (value.length >= 2) {
        e.target.value = value.substring(0, 2) + '/' + value.substring(2, 4);
    } else {
        e.target.value = value;
    }

    // Проверка: ако потребителят трие назад и стигне до чертата, я махаме автоматично
    if (e.inputType === 'deleteContentBackward' && this.value.length === 2) {
        this.value = this.value.substring(0, 1);
    }
});

// Логика за CVC кода
document.getElementById('cvc').addEventListener('input', function (e) {
    // Махаме символите и ограничаваме дължината до точно 3 цифри
    e.target.value = e.target.value.replace(/\D/g, '').substring(0, 3);
});