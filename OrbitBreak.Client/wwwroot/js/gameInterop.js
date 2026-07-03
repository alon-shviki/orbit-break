window.gameInterop = {
    _down: false, _x: 0, _y: 0, _sx: 0, _sy: 0,
    _released: false, _rx: 0, _ry: 0,
    _keys: {},

    getViewportSize() {
        return [window.innerWidth, window.innerHeight];
    },

    startLoop(dotnetRef) {
        // busy guard: if C# is still processing the previous tick, skip this frame instead of
        // queueing a backlog of overlapping Tick calls — dropped frames are absorbed by dt (issue #10)
        let busy = false;
        async function tick(timestamp) {
            window._rafId = requestAnimationFrame(tick);
            if (busy) return;
            busy = true;
            try { await dotnetRef.invokeMethodAsync('Tick', timestamp); }
            finally { busy = false; }
        }
        window._rafId = requestAnimationFrame(tick);
    },

    stopLoop() {
        cancelAnimationFrame(window._rafId);
    },

    initInput() {
        const g = window.gameInterop;
        window.addEventListener('pointerdown', e => {
            if (e.target.tagName !== 'CANVAS') return; // ignore drags starting on UI overlays
            g._down = true;
            g._sx = g._x = e.clientX;
            g._sy = g._y = e.clientY;
        });
        window.addEventListener('pointermove', e => {
            if (g._down) { g._x = e.clientX; g._y = e.clientY; }
        });
        window.addEventListener('pointerup', e => {
            if (!g._down) return;
            g._down = false;
            g._released = true;
            g._rx = e.clientX;
            g._ry = e.clientY;
        });
        window.addEventListener('keydown', e => { g._keys[e.code] = true; });
        window.addEventListener('keyup', e => { g._keys[e.code] = false; });
    },

    // One interop round-trip per frame instead of two (issue #10).
    // [axis, down, x, y, startX, startY, releasedEdge, releaseX, releaseY]
    // axis: -1 (left), 0, or 1 (right) — A/D or arrow keys move the paddle
    getInputState() {
        const g = window.gameInterop;
        const k = g._keys;
        let ax = 0;
        if (k['KeyA'] || k['ArrowLeft'])  ax -= 1;
        if (k['KeyD'] || k['ArrowRight']) ax += 1;
        const released = g._released ? 1 : 0;
        g._released = false;
        return [ax, g._down ? 1 : 0, g._x, g._y, g._sx, g._sy, released, g._rx, g._ry];
    }
};
