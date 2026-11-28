let databasePromise;
let currentScope;

function openDatabase() {
    if (databasePromise) return databasePromise;
    databasePromise = new Promise((resolve, reject) => {
        const request = indexedDB.open("ojunnysy-pos-private-v1", 1);
        request.onupgradeneeded = () => {
            const db = request.result;
            db.createObjectStore("keys");
            db.createObjectStore("drafts");
            db.createObjectStore("catalogs");
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error || new Error("No se pudo abrir el almacenamiento local."));
    });
    return databasePromise;
}

function requestResult(request) {
    return new Promise((resolve, reject) => {
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error || new Error("Falló una operación de almacenamiento local."));
    });
}

async function read(storeName, key) {
    const db = await openDatabase();
    return requestResult(db.transaction(storeName, "readonly").objectStore(storeName).get(key));
}

async function write(storeName, key, value) {
    const db = await openDatabase();
    const transaction = db.transaction(storeName, "readwrite");
    transaction.objectStore(storeName).put(value, key);
    return new Promise((resolve, reject) => {
        transaction.oncomplete = resolve;
        transaction.onerror = () => reject(transaction.error || new Error("No se pudo guardar el borrador."));
        transaction.onabort = () => reject(transaction.error || new Error("Se canceló el guardado local."));
    });
}

async function remove(storeName, key) {
    const db = await openDatabase();
    const transaction = db.transaction(storeName, "readwrite");
    transaction.objectStore(storeName).delete(key);
    return new Promise((resolve, reject) => {
        transaction.oncomplete = resolve;
        transaction.onerror = () => reject(transaction.error || new Error("No se pudo borrar el borrador local."));
    });
}

export async function initialize(userId, tenantId) {
    if (!globalThis.crypto?.subtle || !globalThis.indexedDB || !userId || !tenantId)
        throw new Error("Este navegador no permite guardar borradores POS cifrados para esta sesión.");
    const scopeBytes = new TextEncoder().encode(`${tenantId}:${userId}`);
    const scopeHash = await crypto.subtle.digest("SHA-256", scopeBytes);
    currentScope = Array.from(new Uint8Array(scopeHash), byte => byte.toString(16).padStart(2, "0")).join("");
    let key = await read("keys", currentScope);
    if (!key) {
        key = await crypto.subtle.generateKey({ name: "AES-GCM", length: 256 }, false, ["encrypt", "decrypt"]);
        await write("keys", currentScope, key);
    }
}

async function encrypt(value) {
    if (!currentScope) throw new Error("El almacenamiento POS no está asociado a la sesión actual.");
    const key = await read("keys", currentScope);
    const iv = crypto.getRandomValues(new Uint8Array(12));
    const plainText = new TextEncoder().encode(JSON.stringify(value));
    const cipherText = await crypto.subtle.encrypt({ name: "AES-GCM", iv }, key, plainText);
    return { iv: Array.from(iv), cipherText };
}

async function decrypt(record) {
    if (!record || !currentScope) return null;
    const key = await read("keys", currentScope);
    const plainText = await crypto.subtle.decrypt({ name: "AES-GCM", iv: new Uint8Array(record.iv) }, key, record.cipherText);
    return JSON.parse(new TextDecoder().decode(plainText));
}

export async function saveDraft(draft) {
    if (!draft?.lines?.length) return clearDraft();
    await write("drafts", currentScope, await encrypt(draft));
}

export async function loadDraft() {
    try { return await decrypt(await read("drafts", currentScope)); }
    catch { await remove("drafts", currentScope); return null; }
}

export async function clearDraft() {
    if (currentScope) await remove("drafts", currentScope);
}

export async function saveCatalog(catalog) {
    await write("catalogs", currentScope, await encrypt(catalog));
}

export async function loadCatalog() {
    try { return await decrypt(await read("catalogs", currentScope)); }
    catch { await remove("catalogs", currentScope); return null; }
}
