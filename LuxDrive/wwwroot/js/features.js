document.addEventListener("DOMContentLoaded", function () {
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('show');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1 });

    document.querySelectorAll('.feature-card').forEach(card => observer.observe(card));

    const hero = document.querySelector('.hero');
    if (hero) {
        hero.addEventListener('mousemove', (e) => {
            const rect = hero.getBoundingClientRect();
            hero.style.setProperty('--mouse-x', `${e.clientX - rect.left}px`);
            hero.style.setProperty('--mouse-y', `${e.clientY - rect.top}px`);
        });
    }

    const clouds = document.querySelectorAll('.hero-cloud');
    let mouseX = 0, mouseY = 0, targetX = 0, targetY = 0;

    document.addEventListener('mousemove', (e) => {
        targetX = (window.innerWidth / 2 - e.pageX) / 20;
        targetY = (window.innerHeight / 2 - e.pageY) / 20;
    });

    function animateClouds() {
        mouseX += (targetX - mouseX) * 0.05;
        mouseY += (targetY - mouseY) * 0.05;
        const scrollY = window.scrollY;
        clouds.forEach(cloud => {
            const speed = parseFloat(cloud.getAttribute('data-speed'));
            const depth = parseFloat(cloud.getAttribute('data-depth'));
            cloud.style.transform = `translate3d(${mouseX * depth}px, ${scrollY * depth + mouseY * depth}px, 0) rotate(${scrollY * speed * 5}deg)`;
        });
        requestAnimationFrame(animateClouds);
    }
    animateClouds();

    const canvas = document.getElementById('particles-canvas');
    if (canvas) {
        const ctx = canvas.getContext('2d');
        let width, height, particles = [];

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
                this.size = Math.random() * 2 + 0.5;
                this.speedX = (Math.random() - 0.5) * 0.5;
                this.speedY = (Math.random() - 0.5) * 0.5;
                this.alpha = Math.random() * 0.5 + 0.1;
                this.fadingOut = Math.random() > 0.5;
            }
            update() {
                this.x += this.speedX;
                this.y += this.speedY;
                if (this.fadingOut) {
                    this.alpha -= 0.01;
                    if (this.alpha <= 0) { this.fadingOut = false; this.reset(); }
                } else {
                    this.alpha += 0.01;
                    if (this.alpha >= 0.8) this.fadingOut = true;
                }
            }
            draw() {
                ctx.fillStyle = `rgba(198, 166, 100, ${this.alpha})`;
                ctx.beginPath();
                ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
                ctx.fill();
            }
        }

        for (let i = 0; i < 60; i++) particles.push(new Particle());

        function animateP() {
            ctx.clearRect(0, 0, width, height);
            particles.forEach(p => { p.update(); p.draw(); });
            requestAnimationFrame(animateP);
        }
        animateP();
    }
});