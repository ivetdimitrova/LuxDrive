// Когато цялата страница се зареди напълно, изпълняваме този код:
document.addEventListener("DOMContentLoaded", function () {

    // Вземаме нужните ни елементи от HTML-а чрез техните ID-та и класове:
    const contactForm = document.getElementById('contact-form');
    const successMessage = document.getElementById('success-message');
    const sendBtn = document.getElementById('send-btn');
    const contactHeader = document.querySelector('.contact-header');

    // Проверяваме дали формата изобщо съществува на тази страница:
    if (contactForm) {
        // При изпращане на формата правим следното (събитие 'submit'):
        contactForm.addEventListener('submit', function (e) {
            e.preventDefault(); // Спираме стандартното презареждане на страницата

            // Проверка за валидация, ако ползваме jQuery (дали полетата са попълнени правилно)
            if (typeof jQuery !== 'undefined' && !jQuery(contactForm).valid()) {
                return false;
            }

            // Запазваме оригиналния текст на бутона, за да го върнем по-късно
            const originalText = sendBtn.innerText;
            // Показваме "спинър" и текст, че данните се обработват в момента
            sendBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...';
            sendBtn.style.opacity = '0.8';
            sendBtn.style.pointerEvents = 'none'; // Забраняваме повторни кликове

            // Събираме данните от полетата на формата
            const formData = new FormData(contactForm);

            // Изпращаме данните към сървъра по асинхронен път (без презареждане)
            fetch(contactForm.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest' // Указваме, че това е AJAX заявка
                }
            })
                .then(response => {
                    // Ако сървърът отговори успешно 
                    if (response.ok) {
                        contactForm.style.display = 'none'; // Скриваме формата
                        if (contactHeader) contactHeader.style.display = 'none'; // Скриваме заглавието
                        successMessage.style.display = 'block'; // Показваме съобщението за успех
                        contactForm.reset(); // Изчистваме полетата
                    } else {
                        // Ако има проблем със сървъра, преминаваме към грешка
                        throw new Error('Server validation failed');
                    }
                })
                .catch(error => {
                    // Ако нещо се обърка (няма нет или грешка в сървъра), уведомяваме потребителя
                    alert("Something went wrong. Please check your input.");
                    console.error(error);
                })
                .finally(() => {
                    // Независимо дали е успешно или не, връщаме бутона в нормално състояние
                    sendBtn.innerHTML = originalText;
                    sendBtn.style.opacity = '1';
                    sendBtn.style.pointerEvents = 'auto';
                });
        });
    }
});