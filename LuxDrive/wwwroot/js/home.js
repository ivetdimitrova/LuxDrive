document.addEventListener("DOMContentLoaded", function () {

    // 1. Проследяване на мишката в Hero секцията
    const hero = document.querySelector('.hero');
    hero.addEventListener('mousemove', (e) => {
        const rect = hero.getBoundingClientRect(); // Вземаме размерите и позицията на Hero секцията
        const x = e.clientX - rect.left; // Изчисляваме X позицията на мишката спрямо секцията
        const y = e.clientY - rect.top;  // Изчисляваме Y позицията на мишката спрямо секцията

        // Задаваме тези координати като CSS променливи за динамични стилове
        hero.style.setProperty('--mouse-x', `${x}px`);
        hero.style.setProperty('--mouse-y', `${y}px`);
    });

    // 2. Анимация на облаците (Паралакс и плавно движение)
    const clouds = document.querySelectorAll('.hero-cloud');
    let mouseX = 0, mouseY = 0;
    let targetX = 0, targetY = 0;

    // Следим движението на мишката върху целия документ
    document.addEventListener('mousemove', (e) => {
        // Изчисляваме дестинацията на паралакса (колкото по-далеч е мишката от центъра, толкова повече се движат облаците)
        targetX = (window.innerWidth / 2 - e.pageX) / 20;
        targetY = (window.innerHeight / 2 - e.pageY) / 20;
    });

    function animateClouds() {
        // Линейна интерполация (Lerp) за плавност: 
        // Вместо облаците да "скачат" веднага до мишката, те се придвижват с 5% от дистанцията на всеки кадър
        mouseX += (targetX - mouseX) * 0.05;
        mouseY += (targetY - mouseY) * 0.05;
        const scrollY = window.scrollY;

        clouds.forEach(cloud => {
            const speed = parseFloat(cloud.getAttribute('data-speed')); // Скорост на въртене при скрол
            const depth = parseFloat(cloud.getAttribute('data-depth')); // Дълбочина на движението (3D ефект)

            const rotate = scrollY * speed * 5;
            const yPos = scrollY * depth;
            const xMouse = mouseX * depth;
            const yMouse = mouseY * depth;

            // Добавяме леко "плуване" нагоре-надолу чрез синусова функция
            const time = Date.now() * 0.0005;
            const floatY = Math.sin(time + depth * 10) * 8;

             // Комбинираме всички движения в една трансформация
            cloud.style.transform = `translate3d(${xMouse}px, ${yPos + yMouse + floatY}px, 0) rotate(${rotate}deg)`;
        });
        requestAnimationFrame(animateClouds);
    }
    animateClouds();

    // 3. Система за частици (Particles) върху Canvas
    const canvas = document.getElementById('particles-canvas');
    if (canvas) {
        const ctx = canvas.getContext('2d');
        let width, height;
        let particles = [];

        function resize() {
            width = canvas.width = window.innerWidth;
            height = canvas.height = window.innerHeight;
        }
        window.addEventListener('resize', resize);
        resize();

        class Particle {
            constructor() { this.reset(); }

            reset() {
                this.x = Math.random() * width;
                this.y = Math.random() * height;
                this.size = Math.random() * 2 + 0.5; // Случаен размер на точката
                this.speedX = (Math.random() - 0.5) * 0.5; // Случайна посока наляво/надясно
                this.speedY = (Math.random() - 0.5) * 0.5; // Случайна посока нагоре/надолу
                this.alpha = Math.random() * 0.5 + 0.1; // Начална прозрачност
                this.fadeSpeed = Math.random() * 0.01 + 0.005; // Скорост на "пулсиране"
                this.fadingOut = Math.random() > 0.5;
            }

            update() {
                // Движение на частицата + леко влияние от мишката
                this.x += this.speedX + (mouseX * 0.05 * this.size);
                this.y += this.speedY + (mouseY * 0.05 * this.size);

                // Логика за плавно появяване и изчезване (Fade effect)
                if (this.fadingOut) {
                    this.alpha -= this.fadeSpeed;
                    if (this.alpha <= 0) { this.fadingOut = false; this.reset(); }
                } else {
                    this.alpha += this.fadeSpeed;
                    if (this.alpha >= 0.8) this.fadingOut = true;
                }
            }

            draw() {
                ctx.fillStyle = `rgba(198, 166, 100, ${this.alpha})`; // Златист цвят
                ctx.beginPath();
                ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
                ctx.fill();
            }
        }

        // Генерираме 100 частици
        for (let i = 0; i < 100; i++) particles.push(new Particle());

        function animateParticles() {
            ctx.clearRect(0, 0, width, height); // Изчистваме канваса за новия кадър
            particles.forEach(p => {
                p.update();
                p.draw();
            });
            requestAnimationFrame(animateParticles);
        }
        animateParticles();
    }

    // 4. Показване на елементи при скролване (Intersection Observer)
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                // Когато елементът влезе в полезрението, му добавяме клас 'show' за анимация
                entry.target.classList.add('show');
            }
        });
    }, { threshold: 0.1 }); // 10% от елемента трябва да е видим

    document.querySelectorAll('.feature').forEach(f => observer.observe(f));
});