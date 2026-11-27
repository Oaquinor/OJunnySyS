const CACHE_NAME = "ojunnysy-shell-v2";
const OFFLINE_URL = "/offline.html";
const STATIC_EXTENSIONS = /\.(?:css|js|svg|png|webmanifest|woff2?)$/i;
const PRECACHE_URLS = [OFFLINE_URL, "/manifest.webmanifest", "/icons/ojunnysy-icon.svg", "/features.css"];

self.addEventListener("install", event => {
  event.waitUntil(caches.open(CACHE_NAME).then(cache => cache.addAll(PRECACHE_URLS)));
  self.skipWaiting();
});

self.addEventListener("activate", event => {
  event.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(key => key.startsWith("ojunnysy-shell-") && key !== CACHE_NAME).map(key => caches.delete(key)))));
  self.clients.claim();
});

self.addEventListener("fetch", event => {
  const request = event.request;
  const url = new URL(request.url);
  if (request.method !== "GET" || url.origin !== self.location.origin) return;

  if (request.mode === "navigate") {
    event.respondWith(fetch(request).catch(() => caches.match(OFFLINE_URL)));
    return;
  }

  if (url.pathname.startsWith("/api/") || url.pathname.startsWith("/account/")) return;
  if (!STATIC_EXTENSIONS.test(url.pathname)) return;

  event.respondWith(caches.open(CACHE_NAME).then(async cache => {
    const cached = await cache.match(request);
    try {
      const response = await fetch(request);
      if (response.ok && response.type === "basic") await cache.put(request, response.clone());
      return response;
    } catch {
      return cached || Response.error();
    }
  }));
});
