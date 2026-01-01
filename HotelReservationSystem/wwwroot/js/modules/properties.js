// Properties module for hotel and room management
class PropertiesManager {
    constructor() {
        this.hotels = [];
        this.selectedHotel = null;
        this.rooms = [];
    }

    async initialize() {
        try {
            console.log('Properties manager initializing...');
            
            // Load hotels data
            await this.loadHotels();
            
            // Setup event listeners
            this.setupEventListeners();
            
            console.log('Properties manager initialized successfully');
        } catch (error) {
            console.error('Error initializing properties manager:', error);
            UI.showError('Failed to initialize properties management');
        }
    }

    async loadHotels() {
        try {
            UI.showLoading();
            
            const response = await API.get('/hotels');
            console.log('Hotels API response:', response);
            
            this.hotels = response || [];
            this.populateHotelsTable();
            
        } catch (error) {
            console.error('Error loading hotels:', error);
            this.showHotelsError('Failed to load hotels');
        } finally {
            UI.hideLoading();
        }
    }

    populateHotelsTable() {
        const tbody = document.querySelector('#hotelsTable tbody');
        
        if (!this.hotels || this.hotels.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="7" class="text-center text-muted py-4">
                        <i class="bi bi-building fs-1"></i>
                        <p class="mt-2">No hotels found</p>
                        <p class="small text-muted">Click "Add Hotel" to create your first hotel</p>
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = this.hotels.map(hotel => `
            <tr data-hotel-id="${hotel.id}">
                <td>
                    <strong>${hotel.name}</strong>
                    ${hotel.isActive ? '' : '<span class="badge bg-secondary ms-2">Inactive</span>'}
                </td>
                <td>${hotel.address || 'Not specified'}</td>
                <td>${hotel.phone || 'Not specified'}</td>
                <td>${hotel.email || 'Not specified'}</td>
                <td>
                    <button class="btn btn-link btn-sm p-0" onclick="propertiesManager.showHotelRooms(${hotel.id}, '${hotel.name}')">
                        <i class="bi bi-door-closed"></i>
                        View Rooms
                    </button>
                </td>
                <td>
                    <span class="badge ${hotel.isActive ? 'bg-success' : 'bg-secondary'}">
                        ${hotel.isActive ? 'Active' : 'Inactive'}
                    </span>
                </td>
                <td>
                    <div class="btn-group btn-group-sm" role="group">
                        <button type="button" class="btn btn-outline-primary" onclick="propertiesManager.editHotel(${hotel.id})" data-role="manager">
                            <i class="bi bi-pencil"></i>
                        </button>
                        <button type="button" class="btn btn-outline-danger" onclick="propertiesManager.deleteHotel(${hotel.id})" data-role="admin">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
    }

    async showHotelRooms(hotelId, hotelName) {
        try {
            this.selectedHotel = { id: hotelId, name: hotelName };
            
            // Update UI
            document.getElementById('selectedHotelName').textContent = hotelName;
            document.getElementById('roomsSection').style.display = 'block';
            
            // Scroll to rooms section
            document.getElementById('roomsSection').scrollIntoView({ behavior: 'smooth' });
            
            // Load rooms
            await this.loadRooms(hotelId);
            
        } catch (error) {
            console.error('Error showing hotel rooms:', error);
            UI.showError('Failed to load hotel rooms');
        }
    }

    async loadRooms(hotelId) {
        try {
            const roomsTableBody = document.querySelector('#roomsTable tbody');
            roomsTableBody.innerHTML = `
                <tr>
                    <td colspan="6" class="text-center text-muted py-3">
                        <div class="spinner-border spinner-border-sm me-2" role="status"></div>
                        Loading rooms...
                    </td>
                </tr>
            `;
            
            const response = await API.get(`/hotels/${hotelId}/rooms`);
            console.log('Rooms API response:', response);
            
            this.rooms = response || [];
            this.populateRoomsTable();
            
        } catch (error) {
            console.error('Error loading rooms:', error);
            this.showRoomsError('Failed to load rooms');
        }
    }

    populateRoomsTable() {
        const tbody = document.querySelector('#roomsTable tbody');
        
        if (!this.rooms || this.rooms.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="6" class="text-center text-muted py-4">
                        <i class="bi bi-door-closed fs-1"></i>
                        <p class="mt-2">No rooms found for this hotel</p>
                        <p class="small text-muted">Click "Add Room" to create the first room</p>
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = this.rooms.map(room => `
            <tr data-room-id="${room.id}">
                <td><strong>${room.roomNumber}</strong></td>
                <td>
                    <span class="badge bg-info">${this.getRoomTypeDisplay(room.type)}</span>
                </td>
                <td>
                    <i class="bi bi-people"></i>
                    ${room.capacity} guest${room.capacity > 1 ? 's' : ''}
                </td>
                <td>
                    <strong>$${room.baseRate.toFixed(2)}</strong>
                    <small class="text-muted">/night</small>
                </td>
                <td>
                    <span class="badge ${this.getRoomStatusClass(room.status)}">
                        ${this.getRoomStatusDisplay(room.status)}
                    </span>
                </td>
                <td>
                    <div class="btn-group btn-group-sm" role="group">
                        <button type="button" class="btn btn-outline-primary" onclick="propertiesManager.editRoom(${room.id})" data-role="manager">
                            <i class="bi bi-pencil"></i>
                        </button>
                        <button type="button" class="btn btn-outline-danger" onclick="propertiesManager.deleteRoom(${room.id})" data-role="admin">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
    }

    getRoomTypeDisplay(type) {
        const typeMap = {
            0: 'Single',
            1: 'Double', 
            2: 'Suite',
            3: 'Family',
            'Single': 'Single',
            'Double': 'Double',
            'Suite': 'Suite', 
            'Family': 'Family'
        };
        return typeMap[type] || type;
    }

    getRoomStatusDisplay(status) {
        const statusMap = {
            0: 'Available',
            1: 'Occupied',
            2: 'Maintenance',
            3: 'Out of Order',
            'Available': 'Available',
            'Occupied': 'Occupied',
            'Maintenance': 'Maintenance',
            'OutOfOrder': 'Out of Order'
        };
        return statusMap[status] || status;
    }

    getRoomStatusClass(status) {
        const classMap = {
            0: 'bg-success',
            1: 'bg-warning',
            2: 'bg-info',
            3: 'bg-danger',
            'Available': 'bg-success',
            'Occupied': 'bg-warning', 
            'Maintenance': 'bg-info',
            'OutOfOrder': 'bg-danger'
        };
        return classMap[status] || 'bg-secondary';
    }

    setupEventListeners() {
        // Add Hotel button
        const addHotelBtn = document.querySelector('[data-role="admin"]');
        if (addHotelBtn) {
            addHotelBtn.addEventListener('click', () => {
                this.showAddHotelModal();
            });
        }

        // Add Room button
        const addRoomBtn = document.querySelector('#roomsSection [data-role="manager"]');
        if (addRoomBtn) {
            addRoomBtn.addEventListener('click', () => {
                this.showAddRoomModal();
            });
        }
    }

    showHotelsError(message) {
        const tbody = document.querySelector('#hotelsTable tbody');
        tbody.innerHTML = `
            <tr>
                <td colspan="7" class="text-center text-danger py-4">
                    <i class="bi bi-exclamation-triangle fs-1"></i>
                    <p class="mt-2">${message}</p>
                    <button class="btn btn-outline-primary btn-sm" onclick="propertiesManager.loadHotels()">
                        <i class="bi bi-arrow-clockwise"></i>
                        Retry
                    </button>
                </td>
            </tr>
        `;
    }

    showRoomsError(message) {
        const tbody = document.querySelector('#roomsTable tbody');
        tbody.innerHTML = `
            <tr>
                <td colspan="6" class="text-center text-danger py-4">
                    <i class="bi bi-exclamation-triangle fs-1"></i>
                    <p class="mt-2">${message}</p>
                    <button class="btn btn-outline-primary btn-sm" onclick="propertiesManager.loadRooms(${this.selectedHotel?.id})">
                        <i class="bi bi-arrow-clockwise"></i>
                        Retry
                    </button>
                </td>
            </tr>
        `;
    }

    // Placeholder methods for future implementation
    showAddHotelModal() {
        UI.showInfo('Add Hotel functionality will be implemented in a future update');
    }

    showAddRoomModal() {
        if (!this.selectedHotel) {
            UI.showError('Please select a hotel first');
            return;
        }
        UI.showInfo('Add Room functionality will be implemented in a future update');
    }

    editHotel(hotelId) {
        UI.showInfo('Edit Hotel functionality will be implemented in a future update');
    }

    editRoom(roomId) {
        UI.showInfo('Edit Room functionality will be implemented in a future update');
    }

    async deleteHotel(hotelId) {
        const hotel = this.hotels.find(h => h.id === hotelId);
        if (!hotel) return;

        const confirmed = confirm(`Are you sure you want to delete "${hotel.name}"? This action cannot be undone.`);
        if (!confirmed) return;

        UI.showInfo('Delete Hotel functionality will be implemented in a future update');
    }

    async deleteRoom(roomId) {
        const room = this.rooms.find(r => r.id === roomId);
        if (!room) return;

        const confirmed = confirm(`Are you sure you want to delete room "${room.roomNumber}"? This action cannot be undone.`);
        if (!confirmed) return;

        UI.showInfo('Delete Room functionality will be implemented in a future update');
    }

    // Public methods for external use
    refresh() {
        this.loadHotels();
        if (this.selectedHotel) {
            this.loadRooms(this.selectedHotel.id);
        }
    }

    hideRoomsSection() {
        document.getElementById('roomsSection').style.display = 'none';
        this.selectedHotel = null;
        this.rooms = [];
    }
}

// Export for global use
window.PropertiesManager = PropertiesManager;