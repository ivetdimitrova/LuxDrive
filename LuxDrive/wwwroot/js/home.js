document.addEventListener("DOMContentLoaded", function () {

    const hero = document.querySelector('.hero');
    hero.addEventListener('mousemove', (e) => {
        const rect = hero.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;

        hero.style.setProperty('--mouse-x', `${x}px`);
        hero.style.setProperty('--mouse-y', `${y}px`);
    });

    const clouds = document.querySelectorAll('.hero-cloud');
    let mouseX = 0, mouseY = 0;
    let targetX = 0, targetY = 0;

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

            const rotate = scrollY * speed * 5;
            const yPos = scrollY * depth;
            const xMouse = mouseX * depth;
            const yMouse = mouseY * depth;

            const time = Date.now() * 0.0005;
            const floatY = Math.sin(time + depth * 10) * 8;

            cloud.style.transform = `translate3d(${xMouse}px, ${yPos + yMouse + floatY}px, 0) rotate(${rotate}deg)`;
        });
        requestAnimationFrame(animateClouds);
    }
    animateClouds();

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
            constructor() {
                this.reset();
            }

            reset() {
                this.x = Math.random() * width;
                this.y = Math.random() * height;
                this.size = Math.random() * 2 + 0.5;
                this.speedX = (Math.random() - 0.5) * 0.5;
                this.speedY = (Math.random() - 0.5) * 0.5;
                this.alpha = Math.random() * 0.5 + 0.1;
                this.fadeSpeed = Math.random() * 0.01 + 0.005;
                this.fadingOut = Math.random() > 0.5;
            }

            update() {
                this.x += this.speedX + (mouseX * 0.05 * this.size);
                this.y += this.speedY + (mouseY * 0.05 * this.size);

                if (this.fadingOut) {
                    this.alpha -= this.fadeSpeed;
                    if (this.alpha <= 0) { this.fadingOut = false; this.reset(); }
                } else {
                    this.alpha += this.fadeSpeed;
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

        for (let i = 0; i < 100; i++) particles.push(new Particle());

        function animateParticles() {
            ctx.clearRect(0, 0, width, height);
            particles.forEach(p => {
                p.update();
                p.draw();
            });
            requestAnimationFrame(animateParticles);
        }
        animateParticles();
    }

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('show');
            }
        });
    }, { threshold: 0.1 });

    document.querySelectorAll('.feature').forEach(f => observer.observe(f));
});