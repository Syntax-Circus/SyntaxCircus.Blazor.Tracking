# AGENTS.md

Read `README.md` first. It is the consumer-facing contract for this `net10.0` Razor component library.

## Purpose and boundary

`SyntaxCircus.Blazor.Tracking` provides configuration-driven Umami, direct-GA4, and GTM bootstrapping plus accessible consent markup. It is not legal advice, a consent-management platform certification, a visual design system, a server-side analytics collector, or a replacement for a host's privacy notice and deployment security.

The package may contain the minimal JavaScript required to read/save choices and load configured providers. It must not include a measurement ID, Umami website ID, provider secret, brand copy, CSS framework, host layout, geo-IP policy, or provider enabled by default.

## Public contract

- `AddSyntaxCircusTracking`, `TrackingOptions`, all nested options, and public component parameters are public API.
- Provider configuration is opt-in. An enabled Umami provider requires `ScriptUrl` and `WebsiteId`; enabled direct GA4 requires a `G-` `MeasurementId`; enabled GTM requires a `GTM-` `ContainerId`; direct GA4 and GTM are mutually exclusive.
- `TrackingHead` is rendered once in the host head. `ConsentBanner` and `ConsentSettings` are rendered in the host body.
- GA4 must never make a network request or write a cookie before valid analytics consent. GTM must not load before analytics or marketing consent. Do not change Basic Consent Mode to advanced/cookieless pings without an explicit product and legal decision.
- Initialize Google Consent Mode with all four supported states denied; apply a consent update whenever a valid stored or newly saved choice is used, including revocation. Policy-version changes must invalidate stale choices.
- On revocation, best-effort delete known first-party Google analytics cookies (`_ga`, `_ga_*`, `_gid`, `_gat*`, `_dc_gtm_*`) and marketing cookies (`_gac_*`, `_gcl_*`) across the current host and parent domains. Do not claim to remove third-party/custom GTM cookies or unload an already executing provider script.
- GTM integrations must publish the fixed, vendor-prefixed `syntax_circus_consent_update` data-layer event with a `syntaxCircusConsent` payload after the container starts and on later preference changes. The prefix is intentional collision avoidance and is public contract; do not rename it or make it configurable without an explicit API decision.
- Umami is consent-gated by default (`Umami:RequireConsent` = true): it loads only after analytics consent and drives the banner. A host may opt in to a consent-exempt setup with `RequireConsent` false, in which case it loads on every visit and does not create the banner; that exemption is a legal-position choice documented in the README, so never make it the default without an explicit decision. `RespectDoNotTrack` (default true) sets `data-do-not-track`. In every mode the supplied integration must not use cookies, local storage, fingerprinting, or a distinct-ID feature. Keep the README "Umami and consent" caveats and checklist in step with any change here.
- `ConsentBanner` is CSS-framework-agnostic. `CssClass`, stable `data-privacy-*` hooks, and `RenderFragment` slots are consumer contract. `ChildContent` replaces all generated markup; documented action/category hooks must continue to work.
- Consent stores only the policy version and category choices in the essential preference cookie. Changing `PolicyVersion` invalidates old choices.

## Rendering and accessibility

- Default banner markup must remain semantic, keyboard-operable, and expose accept/reject choices with equivalent availability.
- Do not add Bootstrap, Tailwind, inline colors, a host-specific route, or marketing language.
- When changing default markup or slots, update README parameter/hook tables, examples, and bUnit tests in the same change.
- Custom content is host-owned; document every hook it must preserve to remain functional.

## Tests and documentation

Use xUnit v3, Shouldly, and bUnit. Add coverage for every public rendering behavior and options-validation rule. Browser-level provider/cookie checks belong in a consuming app or end-to-end test suite.

Update `README.md` and this file whenever public options, consent mapping, script-loading behavior, static assets, or package requirements change.

Run from the repository root:

```bash
dotnet restore SyntaxCircus.Blazor.Tracking.slnx
dotnet build SyntaxCircus.Blazor.Tracking.slnx --configuration Release
pwsh tests/SyntaxCircus.Blazor.Tracking.BrowserTests/bin/Release/net10.0/playwright.ps1 install chromium
dotnet test SyntaxCircus.Blazor.Tracking.slnx --no-build --configuration Release
dotnet pack SyntaxCircus.Blazor.Tracking.slnx --no-build --configuration Release
```

## Release conventions

GitVersion derives versions from repository history. Do not hand-edit package versions. The GitHub workflow publishes only from `main` through NuGet OIDC trusted publishing and tags the version it publishes. Keep `.gitignore`, `GitVersion.yml`, package metadata, README badges, and workflow conventions aligned with the other Syntax Circus packages.
