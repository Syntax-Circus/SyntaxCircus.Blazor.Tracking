# SyntaxCircus.Blazor.Tracking

[![Build](https://github.com/Syntax-Circus/SyntaxCircus.Blazor.Tracking/actions/workflows/build.yml/badge.svg)](https://github.com/Syntax-Circus/SyntaxCircus.Blazor.Tracking/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/SyntaxCircus.Blazor.Tracking.svg)](https://www.nuget.org/packages/SyntaxCircus.Blazor.Tracking)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)

Consent-aware analytics building blocks for Blazor applications. The package bootstraps self-hosted Umami, direct GA4, or Google Tag Manager from configuration, and supplies an accessible consent UI that a host can completely style or replace.

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

All providers are disabled by default. An enabled provider must be complete or startup validation fails. Direct GA4 and GTM are alternative Google-provider modes: enable at most one.

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
    },
    "GoogleTagManager": {
      "Enabled": false,
      "ContainerId": "GTM-XXXXXXX"
    }
  }
}
```

| Setting | Purpose |
|---|---|
| `Umami:Enabled` | Enables cookie-free Umami tracking. |
| `Umami:ScriptUrl` | The URL of the self-hosted Umami tracker. |
| `Umami:WebsiteId` | The Umami website ID. |
| `Umami:RequireConsent` | Default `true`: Umami is held until analytics consent, and the banner is shown even when Umami is the only provider. Set `false` to load it on every visit as consent-exempt audience measurement, after reading [Umami and consent](#umami-and-consent). |
| `Umami:RespectDoNotTrack` | Default `true`: adds `data-do-not-track="true"` so Umami ignores browsers that send Do Not Track. |
| `GoogleAnalytics:Enabled` | Enables consent-gated GA4. |
| `GoogleAnalytics:MeasurementId` | GA4 measurement ID, normally supplied through deployment configuration. |
| `GoogleTagManager:Enabled` | Enables consent-gated Google Tag Manager. Mutually exclusive with `GoogleAnalytics:Enabled`. |
| `GoogleTagManager:ContainerId` | GTM web-container ID. |
| `Consent:PolicyVersion` | Invalidates a previous choice when privacy policy changes. |
| `Consent:CookieName` | Essential first-party preference cookie name. |
| `Consent:CookieLifetimeDays` | Preference-cookie lifetime, from 1 to 400 days. |
| `Consent:PrivacyPolicyUrl` | Optional link shown in the default banner. |

Startup validation requires a direct-GA4 `MeasurementId` in `G-…` format and a GTM `ContainerId` in `GTM-…` format. It rejects a configuration that enables both modes.

### Umami only

Enable Umami with a tracker you operate. By default it is gated like the Google providers: the script waits for analytics consent and the banner is shown. Because the integration sets no cookie and uses no browser storage, a host may instead opt in to a consent-exempt setup with `Umami:RequireConsent` set to `false`; read [Umami and consent](#umami-and-consent) first to check that fits your deployment.

```json
"Umami": {
  "Enabled": true,
  "ScriptUrl": "https://analytics.example.com/script.js",
  "WebsiteId": "b2a6af60-8ca1-4c92-a931-5d1d6ec9201d"
}
```

### Umami and consent

> This section describes how the package behaves and the conditions under which consent-exempt measurement is commonly accepted. It is not legal advice; confirm the position for the jurisdictions you serve.

**Default (`RequireConsent: true`).** The script is not requested until the visitor grants **analytics** consent. The banner is shown when Umami is enabled, even without a Google provider. Rejecting, or a policy-version change, keeps Umami off. Withdrawing consent stops Umami from starting on later page loads, but cannot unload a tracker already running in the current page; it takes effect on the next navigation or reload.

**Exempt (`RequireConsent: false`).** The tracker script loads on every page view regardless of any stored choice. If Umami is your only provider, no banner or settings link is rendered, and no consent cookie is written. Google providers, when also enabled, stay fully consent-gated and still show the banner. Only choose this after completing the checklist below.

**Why the exemption is commonly considered reasonable.** The EU ePrivacy rule that drives most consent banners concerns storing or reading information on the visitor's device. This integration sets no cookie, writes no local or session storage, and does no fingerprinting or cross-site identification. Umami does not store IP addresses; it derives a short-lived session hash from IP, user agent and a rotating salt. When you self-host it, no third party receives visitor data. Regulators such as the CNIL accept audience measurement without consent when it meets conditions like the ones in the checklist below.

**Checklist for relying on the exemption.** The package cannot enforce these; they are host responsibilities.

- Self-host Umami (or use a processor bound by a data-processing agreement) and do not combine the data with any other source.
- Use the data only for aggregate audience measurement, not advertising, profiling or individual tracking.
- Do not call `umami.identify()`, and do not send custom events or properties that contain personal data. The package never does either.
- Keep `RespectDoNotTrack` on (or offer another visible opt-out) so measurement is not forced on people who objected.
- Name Umami, what it collects, why, the legal basis (legitimate interest) and the retention period in your privacy notice, and say how visitors can opt out.
- Set a sensible retention period in Umami and secure its dashboard and database.
- Do not enable the Umami cross-domain or session-cookie features if your Umami version offers them.

**Reasons to keep the default `RequireConsent: true` instead.** Your audience is concentrated in a jurisdiction whose regulator does not recognise the exemption or applies it more strictly; your counsel prefers the conservative reading; you cannot meet the checklist; or you later add anything to Umami that identifies individuals.

**Verify:** with the default, confirm no request to the tracker host is made until consent is granted. With `RequireConsent: false`, load a page in a clean profile and confirm the Umami request is sent and no cookie or storage entry is created.

### Direct GA4

Enable GA4 only when the host is ready to request consent. The package uses Basic Consent Mode: it queues denied defaults locally, but does not load `gtag.js`, send a Google request, or set a GA cookie until analytics consent is given. Marketing consent controls `ad_storage`, `ad_user_data`, and `ad_personalization`; analytics consent controls `analytics_storage`.

```json
"GoogleAnalytics": {
  "Enabled": true,
  "MeasurementId": "G-XXXXXXXXXX"
}
```

Use deployment secrets/environment variables rather than committing production IDs when that is your team's policy:

```text
Tracking__GoogleAnalytics__Enabled=true
Tracking__GoogleAnalytics__MeasurementId=G-XXXXXXXXXX
```

### Google Tag Manager

Use GTM when your organization centrally manages Google, third-party, or custom tags. It is a separate mode: do not enable it alongside direct GA4 and do not add the same container manually elsewhere in the host.

```json
"GoogleTagManager": {
  "Enabled": true,
  "ContainerId": "GTM-XXXXXXX"
}
```

The package uses Basic Consent Mode for the container: it queues denied Google consent defaults locally, then loads `gtm.js` only after analytics or marketing consent is granted. A reject-all choice makes no Google request. Direct GA4 loads only after analytics consent.

After GTM starts, and on every later preference change, the package pushes this fixed `dataLayer` event:

```javascript
{
  event: "syntax_circus_consent_update",
  syntaxCircusConsent: { analytics: true, marketing: false }
}
```

The `syntax_circus_` prefix deliberately identifies the package and avoids collisions with host-defined GTM events. It is an integration hook, not visitor-facing data; a host only encounters it when it enables GTM. Configure every GTM tag and trigger to respect the relevant Google consent state and, where needed, the event above.

## Consent lifecycle and revocation

The package initializes Google Consent Mode with all four supported states denied. On a saved or changed choice it sends a consent update, then starts the configured provider only when that choice permits it. A policy-version change invalidates the stored choice and shows the banner again.

When a user withdraws consent, the package updates the loaded Google tag or container to denied and makes a best-effort removal of known first-party Google cookies on the current host and parent domains:

- Analytics: `_ga`, `_ga_*`, `_gid`, `_gat*`, and `_dc_gtm_*`.
- Marketing: `_gac_*` and `_gcl_*`.

It cannot unload a script already executing on the page, infer a cookie written on an unknown path/domain, or remove cookies created by third-party or custom GTM tags. GTM container owners must configure those tags and their cleanup policies themselves.

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
- Decide whether the default Umami consent exemption is right for your audience and disclose Umami in your privacy notice (see [Umami and consent](#umami-and-consent)).
- Keep the dashboard, database, backup policy, and provider credentials secure.
- Do not add direct GA4, the configured GTM container, tracking pixels, or vendor scripts outside `TrackingHead`, or they can bypass consent or duplicate page views.
- Govern GTM publishing carefully: a container can add third-party or custom scripts without a package release, and those tags must carry their own consent requirements and cookie-cleanup policy.
- Treat `syntax_circus_consent_update` and its `syntaxCircusConsent` payload as a stable, fixed package integration contract when configuring GTM triggers or variables.
- Test browser cookies and network traffic after every provider/configuration change.

## Validation

```bash
dotnet restore SyntaxCircus.Blazor.Tracking.slnx
dotnet build SyntaxCircus.Blazor.Tracking.slnx --configuration Release
pwsh tests/SyntaxCircus.Blazor.Tracking.BrowserTests/bin/Release/net10.0/playwright.ps1 install chromium
dotnet test SyntaxCircus.Blazor.Tracking.slnx --no-build --configuration Release
dotnet pack SyntaxCircus.Blazor.Tracking.slnx --no-build --configuration Release
```

Verify a Google deployment in browser developer tools: before an applicable choice there must be no request to `googletagmanager.com` and no Google cookie. After a choice, confirm only the consented provider runs; after revocation, confirm the provider receives denied consent and known Google cookies are removed.
