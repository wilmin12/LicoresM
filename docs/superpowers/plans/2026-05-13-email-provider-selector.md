# Email Provider Selector Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an SMTP genérico / Office 365 provider pill selector to `email-config.html` so admins can choose their mail provider and get pre-filled settings and a setup guide for Office 365.

**Architecture:** Pure frontend change to a single HTML file. A two-button pill selector at the top of the settings card drives `setProvider(type)`, which locks/unlocks fields and toggles an Office 365 setup guide. Provider is inferred from the saved `SmtpHost` value on page load — no new API fields or DB columns needed.

**Tech Stack:** Vanilla JS, Bootstrap 5.3, Font Awesome 6.5 (already loaded on the page)

---

### Task 1: Add provider pills and IDs to existing password elements

**Files:**
- Modify: `frontend/pages/email-config.html`

This task adds the pill buttons HTML at the top of the card body, and adds `id` attributes to the password label and hint elements so the JS in Task 2 can update them.

- [ ] **Step 1: Add `id="lbl-password"` to the App Password label**

Find this block (around line 105):
```html
                <label class="form-label">
                  App Password
                  <span id="password-status" class="badge bg-secondary ms-2" style="font-size:.7rem;">Not set</span>
                </label>
```
Replace with:
```html
                <label class="form-label" id="lbl-password">
                  App Password
                  <span id="password-status" class="badge bg-secondary ms-2" style="font-size:.7rem;">Not set</span>
                </label>
```

- [ ] **Step 2: Add `id="hint-password"` to the password hint div**

Find this block (around line 114):
```html
                <div class="form-text">
                  For Gmail, use an <strong>App Password</strong> (not your regular password).
                  <a href="#" onclick="return false;" title="Google Account → Security → 2-Step Verification → App passwords">How?</a>
                </div>
```
Replace with:
```html
                <div class="form-text" id="hint-password">
                  For Gmail, use an <strong>App Password</strong> (not your regular password).
                  <a href="#" onclick="return false;" title="Google Account → Security → 2-Step Verification → App passwords">How?</a>
                </div>
```

- [ ] **Step 3: Add the Office 365 setup guide panel after the password hint**

Find the closing `</div>` that ends the `mb-4` password block (the `</div>` immediately after the `hint-password` div, around line 119). Insert the setup guide between the hint and that closing tag:

Before (end of password section):
```html
                <div class="form-text" id="hint-password">
                  For Gmail, use an <strong>App Password</strong> (not your regular password).
                  <a href="#" onclick="return false;" title="Google Account → Security → 2-Step Verification → App passwords">How?</a>
                </div>
              </div>
```
After:
```html
                <div class="form-text" id="hint-password">
                  For Gmail, use an <strong>App Password</strong> (not your regular password).
                  <a href="#" onclick="return false;" title="Google Account → Security → 2-Step Verification → App passwords">How?</a>
                </div>

                <!-- Office 365 Setup Guide -->
                <div id="o365-setup-guide" class="d-none mt-3">
                  <div class="alert alert-info py-2 px-3 mb-0" style="font-size:.85rem;">
                    <strong><i class="fab fa-microsoft me-1"></i>Cómo habilitar SMTP AUTH en Office 365</strong>
                    <ol class="mb-0 mt-2 ps-3">
                      <li>Inicia sesión en <strong>Microsoft 365 Admin Center</strong> (<a href="https://admin.microsoft.com" target="_blank" rel="noopener">admin.microsoft.com</a>)</li>
                      <li>Ve a <strong>Usuarios → Usuarios activos</strong> y selecciona el buzón remitente</li>
                      <li>Abre la pestaña <strong>Correo</strong> → haz clic en <strong>Administrar aplicaciones de correo</strong></li>
                      <li>Activa <strong>SMTP autenticado</strong> y guarda</li>
                      <li>Usa ese email y contraseña en este formulario</li>
                    </ol>
                  </div>
                </div>
              </div>
```

- [ ] **Step 4: Add provider pills at the top of the card body**

Find the opening of the card body (around line 63–65):
```html
            <div class="card-body">

              <!-- Enable toggle -->
```
Replace with:
```html
            <div class="card-body">

              <!-- Provider selector -->
              <div class="d-flex gap-2 mb-4">
                <button type="button" id="pill-smtp" class="btn btn-sm btn-outline-secondary active"
                        onclick="setProvider('smtp')">
                  <i class="fas fa-server me-1"></i>SMTP genérico
                </button>
                <button type="button" id="pill-o365" class="btn btn-sm btn-outline-primary"
                        onclick="setProvider('office365')">
                  <i class="fab fa-microsoft me-1"></i>Office 365
                </button>
              </div>

              <!-- Enable toggle -->
```

- [ ] **Step 5: Open the page in a browser and verify**

Open `frontend/pages/email-config.html` (via the dev server or directly). You should see two pill buttons — "SMTP genérico" and "Office 365" — at the top of the settings card. Clicking them does nothing yet (JS not added). No existing functionality should be broken.

---

### Task 2: Add `setProvider()` and `detectProvider()` JavaScript functions

**Files:**
- Modify: `frontend/pages/email-config.html` (the `<script>` block)

- [ ] **Step 1: Add module-level `_provider` variable**

