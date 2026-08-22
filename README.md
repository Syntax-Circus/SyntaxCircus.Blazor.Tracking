# SyntaxCircus.Blazor.Tracking

[![Build](https://github.com/Syntax-Circus/SyntaxCircus.Blazor.Tracking/actions/workflows/build.yml/badge.svg)](https://github.com/Syntax-Circus/SyntaxCircus.Blazor.Tracking/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/SyntaxCircus.Blazor.Tracking.svg)](https://www.nuget.org/packages/SyntaxCircus.Blazor.Tracking)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)

Consent-aware analytics building blocks for Blazor applications. The package bootstraps self-hosted Umami and GA4 from configuration, and supplies an accessible consent UI that a host can completely style or replace.

> **No support guaranteed.** Published as-is and maintained on a best-effort basis. This package supports privacy engineering; it does not provide legal advice or replace a host application's jurisdiction-specific review.

## Install

```bash
dotnet add package SyntaxCircus.Blazor.Tracking
```

Register it in `Program.cs`:

```csharp
builder.Services.AddSyntaxCircusTracking(builder.Configuration);
```

Import the components in the application's `_Imports.razor`:

```razor
@using SyntaxCircus.Blazor.Tracking.Components
```

Render the head bootstrap once, plus the banner and settings control in the body:

```razor
<head>
    <TrackingHead />
</head>
<body>
    <Routes />
    <ConsentBanner />
    <footer><ConsentSettings /></footer>
</body>
```

## Configuration

All providers are disabled by default. An enabled provider must be complete or startup validation fails.

```json
{
  "Tracking": {
    "Consent": {
      "PolicyVersion": "1",
      "CookieName": "syntax_circus_privacy",
      "CookieLifetimeDays": 180,
      "PrivacyPolicyUrl": "/privacy"
    },
    "Umami": {
      "Enabled": false,
      "ScriptUrl": "https://analytics.example.com/script.js",
      "WebsiteId": "your-umami-website-id"
    },
    "GoogleAnalytics": {
      "Enabled": false,
      "MeasurementId": "G-XXXXXXXXXX"
    }
  }
}
```

| Setting | Purpose |
|---|---|
| `Umami:Enabled` | Enables cookie-free Umami tracking. |
| `Umami:ScriptUrl` | The URL of the self-hosted Umami tracker. |
| `Umami:WebsiteId` | The Umami website ID. |
| `GoogleAnalytics:Enabled` | Enables consent-gated GA4. |
| `GoogleAnalytics:MeasurementId` | GA4 measurement ID, normally supplied through deployment configuration. |
| `Consent:PolicyVersion` | Invalidates a previous choice when privacy policy changes. |
| `Consent:CookieName` | Essential first-party preference cookie name. |
| `Consent:CookieLifetimeDays` | Preference-cookie lifetime, from 1 to 400 days. |
| `Consent:PrivacyPolicyUrl` | Optional link shown in the default banner. |

### Umami only

Enable Umami with a tracker you operate. It is loaded on every visit and does not create the consent banner because it uses no visitor cookie.

```json
"Umami": {
  "Enabled": true,
  "ScriptUrl": "https://analytics.example.com/script.js",
  "WebsiteId": "b2a6af60-8ca1-4c92-a931-5d1d6ec9201d"
}
```

### GA4

Enable GA4 only when the host is ready to request consent. The package uses Basic Consent Mode: it does not load `gtag.js`, send a Google request, or set a GA cookie until analytics consent is given. Marketing consent controls `ad_storage`, `ad_user_data`, and `ad_personalization`; analytics consent controls `analytics_storage`.

```json
"GoogleAnalytics": {
  "Enabled": true,
  "MeasurementId": "G-XXXXXXXXXX"
}
```

Use deployment secrets/environment variables rather than committing production IDs when that is your team's policy:

```text
Privacy__GoogleAnalytics__Enabled=true
Privacy__GoogleAnalytics__MeasurementId=G-XXXXXXXXXX
```

## Consent UI customization

The package ships semantic defaults but no CSS. Style the default classes and hooks in the host:

```css
.syntax-circus-consent-banner { position: fixed; inset: auto 1rem 1rem; }
.syntax-circus-consent-settings { text-decoration: underline; }
```

Replace individual regions when only copy or layout changes:

```razor
<ConsentBanner CssClass="my-consent-card">
    <HeaderContent><h2>Privacy, on your terms</h2></HeaderContent>
    <ActionsContent>
        <button type="button" data-privacy-action="accept-all">Allow analytics</button>
        <button type="button" data-privacy-action="reject-all">No thanks</button>
        <button type="button" data-privacy-action="open-settings">Choose settings</button>
        <button type="button" data-privacy-action="save-settings" hidden>Save</button>
    </ActionsContent>
</ConsentBanner>
```

`ChildContent` replaces all default markup. Preserve `data-privacy-banner` on the outer element and use the action/category hooks below so the package JavaScript can perform the choice.

```razor
<ConsentBanner>
    <ChildContent>
        <aside class="my-consent-card" data-privacy-banner hidden role="dialog" aria-label="Privacy choices">
            <h2>Choose optional tracking</h2>
            <label><input type="checkbox" data-privacy-category="analytics" /> Analytics</label>
            <label><input type="checkbox" data-privacy-category="marketing" /> Marketing</label>
            <button type="button" data-privacy-action="save-settings">Save choices</button>
            <button type="button" data-privacy-action="reject-all">Reject all</button>
        </aside>
    </ChildContent>
</ConsentBanner>
```

| Hook | Behavior |
|---|---|
| `data-privacy-banner` | Banner root that can be displayed, hidden, and queried for choices. |
| `data-privacy-action="accept-all"` | Saves analytics and marketing consent. |
| `data-privacy-action="reject-all"` | Saves rejection of optional categories. |
| `data-privacy-action="open-settings"` | Reveals the default settings controls and banner. |
| `data-privacy-action="save-settings"` | Saves category checkboxes. |
| `data-privacy-category="analytics"` | Analytics checkbox. |
| `data-privacy-category="marketing"` | Marketing checkbox. |
| `data-privacy-settings-link` | A persistent control that opens preferences. |

## Host responsibilities

- Write the privacy notice and decide where it is linked.
- Choose the countries in which consent is shown; this package applies the configured policy globally.
- Keep the dashboard, database, backup policy, and provider credentials secure.
- Do not add tracking pixels or vendor scripts outside `TrackingHead`, or they can bypass consent.
- Test browser cookies and network traffic after every provider/configuration change.

## Validation

```bash
dotnet restore SyntaxCircus.Blazor.Tracking.slnx
dotnet build SyntaxCircus.Blazor.Tracking.slnx --configuration Release
dotnet test SyntaxCircus.Blazor.Tracking.slnx --no-build --configuration Release
dotnet pack SyntaxCircus.Blazor.Tracking.slnx --no-build --configuration Release
```

Verify a GA4 deployment in browser developer tools: before an analytics choice there must be no request to `googletagmanager.com` and no `_ga` cookie. After a choice, confirm only the consented provider runs.
