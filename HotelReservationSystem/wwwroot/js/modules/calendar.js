// Calendar module for FullCalendar integration
class CalendarManager {
    constructor() {
        this.calendar = null;
        this.currentFilters = {
            hotel: '',
            roomType: '',
            status: ''
        };
        this.hotels = [];
        this.rooms = [];
        this.reservations = [];
        this.signalRManager = null;
    }

    async initialize() {
        try {
            // Load initial data
            await this.loadHotels();
            await this.loadRooms();
            
            // Initialize calendar
            this.initializeCalendar();
            
            // Initialize date range picker
            this.initializeDateRangePicker();
            
            // Setup event listeners
            this.setupEventListeners();
            
            // Initialize SignalR for real-time updates
            await this.initializeSignalR();
            
            // Load reservations
            await this.loadReservations();
            
            console.log('Calendar initialized successfully');
        } catch (error) {
            console.error('Error initializing calendar:', error);
            UI.showError('Failed to initialize calendar');
        }
    }

    async loadHotels() {
        try {
            const response = await API.get('/api/hotels');
            this.hotels = response.data || [];
            this.populateHotelFilter();
        } catch (error) {
            console.error('Error loading hotels:', error);
            this.hotels = [];
        }
    }

    async loadRooms() {
        try {
            const response = await API.get('/api/rooms');
            this.rooms = response.data || [];
        } catch (error) {
            console.error('Error loading rooms:', error);
            this.rooms = [];
        }
    }

    async loadReservations() {
        try {
            UI.showLoading();
            
            // Build query parameters based on current filters
            const params = new URLSearchParams();
            
            // Use date range filter if set, otherwise use calendar view range
            if (this.currentFilters.dateRange) {
                params.append('from', this.currentFilters.dateRange.start);
                params.append('to', this.currentFilters.dateRange.end);
            } else {
                // Get current calendar date range
                const view = this.calendar.view;
                params.append('from', view.activeStart.toISOString().split('T')[0]);
                params.append('to', view.activeEnd.toISOString().split('T')[0]);
            }
            
            if (this.currentFilters.hotel) {
                params.append('hotelId', this.currentFilters.hotel);
            }
            
            const response = await API.get(`/api/reservations?${params.toString()}`);
            this.reservations = response.data || [];
            
            // Convert reservations to calendar events
            const events = this.convertReservationsToEvents(this.reservations);
            
            // Update calendar with new events
            this.calendar.removeAllEvents();
            this.calendar.addEventSource(events);
            
        } catch (error) {
            console.error('Error loading reservations:', error);
            UI.showError('Failed to load reservations');
        } finally {
            UI.hideLoading();
        }
    }

    initializeCalendar() {
        const calendarEl = document.getElementById('calendar');
        
        this.calendar = new FullCalendar.Calendar(calendarEl, {
            initialView: 'resourceTimelineWeek',
            headerToolbar: {
                left: 'prev,next today',
                center: 'title',
                right: 'resourceTimelineWeek,resourceTimelineMonth,dayGridMonth'
            },
            height: 'auto',
            
            // Timeline specific settings
            resourceAreaHeaderContent: 'Rooms',
            resourceAreaWidth: '200px',
            resourceOrder: 'title',
            
            // Resources (rooms) will be loaded dynamically
            resources: this.getRoomResources.bind(this),
            
            // Event rendering
            eventDisplay: 'block',
            eventContent: this.renderEventContent.bind(this),
            
            // Event interactions
            eventClick: this.handleEventClick.bind(this),
            eventMouseEnter: this.handleEventMouseEnter.bind(this),
            eventMouseLeave: this.handleEventMouseLeave.bind(this),
            
            // Date navigation
            datesSet: this.handleDatesSet.bind(this),
            
            // Styling
            eventClassNames: this.getEventClassNames.bind(this),
            
            // Responsive
            windowResize: this.handleWindowResize.bind(this),
            
            // Resource rendering
            resourceLabelContent: this.renderResourceLabel.bind(this),
            
            // Timeline view settings
            slotMinTime: '00:00:00',
            slotMaxTime: '24:00:00',
            slotDuration: '01:00:00',
            slotLabelInterval: '06:00:00',
            
            // Allow events to overlap
            slotEventOverlap: false,
            
            // Business hours (optional)
            businessHours: {
                startTime: '06:00',
                endTime: '22:00'
            },
            
            // Scrolling
            scrollTime: '08:00:00',
            
            // Selection (for creating new reservations)
            selectable: true,
            selectMirror: true,
            select: this.handleDateSelect.bind(this),
            
            // Event resizing and dragging
            editable: false, // Disable for now, can be enabled later
            eventResizableFromStart: false,
            eventDurationEditable: false
        });

        this.calendar.render();
    }



