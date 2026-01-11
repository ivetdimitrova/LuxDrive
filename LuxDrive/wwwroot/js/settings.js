document.addEventListener("DOMContentLoaded", function () {
    var container = document.querySelector('.settings-container');
    var activeTabName = container ? container.getAttribute('data-active-tab') : 'profile';

    var btn = document.getElementById('btn-' + activeTabName);
    if (btn) btn.click();

    var toastContainer = document.getElementById('force-toast-container');
    if (toastContainer) {
        setTimeout(function () {
            toastContainer.style.display = 'none';
        }, 5000);
    }

    function restrictNameInput(e) {
        e.target.value = e.target.value.replace(/[^a-zA-Zа-яА-Я\s\-]/g, '');
    }

    var firstNameInput = document.getElementById('firstNameInput');
    var lastNameInput = document.getElementById('lastNameInput');

    if (firstNameInput) firstNameInput.addEventListener('input', restrictNameInput);
    if (lastNameInput) lastNameInput.addEventListener('input', restrictNameInput);

    var phoneInput = document.getElementById('phoneInput');
    if (phoneInput) {
        phoneInput.addEventListener('input', function (e) {
            e.target.value = e.target.value.replace(/[^0-9\+]/g, '');
        });
    }

    var cardNumber = document.getElementById('cardNumber');
    if (cardNumber) {
        cardNumber.addEventListener('input', function (e) {
            let value = e.target.value.replace(/\D/g, '');
            e.target.value = value.replace(/(.{4})/g, '$1 ').trim();
        });
    }

    var cardExpiry = document.getElementById('cardExpiry');
    if (cardExpiry) {
        cardExpiry.addEventListener('input', function (e) {
            let value = e.target.value.replace(/\D/g, '');
            if (value.length >= 2) { value = value.substring(0, 2) + '/' + value.substring(2, 4); }
            e.target.value = value;
        });
    }

    var cardCvc = document.getElementById('cardCvc');
    if (cardCvc) {
        cardCvc.addEventListener('input', function (e) {
            e.target.value = e.target.value.replace(/\D/g, '').substring(0, 3);
        });
    }
});

function openTab(evt, tabName) {
    var i, tabcontent, tablinks;

    tabcontent = document.getElementsByClassName("tab-content");
    for (i = 0; i < tabcontent.length; i++) {
        tabcontent[i].style.display = "none";
    }

    tablinks = document.getElementsByClassName("tab-btn");
    for (i = 0; i < tablinks.length; i++) {
        tablinks[i].classList.remove("active");
    }

    document.getElementById(tabName).style.display = "block";
    evt.currentTarget.classList.add("active");
}