/**
 * Dashboard Customization Module
 * Handles GridStack drag-and-drop, widget registry, layout persistence,
 * date-range filtering, hotel filtering, and auto-refresh.
 * Tasks 13.1 – 13.4
 */
'use strict';

let _I18N = {};
async function loadI18n() {
    try {
        const lang = window.__hotelLocale || 'en';
        const res = await fetch('/api/i18n/strings?lang=' + lang);
        const data = await res.json();
        _I18N = data.strings || {};
        window._I18N = _I18N;
    } catch(e) { _I18N = {}; window._I18N = _I18N; }
}

class DashboardManager {
    constructor() {
        this.api            = new ApiClient();
        this.grid           = null;
        this.charts         = {};
        this.currentHotelId = null;
        this.startDate      = null;
        this.endDate        = null;
        this.autoRefresh    = false;
        this.refreshTimer   = null;
        this.availableWidgets = [];   // descriptors from server
        this.currentLayout    = [];   // WidgetConfiguration[]
        this._init();
    }

    // ── Bootstrap ────────────────────────────────────────────────────────

    async _init() {
        this._bindToolbar();
        await this._loadAvailableWidgets();
        await this._loadAndRenderLayout();
        this._populateWidgetCatalog();
    }

    _bindToolbar() {
        document.getElementById('hotelFilter')
            ?.addEventListener('change', e => {
                this.currentHotelId = e.target.value || null;
                this._refreshAllWidgets();
            });

        document.getElementById('refreshDashboard')
            ?.addEventListener('click', () => this._refreshAllWidgets());

        document.getElementById('saveLayoutBtn')
            ?.addEventListener('click', () => this._saveLayout());

        document.getElementById('resetLayoutBtn')
            ?.addEventListener('click', () => this._resetLayout());

        document.getElementById('autoRefreshToggle')
            ?.addEventListener('change', e => {
                this.autoRefresh = e.target.checked;
                this.autoRefresh ? this._startAutoRefresh() : this._stopAutoRefresh();
            });

        document.getElementById('refreshInterval')
            ?.addEventListener('change', () => {
                if (this.autoRefresh) {
                    this._stopAutoRefresh();
                    this._startAutoRefresh();
                }
            });
    }

    // ── Widget registry ───────────────────────────────────────────────────

    async _loadAvailableWidgets() {
        try {
            this.availableWidgets = await this.api.get('/dashboard/widgets') || [];
        } catch (e) {
            console.warn('Could not load widget descriptors:', e);
            this.availableWidgets = [];
        }
    }

    // ── Layout load & render ──────────────────────────────────────────────

    async _loadAndRenderLayout() {
        let layout;
        try {
            layout = await this.api.get('/dashboard/layout');
        } catch (e) {
            console.warn('Could not load layout from server, using defaults:', e);
            layout = { widgets: [] };
        }

        this.currentLayout = layout.widgets || [];
        this._initGrid();
        this._renderWidgetShells();
        await this._refreshAllWidgets();
    }

    _initGrid() {
        const el = document.getElementById('dashboardGrid');
        if (!el) return;

        this.grid = GridStack.init({
            column      : 12,
            cellHeight  : 80,
            margin      : 8,
            animate     : true,
            resizable   : { handles: 'se,sw,ne,nw,e,w,s' },
            draggable   : { handle: '.widget-drag-handle' },
            disableOneColumnMode: false,
        }, el);

        // Persist layout changes on drag/resize stop
        this.grid.on('change', () => this._onGridChange());
    }

    _renderWidgetShells() {
        if (!this.grid) return;
        this.grid.removeAll();

        const visible = this.currentLayout.filter(w => w.isVisible !== false);
        visible.forEach(cfg => {
            const content = this._buildWidgetShell(cfg);
            this.grid.addWidget({
                id  : cfg.widgetId,
                x   : cfg.x ?? 0,
                y   : cfg.y ?? 0,
                w   : cfg.w ?? 3,
                h   : cfg.h ?? 2,
                content,
            });
        });
    }