    getFilteredRooms() {
        let filtered = [...this.rooms];
        
        if (this.currentFilters.hotel) {
            filtered = filtered.filter(room => room.hotelId.toString() === this.currentFilters.hotel);
        }
        
        if (this.currentFilters.roomType) {
            filtered = filtered.filter(room => room.type.toLowerCase() === this.currentFilters.roomType.toLowerCase());
        }
        
        return filtered;
    }

    convertReservationsToEvents(reservations) {
        const filteredReservations = this.getFilteredReservations(reservations);
        
        return filteredReservations.map(reservation => {
            const room = this.rooms.find(r => r.id === reservation.roomId);
            const hotel = this.hotels.find(h => h.id === reservation.hotelId);
            
            return {
                id: reservation.id.toString(),
                resourceId: reservation.roomId.toString(), // Assign event to room resource
                title: `${reservation.guest.firstName} ${reservation.guest.lastName}`,
                start: reservation.checkInDate,
                end: reservation.checkOutDate,
                extendedProps: {
                    reservation: reservation,
                    status: reservation.status,
                    source: reservation.source,
                    totalAmount: reservation.totalAmount,
                    numberOfGuests: reservation.numberOfGuests,
                    specialRequests: reservation.specialRequests,
                    roomNumber: room ? room.roomNumber : 'Unknown',
                    hotelName: hotel ? hotel.name : 'Unknown Hotel'
                }
            };
        });
    }

    getFilteredReservations(reservations) {
        let filtered = [...reservations];
        
        if (this.currentFilters.status) {
            filtered = filtered.filter(res => res.status.toLowerCase() === this.currentFilters.status.toLowerCase());
        }
        
        return filtered;
    }

    renderEventContent(eventInfo) {
        const reservation = eventInfo.event.extendedProps.reservation;
        const roomNumber = eventInfo.event.extendedProps.roomNumber;
        const hotelName = eventInfo.event.extendedProps.hotelName;
        
        return {
            html: `
                <div class="calendar-event-content">
                    <div class="event-title">${eventInfo.event.title}</div>
                    <div class="event-details">
                        <small class="text-muted">
                            Room ${roomNumber} • ${reservation.numberOfGuests} guest${reservation.numberOfGuests > 1 ? 's' : ''}
                            ${reservation.source === 'Booking' ? ' • Booking.com' : ''}
                        </small>
                    </div>
                </div>
            `
        };
    }

    getEventClassNames(eventInfo) {
        const status = eventInfo.event.extendedProps.status.toLowerCase();
        const source = eventInfo.event.extendedProps.source.toLowerCase();
        
        const classes = ['calendar-event'];
        classes.push(`status-${status}`);
        classes.push(`source-${source}`);
        
        return classes;
    }

    handleEventClick(eventInfo) {
        const reservation = eventInfo.event.extendedProps.reservation;
        this.showReservationModal(reservation);
    }

    handleEventMouseEnter(eventInfo) {
        const reservation = eventInfo.event.extendedProps.reservation;
        this.showTooltip(eventInfo.el, reservation);
    }

    handleEventMouseLeave(eventInfo) {
        this.hideTooltip();
    }

