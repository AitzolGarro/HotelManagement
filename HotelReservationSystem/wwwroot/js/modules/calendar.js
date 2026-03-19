/**
 * CalendarManager - Full drag-and-drop calendar interface
 * Uses FullCalendar v6 with resourceTimeline plugin (loaded via CDN globals)
 * Subtasks: 9.1 FullCalendar integration, 9.2 Drag-drop, 9.3 Filters/views, 9.4 SignalR real-time
 */
class CalendarManager {
    constructor() {
        this.calendar = null;
        this.hotels = [];
        this.rooms = [];
        this.signalRManager = null;
        this.currentTooltip = null;
        this.updateTimeout = null;
        this._pendingDrop = null; // stores drag info awaiting confirmation

        // 9.3 – filter state
        this.currentFilters = {
            hotel: '',
            roomType: '',
            status: '',
            dateRange: null
        };

        // 9.3 – localStorage persistence key
        this._STATE_KEY = 'calendarState_v2';
    }

    // ─────────────────────────────────────────────────────────────
    // INIT
    // ─────────────────────────────────────────────────────────────
    async initialize() {
        try {
            this._restoreState();
            await this._loadHotels();
            await this._loadRooms();
            this._initCalendar();
            this._initDateRangePicker();
            this._bindFilterEvents();
            this._bindQuickNav();
            this._bindModalButtons();
            await this._initSignalR();
            await this._loadReservations();
            console.log('[Calendar] Initialized successfully');
        } catch (err) {
            console.error('[Calendar] Init error:', err);
            UI.showError('Failed to initialize calendar');
        }
    }

