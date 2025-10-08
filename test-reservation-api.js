// Mock API for testing reservation management interface
class MockReservationAPI {
    constructor() {
        this.hotels = [
            { id: 1, name: 'Grand Hotel Plaza', address: '123 Main St', isActive: true },
            { id: 2, name: 'Seaside Resort', address: '456 Ocean Ave', isActive: true }
        ];
        
        this.rooms = [
            { id: 1, hotelId: 1, roomNumber: '101', type: 1, capacity: 2, baseRate: 150.00, status: 1 },
            { id: 2, hotelId: 1, roomNumber: '102', type: 0, capacity: 1, baseRate: 120.00, status: 1 },
            { id: 3, hotelId: 2, roomNumber: '201', type: 3, capacity: 4, baseRate: 300.00, status: 1 },
            { id: 4, hotelId: 2, roomNumber: '202', type: 2, capacity: 2, baseRate: 180.00, status: 1 }
        ];
        
        this.reservations = [
            {
                id: 1,
                hotelId: 1,
                roomId: 1,
                guestId: 1,
                bookingReference: 'BK001',
                source: 0,
                checkInDate: '2024-10-15',
                checkOutDate: '2024-10-18',
                numberOfGuests: 2,
                totalAmount: 450.00,
                status: 1,
                specialRequests: 'Late check-in requested',
                internalNotes: 'VIP guest',
                hotelName: 'Grand Hotel Plaza',
                roomNumber: '101',
                guestName: 'John Doe',
                guestEmail: 'john.doe@email.com',
                guestPhone: '+1234567890',
                createdAt: '2024-10-07T10:00:00Z',
                updatedAt: '2024-10-07T10:00:00Z'
            },
            {
                id: 2,
                hotelId: 2,
                roomId: 3,
                guestId: 2,
                bookingReference: 'BK002',
                source: 1,
                checkInDate: '2024-10-20',
                checkOutDate: '2024-10-25',
                numberOfGuests: 4,
                totalAmount: 1500.00,
                status: 0,
                specialRequests: 'Extra towels needed',
                internalNotes: 'Family with children',
                hotelName: 'Seaside Resort',
                roomNumber: '201',
                guestName: 'Jane Smith',
                guestEmail: 'jane.smith@email.com',
                guestPhone: '+0987654321',
                createdAt: '2024-10-06T14:30:00Z',
                updatedAt: '2024-10-06T14:30:00Z'
            }
        ];
        
        this.nextReservationId = 3;
    }
    
    // Mock API methods
    async getHotels() {
        return new Promise(resolve => {
            setTimeout(() => resolve(this.hotels), 100);
        });
    }
    
    async getRooms(hotelId) {
        return new Promise(resolve => {
            setTimeout(() => {
                const hotelRooms = this.rooms.filter(room => room.hotelId == hotelId);
                resolve(hotelRooms);
            }, 100);
        });
    }
    
    async getReservations(params = {}) {
        return new Promise(resolve => {
            setTimeout(() => {
                let filtered = [...this.reservations];
                
                if (params.hotelId) {
                    filtered = filtered.filter(r => r.hotelId == params.hotelId);
                }
                
                if (params.status !== undefined) {
                    filtered = filtered.filter(r => r.status == params.status);
                }
                
                if (params.from) {
                    filtered = filtered.filter(r => r.checkInDate >= params.from);
                }
                
                if (params.to) {
                    filtered = filtered.filter(r => r.checkOutDate <= params.to);
                }
                
                resolve(filtered);
            }, 200);
        });
    }
    
    async getReservation(id) {
        return new Promise(resolve => {
            setTimeout(() => {
                const reservation = this.reservations.find(r => r.id == id);
                resolve(reservation);
            }, 100);
        });
    }
    
    async createManualReservation(data) {
        return new Promise(resolve => {
            setTimeout(() => {
                const newReservation = {
                    id: this.nextReservationId++,
                    hotelId: data.hotelId,
                    roomId: data.roomId,
                    guestId: this.nextReservationId,
                    bookingReference: data.bookingReference || `MAN${this.nextReservationId}`,
                    source: 0, // Manual
                    checkInDate: data.checkInDate,
                    checkOutDate: data.checkOutDate,
                    numberOfGuests: data.numberOfGuests,
                    totalAmount: data.totalAmount,
                    status: data.status || 0,
                    specialRequests: data.specialRequests,
                    internalNotes: data.internalNotes,
                    hotelName: this.hotels.find(h => h.id == data.hotelId)?.name || 'Unknown Hotel',
                    roomNumber: this.rooms.find(r => r.id == data.roomId)?.roomNumber || 'Unknown Room',
                    guestName: `${data.guestFirstName} ${data.guestLastName}`,
                    guestEmail: data.guestEmail,
                    guestPhone: data.guestPhone,
                    createdAt: new Date().toISOString(),
                    updatedAt: new Date().toISOString()
                };
                
                this.reservations.push(newReservation);
                resolve(newReservation);
            }, 300);
        });
    }
    
