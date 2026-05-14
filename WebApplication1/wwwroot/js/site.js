/* ============================================
   HRMS Professional JavaScript
   Clean & Professional
   ============================================ */

// ============================================
// 1. DARK MODE SYSTEM
// ============================================
const DarkMode = {
    init() {
        this.toggle = document.getElementById('darkModeToggle');
        this.icon = document.getElementById('darkModeIcon');
        this.loadSavedTheme();
        this.bindEvents();
    },

    loadSavedTheme() {
        const savedTheme = localStorage.getItem('hrms-theme');
        if (savedTheme === 'dark') {
            this.enableDark();
        } else {
            this.enableLight();
        }
    },

    bindEvents() {
        if (this.toggle) {
            this.toggle.addEventListener('click', () => this.toggleTheme());
        }
    },

    toggleTheme() {
        document.body.classList.contains('dark-mode') ? this.enableLight() : this.enableDark();
    },

    enableDark() {
        document.body.classList.add('dark-mode');
        if (this.icon) {
            this.icon.classList.remove('bi-moon-fill');
            this.icon.classList.add('bi-sun-fill');
        }
        localStorage.setItem('hrms-theme', 'dark');
    },

    enableLight() {
        document.body.classList.remove('dark-mode');
        if (this.icon) {
            this.icon.classList.remove('bi-sun-fill');
            this.icon.classList.add('bi-moon-fill');
        }
        localStorage.setItem('hrms-theme', 'light');
    }
};

// ============================================
// 2. TOAST NOTIFICATION SYSTEM
// ============================================
const Toast = {
    container: null,

    init() {
        this.createContainer();
    },

    createContainer() {
        if (!document.getElementById('toastContainer')) {
            const container = document.createElement('div');
            container.id = 'toastContainer';
            container.className = 'toast-container';
            document.body.appendChild(container);
            this.container = container;
        } else {
            this.container = document.getElementById('toastContainer');
        }
    },

    show(message, type = 'info', duration = 5000) {
        const toast = document.createElement('div');
        toast.className = `toast-notification toast-${type}`;

        const icons = {
            success: 'bi-check-circle-fill',
            error: 'bi-x-circle-fill',
            warning: 'bi-exclamation-triangle-fill',
            info: 'bi-info-circle-fill'
        };

        toast.innerHTML = `
            <div class="toast-icon">
                <i class="bi ${icons[type] || icons.info}"></i>
            </div>
            <div class="toast-content">
                <div class="toast-message">${message}</div>
            </div>
            <button class="toast-close" onclick="this.parentElement.remove()">
                <i class="bi bi-x"></i>
            </button>
            <div class="toast-progress">
                <div class="toast-progress-bar" style="animation-duration: ${duration}ms"></div>
            </div>
        `;

        this.container.appendChild(toast);
        setTimeout(() => toast.classList.add('show'), 10);

        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, duration);

        return toast;
    },

    success(message, duration) { return this.show(message, 'success', duration); },
    error(message, duration) { return this.show(message, 'error', duration); },
    warning(message, duration) { return this.show(message, 'warning', duration); },
    info(message, duration) { return this.show(message, 'info', duration); }
};

// ============================================
// 3. ANIMATED COUNTER SYSTEM
// ============================================
const AnimatedCounter = {
    init() {
        this.observeCounters();
    },

    observeCounters() {
        const counters = document.querySelectorAll('[data-counter]');

        if ('IntersectionObserver' in window) {
            const observer = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        this.animate(entry.target);
                        observer.unobserve(entry.target);
                    }
                });
            }, { threshold: 0.5 });

            counters.forEach(counter => observer.observe(counter));
        } else {
            counters.forEach(counter => this.animate(counter));
        }
    },

    animate(element) {
        const target = parseInt(element.getAttribute('data-counter')) || 0;
        const duration = parseInt(element.getAttribute('data-duration')) || 2000;
        const suffix = element.getAttribute('data-suffix') || '';
        const prefix = element.getAttribute('data-prefix') || '';
        const startTime = performance.now();

        const updateCounter = (currentTime) => {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const easeOutQuart = 1 - Math.pow(1 - progress, 4);
            const current = Math.floor(easeOutQuart * target);

            element.textContent = prefix + current.toLocaleString() + suffix;

            if (progress < 1) {
                requestAnimationFrame(updateCounter);
            } else {
                element.textContent = prefix + target.toLocaleString() + suffix;
            }
        };

        requestAnimationFrame(updateCounter);
    }
};

// ============================================
// 4. LIVE CLOCK SYSTEM
// ============================================
const LiveClock = {
    element: null,
    dateElement: null,

    init() {
        this.element = document.getElementById('liveClock');
        this.dateElement = document.getElementById('liveDate');
        if (this.element) {
            this.update();
            setInterval(() => this.update(), 1000);
        }
    },

    update() {
        const now = new Date();

        if (this.element) {
            this.element.textContent = now.toLocaleTimeString('en-US', {
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: true
            });
        }

        if (this.dateElement) {
            this.dateElement.textContent = now.toLocaleDateString('en-US', {
                weekday: 'long',
                year: 'numeric',
                month: 'long',
                day: 'numeric'
            });
        }
    }
};