    _buildWidgetShell(cfg) {
        const desc = this.availableWidgets.find(d => d.widgetId === cfg.widgetId) || {};
        const icon = desc.icon || 'bi-grid';
        const name = desc.name || cfg.widgetId;

        return `
<div class="card h-100 widget-card" data-widget-id="${cfg.widgetId}">
  <div class="card-header d-flex align-items-center gap-2 py-2 widget-drag-handle"
       style="cursor:grab;" title="Drag to reposition">
    <i class="bi ${icon} text-primary" aria-hidden="true"></i>
    <span class="fw-semibold small flex-grow-1">${name}</span>
    <button class="btn btn-sm btn-link p-0 text-muted widget-refresh-btn"
            data-widget-id="${cfg.widgetId}" title="Refresh widget"
            aria-label="Refresh ${name}">
      <i class="bi bi-arrow-clockwise" aria-hidden="true"></i>
    </button>
    <button class="btn btn-sm btn-link p-0 text-muted widget-remove-btn"
            data-widget-id="${cfg.widgetId}" title="Remove widget"
            aria-label="Remove ${name}">
      <i class="bi bi-x-lg" aria-hidden="true"></i>
    </button>
  </div>
  <div class="card-body p-2 overflow-auto widget-body" id="wb-${cfg.widgetId}">
    <div class="d-flex align-items-center justify-content-center h-100 text-muted">
      <div class="spinner-border spinner-border-sm me-2" role="status">
        <span class="visually-hidden">Loading…</span>
      </div>
      <small>Loading…</small>
    </div>
  </div>
</div>`;
    }

    // ── Widget data refresh ───────────────────────────────────────────────

    async _refreshAllWidgets() {
        const visible = this.currentLayout.filter(w => w.isVisible !== false);
        await Promise.all(visible.map(cfg => this._loadWidgetData(cfg.widgetId)));
    }

    async _loadWidgetData(widgetId) {
        const body = document.getElementById(`wb-${widgetId}`);
        if (!body) return;

        this._setWidgetLoading(body);

        try {
            const params = this._buildQueryParams();
            const result = await this.api.get(`/dashboard/widget-data/${widgetId}${params}`);

            if (!result || !result.success) {
                this._setWidgetError(body, result?.error || 'Failed to load');
                return;
            }

            this._renderWidgetContent(widgetId, result.data, body);
        } catch (e) {
            console.error(`Widget ${widgetId} error:`, e);
            this._setWidgetError(body, 'Could not load widget data');
        }
    }

    _buildQueryParams() {
        const parts = [];
        if (this.currentHotelId) parts.push(`hotelId=${this.currentHotelId}`);
        if (this.startDate)      parts.push(`startDate=${this.startDate.toISOString()}`);
        if (this.endDate)        parts.push(`endDate=${this.endDate.toISOString()}`);
        return parts.length ? '?' + parts.join('&') : '';
    }

    // ── Widget content renderers ──────────────────────────────────────────

    _renderWidgetContent(widgetId, data, body) {
        if (!data) { this._setWidgetError(body, 'No data'); return; }

        switch (widgetId) {
            case 'occupancy-rate':       this._renderOccupancyRate(data, body);       break;
            case 'revenue':              this._renderRevenue(data, body);              break;
            case 'upcoming-checkins':    this._renderCheckIns(data, body);             break;
            case 'upcoming-checkouts':   this._renderCheckOuts(data, body);            break;
            case 'recent-reservations':  this._renderRecentReservations(data, body);   break;
            case 'notifications':        this._renderNotifications(data, body);        break;
            case 'quick-actions':        this._renderQuickActions(data, body);         break;
            case 'revenue-chart':        this._renderRevenueChart(data, body);         break;
            case 'occupancy-breakdown':  this._renderOccupancyBreakdown(data, body);   break;
            default:
                body.innerHTML = `<pre class="small p-2">${JSON.stringify(data, null, 2)}</pre>`;
        }
    }

