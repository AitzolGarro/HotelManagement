// Authentication module
class AuthManager {
    constructor() {
        this.tokenKey = 'jwt_token';
        this.refreshTokenKey = 'refresh_token';
        this.userKey = 'current_user';
        this.twoFactorChallengeKey = '2fa_challenge_token';
        this.sessionTimeout = 2 * 60 * 60 * 1000; // 2 hours in milliseconds
        this.themeKey = 'app_theme';
        this.currencyKey = 'app_currency';
        this.localeKey = 'app_locale';
        
        this.applyAppearanceSettings();
        this.initializeAuth();
        this.setupSessionTimeout();
    }

    getUiLocale() {
        return localStorage.getItem(this.localeKey) || window.__hotelLocale || 'en-US';
    }

    applyAppearanceSettings() {
        const theme = localStorage.getItem(this.themeKey) || 'auto';
        const resolvedTheme = theme === 'auto'
            ? (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
            : theme;

        document.documentElement.setAttribute('data-bs-theme', resolvedTheme);
        document.body?.setAttribute('data-bs-theme', resolvedTheme);

        if (!localStorage.getItem(this.currencyKey)) {
            localStorage.setItem(this.currencyKey, 'EUR');
        }
        if (!localStorage.getItem(this.localeKey)) {
            localStorage.setItem(this.localeKey, this.getUiLocale());
        }
    }

    // Initialize authentication state
    initializeAuth() {
        const token = this.getToken();
        if (token) {
            if (this.isTokenExpired(token)) {
                this.logout();
            } else {
                this.updateUIForAuthenticatedUser();
                this.startTokenRefreshTimer();
            }
        } else {
            this.updateUIForUnauthenticatedUser();
        }
    }

    // Login method
    async login(credentials) {
        try {
            const response = await API.login(credentials);

            if (response.requiresTwoFactor === true) {
                if (!response.challengeToken) {
                    throw new Error('Two-factor challenge token was not returned by the server.');
                }

                this.setPendingTwoFactorChallenge(response.challengeToken);
                this.showTwoFactorStep();
                return false;
            }
            
            if (this.hasAccessToken(response)) {
                await this.completeLogin(response, credentials.rememberMe);
                return true;
            } else {
                throw new Error('Invalid response from server');
            }
        } catch (error) {
            console.error('Login error:', error);
            const errorMessage = error.message || 'Login failed. Please try again.';
            UI.showError(errorMessage);
            return false;
        }
    }

    // Logout method
    logout() {
        // Disconnect SignalR before clearing tokens
        this.disconnectSignalR();
        
        this.clearTokens();
        this.clearPendingTwoFactorChallenge();
        this.clearUser();
        this.updateUIForUnauthenticatedUser();
        this.clearSessionTimeout();
        
        UI.showSuccess('Signed out successfully');
        
        // Redirect to login page if not already there
        if (!window.location.pathname.includes('/login')) {
            window.location.href = '/login';
        }
    }

    // Disconnect SignalR connection
    async disconnectSignalR() {
        try {
            if (window.signalRManager) {
                await window.signalRManager.disconnect();
                window.signalRManager = null;
                console.log('SignalR disconnected on logout');
            }
        } catch (error) {
            console.error('Error disconnecting SignalR on logout:', error);
        }
    }

    // Token management
    getToken() {
        return localStorage.getItem(this.tokenKey);
    }

    setToken(token) {
        localStorage.setItem(this.tokenKey, token);
    }

    getRefreshToken() {
        return localStorage.getItem(this.refreshTokenKey);
    }

    setRefreshToken(token) {
        localStorage.setItem(this.refreshTokenKey, token);
    }

    clearTokens() {
        localStorage.removeItem(this.tokenKey);
        localStorage.removeItem(this.refreshTokenKey);
    }

    getPendingTwoFactorChallenge() {
        return sessionStorage.getItem(this.twoFactorChallengeKey);
    }

    setPendingTwoFactorChallenge(challengeToken) {
        sessionStorage.setItem(this.twoFactorChallengeKey, challengeToken);
    }

    clearPendingTwoFactorChallenge() {
        sessionStorage.removeItem(this.twoFactorChallengeKey);
    }

    // User management
    getUser() {
        const userJson = localStorage.getItem(this.userKey);
        return userJson ? JSON.parse(userJson) : null;
    }

    setUser(user) {
        localStorage.setItem(this.userKey, JSON.stringify(user));
    }

    clearUser() {
        localStorage.removeItem(this.userKey);
    }

    // Token validation
    isTokenExpired(token) {
        if (!token) return true;
        
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            const currentTime = Date.now() / 1000;
            return payload.exp < currentTime;
        } catch (error) {
            return true;
        }
    }