// ============================================
// 5. SCROLL EFFECTS
// ============================================
const ScrollEffects = {
    init() {
        this.navbar = document.querySelector('.navbar');
        this.backToTop = document.getElementById('backToTop');
        this.bindEvents();
    },

    bindEvents() {
        window.addEventListener('scroll', () => {
            this.handleScroll();
        }, { passive: true });

        if (this.backToTop) {
            this.backToTop.addEventListener('click', () => {
                window.scrollTo({ top: 0, behavior: 'smooth' });
            });
        }
    },

    handleScroll() {
        const scrollY = window.scrollY;

        if (this.navbar) {
            if (scrollY > 10) {
                this.navbar.classList.add('navbar-scrolled');
            } else {
                this.navbar.classList.remove('navbar-scrolled');
            }
        }

        if (this.backToTop) {
            if (scrollY > 300) {
                this.backToTop.classList.add('show');
            } else {
                this.backToTop.classList.remove('show');
            }
        }
    }
};

// ============================================
// 6. ACTIVITY FEED LOADER
// ============================================
const ActivityFeed = {
    container: null,

    init() {
        this.container = document.getElementById('activityFeed');
        if (this.container) {
            this.load();
        }
    },

    load() {
        const activities = [
            { icon: 'bi-person-plus', color: 'success', text: 'New employee registered', time: '2 min ago' },
            { icon: 'bi-calendar-check', color: 'primary', text: 'Leave request approved', time: '15 min ago' },
            { icon: 'bi-clock', color: 'info', text: 'Shift schedule updated', time: '1 hour ago' },
            { icon: 'bi-bell', color: 'warning', text: 'System maintenance scheduled', time: '2 hours ago' }
        ];

        let html = '';
        activities.forEach(activity => {
            html += `
                <div class="activity-item">
                    <div class="activity-icon bg-${activity.color}">
                        <i class="bi ${activity.icon}"></i>
                    </div>
                    <div class="activity-content">
                        <p class="activity-text">${activity.text}</p>
                        <small class="activity-time">${activity.time}</small>
                    </div>
                </div>
            `;
        });

        this.container.innerHTML = html;
    }
};

// ============================================
// 7. TOOLTIP INITIALIZATION
// ============================================
const TooltipInit = {
    init() {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }
};

// ============================================
// 8. CONFIRM DIALOGS
// ============================================
const ConfirmDialog = {
    show(options) {
        return new Promise((resolve) => {
            const modal = document.createElement('div');
            modal.className = 'confirm-modal-overlay';
            modal.innerHTML = `
                <div class="confirm-modal">
                    <div class="confirm-modal-icon ${options.type || 'warning'}">
                        <i class="bi ${options.icon || 'bi-exclamation-triangle'}"></i>
                    </div>
                    <h5 class="confirm-modal-title">${options.title || 'Confirm Action'}</h5>
                    <p class="confirm-modal-message">${options.message || 'Are you sure you want to proceed?'}</p>
                    <div class="confirm-modal-buttons">
                        <button class="btn btn-secondary" id="confirmCancel">
                            ${options.cancelText || 'Cancel'}
                        </button>
                        <button class="btn btn-${options.confirmClass || 'primary'}" id="confirmOk">
                            ${options.confirmText || 'Confirm'}
                        </button>
                    </div>
                </div>
            `;

            document.body.appendChild(modal);
            setTimeout(() => modal.classList.add('show'), 10);

            modal.querySelector('#confirmCancel').addEventListener('click', () => {
                modal.classList.remove('show');
                setTimeout(() => modal.remove(), 300);
                resolve(false);
            });

            modal.querySelector('#confirmOk').addEventListener('click', () => {
                modal.classList.remove('show');
                setTimeout(() => modal.remove(), 300);
                resolve(true);
            });

            modal.addEventListener('click', (e) => {
                if (e.target === modal) {
                    modal.classList.remove('show');
                    setTimeout(() => modal.remove(), 300);
                    resolve(false);
                }
            });
        });
    }
};

// ============================================
// 9. PAGE LOADER
// ============================================
const PageLoader = {
    hide() {
        const loader = document.getElementById('pageLoader');
        if (loader) {
            loader.classList.remove('show');
        }
    }
};

// ============================================
// 10. INITIALIZE EVERYTHING
// ============================================
document.addEventListener('DOMContentLoaded', function () {
    DarkMode.init();
    Toast.init();
    AnimatedCounter.init();
    LiveClock.init();
    ScrollEffects.init();
    ActivityFeed.init();
    TooltipInit.init();
    PageLoader.hide();
});

// ============================================
// 11. GLOBAL UTILITY FUNCTIONS
// ============================================
window.HRMS = {
    toast: Toast,
    confirm: ConfirmDialog.show,
    darkMode: DarkMode
};

// Handle confirm dialogs on delete buttons
document.addEventListener('click', async function (e) {
    if (e.target.closest('[data-confirm]')) {
        e.preventDefault();
        const btn = e.target.closest('[data-confirm]');
        const message = btn.getAttribute('data-confirm');

        const confirmed = await ConfirmDialog.show({
            title: 'Confirm Action',
            message: message,
            type: 'warning',
            confirmClass: 'danger',
            confirmText: 'Yes, Proceed'
        });

        if (confirmed) {
            if (btn.tagName === 'A') {
                window.location.href = btn.href;
            } else if (btn.form) {
                btn.form.submit();
            }
        }
    }
});