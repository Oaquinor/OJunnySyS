let shortcutHandler;
let registrations = 0;

export function register() {
    registrations++;
    if (shortcutHandler) return;

    shortcutHandler = (event) => {
        if (event.altKey || event.ctrlKey || event.metaKey || event.repeat) return;

        const targets = {
            F2: () => document.getElementById("pos-search"),
            F3: () => document.getElementById("customer"),
            F4: () => document.getElementById("seller"),
            F6: () => document.querySelector(".pos-tender-row select") ?? document.getElementById("sale-mode"),
            F9: () => document.getElementById("pos-checkout")
        };
        const resolveTarget = targets[event.key];
        if (!resolveTarget) return;

        const target = resolveTarget();
        if (!target || target.disabled) return;

        event.preventDefault();
        if (event.key === "F9") target.click();
        else target.focus({ preventScroll: true });
    };

    document.addEventListener("keydown", shortcutHandler);
}

export function unregister() {
    if (registrations > 0) registrations--;
    if (!shortcutHandler || registrations > 0) return;
    document.removeEventListener("keydown", shortcutHandler);
    shortcutHandler = undefined;
}
