// Reservations module for reservation management
class ReservationsManager {
    constructor() {
        this.reservations = [];
        this.hotels = [];
        this.currentFilters = {};
        this.currentReservationId = null;
        this.isEditMode = false;
        this.roomTypes = [
            { id: 0, name: 'Single' },
            { id: 1, name: 'Double' },
            { id: 2, name: 'Suite' },
            { id: 3, name: 'Family' }
        ];
        this.reservationSources = [
            { id: 1, name: 'Manual' },
            { id: 2, name: 'Booking.com' },
            { id: 3, name: 'Direct' }
        ];
        this.reservationStatuses = [
            { id: 1, name: 'Pending' },
            { id: 2, name: 'Confirmed' },
            { id: 3, name: 'Checked In' },
            { id: 4, name: 'Checked Out' },
            { id: 5, name: 'Cancelled' }
        ];
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
        const applyBtn = document.getElementById('applyReservationFilters');
        const clearBtn = document.getElementById('clearReservationFilters');
        const refreshBtn = document.getElementById('refreshReservations');
        const newBtn = document.getElementById('newReservationBtn');
        const saveBtn = document.getElementById('saveReservationBtn');
        const hotelSelect = document.getElementById('reservationHotel');
        
        if (applyBtn) applyBtn.addEventListener('click', () => this.applyFilters());
        if (clearBtn) clearBtn.addEventListener('click', () => this.clearFilters());
        if (refreshBtn) refreshBtn.addEventListener('click', () => this.loadReservations());
        if (newBtn) newBtn.addEventListener('click', () => this.openNewReservationModal());
        if (saveBtn) saveBtn.addEventListener('click', () => this.saveReservation());
        if (hotelSelect) hotelSelect.addEventListener('change', () => this.onHotelSelectionChange());
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
        
        // Reset form
        document.getElementById('reservationForm').reset();
        
        // Set default dates
        const today = new Date();
        const tomorrow = new Date(today);
        tomorrow.setDate(tomorrow.getDate() + 1);
        
        document.getElementById('checkInDate').value = today.toISOString().split('T')[0];
        document.getElementById('checkOutDate').value = tomorrow.toISOString().split('T')[0];
        
        // Enable hotel selection
        document.getElementById('reservationHotel').disabled = false;
        
        // Show the modal
        const modal = new bootstrap.Modal(document.getElementById('reservationModal'));
        modal.show();
    }

