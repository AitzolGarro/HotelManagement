// Reports module for handling report generation and export
class ReportsModule {
    constructor() {
        this.currentReportData = null;
        this.currentFilters = null;
        this.init();
    }

    init() {
        this.bindEvents();
        this.loadHotels();
        this.setDefaultDates();
    }

    bindEvents() {
        // Report generation
        document.getElementById('reportFiltersForm').addEventListener('submit', (e) => {
            e.preventDefault();
            this.generateReport();
        });

        // Quick date range selection
        document.getElementById('quickDateRange').addEventListener('change', (e) => {
            this.setQuickDateRange(e.target.value);
        });

        // Export functionality
        document.getElementById('exportReportBtn').addEventListener('click', () => {
            this.showExportModal();
        });

        document.getElementById('confirmExportBtn').addEventListener('click', () => {
            this.exportReport();
        });

        // Report type change
        document.getElementById('reportType').addEventListener('change', () => {
            if (this.currentReportData) {
                this.generateReport();
            }
        });
    }

    async loadHotels() {
        try {
            const response = await fetch('/api/hotels');
            if (response.ok) {
                const hotels = await response.json();
                const hotelSelect = document.getElementById('hotelFilter');
                
                hotels.forEach(hotel => {
                    const option = document.createElement('option');
                    option.value = hotel.id;
                    option.textContent = hotel.name;
                    hotelSelect.appendChild(option);
                });
            }
        } catch (error) {
            console.error('Error loading hotels:', error);
        }
    }

    setDefaultDates() {
        const endDate = new Date();
        const startDate = new Date();
        startDate.setDate(startDate.getDate() - 30);

        document.getElementById('startDate').value = startDate.toISOString().split('T')[0];
        document.getElementById('endDate').value = endDate.toISOString().split('T')[0];
    }

    setQuickDateRange(days) {
        if (!days) return;

        const endDate = new Date();
        const startDate = new Date();
        startDate.setDate(startDate.getDate() - parseInt(days));

        document.getElementById('startDate').value = startDate.toISOString().split('T')[0];
        document.getElementById('endDate').value = endDate.toISOString().split('T')[0];
    }

