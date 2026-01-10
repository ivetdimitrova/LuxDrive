document.addEventListener("DOMContentLoaded", function () {

    const alert = document.getElementById('success-alert');

    if (alert) {
        setTimeout(function () {
            alert.style.opacity = '0';
            alert.style.transform = 'translate(-50%, -20px)';

            setTimeout(() => alert.remove(), 500);
        }, 4000);
    }
});