    showTooltip(element, reservation) {
        // Remove existing tooltip
        this.hideTooltip();
        
        const tooltip = document.createElement('div');
        tooltip.className = 'calendar-tooltip';
        tooltip.innerHTML = `
            <div class="tooltip-header">
                <strong>${reservation.guest.firstName} ${reservation.guest.lastName}</strong>
                <span class="badge status-${reservation.status.toLowerCase()}">${reservation.status}</span>
            </div>
            <div class="tooltip-body">
                <div><i class="bi bi-building"></i> ${this.getHotelName(reservation.hotelId)}</div>
                <div><i class="bi bi-door-open"></i> Room ${this.getRoomNumber(reservation.roomId)} (${this.getRoomType(reservation.roomId)})</div>
                <div><i class="bi bi-calendar-check"></i> ${this.formatDate(reservation.checkInDate)} - ${this.formatDate(reservation.checkOutDate)}</div>
                <div><i class="bi bi-people"></i> ${reservation.numberOfGuests} guest${reservation.numberOfGuests > 1 ? 's' : ''}</div>
                <div><i class="bi bi-currency-dollar"></i> $${reservation.totalAmount.toFixed(2)}</div>
                ${reservation.source === 'Booking' ? '<div><i class="bi bi-globe"></i> Booking.com</div>' : ''}
                ${reservation.specialRequests ? `<div><i class="bi bi-chat-text"></i> ${reservation.specialRequests}</div>` : ''}
            </div>
        `;
        
        document.body.appendChild(tooltip);
        
        // Position tooltip
        const rect = element.getBoundingClientRect();
        tooltip.style.position = 'fixed';
        tooltip.style.left = `${rect.left + rect.width / 2}px`;
        tooltip.style.top = `${rect.top - 10}px`;
        tooltip.style.transform = 'translateX(-50%) translateY(-100%)';
        tooltip.style.zIndex = '9999';
        
        this.currentTooltip = tooltip;
    }

    hideTooltip() {
        if (this.currentTooltip) {
            this.currentTooltip.remove();
            this.currentTooltip = null;
        }
    }

    handleDatesSet(dateInfo) {
        // Reload reservations when date range changes
        this.loadReservations();
    }

    handleWindowResize() {
        // Handle responsive behavior for timeline view
        if (window.innerWidth < 768) {
            // Switch to list view on mobile
            this.calendar.changeView('listWeek');
        } else if (window.innerWidth < 1024) {
            // Use timeline week view on tablets
            this.calendar.changeView('resourceTimelineWeek');
        } else {
            // Use timeline month view on desktop
            this.calendar.changeView('resourceTimelineMonth');
        }
        
        // Adjust resource area width based on screen size
        const resourceAreaWidth = window.innerWidth < 768 ? '150px' : '200px';
        this.calendar.setOption('resourceAreaWidth', resourceAreaWidth);
    }

    initializeDateRangePicker() {
        const dateRangePicker = $('#dateRangePicker');
        
        dateRangePicker.daterangepicker({
            opens: 'left',
            drops: 'down',
            autoUpdateInput: false,
            locale: {
                cancelLabel: 'Clear',
                format: 'MM/DD/YYYY'
            },
            ranges: {
                'Today': [moment(), moment()],
                'Yesterday': [moment().subtract(1, 'days'), moment().subtract(1, 'days')],
                'Last 7 Days': [moment().subtract(6, 'days'), moment()],
                'Last 30 Days': [moment().subtract(29, 'days'), moment()],
                'This Month': [moment().startOf('month'), moment().endOf('month')],
                'Last Month': [moment().subtract(1, 'month').startOf('month'), moment().subtract(1, 'month').endOf('month')]
            }
        });

        dateRangePicker.on('apply.daterangepicker', (ev, picker) => {
            dateRangePicker.val(picker.startDate.format('MM/DD/YYYY') + ' - ' + picker.endDate.format('MM/DD/YYYY'));
            this.currentFilters.dateRange = {
                start: picker.startDate.format('YYYY-MM-DD'),
                end: picker.endDate.format('YYYY-MM-DD')
            };
        });

        dateRangePicker.on('cancel.daterangepicker', (ev, picker) => {
            dateRangePicker.val('');
            this.currentFilters.dateRange = null;
        });
    }

