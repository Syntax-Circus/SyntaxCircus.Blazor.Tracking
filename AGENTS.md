# AGENTS.md

Read `README.md` first. It is the consumer-facing contract for this `net10.0` Razor component library.

## Purpose and boundary

`SyntaxCircus.Blazor.Tracking` provides configuration-driven Umami/GA4 bootstrapping and accessible consent markup. It is not legal advice, a consent-management platform certification, a visual design system, a server-side analytics collector, or a replacement for a host's privacy notice and deployment security.

The package may contain the minimal JavaScript required to read/save choices and load configured providers. It must not include a measurement ID, Umami website ID, provider secret, brand copy, CSS framework, host layout, geo-IP policy, or provider enabled by default.

## Public contract

- `AddSyntaxCircusTracking`, `TrackingOptions`, all nested options, and public component parameters are public API.
- Provider configuration is opt-in. An enabled Umami provider requires `ScriptUrl` and `WebsiteId`; an enabled GA4 provider requires `MeasurementId`.
- `TrackingHead` is rendered once in the host head. `ConsentBanner` and `ConsentSettings` are rendered in the host body.
- GA4 must never make a network request or write a cookie before valid analytics consent. Do not change Basic Consent Mode to advanced/cookieless pings without an explicit product and legal decision.
- Umami must remain independent of the consent choice and must not use cookies, local storage, fingerprinting, or a distinct-ID feature in the supplied integration.
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
dotnet test SyntaxCircus.Blazor.Tracking.slnx --no-build --configuration Release
dotnet pack SyntaxCircus.Blazor.Tracking.slnx --no-build --configuration Release
```

## Release conventions

GitVersion derives versions from repository history. Do not hand-edit package versions. The GitHub workflow publishes only from `main` through NuGet OIDC trusted publishing and tags the version it publishes. Keep `.gitignore`, `GitVersion.yml`, package metadata, README badges, and workflow conventions aligned with the other Syntax Circus packages.