    _renderOccupancyRate(d, body) {
        const trend = d.todayRate >= d.weekRate
            ? '<i class="bi bi-arrow-up text-success"></i>'
            : '<i class="bi bi-arrow-down text-danger"></i>';
        body.innerHTML = `
<div class="d-flex justify-content-between align-items-center h-100 px-2">
  <div>
    <div class="display-6 fw-bold text-primary">${d.todayRate}%</div>
    <div class="text-muted small">Today's Occupancy</div>
    <div class="small mt-1">${d.occupiedRoomsToday}/${d.totalRooms} rooms ${trend}</div>
  </div>
  <i class="bi bi-graph-up fs-1 text-primary opacity-25" aria-hidden="true"></i>
</div>`;
    }

    _renderRevenue(d, body) {
        const sign  = d.monthlyVariance >= 0 ? '+' : '';
        const color = d.monthlyVariance >= 0 ? 'text-success' : 'text-danger';
        body.innerHTML = `
<div class="d-flex justify-content-between align-items-center h-100 px-2">
  <div>
    <div class="display-6 fw-bold text-success">${this._fmt(d.monthRevenue)}</div>
    <div class="text-muted small">Monthly Revenue</div>
    <div class="small mt-1 ${color}">${sign}${d.monthlyVariance?.toFixed(1)}% vs last month</div>
  </div>
  <i class="bi bi-currency-dollar fs-1 text-success opacity-25" aria-hidden="true"></i>
</div>`;
    }

    _renderCheckIns(d, body) {
        const list = d.todayCheckIns || [];
        if (!list.length) {
            body.innerHTML = '<p class="text-muted text-center py-3 small">No check-ins today</p>';
            return;
        }
        body.innerHTML = `
<div class="d-flex justify-content-between align-items-center mb-2">
  <span class="small fw-semibold">Today's Check-ins</span>
  <span class="badge bg-success">${d.totalCheckIns}</span>
</div>
<div class="list-group list-group-flush small">
  ${list.slice(0, 5).map(c => `
  <div class="list-group-item px-0 py-1">
    <div class="d-flex justify-content-between">
      <span class="fw-semibold">${c.guestName}</span>
      <span class="badge bg-${this._statusColor(c.status)} ms-1">${c.status}</span>
    </div>
    <div class="text-muted">Room ${c.roomNumber} · ${c.hotelName}</div>
  </div>`).join('')}
  ${list.length > 5 ? `<div class="text-muted text-center py-1">+${list.length - 5} more</div>` : ''}
</div>`;
    }

    _renderCheckOuts(d, body) {
        const list = d.todayCheckOuts || [];
        if (!list.length) {
            body.innerHTML = '<p class="text-muted text-center py-3 small">No check-outs today</p>';
            return;
        }
        body.innerHTML = `
<div class="d-flex justify-content-between align-items-center mb-2">
  <span class="small fw-semibold">Today's Check-outs</span>
  <span class="badge bg-warning text-dark">${d.totalCheckOuts}</span>
</div>
<div class="list-group list-group-flush small">
  ${list.slice(0, 5).map(c => `
  <div class="list-group-item px-0 py-1">
    <div class="d-flex justify-content-between">
      <span class="fw-semibold">${c.guestName}</span>
      <span class="badge bg-${this._statusColor(c.status)} ms-1">${c.status}</span>
    </div>
    <div class="text-muted">Room ${c.roomNumber} · ${c.hotelName}</div>
  </div>`).join('')}
  ${list.length > 5 ? `<div class="text-muted text-center py-1">+${list.length - 5} more</div>` : ''}
</div>`;
    }

