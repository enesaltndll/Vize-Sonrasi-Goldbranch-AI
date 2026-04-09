const CACHE_NAME = 'gb-cache-v2';
const urlsToCache = [
  '/',
  '/css/site.css',
  '/lib/bootstrap/dist/css/bootstrap.min.css',
  '/lib/font-awesome/css/all.min.css'
];

self.addEventListener('install', event => {
  self.skipWaiting();
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(urlsToCache))
  );
});

self.addEventListener('activate', event => {
  event.waitUntil(self.clients.matchAll().then(() => {}));
});

self.addEventListener('fetch', event => {
  event.respondWith(
    caches.match(event.request)
      .then(response => {
        if (response) return response;
        return fetch(event.request).catch(() => {});
      })
  );
});

// ====================================================
// 💚 SAĞLIK KORUMA SİSTEMİ — ARKA PLAN BİLDİRİMLERİ
// Service Worker mesaj dinleyicisi
// ====================================================
self.addEventListener('message', event => {
  if (event.data && event.data.type === 'WELLNESS_NOTIFICATION') {
    const { title, body, icon, tag } = event.data;
    self.registration.showNotification(title, {
      body: body,
      icon: icon || 'https://cdn-icons-png.flaticon.com/512/9334/9334400.png',
      badge: 'https://cdn-icons-png.flaticon.com/512/9334/9334400.png',
      tag: tag || 'wellness-' + Date.now(),
      vibrate: [200, 100, 200],
      requireInteraction: false,
      actions: [
        { action: 'ok', title: '✅ Tamam, Yapıyorum!' },
        { action: 'snooze', title: '⏰ 5dk Sonra Hatırlat' }
      ]
    });
  }
});

// Bildirime tıklama olayı
self.addEventListener('notificationclick', event => {
  event.notification.close();
  if (event.action === 'snooze') {
    // 5 dakika sonra tekrar bildir
    setTimeout(() => {
      self.registration.showNotification('⏰ Hatırlatma', {
        body: 'Az önce ertelediğin sağlık molası hâlâ seni bekliyor!',
        icon: 'https://cdn-icons-png.flaticon.com/512/9334/9334400.png',
        tag: 'wellness-snooze'
      });
    }, 5 * 60 * 1000);
  } else {
    // Uygulamayı ön plana getir
    event.waitUntil(
      self.clients.matchAll({ type: 'window' }).then(clients => {
        if (clients.length > 0) {
          clients[0].focus();
        } else {
          self.clients.openWindow('/Task/Dashboard');
        }
      })
    );
  }
});