    async saveReservation() {
        try {
            const form = document.getElementById('reservationForm');
            
            // Validate form
            if (!UI.validateForm(form)) {
                UI.showError('Please fill in all required fields');
                return;
            }
            
            // Collect form data
            const formData = new FormData(form);
            const reservationData = {
                hotelId: formData.get('hotelId'),
                roomId: formData.get('roomId'),
                guestName: formData.get('guestName'),
                guestEmail: formData.get('guestEmail'),
                guestPhone: formData.get('guestPhone'),
                checkInDate: formData.get('checkInDate'),
                checkOutDate: formData.get('checkOutDate'),
                numberOfGuests: parseInt(formData.get('numberOfGuests')),
                totalAmount: parseFloat(formData.get('totalAmount')),
                source: parseInt(formData.get('source')),
                status: parseInt(formData.get('status')) || 2, // Default to confirmed
                bookingReference: formData.get('bookingReference')
            };
            
            // Add the reservation
            const reservation = await API.addReservation(reservationData);
            
            // Refresh reservations list
            this.loadReservations();
            
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('reservationModal'));
            modal.hide();
            
            UI.showSuccess('Reservation created successfully');
            
        } catch (error) {
            console.error('Error saving reservation:', error);
            UI.showError('Failed to save reservation');
        }
    }

    async viewReservation(id) {
        try {
            // Show loading
            UI.showLoading();
            
            // Get reservation details
            const reservation = await API.getReservation(id);
            
            // Create view modal content
            const modalContent = `
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Reservation Details</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row">
                            <div class="col-md-6">
                                <h6>Guest Information</h6>
                                <p><strong>Name:</strong> ${reservation.guestName || 'N/A'}</p>
                                <p><strong>Email:</strong> ${reservation.guestEmail || 'N/A'}</p>
                                <p><strong>Phone:</strong> ${reservation.guestPhone || 'N/A'}</p>
                            </div>
                            <div class="col-md-6">
                                <h6>Reservation Details</h6>
                                <p><strong>Reference:</strong> ${reservation.bookingReference || 'N/A'}</p>
                                <p><strong>Hotel:</strong> ${reservation.hotelName || 'N/A'}</p>
                                <p><strong>Room:</strong> ${reservation.roomNumber || 'N/A'}</p>
                                <p><strong>Check-in:</strong> ${this.formatDate(reservation.checkInDate)}</p>
                                <p><strong>Check-out:</strong> ${this.formatDate(reservation.checkOutDate)}</p>
                                <p><strong>Guests:</strong> ${reservation.numberOfGuests}</p>
                                <p><strong>Total:</strong> $${reservation.totalAmount.toFixed(2)}</p>
                                <p><strong>Status:</strong> <span class="badge ${this.getStatusBadgeClass(reservation.status)}">${this.getStatusDisplayName(reservation.status)}</span></p>
                                <p><strong>Source:</strong> <span class="badge ${this.getSourceBadgeClass(reservation.source)}">${this.getSourceDisplayName(reservation.source)}</span></p>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                    </div>
                </div>
            `;
            
            // Show in a new modal
            const modalDiv = document.createElement('div');
            modalDiv.className = 'modal fade';
            modalDiv.id = 'reservationViewModal';
            modalDiv.setAttribute('tabindex', '-1');
            modalDiv.innerHTML = modalContent;
            document.body.appendChild(modalDiv);
            
            const modal = new bootstrap.Modal(modalDiv);
            modal.show();
            
            // Clean up after closing
            modalDiv.addEventListener('hidden.bs.modal', () => {
                document.body.removeChild(modalDiv);
            });
            
        } catch (error) {
            console.error('Error viewing reservation:', error);
            UI.showError('Failed to load reservation details');
        } finally {
            UI.hideLoading();
        }
    }

    async editReservation(id) {
        try {
            // Show loading
            UI.showLoading();
            
            // Get reservation details
            const reservation = await API.getReservation(id);
            this.currentReservationId = id;
            this.isEditMode = true;
            
            // Fill form with reservation data
            document.getElementById('reservationHotel').value = reservation.hotelId;
            document.getElementById('reservationRoom').value = reservation.roomId;
            document.getElementById('guestName').value = reservation.guestName;
            document.getElementById('guestEmail').value = reservation.guestEmail;
            document.getElementById('guestPhone').value = reservation.guestPhone;
            document.getElementById('checkInDate').value = reservation.checkInDate.split('T')[0];
            document.getElementById('checkOutDate').value = reservation.checkOutDate.split('T')[0];
            document.getElementById('numberOfGuests').value = reservation.numberOfGuests;
            document.getElementById('totalAmount').value = reservation.totalAmount;
            document.getElementById('source').value = reservation.source;
            document.getElementById('status').value = reservation.status;
            document.getElementById('bookingReference').value = reservation.bookingReference;
            
            // Update modal title
            document.getElementById('reservationModalLabel').innerHTML = `
                <i class="bi bi-pencil"></i>
                Edit Reservation
            `;
            
            // Disable hotel selection for editing
            document.getElementById('reservationHotel').disabled = true;
            
            // Show the modal
            const modal = new bootstrap.Modal(document.getElementById('reservationModal'));
            modal.show();
            
        } catch (error) {
            console.error('Error editing reservation:', error);
            UI.showError('Failed to load reservation for editing');
        } finally {
            UI.hideLoading();
        }
    }

    async cancelReservation(id) {
        try {
            const reservation = this.reservations.find(r => r.id === id);
            if (!reservation) return;

            const confirmed = confirm(`Are you sure you want to cancel the reservation for ${reservation.guestName}? This action cannot be undone.`);
            if (!confirmed) return;

            // Mark reservation as cancelled
            await API.updateReservation(id, { status: 5 });
            
            // Refresh reservations list
            this.loadReservations();
            
            UI.showSuccess('Reservation cancelled successfully');
            
        } catch (error) {
            console.error('Error cancelling reservation:', error);
            UI.showError('Failed to cancel reservation');
        }
    }

    async checkInReservation(id) {
        try {
            const confirmed = confirm('Are you sure you want to check-in this guest?');
            if (!confirmed) return;

            // Mark reservation as checked in
            await API.updateReservation(id, { status: 3 });
            
            // Refresh reservations list
            this.loadReservations();
            
            UI.showSuccess('Guest checked in successfully');
            
        } catch (error) {
            console.error('Error checking in guest:', error);
            UI.showError('Failed to check in guest');
        }
    }

    async checkOutReservation(id) {
        try {
            const confirmed = confirm('Are you sure you want to check out this guest?');
            if (!confirmed) return;

            // Mark reservation as checked out
            await API.updateReservation(id, { status: 4 });
            
            // Refresh reservations list
            this.loadReservations();
            
            UI.showSuccess('Guest checked out successfully');
            
        } catch (error) {
            console.error('Error checking out guest:', error);
            UI.showError('Failed to check out guest');
        }
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