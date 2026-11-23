const saveKey = `blackwake:campaign:v1:${new URL(".", import.meta.url).pathname}`;

export function load() {
    return localStorage.getItem(saveKey);
}

export function save(json) {
    localStorage.setItem(saveKey, json);
}

export function steering(arena, dotnet) {
    const keys = new Set();
    const directions = { ArrowUp: -1, w: -1, ArrowDown: 1, s: 1 };
    const listeners = new AbortController();
    const options = { signal: listeners.signal };
    let direction = 0;

    function sendDirection() {
        const next = Math.sign([...keys].reduce((sum, key) => sum + directions[key], 0));
        if (next === direction) return;
        direction = next;
        void dotnet.invokeMethodAsync("SetDirection", direction);
    }

    window.addEventListener("keydown", event => {
        const key = event.key.length === 1 ? event.key.toLowerCase() : event.key;
        if (!Object.hasOwn(directions, key) || event.altKey || event.ctrlKey || event.metaKey ||
            event.target instanceof HTMLInputElement) return;
        event.preventDefault();
        keys.add(key);
        sendDirection();
    }, options);
    window.addEventListener("keyup", event => {
        keys.delete(event.key.length === 1 ? event.key.toLowerCase() : event.key);
        sendDirection();
    }, options);
    window.addEventListener("blur", () => {
        keys.clear();
        sendDirection();
    }, options);
    arena.addEventListener("pointerdown", event => {
        const transform = arena.getScreenCTM();
        if (!transform) return;
        const point = new DOMPoint(event.clientX, event.clientY).matrixTransform(transform.inverse());
        void dotnet.invokeMethodAsync("Steer", point.y);
    }, options);

    return { dispose: () => listeners.abort() };
}