    // Token refresh
    async refreshToken() {
        const refreshToken = this.getRefreshToken();
        if (!refreshToken) {
            this.logout();
            return false;
        }

        try {
            const response = await API.refreshToken();
            if (response.token) {
                this.setToken(response.token);
                if (response.refreshToken) {
                    this.setRefreshToken(response.refreshToken);
                }
                this.resetSessionTimeout();
                return true;
            }
        } catch (error) {
            console.error('Token refresh failed:', error);
            this.logout();
            return false;
        }
    }

    hasAccessToken(response) {
        return Boolean(response && (response.token || response.accessToken));
    }

    getAccessToken(response) {
        return response.token || response.accessToken;
    }

    async completeLogin(response, rememberMe = false) {
        const token = this.getAccessToken(response);

        if (!token) {
            throw new Error('Authentication token missing from server response');
        }

        console.log('Login successful, storing token'); // Debug log
        this.setToken(token);
        this.clearPendingTwoFactorChallenge();
        this.hideTwoFactorStep();

        if (response.refreshToken) {
            this.setRefreshToken(response.refreshToken);
        }

        if (response.user) {
            console.log('Storing user data:', response.user); // Debug log
            this.setUser(response.user);
        }

        if (rememberMe) {
            localStorage.setItem('remember_me', 'true');
        } else {
            localStorage.removeItem('remember_me');
        }

        this.updateUIForAuthenticatedUser();
        this.startTokenRefreshTimer();
        this.resetSessionTimeout();
        await this.initializeSignalRAfterAuth();

        UI.showSuccess('Sign in successful!');

        setTimeout(() => {
            const intendedUrl = sessionStorage.getItem('intended_url') || '/';
            sessionStorage.removeItem('intended_url');
            window.location.href = intendedUrl;
        }, 1000);
    }

    showTwoFactorStep() {
        const totpStep = document.getElementById('totp-step');
        const totpCodeInput = document.getElementById('totp-code');
        const totpError = document.getElementById('totp-error');
        const loginStepSelectors = [
            '#loginForm .form-floating',
            '#loginForm .row.mb-3',
            '#loginForm .d-grid.mb-3'
        ];

        loginStepSelectors.forEach(selector => {
            document.querySelectorAll(selector).forEach(element => {
                element.style.display = 'none';
            });
        });

        if (totpStep) {
            totpStep.style.display = 'block';
        }

        if (totpError) {
            totpError.textContent = '';
            totpError.style.display = 'none';
        }

        if (totpCodeInput) {
            totpCodeInput.value = '';
            totpCodeInput.focus();
        }
    }

    hideTwoFactorStep() {
        const totpStep = document.getElementById('totp-step');
        const totpError = document.getElementById('totp-error');
        const loginStepSelectors = [
            '#loginForm .form-floating',
            '#loginForm .row.mb-3',
            '#loginForm .d-grid.mb-3'
        ];

        loginStepSelectors.forEach(selector => {
            document.querySelectorAll(selector).forEach(element => {
                element.style.display = '';
            });
        });

        if (totpStep) {
            totpStep.style.display = 'none';
        }

        if (totpError) {
            totpError.textContent = '';
            totpError.style.display = 'none';
        }
    }

    async submitTwoFactorCode() {
        const challengeToken = this.getPendingTwoFactorChallenge();
        const codeInput = document.getElementById('totp-code');
        const errorContainer = document.getElementById('totp-error');
        const rememberMeInput = document.getElementById('rememberMe');
        const code = codeInput ? codeInput.value.trim() : '';

        if (!challengeToken) {
            if (errorContainer) {
                errorContainer.textContent = 'Your login challenge expired. Please sign in again.';
                errorContainer.style.display = 'block';
            }
            this.hideTwoFactorStep();
            return;
        }

        if (!code) {
            if (errorContainer) {
                errorContainer.textContent = 'Please enter your authentication code.';
                errorContainer.style.display = 'block';
            }
            if (codeInput) {
                codeInput.focus();
            }
            return;
        }

        try {
            const response = await API.challenge2FA(challengeToken, code);
            await this.completeLogin(response, rememberMeInput ? rememberMeInput.checked : false);
        } catch (error) {
            if (errorContainer) {
                errorContainer.textContent = error.message || 'Invalid authentication code. Please try again.';
                errorContainer.style.display = 'block';
            }

            if (codeInput) {
                codeInput.value = '';
                codeInput.focus();
            }
        }
    }

    initializeTwoFactorChallengeHandlers() {
        const totpSubmitButton = document.getElementById('totp-submit');
        const totpCodeInput = document.getElementById('totp-code');

        if (totpSubmitButton && !totpSubmitButton.dataset.authBound) {
            totpSubmitButton.dataset.authBound = 'true';
            totpSubmitButton.addEventListener('click', async () => {
                await this.submitTwoFactorCode();
            });
        }

        if (totpCodeInput && !totpCodeInput.dataset.authBound) {
            totpCodeInput.dataset.authBound = 'true';
            totpCodeInput.addEventListener('keydown', async (event) => {
                if (event.key === 'Enter') {
                    event.preventDefault();
                    await this.submitTwoFactorCode();
                }
            });
        }
    }