    _renderRecentReservations(data, body) {
        const list = Array.isArray(data) ? data : [];
        if (!list.length) {
            body.innerHTML = '<p class="text-muted text-center py-3 small">No recent reservations</p>';
            return;
        }
        body.innerHTML = `
<div class="table-responsive">
  <table class="table table-sm table-hover mb-0 small">
    <thead class="table-light">
      <tr>
        <th>Ref</th><th>Guest</th><th>Hotel</th><th>Room</th>
        <th>Check-in</th><th>Check-out</th><th>Status</th><th></th>
      </tr>
    </thead>
    <tbody>
      ${list.map(r => `
      <tr>
        <td class="fw-semibold">${r.bookingReference || '—'}</td>
        <td>${r.guestName}</td>
        <td>${r.hotelName}</td>
        <td>${r.roomNumber}</td>
        <td>${this._fmtDate(r.checkInDate)}</td>
        <td>${this._fmtDate(r.checkOutDate)}</td>
        <td><span class="badge bg-${this._statusColor(r.status)}">${r.status}</span></td>
        <td>
          <a href="/reservations/details/${r.id}" class="btn btn-xs btn-outline-primary py-0 px-1"
             title="View" aria-label="View reservation ${r.bookingReference}">
            <i class="bi bi-eye" aria-hidden="true"></i>
          </a>
        </td>
      </tr>`).join('')}
    </tbody>
  </table>
</div>`;
    }

    _renderNotifications(d, body) {
        const list = d.notifications || [];
        body.innerHTML = `
<div class="d-flex gap-3 mb-2 text-center">
  <div class="flex-fill">
    <div class="fw-bold text-danger">${d.criticalCount || 0}</div>
    <div class="text-muted" style="font-size:.7rem;">Critical</div>
  </div>
  <div class="flex-fill">
    <div class="fw-bold text-warning">${d.warningCount || 0}</div>
    <div class="text-muted" style="font-size:.7rem;">Warning</div>
  </div>
  <div class="flex-fill">
    <div class="fw-bold text-info">${d.infoCount || 0}</div>
    <div class="text-muted" style="font-size:.7rem;">Info</div>
  </div>
</div>
${list.length === 0
    ? '<p class="text-muted text-center small py-2">No notifications</p>'
    : list.slice(0, 6).map(n => `
<div class="border-start border-3 border-${this._notifColor(n.type)} ps-2 mb-2">
  <div class="small fw-semibold">${n.title}</div>
  <div class="text-muted" style="font-size:.72rem;">${n.message}</div>
  <div class="text-muted" style="font-size:.68rem;">${this._fmtDateTime(n.createdAt)}</div>
</div>`).join('')}
${list.length > 6 ? `<a href="/notifications" class="btn btn-sm btn-link p-0 small">View all</a>` : ''}`;
    }

    _renderQuickActions(data, body) {
        const actions = Array.isArray(data) ? data : [];
        body.innerHTML = `
<div class="row g-2">
  ${actions.map(a => `
  <div class="col-6 col-sm-4">
    <a href="${a.url}" class="btn btn-outline-${a.color} btn-sm w-100 d-flex flex-column align-items-center py-2 gap-1">
      <i class="bi ${a.icon} fs-5" aria-hidden="true"></i>
      <span style="font-size:.72rem;">${a.label}</span>
    </a>
  </div>`).join('')}
</div>`;
    }

    _renderRevenueChart(d, body) {
        const daily = d.dailyBreakdown || [];
        // Destroy previous chart instance if any
        if (this.charts[body.id]) {
            this.charts[body.id].destroy();
            delete this.charts[body.id];
        }

        body.innerHTML = '<canvas id="revenueChartCanvas" style="max-height:220px;"></canvas>';
        const ctx = body.querySelector('#revenueChartCanvas');
        if (!ctx || !daily.length) {
            body.innerHTML = '<p class="text-muted text-center small py-3">No revenue data for period</p>';
            return;
        }

        this.charts[body.id] = new Chart(ctx, {
            type: 'line',
            data: {
                labels  : daily.map(d => this._fmtDate(d.date)),
                datasets: [{
                    label          : 'Daily Revenue',
                    data           : daily.map(d => d.revenue),
                    borderColor    : '#0d6efd',
                    backgroundColor: 'rgba(13,110,253,.1)',
                    borderWidth    : 2,
                    fill           : true,
                    tension        : 0.4,
                    pointRadius    : 3,
                    pointHoverRadius: 5
                }]
            },
            options: {
                responsive         : true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales : {
                    y: {
                        beginAtZero: true,
                        ticks: { callback: v => '$' + v.toLocaleString() }
                    },
                    x: { ticks: { maxTicksLimit: 10 } }
                }
            }
        });
    }

