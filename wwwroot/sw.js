self.addEventListener('install', (e) => {
  console.log('[Service Worker] Install');
});

self.addEventListener('fetch', (e) => {
  // Basit bir fetch handler, PWA gereksinimi için
  e.respondWith(fetch(e.request));
});
