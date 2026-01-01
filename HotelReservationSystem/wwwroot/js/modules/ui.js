// UI utility module for loading states and notifications
class UIManager {
    constructor() {
        this.loadingOverlay = document.getElementById('loadingOverlay');
        this.errorAlert = document.getElementById('errorAlert');
        this.successAlert = document.getElementById('successAlert');
        this.errorMessage = document.getElementById('errorMessage');
        this.successMessage = document.getElementById('successMessage');
        
        this.loadingCount = 0;
        this.initializeEventListeners();
    }

    initializeEventListeners() {
        // Auto-hide alerts after 5 seconds
        document.addEventListener('DOMContentLoaded', () => {
            const alerts = document.querySelectorAll('.alert');
            alerts.forEach(alert => {
                setTimeout(() => {
                    if (alert.classList.contains('show')) {
                        this.hideAlert(alert);
                    }
                }, 5000);
            });
        });

        // Handle navigation active states
        this.updateActiveNavigation();
        
        // Handle responsive navigation
        this.initializeResponsiveNavigation();
    }

    // Loading state management
    showLoading(message = 'Loading...') {
        this.loadingCount++;
        if (this.loadingOverlay) {
            const loadingText = this.loadingOverlay.querySelector('.mt-2');
            if (loadingText) {
                loadingText.textContent = message;
            }
            this.loadingOverlay.style.display = 'flex';
        }
    }

    hideLoading() {
        this.loadingCount = Math.max(0, this.loadingCount - 1);
        if (this.loadingCount === 0 && this.loadingOverlay) {
            this.loadingOverlay.style.display = 'none';
        }
    }

    // Alert management
    showError(message, duration = 5000, errorDetails = null) {
        if (this.errorMessage && this.errorAlert) {
            // Handle enhanced error object
            if (typeof message === 'object' && message.message) {
                const error = message;
                this.displayEnhancedError(error);
            } else {
                this.errorMessage.textContent = message;
                this.errorAlert.style.display = 'block';
                this.errorAlert.classList.add('show');
            }
            
            if (duration > 0) {
                setTimeout(() => this.hideError(), duration);
            }
        }
        console.error('UI Error:', message, errorDetails);
    }

    displayEnhancedError(error) {
        const errorContainer = this.errorAlert || document.getElementById('errorAlert');
        if (!errorContainer) return;

        // Create enhanced error display
        const errorHtml = `
            <div class="d-flex align-items-start">
                <div class="flex-grow-1">
                    <strong>Error:</strong> ${error.message}
                    ${error.details ? `<br><small class="text-muted">Details: ${error.details}</small>` : ''}
                    ${error.traceId ? `<br><small class="text-muted">Trace ID: ${error.traceId}</small>` : ''}
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>
        `;

        errorContainer.innerHTML = errorHtml;
        errorContainer.style.display = 'block';
        errorContainer.classList.add('show');

        // Add click handler for close button
        const closeBtn = errorContainer.querySelector('.btn-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => this.hideError());
        }
    }

    showSuccess(message, duration = 3000) {
        if (this.successMessage && this.successAlert) {
            this.successMessage.textContent = message;
            this.successAlert.style.display = 'block';
            this.successAlert.classList.add('show');
            
            if (duration > 0) {
                setTimeout(() => this.hideSuccess(), duration);
            }
        }
    }

    showInfo(message, duration = 4000) {
        // Use success alert for info messages with different styling
        if (this.successMessage && this.successAlert) {
            this.successMessage.textContent = message;
            this.successAlert.style.display = 'block';
            this.successAlert.classList.add('show');
            
            if (duration > 0) {
                setTimeout(() => this.hideSuccess(), duration);
            }
        }
    }

    hideError() {
        this.hideAlert(this.errorAlert);
    }

    hideSuccess() {
        this.hideAlert(this.successAlert);
    }

    hideAlert(alert) {
        if (alert) {
            alert.classList.remove('show');
            setTimeout(() => {
                alert.style.display = 'none';
            }, 150);
        }
    }

    // Navigation management
    updateActiveNavigation() {
        const currentPath = window.location.pathname;
        const navLinks = document.querySelectorAll('.nav-link[data-section]');
        
        navLinks.forEach(link => {
            link.classList.remove('active');
            const section = link.getAttribute('data-section');
            
            if ((currentPath === '/' && section === 'dashboard') ||
                currentPath.includes(`/${section}`)) {
                link.classList.add('active');
            }
        });
    }

