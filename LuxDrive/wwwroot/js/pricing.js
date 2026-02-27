document.addEventListener("DOMContentLoaded", function () {

    // 1. Ефект за появяване на елементи при скролване
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            // Ако елементът е влязъл в изгледа (поне 15%)
            if (entry.isIntersecting) {
                entry.target.classList.add('show'); // Добавяме CSS клас за анимация
                observer.unobserve(entry.target); // Спираме да го следим, за да не се хаби ресурс
            }
        });
    }, { threshold: 0.15 }); // Прагът от 0.15 означава 15% видимост

    // Пускаме наблюдателя за всички елементи с клас .feature
    document.querySelectorAll('.feature').forEach(f => observer.observe(f));

    // 2. Проследяване на мишката в Hero секцията
    const hero = document.querySelector('.hero');
    if (hero) {
        hero.addEventListener('mousemove', (e) => {
            const rect = hero.getBoundingClientRect(); // Вземаме точните координати на секцията
            // Изчисляваме позицията на мишката спрямо началото на Hero блока
            hero.style.setProperty('--mouse-x', `${e.clientX - rect.left}px`);
            hero.style.setProperty('--mouse-y', `${e.clientY - rect.top}px`);
        });
    }

    // 3. Система за летящи частици (Particles)
    const canvas = document.getElementById('particles-canvas');
    if (canvas) {
        const ctx = canvas.getContext('2d');
        let width, height;

        // Напасваме размера на канваса спрямо екрана
        function resize() {
            width = canvas.width = canvas.offsetWidth;
            height = canvas.height = canvas.offsetHeight;
        }
        window.addEventListener('resize', resize);
        resize();

        const particles = [];
        // Клас, който описва поведението на всяка отделна частица
        class Particle {
            constructor() { this.reset(); }

            // Първоначално създаване или рестартиране на точка
            reset() {
                this.x = Math.random() * width;
                this.y = Math.random() * height;
                this.size = Math.random() * 2 + 0.5; // Случаен размер
                this.speedX = (Math.random() - 0.5) * 0.5; // Скорост по X
                this.speedY = (Math.random() - 0.5) * 0.5; // Скорост по Y
                this.alpha = Math.random() * 0.5 + 0.1; // Начална прозрачност
                this.fadingOut = Math.random() > 0.5; // Дали започва с избледняване
            }

            // Логика за движение и "дишане" (fade effect)
            update() {
                this.x += this.speedX;
                this.y += this.speedY;

                // Правим плавно пулсиране на прозрачността
                if (this.fadingOut) {
                    this.alpha -= 0.005;
                    if (this.alpha <= 0) { this.fadingOut = false; this.reset(); }
                } else {
                    this.alpha += 0.005;
                    if (this.alpha >= 0.8) this.fadingOut = true;
                }
            }

            // Рисуваме точката върху канваса
            draw() {
                ctx.fillStyle = `rgba(198, 166, 100, ${this.alpha})`; // Златист цвят
                ctx.beginPath();
                ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
                ctx.fill();
            }
        }

        // Създаваме масив от 60 частици
        for (let i = 0; i < 60; i++) particles.push(new Particle());

        // Основен анимационен цикъл
        function animateP() {
            ctx.clearRect(0, 0, width, height); // Изчистваме стария кадър
            particles.forEach(p => {
                p.update(); // Движим
                p.draw();   // Рисуваме
            });
            requestAnimationFrame(animateP); // Искаме следващия кадър от браузъра
        }
        animateP();
    }
});