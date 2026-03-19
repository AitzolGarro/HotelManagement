/**
 * KeyboardManager - Task 14.3 Keyboard Navigation
 * Implements keyboard shortcuts and navigation improvements
 */
class KeyboardManager {
    constructor() {
        this._shortcuts = new Map();
        this._init();
    }

    _init() {
        this._registerDefaultShortcuts();
        this._initGlobalKeyHandler();
        this._initNotificationTabKeyNav();
        this._initModalFocusReturn();
        console.log('[Keyboard] KeyboardManager initialized');
    }

    // ── Default Shortcuts ────────────────────────────────────────
    _registerDefaultShortcuts() {
        // Alt+D → Dashboard
        this.register('alt+d', () => { window.location.href = '/'; }, 'Go to Dashboard');
        // Alt+C → Calendar
        this.register('alt+c', () => { window.location.href = '/Home/calendar'; }, 'Go to Calendar');
        // Alt+R → Reservations
        this.register('alt+r', () => { window.location.href = '/Home/reservations'; }, 'Go to Reservations');
        // Alt+N → Open notifications dropdown
        this.register('alt+n', () => {
            const bell = document.getElementById('notification-bell');
            if (bell) { bell.click(); bell.focus(); }
        }, 'Open Notifications');
        // Alt+/ → Show keyboard shortcuts
        this.register('alt+/', () => { this._showShortcutsModal(); }, 'Show keyboard shortcuts');
    }

    /**
     * Register a keyboard shortcut.
     * @param {string} combo - e.g. 'alt+d', 'ctrl+shift+r'
     * @param {Function} handler
     * @param {string} description
     */
    register(combo, handler, description = '') {
        this._shortcuts.set(combo.toLowerCase(), { handler, description });
    }

    _initGlobalKeyHandler() {
        document.addEventListener('keydown', (e) => {
            // Don't fire shortcuts when typing in inputs
            const tag = document.activeElement?.tagName?.toLowerCase();
            if (tag === 'input' || tag === 'textarea' || tag === 'select') return;
            if (document.activeElement?.isContentEditable) return;

            const combo = this._buildCombo(e);
            const shortcut = this._shortcuts.get(combo);
            if (shortcut) {
                e.preventDefault();
                shortcut.handler(e);
            }
        });
    }

    _buildCombo(e) {
        const parts = [];
        if (e.ctrlKey)  parts.push('ctrl');
        if (e.altKey)   parts.push('alt');
        if (e.shiftKey) parts.push('shift');
        if (e.metaKey)  parts.push('meta');
        parts.push(e.key.toLowerCase());
        return parts.join('+');
    }

    // ── Notification Tab Keyboard Navigation ─────────────────────
    // Implements roving tabindex for the notification filter tabs
    _initNotificationTabKeyNav() {
        const tabList = document.getElementById('notif-filter-tabs');
        if (!tabList) return;

        tabList.addEventListener('keydown', (e) => {
            const tabs = Array.from(tabList.querySelectorAll('[role="tab"]'));
            const currentIndex = tabs.indexOf(document.activeElement);
            if (currentIndex === -1) return;

            let newIndex = currentIndex;

            if (e.key === 'ArrowRight' || e.key === 'ArrowDown') {
                e.preventDefault();
                newIndex = (currentIndex + 1) % tabs.length;
            } else if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') {
                e.preventDefault();
                newIndex = (currentIndex - 1 + tabs.length) % tabs.length;
            } else if (e.key === 'Home') {
                e.preventDefault();
                newIndex = 0;
            } else if (e.key === 'End') {
                e.preventDefault();
                newIndex = tabs.length - 1;
            } else {
                return;
            }

            // Update tabindex
            tabs.forEach((tab, i) => {
                tab.setAttribute('tabindex', i === newIndex ? '0' : '-1');
            });
            tabs[newIndex].focus();
            tabs[newIndex].click(); // activate the tab
        });
    }

    // ── Modal Focus Return ────────────────────────────────────────
    // Ensures focus returns to the trigger element when a modal closes
    _initModalFocusReturn() {
        let lastFocusedElement = null;

        document.addEventListener('show.bs.modal', () => {
            lastFocusedElement = document.activeElement;
        });

        document.addEventListener('hidden.bs.modal', () => {
            if (lastFocusedElement && document.contains(lastFocusedElement)) {
                lastFocusedElement.focus();
            }
            lastFocusedElement = null;
        });
    }

    // ── Keyboard Shortcuts Modal ──────────────────────────────────
    _showShortcutsModal() {
        // Build shortcuts list from registered shortcuts
        const rows = Array.from(this._shortcuts.entries())
            .filter(([, s]) => s.description)
            .map(([combo, s]) => {
                const keys = combo.split('+').map(k =>
                    `<kbd class="kbd-hint">${k.charAt(0).toUpperCase() + k.slice(1)}</kbd>`
                ).join(' + ');
                return `<tr><td class="pe-3">${keys}</td><td>${s.description}</td></tr>`;
            }).join('');

        const modalId = 'keyboardShortcutsModal';
        let modal = document.getElementById(modalId);

        if (!modal) {
            modal = document.createElement('div');
            modal.id = modalId;
            modal.className = 'modal fade';
            modal.setAttribute('tabindex', '-1');
            modal.setAttribute('aria-labelledby', `${modalId}Label`);
            modal.setAttribute('aria-hidden', 'true');
            modal.innerHTML = `
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="${modalId}Label">
                                <i class="bi bi-keyboard me-2" aria-hidden="true"></i>Keyboard Shortcuts
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            <table class="table table-sm table-borderless">
                                <tbody>${rows}</tbody>
                            </table>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                        </div>
                    </div>
                </div>
            `;
            document.body.appendChild(modal);
        }

        new bootstrap.Modal(modal).show();
    }
}

// Instantiate globally
window.KeyboardManager = KeyboardManager;
document.addEventListener('DOMContentLoaded', () => {
    window.keyboardManager = new KeyboardManager();
});