    _renderOccupancyBreakdown(d, body) {
        const bars = [
            { label: 'Today',      value: d.todayRate, rooms: d.occupiedRoomsToday,  color: 'primary' },
            { label: 'This Week',  value: d.weekRate,  rooms: d.occupiedRoomsWeek,   color: 'info'    },
            { label: 'This Month', value: d.monthRate, rooms: d.occupiedRoomsMonth,  color: 'success' },
        ];
        body.innerHTML = bars.map(b => `
<div class="mb-2">
  <div class="d-flex justify-content-between small mb-1">
    <span>${b.label}</span>
    <span class="fw-semibold">${b.value}% <span class="text-muted fw-normal">(${b.rooms}/${d.totalRooms})</span></span>
  </div>
  <div class="progress" style="height:8px;" role="progressbar"
       aria-valuenow="${b.value}" aria-valuemin="0" aria-valuemax="100"
       aria-label="${b.label} occupancy ${b.value}%">
    <div class="progress-bar bg-${b.color}" style="width:${b.value}%"></div>
  </div>
</div>`).join('');
    }

    // ── Widget state helpers ──────────────────────────────────────────────

    _setWidgetLoading(body) {
        body.innerHTML = `
<div class="d-flex align-items-center justify-content-center h-100 text-muted">
  <div class="spinner-border spinner-border-sm me-2" role="status">
    <span class="visually-hidden">Loading…</span>
  </div>
  <small>Loading…</small>
</div>`;
    }

    _setWidgetError(body, msg) {
        body.innerHTML = `
<div class="d-flex align-items-center justify-content-center h-100 text-danger">
  <i class="bi bi-exclamation-triangle me-2" aria-hidden="true"></i>
  <small>${msg}</small>
</div>`;
    }

    // ── Layout persistence ────────────────────────────────────────────────

    _onGridChange() {
        if (!this.grid) return;
        // Sync grid positions back into currentLayout
        const items = this.grid.getGridItems();
        items.forEach(el => {
            const node     = el.gridstackNode;
            const widgetId = node?.id;
            if (!widgetId) return;
            const cfg = this.currentLayout.find(w => w.widgetId === widgetId);
            if (cfg) {
                cfg.x = node.x;
                cfg.y = node.y;
                cfg.w = node.w;
                cfg.h = node.h;
            }
        });
    }

    async _saveLayout() {
        const btn = document.getElementById('saveLayoutBtn');
        if (btn) { btn.disabled = true; btn.innerHTML = '<i class="bi bi-hourglass-split"></i> Saving…'; }

        try {
            await this.api.post('/dashboard/layout', { widgets: this.currentLayout });
            this._toast(_I18N['Toast_LayoutSaved'] || 'Layout saved successfully', 'success');
        } catch (e) {
            console.error('Save layout error:', e);
            this._toast(_I18N['Toast_LayoutSaveFailed'] || 'Failed to save layout', 'danger');
        } finally {
            if (btn) { btn.disabled = false; btn.innerHTML = '<i class="bi bi-save"></i> <span class="d-none d-sm-inline">Save Layout</span>'; }
        }
    }

    async _resetLayout() {
        if (!confirm('Reset dashboard to default layout? Your current layout will be lost.')) return;

        try {
            const layout = await this.api.delete('/dashboard/layout');
            this.currentLayout = layout.widgets || [];
            this._renderWidgetShells();
            await this._refreshAllWidgets();
            this._toast('Layout reset to default', 'info');
        } catch (e) {
            console.error('Reset layout error:', e);
            this._toast('Failed to reset layout', 'danger');
        }
    }

    // ── Add / Remove widgets ──────────────────────────────────────────────