    async cleanup() {
        if (this.signalRManager) {
            await this.signalRManager.disconnect();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 9.3 – STATE PERSISTENCE
    // ─────────────────────────────────────────────────────────────
    _saveState() {
        try {
            const state = {
                view: this.calendar ? this.calendar.view.type : 'resourceTimelineMonth',
                date: this.calendar ? this.calendar.getDate().toISOString() : null,
                filters: this.currentFilters
            };
            localStorage.setItem(this._STATE_KEY, JSON.stringify(state));
        } catch (_) { /* ignore quota errors */ }
    }

    _restoreState() {
        try {
            const raw = localStorage.getItem(this._STATE_KEY);
            if (!raw) return;
            const state = JSON.parse(raw);
            if (state.filters) {
                // Sanitize: coerce all filter values to strings to guard against stale integer values
                const f = state.filters;
                this.currentFilters = {
                    ...this.currentFilters,
                    hotel:     String(f.hotel    || ''),
                    roomType:  String(f.roomType || ''),
                    status:    String(f.status   || ''),
                    dateRange: f.dateRange || null
                };
            }
            this._restoredView = state.view || 'resourceTimelineMonth';
            this._restoredDate = state.date ? new Date(state.date) : new Date();
        } catch (_) {
            this._restoredView = 'resourceTimelineMonth';
            this._restoredDate = new Date();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // DATA LOADING
    // ─────────────────────────────────────────────────────────────
    async _loadHotels() {
        try {
            this.hotels = await API.getHotels() || [];
            this._populateHotelFilter();
        } catch (err) {
            console.error('[Calendar] loadHotels error:', err);
            this.hotels = [];
        }
    }

    async _loadRooms() {
        try {
            this.rooms = await API.getAllRooms() || [];
        } catch (err) {
            console.error('[Calendar] loadRooms error:', err);
            this.rooms = [];
        }
    }

    async _loadReservations() {
        if (!this.calendar) return;
        try {
            this._showCalLoading(true);
            const view = this.calendar.view;
            const params = {
                from: new Date(view.activeStart).toISOString().split('T')[0],
                to:   new Date(view.activeEnd).toISOString().split('T')[0],
                pageSize: 2000
            };
            if (this.currentFilters.hotel)  params.hotelId = this.currentFilters.hotel;
            if (this.currentFilters.status) params.status  = this._statusNameToNum(this.currentFilters.status);

            const reservations = await API.getReservations(params);
            const events = this._toCalendarEvents(Array.isArray(reservations) ? reservations : []);

            this.calendar.removeAllEvents();
            this.calendar.addEventSource(events);
        } catch (err) {
            console.error('[Calendar] loadReservations error:', err);
            UI.showError('Failed to load reservations');
        } finally {
            this._showCalLoading(false);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 9.1 – FULLCALENDAR INIT
    // ─────────────────────────────────────────────────────────────
    _initCalendar() {
        const el = document.getElementById('calendar');
        if (!el) return;

        // Detect if resource-timeline plugin is available
        const hasTimeline = typeof FullCalendar !== 'undefined' &&
            typeof FullCalendar.ResourceTimelineView !== 'undefined';

        const initialView = hasTimeline
            ? (this._restoredView || 'resourceTimelineMonth')
            : 'dayGridMonth';

        const config = {
            // ── Views ──────────────────────────────────────────
            initialView,
            initialDate: this._restoredDate || new Date(),
            headerToolbar: {
                left:   'prev,next today',
                center: 'title',
                right:  hasTimeline
                    ? 'resourceTimelineDay,resourceTimelineWeek,resourceTimelineMonth,dayGridMonth,listWeek'
                    : 'dayGridMonth,timeGridWeek,listWeek'
            },
            views: {
                resourceTimelineDay:   { buttonText: 'Day',   slotDuration: '01:00:00' },
                resourceTimelineWeek:  { buttonText: 'Week',  slotDuration: '01:00:00' },
                resourceTimelineMonth: { buttonText: 'Month', slotDuration: '24:00:00', slotLabelFormat: { weekday: 'short', day: 'numeric' } },
                dayGridMonth:          { buttonText: 'Grid' },
                listWeek:              { buttonText: 'List' }
            },
            height: 'auto',
            stickyHeaderDates: true,
            nowIndicator: true,

            // ── Resources (room rows) ──────────────────────────
            resources: this._getRoomResources.bind(this),
            resourceGroupField: 'hotelName',
            resourceAreaWidth: 'var(--cal-resource-w, 220px)',
            resourceAreaHeaderContent: 'Rooms',
            resourceLabelContent: this._renderResourceLabel.bind(this),

            // ── Events ────────────────────────────────────────
            eventContent: this._renderEventContent.bind(this),
            eventClassNames: this._getEventClassNames.bind(this),
            eventClick: this._onEventClick.bind(this),
            eventMouseEnter: this._onEventMouseEnter.bind(this),
            eventMouseLeave: this._onEventMouseLeave.bind(this),

            // ── 9.2 Drag-and-drop ─────────────────────────────
            editable: true,
            eventDurationEditable: true,
            eventResourceEditable: true,
            droppable: true,
            eventDrop: this._onEventDrop.bind(this),
            eventResize: this._onEventResize.bind(this),
            eventDragStart: this._onEventDragStart.bind(this),
            eventDragStop: this._onEventDragStop.bind(this),

            // ── Selection ─────────────────────────────────────
            selectable: true,
            selectMirror: true,
            select: this._onDateSelect.bind(this),

            // ── Navigation ────────────────────────────────────
            datesSet: this._onDatesSet.bind(this),

            // ── Responsive ────────────────────────────────────
            windowResize: this._onWindowResize.bind(this),

            // ── Misc ──────────────────────────────────────────
            businessHours: { startTime: '06:00', endTime: '22:00' },
            scrollTime: '08:00:00',
            eventMaxStack: 3,
            dayMaxEvents: true
        };

        this.calendar = new FullCalendar.Calendar(el, config);
        this.calendar.render();
    }

    // ─────────────────────────────────────────────────────────────
    // RESOURCES
    // ─────────────────────────────────────────────────────────────
    _getRoomResources(fetchInfo, successCb) {
        const filtered = this._getFilteredRooms();
        const resources = filtered.map(room => {
            const hotel = this.hotels.find(h => h.id === room.hotelId);
            return {
                id: room.id.toString(),
                title: room.roomNumber,
                hotelName: hotel ? hotel.name : 'Unknown Hotel',
                extendedProps: { room, hotelName: hotel ? hotel.name : '' }
            };
        });
        successCb(resources);
    }

    _getFilteredRooms() {
        let rooms = [...this.rooms];
        if (this.currentFilters.hotel) {
            rooms = rooms.filter(r => r.hotelId.toString() === this.currentFilters.hotel);
        }
        if (this.currentFilters.roomType) {
            const typeNum = this._roomTypeNameToNum(this.currentFilters.roomType);
            if (typeNum !== undefined) rooms = rooms.filter(r => r.type === typeNum);
        }
        // Sort: by hotel, then room type, then room number
        rooms.sort((a, b) => {
            if (a.hotelId !== b.hotelId) return a.hotelId - b.hotelId;
            if (a.type !== b.type) return a.type - b.type;
            return a.roomNumber.localeCompare(b.roomNumber, undefined, { numeric: true });
        });
        return rooms;
    }

    _renderResourceLabel(info) {
        const room = info.resource.extendedProps.room;
        if (!room) return { html: `<span>${info.resource.title}</span>` };

        const statusLabel = this._roomStatusLabel(room.status);
        const statusClass = this._roomStatusClass(room.status);
        const typeLabel   = this._roomTypeLabel(room.type);

        return {
            html: `
                <div class="resource-label">
                    <div class="resource-room-number">${room.roomNumber}</div>
                    <div class="resource-room-type">${typeLabel}</div>
                    <div class="resource-room-meta">${room.capacity} guests · $${room.baseRate}/night</div>
                    <div class="resource-room-status">
                        <span class="badge ${statusClass}">${statusLabel}</span>
                    </div>
                </div>`
        };
    }

    // ─────────────────────────────────────────────────────────────
    // EVENT CONVERSION
    // ─────────────────────────────────────────────────────────────
    _toCalendarEvents(reservations) {
        return reservations
            .filter(r => {
                if (!this.currentFilters.status) return true;
                const statusNum = this._statusNameToNum(this.currentFilters.status);
                return r.status === statusNum;
            })
            .map(r => {
                const statusStr = this._statusNumToName(r.status);
                const nights = this._calcNights(r.checkInDate, r.checkOutDate);
                return {
                    id: r.id.toString(),
                    resourceId: r.roomId ? r.roomId.toString() : undefined,
                    title: r.guestName || 'Guest',
                    start: r.checkInDate,
                    end:   r.checkOutDate,
                    extendedProps: { reservation: r, statusStr, nights }
                };
            });
    }

    // ─────────────────────────────────────────────────────────────
    // 9.1 – EVENT RENDERING
    // ─────────────────────────────────────────────────────────────
    _renderEventContent(info) {
        const { reservation, statusStr, nights } = info.event.extendedProps;
        const guestName = reservation.guestName || 'Guest';
        const isBookingCom = reservation.source === 2 || reservation.source === 'BookingCom';
        const sourceIcon = isBookingCom ? '<i class="bi bi-globe cal-event-source-icon" title="Booking.com"></i>' : '';
        const nightsLabel = nights > 0 ? `${nights}n` : '';

        return {
            html: `
                <div class="cal-event-inner">
                    <span class="cal-event-status-dot"></span>
                    <span class="cal-event-guest">${this._escHtml(guestName)}</span>
                    <span class="cal-event-nights">${nightsLabel}</span>
                    ${sourceIcon}
                </div>`
        };
    }

    _getEventClassNames(info) {
        const { statusStr } = info.event.extendedProps;
        return ['fc-event', `status-${(statusStr || 'unknown').toLowerCase()}`];
    }

    // ─────────────────────────────────────────────────────────────
    // EVENT HANDLERS – click / hover
    // ─────────────────────────────────────────────────────────────
    _onEventClick(info) {
        const { reservation } = info.event.extendedProps;
        // Task 10.4: Use bottom sheet on mobile, modal on desktop
        if (window.mobileManager?.isMobile()) {
            this._showReservationBottomSheet(reservation);
        } else {
            this._showReservationModal(reservation);
        }
    }

    // Task 10.4 – Bottom sheet for event details on mobile
    _showReservationBottomSheet(reservation) {
        if (!window.mobileManager) { this._showReservationModal(reservation); return; }

        const statusStr     = this._statusNumToName(reservation.status);
        const statusDisplay = this._statusDisplayName(statusStr);
        const statusClass   = statusStr.toLowerCase();
        const nights        = this._calcNights(reservation.checkInDate, reservation.checkOutDate);
        const fmt           = d => d ? new Date(d).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' }) : '—';
        const isBookingCom  = reservation.source === 2 || reservation.source === 'BookingCom';

        const bodyHtml = `
            <div class="mb-3">
                <div class="d-flex align-items-center justify-content-between mb-2">
                    <span class="badge status-${statusClass} fs-6">${statusDisplay}</span>
                    ${isBookingCom ? '<span class="badge bg-secondary"><i class="bi bi-globe me-1"></i>Booking.com</span>' : ''}
                </div>
                <div class="list-group list-group-flush">
                    <div class="list-group-item px-0 py-2 d-flex gap-2">
                        <i class="bi bi-person-circle text-primary mt-1" aria-hidden="true"></i>
                        <div>
                            <div class="fw-semibold">${this._escHtml(reservation.guestName || 'Unknown')}</div>
                            <small class="text-muted">${this._escHtml(reservation.guestEmail || '')}${reservation.guestPhone ? ' · ' + this._escHtml(reservation.guestPhone) : ''}</small>
                        </div>
                    </div>
                    <div class="list-group-item px-0 py-2 d-flex gap-2">
                        <i class="bi bi-building text-primary mt-1" aria-hidden="true"></i>
                        <div>
                            <div class="fw-semibold">${this._escHtml(reservation.hotelName || '')}</div>
                            <small class="text-muted">Room ${this._escHtml(reservation.roomNumber || '')} · ${this._roomTypeLabel(reservation.roomType)}</small>
                        </div>
                    </div>
                    <div class="list-group-item px-0 py-2 d-flex gap-2">
                        <i class="bi bi-calendar3 text-primary mt-1" aria-hidden="true"></i>
                        <div>
                            <div>${fmt(reservation.checkInDate)} → ${fmt(reservation.checkOutDate)}</div>
                            <small class="text-muted">${nights} night${nights !== 1 ? 's' : ''} · ${reservation.numberOfGuests || 1} guest${(reservation.numberOfGuests || 1) !== 1 ? 's' : ''}</small>
                        </div>
                    </div>
                    <div class="list-group-item px-0 py-2 d-flex gap-2">
                        <i class="bi bi-currency-dollar text-primary mt-1" aria-hidden="true"></i>
                        <div class="fw-semibold">${(reservation.totalAmount || 0).toFixed(2)}</div>
                    </div>
                    ${reservation.specialRequests ? `
                    <div class="list-group-item px-0 py-2 d-flex gap-2">
                        <i class="bi bi-chat-text text-primary mt-1" aria-hidden="true"></i>
                        <div><small>${this._escHtml(reservation.specialRequests)}</small></div>
                    </div>` : ''}
                </div>
            </div>
            ${(statusClass !== 'cancelled' && statusClass !== 'checkedout') ? `
            <div class="d-grid gap-2">
                <button class="btn btn-danger btn-sm" id="bsCancelBtn">
                    <i class="bi bi-x-circle me-1"></i>Cancel Reservation
                </button>
            </div>` : ''}
        `;

        window.mobileManager.openBottomSheet(reservation.guestName || 'Reservation', bodyHtml);

        // Wire cancel button inside bottom sheet
        setTimeout(() => {
            const cancelBtn = document.getElementById('bsCancelBtn');
            if (cancelBtn) {
                cancelBtn.addEventListener('click', async () => {
                    window.mobileManager.closeBottomSheet();
                    this.currentReservation = reservation;
                    await this._cancelCurrentReservation();
                });
            }
        }, 50);
    }

    _onEventMouseEnter(info) {
        const { reservation, statusStr } = info.event.extendedProps;
        this._showTooltip(info.el, reservation, statusStr);
    }

    _onEventMouseLeave() {
        this._hideTooltip();
    }

    _onDatesSet() {
        this._saveState();
        this._loadReservations();
    }

    _onWindowResize() {
        if (!this.calendar) return;
        const w = window.innerWidth;
        const hasTimeline = typeof FullCalendar !== 'undefined' &&
            typeof FullCalendar.ResourceTimelineView !== 'undefined';
        const currentView = this.calendar.view.type;

        if (w < 576) {
            // Extra small: list view is most readable on any device
            if (currentView !== 'listWeek') {
                this.calendar.changeView('listWeek');
            }
        } else if (w < 768) {
            if (hasTimeline && currentView !== 'resourceTimelineDay') {
                this.calendar.changeView('resourceTimelineDay');
            } else if (!hasTimeline && currentView !== 'listWeek') {
                this.calendar.changeView('listWeek');
            }
        }
        // >= 768: preserve user's chosen view
    }

    _onDateSelect(info) {
        const resourceId = info.resource ? info.resource.id : null;
        this._showAddReservationModal(resourceId, info.start, info.end);
        this.calendar.unselect();
    }

    // ─────────────────────────────────────────────────────────────
    // 9.2 – DRAG-AND-DROP
    // ─────────────────────────────────────────────────────────────
    _onEventDragStart(info) {
        // Add dragging class for visual feedback
        info.el.classList.add('fc-event-dragging');
    }

    _onEventDragStop(info) {
        info.el.classList.remove('fc-event-dragging');
    }

    async _onEventDrop(info) {
        const { reservation } = info.event.extendedProps;
        const newStart  = info.event.start;
        const newEnd    = info.event.end || this._addDays(newStart, 1);
        const newRoomId = info.newResource ? parseInt(info.newResource.id) : reservation.roomId;

        // Check availability first (conflict detection)
        const hasConflict = await this._checkConflict(newRoomId, newStart, newEnd, reservation.id);
        if (hasConflict) {
            info.revert();
            this._showConflictFeedback(info.el);
            this._showToast('Room not available for those dates', 'error');
            return;
        }

        // Store pending drop and show confirmation dialog
        this._pendingDrop = { info, newStart, newEnd, newRoomId, reservation };
        this._showDragConfirmModal(reservation, newStart, newEnd, newRoomId);
    }

    async _onEventResize(info) {
        const { reservation } = info.event.extendedProps;
        const newStart = info.event.start;
        const newEnd   = info.event.end;

        const hasConflict = await this._checkConflict(reservation.roomId, newStart, newEnd, reservation.id);
        if (hasConflict) {
            info.revert();
            this._showConflictFeedback(info.el);
            this._showToast('Room not available for those dates', 'error');
            return;
        }

        this._pendingDrop = { info, newStart, newEnd, newRoomId: reservation.roomId, reservation, isResize: true };
        this._showDragConfirmModal(reservation, newStart, newEnd, reservation.roomId);
    }

    async _checkConflict(roomId, start, end, excludeId) {
        try {
            const result = await API.request('/reservations/check-availability', {
                method: 'POST',
                body: JSON.stringify({
                    roomId,
                    checkInDate:  start.toISOString(),
                    checkOutDate: end.toISOString(),
                    excludeReservationId: excludeId
                })
            });
            return !result.isAvailable;
        } catch (err) {
            console.error('[Calendar] conflict check error:', err);
            return false; // allow on error to avoid blocking
        }
    }

    _showConflictFeedback(el) {
        el.classList.add('drop-invalid');
        setTimeout(() => el.classList.remove('drop-invalid'), 1200);
    }

    // ─────────────────────────────────────────────────────────────
    // DRAG CONFIRMATION MODAL
    // ─────────────────────────────────────────────────────────────
    _showDragConfirmModal(reservation, newStart, newEnd, newRoomId) {
        const oldRoom = this.rooms.find(r => r.id === reservation.roomId);
        const newRoom = this.rooms.find(r => r.id === newRoomId);
        const roomChanged = reservation.roomId !== newRoomId;

        const fmt = d => new Date(d).toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' });

        let roomRow = '';
        if (roomChanged) {
            roomRow = `
                <div class="change-row">
                    <i class="bi bi-door-open"></i>
                    <span>Room: <strong>${oldRoom ? oldRoom.roomNumber : reservation.roomNumber}</strong></span>
                    <span class="change-arrow">→</span>
                    <span><strong>${newRoom ? newRoom.roomNumber : newRoomId}</strong></span>
                </div>`;
        }

        document.getElementById('calDragSummary').innerHTML = `
            <div class="change-summary">
                <div class="change-row">
                    <i class="bi bi-person"></i>
                    <span><strong>${this._escHtml(reservation.guestName || 'Guest')}</strong></span>
                </div>
                <div class="change-row">
                    <i class="bi bi-calendar-check"></i>
                    <span>Check-in: <strong>${fmt(reservation.checkInDate)}</strong></span>
                    <span class="change-arrow">→</span>
                    <span><strong>${fmt(newStart)}</strong></span>
                </div>
                <div class="change-row">
                    <i class="bi bi-calendar-x"></i>
                    <span>Check-out: <strong>${fmt(reservation.checkOutDate)}</strong></span>
                    <span class="change-arrow">→</span>
                    <span><strong>${fmt(newEnd)}</strong></span>
                </div>
                ${roomRow}
            </div>`;

        const modal = new bootstrap.Modal(document.getElementById('calDragConfirmModal'));
        modal.show();
    }

    async _confirmDrop() {
        if (!this._pendingDrop) return;
        const { info, newStart, newEnd, newRoomId, reservation } = this._pendingDrop;
        this._pendingDrop = null;

        bootstrap.Modal.getInstance(document.getElementById('calDragConfirmModal'))?.hide();

        try {
            UI.showLoading();
            await API.request(`/reservations/${reservation.id}/dates`, {
                method: 'PATCH',
                body: JSON.stringify({
                    checkInDate:  newStart.toISOString(),
                    checkOutDate: newEnd.toISOString(),
                    roomId: newRoomId !== reservation.roomId ? newRoomId : null
                })
            });
            this._showToast('Reservation updated successfully', 'success');
            await this._loadReservations();
        } catch (err) {
            console.error('[Calendar] drop confirm error:', err);
            info.revert();
            this._showToast('Failed to update reservation', 'error');
        } finally {
            UI.hideLoading();
        }
    }

    _cancelDrop() {
        if (this._pendingDrop) {
            this._pendingDrop.info.revert();
            this._pendingDrop = null;
        }
        bootstrap.Modal.getInstance(document.getElementById('calDragConfirmModal'))?.hide();
    }

    // ─────────────────────────────────────────────────────────────
    // 9.3 – FILTERS & VIEWS
    // ─────────────────────────────────────────────────────────────
    _bindFilterEvents() {
        const applyBtn  = document.getElementById('applyFilters');
        const clearBtn  = document.getElementById('clearFilters');
        const addBtn    = document.getElementById('addReservationBtn');
        const hotelSel  = document.getElementById('hotelFilter');

        if (applyBtn)  applyBtn.addEventListener('click',  () => this.applyFilters());
        if (clearBtn)  clearBtn.addEventListener('click',  () => this.clearFilters());
        if (addBtn)    addBtn.addEventListener('click',    () => this._showAddReservationModal());
        if (hotelSel)  hotelSel.addEventListener('change', () => this._onHotelFilterChange());

        // Restore filter UI values
        this._restoreFilterUI();
    }

    _restoreFilterUI() {
        const set = (id, val) => { const el = document.getElementById(id); if (el && val) el.value = val; };
        set('hotelFilter',    this.currentFilters.hotel);
        set('roomTypeFilter', this.currentFilters.roomType);
        set('statusFilter',   this.currentFilters.status);
    }

    _onHotelFilterChange() {
        this.currentFilters.hotel = document.getElementById('hotelFilter').value;
        if (this.calendar) this.calendar.refetchResources();
        this._updateSignalRHotelGroup();
    }

    applyFilters() {
        this.currentFilters.hotel    = document.getElementById('hotelFilter')?.value    || '';
        this.currentFilters.roomType = document.getElementById('roomTypeFilter')?.value || '';
        this.currentFilters.status   = document.getElementById('statusFilter')?.value   || '';
        this._saveState();
        if (this.calendar) this.calendar.refetchResources();
        this._loadReservations();
    }

    clearFilters() {
        this.currentFilters = { hotel: '', roomType: '', status: '', dateRange: null };
        ['hotelFilter', 'roomTypeFilter', 'statusFilter', 'dateRangePicker'].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.value = '';
        });
        this._saveState();
        if (this.calendar) this.calendar.refetchResources();
        this._loadReservations();
    }

    // 9.3 – Quick date navigation
    _bindQuickNav() {
        const bind = (id, fn) => {
            const el = document.getElementById(id);
            if (el) el.addEventListener('click', fn);
        };
        bind('navToday',     () => { this.calendar?.today();    this._saveState(); });
        bind('navNextWeek',  () => { this.calendar?.incrementDate({ weeks: 1 });  this._saveState(); });
        bind('navNextMonth', () => { this.calendar?.incrementDate({ months: 1 }); this._saveState(); });
        bind('navPrevWeek',  () => { this.calendar?.incrementDate({ weeks: -1 }); this._saveState(); });
    }

    // 9.3 – Date range picker
    _initDateRangePicker() {
        const $picker = $('#dateRangePicker');
        if (!$picker.length || typeof $.fn.daterangepicker === 'undefined') return;

        $picker.daterangepicker({
            opens: 'left',
            autoUpdateInput: false,
            locale: { cancelLabel: 'Clear', format: 'MM/DD/YYYY' },
            ranges: {
                'Today':       [moment(), moment()],
                'This Week':   [moment().startOf('week'), moment().endOf('week')],
                'Next 7 Days': [moment(), moment().add(6, 'days')],
                'This Month':  [moment().startOf('month'), moment().endOf('month')],
                'Next Month':  [moment().add(1, 'month').startOf('month'), moment().add(1, 'month').endOf('month')]
            }
        });

        $picker.on('apply.daterangepicker', (ev, picker) => {
            $picker.val(picker.startDate.format('MM/DD/YYYY') + ' - ' + picker.endDate.format('MM/DD/YYYY'));
            this.currentFilters.dateRange = {
                start: picker.startDate.format('YYYY-MM-DD'),
                end:   picker.endDate.format('YYYY-MM-DD')
            };
            if (this.calendar) {
                this.calendar.gotoDate(picker.startDate.toDate());
            }
            this._loadReservations();
        });

        $picker.on('cancel.daterangepicker', () => {
            $picker.val('');
            this.currentFilters.dateRange = null;
            this._loadReservations();
        });
    }

    _populateHotelFilter() {
        const sel = document.getElementById('hotelFilter');
        if (!sel) return;
        while (sel.children.length > 1) sel.removeChild(sel.lastChild);
        this.hotels.forEach(h => {
            const opt = document.createElement('option');
            opt.value = h.id.toString();
            opt.textContent = h.name;
            if (this.currentFilters.hotel === h.id.toString()) opt.selected = true;
            sel.appendChild(opt);
        });

        // Also populate reservation modal hotel select
        const resSel = document.getElementById('reservationHotel');
        if (resSel) {
            while (resSel.children.length > 1) resSel.removeChild(resSel.lastChild);
            this.hotels.forEach(h => {
                const opt = document.createElement('option');
                opt.value = h.id.toString();
                opt.textContent = h.name;
                resSel.appendChild(opt);
            });
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 9.4 – SIGNALR REAL-TIME UPDATES
    // ─────────────────────────────────────────────────────────────
    async _initSignalR() {
        try {
            this.signalRManager = new SignalRManager();
            await this.signalRManager.initialize();

            // Listen for reservation events – update calendar in real-time
            this.signalRManager.on('reservationCreated', (data) => {
                console.log('[Calendar] SignalR: reservationCreated', data);
                this._debounceReload();
                this._showToast('New reservation created', 'info');
            });

            this.signalRManager.on('reservationUpdated', (data) => {
                console.log('[Calendar] SignalR: reservationUpdated', data);
                this._debounceReload();
                this._showToast('Reservation updated', 'info');
            });

            this.signalRManager.on('reservationCancelled', (data) => {
                console.log('[Calendar] SignalR: reservationCancelled', data);
                this._debounceReload();
                this._showToast('Reservation cancelled', 'warning');
            });

            this.signalRManager.on('calendarRefresh', () => {
                console.log('[Calendar] SignalR: calendarRefresh');
                this._debounceReload();
            });

            console.log('[Calendar] SignalR initialized');
        } catch (err) {
            console.warn('[Calendar] SignalR init failed (non-fatal):', err);
        }
    }

    _debounceReload() {
        if (this.updateTimeout) clearTimeout(this.updateTimeout);
        this.updateTimeout = setTimeout(() => this._loadReservations(), 600);
    }

    async _updateSignalRHotelGroup() {
        if (!this.signalRManager?.isConnectionActive()) return;
        try {
            if (this.currentFilters.hotel) {
                await this.signalRManager.joinHotelGroup(this.currentFilters.hotel);
            }
        } catch (err) {
            console.warn('[Calendar] SignalR hotel group update failed:', err);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // TOOLTIP
    // ─────────────────────────────────────────────────────────────
    _showTooltip(el, reservation, statusStr) {
        this._hideTooltip();

        const statusDisplay = this._statusDisplayName(statusStr || reservation.status);
        const statusClass   = (statusStr || '').toLowerCase();
        const nights        = this._calcNights(reservation.checkInDate, reservation.checkOutDate);
        const isBookingCom  = reservation.source === 2 || reservation.source === 'BookingCom';
        const fmt           = d => d ? new Date(d).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' }) : '—';

        const tip = document.createElement('div');
        tip.className = 'cal-tooltip';
        tip.innerHTML = `
            <div class="cal-tooltip-header">
                <span class="cal-tooltip-guest">${this._escHtml(reservation.guestName || 'Guest')}</span>
                <span class="badge status-${statusClass}">${statusDisplay}</span>
            </div>
            <div class="cal-tooltip-body">
                <div class="cal-tooltip-row"><i class="bi bi-building"></i><span>${this._escHtml(reservation.hotelName || '')}</span></div>
                <div class="cal-tooltip-row"><i class="bi bi-door-open"></i><span>Room ${this._escHtml(reservation.roomNumber || '')} · ${this._roomTypeLabel(reservation.roomType)}</span></div>
                <div class="cal-tooltip-row"><i class="bi bi-calendar-check"></i><span>${fmt(reservation.checkInDate)} → ${fmt(reservation.checkOutDate)}</span></div>
                <div class="cal-tooltip-row"><i class="bi bi-moon"></i><span>${nights} night${nights !== 1 ? 's' : ''}</span></div>
                <div class="cal-tooltip-row"><i class="bi bi-people"></i><span>${reservation.numberOfGuests || 1} guest${(reservation.numberOfGuests || 1) !== 1 ? 's' : ''}</span></div>
                <div class="cal-tooltip-row"><i class="bi bi-currency-dollar"></i><span>$${(reservation.totalAmount || 0).toFixed(2)}</span></div>
                ${isBookingCom ? '<div class="cal-tooltip-row"><i class="bi bi-globe"></i><span>Booking.com</span></div>' : ''}
                ${reservation.specialRequests ? `<div class="cal-tooltip-row"><i class="bi bi-chat-text"></i><span>${this._escHtml(reservation.specialRequests.substring(0, 60))}${reservation.specialRequests.length > 60 ? '…' : ''}</span></div>` : ''}
            </div>`;

        document.body.appendChild(tip);

        // Position near element
        const rect = el.getBoundingClientRect();
        const tipW = 280;
        let left = rect.left + rect.width / 2 - tipW / 2;
        let top  = rect.top - 10;

        // Keep within viewport
        left = Math.max(8, Math.min(left, window.innerWidth - tipW - 8));
        if (top < 80) top = rect.bottom + 10; // flip below if too close to top

        tip.style.cssText = `left:${left}px;top:${top}px;width:${tipW}px;transform:translateY(-100%)`;
        if (top === rect.bottom + 10) tip.style.transform = 'none';

        this.currentTooltip = tip;
    }

    _hideTooltip() {
        if (this.currentTooltip) {
            this.currentTooltip.remove();
            this.currentTooltip = null;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // RESERVATION DETAIL MODAL
    // ─────────────────────────────────────────────────────────────
    _showReservationModal(reservation) {
        this.currentReservation = reservation;
        const statusStr     = this._statusNumToName(reservation.status);
        const statusDisplay = this._statusDisplayName(statusStr);
        const statusClass   = statusStr.toLowerCase();
        const nights        = this._calcNights(reservation.checkInDate, reservation.checkOutDate);
        const fmt           = d => d ? new Date(d).toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' }) : '—';
        const isBookingCom  = reservation.source === 2 || reservation.source === 'BookingCom';

        document.getElementById('reservationDetails').innerHTML = `
            <div class="row g-3">
                <div class="col-md-6">
                    <h6 class="text-primary border-bottom pb-1"><i class="bi bi-person-circle me-1"></i>Guest</h6>
                    <p class="mb-1"><strong>${this._escHtml(reservation.guestName || 'Unknown')}</strong></p>
                    <p class="mb-1 text-muted small">${this._escHtml(reservation.guestEmail || 'No email')}</p>
                    <p class="mb-0 text-muted small">${this._escHtml(reservation.guestPhone || 'No phone')}</p>
                </div>
                <div class="col-md-6">
                    <h6 class="text-primary border-bottom pb-1"><i class="bi bi-building me-1"></i>Accommodation</h6>
                    <p class="mb-1">${this._escHtml(reservation.hotelName || '')}</p>
                    <p class="mb-1">Room <strong>${this._escHtml(reservation.roomNumber || '')}</strong> · ${this._roomTypeLabel(reservation.roomType)}</p>
                    <p class="mb-0">${reservation.numberOfGuests || 1} guest${(reservation.numberOfGuests || 1) !== 1 ? 's' : ''}</p>
                </div>
                <div class="col-md-6">
                    <h6 class="text-primary border-bottom pb-1"><i class="bi bi-calendar3 me-1"></i>Stay</h6>
                    <p class="mb-1"><strong>Check-in:</strong> ${fmt(reservation.checkInDate)}</p>
                    <p class="mb-1"><strong>Check-out:</strong> ${fmt(reservation.checkOutDate)}</p>
                    <p class="mb-0"><strong>Duration:</strong> ${nights} night${nights !== 1 ? 's' : ''}</p>
                </div>
                <div class="col-md-6">
                    <h6 class="text-primary border-bottom pb-1"><i class="bi bi-info-circle me-1"></i>Details</h6>
                    <p class="mb-1"><strong>Status:</strong> <span class="badge status-${statusClass}">${statusDisplay}</span></p>
                    <p class="mb-1"><strong>Source:</strong> ${isBookingCom ? '<i class="bi bi-globe me-1"></i>Booking.com' : 'Manual'}</p>
                    <p class="mb-1"><strong>Total:</strong> $${(reservation.totalAmount || 0).toFixed(2)}</p>
                    ${reservation.bookingReference ? `<p class="mb-0"><strong>Ref:</strong> <code>${this._escHtml(reservation.bookingReference)}</code></p>` : ''}
                </div>
                ${reservation.specialRequests ? `
                <div class="col-12">
                    <h6 class="text-primary border-bottom pb-1"><i class="bi bi-chat-text me-1"></i>Special Requests</h6>
                    <p class="mb-0 text-muted">${this._escHtml(reservation.specialRequests)}</p>
                </div>` : ''}
                ${reservation.internalNotes ? `
                <div class="col-12">
                    <h6 class="text-primary border-bottom pb-1"><i class="bi bi-sticky me-1"></i>Internal Notes</h6>
                    <p class="mb-0 text-muted">${this._escHtml(reservation.internalNotes)}</p>
                </div>` : ''}
            </div>`;

        // Show/hide action buttons
        const editBtn   = document.getElementById('editReservationBtn');
        const cancelBtn = document.getElementById('cancelReservationBtn');
        if (editBtn)   editBtn.style.display   = statusClass === 'cancelled' || statusClass === 'checkedout' ? 'none' : 'inline-block';
        if (cancelBtn) cancelBtn.style.display = (statusClass === 'cancelled' || statusClass === 'checkedout') ? 'none' : 'inline-block';

        new bootstrap.Modal(document.getElementById('reservationModal')).show();
    }

    // ─────────────────────────────────────────────────────────────
    // ADD RESERVATION MODAL
    // ─────────────────────────────────────────────────────────────
    _showAddReservationModal(preRoomId = null, startDate = null, endDate = null) {
        document.getElementById('reservationForm')?.reset();
        this._populateHotelFilter();

        const today    = new Date();
        const tomorrow = new Date(today); tomorrow.setDate(tomorrow.getDate() + 1);
        const fmtDate  = d => d instanceof Date ? d.toISOString().split('T')[0] : new Date(d).toISOString().split('T')[0];

        document.getElementById('checkInDate').value  = fmtDate(startDate || today);
        document.getElementById('checkOutDate').value = fmtDate(endDate   || tomorrow);

        if (preRoomId) {
            const room = this.rooms.find(r => r.id.toString() === preRoomId);
            if (room) {
                document.getElementById('reservationHotel').value = room.hotelId.toString();
                this._updateRoomOptions();
                setTimeout(() => { document.getElementById('reservationRoom').value = preRoomId; }, 120);
            }
        }

        new bootstrap.Modal(document.getElementById('addReservationModal')).show();
    }

    _updateRoomOptions() {
        const hotelId  = document.getElementById('reservationHotel')?.value;
        const roomSel  = document.getElementById('reservationRoom');
        if (!roomSel) return;
        while (roomSel.children.length > 1) roomSel.removeChild(roomSel.lastChild);
        if (!hotelId) return;
        this.rooms
            .filter(r => r.hotelId.toString() === hotelId)
            .forEach(r => {
                const opt = document.createElement('option');
                opt.value = r.id.toString();
                opt.textContent = `${r.roomNumber} (${this._roomTypeLabel(r.type)}) - $${r.baseRate}/night`;
                roomSel.appendChild(opt);
            });
    }

    async _saveReservation() {
        const form = document.getElementById('reservationForm');
        if (!form?.checkValidity()) { form?.reportValidity(); return; }

        const payload = {
            hotelId:        parseInt(document.getElementById('reservationHotel').value),
            roomId:         parseInt(document.getElementById('reservationRoom').value),
            checkInDate:    document.getElementById('checkInDate').value,
            checkOutDate:   document.getElementById('checkOutDate').value,
            numberOfGuests: parseInt(document.getElementById('numberOfGuests').value),
            totalAmount:    parseFloat(document.getElementById('totalAmount').value),
            specialRequests: document.getElementById('specialRequests').value,
            internalNotes:  document.getElementById('internalNotes').value,
            guestFirstName: document.getElementById('guestFirstName').value,
            guestLastName:  document.getElementById('guestLastName').value,
            guestEmail:     document.getElementById('guestEmail').value,
            guestPhone:     document.getElementById('guestPhone').value
        };

        try {
            UI.showLoading();
            await API.post('/reservations/manual', payload);
            bootstrap.Modal.getInstance(document.getElementById('addReservationModal'))?.hide();
            this._showToast('Reservation created successfully', 'success');
            await this._loadReservations();
        } catch (err) {
            console.error('[Calendar] saveReservation error:', err);
            this._showToast('Failed to create reservation', 'error');
        } finally {
            UI.hideLoading();
        }
    }

    async _cancelCurrentReservation() {
        if (!this.currentReservation) return;
        const reason = prompt('Cancellation reason:');
        if (!reason) return;
        try {
            UI.showLoading();
            await API.request(`/reservations/${this.currentReservation.id}`, {
                method: 'DELETE',
                body: JSON.stringify({ reason })
            });
            bootstrap.Modal.getInstance(document.getElementById('reservationModal'))?.hide();
            this._showToast('Reservation cancelled', 'warning');
            await this._loadReservations();
        } catch (err) {
            console.error('[Calendar] cancelReservation error:', err);
            this._showToast('Failed to cancel reservation', 'error');
        } finally {
            UI.hideLoading();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // MODAL BUTTON BINDINGS
    // ─────────────────────────────────────────────────────────────
    _bindModalButtons() {
        document.getElementById('saveReservationBtn')?.addEventListener('click',   () => this._saveReservation());
        document.getElementById('cancelReservationBtn')?.addEventListener('click', () => this._cancelCurrentReservation());
        document.getElementById('reservationHotel')?.addEventListener('change',    () => this._updateRoomOptions());
        document.getElementById('calDragConfirmBtn')?.addEventListener('click',    () => this._confirmDrop());
        document.getElementById('calDragCancelBtn')?.addEventListener('click',     () => this._cancelDrop());
    }

    // ─────────────────────────────────────────────────────────────
    // 9.4 – TOAST NOTIFICATIONS
    // ─────────────────────────────────────────────────────────────
    _showToast(message, type = 'info') {
        let container = document.getElementById('calToastContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'calToastContainer';
            container.className = 'cal-toast-container';
            document.body.appendChild(container);
        }

        const iconMap = { success: 'bi-check-circle-fill', info: 'bi-info-circle-fill', warning: 'bi-exclamation-triangle-fill', error: 'bi-x-circle-fill' };
        const icon = iconMap[type] || iconMap.info;

        const toast = document.createElement('div');
        toast.className = `cal-toast toast-${type}`;
        toast.innerHTML = `<i class="bi ${icon}"></i><span>${this._escHtml(message)}</span>`;
        container.appendChild(toast);

        setTimeout(() => {
            toast.classList.add('removing');
            setTimeout(() => toast.remove(), 220);
        }, 3500);
    }

    // ─────────────────────────────────────────────────────────────
    // LOADING OVERLAY
    // ─────────────────────────────────────────────────────────────
    _showCalLoading(show) {
        const wrapper = document.querySelector('.calendar-wrapper');
        if (!wrapper) return;
        let overlay = wrapper.querySelector('.cal-loading-overlay');
        if (show) {
            if (!overlay) {
                overlay = document.createElement('div');
                overlay.className = 'cal-loading-overlay';
                overlay.innerHTML = '<div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading…</span></div>';
                wrapper.appendChild(overlay);
            }
        } else {
            overlay?.remove();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // HELPERS – status / room type mappings
    // ─────────────────────────────────────────────────────────────
    _statusNumToName(num) {
        const map = { 1: 'Pending', 2: 'Confirmed', 3: 'Cancelled', 4: 'CheckedIn', 5: 'CheckedOut', 6: 'NoShow' };
        return map[num] || (typeof num === 'string' ? num : 'Unknown');
    }

    _statusNameToNum(name) {
        const map = { pending: 1, confirmed: 2, cancelled: 3, checkedin: 4, checkedout: 5, noshow: 6 };
        return map[(name || '').toLowerCase()];
    }

    _statusDisplayName(statusStr) {
        const map = { Pending: 'Pending', Confirmed: 'Confirmed', Cancelled: 'Cancelled', CheckedIn: 'Checked In', CheckedOut: 'Checked Out', NoShow: 'No Show' };
        return map[statusStr] || statusStr || 'Unknown';
    }

    _roomTypeLabel(type) {
        const map = { 1: 'Single', 2: 'Double', 3: 'Suite', 4: 'Family', 5: 'Deluxe', 6: 'Twin', 7: 'Triple', 8: 'Quad', 9: 'Standard' };
        if (typeof type === 'string') return type;
        return map[type] || 'Room';
    }

    _roomTypeNameToNum(name) {
        // Guard: if already a number, return it directly
        if (typeof name === 'number') return name;
        const map = { single: 1, double: 2, suite: 3, family: 4, deluxe: 5, twin: 6, triple: 7, quad: 8, standard: 9 };
        return map[String(name || '').toLowerCase()];
    }

    _roomStatusLabel(status) {
        const map = { 1: 'Available', 2: 'Maintenance', 3: 'Blocked', 4: 'Out of Order', 5: 'Occupied', 6: 'Cleaning' };
        if (typeof status === 'string') return status;
        return map[status] || 'Unknown';
    }

    _roomStatusClass(status) {
        const map = { 1: 'bg-success', 2: 'bg-warning text-dark', 3: 'bg-danger', 4: 'bg-secondary', 5: 'bg-info text-dark', 6: 'bg-warning text-dark' };
        return map[status] || 'bg-secondary';
    }

    _calcNights(checkIn, checkOut) {
        if (!checkIn || !checkOut) return 0;
        const ms = new Date(checkOut) - new Date(checkIn);
        return Math.max(0, Math.round(ms / 86400000));
    }

    _addDays(date, days) {
        const d = new Date(date);
        d.setDate(d.getDate() + days);
        return d;
    }

    _escHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

}

// Expose globally
window.CalendarManager = CalendarManager;
