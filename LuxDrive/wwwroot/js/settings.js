document.addEventListener("DOMContentLoaded", function () {
    var container = document.querySelector('.settings-container');
    var activeTabName = container ? container.getAttribute('data-active-tab') : 'profile';

    var btn = document.getElementById('btn-' + activeTabName);
    if (btn) btn.click();

    var toastContainer = document.getElementById('force-toast-container');
    if (toastContainer) {
        setTimeout(function () {
            toastContainer.style.opacity = '0';
            setTimeout(() => { toastContainer.style.display = 'none'; }, 500);
        }, 5000);
    }

    function restrictToLetters(e) {
        e.target.value = e.target.value.replace(/[^a-zA-Zа-яА-Я\s\-]/g, '');
    }

    ['firstNameInput', 'lastNameInput', 'newCardName'].forEach(id => {
        var el = document.getElementById(id);
        if (el) el.addEventListener('input', restrictToLetters);
    });

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
            if (value.length >= 2) {
                value = value.substring(0, 2) + '/' + value.substring(2, 4);
            }
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

function handleImageUpload(event) {
    const file = event.target.files[0];
    if (file) {
        const reader = new FileReader();
        reader.onload = function (e) {
            const preview = document.getElementById('profile-preview');
            preview.style.opacity = '0';
            setTimeout(() => {
                preview.src = e.target.result;
                preview.style.opacity = '1';
                document.getElementById('removePhotoFlag').value = "false";
            }, 300);
        }
        reader.readAsDataURL(file);
    }
}

function markForRemoval() {
    const preview = document.getElementById('profile-preview');
    preview.style.opacity = '0';
    setTimeout(() => {
        preview.src = '/images/default-avatar.png';
        preview.style.opacity = '1';
        document.getElementById('removePhotoFlag').value = "true";
        var fileInput = document.getElementById('imageInput');
        if (fileInput) fileInput.value = "";
    }, 300);
}