    initializeTwoFactorSetupHandlers() {
        const qrcodeElement = document.getElementById('qrcode');

        if (!qrcodeElement) {
            return;
        }

        this.setup2FA();

        const retryButton = document.getElementById('retryBtn');
        const enableButton = document.getElementById('enableBtn');
        const verificationCodeInput = document.getElementById('verificationCode');
        const copyKeyButton = document.getElementById('copyKeyBtn');
        const copyCodesButton = document.getElementById('copyCodesBtn');
        const downloadCodesButton = document.getElementById('downloadCodesBtn');

        if (retryButton && !retryButton.dataset.authBound) {
            retryButton.dataset.authBound = 'true';
            retryButton.addEventListener('click', () => this.setup2FA());
        }

        if (enableButton && !enableButton.dataset.authBound) {
            enableButton.dataset.authBound = 'true';
            enableButton.addEventListener('click', async () => {
                await this.enableTwoFactorSetup();
            });
        }

        if (verificationCodeInput && !verificationCodeInput.dataset.authBound) {
            verificationCodeInput.dataset.authBound = 'true';
            verificationCodeInput.addEventListener('keypress', async (event) => {
                if (event.key === 'Enter') {
                    event.preventDefault();
                    await this.enableTwoFactorSetup();
                }
            });
        }

        if (copyKeyButton && !copyKeyButton.dataset.authBound) {
            copyKeyButton.dataset.authBound = 'true';
            copyKeyButton.addEventListener('click', async () => {
                const manualEntryKeyInput = document.getElementById('manualEntryKey');
                if (manualEntryKeyInput && manualEntryKeyInput.value) {
                    await navigator.clipboard.writeText(manualEntryKeyInput.value);
                    UI.showToast('Key copied to clipboard', 'success');
                }
            });
        }

        if (copyCodesButton && !copyCodesButton.dataset.authBound) {
            copyCodesButton.dataset.authBound = 'true';
            copyCodesButton.addEventListener('click', async () => {
                if (Array.isArray(this.recoveryCodes) && this.recoveryCodes.length > 0) {
                    await navigator.clipboard.writeText(this.recoveryCodes.join('\n'));
                    UI.showToast('Recovery codes copied to clipboard', 'success');
                }
            });
        }

        if (downloadCodesButton && !downloadCodesButton.dataset.authBound) {
            downloadCodesButton.dataset.authBound = 'true';
            downloadCodesButton.addEventListener('click', () => {
                if (!Array.isArray(this.recoveryCodes) || this.recoveryCodes.length === 0) {
                    return;
                }

                const content = 'Hotel Reservation System — 2FA Recovery Codes\n\n' + this.recoveryCodes.join('\n');
                const blob = new Blob([content], { type: 'text/plain' });
                const url = URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = url;
                link.download = 'hotel-2fa-recovery-codes.txt';
                link.click();
                URL.revokeObjectURL(url);
            });
        }
    }

    showTwoFactorSetupStep(stepId) {
        ['step-setup', 'step-recovery', 'step-loading', 'step-error'].forEach(id => {
            const element = document.getElementById(id);
            if (element) {
                element.style.display = id === stepId ? 'block' : 'none';
            }
        });
    }

    getResponseValue(response, ...propertyNames) {
        for (const propertyName of propertyNames) {
            if (response && response[propertyName] !== undefined && response[propertyName] !== null) {
                return response[propertyName];
            }
        }

        return null;
    }

    async setup2FA() {
        const setupErrorMessage = document.getElementById('setupErrorMessage');
        const qrCodeContainer = document.getElementById('qrcode');
        const manualEntryKeyInput = document.getElementById('manualEntryKey');

        if (!qrCodeContainer || !manualEntryKeyInput) {
            return;
        }

        this.showTwoFactorSetupStep('step-loading');

        try {
            const response = await API.setup2FA();
            const authenticatorUri = this.getResponseValue(response, 'AuthenticatorUri', 'authenticatorUri');
            const manualEntryKey = this.getResponseValue(response, 'ManualEntryKey', 'manualEntryKey');

            if (!authenticatorUri || !manualEntryKey) {
                throw new Error('Incomplete two-factor setup response from server.');
            }

            qrCodeContainer.innerHTML = '';
            new QRCode(qrCodeContainer, {
                text: authenticatorUri,
                width: 200,
                height: 200,
                colorDark: '#000000',
                colorLight: '#ffffff',
                correctLevel: QRCode.CorrectLevel.M
            });

            manualEntryKeyInput.value = manualEntryKey;
            this.showTwoFactorSetupStep('step-setup');
        } catch (error) {
            if (setupErrorMessage) {
                setupErrorMessage.textContent = error.message || 'Failed to load 2FA setup information.';
            }
            this.showTwoFactorSetupStep('step-error');
        }
    }

