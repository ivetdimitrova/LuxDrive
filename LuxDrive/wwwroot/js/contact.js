document.addEventListener("DOMContentLoaded", function () {

    const contactForm = document.getElementById('contact-form');
    const successMessage = document.getElementById('success-message');
    const sendBtn = document.getElementById('send-btn');
    const contactHeader = document.querySelector('.contact-header'); 

    if (contactForm) {
        contactForm.addEventListener('submit', function (e) {
            e.preventDefault();

            if (typeof jQuery !== 'undefined' && !jQuery(contactForm).valid()) {
                return false;
            }

            const originalText = sendBtn.innerText;
            sendBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...';
            sendBtn.style.opacity = '0.8';
            sendBtn.style.pointerEvents = 'none';

            const formData = new FormData(contactForm);

            fetch(contactForm.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            })
                .then(response => {
                    if (response.ok) {
                        contactForm.style.display = 'none';
                        if (contactHeader) contactHeader.style.display = 'none';
                        successMessage.style.display = 'block';
                        contactForm.reset();
                    } else {
                        throw new Error('Server validation failed');
                    }
                })
                .catch(error => {
                    alert("Something went wrong. Please check your input.");
                    console.error(error);
                })
                .finally(() => {
                    sendBtn.innerHTML = originalText;
                    sendBtn.style.opacity = '1';
                    sendBtn.style.pointerEvents = 'auto';
                });
        });
    }
});