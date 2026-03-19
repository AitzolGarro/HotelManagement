/**
 * Advanced Search module for the Reservations page.
 * Handles: multi-select dropdowns, sort toggle, saved searches (localStorage),
 * search API calls, result count display, and export triggers.
 */
class AdvancedSearch {
    constructor() {
        this._sortDir = 'desc';
        this._currentCriteria = {};
        this._STORAGE_KEY = 'reservationSavedSearches';
    }

    init() {
        this._bindSortToggle();
        this._bindMultiSelectLabels('.status-check', 'statusDropdownLabel', 'Status');
        this._bindMultiSelectLabels('.source-check', 'sourceDropdownLabel', 'Source');
        this._bindSavedSearches();
        this._bindExportButtons();
        this._renderSavedSearches();

        // Keep dropdowns open when clicking checkboxes inside them
        document.querySelectorAll('#advancedFiltersPanel .dropdown-menu').forEach(menu => {
            menu.addEventListener('click', e => e.stopPropagation());
        });
    }

    // ── Sort toggle ──────────────────────────────────────────────────────────

    _bindSortToggle() {
        const btn = document.getElementById('sortDirBtn');
        if (!btn) return;
        btn.addEventListener('click', () => {
            this._sortDir = this._sortDir === 'desc' ? 'asc' : 'desc';
            const icon = document.getElementById('sortDirIcon');
            if (icon) {
                icon.className = this._sortDir === 'desc' ? 'bi bi-sort-down' : 'bi bi-sort-up';
            }
            btn.setAttribute('data-dir', this._sortDir);
        });
    }

    // ── Multi-select dropdown label updater ──────────────────────────────────

    _bindMultiSelectLabels(checkboxSelector, labelId, defaultLabel) {
        document.querySelectorAll(checkboxSelector).forEach(cb => {
            cb.addEventListener('change', () => this._updateDropdownLabel(checkboxSelector, labelId, defaultLabel));
        });
    }

    _updateDropdownLabel(checkboxSelector, labelId, defaultLabel) {
        const checked = [...document.querySelectorAll(`${checkboxSelector}:checked`)]
            .map(cb => cb.closest('.form-check').querySelector('label').textContent.trim());
        const label = document.getElementById(labelId);
        if (!label) return;
        label.textContent = checked.length === 0 ? `All ${defaultLabel}s`
            : checked.length === 1 ? checked[0]
            : `${checked.length} selected`;
    }

    // ── Build criteria from UI ────────────────────────────────────────────────

    buildCriteria() {
        const val = id => (document.getElementById(id)?.value || '').trim();

        const statuses = [...document.querySelectorAll('.status-check:checked')].map(cb => parseInt(cb.value));
        const sources  = [...document.querySelectorAll('.source-check:checked')].map(cb => parseInt(cb.value));

        const criteria = {
            dateFrom:        val('dateFromFilter')   || undefined,
            dateTo:          val('dateToFilter')     || undefined,
            hotelId:         val('hotelFilterRes')   ? parseInt(val('hotelFilterRes'))   : undefined,
            guestName:       val('guestNameFilter')  || undefined,
            bookingReference:val('bookingRefFilter') || undefined,
            roomType:        val('roomTypeFilter')   ? parseInt(val('roomTypeFilter'))   : undefined,
            minAmount:       val('minAmountFilter')  ? parseFloat(val('minAmountFilter')): undefined,
            maxAmount:       val('maxAmountFilter')  ? parseFloat(val('maxAmountFilter')): undefined,
            sortBy:          val('sortByFilter')     || 'created',
            sortDirection:   this._sortDir,
        };

        if (statuses.length) criteria.statuses = statuses;
        if (sources.length)  criteria.sources  = sources;

        // Remove undefined keys so URLSearchParams stays clean
        Object.keys(criteria).forEach(k => criteria[k] === undefined && delete criteria[k]);

        this._currentCriteria = criteria;
        return criteria;
    }

    // ── Search API call ───────────────────────────────────────────────────────

