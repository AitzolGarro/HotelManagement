/**
 * Guest Portal – shared JS
 * Handles: auth state, nav active states, logout, token expiry check
 */
(function () {
    'use strict';

    const TOKEN_KEY   = 'guestToken';
    const EXPIRES_KEY = 'guestExpires';
    const PROFILE_KEY = 'guestProfile';

    // ─── Auth helpers ─────────────────────────────────────────────────────────

    function getToken()   { return localStorage.getItem(TOKEN_KEY); }
    function getProfile() {
        try { return JSON.parse(localStorage.getItem(PROFILE_KEY) || '{}'); }
        catch { return {}; }
    }

    function isTokenExpired() {
        const exp = localStorage.getItem(EXPIRES_KEY);
        if (!exp) return true;
        return new Date(exp) < new Date();
    }

    function clearSession() {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(EXPIRES_KEY);
        localStorage.removeItem(PROFILE_KEY);
    }

    // ─── UI state ─────────────────────────────────────────────────────────────

    function updateNavState() {
        const token   = getToken();
        const profile = getProfile();
        const loggedIn = !!token && !isTokenExpired();

        // Navbar buttons
        const loginBtn  = document.getElementById('gpLoginBtn');
        const logoutBtn = document.getElementById('gpLogoutBtn');
        const nameEl    = document.getElementById('gpGuestName');

        if (loginBtn)  loginBtn.style.display  = loggedIn ? 'none' : '';
        if (logoutBtn) logoutBtn.style.removeProperty('display');
        if (logoutBtn) logoutBtn.style.display  = loggedIn ? '' : 'none';
        if (nameEl && loggedIn) nameEl.textContent = `${profile.firstName || ''} ${profile.lastName || ''}`.trim();

        // Sidebar logout link visibility
        const sidebarLogout = document.getElementById('gpSidebarLogout');
        if (sidebarLogout) sidebarLogout.style.display = loggedIn ? '' : 'none';
    }

    // ─── Active nav link ──────────────────────────────────────────────────────

    function setActiveNavLink() {
        const path = window.location.pathname.toLowerCase();
        document.querySelectorAll('[data-gp-page]').forEach(el => {
            const page = el.dataset.gpPage;
            const isActive =
                (page === 'dashboard'     && (path.endsWith('/dashboard') || path.endsWith('/guestportal') || path.endsWith('/guestportal/'))) ||
                (page === 'reservations'  && path.includes('/reservations')) ||
                (page === 'profile'       && path.includes('/profile'));
            el.classList.toggle('active', isActive);
        });
    }

    // ─── Alert helpers (exposed globally) ────────────────────────────────────

    window.gpShowError = function (msg) {
        const el = document.getElementById('gpErrorAlert');
        const msgEl = document.getElementById('gpErrorMessage');
        if (!el || !msgEl) return;
        msgEl.textContent = msg;
        el.classList.remove('d-none');
        el.classList.add('show');
        setTimeout(() => el.classList.add('d-none'), 6000);
    };

    window.gpShowSuccess = function (msg) {
        const el = document.getElementById('gpSuccessAlert');
        const msgEl = document.getElementById('gpSuccessMessage');
        if (!el || !msgEl) return;
        msgEl.textContent = msg;
        el.classList.remove('d-none');
        el.classList.add('show');
        setTimeout(() => el.classList.add('d-none'), 4000);
    };

    // ─── Authenticated fetch wrapper ──────────────────────────────────────────

    window.gpFetch = async function (url, options = {}) {
        const token = getToken();
        if (!token || isTokenExpired()) {
            clearSession();
            window.location.href = '/GuestPortal/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
            return null;
        }
        const headers = { 'Content-Type': 'application/json', ...options.headers, 'Authorization': 'Bearer ' + token };
        const res = await fetch(url, { ...options, headers });
        if (res.status === 401) {
            clearSession();
            window.location.href = '/GuestPortal/Login';
            return null;
        }
        return res;
    };

    // ─── Init ─────────────────────────────────────────────────────────────────

    document.addEventListener('DOMContentLoaded', function () {
        updateNavState();
        setActiveNavLink();

        // Logout buttons
        document.querySelectorAll('#gpLogoutBtn, #gpSidebarLogout').forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                clearSession();
                window.location.href = '/GuestPortal/Login';
            });
        });

        // Auto-expire check
        if (isTokenExpired() && getToken()) {
            clearSession();
            const path = window.location.pathname.toLowerCase();
            const publicPages = ['/guestportal/login', '/guestportal/logout'];
            if (!publicPages.some(p => path.includes(p))) {
                window.location.href = '/GuestPortal/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
            }
        }
    });
})();
