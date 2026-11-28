let dotnetReference;
const updateOnline = () => dotnetReference?.invokeMethodAsync("SetOnline", navigator.onLine);

export function watch(reference) {
    unwatch();
    dotnetReference = reference;
    window.addEventListener("online", updateOnline);
    window.addEventListener("offline", updateOnline);
    updateOnline();
}

export function unwatch() {
    window.removeEventListener("online", updateOnline);
    window.removeEventListener("offline", updateOnline);
    dotnetReference = undefined;
}