    async updateReservation(id, data) {
        return new Promise(resolve => {
            setTimeout(() => {
                const index = this.reservations.findIndex(r => r.id == id);
                if (index !== -1) {
                    this.reservations[index] = {
                        ...this.reservations[index],
                        ...data,
                        updatedAt: new Date().toISOString()
                    };
                    resolve(this.reservations[index]);
                } else {
                    throw new Error('Reservation not found');
                }
            }, 200);
        });
    }
    
    async checkAvailability(data) {
        return new Promise(resolve => {
            setTimeout(() => {
                // Simple availability check - assume available if no conflicts
                const conflicts = this.reservations.filter(r => 
                    r.roomId == data.roomId &&
                    r.status !== 4 && // Not cancelled
                    r.id !== data.excludeReservationId &&
                    ((r.checkInDate <= data.checkInDate && r.checkOutDate > data.checkInDate) ||
                     (r.checkInDate < data.checkOutDate && r.checkOutDate >= data.checkOutDate) ||
                     (r.checkInDate >= data.checkInDate && r.checkOutDate <= data.checkOutDate))
                );
                
                resolve({
                    isAvailable: conflicts.length === 0,
                    conflicts: conflicts.map(c => ({
                        reservationId: c.id,
                        bookingReference: c.bookingReference,
                        checkInDate: c.checkInDate,
                        checkOutDate: c.checkOutDate,
                        guestName: c.guestName,
                        status: c.status,
                        conflictType: 'overlap',
                        description: 'Date range overlaps with existing reservation'
                    }))
                });
            }, 150);
        });
    }
    
    async cancelReservation(id, reason) {
        return new Promise(resolve => {
            setTimeout(() => {
                const index = this.reservations.findIndex(r => r.id == id);
                if (index !== -1) {
                    this.reservations[index].status = 4; // Cancelled
                    this.reservations[index].internalNotes += `\nCancelled: ${reason}`;
                    this.reservations[index].updatedAt = new Date().toISOString();
                    resolve(true);
                } else {
                    throw new Error('Reservation not found');
                }
            }, 200);
        });
    }
    
    async checkInReservation(id) {
        return new Promise(resolve => {
            setTimeout(() => {
                const index = this.reservations.findIndex(r => r.id == id);
                if (index !== -1) {
                    this.reservations[index].status = 2; // Checked In
                    this.reservations[index].updatedAt = new Date().toISOString();
                    resolve(this.reservations[index]);
                } else {
                    throw new Error('Reservation not found');
                }
            }, 200);
        });
    }
    
    async checkOutReservation(id) {
        return new Promise(resolve => {
            setTimeout(() => {
                const index = this.reservations.findIndex(r => r.id == id);
                if (index !== -1) {
                    this.reservations[index].status = 3; // Checked Out
                    this.reservations[index].updatedAt = new Date().toISOString();
                    resolve(this.reservations[index]);
                } else {
                    throw new Error('Reservation not found');
                }
            }, 200);
        });
    }
}

// Test the reservation management functionality
function testReservationInterface() {
    const mockAPI = new MockReservationAPI();
    
    console.log('Testing Reservation Management Interface...');
    
    // Test 1: Load hotels
    mockAPI.getHotels().then(hotels => {
        console.log('✓ Hotels loaded:', hotels.length);
    });
    
    // Test 2: Load rooms for hotel
    mockAPI.getRooms(1).then(rooms => {
        console.log('✓ Rooms loaded for hotel 1:', rooms.length);
    });
    
    // Test 3: Load reservations
    mockAPI.getReservations().then(reservations => {
        console.log('✓ Reservations loaded:', reservations.length);
    });
    
    // Test 4: Check availability
    mockAPI.checkAvailability({
        roomId: 1,
        checkInDate: '2024-10-10',
        checkOutDate: '2024-10-12'
    }).then(result => {
        console.log('✓ Availability check:', result.isAvailable ? 'Available' : 'Not available');
    });
    
    // Test 5: Create manual reservation
    mockAPI.createManualReservation({
        hotelId: 1,
        roomId: 2,
        checkInDate: '2024-10-25',
        checkOutDate: '2024-10-27',
        numberOfGuests: 1,
        totalAmount: 240.00,
        guestFirstName: 'Test',
        guestLastName: 'User',
        guestEmail: 'test@example.com',
        specialRequests: 'Test reservation'
    }).then(reservation => {
        console.log('✓ Manual reservation created:', reservation.id);
    });
    
    console.log('All tests initiated. Check console for results.');
}

// Export for use in browser
if (typeof window !== 'undefined') {
    window.MockReservationAPI = MockReservationAPI;
    window.testReservationInterface = testReservationInterface;
}

// Export for Node.js
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { MockReservationAPI, testReservationInterface };
}