    async enableTwoFactorSetup() {
        const codeInput = document.getElementById('verificationCode');
        const codeError = document.getElementById('codeError');
        const code = codeInput ? codeInput.value.trim() : '';

        if (!codeInput || !codeError) {
            return;
        }

        if (!code || code.length < 6 || code.length > 8) {
            codeInput.classList.add('is-invalid');
            codeError.textContent = 'Please enter a valid 6-8 digit code.';
            return;
        }

        codeInput.classList.remove('is-invalid');

        try {
            UI.showLoading('Enabling 2FA...');
            const response = await API.enable2FA(code);
            const recoveryCodes = this.getResponseValue(response, 'RecoveryCodes', 'recoveryCodes');

            if (!Array.isArray(recoveryCodes) || recoveryCodes.length === 0) {
                throw new Error('Recovery codes were not returned by the server.');
            }

            this.recoveryCodes = recoveryCodes;
            this.renderRecoveryCodes(recoveryCodes);
            this.showTwoFactorSetupStep('step-recovery');
        } catch (error) {
            codeInput.classList.add('is-invalid');
            codeError.textContent = error.message || 'Invalid code. Please try again.';
            codeInput.value = '';
            codeInput.focus();
        } finally {
            UI.hideLoading();
        }
    }

    renderRecoveryCodes(codes) {
        const recoveryCodesList = document.getElementById('recoveryCodesList');

        if (!recoveryCodesList) {
            return;
        }

        recoveryCodesList.innerHTML = codes
            .map(code => `<div class="mb-1">${this.escapeHtml(code)}</div>`)
            .join('');
    }

