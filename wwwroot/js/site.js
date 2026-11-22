// APEX ATTENDANCE - GLOBAL UX CONTROLLER

document.addEventListener('DOMContentLoaded', () => {
    // 1. Live Digital Clock in Topbar
    const clockEl = document.getElementById('topbar-live-clock');
    if (clockEl) {
        const updateClock = () => {
            const now = new Date();
            const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true });
            const dateStr = now.toLocaleDateString([], { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' });
            clockEl.innerHTML = `<span class="text-white font-mono fw-bold">${timeStr}</span> <span class="text-muted ms-2">${dateStr}</span>`;
        };
        updateClock();
        setInterval(updateClock, 1000);
    }

    // 2. Load Biometric Profiles globally
    if (window.apexBiometrics) {
        window.apexBiometrics.loadEnrolledProfiles();
    }
});

// Toast notification helper
function showToast(title, message, type = 'info') {
    let container = document.getElementById('apex-toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'apex-toast-container';
        container.style.cssText = 'position: fixed; bottom: 24px; right: 24px; z-index: 9999; display: flex; flex-direction: column; gap: 12px; pointer-events: none;';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = 'glass-panel p-3';
    toast.style.cssText = `
        min-width: 320px;
        max-width: 420px;
        pointer-events: auto;
        border-left: 4px solid ${type === 'success' ? '#10b981' : type === 'error' ? '#f43f5e' : '#06b6d4'};
        box-shadow: 0 10px 30px rgba(0,0,0,0.5);
        animation: toastIn 0.3s cubic-bezier(0.16, 1, 0.3, 1);
    `;

    const icon = type === 'success' ? 'bi-check-circle-fill text-success' : type === 'error' ? 'bi-exclamation-octagon-fill text-danger' : 'bi-info-circle-fill text-info';

    toast.innerHTML = `
        <div class="d-flex align-items-start gap-3">
            <i class="bi ${icon} fs-4"></i>
            <div class="flex-grow-1">
                <div class="fw-bold text-white mb-1">${title}</div>
                <div class="text-secondary small">${message}</div>
            </div>
            <button type="button" class="btn-close btn-close-white ms-auto" style="font-size: 10px;" onclick="this.parentElement.parentElement.remove()"></button>
        </div>
    `;

    container.appendChild(toast);
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateY(10px)';
        toast.style.transition = 'all 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, 4500);
}
