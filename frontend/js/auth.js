/**
 * Licores Maduro - Auth Utilities
 * Handles token storage, JWT decoding, session checks, and logout.
 */

const Auth = (() => {
  const TOKEN_KEY = 'lm_token';
  const USER_KEY  = 'lm_user';

  // ── Storage helpers ─────────────────────────────────────────────────────────
  function getToken()   { return sessionStorage.getItem(TOKEN_KEY); }
  function getUser()    { return JSON.parse(sessionStorage.getItem(USER_KEY) || 'null'); }
  function clearSession() {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(USER_KEY);
  }

  // ── JWT decoding (without verification – server validates) ──────────────────
  function decodeToken(token) {
    try {
      const payload = token.split('.')[1];
      const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(decoded);
    } catch {
      return null;
    }
  }

  // ── Token expiry check ──────────────────────────────────────────────────────
  function isTokenExpired(token) {
    const payload = decodeToken(token);
    if (!payload || !payload.exp) return true;
    // exp is in seconds, Date.now() in ms
    return Date.now() >= payload.exp * 1000;
  }

  // ── Session status ──────────────────────────────────────────────────────────
  function isLoggedIn() {
    const token = getToken();
    return !!token && !isTokenExpired(token);
  }

  // ── Guard: redirect to login if not authenticated ───────────────────────────
  function requireAuth() {
    if (!isLoggedIn()) {
      clearSession();
      window.location.href = '/index.html';
      return false;
    }
    return true;
  }

  // ── Guard: redirect to dashboard if already authenticated ──────────────────
  function redirectIfLoggedIn() {
    if (isLoggedIn()) {
      window.location.href = '/dashboard.html';
    }
  }

  // ── Get current user info from storage ─────────────────────────────────────
  function getCurrentUser() {
    const user = getUser();
    if (!user) return null;

    // Supplement with decoded JWT claims if needed
    const token   = getToken();
    const payload = token ? decodeToken(token) : null;

    return {
      ...user,
      tokenExp: payload?.exp ? new Date(payload.exp * 1000) : null
    };
  }

  // ── Permissions helper ──────────────────────────────────────────────────────
  function getPermissions() {
    const user = getUser();
    return user?.Permissions ?? [];
  }

  function hasPermission(submoduleCode, permType = 'CanRead') {
    const user = getUser();
    if (!user) return false;
    if (user.RoleName === 'SuperAdmin') return true;  // SuperAdmin bypasses all checks

    const perm = (user.Permissions || []).find(
      p => p.SubmoduleCode === submoduleCode
    );
    if (!perm) return false;
    return perm[permType] === true;
  }

  function hasModuleAccess(moduleCode) {
    const user = getUser();
    if (!user) return false;
    if (user.RoleName === 'SuperAdmin') return true;

    return (user.Permissions || []).some(
      p => p.ModuleCode === moduleCode && p.CanAccess
    );
  }

  // ── Get user initials for avatar ────────────────────────────────────────────
  function getUserInitials() {
    const user = getCurrentUser();
    if (!user?.FullName) return '?';
    return user.FullName
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  // ── Session tracking ─────────────────────────────────────────────────────────
  const SESSION_KEY = 'lm_session_key';

  async function openSession() {
    try {
      if (!window.API) return;
      const res = await window.API.post('/api/sessions/open', {});
      if (res?.Data?.SessionKey)
        sessionStorage.setItem(SESSION_KEY, res.Data.SessionKey);
    } catch { /* non-critical */ }
  }

  async function closeSession() {
    try {
      const key = sessionStorage.getItem(SESSION_KEY);
      if (key && window.API) await window.API.post('/api/sessions/close', { SessionKey: key });
    } catch { /* non-critical */ } finally {
      sessionStorage.removeItem(SESSION_KEY);
    }
  }

  async function sendHeartbeat() {
    try {
      const key = sessionStorage.getItem(SESSION_KEY);
      if (key && window.API) await window.API.post('/api/sessions/heartbeat', { SessionKey: key });
    } catch { /* non-critical */ }
  }

  // ── Logout ──────────────────────────────────────────────────────────────────
  async function logout() {
    try {
      await closeSession();
      if (window.API) await window.API.logout();
    } catch { /* ignore */ } finally {
      clearSession();
      window.location.href = '/index.html';
    }
  }

  // ── Set avatar element (image or initials fallback) ────────────────────────
  function _setAvatar(el, user) {
    if (!el) return;
    if (user?.AvatarUrl) {
      el.style.background = 'transparent';
      el.style.padding    = '0';
      el.innerHTML = `<img src="${user.AvatarUrl}" alt="${getUserInitials()}"
        style="width:100%;height:100%;border-radius:50%;object-fit:cover;display:block;">`;
    } else {
      el.style.background = '';
      el.style.padding    = '';
      el.textContent = getUserInitials();
    }
  }

  // ── Update stored user (e.g. after avatar change) ───────────────────────────
  function updateStoredUser(patch) {
    const user = getUser();
    if (!user) return;
    const updated = { ...user, ...patch };
    sessionStorage.setItem(USER_KEY, JSON.stringify(updated));
  }

  // ── Populate user info on page ──────────────────────────────────────────────
  function populateUserUI() {
    const user = getCurrentUser();
    if (!user) return;

    // Topbar elements
    const elName     = document.getElementById('topbar-user-name');
    const elFullname = document.getElementById('topbar-user-fullname');
    const elRole     = document.getElementById('topbar-user-role');
    const elInit     = document.getElementById('topbar-user-initials');

    if (elName)     elName.textContent     = user.FullName || user.Username;
    if (elFullname) elFullname.textContent = user.FullName || user.Username;
    if (elRole)     elRole.textContent     = user.RoleName || '';
    _setAvatar(elInit, user);

    // Sidebar footer elements
    const sbName = document.getElementById('sidebar-user-name');
    const sbInit = document.getElementById('sidebar-user-initials');
    if (sbName) sbName.textContent = user.FullName || user.Username;
    _setAvatar(sbInit, user);

    // Welcome message
    const elWelcome = document.getElementById('welcome-name');
    if (elWelcome) elWelcome.textContent = user.FullName?.split(' ')[0] || user.Username;

    // Inject "My Profile" link into topbar dropdown (once)
    const logoutItem = document.querySelector('#btn-logout')?.closest('li');
    if (logoutItem && !document.getElementById('btn-my-profile')) {
      const li = document.createElement('li');
      li.innerHTML = '<a class="dropdown-item" id="btn-my-profile" href="/pages/profile.html">'
        + '<i class="fas fa-user-circle me-2"></i>My Profile</a>';
      logoutItem.before(li);
    }
  }

  // ── Token refresh timer + heartbeat ─────────────────────────────────────────
  function startExpiryWatcher() {
    // Check token expiry every minute
    setInterval(() => {
      const token = getToken();
      if (token && isTokenExpired(token)) {
        console.warn('[Auth] Token expired – redirecting to login.');
        clearSession();
        window.location.href = '/index.html';
      }
    }, 60_000);

    // Send heartbeat every 2 minutes to keep session alive
    setInterval(() => { sendHeartbeat(); }, 120_000);
  }

  // ── Public ──────────────────────────────────────────────────────────────────
  return {
    isLoggedIn,
    requireAuth,
    redirectIfLoggedIn,
    getCurrentUser,
    getPermissions,
    hasPermission,
    hasModuleAccess,
    getUserInitials,
    updateStoredUser,
    logout,
    populateUserUI,
    startExpiryWatcher,
    openSession,
    decodeToken,
    getToken
  };
})();

window.Auth = Auth;