    _populateWidgetCatalog() {
        const catalog = document.getElementById('widgetCatalog');
        if (!catalog || !this.availableWidgets.length) return;

        catalog.innerHTML = this.availableWidgets.map(desc => {
            const active = this.currentLayout.some(
                w => w.widgetId === desc.widgetId && w.isVisible !== false);
            return `
<div class="col-12 col-sm-6 col-md-4">
  <div class="card h-100 ${active ? 'border-success opacity-75' : 'border-primary'}"
       style="cursor:${active ? 'default' : 'pointer'};"
       data-widget-id="${desc.widgetId}">
    <div class="card-body d-flex align-items-start gap-3">
      <i class="bi ${desc.icon} fs-3 text-primary mt-1" aria-hidden="true"></i>
      <div class="flex-grow-1">
        <div class="fw-semibold">${desc.name}</div>
        <div class="text-muted small">${desc.description}</div>
        <div class="text-muted" style="font-size:.7rem;">
          Default size: ${desc.defaultW}×${desc.defaultH} columns
        </div>
      </div>
      ${active
        ? '<span class="badge bg-success align-self-start">Active</span>'
        : `<button class="btn btn-sm btn-primary align-self-start add-widget-btn"
                   data-widget-id="${desc.widgetId}" aria-label="Add ${desc.name}">
             <i class="bi bi-plus" aria-hidden="true"></i> Add
           </button>`}
    </div>
  </div>
</div>`;
        }).join('');

        // Bind add buttons
        catalog.querySelectorAll('.add-widget-btn').forEach(btn => {
            btn.addEventListener('click', e => {
                e.stopPropagation();
                this._addWidget(btn.dataset.widgetId);
            });
        });
    }

    _addWidget(widgetId) {
        const desc = this.availableWidgets.find(d => d.widgetId === widgetId);
        if (!desc) return;

        // Check if already in layout
        const existing = this.currentLayout.find(w => w.widgetId === widgetId);
        if (existing) {
            existing.isVisible = true;
        } else {
            this.currentLayout.push({
                widgetId,
                type     : desc.type,
                x        : 0,
                y        : 9999,   // GridStack places it at the bottom
                w        : desc.defaultW,
                h        : desc.defaultH,
                isVisible: true,
                settings : {}
            });
        }

        // Add to grid
        const content = this._buildWidgetShell({ widgetId });
        this.grid.addWidget({
            id     : widgetId,
            w      : desc.defaultW,
            h      : desc.defaultH,
            content,
        });

        // Load data
        this._loadWidgetData(widgetId);

        // Refresh catalog state
        this._populateWidgetCatalog();

        // Close modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('addWidgetModal'));
        modal?.hide();

        this._toast(`"${desc.name}" added to dashboard`, 'success');
    }

    _removeWidget(widgetId) {
        // Mark invisible in layout
        const cfg = this.currentLayout.find(w => w.widgetId === widgetId);
        if (cfg) cfg.isVisible = false;

        // Remove from grid
        const el = this.grid?.getGridItems()
            .find(i => i.gridstackNode?.id === widgetId);
        if (el) this.grid.removeWidget(el);

        // Destroy chart if any
        const bodyId = `wb-${widgetId}`;
        if (this.charts[bodyId]) {
            this.charts[bodyId].destroy();
            delete this.charts[bodyId];
        }

        // Refresh catalog
        this._populateWidgetCatalog();

        const desc = this.availableWidgets.find(d => d.widgetId === widgetId);
        this._toast(`"${desc?.name || widgetId}" removed`, 'info');
    }

    // ── Event delegation for per-widget buttons ───────────────────────────

    _bindWidgetEvents() {
        document.getElementById('dashboardGrid')
            ?.addEventListener('click', e => {
                const refreshBtn = e.target.closest('.widget-refresh-btn');
                if (refreshBtn) {
                    this._loadWidgetData(refreshBtn.dataset.widgetId);
                    return;
                }
                const removeBtn = e.target.closest('.widget-remove-btn');
                if (removeBtn) {
                    this._removeWidget(removeBtn.dataset.widgetId);
                }
            });
    }

