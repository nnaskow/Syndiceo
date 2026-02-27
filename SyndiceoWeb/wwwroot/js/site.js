const bgContainer = document.querySelector('.bg-objects');
const totalItems = 25;

const icons = ['notebook', 'clipboard', 'building', 'check','logo'];

function getSVG(icon) {
    switch (icon) {
        case 'notebook':
            return "url(\"data:image/svg+xml,%3Csvg viewBox='0 0 24 24' fill='none' stroke='%232C5282' stroke-width='1.5' xmlns='http://www.w3.org/2000/svg'%3E%3Crect x='4' y='2' width='16' height='20' rx='2'/%3E%3Cline x1='8' y1='6' x2='16' y2='6'/%3E%3Cline x1='8' y1='10' x2='16' y2='10'/%3E%3C/svg%3E\")";
        case 'clipboard':
            return "url(\"data:image/svg+xml,%3Csvg viewBox='0 0 24 24' fill='none' stroke='%232B6CB0' stroke-width='1.5' xmlns='http://www.w3.org/2000/svg'%3E%3Crect x='6' y='4' width='12' height='16' rx='2'/%3E%3Cpath d='M9 4h6v2H9z'/%3E%3C/svg%3E\")";
        case 'building':
            return "url(\"data:image/svg+xml,%3Csvg viewBox='0 0 24 24' fill='none' stroke='%232C5282' stroke-width='1.5' xmlns='http://www.w3.org/2000/svg'%3E%3Crect x='3' y='6' width='4' height='15'/%3E%3Crect x='9' y='3' width='4' height='18'/%3E%3Crect x='15' y='9' width='4' height='12'/%3E%3Cline x1='4' y1='8' x2='6' y2='8'/%3E%3Cline x1='4' y1='12' x2='6' y2='12'/%3E%3Cline x1='10' y1='5' x2='12' y2='5'/%3E%3Cline x1='10' y1='9' x2='12' y2='9'/%3E%3Cline x1='10' y1='13' x2='12' y2='13'/%3E%3Cline x1='16' y1='11' x2='18' y2='11'/%3E%3Cline x1='16' y1='15' x2='18' y2='15'/%3E%3C/svg%3E\")";
        case 'check':
            return "url(\"data:image/svg+xml,%3Csvg viewBox='0 0 24 24' fill='none' stroke='%232B6CB0' stroke-width='2' xmlns='http://www.w3.org/2000/svg'%3E%3Cpolyline points='4 12 9 17 20 6'/%3E%3C/svg%3E\")";
        case 'logo':
            return "url('/images/logo.png')";    }
}

const items = [];
for (let i = 0; i < totalItems; i++) {
    const item = document.createElement('div');
    item.classList.add('bg-item');

    const icon = icons[Math.floor(Math.random() * icons.length)];
    item.iconType = icon;
    item.style.backgroundImage = getSVG(icon);

    const size = 40 + Math.random() * 100;
    item.style.width = size + 'px';
    item.style.height = size + 'px';

    item.x = Math.random() * window.innerWidth;
    item.y = Math.random() * window.innerHeight;

    const baseSpeed = 0.2 + Math.random() * 0.5;
    item.speedMultiplier = (icon === 'logo') ? 0.15 : 1;
    const angle = Math.random() * 2 * Math.PI;
    item.vx = Math.cos(angle) * baseSpeed * item.speedMultiplier;
    item.vy = Math.sin(angle) * baseSpeed * item.speedMultiplier;

    bgContainer.appendChild(item);
    items.push(item);
}

function animate() {
    items.forEach(item => {
        item.x += item.vx;
        item.y += item.vy;


        if (item.x > window.innerWidth) item.x = -item.offsetWidth;
        if (item.x < -item.offsetWidth) item.x = window.innerWidth;
        if (item.y > window.innerHeight) item.y = -item.offsetHeight;
        if (item.y < -item.offsetHeight) item.y = window.innerHeight;

        item.style.transform = `translate(${item.x}px, ${item.y}px)`;
    });
    requestAnimationFrame(animate);
}
animate();