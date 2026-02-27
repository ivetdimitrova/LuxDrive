let generatedCode = ""; // Глобална променлива за съхранение на генерирания код

// Изпълняваме кода, когато HTML документът е напълно зареден
document.addEventListener("DOMContentLoaded", function () {
    const initialAlert = document.getElementById('success-alert');
    if (initialAlert) {
        // Ако има съобщение при зареждане, го скриваме автоматично след 4 секунди
        setTimeout(() => hideToast(initialAlert), 4000);
    }
});

// Функция за създаване и показване на модерен (Lux) "Toast" (известие)
function showLuxToast(title, message, type = 'success') {
    let existingToast = document.getElementById('success-alert');
    if (existingToast) existingToast.remove(); // Махаме старо известие, ако има такова

    const toast = document.createElement('div');
    toast.id = 'success-alert';
    toast.className = `lux-alert lux-alert-${type}`; // Динамичен клас спрямо типа (успех или грешка)
    const icon = type === 'success' ? '✨' : '⚠️';

    toast.innerHTML = `
        <div class="alert-icon">${icon}</div>
        <div class="alert-content">
            <h4>${title}</h4>
            <p>${message}</p>
        </div>
    `;

    document.body.appendChild(toast); // Добавяме го в края на бодито

    // Малък таймер, за да се задейства CSS анимацията за появяване
    setTimeout(() => {
        toast.style.display = 'flex';
        toast.style.opacity = '1';
        toast.style.transform = 'translate(-50%, 0)';
    }, 10);

    // Ако съобщението съдържа кода за достъп, го показваме за 10 сек, иначе за 5 сек
    const displayTime = message.includes(generatedCode) && generatedCode !== "" ? 10000 : 5000;
    setTimeout(() => hideToast(toast), displayTime);
}

// Функция за плавно скриване и премахване на известието
function hideToast(element) {
    if (!element) return;
    element.style.opacity = '0';
    element.style.transform = 'translate(-50%, -20px)'; // Анимация нагоре при изчезване
    setTimeout(() => element.remove(), 500); // Премахваме от DOM след края на анимацията
}

// Отваряне на модалния прозорец за забравена парола
document.getElementById('forgot-password').onclick = function (e) {
    e.preventDefault();
    document.getElementById('forgotPasswordModal').style.display = 'flex';
};

// Затваряне на модалния прозорец
function closeForgotModal() {
    document.getElementById('forgotPasswordModal').style.display = 'none';
}

// СТЪПКА 1: Проверка на имейл и генериране на код
async function simulateSendCode() {
    const email = document.getElementById('reset-email').value;
    if (!email) {
        showLuxToast("Input Error", "Please enter an email address.", "error");
        return;
    }

    // Вземаме CSRF токена за сигурност при заявката
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    try {
        const response = await fetch('?handler=CheckEmail', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': token
            },
            body: `email=${encodeURIComponent(email)}`
        });

        const result = await response.json();

        if (result.exists) {
            // Генерираме произволен 4-цифрен код
            generatedCode = Math.floor(1000 + Math.random() * 9000).toString();
            showLuxToast("Verification", `Email found! Your reset code is: ${generatedCode}`, "success");

            // Преминаваме към следващата стъпка на формата
            document.getElementById('reset-step-1').style.display = 'none';
            document.getElementById('reset-step-2').style.display = 'block';
        } else {
            showLuxToast("Access Denied", result.error, "error");
        }
    } catch (err) {
        showLuxToast("Server Error", "Could not connect to database.", "error");
    }
}

// СТЪПКА 2: Сравняване на въведения от потребителя код с генерирания
function simulateVerifyCode() {
    const input = document.getElementById('reset-code-input').value;
    if (input === generatedCode) {
        showLuxToast("Success", "Code verified! Enter your new password.", "success");
        document.getElementById('reset-step-2').style.display = 'none';
        document.getElementById('reset-step-3').style.display = 'block';
    } else {
        showLuxToast("Security", "The verification code is incorrect.", "error");
    }
}

// СТЪПКА 3: Записване на новата парола
async function simulateFinishReset() {
    const p1 = document.getElementById('new-password').value;
    const p2 = document.getElementById('confirm-password').value;
    const email = document.getElementById('reset-email').value;

    if (p1 !== p2) {
        showLuxToast("Validation", "Passwords do not match.", "error");
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const formData = new URLSearchParams();
    formData.append('email', email);
    formData.append('newPassword', p1);
    formData.append('__RequestVerificationToken', token);

    try {
        const response = await fetch('?handler=SimulateReset', {
            method: 'POST',
            body: formData,
            headers: { 'RequestVerificationToken': token }
        });

        const result = await response.json();

        if (result.success) {
            showLuxToast("System", "Password updated successfully! Reloading...", "success");
            setTimeout(() => location.reload(), 2000); // Презареждаме за вход с новата парола
        } else {
            showLuxToast("Identity Error", result.error, "error");
        }
    } catch (err) {
        showLuxToast("Fatal Error", "Failed to update password.", "error");
    }
}