    async search(pageNumber = 1, pageSize = 20) {
        const criteria = this.buildCriteria();

        // Build flat query params (arrays need repeated keys)
        const params = new URLSearchParams();
        Object.entries(criteria).forEach(([k, v]) => {
            if (Array.isArray(v)) {
                v.forEach(item => params.append(k, item));
            } else {
                params.append(k, v);
            }
        });
        params.append('pageNumber', pageNumber);
        params.append('pageSize', pageSize);

        try {
            const response = await fetch(`/api/reservations/search?${params.toString()}`, {
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`,
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) throw new Error(`Search failed: ${response.status}`);

            const result = await response.json();
            this._updateResultCount(result.totalCount);
            return result;
        } catch (err) {
            console.error('Advanced search error:', err);
            throw err;
        }
    }

    _updateResultCount(total) {
        const badge = document.getElementById('resultCountBadge');
        if (!badge) return;
        badge.textContent = `${total.toLocaleString()} result${total !== 1 ? 's' : ''}`;
        badge.style.display = total !== undefined ? 'inline-block' : 'none';
    }

    // ── Clear filters ─────────────────────────────────────────────────────────

    clearAll() {
        ['dateFromFilter','dateToFilter','guestNameFilter','bookingRefFilter',
         'roomTypeFilter','minAmountFilter','maxAmountFilter'].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.value = '';
        });

        const hotelSel = document.getElementById('hotelFilterRes');
        if (hotelSel) hotelSel.value = '';

        document.querySelectorAll('.status-check, .source-check').forEach(cb => {
            cb.checked = false;
        });

        this._updateDropdownLabel('.status-check', 'statusDropdownLabel', 'Status');
        this._updateDropdownLabel('.source-check', 'sourceDropdownLabel', 'Source');

        const sortSel = document.getElementById('sortByFilter');
        if (sortSel) sortSel.value = 'created';
        this._sortDir = 'desc';
        const icon = document.getElementById('sortDirIcon');
        if (icon) icon.className = 'bi bi-sort-down';

        this._updateResultCount(undefined);
    }

    // ── Saved searches (localStorage) ────────────────────────────────────────

    _bindSavedSearches() {
        const saveBtn = document.getElementById('saveCurrentSearch');
        if (saveBtn) {
            saveBtn.addEventListener('click', () => this._saveCurrentSearch());
        }
    }

    _saveCurrentSearch() {
        const criteria = this.buildCriteria();
        const name = prompt('Name this search:');
        if (!name) return;

        const saved = this._loadSavedSearches();
        saved.push({ name, criteria, savedAt: new Date().toISOString() });
        localStorage.setItem(this._STORAGE_KEY, JSON.stringify(saved));
        this._renderSavedSearches();
        UI.showSuccess?.(`Search "${name}" saved.`);
    }

    _loadSavedSearches() {
        try {
            return JSON.parse(localStorage.getItem(this._STORAGE_KEY) || '[]');
        } catch {
            return [];
        }
    }

    _renderSavedSearches() {
        const container = document.getElementById('savedSearchesList');
        const noSaved   = document.getElementById('noSavedSearches');
        if (!container) return;

        const saved = this._loadSavedSearches();
        container.innerHTML = '';

        if (saved.length === 0) {
            if (noSaved) noSaved.style.display = 'inline';
            return;
        }
        if (noSaved) noSaved.style.display = 'none';

        saved.forEach((s, idx) => {
            const wrapper = document.createElement('span');
            wrapper.className = 'd-inline-flex align-items-center gap-1';

            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'btn btn-outline-primary btn-sm py-0 px-2';
            btn.textContent = s.name;
            btn.title = `Applied: ${new Date(s.savedAt).toLocaleDateString()}`;
            btn.addEventListener('click', () => this._applySavedSearch(s.criteria));

            const del = document.createElement('button');
            del.type = 'button';
            del.className = 'btn btn-link btn-sm p-0 text-danger';
            del.innerHTML = '<i class="bi bi-x"></i>';
            del.title = 'Delete saved search';
            del.addEventListener('click', () => this._deleteSavedSearch(idx));

            wrapper.appendChild(btn);
            wrapper.appendChild(del);
            container.appendChild(wrapper);
        });
    }

    _applySavedSearch(criteria) {
        // Restore basic fields
        const set = (id, val) => { const el = document.getElementById(id); if (el && val !== undefined) el.value = val; };
        set('dateFromFilter',   criteria.dateFrom);
        set('dateToFilter',     criteria.dateTo);
        set('hotelFilterRes',   criteria.hotelId);
        set('guestNameFilter',  criteria.guestName);
        set('bookingRefFilter', criteria.bookingReference);
        set('roomTypeFilter',   criteria.roomType);
        set('minAmountFilter',  criteria.minAmount);
        set('maxAmountFilter',  criteria.maxAmount);
        set('sortByFilter',     criteria.sortBy || 'created');

        // Restore sort direction
        this._sortDir = criteria.sortDirection || 'desc';
        const icon = document.getElementById('sortDirIcon');
        if (icon) icon.className = this._sortDir === 'desc' ? 'bi bi-sort-down' : 'bi bi-sort-up';

        // Restore checkboxes
        document.querySelectorAll('.status-check').forEach(cb => {
            cb.checked = (criteria.statuses || []).includes(parseInt(cb.value));
        });
        document.querySelectorAll('.source-check').forEach(cb => {
            cb.checked = (criteria.sources || []).includes(parseInt(cb.value));
        });

        this._updateDropdownLabel('.status-check', 'statusDropdownLabel', 'Status');
        this._updateDropdownLabel('.source-check', 'sourceDropdownLabel', 'Source');

        // Expand advanced panel if any advanced filter is set
        const hasAdvanced = criteria.statuses?.length || criteria.sources?.length ||
            criteria.roomType || criteria.minAmount || criteria.maxAmount;
        if (hasAdvanced) {
            const panel = document.getElementById('advancedFiltersPanel');
            if (panel && !panel.classList.contains('show')) {
                new bootstrap.Collapse(panel, { show: true });
            }
        }

        // Trigger search
        document.getElementById('applyReservationFilters')?.click();
    }

    _deleteSavedSearch(idx) {
        const saved = this._loadSavedSearches();
        saved.splice(idx, 1);
        localStorage.setItem(this._STORAGE_KEY, JSON.stringify(saved));
        this._renderSavedSearches();
    }

    // ── Export ────────────────────────────────────────────────────────────────

    _bindExportButtons() {
        const bind = (id, format) => {
            const el = document.getElementById(id);
            if (el) el.addEventListener('click', e => { e.preventDefault(); this._triggerExport(format); });
        };
        bind('exportCsvBtn',   'csv');
        bind('exportExcelBtn', 'excel');
        bind('exportPdfBtn',   'pdf');
    }

    _triggerExport(format) {
        const criteria = this.buildCriteria();
        const params = new URLSearchParams();
        Object.entries(criteria).forEach(([k, v]) => {
            if (Array.isArray(v)) v.forEach(item => params.append(k, item));
            else params.append(k, v);
        });
        params.append('format', format);

        const token = localStorage.getItem('jwt_token');
        // Use a hidden form POST so the JWT is sent and the browser handles the file download
        const form = document.createElement('form');
        form.method = 'GET';
        form.action = `/api/reservations/export?${params.toString()}`;
        form.target = '_blank';

        // Inject token via a temporary cookie approach isn't ideal;
        // instead open in same tab with Authorization header via fetch + blob
        this._downloadViaFetch(`/api/reservations/export?${params.toString()}`, format, token);
    }

    async _downloadViaFetch(url, format, token) {
        try {
            UI.showLoading?.();
            const response = await fetch(url, {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (!response.ok) throw new Error(`Export failed: ${response.status}`);

            const blob = await response.blob();
            const ext  = format === 'excel' ? 'xlsx' : format === 'pdf' ? 'pdf' : 'csv';
            const filename = `Reservations_${new Date().toISOString().slice(0,10)}.${ext}`;

            const link = document.createElement('a');
            link.href = URL.createObjectURL(blob);
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(link.href);
        } catch (err) {
            console.error('Export error:', err);
            UI.showError?.('Export failed. Please try again.');
        } finally {
            UI.hideLoading?.();
        }
    }
}

// Singleton
window.AdvancedSearch = new AdvancedSearch();
