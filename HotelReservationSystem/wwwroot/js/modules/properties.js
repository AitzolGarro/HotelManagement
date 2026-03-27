// Properties module for hotel and room management
function _propT(key, fallback = key) {
    return (window._I18N && window._I18N[key]) || fallback;
}

async function _propLoadI18n() {
    if (window._I18N) return;
    try {
        const lang = window.__hotelLocale || 'en';
        const res = await fetch('/api/i18n/strings?lang=' + lang + '&v=20260327c');
        const data = await res.json();
        window._I18N = data.strings || {};
    } catch (_) {
        window._I18N = {};
    }
}

class PropertiesManager {
    constructor() {
        this.hotels = [];
        this.selectedHotel = null;
        this.rooms = [];
    }

    async initialize() {
        try {
            console.log('Properties manager initializing...');
            await _propLoadI18n();
            
            // Load hotels data
            await this.loadHotels();
            
            // Setup event listeners
            this.setupEventListeners();
            
            console.log('Properties manager initialized successfully');
        } catch (error) {
            console.error('Error initializing properties manager:', error);
            UI.showError(_propT('Properties_InitFailed', 'Failed to initialize properties management'));
        }
    }

    async loadHotels() {
        try {
            UI.showLoading();
            
            const response = await API.getHotels();
            console.log('Hotels API response:', response);
            
            this.hotels = response || [];
            this.populateHotelsTable();
            
        } catch (error) {
            console.error('Error loading hotels:', error);
            this.showHotelsError(_propT('Properties_LoadHotelsFailed', 'Failed to load hotels'));
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
                        <p class="mt-2">${_propT('Properties_NoHotelsFound', 'No hotels found')}</p>
                        <p class="small text-muted">${_propT('Properties_AddFirstHotel', 'Click "Add Hotel" to create your first hotel')}</p>
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = this.hotels.map(hotel => `
            <tr data-hotel-id="${hotel.id}">
                <td>
                    <strong>${hotel.name}</strong>
                    ${hotel.isActive ? '' : `<span class="badge bg-secondary ms-2">${_propT('Properties_Inactive', 'Inactive')}</span>`}
                </td>
                <td>${hotel.address || _propT('Common_NotSpecified', 'Not specified')}</td>
                <td>${hotel.phone || _propT('Common_NotSpecified', 'Not specified')}</td>
                <td>${hotel.email || _propT('Common_NotSpecified', 'Not specified')}</td>
                <td>
                    <button class="btn btn-link btn-sm p-0" onclick="propertiesManager.showHotelRooms(${hotel.id}, '${hotel.name}')">
                        <i class="bi bi-door-closed"></i>
                        ${_propT('Properties_ViewRooms', 'View Rooms')}
                    </button>
                </td>
                <td>
                    <span class="badge ${hotel.isActive ? 'bg-success' : 'bg-secondary'}">
                        ${hotel.isActive ? _propT('Common_Active', 'Active') : _propT('Properties_Inactive', 'Inactive')}
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
            UI.showError(_propT('Properties_LoadRoomsFailed', 'Failed to load hotel rooms'));
        }
    }

    async loadRooms(hotelId) {
        try {
            const roomsTableBody = document.querySelector('#roomsTable tbody');
            roomsTableBody.innerHTML = `
                <tr>
                    <td colspan="6" class="text-center text-muted py-3">
                        <div class="spinner-border spinner-border-sm me-2" role="status"></div>
                        ${_propT('Properties_LoadingRooms', 'Loading rooms...')}
                    </td>
                </tr>
            `;
            
            const response = await API.get(`/hotels/${hotelId}/rooms`);
            console.log('Rooms API response:', response);
            
            this.rooms = response || [];
            this.populateRoomsTable();
            
        } catch (error) {
            console.error('Error loading rooms:', error);
            this.showRoomsError(_propT('Properties_LoadRoomsFailed', 'Failed to load rooms'));
        }
    }

    populateRoomsTable() {
        const tbody = document.querySelector('#roomsTable tbody');
        
        if (!this.rooms || this.rooms.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="6" class="text-center text-muted py-4">
                        <i class="bi bi-door-closed fs-1"></i>
                        <p class="mt-2">${_propT('Properties_NoRoomsFound', 'No rooms found for this hotel')}</p>
                        <p class="small text-muted">${_propT('Properties_AddFirstRoom', 'Click "Add Room" to create the first room')}</p>
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
                    ${room.capacity} ${room.capacity > 1 ? _propT('GP_Res_Guests', 'guests') : _propT('GP_Res_Guest', 'guest')}
                </td>
                <td>
                    <strong>${UI.formatCurrency(room.baseRate)}</strong>
                    <small class="text-muted">/${_propT('GP_Res_Night', 'night')}</small>
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
            0: _propT('room_type_single', 'Single'),
            1: _propT('room_type_double', 'Double'), 
            2: _propT('room_type_suite', 'Suite'),
            3: _propT('room_type_family', 'Family'),
            'Single': _propT('room_type_single', 'Single'),
            'Double': _propT('room_type_double', 'Double'),
            'Suite': _propT('room_type_suite', 'Suite'), 
            'Family': _propT('room_type_family', 'Family')
        };
        return typeMap[type] || type;
    }

    getRoomStatusDisplay(status) {
        const statusMap = {
            0: _propT('room_status_available', 'Available'),
            1: _propT('room_status_occupied', 'Occupied'),
            2: _propT('room_status_maintenance', 'Maintenance'),
            3: _propT('room_status_out_of_order', 'Out of Order'),
            'Available': _propT('room_status_available', 'Available'),
            'Occupied': _propT('room_status_occupied', 'Occupied'),
            'Maintenance': _propT('room_status_maintenance', 'Maintenance'),
            'OutOfOrder': _propT('room_status_out_of_order', 'Out of Order')
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
        const addHotelBtn = document.getElementById('addHotelBtn');
        if (addHotelBtn) {
            addHotelBtn.addEventListener('click', () => {
                this.showAddHotelModal();
            });
        }

        // Add Room button
        const addRoomBtn = document.getElementById('addRoomBtn');
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
        const existing = document.getElementById('hotelModal');
        if (existing) existing.remove();
        document.body.insertAdjacentHTML('beforeend', `
            <div class="modal fade" id="hotelModal" tabindex="-1">
              <div class="modal-dialog">
                <div class="modal-content">
                  <div class="modal-header">
                    <h5 class="modal-title">${_propT('Properties_AddHotel', 'Add Hotel')}</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                  </div>
                  <form id="hotelForm">
                    <div class="modal-body">
                      <div class="mb-3"><label class="form-label">${_propT('Common_Name', 'Name')}</label><input class="form-control" name="name" required maxlength="200"></div>
                      <div class="mb-3"><label class="form-label">${_propT('GP_Prof_Address', 'Address')}</label><input class="form-control" name="address" maxlength="500"></div>
                      <div class="mb-3"><label class="form-label">${_propT('Calendar_Phone', 'Phone')}</label><input class="form-control" name="phone" maxlength="20"></div>
                      <div class="mb-3"><label class="form-label">${_propT('Calendar_Email', 'Email')}</label><input type="email" class="form-control" name="email" maxlength="100"></div>
                      <div class="form-check form-switch"><input class="form-check-input" type="checkbox" name="isActive" checked><label class="form-check-label">${_propT('Common_Active', 'Active')}</label></div>
                    </div>
                    <div class="modal-footer">
                      <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">${_propT('Common_Cancel', 'Cancel')}</button>
                      <button type="submit" class="btn btn-primary">${_propT('Properties_AddHotel', 'Add Hotel')}</button>
                    </div>
                  </form>
                </div>
              </div>
            </div>`);
        const modalEl = document.getElementById('hotelModal');
        const modal = new bootstrap.Modal(modalEl);
        modal.show();
        modalEl.addEventListener('hidden.bs.modal', () => modalEl.remove());
        document.getElementById('hotelForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            const form = new FormData(e.target);
            const payload = {
                name: form.get('name'),
                address: form.get('address') || null,
                phone: form.get('phone') || null,
                email: form.get('email') || null,
                isActive: form.get('isActive') === 'on'
            };
            try {
                await API.createHotel(payload);
                modal.hide();
                UI.showSuccess(_propT('Properties_AddHotelSuccess', 'Hotel created successfully'));
                await this.loadHotels();
            } catch (error) {
                UI.showError(error.message || _propT('Properties_AddHotelFailed', 'Failed to create hotel'));
            }
        });
    }

    showAddRoomModal() {
        if (!this.selectedHotel) {
            UI.showError(_propT('Properties_SelectHotelFirst', 'Please select a hotel first'));
            return;
        }
        const existing = document.getElementById('roomModal');
        if (existing) existing.remove();
        document.body.insertAdjacentHTML('beforeend', `
            <div class="modal fade" id="roomModal" tabindex="-1">
              <div class="modal-dialog">
                <div class="modal-content">
                  <div class="modal-header">
                    <h5 class="modal-title">${_propT('Properties_AddRoom', 'Add Room')} - ${this.selectedHotel.name}</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                  </div>
                  <form id="roomForm">
                    <div class="modal-body">
                      <div class="mb-3"><label class="form-label">${_propT('Properties_RoomNumber', 'Room Number')}</label><input class="form-control" name="roomNumber" required maxlength="10"></div>
                      <div class="mb-3"><label class="form-label">${_propT('Properties_Type', 'Type')}</label>
                        <select class="form-select" name="type" required>
                          <option value="1">${_propT('room_type_single','Single')}</option>
                          <option value="2">${_propT('room_type_double','Double')}</option>
                          <option value="3">${_propT('room_type_suite','Suite')}</option>
                          <option value="4">${_propT('room_type_family','Family')}</option>
                          <option value="5">${_propT('room_type_deluxe','Deluxe')}</option>
                          <option value="6">${_propT('room_type_twin','Twin')}</option>
                          <option value="7">${_propT('room_type_triple','Triple')}</option>
                          <option value="8">${_propT('room_type_quad','Quad')}</option>
                          <option value="9">${_propT('room_type_standard','Standard')}</option>
                        </select>
                      </div>
                      <div class="mb-3"><label class="form-label">${_propT('Properties_Capacity', 'Capacity')}</label><input type="number" min="1" max="20" class="form-control" name="capacity" required value="2"></div>
                      <div class="mb-3"><label class="form-label">${_propT('Properties_BaseRate', 'Base Rate')}</label><input type="number" min="0.01" step="0.01" class="form-control" name="baseRate" required value="100"></div>
                      <div class="mb-3"><label class="form-label">${_propT('Calendar_Status', 'Status')}</label>
                        <select class="form-select" name="status">
                          <option value="1">${_propT('room_status_available','Available')}</option>
                          <option value="2">${_propT('room_status_maintenance','Maintenance')}</option>
                          <option value="3">${_propT('room_status_blocked','Blocked')}</option>
                          <option value="4">${_propT('room_status_out_of_order','Out of Order')}</option>
                          <option value="5">${_propT('room_status_occupied','Occupied')}</option>
                          <option value="6">${_propT('room_status_cleaning','Cleaning')}</option>
                        </select>
                      </div>
                      <div class="mb-3"><label class="form-label">Description</label><textarea class="form-control" name="description" rows="3" maxlength="1000"></textarea></div>
                    </div>
                    <div class="modal-footer">
                      <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">${_propT('Common_Cancel', 'Cancel')}</button>
                      <button type="submit" class="btn btn-primary">${_propT('Properties_AddRoom', 'Add Room')}</button>
                    </div>
                  </form>
                </div>
              </div>
            </div>`);
        const modalEl = document.getElementById('roomModal');
        const modal = new bootstrap.Modal(modalEl);
        modal.show();
        modalEl.addEventListener('hidden.bs.modal', () => modalEl.remove());
        document.getElementById('roomForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            const form = new FormData(e.target);
            const payload = {
                hotelId: this.selectedHotel.id,
                roomNumber: form.get('roomNumber'),
                type: parseInt(form.get('type'), 10),
                capacity: parseInt(form.get('capacity'), 10),
                baseRate: parseFloat(form.get('baseRate')),
                status: parseInt(form.get('status'), 10),
                description: form.get('description') || null
            };
            try {
                await API.createRoom(this.selectedHotel.id, payload);
                modal.hide();
                UI.showSuccess(_propT('Properties_AddRoomSuccess', 'Room created successfully'));
                await this.loadRooms(this.selectedHotel.id);
            } catch (error) {
                UI.showError(error.message || _propT('Properties_AddRoomFailed', 'Failed to create room'));
            }
        });
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
