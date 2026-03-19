// Main site JavaScript - initialization and global functionality
document.addEventListener('DOMContentLoaded', function() {
    // Initialize the application
    initializeApp();
});

function initializeApp() {
    // Initialize global modules first
    initializeGlobalModules();
    
    // Initialize error monitoring and connection monitoring
    UI.initializeErrorBoundary();
    UI.initializeConnectionMonitoring();
    
    // Update navigation active states
    UI.updateActiveNavigation();
    
    // Initialize global event listeners
    initializeGlobalEventListeners();
    
    // Initialize SignalR connection for real-time updates (only if authenticated)
    if (Auth && Auth.isAuthenticated && Auth.isAuthenticated()) {
        initializeSignalR();
    }
    
    // Initialize page-specific functionality
    initializePageSpecificFeatures();
    
    // Setup periodic token refresh check
    setupPeriodicChecks();

    // Initialize mobile features (Task 10)
    if (typeof MobileManager !== 'undefined') {
        window.mobileManager = new MobileManager();
        window.mobileManager.initialize();
    }
}

async function initializeSignalR() {
    try {
        // Initialize SignalR manager if it exists
        if (typeof SignalRManager !== 'undefined') {
            window.signalRManager = new SignalRManager();
            await window.signalRManager.initialize();
            console.log('SignalR initialized successfully');
        }
    } catch (error) {
        console.error('Failed to initialize SignalR:', error);
        // Don't block the app if SignalR fails
    }
}

// Initialize global modules
function initializeGlobalModules() {
    // Initialize Auth module
    if (typeof AuthManager !== 'undefined') {
        window.Auth = new AuthManager();
        console.log('Auth module initialized');
    }
    
    // Initialize API client
    if (typeof ApiClient !== 'undefined') {
        window.API = new ApiClient();
        console.log('API client initialized');
    }
}

function initializeGlobalEventListeners() {
    // Handle navigation clicks
    document.addEventListener('click', function(e) {
        // Handle navigation links
        if (e.target.matches('.nav-link[data-section]')) {
            e.preventDefault(); // Prevent default link navigation
            const section = e.target.getAttribute('data-section');
            navigateToSection(section);
        }
        
        // Handle logout button
        if (e.target.matches('#logoutBtn')) {
            e.preventDefault();
            Auth.logout();
        }
        
        // Handle modal triggers
        if (e.target.matches('[data-bs-toggle="modal"]')) {
            const targetModal = e.target.getAttribute('data-bs-target');
            if (targetModal) {
                UI.showModal(targetModal.substring(1));
            }
        }
    });

    // Handle form submissions
    document.addEventListener('submit', function(e) {
        // Prevent default form submission for AJAX forms
        if (e.target.matches('.ajax-form')) {
            e.preventDefault();
            handleAjaxForm(e.target);
        }
    });

    // Handle input validation
    document.addEventListener('input', function(e) {
        if (e.target.matches('input.is-invalid, select.is-invalid, textarea.is-invalid')) {
            UI.clearFieldError(e.target);
        }
    });

    // Handle window resize for responsive features
    window.addEventListener('resize', UI.throttle(function() {
        handleWindowResize();
    }, 250));

    // Handle browser back/forward buttons
    window.addEventListener('popstate', function(e) {
        UI.updateActiveNavigation();
    });
}

function initializePageSpecificFeatures() {
    const currentPath = window.location.pathname;
    
    // Login page - no authentication required
    if (currentPath.includes('/login')) {
        initializeLogin();
        return;
    }
    
    // All other pages require authentication
    if (!Auth.requireAuth()) {
        return; // User will be redirected to login
    }
    
    // Dashboard page
    if (currentPath === '/' || currentPath === '/dashboard') {
        initializeDashboard();
    }
    
    // Calendar page
    else if (currentPath.includes('/calendar')) {
        initializeCalendar();
    }
    
    // Properties page
    else if (currentPath.includes('/properties')) {
        initializeProperties();
    }
    
    // Reports page
    else if (currentPath.includes('/reports')) {
        initializeReports();
    }
    
    // Reservations page
    else if (currentPath.includes('/reservations')) {
        initializeReservations();
    }
}

