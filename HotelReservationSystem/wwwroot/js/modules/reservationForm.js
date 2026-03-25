// Reservation form handling for the reservations page
class ReservationForm {
    constructor() {
        this.initializeForm();
    }

    initializeForm() {
        // Set up date validation
        this.setupDateValidation();
        
        // Set up room selection based on hotel
        this.setupHotelRoomSelection();
        
        // Set up form submission handling
        this.setupFormSubmission();
    }

    setupDateValidation() {
        // Ensure check-out date is after check-in date
        const checkInInput = document.getElementById('checkInDate');
        const checkOutInput = document.getElementById('checkOutDate');

        if (checkInInput && checkOutInput) {
            checkInInput.addEventListener('change', () => {
                this.validateDates();
            });
            
            checkOutInput.addEventListener('change', () => {
                this.validateDates();
            });
        }
    }

    validateDates() {
        const checkInInput = document.getElementById('checkInDate');
        const checkOutInput = document.getElementById('checkOutDate');

        if (checkInInput && checkOutInput) {
            const checkInDate = new Date(checkInInput.value);
            const checkOutDate = new Date(checkOutInput.value);

            if (checkInDate && checkOutDate && checkOutDate <= checkInDate) {
                UI.showError('Check-out date must be after check-in date');
                checkOutInput.setCustomValidity('Check-out date must be after check-in date');
            } else {
                checkOutInput.setCustomValidity('');
            }
        }
    }

    setupHotelRoomSelection() {
        const hotelSelect = document.getElementById('reservationHotel');
        const roomSelect = document.getElementById('reservationRoom');

        if (hotelSelect && roomSelect) {
            hotelSelect.addEventListener('change', () => {
                this.loadRoomsForHotel(hotelSelect.value);
            });
        }
    }

    async loadRoomsForHotel(hotelId) {
        try {
            if (!hotelId) {
                // Reset room selection if no hotel selected
                const roomSelect = document.getElementById('reservationRoom');
                if (roomSelect) {
                    roomSelect.innerHTML = '<option value="">Select Room</option>';
                    roomSelect.disabled = true;
                }
                return;
            }

            const rooms = await API.getRooms(hotelId);
            const roomSelect = document.getElementById('reservationRoom');
            
            if (roomSelect) {
                roomSelect.innerHTML = '<option value="">Select Room</option>';
                roomSelect.disabled = false;
                
                rooms.forEach(room => {
                    const option = document.createElement('option');
                    option.value = room.id;
                    option.textContent = `${room.roomNumber} - ${this.getRoomTypeDisplayName(room.type)} (${room.capacity} guests)`;
                    option.dataset.baseRate = room.baseRate;
                    roomSelect.appendChild(option);
                });
            }
        } catch (error) {
            console.error('Error loading rooms:', error);
            UI.showError('Failed to load rooms for selected hotel');
        }
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

    setupFormSubmission() {
        const form = document.getElementById('reservationForm');
        
        if (form) {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                
                // Validate form
                if (UI.validateFormWithFeedback(form)) {
                    this.submitReservationForm();
                }
            });
        }
    }

    async submitReservationForm() {
        try {
            const form = document.getElementById('reservationForm');
            const formData = new FormData(form);
            
            // Collect form data
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
            
            // Reset and refresh form
            form.reset();
            
            UI.showSuccess('Reservation created successfully');
            
        } catch (error) {
            console.error('Error saving reservation:', error);
            UI.showError('Failed to save reservation');
        }
    }
}

// Initialize form when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // We're initializing in site.js directly, so we don't need this
    // If using with the dedicated reservationForm module, we would use this
    // For now, we'll keep the original code structure
});