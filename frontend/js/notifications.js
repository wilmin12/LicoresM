/**
 * Licores Maduro — In-App Notifications
 * Polls /api/notifications every 30 s and renders a bell icon in the topbar.
 */
const Notifications = (() => {
  let _pollTimer   = null;
  let _lastCount   = -1;     // -1 = first poll (don't play sound on page load)
  let _audioCtx    = null;   // reusable AudioContext, unlocked on first user gesture
  let _initialized = false;  // guard against double-init (sidebar.js loads this on every page)

  // Unlock AudioContext on first user interaction (browser autoplay policy)
  function _unlockAudio() {
    if (_audioCtx) return;
    _audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    // Remove listeners once unlocked
    document.removeEventListener('click',   _unlockAudio);
    document.removeEventListener('keydown', _unlockAudio);
  }

  // ── Init ──────────────────────────────────────────────────────────────────
  function init() {
    if (_initialized) return;
    if (!window.API || !window.Auth?.isLoggedIn()) return;

    // Only Admin and SuperAdmin see the notification bell
    const role = Auth.getCurrentUser()?.RoleName;
    if (role !== 'Admin' && role !== 'SuperAdmin') return;

    _initialized = true;

    // Register unlock listeners — AudioContext needs a user gesture first
    document.addEventListener('click',   _unlockAudio, { once: true });
    document.addEventListener('keydown', _unlockAudio, { once: true });

    _injectBell();
    poll();                                    // immediate first fetch
    _pollTimer = setInterval(poll, 30_000);    // then every 30 s
  }

  // ── Inject bell into topbar ───────────────────────────────────────────────
  function _injectBell() {
    if (document.getElementById('ntf-bell-btn')) return;   // already injected

    // Find the user dropdown inside the topbar
    const userDropdown = document.querySelector('.topbar .dropdown');
    if (!userDropdown) return;

    const wrapper = document.createElement('div');
    wrapper.className = 'dropdown';
    wrapper.id = 'ntf-wrapper';
    wrapper.innerHTML = `
      <button id="ntf-bell-btn"
              class="btn btn-outline-secondary btn-sm position-relative"
              type="button"
              data-bs-toggle="dropdown"
              aria-expanded="false"
              title="Notificaciones">
        <i class="fas fa-bell"></i>
        <span id="ntf-badge"
              class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"
              style="display:none;font-size:.6rem;min-width:16px;">0</span>
      </button>

      <div class="dropdown-menu dropdown-menu-end shadow-sm p-0" style="min-width:320px;">

        <!-- Header -->
        <div class="d-flex justify-content-between align-items-center px-3 py-2 border-bottom">
          <strong style="font-size:.85rem;"><i class="fas fa-bell me-1 text-wine"></i>Notificaciones</strong>
          <button class="btn btn-link btn-sm p-0 text-muted text-decoration-none"
                  style="font-size:.75rem;"
                  onclick="Notifications.markAllRead(event)">
            Marcar todo leído
          </button>
        </div>

        <!-- List -->
        <div id="ntf-list" style="overflow-y:auto;max-height:360px;">
          <div class="text-center text-muted py-4" style="font-size:.82rem;">
            <i class="fas fa-bell-slash mb-2 d-block" style="font-size:1.5rem;opacity:.3;"></i>
            Sin notificaciones
          </div>
        </div>
      </div>`;

    // Insert before the user dropdown
    userDropdown.parentElement.insertBefore(wrapper, userDropdown);
  }

  // ── Poll API ──────────────────────────────────────────────────────────────
  async function poll() {
    try {
      const res   = await API.get('/api/notifications?unread=true');
      const items = res?.Data || [];

      // Play sound only when count increases after the first poll
      if (_lastCount >= 0 && items.length > _lastCount) _playDing();
      _lastCount = items.length;

      _render(items);
    } catch { /* non-critical — silently ignore */ }
  }

  // ── Notification sound (Web Audio API) ────────────────────────────────────
  async function _playDing() {
    try {
      // If AudioContext was never created, create it now
      if (!_audioCtx)
        _audioCtx = new (window.AudioContext || window.webkitAudioContext)();

      // Resume if suspended (browser autoplay policy)
      if (_audioCtx.state === 'suspended')
        await _audioCtx.resume();

      const t = _audioCtx.currentTime;

      function tone(freq, startAt, duration, gainPeak, type = 'sine') {
        const o = _audioCtx.createOscillator();
        const g = _audioCtx.createGain();
        o.connect(g);
        g.connect(_audioCtx.destination);
        o.type = type;
        o.frequency.setValueAtTime(freq, t + startAt);
        g.gain.setValueAtTime(0,        t + startAt);
        g.gain.linearRampToValueAtTime(gainPeak, t + startAt + 0.02);
        g.gain.exponentialRampToValueAtTime(0.001, t + startAt + duration);
        o.start(t + startAt);
        o.stop(t  + startAt + duration);
      }

      // 3-note ascending chime: C5 → E5 → G5
      tone(523,  0.00, 0.5, 0.8);           // C5
      tone(659,  0.18, 0.5, 0.8);           // E5
      tone(784,  0.36, 0.8, 1.0);           // G5 — loudest
      tone(1568, 0.36, 0.5, 0.3, 'triangle'); // G6 shimmer
    } catch (e) {
      console.warn('[Notifications] Audio error:', e);
    }
  }

  // ── Render list ───────────────────────────────────────────────────────────
  function _render(items) {
    const badge = document.getElementById('ntf-badge');
    const list  = document.getElementById('ntf-list');
    if (!badge || !list) return;

    // Badge
    if (items.length > 0) {
      badge.style.display = '';
      badge.textContent   = items.length > 99 ? '99+' : items.length;
    } else {
      badge.style.display = 'none';
    }

    // List
    if (!items.length) {
      list.innerHTML = `
        <div class="text-center text-muted py-4" style="font-size:.82rem;">
          <i class="fas fa-bell-slash mb-2 d-block" style="font-size:1.5rem;opacity:.3;"></i>
          Sin notificaciones
        </div>`;
      return;
    }

    const iconMap = { INFO:'fa-info-circle', SUCCESS:'fa-check-circle', WARNING:'fa-exclamation-triangle', DANGER:'fa-times-circle' };
    const colorMap= { INFO:'#0d6efd', SUCCESS:'#16a34a', WARNING:'#f59e0b', DANGER:'#dc3545' };

    list.innerHTML = items.map(n => {
      const icon  = iconMap[n.NtfType]  || 'fa-bell';
      const color = colorMap[n.NtfType] || '#6c757d';
      const bg    = n.NtfIsRead ? '#fff' : '#fffbf0';
      return `
        <div class="ntf-item px-3 py-2 border-bottom"
             style="cursor:pointer;background:${bg};transition:background .15s;"
             onmouseenter="this.style.background='#f8f9fa'"
             onmouseleave="this.style.background='${bg}'"
             onclick="Notifications.markRead(${n.NtfId},'${n.NtfUrl || ''}')">
          <div class="d-flex align-items-start gap-2">
            <i class="fas ${icon} mt-1 flex-shrink-0" style="color:${color};font-size:.8rem;"></i>
            <div style="flex:1;min-width:0;">
              <div style="font-size:.8rem;font-weight:600;line-height:1.2;">${_esc(n.NtfTitle)}</div>
              <div style="font-size:.75rem;color:#6c757d;margin-top:2px;">${_esc(n.NtfMessage)}</div>
              <div style="font-size:.7rem;color:#adb5bd;margin-top:2px;">${_timeAgo(n.CreatedAt)}</div>
            </div>
            ${!n.NtfIsRead
              ? '<span style="width:8px;height:8px;border-radius:50%;background:#f59e0b;flex-shrink:0;margin-top:5px;display:inline-block;"></span>'
              : ''}
          </div>
        </div>`;
    }).join('');
  }

  // ── Mark one read ─────────────────────────────────────────────────────────
  async function markRead(id, url) {
    try { await API.patch(`/api/notifications/${id}/read`, {}); } catch { /* ignore */ }
    if (url) window.location.href = url;
    else     await poll();
  }

  // ── Mark all read ─────────────────────────────────────────────────────────
  async function markAllRead(e) {
    e?.stopPropagation();
    try { await API.patch('/api/notifications/read-all', {}); } catch { /* ignore */ }
    await poll();
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  function _esc(s) {
    return String(s ?? '')
      .replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;')
      .replace(/"/g,'&quot;');
  }

  function _timeAgo(dateStr) {
    const diff = Date.now() - new Date(dateStr + (dateStr.endsWith('Z') ? '' : 'Z')).getTime();
    const m = Math.floor(diff / 60_000);
    if (m <  1)  return 'Ahora mismo';
    if (m < 60)  return `Hace ${m} min`;
    const h = Math.floor(m / 60);
    if (h < 24)  return `Hace ${h} h`;
    return `Hace ${Math.floor(h / 24)} d`;
  }

  // ── Public API ────────────────────────────────────────────────────────────
  return { init, poll, markRead, markAllRead };
})();

window.Notifications = Notifications;
