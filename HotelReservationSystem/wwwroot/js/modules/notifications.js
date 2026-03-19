// Notification management module
class NotificationManager {
    constructor() {
        this.connection = null;
        this.notifications = [];
        this.unreadCount = 0;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 1000;
        
        this.init();
    }

    async init() {
        try {
            await this.setupSignalRConnection();
            await this.loadInitialNotifications();
            this.setupUI();
            this.requestNotificationPermission();
        } catch (error) {
            console.error('Failed to initialize notification manager:', error);
        }
    }

    async setupSignalRConnection() {
        try {
            // Get JWT token for authentication
            const token = localStorage.getItem('jwt_token');
            
            // Create SignalR connection with authentication
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/reservationHub", {
                    accessTokenFactory: () => token
                })
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Set up event handlers
            this.setupSignalREventHandlers();

            // Start connection
            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;
            
            console.log('SignalR connected for notifications');

            // Join notification groups
            await this.joinNotificationGroups();

        } catch (error) {
            console.error('SignalR connection failed:', error);
            this.scheduleReconnect();
        }
    }

    setupSignalREventHandlers() {
        // Handle new notifications
        this.connection.on("NewNotification", (notification) => {
            this.handleNewNotification(notification);
        });

        // Handle notification read status changes
        this.connection.on("NotificationRead", (notificationId) => {
            this.markNotificationAsRead(notificationId, false);
        });

        // Handle bulk read status changes
        this.connection.on("NotificationsBulkRead", (notificationIds) => {
            notificationIds.forEach(id => this.markNotificationAsRead(id, false));
        });

        // Handle high priority notifications
        this.connection.on("HighPriorityNotification", (notification) => {
            this.handleHighPriorityNotification(notification);
        });

        // Handle system alerts
        this.connection.on("SystemAlert", (notification) => {
            this.handleSystemAlert(notification);
        });

        // Handle reservation updates
        this.connection.on("ReservationUpdate", (notification) => {
            this.handleReservationUpdate(notification);
        });

        // Handle conflict alerts
        this.connection.on("ConflictAlert", (notification) => {
            this.handleConflictAlert(notification);
        });

        // Handle browser notifications
        this.connection.on("BrowserNotification", (request) => {
            this.showBrowserNotification(request);
        });

        // Handle notification deleted
        this.connection.on("NotificationDeleted", (notificationId) => {
            this.notifications = this.notifications.filter(n => n.id !== notificationId);
            this.updateNotificationsList();
        });

        // Handle connection events
        this.connection.onreconnecting(() => {
            console.log('SignalR reconnecting...');
            this.isConnected = false;
            this.updateConnectionStatus(false);
        });

        this.connection.onreconnected(() => {
            console.log('SignalR reconnected');
            this.isConnected = true;
            this.reconnectAttempts = 0;
            this.updateConnectionStatus(true);
            this.joinNotificationGroups();
        });

        this.connection.onclose(() => {
            console.log('SignalR connection closed');
            this.isConnected = false;
            this.updateConnectionStatus(false);
            this.scheduleReconnect();
        });
    }

    async joinNotificationGroups() {
        try {
            await this.connection.invoke("JoinNotificationGroup");
            await this.connection.invoke("JoinUserGroup");
            
            // Join hotel-specific groups if user has hotel context
            const hotelId = this.getCurrentHotelId();
            if (hotelId) {
                await this.connection.invoke("JoinHotelGroup", hotelId.toString());
            }

            // Join admin group if user is admin
            if (this.isCurrentUserAdmin()) {
                await this.connection.invoke("JoinAdminGroup");
            }
        } catch (error) {
            console.error('Failed to join notification groups:', error);
        }
    }

    async loadInitialNotifications() {
        try {
            // Use ApiClient for authenticated requests
            const apiClient = new ApiClient();
            
            const notifications = await apiClient.get('/notifications?limit=20');
            this.notifications = notifications || [];
            this.updateNotificationsList();

            // Load unread count
            const unreadCount = await apiClient.get('/notifications/unread-count');
            this.unreadCount = unreadCount || 0;
            this.updateUnreadBadge();
        } catch (error) {
            console.error('Failed to load initial notifications:', error);
        }
    }

    setupUI() {
        // Create notification UI elements if they don't exist
        this.createNotificationUI();
        
        // Set up event listeners
        this.setupEventListeners();

        // Show bell once user is authenticated
        this.showBellWhenAuthenticated();
    }

    showBellWhenAuthenticated() {
        const token = localStorage.getItem('jwt_token') || localStorage.getItem('token');
        const container = document.getElementById('notificationBellContainer');
        if (container && token) {
            container.style.display = '';
        }
    }

    createNotificationUI() {
        // Bell is now in _Layout.cshtml — skip dynamic creation if already present
        if (document.getElementById('notification-bell')) return;
    }

    setupEventListeners() {
        // Mark all as read
        const markAllReadBtn = document.getElementById('mark-all-read');
        if (markAllReadBtn) {
            markAllReadBtn.addEventListener('click', () => this.markAllAsRead());
        }

        // Dropdown filter tabs
        document.querySelectorAll('.notif-tab').forEach(btn => {
            btn.addEventListener('click', () => {
                document.querySelectorAll('.notif-tab').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');

                // Update aria-selected and tabindex for tab pattern
                document.querySelectorAll('.notif-tab[role="tab"]').forEach(tab => {
                    const isActive = tab === btn;
                    tab.setAttribute('aria-selected', isActive ? 'true' : 'false');
                    tab.setAttribute('tabindex', isActive ? '0' : '-1');
                });
                // Update tabpanel labelledby to active tab
                const activeTab = document.querySelector('.notif-tab.active[role="tab"]');
                if (activeTab) {
                    document.getElementById('notifications-list')?.setAttribute('aria-labelledby', activeTab.id);
                }

                const filter = btn.dataset.filter;
                const filtered = filter === 'unread'
                    ? this.notifications.filter(n => !n.isRead)
                    : filter === 'reservations'
                        ? this.notifications.filter(n => n.type === 5)
                        : this.notifications;
                this.renderFilteredList(filtered);
            });
        });

        // Individual notification clicks
        document.addEventListener('click', (e) => {
            if (e.target.closest('.notification-item')) {
                const notificationId = e.target.closest('.notification-item').dataset.notificationId;
                this.markAsRead(parseInt(notificationId));
            }
        });
    }

    renderFilteredList(items) {
        const container = document.getElementById('notifications-list');
        if (!container) return;

        if (items.length === 0) {
            container.innerHTML = '<div class="text-center p-3 text-muted small">No notifications</div>';
            return;
        }

        const html = items.slice(0, 10).map(notification => {
            const timeAgo = this.getTimeAgo(new Date(notification.createdAt));
            const priorityClass = this.getPriorityClass(notification.priority);
            const typeIcon = this.getTypeIcon(notification.type);

            return `
                <div class="dropdown-item notification-item px-3 py-2 ${notification.isRead ? '' : 'bg-primary-subtle'} ${priorityClass}"
                     data-notification-id="${notification.id}" style="cursor:pointer; white-space:normal;">
                    <div class="d-flex align-items-start gap-2">
                        <i class="${typeIcon} mt-1 flex-shrink-0"></i>
                        <div class="flex-grow-1 min-w-0">
                            <div class="fw-semibold small text-truncate">${this.escapeHtml(notification.title)}</div>
                            <div class="small text-muted text-truncate">${this.escapeHtml(notification.message)}</div>
                            <div class="small text-muted">${timeAgo}</div>
                        </div>
                        ${!notification.isRead ? '<span class="badge bg-primary rounded-pill flex-shrink-0">New</span>' : ''}
                    </div>
                </div>`;
        }).join('');

        container.innerHTML = html;
    }

    handleNewNotification(notification) {
        // Add to notifications array
        this.notifications.unshift(notification);
        
        // Update unread count
        if (!notification.isRead) {
            this.unreadCount++;
        }
        
        // Update UI
        this.updateNotificationsList();
        this.updateUnreadBadge();
        
        // Show toast notification
        this.showToastNotification(notification);
        
        // Show browser notification if permission granted
        if (notification.priority === 3 || notification.priority === 4) { // High or Critical
            this.showBrowserNotification({
                title: notification.title,
                body: notification.message,
                icon: '/images/notification-icon.png',
                tag: `notification-${notification.id}`
            });
        }
        
        // Play notification sound for high priority
        if (notification.priority >= 3) {
            this.playNotificationSound();
        }
    }

    handleHighPriorityNotification(notification) {
        // Show modal for critical notifications
        if (notification.priority === 4) { // Critical
            this.showCriticalNotificationModal(notification);
        } else {
            this.handleNewNotification(notification);
        }
    }

    handleSystemAlert(notification) {
        // System alerts always show as modals
        this.showSystemAlertModal(notification);
    }

    handleReservationUpdate(notification) {
        this.handleNewNotification(notification);
        
        // Trigger calendar refresh if calendar is visible
        if (window.calendarManager) {
            window.calendarManager.refreshEvents();
        }
    }

    handleConflictAlert(notification) {
        // Conflict alerts are high priority
        this.handleHighPriorityNotification(notification);
        
        // Add special styling for conflicts
        setTimeout(() => {
            const notificationElement = document.querySelector(`[data-notification-id="${notification.id}"]`);
            if (notificationElement) {
                notificationElement.classList.add('border-danger', 'bg-danger-subtle');
            }
        }, 100);
    }

    async markAsRead(notificationId) {
        try {
            const apiClient = new ApiClient();
            await apiClient.request(`/notifications/${notificationId}/read`, {
                method: 'PUT'
            });
            
            this.markNotificationAsRead(notificationId, true);
        } catch (error) {
            console.error('Failed to mark notification as read:', error);
        }
    }

    async markAllAsRead() {
        try {
            const hotelId = this.getCurrentHotelId();
            const endpoint = hotelId ? `/notifications/read-all?hotelId=${hotelId}` : '/notifications/read-all';
            
            const apiClient = new ApiClient();
            await apiClient.request(endpoint, {
                method: 'PUT'
            });

            // Mark all notifications as read locally
            this.notifications.forEach(n => n.isRead = true);
            this.unreadCount = 0;
            this.updateNotificationsList();
            this.updateUnreadBadge();
        } catch (error) {
            console.error('Failed to mark all notifications as read:', error);
        }
    }

    markNotificationAsRead(notificationId, updateServer = true) {
        const notification = this.notifications.find(n => n.id === notificationId);
        if (notification && !notification.isRead) {
            notification.isRead = true;
            this.unreadCount = Math.max(0, this.unreadCount - 1);
            this.updateNotificationsList();
            this.updateUnreadBadge();
        }
    }

    updateNotificationsList() {
        this.renderFilteredList(this.notifications);
    }

    updateUnreadBadge() {
        const badge = document.getElementById('notification-badge');
        if (!badge) return;

        if (this.unreadCount > 0) {
            const count = this.unreadCount > 99 ? 99 : this.unreadCount;
            badge.textContent = this.unreadCount > 99 ? '99+' : this.unreadCount.toString();
            badge.setAttribute('aria-label', count + ' unread notification' + (count !== 1 ? 's' : ''));
            badge.style.display = 'block';
        } else {
            badge.setAttribute('aria-label', '0 unread notifications');
            badge.style.display = 'none';
        }
    }

    updateConnectionStatus(isConnected) {
        const statusElement = document.getElementById('connection-status');
        const messageElement = document.getElementById('connection-message');
        
        if (!statusElement || !messageElement) return;

        const alertElement = statusElement.querySelector('.alert');
        
        if (isConnected) {
            alertElement.className = 'alert alert-success alert-dismissible fade show';
            messageElement.textContent = 'Connected';
            messageElement.innerHTML = '<i class="fas fa-wifi me-2"></i>Connected';
            
            // Hide after 3 seconds
            setTimeout(() => {
                alertElement.style.display = 'none';
            }, 3000);
        } else {
            alertElement.className = 'alert alert-warning alert-dismissible fade show';
            messageElement.innerHTML = '<i class="fas fa-wifi-slash me-2"></i>Reconnecting...';
            alertElement.style.display = 'block';
        }
    }

    showToastNotification(notification) {
        // Create toast element
        const toastHtml = `
            <div class="toast align-items-center border-0" role="alert" data-bs-delay="5000">
                <div class="d-flex">
                    <div class="toast-body">
                        <div class="d-flex align-items-center">
                            <i class="${this.getTypeIcon(notification.type)} me-2"></i>
                            <div>
                                <div class="fw-semibold">${this.escapeHtml(notification.title)}</div>
                                <div class="small">${this.escapeHtml(notification.message)}</div>
                            </div>
                        </div>
                    </div>
                    <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            </div>
        `;

        // Add to toast container
        let toastContainer = document.getElementById('toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toast-container';
            toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
            toastContainer.style.zIndex = '1055';
            document.body.appendChild(toastContainer);
        }

        const toastElement = document.createElement('div');
        toastElement.innerHTML = toastHtml;
        const toast = toastElement.firstElementChild;
        
        // Add priority-based styling
        const priorityClass = this.getPriorityClass(notification.priority);
        if (priorityClass) {
            toast.classList.add(priorityClass);
        }

        toastContainer.appendChild(toast);

        // Initialize and show toast
        const bsToast = new bootstrap.Toast(toast);
        bsToast.show();

        // Remove from DOM after hiding
        toast.addEventListener('hidden.bs.toast', () => {
            toast.remove();
        });
    }

    showBrowserNotification(request) {
        if (Notification.permission === 'granted') {
            const notification = new Notification(request.title, {
                body: request.body,
                icon: request.icon || '/favicon.ico',
                badge: request.badge,
                tag: request.tag,
                requireInteraction: request.requireInteraction || false,
                data: request.data
            });

            // Auto-close after 5 seconds unless requireInteraction is true
            if (!request.requireInteraction) {
                setTimeout(() => notification.close(), 5000);
            }

            notification.onclick = () => {
                window.focus();
                notification.close();
            };
        }
    }

    showCriticalNotificationModal(notification) {
        const modalHtml = `
            <div class="modal fade" id="criticalNotificationModal" tabindex="-1">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content border-danger">
                        <div class="modal-header bg-danger text-white">
                            <h5 class="modal-title">
                                <i class="fas fa-exclamation-triangle me-2"></i>
                                Critical Alert
                            </h5>
                        </div>
                        <div class="modal-body">
                            <h6>${this.escapeHtml(notification.title)}</h6>
                            <p>${this.escapeHtml(notification.message)}</p>
                            <small class="text-muted">
                                ${new Date(notification.createdAt).toLocaleString()}
                            </small>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-primary" data-bs-dismiss="modal">
                                Acknowledge
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Remove existing modal if present
        const existingModal = document.getElementById('criticalNotificationModal');
        if (existingModal) {
            existingModal.remove();
        }

        // Add modal to DOM
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        
        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('criticalNotificationModal'));
        modal.show();

        // Mark as read when acknowledged
        document.getElementById('criticalNotificationModal').addEventListener('hidden.bs.modal', () => {
            this.markAsRead(notification.id);
            document.getElementById('criticalNotificationModal').remove();
        });

        // Play alert sound
        this.playNotificationSound();
    }

    showSystemAlertModal(notification) {
        // Similar to critical notification but with different styling
        this.showCriticalNotificationModal(notification);
    }

    async requestNotificationPermission() {
        if ('Notification' in window && Notification.permission === 'default') {
            try {
                const permission = await Notification.requestPermission();
                console.log('Notification permission:', permission);
            } catch (error) {
                console.error('Failed to request notification permission:', error);
            }
        }
    }

    playNotificationSound() {
        try {
            // Create audio element for notification sound
            const audio = new Audio('/sounds/notification.mp3');
            audio.volume = 0.5;
            audio.play().catch(e => {
                // Fallback to system beep if audio file not available
                console.log('Notification sound not available');
            });
        } catch (error) {
            console.error('Failed to play notification sound:', error);
        }
    }

    scheduleReconnect() {
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            const delay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1);
            
            console.log(`Scheduling reconnect attempt ${this.reconnectAttempts} in ${delay}ms`);
            
            setTimeout(() => {
                this.setupSignalRConnection();
            }, delay);
        } else {
            console.error('Max reconnection attempts reached');
            this.updateConnectionStatus(false);
        }
    }

    // Utility methods
    getCurrentHotelId() {
        // This would typically come from user context or URL
        return localStorage.getItem('currentHotelId');
    }

    isCurrentUserAdmin() {
        // This would typically come from JWT token or user context
        const token = localStorage.getItem('token');
        if (!token) return false;
        
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            return payload.role === 'Admin';
        } catch {
            return false;
        }
    }

    getTimeAgo(date) {
        const now = new Date();
        const diffMs = now - date;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return 'Just now';
        if (diffMins < 60) return `${diffMins}m ago`;
        if (diffHours < 24) return `${diffHours}h ago`;
        if (diffDays < 7) return `${diffDays}d ago`;
        return date.toLocaleDateString();
    }

    getPriorityClass(priority) {
        switch (priority) {
            case 4: return 'border-danger bg-danger-subtle'; // Critical
            case 3: return 'border-warning bg-warning-subtle'; // High
            case 2: return ''; // Normal
            case 1: return 'text-muted'; // Low
            default: return '';
        }
    }

    getTypeIcon(type) {
        switch (type) {
            case 1: return 'fas fa-info-circle text-info'; // Info
            case 2: return 'fas fa-exclamation-triangle text-warning'; // Warning
            case 3: return 'fas fa-times-circle text-danger'; // Error
            case 4: return 'fas fa-check-circle text-success'; // Success
            case 5: return 'fas fa-calendar-alt text-primary'; // ReservationUpdate
            case 6: return 'fas fa-exclamation-triangle text-danger'; // Conflict
            case 7: return 'fas fa-cog text-secondary'; // SystemAlert
            case 8: return 'fas fa-sync text-info'; // BookingComSync
            default: return 'fas fa-bell text-secondary';
        }
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize notification manager when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.notificationManager = new NotificationManager();
});

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = NotificationManager;
}