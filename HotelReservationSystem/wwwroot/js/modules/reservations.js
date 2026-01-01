// Reservations module for reservation management
class ReservationsManager {
    constructor() {
        this.reservations = [];
        this.hotels = [];
        this.currentFilters = {};
        this.currentReservationId = null;
        this.isEditMode = false;
    }

    async initialize() {
        try {
            console.log('Reservations manager initializing...');
            
            // Setup event listeners
            this.setupEventListeners();
            
            // Set default date range
            this.setDefaultDateRange();
            
            // Load initial data
            await this.loadHotels();
            await this.loadReservations();
            
            console.log('Reservations manager initialized successfully');
        } catch (error) {
            console.error('Error initializing reservations manager:', error);
            UI.showError('Failed to initialize reservations management');
        }
    }

    setupEventListeners() {
        // Filter event listeners
        document.getElementById('applyReservationFilters').addEventListener('click', () => this.applyFilters());
        document.getElementById('clearReservationFilters').addEventListener('click', () => this.clearFilters());
        document.getElementById('refreshReservations').addEventListener('click', () => this.loadReservations());
        
        // Modal event listeners
        document.getElementById('newReservationBtn').addEventListener('click', () => this.openNewReservationModal());
        
        // Hotel selection change listeners
        document.getElementById('reservationHotel').addEventListener('change', () => this.onHotelSelectionChange());
    }

    setDefaultDateRange() {
        const today = new Date();
        const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
        const lastDay = new Date(today.getFullYear(), today.getMonth() + 1, 0);
        
        document.getElementById('dateFromFilter').value = firstDay.toISOString().split('T')[0];
        document.getElementById('dateToFilter').value = lastDay.toISOString().split('T')[0];
    }

    async loadHotels() {
        try {
            console.log('Loading hotels...');
            this.hotels = await API.getHotels();
            console.log('Hotels loaded:', this.hotels);
            
            // Populate hotel dropdowns
            this.populateHotelDropdowns();
            
        } catch (error) {
            console.error('Error loading hotels:', error);
            UI.showError('Failed to load hotels');
        }
    }

    populateHotelDropdowns() {
        const hotelSelects = [
            document.getElementById('hotelFilterRes'),
            document.getElementById('reservationHotel'),
            document.getElementById('blockingHotel')
        ];
        
        hotelSelects.forEach(select => {
            if (!select) return;
            
            // Clear existing options except the first one
            while (select.children.length > 1) {
                select.removeChild(select.lastChild);
            }
            
            // Add hotel options
            this.hotels.forEach(hotel => {
                const option = document.createElement('option');
                option.value = hotel.id;
                option.textContent = hotel.name;
                select.appendChild(option);
            });
        });
    }

    async loadReservations() {
        try {
            console.log('Loading reservations...');
            UI.showLoading();
            
            // Build query parameters
            const params = {};
            
            const dateFrom = document.getElementById('dateFromFilter').value;
            const dateTo = document.getElementById('dateToFilter').value;
            const hotelId = document.getElementById('hotelFilterRes').value;
            const status = document.getElementById('statusFilterRes').value;
            
            if (dateFrom) params.from = dateFrom;
            if (dateTo) params.to = dateTo;
            if (hotelId) params.hotelId = hotelId;
            if (status) params.status = this.mapStatusToEnum(status);
            
            console.log('Reservations API params:', params);
            
            this.reservations = await API.getReservations(params);
            console.log('Reservations loaded:', this.reservations);
            
            // Filter by source if specified (client-side filtering)
            const sourceFilter = document.getElementById('sourceFilter').value;
            if (sourceFilter) {
                const sourceEnum = this.mapSourceToEnum(sourceFilter);
                this.reservations = this.reservations.filter(r => r.source === sourceEnum);
            }
            
            this.displayReservations();
            
        } catch (error) {
            console.error('Error loading reservations:', error);
            UI.showError('Failed to load reservations');
            this.displayReservations([]);
        } finally {
            UI.hideLoading();
        }
    }