    escapeHtml(value) {
        return String(value).replace(/[&<>"']/g, match => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        }[match]));
    }

    // Start automatic token refresh
    startTokenRefreshTimer() {
        const token = this.getToken();
        if (!token) return;

        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            const expirationTime = payload.exp * 1000;
            const currentTime = Date.now();
            const refreshTime = expirationTime - (5 * 60 * 1000); // Refresh 5 minutes before expiry

            if (refreshTime > currentTime) {
                setTimeout(() => {
                    this.refreshToken();
                }, refreshTime - currentTime);
            }
        } catch (error) {
            console.error('Error setting up token refresh timer:', error);
        }
    }

    // Session timeout management
    setupSessionTimeout() {
        // Reset timeout on user activity
        const events = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart'];
        events.forEach(event => {
            document.addEventListener(event, () => {
                this.resetSessionTimeout();
            }, true);
        });
    }

    resetSessionTimeout() {
        if (this.sessionTimeoutId) {
            clearTimeout(this.sessionTimeoutId);
        }
        
        if (this.warningTimeoutId) {
            clearTimeout(this.warningTimeoutId);
        }

        if (this.getToken()) {
            // Show warning 5 minutes before session expires
            const warningTime = this.sessionTimeout - (5 * 60 * 1000);
            
            this.warningTimeoutId = setTimeout(() => {
                this.showSessionWarning();
            }, warningTime);
            
            this.sessionTimeoutId = setTimeout(() => {
                UI.showError('Session expired due to inactivity');
                this.logout();
            }, this.sessionTimeout);
        }
    }

    showSessionWarning() {
        // Create a more user-friendly session warning modal
        this.createSessionWarningModal();
    }

    createSessionWarningModal() {
        // Remove existing modal if present
        const existingModal = document.getElementById('sessionWarningModal');
        if (existingModal) {
            existingModal.remove();
        }

        // Create modal HTML
        const modalHtml = `
            <div class="modal fade" id="sessionWarningModal" tabindex="-1" data-bs-backdrop="static" data-bs-keyboard="false">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content">
                        <div class="modal-header bg-warning text-dark">
                            <h5 class="modal-title">
                                <i class="bi bi-exclamation-triangle-fill me-2"></i>
                                Session Expiring Soon
                            </h5>
                        </div>
                        <div class="modal-body">
                            <p class="mb-3">Your session will expire in <strong id="sessionCountdown">5:00</strong> due to inactivity.</p>
                            <p class="mb-0">Would you like to extend your session?</p>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" id="sessionLogoutBtn">
                                <i class="bi bi-box-arrow-right me-1"></i>
                                Logout Now
                            </button>
                            <button type="button" class="btn btn-primary" id="sessionExtendBtn">
                                <i class="bi bi-clock-history me-1"></i>
                                Extend Session
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Add modal to DOM
        document.body.insertAdjacentHTML('beforeend', modalHtml);

        // Get modal elements
        const modal = document.getElementById('sessionWarningModal');
        const countdown = document.getElementById('sessionCountdown');
        const extendBtn = document.getElementById('sessionExtendBtn');
        const logoutBtn = document.getElementById('sessionLogoutBtn');

        // Show modal
        const bsModal = new bootstrap.Modal(modal);
        bsModal.show();

        // Start countdown
        let timeLeft = 5 * 60; // 5 minutes in seconds
        const countdownInterval = setInterval(() => {
            timeLeft--;
            const minutes = Math.floor(timeLeft / 60);
            const seconds = timeLeft % 60;
            countdown.textContent = `${minutes}:${seconds.toString().padStart(2, '0')}`;

            if (timeLeft <= 0) {
                clearInterval(countdownInterval);
                bsModal.hide();
                UI.showError('Session expired due to inactivity');
                this.logout();
            }
        }, 1000);

        // Handle extend session
        extendBtn.addEventListener('click', () => {
            clearInterval(countdownInterval);
            bsModal.hide();
            this.resetSessionTimeout();
            
            // Make a keep-alive request
            API.keepAlive().then(() => {
                UI.showSuccess('Session extended successfully');
            }).catch(() => {
                UI.showError('Failed to extend session. Please login again.');
                this.logout();
            });
        });

        // Handle logout
        logoutBtn.addEventListener('click', () => {
            clearInterval(countdownInterval);
            bsModal.hide();
            this.logout();
        });

        // Clean up when modal is hidden
        modal.addEventListener('hidden.bs.modal', () => {
            clearInterval(countdownInterval);
            modal.remove();
        });
    }

    clearSessionTimeout() {
        if (this.sessionTimeoutId) {
            clearTimeout(this.sessionTimeoutId);
            this.sessionTimeoutId = null;
        }
        if (this.warningTimeoutId) {
            clearTimeout(this.warningTimeoutId);
            this.warningTimeoutId = null;
        }
    }

    // UI updates
    updateUIForAuthenticatedUser() {
        const user = this.getUser();
        const userMenu = document.getElementById('userMenu');
        const loginMenu = document.getElementById('loginMenu');
        const userName = document.getElementById('userName');
        const userFullName = document.getElementById('userFullName');
        const userEmail = document.getElementById('userEmail');
        const userRole = document.getElementById('userRole');

        if (userMenu) userMenu.style.display = 'block';
        if (loginMenu) loginMenu.style.display = 'none';
        
        if (user) {
            const displayName = user.firstName || user.email || 'User';
            const fullName = user.firstName && user.lastName ? 
                `${user.firstName} ${user.lastName}` : displayName;
            
            if (userName) userName.textContent = displayName;
            if (userFullName) userFullName.textContent = fullName;
            if (userEmail) userEmail.textContent = user.email || '';
            if (userRole) userRole.textContent = `(${user.role || 'Staff'})`;
        }

        // Show/hide elements based on user role
        this.updateRoleBasedUI(user);
        
        // Initialize user menu event listeners
        this.initializeUserMenuEvents();
    }

    updateUIForUnauthenticatedUser() {
        const userMenu = document.getElementById('userMenu');
        const loginMenu = document.getElementById('loginMenu');

        if (userMenu) userMenu.style.display = 'none';
        if (loginMenu) loginMenu.style.display = 'block';
    }

    updateRoleBasedUI(user) {
        if (!user) {
            // Hide all role-based elements if no user
            const roleElements = document.querySelectorAll('[data-role]');
            roleElements.forEach(element => {
                element.style.display = 'none';
            });
            return;
        }

        const userRole = user.role || user.roles;
        console.log('User role for UI update:', userRole, 'User object:', user); // Debug log
        
        // Normalize role to handle both string and numeric values
        const normalizedRole = this.normalizeRole(userRole);
        
        // Handle admin-only elements
        const adminElements = document.querySelectorAll('[data-role="admin"]');
        adminElements.forEach(element => {
            const shouldShow = this.hasRoleAccess(normalizedRole, 'Admin');
            element.style.display = shouldShow ? '' : 'none';
            console.log('Admin element:', element, 'Show:', shouldShow); // Debug log
        });

        // Handle manager-only elements (admin and manager can see)
        const managerElements = document.querySelectorAll('[data-role="manager"]');
        managerElements.forEach(element => {
            const shouldShow = this.hasRoleAccess(normalizedRole, 'Manager');
            element.style.display = shouldShow ? '' : 'none';
            console.log('Manager element:', element, 'Show:', shouldShow); // Debug log
        });

        // Handle staff-only elements (all authenticated users can see)
        const staffElements = document.querySelectorAll('[data-role="staff"]');
        staffElements.forEach(element => {
            element.style.display = '';
        });

        // Update user role display in UI
        const userRoleElements = document.querySelectorAll('[data-user-role]');
        userRoleElements.forEach(element => {
            element.textContent = normalizedRole;
        });
    }

    // Normalize role to handle different formats
    normalizeRole(role) {
        if (typeof role === 'number') {
            const roleMap = { 1: 'Staff', 2: 'Manager', 3: 'Admin' };
            return roleMap[role] || 'Staff';
        }
        return role || 'Staff';
    }

    // Check if user has access to features for a specific role level
    hasRoleAccess(userRole, requiredRole) {
        const roleHierarchy = { 'Staff': 1, 'Manager': 2, 'Admin': 3 };
        const userLevel = roleHierarchy[userRole] || 1;
        const requiredLevel = roleHierarchy[requiredRole] || 1;
        return userLevel >= requiredLevel;
    }

    // Initialize SignalR after successful authentication
    async initializeSignalRAfterAuth() {
        try {
            if (typeof SignalRManager !== 'undefined') {
                // Disconnect existing connection if any
                if (window.signalRManager) {
                    await window.signalRManager.disconnect();
                }
                
                // Create new SignalR connection with fresh token
                window.signalRManager = new SignalRManager();
                await window.signalRManager.initialize();
                console.log('SignalR initialized after authentication');
            }
        } catch (error) {
            console.error('Failed to initialize SignalR after authentication:', error);
            // Don't block the login process if SignalR fails
        }
    }

    getUserRoles(user) {
        if (!user) return [];
        const role = user.role || user.roles;
        return Array.isArray(role) ? role : [role];
    }

    // Check if user has specific role
    hasRole(role) {
        const user = this.getUser();
        if (!user || !user.roles) return false;
        
        const roles = Array.isArray(user.roles) ? user.roles : [user.roles];
        return roles.includes(role);
    }

    // Check if user is authenticated
    isAuthenticated() {
        const token = this.getToken();
        return token && !this.isTokenExpired(token);
    }

    // Require authentication for protected pages
    requireAuth() {
        if (!this.isAuthenticated()) {
            // Store intended URL for redirect after login
            sessionStorage.setItem('intended_url', window.location.pathname + window.location.search);
            window.location.href = '/login';
            return false;
        }
        return true;
    }

    // Initialize logout button
    initializeLogoutButton() {
        const logoutBtn = document.getElementById('logoutBtn');
        if (logoutBtn) {
            logoutBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.logout();
            });
        }
    }

    // Initialize user menu event listeners
    initializeUserMenuEvents() {
        // Profile link
        const profileLink = document.getElementById('profileLink');
        if (profileLink) {
            profileLink.addEventListener('click', (e) => {
                e.preventDefault();
                this.showUserProfile();
            });
        }

        // Change password link
        const changePasswordLink = document.getElementById('changePasswordLink');
        if (changePasswordLink) {
            changePasswordLink.addEventListener('click', (e) => {
                e.preventDefault();
                this.showChangePasswordModal();
            });
        }

        // User management link (admin only)
        const userManagementLink = document.getElementById('userManagementLink');
        if (userManagementLink) {
            userManagementLink.addEventListener('click', (e) => {
                e.preventDefault();
                window.location.href = '/admin/users';
            });
        }

        // Settings link
        const settingsLink = document.getElementById('settingsLink');
        if (settingsLink) {
            settingsLink.addEventListener('click', (e) => {
                e.preventDefault();
                this.showSettingsModal();
            });
        }

        // Help link
        const helpLink = document.getElementById('helpLink');
        if (helpLink) {
            helpLink.addEventListener('click', (e) => {
                e.preventDefault();
                this.showHelpModal();
            });
        }
    }

    // Show user profile modal
    showUserProfile() {
        const user = this.getUser();
        if (!user) return;

        const modalHtml = `
            <div class="modal fade" id="userProfileModal" tabindex="-1">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">
                                <i class="bi bi-person-circle me-2"></i>
                                User Profile
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <div class="row">
                                <div class="col-md-4 text-center mb-3">
                                    <i class="bi bi-person-circle display-1 text-primary"></i>
                                    <h6 class="mt-2">${user.firstName || ''} ${user.lastName || ''}</h6>
                                    <small class="text-muted">${user.role || 'Staff'}</small>
                                </div>
                                <div class="col-md-8">
                                    <div class="mb-3">
                                        <label class="form-label fw-semibold">Email</label>
                                        <div class="form-control-plaintext">${user.email || 'N/A'}</div>
                                    </div>
                                    <div class="mb-3">
                                        <label class="form-label fw-semibold">Role</label>
                                        <div class="form-control-plaintext">
                                            <span class="badge bg-primary">${user.role || 'Staff'}</span>
                                        </div>
                                    </div>
                                    <div class="mb-3">
                                        <label class="form-label fw-semibold">Account Status</label>
                                        <div class="form-control-plaintext">
                                            <span class="badge bg-success">Active</span>
                                        </div>
                                    </div>
                                    ${user.accessibleHotelIds && user.accessibleHotelIds.length > 0 ? `
                                    <div class="mb-3">
                                        <label class="form-label fw-semibold">Accessible Properties</label>
                                        <div class="form-control-plaintext">
                                            <small class="text-muted">${user.accessibleHotelIds.length} properties</small>
                                        </div>
                                    </div>
                                    ` : ''}
                                </div>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                            <button type="button" class="btn btn-primary" id="editProfileBtn">
                                <i class="bi bi-pencil me-1"></i>
                                Edit Profile
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Remove existing modal
        const existingModal = document.getElementById('userProfileModal');
        if (existingModal) existingModal.remove();

        // Add modal to DOM and show
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        const modal = new bootstrap.Modal(document.getElementById('userProfileModal'));
        modal.show();

        // Handle edit profile button
        document.getElementById('editProfileBtn').addEventListener('click', () => {
            modal.hide();
            UI.showToast('Profile editing will be implemented in a future update', 'info');
        });

        // Clean up when modal is hidden
        document.getElementById('userProfileModal').addEventListener('hidden.bs.modal', function() {
            this.remove();
        });
    }

    // Show change password modal
    showChangePasswordModal() {
        const modalHtml = `
            <div class="modal fade" id="changePasswordModal" tabindex="-1">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">
                                <i class="bi bi-key me-2"></i>
                                Change Password
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <form id="changePasswordForm" class="ajax-form">
                            <div class="modal-body">
                                <div class="mb-3">
                                    <label for="currentPassword" class="form-label">Current Password</label>
                                    <input type="password" class="form-control" id="currentPassword" name="currentPassword" required>
                                    <div class="invalid-feedback"></div>
                                </div>
                                <div class="mb-3">
                                    <label for="newPassword" class="form-label">New Password</label>
                                    <input type="password" class="form-control" id="newPassword" name="newPassword" required minlength="6">
                                    <div class="form-text">Password must be at least 6 characters long</div>
                                    <div class="invalid-feedback"></div>
                                </div>
                                <div class="mb-3">
                                    <label for="confirmPassword" class="form-label">Confirm New Password</label>
                                    <input type="password" class="form-control" id="confirmPassword" name="confirmPassword" required>
                                    <div class="invalid-feedback"></div>
                                </div>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                                <button type="submit" class="btn btn-primary">
                                    <i class="bi bi-check-lg me-1"></i>
                                    Change Password
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        `;

        // Remove existing modal
        const existingModal = document.getElementById('changePasswordModal');
        if (existingModal) existingModal.remove();

        // Add modal to DOM and show
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        const modal = new bootstrap.Modal(document.getElementById('changePasswordModal'));
        modal.show();

        // Handle form submission
        document.getElementById('changePasswordForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            await this.handleChangePassword(e.target, modal);
        });

        // Clean up when modal is hidden
        document.getElementById('changePasswordModal').addEventListener('hidden.bs.modal', function() {
            this.remove();
        });
    }

