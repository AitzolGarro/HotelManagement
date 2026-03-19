/**
 * MobileManager - Task 10 Mobile-Responsive Design
 * Handles: bottom nav active states, swipe gestures, bottom sheet,
 *          FAB, PWA install prompt, offline indicator, push notifications
 */
class MobileManager {
    constructor() {
        this._deferredInstallPrompt = null;
        this._bottomSheet = null;
        this._bottomSheetOverlay = null;
        this._swipeState = null;
        this._isOnline = navigator.onLine;
        this._pushSupported = 'PushManager' in window && 'serviceWorker' in navigator;
    }

    // ─────────────────────────────────────────────────────────────
    // INIT
    // ─────────────────────────────────────────────────────────────
    initialize() {
        this._initBottomNav();
        this._initFAB();
        this._initBottomSheet();
        this._initOfflineIndicator();
        this._initPWAInstallPrompt();
        this._initCalendarSwipe();
        this._initMobileFormEnhancements();
        console.log('[Mobile] MobileManager initialized');
    }

    // ─────────────────────────────────────────────────────────────
    // 10.2 – BOTTOM NAVIGATION
    // ─────────────────────────────────────────────────────────────
    _initBottomNav() {
        const nav = document.querySelector('.mobile-bottom-nav');
        if (!nav) return;

        const currentPath = window.location.pathname.toLowerCase();
        const items = nav.querySelectorAll('.nav-item[data-section]');

        items.forEach(item => {
            const section = item.getAttribute('data-section');
            const href = item.getAttribute('href') || '';

            // Determine active state
            const isActive =
                (section === 'dashboard' && (currentPath === '/' || currentPath === '/home/index')) ||
                (section === 'calendar'  && currentPath.includes('calendar')) ||
                (section === 'reservations' && currentPath.includes('reservation'));

            if (isActive) {
                item.classList.add('active');
            }

            // Ensure touch targets
            item.setAttribute('role', 'link');
            item.setAttribute('tabindex', '0');

            // Keyboard support
            item.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    window.location.href = href;
                }
            });
        });

        // Set aria-current on [data-nav-link] elements (A-006)
        this._updateMobileNavAriaCurrent();
    }

    // ─────────────────────────────────────────────────────────────
    // A-006 – aria-current="page" for mobile bottom nav
    // ─────────────────────────────────────────────────────────────
    _updateMobileNavAriaCurrent() {
        const currentPath = window.location.pathname.toLowerCase();
        document.querySelectorAll('[data-nav-link]').forEach(link => {
            const href = (link.getAttribute('href') || '').toLowerCase();
            const isCurrent =
                href === currentPath ||
                (href !== '/' && currentPath.startsWith(href));
            if (isCurrent) {
                link.setAttribute('aria-current', 'page');
            } else {
                link.removeAttribute('aria-current');
            }
        });
    }

    // ─────────────────────────────────────────────────────────────
    // 10.3 – FLOATING ACTION BUTTON
    // ─────────────────────────────────────────────────────────────
    _initFAB() {
        const fab = document.getElementById('mobileFab');
        if (!fab) return;

        // Scroll behaviour: hide FAB when scrolling down, show when scrolling up
        let lastScrollY = window.scrollY;
        let ticking = false;

        window.addEventListener('scroll', () => {
            if (!ticking) {
                requestAnimationFrame(() => {
                    const currentScrollY = window.scrollY;
                    if (currentScrollY > lastScrollY + 10) {
                        fab.style.transform = 'scale(0) translateY(20px)';
                        fab.style.opacity = '0';
                        fab.style.pointerEvents = 'none';
                    } else if (currentScrollY < lastScrollY - 5) {
                        fab.style.transform = '';
                        fab.style.opacity = '';
                        fab.style.pointerEvents = '';
                    }
                    lastScrollY = currentScrollY;
                    ticking = false;
                });
                ticking = true;
            }
        }, { passive: true });
    }

    // ─────────────────────────────────────────────────────────────
    // 10.4 – BOTTOM SHEET
    // ─────────────────────────────────────────────────────────────
    _initBottomSheet() {
        // Create overlay
        this._bottomSheetOverlay = document.createElement('div');
        this._bottomSheetOverlay.className = 'bottom-sheet-overlay';
        this._bottomSheetOverlay.setAttribute('aria-hidden', 'true');
        this._bottomSheetOverlay.addEventListener('click', () => this.closeBottomSheet());
        document.body.appendChild(this._bottomSheetOverlay);

        // Create bottom sheet
        this._bottomSheet = document.createElement('div');
        this._bottomSheet.className = 'bottom-sheet';
        this._bottomSheet.setAttribute('role', 'dialog');
        this._bottomSheet.setAttribute('aria-modal', 'true');
        this._bottomSheet.setAttribute('aria-labelledby', 'bottomSheetTitle');
        this._bottomSheet.innerHTML = `
            <div class="bottom-sheet-handle" aria-hidden="true"></div>
            <div class="bottom-sheet-header">
                <h2 class="bottom-sheet-title" id="bottomSheetTitle">Details</h2>
                <button class="bottom-sheet-close" aria-label="Close details">
                    <i class="bi bi-x-lg" aria-hidden="true"></i>
                </button>
            </div>
            <div class="bottom-sheet-body" id="bottomSheetBody"></div>
        `;
        document.body.appendChild(this._bottomSheet);

        // Close button
        this._bottomSheet.querySelector('.bottom-sheet-close')
            .addEventListener('click', () => this.closeBottomSheet());

        // Drag-to-dismiss on handle
        this._initBottomSheetDrag();

        // Keyboard: Escape closes
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this._bottomSheet.classList.contains('active')) {
                this.closeBottomSheet();
            }
        });
    }

    _initBottomSheetDrag() {
        const handle = this._bottomSheet.querySelector('.bottom-sheet-handle');
        if (!handle) return;

        let startY = 0;
        let currentY = 0;
        let isDragging = false;

        const onStart = (e) => {
            startY = e.type === 'touchstart' ? e.touches[0].clientY : e.clientY;
            isDragging = true;
            this._bottomSheet.style.transition = 'none';
        };

        const onMove = (e) => {
            if (!isDragging) return;
            currentY = (e.type === 'touchmove' ? e.touches[0].clientY : e.clientY) - startY;
            if (currentY > 0) {
                this._bottomSheet.style.transform = `translateY(${currentY}px)`;
            }
        };

        const onEnd = () => {
            if (!isDragging) return;
            isDragging = false;
            this._bottomSheet.style.transition = '';

            if (currentY > 120) {
                this.closeBottomSheet();
            } else {
                this._bottomSheet.style.transform = '';
            }
            currentY = 0;
        };

        handle.addEventListener('touchstart', onStart, { passive: true });
        handle.addEventListener('touchmove', onMove, { passive: true });
        handle.addEventListener('touchend', onEnd);
        handle.addEventListener('mousedown', onStart);
        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onEnd);
    }

    /**
     * Open the bottom sheet with given title and HTML content.
     * On desktop (>= 992px) falls back to Bootstrap modal if available.
     */
    openBottomSheet(title, bodyHtml) {
        const isMobile = window.innerWidth < 992;

        if (!isMobile) {
            // On desktop, use a regular Bootstrap modal if one is open, else just skip
            return;
        }

        document.getElementById('bottomSheetTitle').textContent = title;
        document.getElementById('bottomSheetBody').innerHTML = bodyHtml;

        this._bottomSheetOverlay.classList.add('active');
        this._bottomSheet.classList.add('active');
        this._bottomSheet.removeAttribute('aria-hidden');
        this._bottomSheetOverlay.removeAttribute('aria-hidden');

        // Trap focus
        this._bottomSheet.querySelector('.bottom-sheet-close').focus();

        // Prevent body scroll
        document.body.style.overflow = 'hidden';
    }

    closeBottomSheet() {
        this._bottomSheet.classList.remove('active');
        this._bottomSheetOverlay.classList.remove('active');
        this._bottomSheet.setAttribute('aria-hidden', 'true');
        this._bottomSheetOverlay.setAttribute('aria-hidden', 'true');
        this._bottomSheet.style.transform = '';
        document.body.style.overflow = '';
    }

    // ─────────────────────────────────────────────────────────────
    // 10.4 – CALENDAR SWIPE GESTURES
    // ─────────────────────────────────────────────────────────────
    _initCalendarSwipe() {
        const calEl = document.getElementById('calendar');
        if (!calEl) return;

        let touchStartX = 0;
        let touchStartY = 0;
        let touchStartTime = 0;
        const SWIPE_THRESHOLD = 60;   // px
        const SWIPE_MAX_Y = 80;       // px – vertical tolerance
        const SWIPE_MAX_TIME = 400;   // ms

        calEl.addEventListener('touchstart', (e) => {
            touchStartX = e.touches[0].clientX;
            touchStartY = e.touches[0].clientY;
            touchStartTime = Date.now();
        }, { passive: true });

        calEl.addEventListener('touchend', (e) => {
            const dx = e.changedTouches[0].clientX - touchStartX;
            const dy = e.changedTouches[0].clientY - touchStartY;
            const dt = Date.now() - touchStartTime;

            if (dt > SWIPE_MAX_TIME) return;
            if (Math.abs(dy) > SWIPE_MAX_Y) return;
            if (Math.abs(dx) < SWIPE_THRESHOLD) return;

            // Dispatch custom event so CalendarManager can react
            const direction = dx < 0 ? 'left' : 'right';
            calEl.dispatchEvent(new CustomEvent('calendarSwipe', {
                detail: { direction },
                bubbles: true
            }));
        }, { passive: true });

        // Listen for the custom event and navigate the calendar
        calEl.addEventListener('calendarSwipe', (e) => {
            if (typeof calendarManager === 'undefined' || !calendarManager?.calendar) return;
            if (e.detail.direction === 'left') {
                calendarManager.calendar.next();
            } else {
                calendarManager.calendar.prev();
            }
        });
    }

    // ─────────────────────────────────────────────────────────────
    // 10.3 – MOBILE FORM ENHANCEMENTS
    // ─────────────────────────────────────────────────────────────
    _initMobileFormEnhancements() {
        // Add autocomplete attributes to common fields if missing
        const fieldMap = {
            'guestFirstName': 'given-name',
            'guestLastName':  'family-name',
            'guestEmail':     'email',
            'guestPhone':     'tel',
        };

        Object.entries(fieldMap).forEach(([id, autocomplete]) => {
            const el = document.getElementById(id);
            if (el && !el.getAttribute('autocomplete')) {
                el.setAttribute('autocomplete', autocomplete);
            }
        });

        // Ensure number inputs have correct inputmode for mobile keyboards
        document.querySelectorAll('input[type="number"]').forEach(el => {
            if (!el.getAttribute('inputmode')) {
                el.setAttribute('inputmode', 'numeric');
            }
        });

        // Ensure tel inputs have correct inputmode
        document.querySelectorAll('input[type="tel"]').forEach(el => {
            if (!el.getAttribute('inputmode')) {
                el.setAttribute('inputmode', 'tel');
            }
        });

        // Ensure email inputs have correct inputmode
        document.querySelectorAll('input[type="email"]').forEach(el => {
            if (!el.getAttribute('inputmode')) {
                el.setAttribute('inputmode', 'email');
            }
        });

        // Scroll to first invalid field on form submit
        document.querySelectorAll('form').forEach(form => {
            form.addEventListener('submit', () => {
                const firstInvalid = form.querySelector(':invalid');
                if (firstInvalid) {
                    firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    firstInvalid.focus();
                }
            });
        });
    }

    // ─────────────────────────────────────────────────────────────
    // 10.5 – OFFLINE INDICATOR
    // ─────────────────────────────────────────────────────────────
    _initOfflineIndicator() {
        // Create indicator element
        const indicator = document.createElement('div');
        indicator.className = 'offline-indicator';
        indicator.setAttribute('role', 'status');
        indicator.setAttribute('aria-live', 'polite');
        indicator.innerHTML = `
            <i class="bi bi-wifi-off" aria-hidden="true"></i>
            <span>You're offline – some features may be unavailable</span>
        `;
        document.body.prepend(indicator);

        const update = () => {
            if (!navigator.onLine) {
                indicator.classList.add('show');
                document.body.classList.add('offline');
            } else {
                indicator.classList.remove('show');
                document.body.classList.remove('offline');
            }
        };

        window.addEventListener('online',  update);
        window.addEventListener('offline', update);
        update(); // initial state
    }

    // ─────────────────────────────────────────────────────────────
    // 10.5 – PWA INSTALL PROMPT
    // ─────────────────────────────────────────────────────────────
    _initPWAInstallPrompt() {
        // Capture the beforeinstallprompt event
        window.addEventListener('beforeinstallprompt', (e) => {
            e.preventDefault();
            this._deferredInstallPrompt = e;

            // Only show if not already installed and not dismissed recently
            const dismissed = sessionStorage.getItem('pwa-install-dismissed');
            if (!dismissed) {
                // Delay slightly so page loads first
                setTimeout(() => this._showInstallBanner(), 3000);
            }
        });

        // Hide banner if app is installed
        window.addEventListener('appinstalled', () => {
            this._hideInstallBanner();
            this._deferredInstallPrompt = null;
            console.log('[PWA] App installed');
        });
    }

    _showInstallBanner() {
        let banner = document.getElementById('pwaInstallBanner');
        if (!banner) {
            banner = document.createElement('div');
            banner.id = 'pwaInstallBanner';
            banner.className = 'pwa-install-banner';
            banner.setAttribute('role', 'complementary');
            banner.setAttribute('aria-label', 'Install app prompt');
            banner.innerHTML = `
                <div class="pwa-install-banner-icon" aria-hidden="true">
                    <i class="bi bi-building"></i>
                </div>
                <div class="pwa-install-banner-text">
                    <strong>Install Hotel Res</strong>
                    <small>Add to home screen for quick access</small>
                </div>
                <div class="pwa-install-banner-actions">
                    <button class="btn btn-outline-secondary btn-sm" id="pwaInstallDismiss"
                            aria-label="Dismiss install prompt">
                        Not now
                    </button>
                    <button class="btn btn-primary btn-sm" id="pwaInstallAccept"
                            aria-label="Install app">
                        Install
                    </button>
                </div>
            `;
            document.body.appendChild(banner);

            document.getElementById('pwaInstallDismiss').addEventListener('click', () => {
                this._hideInstallBanner();
                sessionStorage.setItem('pwa-install-dismissed', '1');
            });

            document.getElementById('pwaInstallAccept').addEventListener('click', () => {
                this._triggerInstall();
            });
        }

        // Trigger reflow then add show class
        requestAnimationFrame(() => {
            requestAnimationFrame(() => banner.classList.add('show'));
        });
    }

    _hideInstallBanner() {
        const banner = document.getElementById('pwaInstallBanner');
        if (banner) {
            banner.classList.remove('show');
            setTimeout(() => banner.remove(), 400);
        }
    }

    async _triggerInstall() {
        if (!this._deferredInstallPrompt) return;
        this._deferredInstallPrompt.prompt();
        const { outcome } = await this._deferredInstallPrompt.userChoice;
        console.log('[PWA] Install outcome:', outcome);
        this._deferredInstallPrompt = null;
        this._hideInstallBanner();
    }

    // ─────────────────────────────────────────────────────────────
    // 10.5 – PUSH NOTIFICATIONS SETUP
    // ─────────────────────────────────────────────────────────────
    async requestPushPermission() {
        if (!this._pushSupported) {
            console.warn('[PWA] Push notifications not supported');
            return false;
        }

        try {
            const permission = await Notification.requestPermission();
            if (permission === 'granted') {
                await this._subscribeToPush();
                return true;
            }
            return false;
        } catch (err) {
            console.error('[PWA] Push permission error:', err);
            return false;
        }
    }

    async _subscribeToPush() {
        try {
            const registration = await navigator.serviceWorker.ready;
            const subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                // In production, replace with real VAPID public key from server
                applicationServerKey: this._urlBase64ToUint8Array(
                    'BEl62iUYgUivxIkv69yViEuiBIa-Ib9-SkvMeAtA3LFgDzkrxZJjSgSnfckjBJuBkr3qBUYIHBQFLXYp5Nksh8U'
                )
            });
            console.log('[PWA] Push subscription created:', subscription.endpoint);
            // In production: send subscription to server via API
        } catch (err) {
            console.warn('[PWA] Push subscription failed:', err);
        }
    }

    _urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        const rawData = window.atob(base64);
        return Uint8Array.from([...rawData].map(c => c.charCodeAt(0)));
    }

    // ─────────────────────────────────────────────────────────────
    // PUBLIC HELPERS
    // ─────────────────────────────────────────────────────────────

    /** Returns true when viewport is mobile (<992px) */
    isMobile() {
        return window.innerWidth < 992;
    }

    /** Returns true when viewport is small mobile (<576px) */
    isSmallMobile() {
        return window.innerWidth < 576;
    }
}

// Instantiate and expose globally
window.MobileManager = MobileManager;
