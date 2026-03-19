/**
 * Service Worker – Task 10.5 PWA Features
 * Strategy:
 *   - App shell (CSS/JS/fonts): Cache-first
 *   - API calls (/api/*): Network-first with offline fallback
 *   - Navigation requests: Network-first, fallback to /offline
 *   - Images: Cache-first with stale-while-revalidate
 */

const CACHE_VERSION = 'v2';
const SHELL_CACHE   = `hotel-shell-${CACHE_VERSION}`;
const DATA_CACHE    = `hotel-data-${CACHE_VERSION}`;
const IMAGE_CACHE   = `hotel-images-${CACHE_VERSION}`;

// App shell assets – cached on install
const SHELL_ASSETS = [
    '/',
    '/offline',
    '/css/site.css',
    '/css/mobile.css',
    '/css/calendar.css',
    '/js/site.js',
    '/js/modules/api.js',
    '/js/modules/auth.js',
    '/js/modules/ui.js',
    '/js/modules/mobile.js',
    '/js/modules/notifications.js',
    '/manifest.json',
    // CDN assets
    'https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css',
    'https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css',
    'https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js',
];

// ── Install ──────────────────────────────────────────────────
self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(SHELL_CACHE)
            .then(cache => cache.addAll(SHELL_ASSETS.map(url => new Request(url, { cache: 'reload' }))))
            .then(() => self.skipWaiting())
            .catch(err => console.warn('[SW] Shell cache failed (some assets may be missing):', err))
    );
});

// ── Activate – clean up old caches ──────────────────────────
self.addEventListener('activate', (event) => {
    const validCaches = [SHELL_CACHE, DATA_CACHE, IMAGE_CACHE];
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(
                keys
                    .filter(key => !validCaches.includes(key))
                    .map(key => caches.delete(key))
            ))
            .then(() => self.clients.claim())
    );
});

// ── Fetch ────────────────────────────────────────────────────
self.addEventListener('fetch', (event) => {
    const { request } = event;
    const url = new URL(request.url);

    // Skip non-GET and chrome-extension requests
    if (request.method !== 'GET') return;
    if (url.protocol === 'chrome-extension:') return;

    // API calls: network-first, no offline fallback (return error)
    if (url.pathname.startsWith('/api/')) {
        event.respondWith(networkFirst(request, DATA_CACHE, null));
        return;
    }

    // Navigation requests: network-first, fallback to /offline
    if (request.mode === 'navigate') {
        event.respondWith(networkFirst(request, SHELL_CACHE, '/offline'));
        return;
    }

    // Images: cache-first
    if (request.destination === 'image') {
        event.respondWith(cacheFirst(request, IMAGE_CACHE));
        return;
    }

    // App shell (CSS, JS, fonts): cache-first
    event.respondWith(cacheFirst(request, SHELL_CACHE));
});

// ── Strategy: Network-first ──────────────────────────────────
async function networkFirst(request, cacheName, offlineFallback) {
    try {
        const networkResponse = await fetch(request);
        if (networkResponse.ok) {
            const cache = await caches.open(cacheName);
            cache.put(request, networkResponse.clone());
        }
        return networkResponse;
    } catch (_) {
        const cached = await caches.match(request);
        if (cached) return cached;
        if (offlineFallback) {
            const fallback = await caches.match(offlineFallback);
            if (fallback) return fallback;
        }
        return new Response('Offline', { status: 503, statusText: 'Service Unavailable' });
    }
}

// ── Strategy: Cache-first ────────────────────────────────────
async function cacheFirst(request, cacheName) {
    const cached = await caches.match(request);
    if (cached) return cached;
    try {
        const networkResponse = await fetch(request);
        if (networkResponse.ok) {
            const cache = await caches.open(cacheName);
            cache.put(request, networkResponse.clone());
        }
        return networkResponse;
    } catch (_) {
        return new Response('', { status: 404, statusText: 'Not Found' });
    }
}

// ── Push Notifications ───────────────────────────────────────
self.addEventListener('push', (event) => {
    let data = {};
    try { data = event.data ? event.data.json() : {}; } catch (_) {}

    const title   = data.title   || 'Hotel Reservation System';
    const options = {
        body:    data.body    || 'You have a new notification.',
        icon:    data.icon    || '/images/icon-192x192.png',
        badge:   data.badge   || '/images/icon-192x192.png',
        tag:     data.tag     || 'hotel-notification',
        data:    data.url     ? { url: data.url } : {},
        actions: data.actions || [],
        vibrate: [200, 100, 200],
        requireInteraction: data.requireInteraction || false
    };

    event.waitUntil(self.registration.showNotification(title, options));
});

// ── Notification click ───────────────────────────────────────
self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    const url = event.notification.data?.url || '/';
    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then(windowClients => {
                const existing = windowClients.find(c => c.url === url && 'focus' in c);
                if (existing) return existing.focus();
                return clients.openWindow(url);
            })
    );
});

// ── Background sync (for offline form submissions) ───────────
self.addEventListener('sync', (event) => {
    if (event.tag === 'sync-reservations') {
        event.waitUntil(syncPendingReservations());
    }
});

async function syncPendingReservations() {
    // Placeholder: in production, read from IndexedDB and POST to API
    console.log('[SW] Background sync: sync-reservations');
}
