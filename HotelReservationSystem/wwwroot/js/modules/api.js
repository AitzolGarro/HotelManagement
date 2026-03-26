// API communication module
class ApiClient {
    constructor() {
        this.baseUrl = '/api';
        this.defaultHeaders = {
            'Content-Type': 'application/json'
        };
    }

    // Get authorization header with JWT token
    getAuthHeaders() {
        const token = localStorage.getItem('jwt_token');
        console.log('JWT Token for API request:', token ? 'Token exists' : 'No token found'); // Debug log
        return token ? { ...this.defaultHeaders, 'Authorization': `Bearer ${token}` } : this.defaultHeaders;
    }

    // Generic request method with error handling and loading states
    async request(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;
        const config = {
            headers: this.getAuthHeaders(),
            credentials: 'same-origin',
            ...options
        };

        // Show loading state only if not explicitly disabled
        if (options.showLoading !== false) {
            UI.showLoading();
        }

        try {
            const response = await fetch(url, config);
            
            // Handle authentication errors
            if (response.status === 401) {
                // Don't logout if we're already on the login page or trying to login
                if (!window.location.pathname.includes('/login') && !endpoint.includes('/auth/login')) {
                    // Store current page for redirect after login
                    sessionStorage.setItem('intended_url', window.location.pathname + window.location.search);
                    Auth.logout();
                }
                throw new Error('Authentication required');
            }

            // Handle forbidden errors
            if (response.status === 403) {
                throw new Error('You do not have permission to perform this action');
            }

            // Handle other HTTP errors
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                
                // Create enhanced error object with server response details
                const error = new Error(errorData.message || `HTTP ${response.status}: ${response.statusText}`);
                error.statusCode = response.status;
                error.details = errorData.details;
                error.traceId = errorData.traceId;
                error.timestamp = errorData.timestamp;
                
                throw error;
            }

            // Return JSON data or empty object for no content
            return response.status === 204 ? {} : await response.json();
        } catch (error) {
            console.error('API request failed:', error);
            
            // Don't show error UI for silent requests
            if (options.silent !== true) {
                UI.handleApiError(error, endpoint);
            }
            throw error;
        } finally {
            if (options.showLoading !== false) {
                UI.hideLoading();
            }
        }
    }

    // GET request
    async get(endpoint, params = {}) {
        const queryString = new URLSearchParams(params).toString();
        const url = queryString ? `${endpoint}?${queryString}` : endpoint;
        return this.request(url, { method: 'GET' });
    }

    // POST request
    async post(endpoint, data = {}) {
        return this.request(endpoint, {
            method: 'POST',
            body: JSON.stringify(data)
        });
    }

    // PUT request
    async put(endpoint, data = {}) {
        return this.request(endpoint, {
            method: 'PUT',
            body: JSON.stringify(data)
        });
    }

    // DELETE request
    async delete(endpoint) {
        return this.request(endpoint, { method: 'DELETE' });
    }

    // Hotels API
    async getHotels() {
        const response = await this.get('/hotels');
        return (response && response.items) ? response.items : response;
    }

    async getHotel(id) {
        return this.get(`/hotels/${id}`);
    }

    async createHotel(hotelData) {
        return this.post('/hotels', hotelData);
    }

    async updateHotel(id, hotelData) {
        return this.put(`/hotels/${id}`, hotelData);
    }

    async deleteHotel(id) {
        return this.delete(`/hotels/${id}`);
    }

    // Rooms API
    async getAllRooms() {
        const response = await this.get('/rooms');
        return (response && response.items) ? response.items : response;
    }

    async getRooms(hotelId) {
        const response = await this.get(`/hotels/${hotelId}/rooms`);
        return (response && response.items) ? response.items : response;
    }

    async getRoom(hotelId, roomId) {
        return this.get(`/hotels/${hotelId}/rooms/${roomId}`);
    }

    async createRoom(hotelId, roomData) {
        return this.post(`/hotels/${hotelId}/rooms`, roomData);
    }

    async updateRoom(hotelId, roomId, roomData) {
        return this.put(`/hotels/${hotelId}/rooms/${roomId}`, roomData);
    }

    async deleteRoom(hotelId, roomId) {
        return this.delete(`/hotels/${hotelId}/rooms/${roomId}`);
    }

    // Reservations API
    async getReservations(params = {}) {
        const response = await this.get('/reservations', params);
        return (response && response.items) ? response.items : response;
    }

    async getReservation(id) {
        return this.get(`/reservations/${id}`);
    }

    async createReservation(reservationData) {
        return this.post('/reservations', reservationData);
    }

    async updateReservation(id, reservationData) {
        return this.put(`/reservations/${id}`, reservationData);
    }

    async cancelReservation(id, reason) {
        return this.request(`/reservations/${id}`, {
            method: 'DELETE',
            body: JSON.stringify({ reason })
        });
    }

    // PATCH reservation dates (drag-and-drop calendar)
    async updateReservationDates(id, checkInDate, checkOutDate, roomId = null) {
        const body = { checkInDate, checkOutDate };
        if (roomId !== null) body.roomId = roomId;
        return this.request(`/reservations/${id}/dates`, {
            method: 'PATCH',
            body: JSON.stringify(body)
        });
    }

    async checkAvailability(roomId, checkIn, checkOut) {
        return this.get('/reservations/availability', {
            roomId,
            checkIn: checkIn.toISOString().split('T')[0],
            checkOut: checkOut.toISOString().split('T')[0]
        });
    }

    // Authentication API
    async login(credentials) {
        return this.request('/auth/login', {
            method: 'POST',
            body: JSON.stringify(credentials)
        });
    }

    async register(userData) {
        return this.request('/auth/register', {
            method: 'POST',
            body: JSON.stringify(userData)
        });
    }

    async refreshToken() {
        const refreshToken = localStorage.getItem('refresh_token');
        return this.request('/auth/refresh', {
            method: 'POST',
            body: JSON.stringify({ refreshToken })
        });
    }

    async getCurrentUser() {
        return this.get('/auth/me');
    }

    async challenge2FA(challengeToken, code) {
        return this.post('/auth/2fa/challenge', {
            challengeToken,
            code
        });
    }

    async setup2FA() {
        return this.post('/auth/2fa/setup');
    }

    async enable2FA(code) {
        return this.post('/auth/2fa/enable', {
            verificationCode: code
        });
    }

    async disable2FA(password) {
        return this.post('/auth/2fa/disable', {
            password
        });
    }

    // Silent keep-alive request for session management
    async keepAlive() {
        return this.request('/auth/me', {
            method: 'GET',
            silent: true,
            showLoading: false
        });
    }

    // Dashboard API
    async getDashboardData() {
        return this.get('/dashboard');
    }

    async getOccupancyStats(dateRange) {
        return this.get('/dashboard/occupancy', dateRange);
    }

    async getRevenueStats(dateRange) {
        return this.get('/dashboard/revenue', dateRange);
    }
}

// Create global API instance
window.API = new ApiClient();