    // Responsive navigation
    initializeResponsiveNavigation() {
        const navbarToggler = document.querySelector('.navbar-toggler');
        const navbarCollapse = document.querySelector('.navbar-collapse');
        
        if (navbarToggler && navbarCollapse) {
            // Close mobile menu when clicking on nav links
            const navLinks = navbarCollapse.querySelectorAll('.nav-link');
            navLinks.forEach(link => {
                link.addEventListener('click', () => {
                    if (window.innerWidth < 992) {
                        const bsCollapse = new bootstrap.Collapse(navbarCollapse, {
                            toggle: false
                        });
                        bsCollapse.hide();
                    }
                });
            });
        }
    }

    // Modal management
    showModal(modalId, options = {}) {
        const modalElement = document.getElementById(modalId);
        if (modalElement) {
            const modal = new bootstrap.Modal(modalElement, options);
            modal.show();
            return modal;
        }
        return null;
    }

    hideModal(modalId) {
        const modalElement = document.getElementById(modalId);
        if (modalElement) {
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            }
        }
    }

    // Form validation helpers
    validateForm(formElement) {
        if (!formElement) return false;
        
        const inputs = formElement.querySelectorAll('input[required], select[required], textarea[required]');
        let isValid = true;
        
        inputs.forEach(input => {
            if (!input.value.trim()) {
                this.showFieldError(input, 'This field is required');
                isValid = false;
            } else {
                this.clearFieldError(input);
            }
        });
        
        return isValid;
    }

    showFieldError(input, message) {
        input.classList.add('is-invalid');
        
        let feedback = input.parentNode.querySelector('.invalid-feedback');
        if (!feedback) {
            feedback = document.createElement('div');
            feedback.className = 'invalid-feedback';
            input.parentNode.appendChild(feedback);
        }
        feedback.textContent = message;
    }

    clearFieldError(input) {
        input.classList.remove('is-invalid');
        const feedback = input.parentNode.querySelector('.invalid-feedback');
        if (feedback) {
            feedback.remove();
        }
    }

    // Table helpers
    createDataTable(tableId, options = {}) {
        const table = document.getElementById(tableId);
        if (!table) return null;

        const defaultOptions = {
            responsive: true,
            pageLength: 25,
            order: [[0, 'desc']],
            language: {
                search: 'Search:',
                lengthMenu: 'Show _MENU_ entries',
                info: 'Showing _START_ to _END_ of _TOTAL_ entries',
                paginate: {
                    first: 'First',
                    last: 'Last',
                    next: 'Next',
                    previous: 'Previous'
                }
            }
        };

        return $(table).DataTable({ ...defaultOptions, ...options });
    }

    // Utility functions
    formatDate(date, options = {}) {
        const defaultOptions = {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        };
        
        return new Date(date).toLocaleDateString('en-US', { ...defaultOptions, ...options });
    }

    formatCurrency(amount, currency = 'USD') {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: currency
        }).format(amount);
    }

    formatNumber(number, options = {}) {
        return new Intl.NumberFormat('en-US', options).format(number);
    }

    // Animation helpers
    fadeIn(element, duration = 300) {
        element.style.opacity = '0';
        element.style.display = 'block';
        
        let start = null;
        const animate = (timestamp) => {
            if (!start) start = timestamp;
            const progress = timestamp - start;
            
            element.style.opacity = Math.min(progress / duration, 1);
            
            if (progress < duration) {
                requestAnimationFrame(animate);
            }
        };
        
        requestAnimationFrame(animate);
    }

    fadeOut(element, duration = 300) {
        let start = null;
        const animate = (timestamp) => {
            if (!start) start = timestamp;
            const progress = timestamp - start;
            
            element.style.opacity = Math.max(1 - (progress / duration), 0);
            
            if (progress < duration) {
                requestAnimationFrame(animate);
            } else {
                element.style.display = 'none';
            }
        };
        
        requestAnimationFrame(animate);
    }

    // Debounce utility
    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // Throttle utility
    throttle(func, limit) {
        let inThrottle;
        return function() {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    }

    // Enhanced loading states for specific operations
    showLoadingForElement(element, message = 'Loading...') {
        if (!element) return;
        
        const originalContent = element.innerHTML;
        element.setAttribute('data-original-content', originalContent);
        element.innerHTML = `
            <div class="d-flex align-items-center justify-content-center py-3">
                <div class="spinner-border spinner-border-sm me-2" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <span>${message}</span>
            </div>
        `;
        element.style.pointerEvents = 'none';
    }

    hideLoadingForElement(element) {
        if (!element) return;
        
        const originalContent = element.getAttribute('data-original-content');
        if (originalContent) {
            element.innerHTML = originalContent;
            element.removeAttribute('data-original-content');
        }
        element.style.pointerEvents = '';
    }

    // Toast notifications for better UX
    showToast(message, type = 'info', duration = 3000) {
        const toastContainer = this.getOrCreateToastContainer();
        const toastId = 'toast-' + Date.now();
        
        const toastHtml = `
            <div class="toast align-items-center text-bg-${type} border-0" role="alert" id="${toastId}">
                <div class="d-flex">
                    <div class="toast-body">
                        ${message}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            </div>
        `;
        
        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement, { delay: duration });
        toast.show();
        
        // Remove toast element after it's hidden
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });
    }

    getOrCreateToastContainer() {
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.className = 'toast-container position-fixed top-0 end-0 p-3';
            container.style.zIndex = '1055';
            document.body.appendChild(container);
        }
        return container;
    }

    // Enhanced form validation with better UX
    validateFormWithFeedback(formElement) {
        if (!formElement) return false;
        
        const inputs = formElement.querySelectorAll('input[required], select[required], textarea[required]');
        let isValid = true;
        let firstInvalidInput = null;
        
        inputs.forEach(input => {
            const isInputValid = this.validateInput(input);
            if (!isInputValid && !firstInvalidInput) {
                firstInvalidInput = input;
            }
            isValid = isValid && isInputValid;
        });
        
        // Focus on first invalid input
        if (firstInvalidInput) {
            firstInvalidInput.focus();
            firstInvalidInput.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
        
        return isValid;
    }

    validateInput(input) {
        const value = input.value.trim();
        const type = input.type;
        let isValid = true;
        let message = '';
        
        // Required field validation
        if (input.hasAttribute('required') && !value) {
            isValid = false;
            message = 'This field is required';
        }
        // Email validation
        else if (type === 'email' && value && !this.isValidEmail(value)) {
            isValid = false;
            message = 'Please enter a valid email address';
        }
        // Number validation
        else if (type === 'number' && value) {
            const min = input.getAttribute('min');
            const max = input.getAttribute('max');
            const numValue = parseFloat(value);
            
            if (isNaN(numValue)) {
                isValid = false;
                message = 'Please enter a valid number';
            } else if (min && numValue < parseFloat(min)) {
                isValid = false;
                message = `Value must be at least ${min}`;
            } else if (max && numValue > parseFloat(max)) {
                isValid = false;
                message = `Value must be no more than ${max}`;
            }
        }
        // Date validation
        else if (type === 'date' && value && !this.isValidDate(value)) {
            isValid = false;
            message = 'Please enter a valid date';
        }
        
        if (isValid) {
            this.clearFieldError(input);
        } else {
            this.showFieldError(input, message);
        }
        
        return isValid;
    }

    isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    isValidDate(dateString) {
        const date = new Date(dateString);
        return date instanceof Date && !isNaN(date);
    }

    // Enhanced error handling methods
    handleApiError(error, context = '') {
        console.error(`API Error${context ? ` in ${context}` : ''}:`, error);
        
        // Determine error type and show appropriate message
        if (error.statusCode) {
            switch (error.statusCode) {
                case 400:
                    this.showToast('Invalid request. Please check your input and try again.', 'warning', 5000);
                    break;
                case 401:
                    this.showToast('Authentication required. Please log in.', 'warning', 5000);
                    break;
                case 403:
                    this.showToast('You do not have permission to perform this action.', 'warning', 5000);
                    break;
                case 404:
                    this.showToast('The requested resource was not found.', 'warning', 5000);
                    break;
                case 409:
                    this.showToast('Conflict detected. ' + (error.message || 'Please resolve and try again.'), 'warning', 7000);
                    break;
                case 429:
                    this.showToast('Too many requests. Please wait a moment and try again.', 'warning', 5000);
                    break;
                case 500:
                    this.showToast('Server error occurred. Please try again later.', 'danger', 7000);
                    break;
                case 502:
                case 503:
                    this.showToast('Service temporarily unavailable. Please try again later.', 'warning', 7000);
                    break;
                default:
                    this.showToast(error.message || 'An unexpected error occurred.', 'danger', 5000);
            }
        } else {
            // Network or other errors
            if (error.message.includes('fetch')) {
                this.showToast('Network error. Please check your connection and try again.', 'danger', 7000);
            } else {
                this.showToast(error.message || 'An unexpected error occurred.', 'danger', 5000);
            }
        }

        // Show detailed error in console for debugging
        if (error.traceId) {
            console.error(`Trace ID: ${error.traceId}`);
        }
        if (error.details) {
            console.error(`Details: ${error.details}`);
        }
    }

    // Form submission error handling
    handleFormError(error, formElement) {
        if (error.statusCode === 400 && error.details) {
            try {
                // Try to parse validation errors
                const validationErrors = typeof error.details === 'string' 
                    ? JSON.parse(error.details) 
                    : error.details;
                
                if (typeof validationErrors === 'object') {
                    this.displayValidationErrors(formElement, validationErrors);
                    return;
                }
            } catch (e) {
                // Fall back to general error handling
            }
        }
        
        // General form error handling
        this.handleApiError(error, 'form submission');
    }

    displayValidationErrors(formElement, validationErrors) {
        if (!formElement || !validationErrors) return;

        // Clear existing errors
        const existingErrors = formElement.querySelectorAll('.is-invalid');
        existingErrors.forEach(input => this.clearFieldError(input));

        // Display new validation errors
        Object.keys(validationErrors).forEach(fieldName => {
            const field = formElement.querySelector(`[name="${fieldName}"], #${fieldName}`);
            if (field) {
                const errors = Array.isArray(validationErrors[fieldName]) 
                    ? validationErrors[fieldName] 
                    : [validationErrors[fieldName]];
                this.showFieldError(field, errors.join(', '));
            }
        });

        // Focus on first invalid field
        const firstInvalidField = formElement.querySelector('.is-invalid');
        if (firstInvalidField) {
            firstInvalidField.focus();
            firstInvalidField.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }

    // Retry mechanism for failed operations
    async retryOperation(operation, maxRetries = 3, delay = 1000) {
        let lastError;
        
        for (let attempt = 1; attempt <= maxRetries; attempt++) {
            try {
                return await operation();
            } catch (error) {
                lastError = error;
                
                if (attempt === maxRetries) {
                    break;
                }
                
                // Don't retry on client errors (4xx) except 429
                if (error.statusCode >= 400 && error.statusCode < 500 && error.statusCode !== 429) {
                    break;
                }
                
                // Show retry notification
                this.showToast(`Attempt ${attempt} failed. Retrying in ${delay/1000} seconds...`, 'info', delay);
                
                // Wait before retry
                await new Promise(resolve => setTimeout(resolve, delay));
                
                // Exponential backoff
                delay *= 2;
            }
        }
        
        throw lastError;
    }

    // Connection status monitoring
    initializeConnectionMonitoring() {
        let isOnline = navigator.onLine;
        
        const updateConnectionStatus = () => {
            const wasOnline = isOnline;
            isOnline = navigator.onLine;
            
            if (!wasOnline && isOnline) {
                this.showToast('Connection restored', 'success', 3000);
            } else if (wasOnline && !isOnline) {
                this.showToast('Connection lost. Some features may not work.', 'warning', 0); // Don't auto-hide
            }
        };
        
        window.addEventListener('online', updateConnectionStatus);
        window.addEventListener('offline', updateConnectionStatus);
        
        // Initial status check
        if (!isOnline) {
            this.showToast('No internet connection detected.', 'warning', 0);
        }
    }

    // Error boundary for JavaScript errors
    initializeErrorBoundary() {
        window.addEventListener('error', (event) => {
            console.error('JavaScript Error:', event.error);
            this.showToast('An unexpected error occurred. Please refresh the page if problems persist.', 'danger', 10000);
            
            // Log error details for debugging
            console.error('Error details:', {
                message: event.message,
                filename: event.filename,
                lineno: event.lineno,
                colno: event.colno,
                error: event.error
            });
        });

        window.addEventListener('unhandledrejection', (event) => {
            console.error('Unhandled Promise Rejection:', event.reason);
            this.showToast('An unexpected error occurred. Please try again.', 'danger', 7000);
            
            // Prevent the default browser behavior
            event.preventDefault();
        });
    }
}

// Create global UI instance
window.UI = new UIManager();