    setupEventListeners() {
        // Filter controls
        document.getElementById('applyFilters').addEventListener('click', () => {
            this.applyFilters();
        });
        
        document.getElementById('clearFilters').addEventListener('click', () => {
            this.clearFilters();
        });
        
        // Add reservation button
        document.getElementById('addReservationBtn').addEventListener('click', () => {
            this.showAddReservationModal();
        });
        
        // Modal event listeners
        document.getElementById('saveReservationBtn').addEventListener('click', () => {
            this.saveReservation();
        });
        
        document.getElementById('editReservationBtn').addEventListener('click', () => {
            this.editCurrentReservation();
        });
        
        document.getElementById('cancelReservationBtn').addEventListener('click', () => {
            this.cancelCurrentReservation();
        });
        
        // Hotel selection change in reservation modal
        document.getElementById('reservationHotel').addEventListener('change', () => {
            this.updateRoomOptions();
        });
        
        // Filter change events
        document.getElementById('hotelFilter').addEventListener('change', () => {
            this.currentFilters.hotel = document.getElementById('hotelFilter').value;
            this.updateRoomResources();
            
            // Update SignalR hotel group subscription
            this.updateSignalRHotelSubscription();
        });
    }

    async initializeSignalR() {
        try {
            // Initialize SignalR manager
            this.signalRManager = new SignalRManager();
            await this.signalRManager.initialize();

            // Set up event handlers for calendar updates
            this.signalRManager.on('reservationCreated', (data) => {
                console.log('Received reservation created notification:', data);
                this.handleReservationUpdate();
            });

            this.signalRManager.on('reservationUpdated', (data) => {
                console.log('Received reservation updated notification:', data);
                this.handleReservationUpdate();
            });

            this.signalRManager.on('reservationCancelled', (data) => {
                console.log('Received reservation cancelled notification:', data);
                this.handleReservationUpdate();
            });

            this.signalRManager.on('calendarRefresh', () => {
                console.log('Received calendar refresh notification');
                this.handleReservationUpdate();
            });

            console.log('SignalR integration initialized for calendar');
        } catch (error) {
            console.error('Error initializing SignalR for calendar:', error);
            // Don't fail calendar initialization if SignalR fails
        }
    }

    async updateSignalRHotelSubscription() {
        if (!this.signalRManager || !this.signalRManager.isConnectionActive()) {
            return;
        }

        try {
            // Leave all hotel groups first (we'll rejoin the current one)
            // Note: In a real implementation, you might want to track which groups you're in
            
            // Join the current hotel group if a hotel is selected
            if (this.currentFilters.hotel) {
                await this.signalRManager.joinHotelGroup(this.currentFilters.hotel);
            }
        } catch (error) {
            console.error('Error updating SignalR hotel subscription:', error);
        }
    }

    handleReservationUpdate() {
        // Debounce the update to avoid too many rapid refreshes
        if (this.updateTimeout) {
            clearTimeout(this.updateTimeout);
        }

        this.updateTimeout = setTimeout(() => {
            this.loadReservations();
        }, 500); // Wait 500ms before refreshing
    }

    applyFilters() {
        this.currentFilters.hotel = document.getElementById('hotelFilter').value;
        this.currentFilters.roomType = document.getElementById('roomTypeFilter').value;
        this.currentFilters.status = document.getElementById('statusFilter').value;
        // dateRange is already set by the date range picker event handlers
        
        this.updateRoomResources();
        this.loadReservations();
    }

    clearFilters() {
        this.currentFilters = {
            hotel: '',
            roomType: '',
            status: '',
            dateRange: null
        };
        
        // Reset filter controls
        document.getElementById('hotelFilter').value = '';
        document.getElementById('roomTypeFilter').value = '';
        document.getElementById('statusFilter').value = '';
        document.getElementById('dateRangePicker').value = '';
        
        this.updateRoomResources();
        this.loadReservations();
    }

    getRoomResources() {
        const filteredRooms = this.getFilteredRooms();
        
        return filteredRooms.map(room => {
            const hotel = this.hotels.find(h => h.id === room.hotelId);
            
            return {
                id: room.id.toString(),
                title: `${room.roomNumber} (${this.getRoomTypeDisplay(room.type)})`,
                extendedProps: {
                    room: room,
                    hotelName: hotel ? hotel.name : 'Unknown Hotel',
                    roomType: room.type,
                    capacity: room.capacity,
                    baseRate: room.baseRate,
                    status: room.status
                }
            };
        });
    }