    // Handle change password form submission
    async handleChangePassword(form, modal) {
        const formData = new FormData(form);
        const currentPassword = formData.get('currentPassword');
        const newPassword = formData.get('newPassword');
        const confirmPassword = formData.get('confirmPassword');

        // Clear previous validation
        form.querySelectorAll('.is-invalid').forEach(input => {
            input.classList.remove('is-invalid');
        });

        // Validate passwords match
        if (newPassword !== confirmPassword) {
            const confirmInput = form.querySelector('#confirmPassword');
            confirmInput.classList.add('is-invalid');
            confirmInput.nextElementSibling.textContent = 'Passwords do not match';
            return;
        }

        try {
            UI.showLoading('Changing password...');
            
            const response = await API.request('/auth/change-password', {
                method: 'POST',
                body: JSON.stringify({
                    currentPassword,
                    newPassword
                })
            });

            modal.hide();
            UI.showSuccess('Password changed successfully');
        } catch (error) {
            const errorMessage = error.message || 'Failed to change password';
            
            if (errorMessage.includes('current password')) {
                const currentInput = form.querySelector('#currentPassword');
                currentInput.classList.add('is-invalid');
                currentInput.nextElementSibling.textContent = 'Current password is incorrect';
            } else {
                UI.showError(errorMessage);
            }
        } finally {
            UI.hideLoading();
        }
    }

