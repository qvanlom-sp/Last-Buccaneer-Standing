export function enableDrag(grid, preview, dotnet) {
    const plan = grid.parentElement;
    const listeners = new AbortController();
    const options = { signal: listeners.signal };
    const cellSelector = ".grid-deck.active [data-cell]";
    const coordinates = cell => cell.dataset.cell.split(",").map(Number);
    let drag;
    let target;
    let hoverTab;
    let hoverTimer;
    let suppressClick = false;

    function setTarget(cell) {
        if (target === cell) return;
        target?.classList.remove("drop-target", "drop-swap");
        target = cell;
        target?.classList.add(target.dataset.mounted ? "drop-swap" : "drop-target");
    }

    function previewDeck(tab) {
        if (hoverTab === tab) return;
        clearTimeout(hoverTimer);
        hoverTab = tab;
        if (tab && tab.getAttribute("aria-pressed") !== "true") {
            hoverTimer = setTimeout(() => {
                void dotnet.invokeMethodAsync("PreviewDeck", Number(tab.dataset.deckTab));
            }, 350);
        }
    }

    function finish() {
        const gesture = drag;
        drag = undefined;
        if (!gesture?.started) return;
        suppressClick = true;
        setTarget(null);
        previewDeck(null);
        gesture.cell.classList.remove("drag-source");
        grid.classList.remove("dragging");
        preview.replaceChildren();
        if (grid.hasPointerCapture(gesture.id)) grid.releasePointerCapture(gesture.id);
    }

    window.addEventListener("pointerdown", () => { suppressClick = false; }, { ...options, capture: true });
    grid.addEventListener("pointerdown", event => {
        if (drag || !event.isPrimary || event.button !== 0) return;
        const cell = event.target.closest(cellSelector);
        if (!cell?.dataset.mounted) return;
        drag = { id: event.pointerId, cell, startX: event.clientX, startY: event.clientY, started: false };
    }, options);

    window.addEventListener("pointermove", event => {
        if (drag?.id !== event.pointerId) return;
        if (!drag.started) {
            if (Math.hypot(event.clientX - drag.startX, event.clientY - drag.startY) < 6) return;
            drag.started = true;
            grid.setPointerCapture(event.pointerId);
            const ghost = drag.cell.cloneNode(true);
            ghost.removeAttribute("transform");
            ghost.removeAttribute("tabindex");
            ghost.classList.remove("chosen", "gunner-target");
            preview.append(ghost);
            drag.cell.classList.add("drag-source");
            grid.classList.add("dragging");
        }

        const matrix = grid.getScreenCTM();
        if (matrix) {
            const point = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
            preview.setAttribute("transform", `translate(${point.x} ${point.y})`);
        }
        const element = document.elementFromPoint(event.clientX, event.clientY);
        const tab = element?.closest("[data-deck-tab]");
        previewDeck(tab && plan.contains(tab) ? tab : null);
        const cell = element?.closest(cellSelector);
        setTarget(cell && cell !== drag.cell && grid.contains(cell) ? cell : null);
    }, options);

    window.addEventListener("pointerup", event => {
        if (drag?.id !== event.pointerId) return;
        const cell = drag.started && document.elementFromPoint(event.clientX, event.clientY)?.closest(cellSelector);
        const source = drag.cell;
        finish();
        if (cell && cell !== source && grid.contains(cell)) {
            void dotnet.invokeMethodAsync("Drop", coordinates(source), coordinates(cell));
        }
    }, options);

    function cancelPointer(event) {
        if (drag?.id === event.pointerId) finish();
    }
    window.addEventListener("pointercancel", cancelPointer, options);
    grid.addEventListener("lostpointercapture", cancelPointer, options);
    window.addEventListener("keydown", event => {
        if (event.key === "Escape") finish();
    }, options);
    plan.addEventListener("click", event => {
        if (suppressClick && event.detail !== 0) {
            event.preventDefault();
            event.stopPropagation();
        }
    }, { ...options, capture: true });

    return {
        dispose() {
            finish();
            listeners.abort();
        }
    };
}