    renderResourceLabel(resourceInfo) {
        const room = resourceInfo.resource.extendedProps.room;
        const hotelName = resourceInfo.resource.extendedProps.hotelName;
        
        return {
            html: `
                <div class="resource-label">
                    <div class="room-title">${resourceInfo.resource.title}</div>
                    <div class="room-details">
                        <small class="text-muted">
                            ${hotelName} • ${room.capacity} guests • $${room.baseRate}/night
                        </small>
                    </div>
                    <div class="room-status">
                        <span class="badge room-status-${room.status.toLowerCase()}">${room.status}</span>
                    </div>
                </div>
            `
        };
    }

    handleDateSelect(selectInfo) {
        // Handle date selection for creating new reservations
        const resourceId = selectInfo.resource ? selectInfo.resource.id : null;
        
        if (resourceId) {
            // Pre-fill the add reservation modal with selected room and dates
            this.showAddReservationModal(resourceId, selectInfo.start, selectInfo.end);
        } else {
            // Show general add reservation modal
            this.showAddReservationModal();
        }
        
        // Clear the selection
        this.calendar.unselect();
    }

    updateRoomResources() {
        // Refresh the calendar resources when filters change
        this.calendar.refetchResources();
        this.loadReservations();
    }

    populateHotelFilter() {
        const hotelFilter = document.getElementById('hotelFilter');
        
        // Clear existing options (except "All Hotels")
        while (hotelFilter.children.length > 1) {
            hotelFilter.removeChild(hotelFilter.lastChild);
        }
        
        // Add hotel options
        this.hotels.forEach(hotel => {
            const option = document.createElement('option');
            option.value = hotel.id.toString();
            option.textContent = hotel.name;
            hotelFilter.appendChild(option);
        });
    }

