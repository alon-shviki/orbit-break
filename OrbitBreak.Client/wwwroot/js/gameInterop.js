window.gameInterop = {
    _down: false, _x: 0, _y: 0, _sx: 0, _sy: 0,
    _released: false, _rx: 0, _ry: 0,
    _keys: {},

    getViewportSize() {
        return [window.innerWidth, window.innerHeight];
    },

    startLoop(dotnetRef) {
        function tick(timestamp) {
            dotnetRef.invokeMethodAsync('Tick', timestamp);
            window._rafId = requestAnimationFrame(tick);
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

    // [down, x, y, startX, startY, releasedEdge, releaseX, releaseY]
    getPointerState() {
        const g = window.gameInterop;
        const released = g._released ? 1 : 0;
        g._released = false;
        return [g._down ? 1 : 0, g._x, g._y, g._sx, g._sy, released, g._rx, g._ry];
    },

    // -1 (left), 0, or 1 (right) — A/D or arrow keys move the paddle
    getPaddleAxis() {
        const k = window.gameInterop._keys;
        let ax = 0;
        if (k['KeyA'] || k['ArrowLeft'])  ax -= 1;
        if (k['KeyD'] || k['ArrowRight']) ax += 1;
        return ax;
    }
};
