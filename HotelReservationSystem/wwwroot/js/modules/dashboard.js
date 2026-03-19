// Dashboard module for KPI widgets and data visualization
class DashboardManager {
    constructor() {
        this.apiClient = new ApiClient();
        this.currentHotelId = null;
        this.refreshInterval = null;
        this.charts = {};
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadDashboardData();
        this.startAutoRefresh();
    }

    setupEventListeners() {
        // Hotel filter change
        const hotelFilter = document.getElementById('hotelFilter');
        if (hotelFilter) {
            hotelFilter.addEventListener('change', (e) => {
                this.currentHotelId = e.target.value || null;
                this.loadDashboardData();
            });
        }

        // Refresh button
        const refreshBtn = document.getElementById('refreshDashboard');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', () => {
                this.loadDashboardData();
            });
        }

        // Notification actions
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('mark-notification-read')) {
                const notificationId = e.target.dataset.notificationId;
                this.markNotificationAsRead(notificationId);
            }
            
            if (e.target.classList.contains('mark-all-notifications-read')) {
                this.markAllNotificationsAsRead();
            }
        });
    }

    async loadDashboardData() {
        try {
            this.showLoadingState();
            
            // Load all dashboard data in parallel
            const [kpiData, dailyOps, notifications, recentReservations] = await Promise.all([
                this.loadKpiData(),
                this.loadDailyOperations(),
                this.loadNotifications(),
                this.loadRecentReservations()
            ]);

            this.updateKpiWidgets(kpiData);
            this.updateDailyOperations(dailyOps);
            this.updateNotifications(notifications);
            this.updateRecentReservations(recentReservations);
            this.updateRevenueChart(kpiData.revenueTracking);
            
            this.hideLoadingState();
        } catch (error) {
            console.error('Error loading dashboard data:', error);
            this.showErrorState('Failed to load dashboard data');
        }
    }

    async loadKpiData() {
        const params = this.currentHotelId ? `?hotelId=${this.currentHotelId}` : '';
        return await this.apiClient.get(`/dashboard/kpi${params}`);
    }

    async loadDailyOperations() {
        const params = this.currentHotelId ? `?hotelId=${this.currentHotelId}` : '';
        return await this.apiClient.get(`/dashboard/daily-operations${params}`);
    }

    async loadNotifications() {
        const params = this.currentHotelId ? `?hotelId=${this.currentHotelId}` : '';
        const response = await this.apiClient.get(`/dashboard/notifications${params}`);
        if (response && response.notifications && response.notifications.items) {
            response.notifications = response.notifications.items;
        }
        return response;
    }

    async loadRecentReservations() {
        const params = this.currentHotelId ? `?hotelId=${this.currentHotelId}` : '';
        const response = await this.apiClient.get(`/dashboard/recent-reservations${params}`);
        return (response && response.items) ? response.items : (response || []);
    }

    updateKpiWidgets(kpiData) {
        // Occupancy Rate Widget
        this.updateWidget('occupancyRate', {
            value: `${kpiData.occupancyRate.todayRate}%`,
            subtitle: `${kpiData.occupancyRate.occupiedRoomsToday}/${kpiData.occupancyRate.totalRooms} rooms`,
            trend: this.calculateTrend(kpiData.occupancyRate.todayRate, kpiData.occupancyRate.weekRate)
        });

        // Revenue Widget
        this.updateWidget('monthlyRevenue', {
            value: this.formatCurrency(kpiData.revenueTracking.monthRevenue),
            subtitle: `${kpiData.revenueTracking.monthlyVariance >= 0 ? '+' : ''}${kpiData.revenueTracking.monthlyVariance.toFixed(1)}% vs last month`,
            trend: kpiData.revenueTracking.monthlyVariance >= 0 ? 'up' : 'down'
        });

        // Today's Revenue Widget
        this.updateWidget('todayRevenue', {
            value: this.formatCurrency(kpiData.revenueTracking.todayRevenue),
            subtitle: `Week: ${this.formatCurrency(kpiData.revenueTracking.weekRevenue)}`,
            trend: this.calculateTrend(kpiData.revenueTracking.todayRevenue, kpiData.revenueTracking.weekRevenue / 7)
        });

        // Projected Revenue Widget
        this.updateWidget('projectedRevenue', {
            value: this.formatCurrency(kpiData.revenueTracking.projectedMonthRevenue),
            subtitle: 'Projected this month',
            trend: kpiData.revenueTracking.projectedMonthRevenue > kpiData.revenueTracking.lastMonthRevenue ? 'up' : 'down'
        });

        // Update occupancy breakdown
        this.updateOccupancyBreakdown(kpiData.occupancyRate);
    }

    updateWidget(widgetId, data) {
        const widget = document.getElementById(widgetId);
        if (!widget) return;

        const valueElement = widget.querySelector('.dashboard-stat');
        const subtitleElement = widget.querySelector('.dashboard-subtitle');
        const trendElement = widget.querySelector('.dashboard-trend');

        if (valueElement) valueElement.textContent = data.value;
        if (subtitleElement) subtitleElement.textContent = data.subtitle;
        
        if (trendElement && data.trend) {
            trendElement.className = `dashboard-trend trend-${data.trend}`;
            trendElement.innerHTML = data.trend === 'up' ? 
                '<i class="bi bi-arrow-up"></i>' : 
                '<i class="bi bi-arrow-down"></i>';
        }
    }

    updateOccupancyBreakdown(occupancyData) {
        const container = document.getElementById('occupancyBreakdown');
        if (!container) return;

        container.innerHTML = `
            <div class="row text-center">
                <div class="col-4">
                    <div class="occupancy-metric">
                        <div class="metric-value">${occupancyData.todayRate}%</div>
                        <div class="metric-label">Today</div>
                    </div>
                </div>
                <div class="col-4">
                    <div class="occupancy-metric">
                        <div class="metric-value">${occupancyData.weekRate}%</div>
                        <div class="metric-label">This Week</div>
                    </div>
                </div>
                <div class="col-4">
                    <div class="occupancy-metric">
                        <div class="metric-value">${occupancyData.monthRate}%</div>
                        <div class="metric-label">This Month</div>
                    </div>
                </div>
            </div>
        `;
    }

    updateDailyOperations(dailyOps) {
        this.updateCheckInsList(dailyOps.todayCheckIns);
        this.updateCheckOutsList(dailyOps.todayCheckOuts);
        
        // Update counters
        const checkInCount = document.getElementById('checkInCount');
        const checkOutCount = document.getElementById('checkOutCount');
        
        if (checkInCount) checkInCount.textContent = dailyOps.totalCheckIns;
        if (checkOutCount) checkOutCount.textContent = dailyOps.totalCheckOuts;
    }

    updateCheckInsList(checkIns) {
        const container = document.getElementById('todayCheckIns');
        if (!container) return;

        if (checkIns.length === 0) {
            container.innerHTML = '<p class="text-muted text-center py-3">No check-ins scheduled for today</p>';
            return;
        }

        const html = checkIns.map(checkIn => `
            <div class="check-in-item mb-2 p-2 border rounded">
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <strong>${checkIn.guestName}</strong>
                        <div class="small text-muted">
                            Room ${checkIn.roomNumber} • ${checkIn.hotelName}
                        </div>
                        ${checkIn.guestPhone ? `<div class="small text-muted">${checkIn.guestPhone}</div>` : ''}
                        ${checkIn.specialRequests ? `<div class="small text-info mt-1">${checkIn.specialRequests}</div>` : ''}
                    </div>
                    <div class="text-end">
                        <span class="badge bg-${this.getStatusColor(checkIn.status)}">${checkIn.status}</span>
                        <div class="small text-muted mt-1">${checkIn.bookingReference}</div>
                    </div>
                </div>
            </div>
        `).join('');

        container.innerHTML = html;
    }

    updateCheckOutsList(checkOuts) {
        const container = document.getElementById('todayCheckOuts');
        if (!container) return;

        if (checkOuts.length === 0) {
            container.innerHTML = '<p class="text-muted text-center py-3">No check-outs scheduled for today</p>';
            return;
        }

        const html = checkOuts.map(checkOut => `
            <div class="check-out-item mb-2 p-2 border rounded">
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <strong>${checkOut.guestName}</strong>
                        <div class="small text-muted">
                            Room ${checkOut.roomNumber} • ${checkOut.hotelName}
                        </div>
                        ${checkOut.guestPhone ? `<div class="small text-muted">${checkOut.guestPhone}</div>` : ''}
                    </div>
                    <div class="text-end">
                        <span class="badge bg-${this.getStatusColor(checkOut.status)}">${checkOut.status}</span>
                        <div class="small text-muted mt-1">${checkOut.bookingReference}</div>
                    </div>
                </div>
            </div>
        `).join('');

        container.innerHTML = html;
    }

    updateNotifications(notificationData) {
        const container = document.getElementById('notificationsList');
        const badge = document.getElementById('notificationBadge');
        
        if (!container) return;

        // Update notification badge
        if (badge) {
            if (notificationData.totalCount > 0) {
                badge.textContent = notificationData.totalCount;
                badge.style.display = 'inline';
            } else {
                badge.style.display = 'none';
            }
        }

        // Update notification counts
        this.updateNotificationCounts(notificationData);

        if (notificationData.notifications.length === 0) {
            container.innerHTML = `
                <div class="text-center text-muted py-3">
                    <i class="bi bi-bell-slash fs-1"></i>
                    <p class="mt-2">No new notifications</p>
                </div>
            `;
            return;
        }

        const html = notificationData.notifications.map(notification => `
            <div class="notification-item mb-2 p-2 border-start border-${this.getNotificationColor(notification.type)} border-3 bg-light">
                <div class="d-flex justify-content-between align-items-start">
                    <div class="flex-grow-1">
                        <div class="fw-bold">${notification.title}</div>
                        <div class="small text-muted mb-1">${notification.message}</div>
                        <div class="small text-muted">${this.formatDateTime(notification.createdAt)}</div>
                    </div>
                    <div class="ms-2">
                        <button class="btn btn-sm btn-outline-secondary mark-notification-read" 
                                data-notification-id="${notification.id}">
                            <i class="bi bi-check"></i>
                        </button>
                    </div>
                </div>
            </div>
        `).join('');

        container.innerHTML = html + `
            <div class="text-center mt-3">
                <button class="btn btn-sm btn-outline-primary mark-all-notifications-read">
                    Mark All as Read
                </button>
            </div>
        `;
    }

    updateNotificationCounts(notificationData) {
        const criticalCount = document.getElementById('criticalNotificationCount');
        const warningCount = document.getElementById('warningNotificationCount');
        const infoCount = document.getElementById('infoNotificationCount');

        if (criticalCount) criticalCount.textContent = notificationData.criticalCount;
        if (warningCount) warningCount.textContent = notificationData.warningCount;
        if (infoCount) infoCount.textContent = notificationData.infoCount;
    }

    updateRecentReservations(reservations) {
        const tableBody = document.querySelector('#recentReservationsTable tbody');
        if (!tableBody) return;

        if (reservations.length === 0) {
            tableBody.innerHTML = `
                <tr>
                    <td colspan="8" class="text-center text-muted py-4">
                        <i class="bi bi-calendar-x fs-1"></i>
                        <p class="mt-2">No recent reservations found</p>
                    </td>
                </tr>
            `;
            return;
        }

        const html = reservations.map(reservation => `
            <tr>
                <td>
                    <span class="fw-bold">${reservation.bookingReference}</span>
                    <div class="small text-muted">
                        <span class="badge badge-sm bg-${this.getSourceColor(reservation.source)}">${this.getSourceName(reservation.source)}</span>
                    </div>
                </td>
                <td>
                    <div class="fw-bold">${reservation.guestName}</div>
                </td>
                <td>${reservation.hotelName}</td>
                <td>
                    <span class="fw-bold">${reservation.roomNumber}</span>
                </td>
                <td>
                    <div>${new Date(reservation.checkInDate).toLocaleDateString()}</div>
                    <div class="small text-muted">${new Date(reservation.checkInDate).toLocaleDateString('en-US', { weekday: 'short' })}</div>
                </td>
                <td>
                    <div>${new Date(reservation.checkOutDate).toLocaleDateString()}</div>
                    <div class="small text-muted">${new Date(reservation.checkOutDate).toLocaleDateString('en-US', { weekday: 'short' })}</div>
                </td>
                <td>
                    <span class="badge bg-${this.getStatusColor(reservation.status)}">${reservation.status}</span>
                </td>
                <td>
                    <div class="btn-group btn-group-sm">
                        <button class="btn btn-outline-primary btn-sm" onclick="viewReservation(${reservation.id})" title="View Details">
                            <i class="bi bi-eye"></i>
                        </button>
                        <button class="btn btn-outline-secondary btn-sm" onclick="editReservation(${reservation.id})" title="Edit">
                            <i class="bi bi-pencil"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');

        tableBody.innerHTML = html;
    }

    updateRevenueChart(revenueData) {
        const ctx = document.getElementById('revenueChart');
        if (!ctx) return;

        // Destroy existing chart if it exists
        if (this.charts.revenue) {
            this.charts.revenue.destroy();
        }

        const labels = revenueData.dailyBreakdown.map(d => new Date(d.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }));
        const data = revenueData.dailyBreakdown.map(d => d.revenue);

        this.charts.revenue = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Daily Revenue',
                    data: data,
                    borderColor: '#0d6efd',
                    backgroundColor: 'rgba(13, 110, 253, 0.1)',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return '$' + value.toLocaleString();
                            }
                        }
                    }
                },
                elements: {
                    point: {
                        radius: 4,
                        hoverRadius: 6
                    }
                }
            }
        });
    }

    async markNotificationAsRead(notificationId) {
        try {
            await this.apiClient.put(`/api/dashboard/notifications/${notificationId}/read`);
            // Reload notifications
            const notifications = await this.loadNotifications();
            this.updateNotifications(notifications);
        } catch (error) {
            console.error('Error marking notification as read:', error);
        }
    }

    async markAllNotificationsAsRead() {
        try {
            const params = this.currentHotelId ? `?hotelId=${this.currentHotelId}` : '';
            await this.apiClient.put(`/api/dashboard/notifications/read-all${params}`);
            // Reload notifications
            const notifications = await this.loadNotifications();
            this.updateNotifications(notifications);
        } catch (error) {
            console.error('Error marking all notifications as read:', error);
        }
    }

    startAutoRefresh() {
        // Refresh dashboard data every 5 minutes
        this.refreshInterval = setInterval(() => {
            this.loadDashboardData();
        }, 5 * 60 * 1000);
    }

    stopAutoRefresh() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }

    showLoadingState() {
        const loadingElements = document.querySelectorAll('.dashboard-loading');
        loadingElements.forEach(el => el.style.display = 'block');
        
        const contentElements = document.querySelectorAll('.dashboard-content');
        contentElements.forEach(el => el.style.opacity = '0.5');
    }

    hideLoadingState() {
        const loadingElements = document.querySelectorAll('.dashboard-loading');
        loadingElements.forEach(el => el.style.display = 'none');
        
        const contentElements = document.querySelectorAll('.dashboard-content');
        contentElements.forEach(el => el.style.opacity = '1');
    }

    showErrorState(message) {
        const errorContainer = document.getElementById('dashboardError');
        if (errorContainer) {
            errorContainer.innerHTML = `
                <div class="alert alert-danger" role="alert">
                    <i class="bi bi-exclamation-triangle"></i>
                    ${message}
                    <button type="button" class="btn btn-sm btn-outline-danger ms-2" onclick="dashboardManager.loadDashboardData()">
                        Retry
                    </button>
                </div>
            `;
            errorContainer.style.display = 'block';
        }
    }

    // Utility methods
    formatCurrency(amount) {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD',
            minimumFractionDigits: 0,
            maximumFractionDigits: 0
        }).format(amount);
    }

    formatDateTime(dateString) {
        return new Date(dateString).toLocaleString('en-US', {
            month: 'short',
            day: 'numeric',
            hour: 'numeric',
            minute: '2-digit'
        });
    }

    calculateTrend(current, previous) {
        if (previous === 0) return 'neutral';
        return current > previous ? 'up' : 'down';
    }

    getStatusColor(status) {
        const statusColors = {
            'Pending': 'warning',
            'Confirmed': 'success',
            'CheckedIn': 'info',
            'CheckedOut': 'secondary',
            'Cancelled': 'danger'
        };
        return statusColors[status] || 'secondary';
    }

    getNotificationColor(type) {
        const typeColors = {
            'Critical': 'danger',
            'Overbooking': 'danger',
            'Warning': 'warning',
            'MaintenanceConflict': 'warning',
            'Info': 'info',
            'SystemError': 'danger',
            'IntegrationError': 'warning'
        };
        return typeColors[type] || 'info';
    }

    getSourceColor(source) {
        const sourceColors = {
            'Manual': 'secondary',
            'BookingCom': 'primary',
            'Direct': 'success',
            'Other': 'info'
        };
        return sourceColors[source] || 'secondary';
    }

    getSourceName(source) {
        const sourceNames = {
            'Manual': 'Manual',
            'BookingCom': 'Booking.com',
            'Direct': 'Direct',
            'Other': 'Other'
        };
        return sourceNames[source] || source;
    }

    destroy() {
        this.stopAutoRefresh();
        
        // Destroy charts
        Object.values(this.charts).forEach(chart => {
            if (chart) chart.destroy();
        });
        
        this.charts = {};
    }
}

// Initialize dashboard when DOM is loaded
let dashboardManager;
document.addEventListener('DOMContentLoaded', function() {
    if (document.getElementById('dashboardContainer')) {
        dashboardManager = new DashboardManager();
    }
});

// Global functions for reservation actions
function viewReservation(reservationId) {
    // Navigate to reservation details page or open modal
    window.location.href = `/reservations/details/${reservationId}`;
}

function editReservation(reservationId) {
    // Navigate to reservation edit page or open modal
    window.location.href = `/reservations/edit/${reservationId}`;
}

// Cleanup on page unload
window.addEventListener('beforeunload', function() {
    if (dashboardManager) {
        dashboardManager.destroy();
    }
});