    showReservationModal(reservation) {
        this.currentReservation = reservation;
        
        // Populate reservation details
        const detailsHtml = `
            <div class="row">
                <div class="col-md-6">
                    <h6><i class="bi bi-person-circle"></i> Guest Information</h6>
                    <p><strong>Name:</strong> ${reservation.guest.firstName} ${reservation.guest.lastName}</p>
                    <p><strong>Email:</strong> ${reservation.guest.email || 'Not provided'}</p>
                    <p><strong>Phone:</strong> ${reservation.guest.phone || 'Not provided'}</p>
                </div>
                <div class="col-md-6">
                    <h6><i class="bi bi-building"></i> Accommodation Details</h6>
                    <p><strong>Hotel:</strong> ${this.getHotelName(reservation.hotelId)}</p>
                    <p><strong>Room:</strong> ${this.getRoomNumber(reservation.roomId)} (${this.getRoomType(reservation.roomId)})</p>
                    <p><strong>Guests:</strong> ${reservation.numberOfGuests}</p>
                </div>
            </div>
            <div class="row mt-3">
                <div class="col-md-6">
                    <h6><i class="bi bi-calendar-check"></i> Stay Information</h6>
                    <p><strong>Check-in:</strong> ${this.formatFullDate(reservation.checkInDate)}</p>
                    <p><strong>Check-out:</strong> ${this.formatFullDate(reservation.checkOutDate)}</p>
                    <p><strong>Duration:</strong> ${this.calculateNights(reservation.checkInDate, reservation.checkOutDate)} night(s)</p>
                </div>
                <div class="col-md-6">
                    <h6><i class="bi bi-info-circle"></i> Reservation Details</h6>
                    <p><strong>Status:</strong> <span class="badge status-${reservation.status.toLowerCase()}">${reservation.status}</span></p>
                    <p><strong>Source:</strong> ${reservation.source === 'Booking' ? 'Booking.com' : 'Manual'}</p>
                    <p><strong>Total Amount:</strong> $${reservation.totalAmount.toFixed(2)}</p>
                    ${reservation.bookingReference ? `<p><strong>Booking Reference:</strong> ${reservation.bookingReference}</p>` : ''}
                </div>
            </div>
            ${reservation.specialRequests ? `
                <div class="row mt-3">
                    <div class="col-12">
                        <h6><i class="bi bi-chat-text"></i> Special Requests</h6>
                        <p class="text-muted">${reservation.specialRequests}</p>
                    </div>
                </div>
            ` : ''}
            ${reservation.internalNotes ? `
                <div class="row mt-3">
                    <div class="col-12">
                        <h6><i class="bi bi-sticky"></i> Internal Notes</h6>
                        <p class="text-muted">${reservation.internalNotes}</p>
                    </div>
                </div>
            ` : ''}
        `;
        
        document.getElementById('reservationDetails').innerHTML = detailsHtml;
        
        // Show/hide action buttons based on reservation status
        const editBtn = document.getElementById('editReservationBtn');
        const cancelBtn = document.getElementById('cancelReservationBtn');
        
        if (reservation.status.toLowerCase() === 'cancelled') {
            editBtn.style.display = 'none';
            cancelBtn.style.display = 'none';
        } else {
            editBtn.style.display = 'inline-block';
            cancelBtn.style.display = reservation.status.toLowerCase() !== 'checkedout' ? 'inline-block' : 'none';
        }
        
        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('reservationModal'));
        modal.show();
    }

    showAddReservationModal(preSelectedRoomId = null, startDate = null, endDate = null) {
        // Reset form
        document.getElementById('reservationForm').reset();
        
        // Populate hotel options
        this.populateReservationHotelOptions();
        
        // Set dates - use provided dates or defaults
        let checkInDate, checkOutDate;
        
        if (startDate && endDate) {
            checkInDate = startDate.toISOString().split('T')[0];
            checkOutDate = endDate.toISOString().split('T')[0];
        } else {
            // Set default dates (today and tomorrow)
            const today = new Date();
            const tomorrow = new Date(today);
            tomorrow.setDate(tomorrow.getDate() + 1);
            
            checkInDate = today.toISOString().split('T')[0];
            checkOutDate = tomorrow.toISOString().split('T')[0];
        }
        
        document.getElementById('checkInDate').value = checkInDate;
        document.getElementById('checkOutDate').value = checkOutDate;
        
        // Pre-select room if provided
        if (preSelectedRoomId) {
            const room = this.rooms.find(r => r.id.toString() === preSelectedRoomId);
            if (room) {
                // Select the hotel first
                document.getElementById('reservationHotel').value = room.hotelId.toString();
                
                // Trigger hotel change to populate rooms
                this.updateRoomOptions();
                
                // Then select the room
                setTimeout(() => {
                    document.getElementById('reservationRoom').value = preSelectedRoomId;
                }, 100);
            }
        }
        
        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('addReservationModal'));
        modal.show();
    }

    populateReservationHotelOptions() {
        const hotelSelect = document.getElementById('reservationHotel');
        
        // Clear existing options (except first one)
        while (hotelSelect.children.length > 1) {
            hotelSelect.removeChild(hotelSelect.lastChild);
        }
        
        // Add hotel options
        this.hotels.forEach(hotel => {
            const option = document.createElement('option');
            option.value = hotel.id.toString();
            option.textContent = hotel.name;
            hotelSelect.appendChild(option);
        });
    }

    updateRoomOptions() {
        const hotelId = document.getElementById('reservationHotel').value;
        const roomSelect = document.getElementById('reservationRoom');
        
        // Clear existing options (except first one)
        while (roomSelect.children.length > 1) {
            roomSelect.removeChild(roomSelect.lastChild);
        }
        
        if (hotelId) {
            const hotelRooms = this.rooms.filter(room => room.hotelId.toString() === hotelId);
            
            hotelRooms.forEach(room => {
                const option = document.createElement('option');
                option.value = room.id.toString();
                option.textContent = `${room.roomNumber} (${this.getRoomTypeDisplay(room.type)}) - $${room.baseRate}/night`;
                roomSelect.appendChild(option);
            });
        }
    }

    async saveReservation() {
        const form = document.getElementById('reservationForm');
        
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }
        
        try {
            UI.showLoading();
            
            const reservationData = {
                guestFirstName: document.getElementById('guestFirstName').value,
                guestLastName: document.getElementById('guestLastName').value,
                guestEmail: document.getElementById('guestEmail').value,
                guestPhone: document.getElementById('guestPhone').value,
                hotelId: parseInt(document.getElementById('reservationHotel').value),
                roomId: parseInt(document.getElementById('reservationRoom').value),
                checkInDate: document.getElementById('checkInDate').value,
                checkOutDate: document.getElementById('checkOutDate').value,
                numberOfGuests: parseInt(document.getElementById('numberOfGuests').value),
                totalAmount: parseFloat(document.getElementById('totalAmount').value),
                specialRequests: document.getElementById('specialRequests').value,
                internalNotes: document.getElementById('internalNotes').value
            };
            
            const response = await API.post('/api/reservations/manual', reservationData);
            
            if (response.success) {
                UI.showSuccess('Reservation created successfully');
                
                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('addReservationModal'));
                modal.hide();
                
                // Refresh calendar
                this.loadReservations();
            } else {
                UI.showError(response.message || 'Failed to create reservation');
            }
            
        } catch (error) {
            console.error('Error saving reservation:', error);
            UI.showError('Failed to create reservation');
        } finally {
            UI.hideLoading();
        }
    }

    editCurrentReservation() {
        if (this.currentReservation) {
            // Close current modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('reservationModal'));
            modal.hide();
            
            // Show edit modal (for now, just show info)
            UI.showInfo('Edit reservation functionality will be implemented in a future update');
        }
    }

    async cancelCurrentReservation() {
        if (!this.currentReservation) return;
        
        const confirmed = confirm(`Are you sure you want to cancel the reservation for ${this.currentReservation.guest.firstName} ${this.currentReservation.guest.lastName}?`);
        
        if (!confirmed) return;
        
        try {
            UI.showLoading();
            
            const response = await API.delete(`/api/reservations/${this.currentReservation.id}`, {
                reason: 'Cancelled by staff'
            });
            
            if (response.success) {
                UI.showSuccess('Reservation cancelled successfully');
                
                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('reservationModal'));
                modal.hide();
                
                // Refresh calendar
                this.loadReservations();
            } else {
                UI.showError(response.message || 'Failed to cancel reservation');
            }
            
        } catch (error) {
            console.error('Error cancelling reservation:', error);
            UI.showError('Failed to cancel reservation');
        } finally {
            UI.hideLoading();
        }
    }

    getHotelName(hotelId) {
        const hotel = this.hotels.find(h => h.id === hotelId);
        return hotel ? hotel.name : 'Unknown Hotel';
    }

    getRoomNumber(roomId) {
        const room = this.rooms.find(r => r.id === roomId);
        return room ? room.roomNumber : 'Unknown';
    }

    getRoomType(roomId) {
        const room = this.rooms.find(r => r.id === roomId);
        return room ? this.getRoomTypeDisplay(room.type) : 'Unknown Type';
    }

    getRoomTypeDisplay(type) {
        const typeMap = {
            'single': 'Single',
            'double': 'Double',
            'suite': 'Suite',
            'family': 'Family'
        };
        return typeMap[type.toLowerCase()] || type;
    }

    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric'
        });
    }

    formatFullDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            weekday: 'long',
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    }

    calculateNights(checkInDate, checkOutDate) {
        const checkIn = new Date(checkInDate);
        const checkOut = new Date(checkOutDate);
        const timeDiff = checkOut.getTime() - checkIn.getTime();
        return Math.ceil(timeDiff / (1000 * 3600 * 24));
    }

    // Public methods for external use
    refresh() {
        this.loadReservations();
    }

    goToDate(date) {
        this.calendar.gotoDate(date);
    }

    changeView(viewName) {
        this.calendar.changeView(viewName);
    }

    // Cleanup method for SignalR connection
    async cleanup() {
        if (this.signalRManager) {
            await this.signalRManager.disconnect();
        }
        
        if (this.updateTimeout) {
            clearTimeout(this.updateTimeout);
        }
    }
}

// Export for global use
window.CalendarManager = CalendarManager;