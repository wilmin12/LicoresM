# Email Provider Selector — Design Spec
**Date:** 2026-05-13  
**Scope:** Frontend only (`frontend/pages/email-config.html`)  
**Backend changes:** None

## Problem

The client uses Office 365 (Microsoft 365 business). The current Email Configuration form only shows generic SMTP fields with a Gmail example, giving no guidance for Office 365 users. Admins must know on their own that Office 365 uses `smtp.office365.com:587`, and must figure out how to enable SMTP AUTH in their tenant.

## Solution: Provider selector in the UI

Add a two-option pill selector at the top of the SMTP Settings card:

```
[ SMTP genérico ]   [ Office 365 ]
```

### SMTP genérico mode (default)
Identical to the current form. No changes to fields, labels, or hints.

### Office 365 mode
- Auto-fills: `SmtpHost = smtp.office365.com`, `SmtpPort = 587`, `UseSsl = true`
- Host and Port inputs become **read-only** (visually locked)
- Password label changes to "Office 365 Password"
- Password hint changes to point to the Microsoft 365 account password
- A collapsible **Setup Guide** panel appears below the password field with these steps:
  1. Sign in to [Microsoft 365 Admin Center](https://admin.microsoft.com)
  2. Go to **Users → Active Users** and select the sender mailbox
  3. Open the **Mail** tab → click **Manage email apps**
  4. Check **Authenticated SMTP** and save
  5. Use that mailbox's email address and password in this form

### Provider detection on load
When the saved config is loaded from the API, the active provider tab is inferred from `SmtpHost`:
- Contains `office365.com` or `outlook.office365.com` → activate Office 365 tab
- Anything else → activate SMTP genérico tab

## Data flow
No new fields, no new API calls. The provider selector is purely presentational. The SMTP fields (`SmtpHost`, `SmtpPort`, `UseSsl`, `SenderPassword`) already carry all information needed — the tab just controls what gets pre-filled and what instructions are shown.

## Files changed
| File | Change |
|------|--------|
| `frontend/pages/email-config.html` | Add provider pills, conditional field states, Office 365 setup guide |
| `publish/frontend/pages/email-config.html` | Mirror the same change |

## Out of scope
- Microsoft Graph API / OAuth2 (can be added as a third tab later)
- Backend model changes
- Database migration

## Testing checklist (for manual QA)
1. Load page with existing SMTP config → SMTP tab is active, fields populated correctly
2. Load page with `smtp.office365.com` saved → Office 365 tab auto-selected
3. Click Office 365 tab → host/port auto-fill, fields lock, setup guide appears
4. Click SMTP tab → fields unlock, generic placeholders restored
5. Switch to Office 365, fill sender email + password, save → config saves successfully
6. Send test email with Office 365 config → email arrives (requires SMTP AUTH enabled on tenant)
7. Switch provider tabs multiple times → no stale values bleed between modes