    displayReservations() {
        const tbody = document.querySelector('#reservationsTable tbody');
        
        if (!this.reservations || this.reservations.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="11" class="text-center text-muted py-4">
                        <i class="bi bi-journal-bookmark fs-1"></i>
                        <p class="mt-2">No reservations found</p>
                        <p class="small text-muted">Try adjusting your filters or create a new reservation</p>
                    </td>
                </tr>
            `;
            return;
        }
        
        tbody.innerHTML = this.reservations.map(reservation => `
            <tr>
                <td>
                    <span class="fw-medium">${reservation.bookingReference || 'N/A'}</span>
                </td>
                <td>
                    <div>
                        <div class="fw-medium">${reservation.guestName || 'Unknown Guest'}</div>
                        <small class="text-muted">${reservation.guestEmail || ''}</small>
                    </div>
                </td>
                <td>${reservation.hotelName || 'Unknown Hotel'}</td>
                <td>
                    <span class="badge bg-secondary">${reservation.roomNumber || 'N/A'}</span>
                </td>
                <td>${this.formatDate(reservation.checkInDate)}</td>
                <td>${this.formatDate(reservation.checkOutDate)}</td>
                <td>
                    <span class="badge bg-info">${reservation.numberOfGuests}</span>
                </td>
                <td>
                    <span class="fw-medium">$${reservation.totalAmount.toFixed(2)}</span>
                </td>
                <td>
                    <span class="badge ${this.getSourceBadgeClass(reservation.source)}">${this.getSourceDisplayName(reservation.source)}</span>
                </td>
                <td>
                    <span class="badge ${this.getStatusBadgeClass(reservation.status)}">${this.getStatusDisplayName(reservation.status)}</span>
                </td>
                <td>
                    <div class="btn-group btn-group-sm" role="group">
                        <button type="button" class="btn btn-outline-primary" onclick="reservationsManager.viewReservation(${reservation.id})" title="View Details">
                            <i class="bi bi-eye"></i>
                        </button>
                        <button type="button" class="btn btn-outline-secondary" onclick="reservationsManager.editReservation(${reservation.id})" title="Edit">
                            <i class="bi bi-pencil"></i>
                        </button>
                        ${reservation.status !== 4 ? `
                            <button type="button" class="btn btn-outline-danger" onclick="reservationsManager.cancelReservation(${reservation.id})" title="Cancel">
                                <i class="bi bi-x-circle"></i>
                            </button>
                        ` : ''}
                        ${reservation.status === 1 ? `
                            <button type="button" class="btn btn-outline-success" onclick="reservationsManager.checkInReservation(${reservation.id})" title="Check In">
                                <i class="bi bi-box-arrow-in-right"></i>
                            </button>
                        ` : ''}
                        ${reservation.status === 2 ? `
                            <button type="button" class="btn btn-outline-warning" onclick="reservationsManager.checkOutReservation(${reservation.id})" title="Check Out">
                                <i class="bi bi-box-arrow-right"></i>
                            </button>
                        ` : ''}
                    </div>
                </td>
            </tr>
        `).join('');
    }

    async onHotelSelectionChange() {
        const hotelId = document.getElementById('reservationHotel').value;
        const roomSelect = document.getElementById('reservationRoom');
        
        // Reset room selection
        roomSelect.innerHTML = '<option value="">Select Room</option>';
        roomSelect.disabled = !hotelId;
        
        if (hotelId) {
            try {
                const rooms = await API.getRooms(hotelId);
                console.log('Rooms loaded for hotel', hotelId, ':', rooms);
                
                rooms.forEach(room => {
                    const option = document.createElement('option');
                    option.value = room.id;
                    option.textContent = `${room.roomNumber} - ${this.getRoomTypeDisplayName(room.type)} (${room.capacity} guests)`;
                    option.dataset.baseRate = room.baseRate;
                    roomSelect.appendChild(option);
                });
                
            } catch (error) {
                console.error('Error loading rooms:', error);
                UI.showError('Failed to load rooms for selected hotel');
            }
        }
    }

    applyFilters() {
        console.log('Applying reservation filters...');
        this.loadReservations();
    }

    clearFilters() {
        console.log('Clearing reservation filters...');
        document.getElementById('dateFromFilter').value = '';
        document.getElementById('dateToFilter').value = '';
        document.getElementById('hotelFilterRes').value = '';
        document.getElementById('statusFilterRes').value = '';
        document.getElementById('sourceFilter').value = '';
        
        this.setDefaultDateRange();
        this.loadReservations();
    }

    openNewReservationModal() {
        this.isEditMode = false;
        this.currentReservationId = null;
        
        // Update modal title
        document.getElementById('reservationModalLabel').innerHTML = `
            <i class="bi bi-plus-circle"></i>
            New Reservation
        `;
        
        // Set default dates
        const today = new Date();
        const tomorrow = new Date(today);
        tomorrow.setDate(tomorrow.getDate() + 1);
        
        document.getElementById('checkInDate').value = today.toISOString().split('T')[0];
        document.getElementById('checkOutDate').value = tomorrow.toISOString().split('T')[0];
    }

    // Placeholder methods for future implementation
    async viewReservation(id) {
        UI.showInfo('View reservation functionality will be implemented in a future update');
    }

    async editReservation(id) {
        UI.showInfo('Edit reservation functionality will be implemented in a future update');
    }

    async cancelReservation(id) {
        const reservation = this.reservations.find(r => r.id === id);
        if (!reservation) return;

        const confirmed = confirm(`Are you sure you want to cancel the reservation for ${reservation.guestName}?`);
        if (!confirmed) return;

        UI.showInfo('Cancel reservation functionality will be implemented in a future update');
    }

    async checkInReservation(id) {
        UI.showInfo('Check-in functionality will be implemented in a future update');
    }

    async checkOutReservation(id) {
        UI.showInfo('Check-out functionality will be implemented in a future update');
    }

    // Utility methods
    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    }

    getStatusBadgeClass(status) {
        const statusClasses = {
            1: 'bg-warning text-dark', // Pending
            2: 'bg-success',           // Confirmed
            3: 'bg-primary',           // Checked In
            4: 'bg-info',              // Checked Out
            5: 'bg-danger'             // Cancelled
        };
        return statusClasses[status] || 'bg-secondary';
    }

    getStatusDisplayName(status) {
        const statusNames = {
            1: 'Pending',
            2: 'Confirmed',
            3: 'Checked In',
            4: 'Checked Out',
            5: 'Cancelled'
        };
        return statusNames[status] || 'Unknown';
    }

    getSourceBadgeClass(source) {
        const sourceClasses = {
            1: 'bg-secondary', // Manual
            2: 'bg-info',      // Booking.com
            3: 'bg-success'    // Direct
        };
        return sourceClasses[source] || 'bg-light text-dark';
    }

    getSourceDisplayName(source) {
        const sourceNames = {
            1: 'Manual',
            2: 'Booking.com',
            3: 'Direct'
        };
        return sourceNames[source] || 'Unknown';
    }

    getRoomTypeDisplayName(type) {
        const typeNames = {
            0: 'Single',
            1: 'Double',
            2: 'Suite',
            3: 'Family'
        };
        return typeNames[type] || 'Unknown';
    }

    mapStatusToEnum(statusString) {
        const statusMap = {
            'pending': 1,
            'confirmed': 2,
            'checkedin': 3,
            'checkedout': 4,
            'cancelled': 5
        };
        return statusMap[statusString];
    }

    mapSourceToEnum(sourceString) {
        const sourceMap = {
            'manual': 1,
            'booking': 2,
            'direct': 3
        };
        return sourceMap[sourceString];
    }

    // Public methods for external use
    refresh() {
        this.loadReservations();
    }
}

// Export for global use
window.ReservationsManager = ReservationsManager;