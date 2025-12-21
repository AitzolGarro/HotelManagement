// SignalR connection manager for real-time updates
class SignalRManager {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 1000; // Start with 1 second
        this.eventHandlers = new Map();
    }

    async initialize() {
        try {
            // Get JWT token for authentication
            const token = localStorage.getItem('jwt_token');
            
            // Create SignalR connection with authentication
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/reservationHub", {
                    accessTokenFactory: () => token
                })
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        if (retryContext.previousRetryCount < 3) {
                            return Math.random() * 10000;
                        } else {
                            return null; // Stop retrying after 3 attempts
                        }
                    }
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Set up connection event handlers
            this.setupConnectionHandlers();

            // Set up message handlers
            this.setupMessageHandlers();

            // Start the connection
            await this.startConnection();

            console.log('SignalR connection initialized successfully');
        } catch (error) {
            console.error('Error initializing SignalR connection:', error);
            this.scheduleReconnect();
        }
    }

    setupConnectionHandlers() {
        this.connection.onclose(async (error) => {
            this.isConnected = false;
            console.log('SignalR connection closed:', error);
            
            if (error) {
                console.error('Connection closed due to error:', error);
                this.scheduleReconnect();
            }
        });

        this.connection.onreconnecting((error) => {
            this.isConnected = false;
            console.log('SignalR connection lost, attempting to reconnect...', error);
            UI.showInfo('Connection lost, attempting to reconnect...');
        });

        this.connection.onreconnected((connectionId) => {
            this.isConnected = true;
            this.reconnectAttempts = 0;
            console.log('SignalR connection reestablished:', connectionId);
            UI.showSuccess('Connection reestablished');
            
            // Rejoin groups after reconnection
            this.rejoinGroups();
        });
    }

    setupMessageHandlers() {
        // Reservation created
        this.connection.on('ReservationCreated', (data) => {
            console.log('Reservation created:', data);
            this.notifyHandlers('reservationCreated', data);
            UI.showSuccess('New reservation created');
        });

        // Reservation updated
        this.connection.on('ReservationUpdated', (data) => {
            console.log('Reservation updated:', data);
            this.notifyHandlers('reservationUpdated', data);
            UI.showInfo('Reservation updated');
        });

        // Reservation cancelled
        this.connection.on('ReservationCancelled', (data) => {
            console.log('Reservation cancelled:', data);
            this.notifyHandlers('reservationCancelled', data);
            UI.showInfo('Reservation cancelled');
        });

        // Calendar refresh
        this.connection.on('CalendarRefresh', () => {
            console.log('Calendar refresh requested');
            this.notifyHandlers('calendarRefresh');
        });
    }

    async startConnection() {
        try {
            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;
            console.log('SignalR connection started successfully');

            // Join calendar group automatically
            await this.joinCalendarGroup();

        } catch (error) {
            console.error('Error starting SignalR connection:', error);
            throw error;
        }
    }

    async joinCalendarGroup() {
        if (this.isConnected) {
            try {
                await this.connection.invoke('JoinCalendarGroup');
                console.log('Joined calendar group');
            } catch (error) {
                console.error('Error joining calendar group:', error);
            }
        }
    }

    async leaveCalendarGroup() {
        if (this.isConnected) {
            try {
                await this.connection.invoke('LeaveCalendarGroup');
                console.log('Left calendar group');
            } catch (error) {
                console.error('Error leaving calendar group:', error);
            }
        }
    }

    async joinHotelGroup(hotelId) {
        if (this.isConnected && hotelId) {
            try {
                await this.connection.invoke('JoinHotelGroup', hotelId.toString());
                console.log(`Joined hotel group: ${hotelId}`);
            } catch (error) {
                console.error(`Error joining hotel group ${hotelId}:`, error);
            }
        }
    }

    async leaveHotelGroup(hotelId) {
        if (this.isConnected && hotelId) {
            try {
                await this.connection.invoke('LeaveHotelGroup', hotelId.toString());
                console.log(`Left hotel group: ${hotelId}`);
            } catch (error) {
                console.error(`Error leaving hotel group ${hotelId}:`, error);
            }
        }
    }

    async rejoinGroups() {
        // Rejoin calendar group
        await this.joinCalendarGroup();

        // Rejoin hotel groups if any are stored
        const currentHotelFilter = document.getElementById('hotelFilter')?.value;
        if (currentHotelFilter) {
            await this.joinHotelGroup(currentHotelFilter);
        }
    }

    scheduleReconnect() {
        if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            console.error('Max reconnection attempts reached');
            UI.showError('Connection lost. Please refresh the page to reconnect.');
            return;
        }

        this.reconnectAttempts++;
        const delay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1); // Exponential backoff

        console.log(`Scheduling reconnection attempt ${this.reconnectAttempts} in ${delay}ms`);

        setTimeout(async () => {
            try {
                await this.startConnection();
            } catch (error) {
                console.error('Reconnection attempt failed:', error);
                this.scheduleReconnect();
            }
        }, delay);
    }

    // Event handler management
    on(eventName, handler) {
        if (!this.eventHandlers.has(eventName)) {
            this.eventHandlers.set(eventName, []);
        }
        this.eventHandlers.get(eventName).push(handler);
    }

    off(eventName, handler) {
        if (this.eventHandlers.has(eventName)) {
            const handlers = this.eventHandlers.get(eventName);
            const index = handlers.indexOf(handler);
            if (index > -1) {
                handlers.splice(index, 1);
            }
        }
    }

    notifyHandlers(eventName, data = null) {
        if (this.eventHandlers.has(eventName)) {
            const handlers = this.eventHandlers.get(eventName);
            handlers.forEach(handler => {
                try {
                    handler(data);
                } catch (error) {
                    console.error(`Error in event handler for ${eventName}:`, error);
                }
            });
        }
    }

    // Connection status
    getConnectionState() {
        return this.connection ? this.connection.state : 'Disconnected';
    }

    isConnectionActive() {
        return this.isConnected && this.connection && this.connection.state === signalR.HubConnectionState.Connected;
    }

    // Cleanup
    async disconnect() {
        if (this.connection) {
            try {
                await this.leaveCalendarGroup();
                await this.connection.stop();
                console.log('SignalR connection stopped');
            } catch (error) {
                console.error('Error stopping SignalR connection:', error);
            }
        }
    }
}

// Export for global use
window.SignalRManager = SignalRManager;