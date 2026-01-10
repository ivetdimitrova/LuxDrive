document.addEventListener("DOMContentLoaded", function () {

    const contactForm = document.getElementById('contact-form');
    const successMessage = document.getElementById('success-message');
    const sendBtn = document.getElementById('send-btn');
    const contactHeader = document.querySelector('.contact-header'); 

    if (contactForm) {
        contactForm.addEventListener('submit', function (e) {
            e.preventDefault();

            const originalText = sendBtn.innerText;
            sendBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...';
            sendBtn.style.opacity = '0.8';
            sendBtn.style.pointerEvents = 'none';

            setTimeout(function () {
                contactForm.style.display = 'none';
                if (contactHeader) contactHeader.style.display = 'none';

                successMessage.style.display = 'block';

                contactForm.reset();
                sendBtn.innerHTML = originalText;
                sendBtn.style.opacity = '1';
                sendBtn.style.pointerEvents = 'auto';
            }, 1500);
        });
    }
});