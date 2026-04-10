/**
 * Licores Maduro — Real-time Chat Widget
 * Auto-injected by Sidebar.init() on all authenticated pages.
 * Requires: api.js, auth.js, SignalR client already loaded.
 */
const ChatWidget = (() => {
  const BASE_URL = window.API?.BASE_URL ?? '';

  let _connection   = null;
  let _myUser       = null;
  let _activeUserId = null;   // user currently open in thread view
  let _unread       = {};     // { userId: count }
  let _typingTimer  = null;

  // ── Inject widget HTML + CSS ────────────────────────────────────────────────
  function _inject() {
    if (document.getElementById('chat-widget')) return;

    // CSS
    const style = document.createElement('style');
    style.textContent = `
      #chat-widget { position:fixed; bottom:24px; right:24px; z-index:10000; font-family:inherit; }
      #chat-bubble {
        width:52px; height:52px; border-radius:50%;
        background:var(--primary,#A52535); color:#fff;
        display:flex; align-items:center; justify-content:center;
        cursor:pointer; box-shadow:0 4px 16px rgba(0,0,0,.25);
        font-size:1.35rem; position:relative; transition:transform .15s;
      }
      #chat-bubble:hover { transform:scale(1.08); }
      #chat-unread-badge {
        position:absolute; top:-4px; right:-4px;
        background:#dc3545; color:#fff; border-radius:50%;
        min-width:20px; height:20px; font-size:.68rem; font-weight:700;
        display:flex; align-items:center; justify-content:center;
        padding:0 4px; display:none;
      }
      #chat-panel {
        position:absolute; bottom:64px; right:0;
        width:320px; height:440px;
        background:#fff; border-radius:14px;
        box-shadow:0 8px 32px rgba(0,0,0,.18);
        display:none; flex-direction:column; overflow:hidden;
        border:1px solid rgba(0,0,0,.08);
      }
      #chat-panel.open { display:flex; }
      #chat-panel-header {
        background:var(--primary,#A52535); color:#fff;
        padding:12px 14px; display:flex; align-items:center; justify-content:space-between;
        font-weight:600; font-size:.95rem; flex-shrink:0;
      }
      #chat-panel-header button {
        background:none; border:none; color:#fff; opacity:.8;
        cursor:pointer; font-size:1.1rem; padding:0; line-height:1;
      }
      #chat-panel-header button:hover { opacity:1; }
      /* Conversation list */
      #chat-conv-list { flex:1; overflow-y:auto; }
      .chat-conv-item {
        display:flex; align-items:center; gap:10px;
        padding:10px 14px; cursor:pointer; border-bottom:1px solid #f0f0f0;
        transition:background .1s;
      }
      .chat-conv-item:hover { background:#f8f9fa; }
      .chat-conv-avatar {
        width:36px; height:36px; border-radius:50%; flex-shrink:0;
        background:var(--primary,#A52535); color:#fff;
        display:flex; align-items:center; justify-content:center;
        font-size:.75rem; font-weight:700;
      }
      .chat-conv-info { flex:1; min-width:0; }
      .chat-conv-name { font-weight:600; font-size:.85rem; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
      .chat-conv-last { font-size:.75rem; color:#6c757d; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
      .chat-conv-badge {
        background:#dc3545; color:#fff; border-radius:50%;
        min-width:18px; height:18px; font-size:.65rem; font-weight:700;
        display:flex; align-items:center; justify-content:center; padding:0 4px;
      }
      /* New chat button */
      #btn-new-chat {
        margin:10px 14px; padding:7px 12px; border-radius:8px;
        background:var(--primary,#A52535); color:#fff; border:none;
        font-size:.82rem; font-weight:600; cursor:pointer; width:calc(100% - 28px);
        display:flex; align-items:center; justify-content:center; gap:6px;
      }
      #btn-new-chat:hover { opacity:.9; }
      /* User picker */
      #chat-user-picker { flex:1; overflow-y:auto; }
      .chat-pick-item {
        display:flex; align-items:center; gap:10px;
        padding:9px 14px; cursor:pointer; border-bottom:1px solid #f0f0f0;
      }
      .chat-pick-item:hover { background:#f8f9fa; }
      .chat-pick-online { width:8px; height:8px; border-radius:50%; background:#198754; flex-shrink:0; }
      /* Thread */
      #chat-thread { display:none; flex-direction:column; flex:1; overflow:hidden; }
      #chat-thread-header {
        padding:10px 14px; border-bottom:1px solid #f0f0f0;
        display:flex; align-items:center; gap:8px; flex-shrink:0;
      }
      #btn-chat-back { background:none; border:none; cursor:pointer; color:#6c757d; font-size:1rem; padding:0; }
      #btn-chat-back:hover { color:var(--primary,#A52535); }
      #chat-thread-name { font-weight:600; font-size:.88rem; }
      #chat-typing { font-size:.72rem; color:#6c757d; font-style:italic; min-height:16px; padding:0 14px 4px; flex-shrink:0; }
      #chat-messages {
        flex:1; overflow-y:auto; padding:10px 14px;
        display:flex; flex-direction:column; gap:6px;
      }
      .chat-msg { max-width:75%; padding:7px 11px; border-radius:14px; font-size:.82rem; line-height:1.4; word-break:break-word; }
      .chat-msg.mine { align-self:flex-end; background:var(--primary,#A52535); color:#fff; border-bottom-right-radius:4px; }
      .chat-msg.theirs { align-self:flex-start; background:#f0f0f0; color:#212529; border-bottom-left-radius:4px; }
      .chat-msg-time { font-size:.65rem; opacity:.65; margin-top:2px; }
      #chat-input-row {
        display:flex; gap:6px; padding:8px 10px;
        border-top:1px solid #f0f0f0; flex-shrink:0;
      }
      #chat-input {
        flex:1; border:1px solid #dee2e6; border-radius:20px;
        padding:6px 14px; font-size:.82rem; outline:none;
      }
      #chat-input:focus { border-color:var(--primary,#A52535); }
      #btn-chat-send {
        width:34px; height:34px; border-radius:50%;
        background:var(--primary,#A52535); color:#fff; border:none;
        cursor:pointer; font-size:.85rem; display:flex; align-items:center; justify-content:center;
      }
      #btn-chat-send:hover { opacity:.85; }
      .chat-empty { text-align:center; color:#aaa; font-size:.82rem; padding:30px 0; }
    `;
    document.head.appendChild(style);

    // HTML
    const widget = document.createElement('div');
    widget.id = 'chat-widget';
    widget.innerHTML = `
      <div id="chat-panel">
        <div id="chat-panel-header">
          <span id="chat-panel-title"><i class="fas fa-comments me-2"></i>Messages</span>
          <button onclick="ChatWidget.close()" title="Close"><i class="fas fa-times"></i></button>
        </div>

        <!-- Conversation list view -->
        <div id="chat-view-list" style="display:flex;flex-direction:column;flex:1;overflow:hidden;">
          <button id="btn-new-chat" onclick="ChatWidget.showUserPicker()">
            <i class="fas fa-edit"></i> New Message
          </button>
          <div id="chat-conv-list"><div class="chat-empty">No conversations yet.</div></div>
        </div>

        <!-- User picker view -->
        <div id="chat-view-picker" style="display:none;flex-direction:column;flex:1;overflow:hidden;">
          <div style="padding:8px 14px;border-bottom:1px solid #f0f0f0;display:flex;align-items:center;gap:8px;">
            <button id="btn-picker-back" onclick="ChatWidget.showConvList()" style="background:none;border:none;cursor:pointer;color:#6c757d;"><i class="fas fa-arrow-left"></i></button>
            <span style="font-size:.85rem;font-weight:600;">Select a user</span>
          </div>
          <div id="chat-user-picker"></div>
        </div>

        <!-- Thread view -->
        <div id="chat-thread">
          <div id="chat-thread-header">
            <button id="btn-chat-back" onclick="ChatWidget.showConvList()"><i class="fas fa-arrow-left"></i></button>
            <div class="chat-conv-avatar" id="chat-thread-avatar" style="width:28px;height:28px;font-size:.68rem;"></div>
            <span id="chat-thread-name">–</span>
          </div>
          <div id="chat-typing"></div>
          <div id="chat-messages"></div>
          <div id="chat-input-row">
            <input id="chat-input" type="text" placeholder="Type a message..." maxlength="1000"
              onkeydown="if(event.key==='Enter')ChatWidget.send()"
              oninput="ChatWidget.onTyping()"/>
            <button id="btn-chat-send" onclick="ChatWidget.send()"><i class="fas fa-paper-plane"></i></button>
          </div>
        </div>
      </div>

      <div id="chat-bubble" onclick="ChatWidget.toggle()">
        <i class="fas fa-comments"></i>
        <span id="chat-unread-badge">0</span>
      </div>
    `;
    document.body.appendChild(widget);
  }

  // ── Start SignalR connection ─────────────────────────────────────────────────
  async function _connect() {
    if (_connection) return;
    const token = window.API?.getToken();
    if (!token) return;

    _connection = new signalR.HubConnectionBuilder()
      .withUrl(`${BASE_URL}/chatHub`, { accessTokenFactory: () => window.API.getToken() })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    _connection.on('ReceiveMessage', _onReceiveMessage);
    _connection.on('UserTyping',     _onUserTyping);
    _connection.on('UnreadCountChanged', _refreshUnread);

    try {
      await _connection.start();
    } catch (e) {
      console.warn('[Chat] SignalR connect failed:', e);
    }
  }

  // ── Handle incoming message ─────────────────────────────────────────────────
  function _onReceiveMessage(msg) {
    const myId = _myUser?.UserId;

    // If thread is open with this user, append message
    const otherId = msg.FromUserId === myId ? msg.ToUserId : msg.FromUserId;
    if (_activeUserId === otherId) {
      _appendMessage(msg, myId);
      // Mark read via hub
      if (msg.FromUserId !== myId) _connection.invoke('MarkRead', msg.FromUserId).catch(() => {});
    } else if (msg.FromUserId !== myId) {
      // Increment unread badge
      _unread[msg.FromUserId] = (_unread[msg.FromUserId] || 0) + 1;
      _updateBadge();
      // Flash bubble
      const bubble = document.getElementById('chat-bubble');
      if (bubble) { bubble.style.animation = 'none'; setTimeout(() => bubble.style.animation = '', 10); }
    }

    // Refresh conversation list if visible
    if (document.getElementById('chat-view-list')?.style.display !== 'none')
      _loadConversations();
  }

  function _onUserTyping(fromUserId) {
    if (fromUserId !== _activeUserId) return;
    const el = document.getElementById('chat-typing');
    if (!el) return;
    el.textContent = 'typing...';
    clearTimeout(_typingTimer);
    _typingTimer = setTimeout(() => { if (el) el.textContent = ''; }, 2000);
  }

  // ── Update unread badge on bubble ────────────────────────────────────────────
  function _updateBadge() {
    const total = Object.values(_unread).reduce((a, b) => a + b, 0);
    const badge = document.getElementById('chat-unread-badge');
    if (!badge) return;
    badge.textContent = total > 99 ? '99+' : total;
    badge.style.display = total > 0 ? 'flex' : 'none';
  }

  async function _refreshUnread() {
    try {
      const res = await fetch(`${BASE_URL}/api/chat/unread-count`, {
        headers: { Authorization: `Bearer ${window.API?.getToken()}` }
      });
      const data = await res.json();
      const count = data?.Data?.Count || 0;
      // Reset and set total (simplified — not per-user)
      if (count === 0) _unread = {};
      _updateBadge();
    } catch { /* ignore */ }
  }

  // ── Load conversation list ────────────────────────────────────────────────────
  async function _loadConversations() {
    const el = document.getElementById('chat-conv-list');
    if (!el) return;
    try {
      const res  = await fetch(`${BASE_URL}/api/chat/conversations`, {
        headers: { Authorization: `Bearer ${window.API?.getToken()}` }
      });
      const data = await res.json();
      const convs = data?.Data || [];

      if (!convs.length) { el.innerHTML = '<div class="chat-empty">No conversations yet.</div>'; return; }

      el.innerHTML = convs.map(c => {
        const initials = (c.FullName || c.Username || '?').split(' ').map(w => w[0]).join('').slice(0,2).toUpperCase();
        const unread   = c.Unread > 0 ? `<span class="chat-conv-badge">${c.Unread}</span>` : '';
        const last     = _esc(c.LastMessage || '').slice(0, 35);
        return `<div class="chat-conv-item" onclick="ChatWidget.openThread(${c.UserId}, '${_esc(c.FullName || c.Username)}')">
          <div class="chat-conv-avatar">${_esc(initials)}</div>
          <div class="chat-conv-info">
            <div class="chat-conv-name">${_esc(c.FullName || c.Username)}</div>
            <div class="chat-conv-last">${last}${last.length >= 35 ? '…' : ''}</div>
          </div>
          ${unread}
        </div>`;
      }).join('');
    } catch { el.innerHTML = '<div class="chat-empty">Could not load conversations.</div>'; }
  }

  // ── Load all users for picker (online status shown) ──────────────────────────
  async function _loadUserPicker() {
    const el = document.getElementById('chat-user-picker');
    if (!el) return;
    el.innerHTML = '<div class="chat-empty">Loading...</div>';
    try {
      const res  = await fetch(`${BASE_URL}/api/chat/contacts`, {
        headers: { Authorization: `Bearer ${window.API?.getToken()}` }
      });
      const data = await res.json();
      const users = data?.Data || [];

      if (!users.length) { el.innerHTML = '<div class="chat-empty">No other users found.</div>'; return; }

      el.innerHTML = users.map(u => {
        const initials   = (u.FullName || u.Username || '?').split(' ').map(w => w[0]).join('').slice(0,2).toUpperCase();
        const statusHtml = u.IsOnline
          ? `<div class="chat-conv-last" style="color:#198754;"><i class="fas fa-circle" style="font-size:.5rem;"></i> Online</div>`
          : `<div class="chat-conv-last" style="color:#adb5bd;"><i class="fas fa-circle" style="font-size:.5rem;"></i> Offline</div>`;
        return `<div class="chat-pick-item" onclick="ChatWidget.openThread(${u.UserId}, '${_esc(u.FullName || u.Username)}')">
          <div class="chat-conv-avatar">${_esc(initials)}</div>
          <div class="chat-conv-info">
            <div class="chat-conv-name">${_esc(u.FullName || u.Username)}</div>
            ${statusHtml}
          </div>
        </div>`;
      }).join('');
    } catch { el.innerHTML = '<div class="chat-empty">Could not load users.</div>'; }
  }

  // ── Open thread with a specific user ─────────────────────────────────────────
  async function openThread(userId, name) {
    _activeUserId = userId;
    // Clear unread for this user
    delete _unread[userId];
    _updateBadge();

    // Switch views
    document.getElementById('chat-view-list').style.display    = 'none';
    document.getElementById('chat-view-picker').style.display  = 'none';
    document.getElementById('chat-thread').style.display       = 'flex';

    const initials = name.split(' ').map(w => w[0]).join('').slice(0,2).toUpperCase();
    document.getElementById('chat-thread-name').textContent    = name;
    document.getElementById('chat-thread-avatar').textContent  = initials;
    document.getElementById('chat-panel-title').innerHTML      = '<i class="fas fa-comments me-2"></i>Messages';
    document.getElementById('chat-typing').textContent         = '';

    // Load history
    const msgEl = document.getElementById('chat-messages');
    msgEl.innerHTML = '<div class="chat-empty">Loading...</div>';
    try {
      const res  = await fetch(`${BASE_URL}/api/chat/history/${userId}`, {
        headers: { Authorization: `Bearer ${window.API?.getToken()}` }
      });
      const data = await res.json();
      const msgs = data?.Data || [];
      msgEl.innerHTML = '';
      if (!msgs.length) { msgEl.innerHTML = '<div class="chat-empty">No messages yet. Say hi! 👋</div>'; }
      else msgs.forEach(m => _appendMessage(m, _myUser?.UserId));
      msgEl.scrollTop = msgEl.scrollHeight;
    } catch { msgEl.innerHTML = '<div class="chat-empty">Could not load messages.</div>'; }

    // Mark read via hub
    if (_connection?.state === signalR.HubConnectionState.Connected)
      _connection.invoke('MarkRead', userId).catch(() => {});

    document.getElementById('chat-input')?.focus();
  }

  function _appendMessage(msg, myId) {
    const msgEl = document.getElementById('chat-messages');
    if (!msgEl) return;
    // Remove "no messages" placeholder
    msgEl.querySelectorAll('.chat-empty').forEach(e => e.remove());

    const mine = msg.FromUserId === myId;
    const time = new Date(msg.SentAt).toLocaleTimeString('en-US', { hour:'2-digit', minute:'2-digit', hour12:false });
    const div  = document.createElement('div');
    div.className = `chat-msg ${mine ? 'mine' : 'theirs'}`;
    div.innerHTML = `${_esc(msg.Message)}<div class="chat-msg-time">${time}</div>`;
    msgEl.appendChild(div);
    msgEl.scrollTop = msgEl.scrollHeight;
  }

  // ── Send message ─────────────────────────────────────────────────────────────
  async function send() {
    const input = document.getElementById('chat-input');
    if (!input || !_activeUserId) return;
    const text = input.value.trim();
    if (!text) return;
    if (!_connection || _connection.state !== signalR.HubConnectionState.Connected) {
      alert('Not connected to chat. Please refresh the page.'); return;
    }
    input.value = '';
    try {
      await _connection.invoke('SendMessage', _activeUserId, text);
    } catch (e) { console.error('[Chat] Send failed:', e); }
  }

  // ── Typing indicator ──────────────────────────────────────────────────────────
  function onTyping() {
    if (!_activeUserId || !_connection || _connection.state !== signalR.HubConnectionState.Connected) return;
    _connection.invoke('Typing', _activeUserId).catch(() => {});
  }

  // ── View helpers ──────────────────────────────────────────────────────────────
  function showConvList() {
    _activeUserId = null;
    document.getElementById('chat-view-list').style.display   = 'flex';
    document.getElementById('chat-view-picker').style.display = 'none';
    document.getElementById('chat-thread').style.display      = 'none';
    document.getElementById('chat-panel-title').innerHTML     = '<i class="fas fa-comments me-2"></i>Messages';
    _loadConversations();
  }

  function showUserPicker() {
    document.getElementById('chat-view-list').style.display   = 'none';
    document.getElementById('chat-view-picker').style.display = 'flex';
    document.getElementById('chat-thread').style.display      = 'none';
    _loadUserPicker();
  }

  function toggle() {
    const panel = document.getElementById('chat-panel');
    if (!panel) return;
    if (panel.classList.contains('open')) { close(); }
    else {
      panel.classList.add('open');
      showConvList();
    }
  }

  function close() {
    document.getElementById('chat-panel')?.classList.remove('open');
  }

  // ── Init ──────────────────────────────────────────────────────────────────────
  async function init() {
    if (!window.Auth?.isLoggedIn()) return;
    _myUser = window.Auth.getCurrentUser();

    _inject();

    // Load SignalR script dynamically then connect
    if (typeof signalR === 'undefined') {
      const s = document.createElement('script');
      s.src = 'https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.0/dist/browser/signalr.min.js';
      s.onload = async () => { await _connect(); await _refreshUnread(); };
      document.head.appendChild(s);
    } else {
      await _connect();
      await _refreshUnread();
    }
  }

  function _esc(s) {
    return String(s ?? '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
  }

  // ── Public ────────────────────────────────────────────────────────────────────
  return { init, toggle, close, send, onTyping, openThread, showConvList, showUserPicker };
})();

window.ChatWidget = ChatWidget;