    async generateReport() {
        const formData = new FormData(document.getElementById('reportFiltersForm'));
        const reportType = formData.get('reportType');
        const hotelId = formData.get('hotelId') || null;
        const startDate = formData.get('startDate');
        const endDate = formData.get('endDate');

        if (!startDate || !endDate) {
            UI.showAlert('Please select both start and end dates', 'warning');
            return;
        }

        if (new Date(startDate) >= new Date(endDate)) {
            UI.showAlert('Start date must be before end date', 'warning');
            return;
        }

        this.currentFilters = { reportType, hotelId, startDate, endDate };

        try {
            UI.showLoading('Generating report...');
            
            let endpoint = '';
            switch (reportType) {
                case 'occupancy':
                    endpoint = '/api/reports/occupancy';
                    break;
                case 'revenue':
                    endpoint = '/api/reports/revenue';
                    break;
                case 'guestPattern':
                    endpoint = '/api/reports/guest-patterns';
                    break;
                default:
                    throw new Error('Invalid report type');
            }

            const params = new URLSearchParams({
                startDate,
                endDate
            });
            
            if (hotelId) {
                params.append('hotelId', hotelId);
            }

            const response = await fetch(`${endpoint}?${params}`, {
                headers: {
                    'Authorization': `Bearer ${Auth.getToken()}`
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const reportData = await response.json();
            this.currentReportData = reportData;
            this.renderReport(reportType, reportData);

        } catch (error) {
            console.error('Error generating report:', error);
            UI.showAlert('Error generating report: ' + error.message, 'danger');
        } finally {
            UI.hideLoading();
        }
    }

    renderReport(reportType, data) {
        const container = document.getElementById('reportContent');
        
        switch (reportType) {
            case 'occupancy':
                container.innerHTML = this.renderOccupancyReport(data);
                break;
            case 'revenue':
                container.innerHTML = this.renderRevenueReport(data);
                break;
            case 'guestPattern':
                container.innerHTML = this.renderGuestPatternReport(data);
                break;
        }

        // Initialize charts if needed
        this.initializeCharts(reportType, data);
    }

    renderOccupancyReport(data) {
        return `
            <div class="card">
                <div class="card-header">
                    <h5 class="card-title">Occupancy Report</h5>
                    <small class="text-muted">${data.hotelName || 'All Hotels'} - ${data.startDate} to ${data.endDate}</small>
                </div>
                <div class="card-body">
                    <div class="row mb-4">
                        <div class="col-md-3">
                            <div class="card bg-primary text-white">
                                <div class="card-body text-center">
                                    <h3>${data.overallOccupancyRate}%</h3>
                                    <p class="mb-0">Overall Occupancy</p>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="card bg-info text-white">
                                <div class="card-body text-center">
                                    <h3>${data.totalRooms}</h3>
                                    <p class="mb-0">Total Rooms</p>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="card bg-success text-white">
                                <div class="card-body text-center">
                                    <h3>${data.occupiedRoomNights}</h3>
                                    <p class="mb-0">Occupied Room Nights</p>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="card bg-warning text-white">
                                <div class="card-body text-center">
                                    <h3>${data.totalRoomNights}</h3>
                                    <p class="mb-0">Total Room Nights</p>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-6">
                            <h6>Room Type Breakdown</h6>
                            <div class="table-responsive">
                                <table class="table table-sm">
                                    <thead>
                                        <tr>
                                            <th>Room Type</th>
                                            <th>Occupancy Rate</th>
                                            <th>Revenue</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${data.roomTypeBreakdown.map(rt => `
                                            <tr>
                                                <td>${rt.roomTypeName}</td>
                                                <td>${rt.occupancyRate}%</td>
                                                <td>$${rt.totalRevenue.toFixed(2)}</td>
                                            </tr>
                                        `).join('')}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <h6>Daily Occupancy Chart</h6>
                            <canvas id="occupancyChart" width="400" height="200"></canvas>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    renderRevenueReport(data) {
        return `
            <div class="card">
                <div class="card-header">
                    <h5 class="card-title">Revenue Report</h5>
                    <small class="text-muted">${data.hotelName || 'All Hotels'} - ${data.startDate} to ${data.endDate}</small>
                </div>
                <div class="card-body">
                    <div class="row mb-4">
                        <div class="col-md-3">
                            <div class="card bg-success text-white">
                                <div class="card-body text-center">
                                    <h3>$${data.totalRevenue.toFixed(2)}</h3>
                                    <p class="mb-0">Total Revenue</p>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="card bg-info text-white">
                                <div class="card-body text-center">
                                    <h3>$${data.averageRevenuePerDay.toFixed(2)}</h3>
                                    <p class="mb-0">Avg Revenue/Day</p>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="card bg-warning text-white">
                                <div class="card-body text-center">
                                    <h3>${data.variancePercentage}%</h3>
                                    <p class="mb-0">Variance</p>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="card bg-primary text-white">
                                <div class="card-body text-center">
                                    <h3>$${data.averageRevenuePerReservation.toFixed(2)}</h3>
                                    <p class="mb-0">Avg Revenue/Booking</p>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-6">
                            <h6>Revenue by Source</h6>
                            <div class="table-responsive">
                                <table class="table table-sm">
                                    <thead>
                                        <tr>
                                            <th>Source</th>
                                            <th>Revenue</th>
                                            <th>Percentage</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${data.revenueBySource.map(source => `
                                            <tr>
                                                <td>${source.sourceName}</td>
                                                <td>$${source.revenue.toFixed(2)}</td>
                                                <td>${source.percentage}%</td>
                                            </tr>
                                        `).join('')}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <h6>Revenue by Room Type</h6>
                            <div class="table-responsive">
                                <table class="table table-sm">
                                    <thead>
                                        <tr>
                                            <th>Room Type</th>
                                            <th>Revenue</th>
                                            <th>Percentage</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${data.revenueByRoomType.map(rt => `
                                            <tr>
                                                <td>${rt.roomTypeName}</td>
                                                <td>$${rt.revenue.toFixed(2)}</td>
                                                <td>${rt.percentage}%</td>
                                            </tr>
                                        `).join('')}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    renderGuestPatternReport(data) {
        return `
            <div class="card">
                <div class="card-header">
                    <h5 class="card-title">Guest Pattern Report</h5>
                    <small class="text-muted">${data.hotelName || 'All Hotels'} - ${data.startDate} to ${data.endDate}</small>
                </div>
                <div class="card-body">
                    <div class="row mb-4">
                        <div class="col-md-3">
                            <div class="card bg-primary text-white">
                                <div class="card-body text-center">
                                    <h3>${data.totalGuests}</h3>
                                    <p class="mb-0">Total Guests</p>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="card bg-info text-white">
                                <div class="card-body text-center">
                                    <h3>${data.uniqueGuests}</h3>
                                    <p class="mb-0">Unique Guests</p>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="card bg-success text-white">
                                <div class="card-body text-center">
                                    <h3>${data.repeatGuestPercentage}%</h3>
                                    <p class="mb-0">Repeat Guests</p>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="card bg-warning text-white">
                                <div class="card-body text-center">
                                    <h3>${data.averageStayDuration}</h3>
                                    <p class="mb-0">Avg Stay (nights)</p>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-6">
                            <h6>Booking Source Patterns</h6>
                            <div class="table-responsive">
                                <table class="table table-sm">
                                    <thead>
                                        <tr>
                                            <th>Source</th>
                                            <th>Reservations</th>
                                            <th>Percentage</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${data.bookingSourcePatterns.map(pattern => `
                                            <tr>
                                                <td>${pattern.sourceName}</td>
                                                <td>${pattern.reservationCount}</td>
                                                <td>${pattern.percentage}%</td>
                                            </tr>
                                        `).join('')}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <h6>Stay Duration Patterns</h6>
                            <div class="table-responsive">
                                <table class="table table-sm">
                                    <thead>
                                        <tr>
                                            <th>Duration</th>
                                            <th>Reservations</th>
                                            <th>Percentage</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${data.stayDurationPatterns.map(pattern => `
                                            <tr>
                                                <td>${pattern.durationRange}</td>
                                                <td>${pattern.reservationCount}</td>
                                                <td>${pattern.percentage}%</td>
                                            </tr>
                                        `).join('')}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    initializeCharts(reportType, data) {
        // This would initialize Chart.js charts based on the report type
        // For now, we'll just log that charts would be initialized
        console.log(`Initializing charts for ${reportType} report`);
    }

    showExportModal() {
        if (!this.currentReportData || !this.currentFilters) {
            UI.showAlert('Please generate a report first', 'warning');
            return;
        }

        const modal = new bootstrap.Modal(document.getElementById('exportModal'));
        modal.show();
    }

    async exportReport() {
        if (!this.currentFilters) {
            UI.showAlert('No report data to export', 'warning');
            return;
        }

        const exportFormat = document.getElementById('exportFormat').value;
        
        try {
            UI.showLoading('Exporting report...');

            const exportRequest = {
                reportType: this.getReportTypeEnum(this.currentFilters.reportType),
                startDate: this.currentFilters.startDate,
                endDate: this.currentFilters.endDate,
                hotelId: this.currentFilters.hotelId ? parseInt(this.currentFilters.hotelId) : null,
                exportFormat: this.getExportFormatEnum(exportFormat)
            };

            const response = await fetch('/api/reports/export', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${Auth.getToken()}`
                },
                body: JSON.stringify(exportRequest)
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            // Download the file
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.style.display = 'none';
            a.href = url;
            
            // Get filename from response headers or create one
            const contentDisposition = response.headers.get('content-disposition');
            let filename = `${this.currentFilters.reportType}_report.${exportFormat}`;
            if (contentDisposition) {
                const filenameMatch = contentDisposition.match(/filename="(.+)"/);
                if (filenameMatch) {
                    filename = filenameMatch[1];
                }
            }
            
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);

            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('exportModal'));
            modal.hide();

            UI.showAlert('Report exported successfully', 'success');

        } catch (error) {
            console.error('Error exporting report:', error);
            UI.showAlert('Error exporting report: ' + error.message, 'danger');
        } finally {
            UI.hideLoading();
        }
    }

    getReportTypeEnum(reportType) {
        switch (reportType) {
            case 'occupancy': return 1;
            case 'revenue': return 2;
            case 'guestPattern': return 3;
            default: return 1;
        }
    }

    getExportFormatEnum(format) {
        switch (format) {
            case 'json': return 1;
            case 'pdf': return 2;
            case 'excel': return 3;
            case 'csv': return 4;
            default: return 1;
        }
    }
}

// Initialize reports module when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.Reports = new ReportsModule();
});