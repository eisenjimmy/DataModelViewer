// Simple Particle Animation
(function () {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    let container = null;
    let particles = [];
    let animationId = null;

    function initParticles() {
        container = document.getElementById('particles-js');
        if (!container) return;

        // Clear existing canvas if any
        container.innerHTML = '';
        container.appendChild(canvas);

        resizeCanvas();
        window.addEventListener('resize', resizeCanvas);

        // Create particles
        const particleCount = 150; // Increased from 50
        for (let i = 0; i < particleCount; i++) {
            particles.push(createParticle());
        }

        animate();
    }

    function createParticle() {
        return {
            x: Math.random() * canvas.width,
            y: Math.random() * canvas.height,
            vx: (Math.random() - 0.5) * 0.5,
            vy: (Math.random() - 0.5) * 0.5,
            size: Math.random() * 3 + 1,
            color: `rgba(100, 149, 237, ${Math.random() * 0.5 + 0.2})` // Higher opacity range
        };
    }

    function resizeCanvas() {
        if (!container) return;
        canvas.width = container.clientWidth;
        canvas.height = container.clientHeight;
    }

    function animate() {
        if (!container) return;

        ctx.clearRect(0, 0, canvas.width, canvas.height);

        particles.forEach(p => {
            p.x += p.vx;
            p.y += p.vy;

            // Bounce off edges
            if (p.x < 0 || p.x > canvas.width) p.vx *= -1;
            if (p.y < 0 || p.y > canvas.height) p.vy *= -1;

            ctx.beginPath();
            ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
            ctx.fillStyle = p.color;
            ctx.fill();
        });

        // Draw connections
        ctx.strokeStyle = 'rgba(100, 149, 237, 0.05)';
        ctx.lineWidth = 1;
        for (let i = 0; i < particles.length; i++) {
            for (let j = i + 1; j < particles.length; j++) {
                const dx = particles[i].x - particles[j].x;
                const dy = particles[i].y - particles[j].y;
                const dist = Math.sqrt(dx * dx + dy * dy);

                if (dist < 150) {
                    ctx.beginPath();
                    ctx.moveTo(particles[i].x, particles[i].y);
                    ctx.lineTo(particles[j].x, particles[j].y);
                    ctx.stroke();
                }
            }
        }

        animationId = requestAnimationFrame(animate);
    }

    // Initialize when DOM is ready or when navigating
    // Since Blazor is SPA, we might need to re-init on page changes.
    // We'll expose a global function to init.
    window.initParticles = initParticles;

    // Auto-init if element exists on load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initParticles);
    } else {
        initParticles();
    }
})();