    // ── Date range ────────────────────────────────────────────────────────

    setDateRange(start, end) {
        this.startDate = start;
        this.endDate   = end;
        this._refreshAllWidgets();
    }

    // ── Auto-refresh ──────────────────────────────────────────────────────

    _startAutoRefresh() {
        this._stopAutoRefresh();
        const seconds = parseInt(
            document.getElementById('refreshInterval')?.value || '300', 10);
        this.refreshTimer = setInterval(
            () => this._refreshAllWidgets(), seconds * 1000);
    }

    _stopAutoRefresh() {
        if (this.refreshTimer) {
            clearInterval(this.refreshTimer);
            this.refreshTimer = null;
        }
    }

    // ── Toast notification ────────────────────────────────────────────────

    _toast(message, type = 'success') {
        const id  = 'toast-' + Date.now();
        const map = { success: 'text-bg-success', danger: 'text-bg-danger',
                      info: 'text-bg-info', warning: 'text-bg-warning' };
        const cls = map[type] || 'text-bg-secondary';
        document.body.insertAdjacentHTML('beforeend', `
<div id="${id}" class="toast align-items-center ${cls} border-0 position-fixed bottom-0 end-0 m-3"
     role="alert" aria-live="assertive" aria-atomic="true" style="z-index:9999;">
  <div class="d-flex">
    <div class="toast-body">${message}</div>
    <button type="button" class="btn-close btn-close-white me-2 m-auto"
            data-bs-dismiss="toast" aria-label="Close"></button>
  </div>
</div>`);
        const el    = document.getElementById(id);
        const toast = new bootstrap.Toast(el, { delay: 3000 });
        toast.show();
        el.addEventListener('hidden.bs.toast', () => el.remove());
    }

    // ── Utility formatters ────────────────────────────────────────────────

    _fmt(amount) {
        return new Intl.NumberFormat(_I18N['Fmt_CurrencyLocale'] || 'en-US', {
            style: 'currency', currency: 'USD',
            minimumFractionDigits: 0, maximumFractionDigits: 0
        }).format(amount || 0);
    }

    _fmtDate(d) {
        if (!d) return '—';
        return new Date(d).toLocaleDateString(_I18N['Fmt_DateLocale'] || 'en-US',
            { month: 'short', day: 'numeric', year: 'numeric' });
    }

    _fmtDateTime(d) {
        if (!d) return '—';
        return new Date(d).toLocaleString(_I18N['Fmt_DateLocale'] || 'en-US',
            { month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' });
    }

    _statusColor(status) {
        const map = {
            Pending: 'warning', Confirmed: 'success',
            CheckedIn: 'info', CheckedOut: 'secondary', Cancelled: 'danger'
        };
        return map[status] || 'secondary';
    }

    _notifColor(type) {
        const map = {
            Critical: 'danger', Overbooking: 'danger',
            Warning: 'warning', MaintenanceConflict: 'warning',
            Info: 'info', Success: 'success'
        };
        return map[type] || 'info';
    }

    destroy() {
        this._stopAutoRefresh();
        Object.values(this.charts).forEach(c => c?.destroy());
        this.charts = {};
    }
}

// ── Bootstrap ─────────────────────────────────────────────────────────────────

window.dashboardManager = null;

document.addEventListener('DOMContentLoaded', async () => {
    if (document.getElementById('dashboardContainer')) {
        await loadI18n();
        window.dashboardManager = new DashboardManager();
        // Bind widget-level events after grid is ready (slight delay for DOM)
        setTimeout(() => window.dashboardManager._bindWidgetEvents(), 500);
    }
});

window.addEventListener('beforeunload', () => window.dashboardManager?.destroy());

// Legacy global helpers kept for backward compatibility
function viewReservation(id)  { window.location.href = `/reservations/details/${id}`; }
function editReservation(id)  { window.location.href = `/reservations/edit/${id}`; }