In the `<script>` block, find the `// ── Auth guard` comment. Insert the following ABOVE it:

```javascript
  // ── Provider selector ────────────────────────────────────────────────────────
  let _provider = 'smtp';

  function setProvider(type) {
    _provider = type;

    const isO365 = type === 'office365';

    // Update pill active states
    document.getElementById('pill-smtp').classList.toggle('active', !isO365);
    document.getElementById('pill-o365').classList.toggle('active', isO365);

    const hostEl    = document.getElementById('f-smtp-host');
    const portEl    = document.getElementById('f-smtp-port');
    const sslEl     = document.getElementById('f-use-ssl');
    const pwdLabel  = document.getElementById('lbl-password');
    const pwdHint   = document.getElementById('hint-password');
    const guideEl   = document.getElementById('o365-setup-guide');
    const pwdStatus = document.getElementById('password-status');

    if (isO365) {
      hostEl.value    = 'smtp.office365.com';
      portEl.value    = '587';
      sslEl.checked   = true;
      hostEl.readOnly = true;
      portEl.readOnly = true;
      hostEl.classList.add('bg-light');
      portEl.classList.add('bg-light');
      pwdLabel.childNodes[0].textContent = 'Office 365 Password ';
      pwdHint.innerHTML = 'Enter the password for the Office 365 mailbox used as sender.';
      guideEl.classList.remove('d-none');
    } else {
      hostEl.readOnly = false;
      portEl.readOnly = false;
      hostEl.classList.remove('bg-light');
      portEl.classList.remove('bg-light');
      pwdLabel.childNodes[0].textContent = 'App Password ';
      pwdHint.innerHTML = 'For Gmail, use an <strong>App Password</strong> (not your regular password). <a href="#" onclick="return false;" title="Google Account → Security → 2-Step Verification → App passwords">How?</a>';
      guideEl.classList.add('d-none');
    }
  }

  function detectProvider(host) {
    if (host && (host.includes('office365.com') || host.includes('outlook.office365.com'))) {
      setProvider('office365');
    } else {
      setProvider('smtp');
    }
  }

```

- [ ] **Step 2: Wire `detectProvider` into `loadConfig()`**

Find the end of the `loadConfig()` try block. It currently ends with `updateEnabledBadge(!!c.IsEnabled);`. Add the `detectProvider` call immediately after:

Before:
```javascript
      updateEnabledBadge(!!c.IsEnabled);
    } catch (err) {
      showStatus('danger', `Error loading configuration: ${err.message}`);
    }
```
After:
```javascript
      updateEnabledBadge(!!c.IsEnabled);
      detectProvider(c.SmtpHost || '');
    } catch (err) {
      showStatus('danger', `Error loading configuration: ${err.message}`);
    }
```

- [ ] **Step 3: Verify pill switching in the browser**

Open the page. Test each of these interactions:

| Action | Expected result |
|--------|----------------|
| Click "Office 365" pill | Host = `smtp.office365.com`, Port = `587`, SSL checked, both fields read-only (grey background), label says "Office 365 Password", setup guide appears |
| Click "SMTP genérico" pill | Host and Port fields become editable, label says "App Password", Gmail hint restored, setup guide hidden |
| Click "Office 365" then "SMTP genérico" repeatedly | No visual glitches, values stay consistent |

---

### Task 3: Mirror changes to `publish/` and commit

**Files:**
- Modify: `publish/frontend/pages/email-config.html`

The `publish/` directory mirrors `frontend/`. Both files must be kept in sync.

- [ ] **Step 1: Apply the identical changes to `publish/frontend/pages/email-config.html`**

Apply every edit from Task 1 and Task 2 to `publish/frontend/pages/email-config.html`. The file content is identical to `frontend/pages/email-config.html` — all line numbers and surrounding context are the same.

- [ ] **Step 2: Verify both files are consistent**

Run:
```powershell
Compare-Object (Get-Content "frontend\pages\email-config.html") (Get-Content "publish\frontend\pages\email-config.html")
```
Expected output: *(empty — no differences)*

- [ ] **Step 3: Commit**

```powershell
git add frontend/pages/email-config.html publish/frontend/pages/email-config.html docs/superpowers/specs/2026-05-13-email-provider-selector-design.md docs/superpowers/plans/2026-05-13-email-provider-selector.md
git commit -m "feat(email-config): add Office 365 provider selector with setup guide"
```

---

## Manual QA Checklist

After implementation is complete, verify these scenarios:

1. **Existing SMTP config loads correctly** — open the page with a non-O365 host saved → SMTP tab is active, fields editable, Gmail hint shown
2. **Office 365 config auto-detected** — save `smtp.office365.com` as host, reload page → Office 365 tab activates automatically, fields locked
3. **Switching to Office 365** — click Office 365 pill → host/port auto-fill and lock, setup guide appears
4. **Switching back to SMTP** — click SMTP genérico pill → fields unlock, hint restored, guide hidden
5. **Save works from Office 365 mode** — fill sender email + password, click Save → config saves without errors
6. **Send test email (O365)** — requires SMTP AUTH enabled on tenant — test email arrives
7. **No bleed between modes** — switch tabs multiple times, then save → saved host reflects the currently active mode's value