function navigateToSection(section) {
    console.log('Navigating to section:', section);
    
    const routes = {
        'dashboard': '/',
        'calendar': '/Home/Calendar',
        'properties': '/Home/Properties',
        'reservations': '/Home/Reservations',
        'reports': '/Home/Reports'
    };
    
    const url = routes[section];
    console.log('Target URL:', url);
    
    if (url) {
        // Check authentication for protected routes
        const isAuth = Auth && Auth.isAuthenticated ? Auth.isAuthenticated() : true;
        console.log('Is authenticated:', isAuth);
        
        if (section !== 'login' && !isAuth) {
            console.log('Not authenticated, redirecting to login');
            sessionStorage.setItem('intended_url', url);
            window.location.href = '/login';
            return;
        }
        
        console.log('Navigating to:', url);
        window.location.href = url;
    } else {
        console.log('No route found for section:', section);
    }
}

function handleAjaxForm(form) {
    if (!UI.validateFormWithFeedback(form)) {
        return;
    }
    
    const formData = new FormData(form);
    const data = Object.fromEntries(formData.entries());
    const method = form.getAttribute('method') || 'POST';
    
    // Handle different form types with enhanced error handling
    try {
        if (form.classList.contains('login-form')) {
            handleLoginForm(data);
        } else if (form.classList.contains('reservation-form')) {
            handleReservationForm(data, method, form);
        } else if (form.classList.contains('hotel-form')) {
            handleHotelForm(data, method, form);
        } else if (form.classList.contains('room-form')) {
            handleRoomForm(data, method, form);
        }
    } catch (error) {
        UI.handleFormError(error, form);
    }
}

async function handleLoginForm(data) {
    const success = await Auth.login(data);
    if (success) {
        // Redirect will be handled by Auth.login
    }
}

function handleWindowResize() {
    // Update responsive elements
    const width = window.innerWidth;
    
    // Update navigation for mobile
    if (width < 992) {
        // Mobile-specific adjustments
        document.body.classList.add('mobile-view');
    } else {
        document.body.classList.remove('mobile-view');
    }
    
    // Trigger resize events for charts and calendars
    window.dispatchEvent(new Event('resize-complete'));
}

function setupPeriodicChecks() {
    // Check authentication status every 5 minutes
    setInterval(() => {
        if (Auth && Auth.isAuthenticated && Auth.isAuthenticated()) {
            // Ping server to keep session alive
            if (API && API.keepAlive) {
                API.keepAlive().catch(() => {
                    // If keep-alive fails, token might be invalid
                    console.log('Keep-alive failed, logging out');
                    Auth.logout();
                });
            }
        }
    }, 5 * 60 * 1000);
    
    // Notification system will be initialized automatically via notifications.js module
}

async function checkForNotifications() {
    try {
        // This would be implemented when notification system is ready
        // const notifications = await API.getNotifications();
        // updateNotificationBadge(notifications);
    } catch (error) {
        // Silently handle notification check errors
        console.debug('Notification check failed:', error);
    }
}

// Page-specific initialization functions
function initializeDashboard() {
    console.log('Dashboard page initialized');
    // Dashboard initialization is handled by the view-specific script
    // This function is called when navigating to dashboard via SPA-style navigation
}

function initializeCalendar() {
    console.log('Calendar page initialized');
    // Calendar initialization is handled by the view-specific script
    // This function is called when navigating to calendar via SPA-style navigation
}

function initializeProperties() {
    console.log('Properties page initialized');
    // Properties-specific initialization will be implemented later
}

function initializeReservations() {
    console.log('Reservations page initialized');
    // Reservations-specific initialization will be implemented in task 10
}

function initializeReports() {
    console.log('Reports page initialized');
    // Reports initialization is handled by the view-specific script
    // This function is called when navigating to reports via SPA-style navigation
}

function initializeLogin() {
    console.log('Login page initialized');
    // Login-specific initialization will be implemented in task 8.2
}

// Utility functions
function formatDateForInput(date) {
    if (!date) return '';
    const d = new Date(date);
    return d.toISOString().split('T')[0];
}

function formatTimeForInput(date) {
    if (!date) return '';
    const d = new Date(date);
    return d.toTimeString().split(' ')[0].substring(0, 5);
}

function parseDateTime(dateStr, timeStr) {
    if (!dateStr) return null;
    const date = new Date(dateStr);
    if (timeStr) {
        const [hours, minutes] = timeStr.split(':');
        date.setHours(parseInt(hours), parseInt(minutes));
    }
    return date;
}

// Export functions for use in other modules
window.AppUtils = {
    formatDateForInput,
    formatTimeForInput,
    parseDateTime,
    navigateToSection
};