// CPM service worker — bewust minimaal en veilig. Werfdata/ingelogde pagina's worden NOOIT gecacheerd.
//  - Pagina's (navigaties): network-first; enkel bij een netwerkfout de offline-pagina.
//  - Statische assets (css/js/fonts/iconen/afbeeldingen, zelfde origin): cache-first, versie in de cache-naam.
//  - Alles anders (POST/PUT/DELETE, AJAX/JSON, API, andere origins): niet onderschept, gaat gewoon naar het netwerk.
// Nieuwe release: verhoog VERSION → oude caches worden bij activate opgeruimd; skipWaiting + clients.claim
// zorgen dat een nieuwe worker meteen overneemt (geen vastlopen op een oude versie).
const VERSION = "v1";
const STATIC_CACHE = "cpm-static-" + VERSION;
const CORE_CACHE = "cpm-core-" + VERSION;
const OFFLINE_URL = "/offline.html";
const CORE_ASSETS = [OFFLINE_URL, "/icons/icon-192.png", "/icons/icon-512.png"];

self.addEventListener("install", (event) => {
    event.waitUntil(caches.open(CORE_CACHE).then((c) => c.addAll(CORE_ASSETS)).then(() => self.skipWaiting()));
});

self.addEventListener("activate", (event) => {
    event.waitUntil(
        caches.keys()
            .then((keys) => Promise.all(keys.filter((k) => k.startsWith("cpm-") && k !== STATIC_CACHE && k !== CORE_CACHE).map((k) => caches.delete(k))))
            .then(() => self.clients.claim())
    );
});

function isStaticAsset(url, request) {
    if (url.origin !== self.location.origin) return false;
    // css/js van de app zelf alleen cache-first met versie-hash (?v=…, asp-append-version): zonder hash zou
    // een deploy op een verouderde kopie blijven hangen. lib/fonts/img/icons zijn stabiel.
    if (/^\/(css|js)\//i.test(url.pathname)) return url.searchParams.has("v");
    if (/^\/(lib|fonts|img|icons)\//i.test(url.pathname)) return true;
    return ["style", "script", "font", "image"].includes(request.destination) && /\.(css|js|woff2?|ttf|png|jpg|jpeg|svg|gif|webp|ico)$/i.test(url.pathname);
}

self.addEventListener("fetch", (event) => {
    const request = event.request;
    if (request.method !== "GET") return;
    const url = new URL(request.url);
    if (url.origin !== self.location.origin) return;

    if (request.mode === "navigate") {
        event.respondWith(
            fetch(request).catch(() => caches.open(CORE_CACHE).then((c) => c.match(OFFLINE_URL)))
        );
        return;
    }

    if (isStaticAsset(url, request)) {
        event.respondWith(
            caches.open(STATIC_CACHE).then((cache) =>
                cache.match(request).then((hit) => hit || fetch(request).then((res) => {
                    if (res.ok && res.type === "basic") cache.put(request, res.clone());
                    return res;
                }))
            )
        );
    }
});