    // Show settings modal
    showSettingsModal() {
        const currentTheme = localStorage.getItem(this.themeKey) || 'auto';
        const currentCurrency = localStorage.getItem(this.currencyKey) || 'EUR';
        const modalHtml = `
            <div class="modal fade" id="settingsModal" tabindex="-1">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">
                                <i class="bi bi-gear me-2"></i>
                                ${window._I18N?.Settings_Title || 'Settings'}
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <div class="mb-3">
                                <h6>${window._I18N?.Settings_Session || 'Session Settings'}</h6>
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="rememberMeSetting" ${localStorage.getItem('remember_me') ? 'checked' : ''}>
                                    <label class="form-check-label" for="rememberMeSetting">
                                        ${window._I18N?.Settings_RememberDevice || 'Remember me on this device'}
                                    </label>
                                </div>
                            </div>
                            <div class="mb-3">
                                <h6>${window._I18N?.Settings_Notifications || 'Notifications'}</h6>
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="browserNotifications" checked>
                                    <label class="form-check-label" for="browserNotifications">
                                        ${window._I18N?.Settings_EnableBrowserNotifications || 'Enable browser notifications'}
                                    </label>
                                </div>
                            </div>
                            <div class="mb-3">
                                <h6>${window._I18N?.Settings_Theme || 'Theme'}</h6>
                                <select class="form-select" id="themeSelect">
                                    <option value="light" ${currentTheme === 'light' ? 'selected' : ''}>${window._I18N?.Settings_ThemeLight || 'Light'}</option>
                                    <option value="dark" ${currentTheme === 'dark' ? 'selected' : ''}>${window._I18N?.Settings_ThemeDark || 'Dark'}</option>
                                    <option value="auto" ${currentTheme === 'auto' ? 'selected' : ''}>${window._I18N?.Settings_ThemeAuto || 'Auto (System)'}</option>
                                </select>
                            </div>
                            <div class="mb-3">
                                <h6>${window._I18N?.Settings_Currency || 'Currency'}</h6>
                                <select class="form-select" id="currencySelect">
                                    <option value="EUR" ${currentCurrency === 'EUR' ? 'selected' : ''}>EUR (€)</option>
                                    <option value="USD" ${currentCurrency === 'USD' ? 'selected' : ''}>USD ($)</option>
                                    <option value="GBP" ${currentCurrency === 'GBP' ? 'selected' : ''}>GBP (£)</option>
                                </select>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">${window._I18N?.Common_Close || 'Close'}</button>
                            <button type="button" class="btn btn-primary" id="saveSettingsBtn">
                                <i class="bi bi-check-lg me-1"></i>
                                ${window._I18N?.Settings_Save || 'Save Settings'}
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Remove existing modal
        const existingModal = document.getElementById('settingsModal');
        if (existingModal) existingModal.remove();

        // Add modal to DOM and show
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        const modal = new bootstrap.Modal(document.getElementById('settingsModal'));
        modal.show();

        // Handle save settings
        document.getElementById('saveSettingsBtn').addEventListener('click', () => {
            const rememberMe = document.getElementById('rememberMeSetting').checked;
            const theme = document.getElementById('themeSelect').value;
            const currency = document.getElementById('currencySelect').value;
            if (rememberMe) {
                localStorage.setItem('remember_me', 'true');
            } else {
                localStorage.removeItem('remember_me');
            }
            localStorage.setItem(this.themeKey, theme);
            localStorage.setItem(this.currencyKey, currency);
            localStorage.setItem(this.localeKey, window.__hotelLocale || 'en-US');
            this.applyAppearanceSettings();
            
            modal.hide();
            UI.showSuccess(window._I18N?.Settings_Saved || 'Settings saved successfully');
        });

        // Clean up when modal is hidden
        document.getElementById('settingsModal').addEventListener('hidden.bs.modal', function() {
            this.remove();
        });
    }

    // Show help modal
    showHelpModal() {
        const modalHtml = `
            <div class="modal fade" id="helpModal" tabindex="-1">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">
                                <i class="bi bi-question-circle me-2"></i>
                                Help & Support
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <div class="row">
                                <div class="col-md-6">
                                    <h6><i class="bi bi-book me-2"></i>Quick Start Guide</h6>
                                    <ul class="list-unstyled">
                                        <li><a href="#" class="text-decoration-none">Getting Started</a></li>
                                        <li><a href="#" class="text-decoration-none">Managing Reservations</a></li>
                                        <li><a href="#" class="text-decoration-none">Using the Calendar</a></li>
                                        <li><a href="#" class="text-decoration-none">Property Management</a></li>
                                    </ul>
                                </div>
                                <div class="col-md-6">
                                    <h6><i class="bi bi-headset me-2"></i>Support</h6>
                                    <p class="small text-muted">Need help? Contact your system administrator or IT support team.</p>
                                    <div class="d-grid gap-2">
                                        <button class="btn btn-outline-primary btn-sm" onclick="window.open('mailto:support@hotel-system.com')">
                                            <i class="bi bi-envelope me-1"></i>
                                            Email Support
                                        </button>
                                        <button class="btn btn-outline-secondary btn-sm" onclick="UI.showToast('Phone support: +1-800-HOTEL-SYS', 'info', 5000)">
                                            <i class="bi bi-telephone me-1"></i>
                                            Phone Support
                                        </button>
                                    </div>
                                </div>
                            </div>
                            <hr>
                            <div class="text-center">
                                <small class="text-muted">
                                    Hotel Reservation System v1.0 | 
                                    <a href="#" class="text-decoration-none">Privacy Policy</a> | 
                                    <a href="#" class="text-decoration-none">Terms of Service</a>
                                </small>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Remove existing modal
        const existingModal = document.getElementById('helpModal');
        if (existingModal) existingModal.remove();

        // Add modal to DOM and show
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        const modal = new bootstrap.Modal(document.getElementById('helpModal'));
        modal.show();

        // Clean up when modal is hidden
        document.getElementById('helpModal').addEventListener('hidden.bs.modal', function() {
            this.remove();
        });
    }
}

// Create global Auth instance
window.Auth = new AuthManager();

// Initialize logout button when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    Auth.initializeLogoutButton();
    Auth.initializeTwoFactorChallengeHandlers();
    Auth.initializeTwoFactorSetupHandlers();
});
