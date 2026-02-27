document.addEventListener("DOMContentLoaded", function () {

    // 1. Анимация при скролване (Intersection Observer)
    // Следим кога картите с характеристики (feature-cards) стават видими на екрана
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                // Когато картата влезе в изгледа (поне 10%), добавяме клас 'show', за да се появи с ефект
                entry.target.classList.add('show');
                // Спираме да наблюдаваме този елемент, след като веднъж се е появил
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1 }); // Праг от 10% видимост

    // Пускаме наблюдателя за всяка карта
    document.querySelectorAll('.feature-card').forEach(card => observer.observe(card));

    // 2. Интерактивен ефект в Hero секцията
    const hero = document.querySelector('.hero');
    if (hero) {
        hero.addEventListener('mousemove', (e) => {
            const rect = hero.getBoundingClientRect();
            // Пресмятаме позицията на мишката спрямо Hero елемента
            // Задаваме CSS променливи, които могат да се използват за светлинни ефекти в стиловете
            hero.style.setProperty('--mouse-x', `${e.clientX - rect.left}px`);
            hero.style.setProperty('--mouse-y', `${e.clientY - rect.top}px`);
        });
    }

    // 3. Паралакс ефект с облаци
    const clouds = document.querySelectorAll('.hero-cloud');
    let mouseX = 0, mouseY = 0, targetX = 0, targetY = 0;

    // Следим движението на мишката за паралакс ефекта
    document.addEventListener('mousemove', (e) => {
        // Изчисляваме разстоянието от центъра на екрана
        targetX = (window.innerWidth / 2 - e.pageX) / 20;
        targetY = (window.innerHeight / 2 - e.pageY) / 20;
    });

    function animateClouds() {
        // Правим движението "плавно" (Lerp - Linear Interpolation)
        mouseX += (targetX - mouseX) * 0.05;
        mouseY += (targetY - mouseY) * 0.05;
        const scrollY = window.scrollY;

        clouds.forEach(cloud => {
            const speed = parseFloat(cloud.getAttribute('data-speed')); // Скорост на въртене
            const depth = parseFloat(cloud.getAttribute('data-depth')); // Дълбочина (колко силно се движи)

            // Движим облака на база мишката, скрола и неговата дълбочина (за 3D ефект)
            cloud.style.transform = `translate3d(${mouseX * depth}px, ${scrollY * depth + mouseY * depth}px, 0) rotate(${scrollY * speed * 5}deg)`;
        });
        requestAnimationFrame(animateClouds); // Пускаме следващия кадър на анимацията
    }
    animateClouds();

    // 4. Система за частици (Particles) на Canvas
    const canvas = document.getElementById('particles-canvas');
    if (canvas) {
        const ctx = canvas.getContext('2d');
        let width, height, particles = [];

        // Функция за напасване на размера на канваса при промяна на прозореца
        function resize() {
            width = canvas.width = window.innerWidth;
            height = canvas.height = window.innerHeight;
        }
        window.addEventListener('resize', resize);
        resize();

        // Клас, който дефинира всяка отделна частица
        class Particle {
            constructor() { this.reset(); }

            // Първоначални настройки (позиция, размер, скорост, прозрачност)
            reset() {
                this.x = Math.random() * width;
                this.y = Math.random() * height;
                this.size = Math.random() * 2 + 0.5;
                this.speedX = (Math.random() - 0.5) * 0.5;
                this.speedY = (Math.random() - 0.5) * 0.5;
                this.alpha = Math.random() * 0.5 + 0.1;
                this.fadingOut = Math.random() > 0.5; // Дали в момента избледнява
            }

            // Обновяване на състоянието на частицата (движение и "дишане" на прозрачността)
            update() {
                this.x += this.speedX;
                this.y += this.speedY;

                // Ефект на пулсиране (избледняване и появяване)
                if (this.fadingOut) {
                    this.alpha -= 0.01;
                    if (this.alpha <= 0) { this.fadingOut = false; this.reset(); }
                } else {
                    this.alpha += 0.01;
                    if (this.alpha >= 0.8) this.fadingOut = true;
                }
            }

            // Рисуване на частицата върху канваса
            draw() {
                ctx.fillStyle = `rgba(198, 166, 100, ${this.alpha})`; // Златист цвят
                ctx.beginPath();
                ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
                ctx.fill();
            }
        }

        // Създаваме 60 частици
        for (let i = 0; i < 60; i++) particles.push(new Particle());

        // Основен цикъл за анимация на частиците
        function animateP() {
            ctx.clearRect(0, 0, width, height); // Изчистваме стария кадър
            particles.forEach(p => { p.update(); p.draw(); }); // Обновяваме и рисуваме всяка частица
            requestAnimationFrame(animateP);
        }
        animateP();
    }
});