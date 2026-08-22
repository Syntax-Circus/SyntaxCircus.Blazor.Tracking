(function () {
    "use strict";

    const configElement = document.getElementById("syntax-circus-tracking-config");
    if (!configElement) return;

    const config = JSON.parse(configElement.textContent);
    const consentRequired = Boolean(config.googleAnalytics && config.googleAnalytics.enabled);
    const maxAge = config.consent.cookieLifetimeDays * 86400;
    let googleAnalyticsStarted = false;

    function readConsent() {
        const prefix = encodeURIComponent(config.consent.cookieName) + "=";
        const value = document.cookie.split("; ").find(row => row.startsWith(prefix));
        if (!value) return null;
        try {
            const consent = JSON.parse(decodeURIComponent(value.slice(prefix.length)));
            return consent.version === config.consent.policyVersion ? consent : null;
        } catch {
            return null;
        }
    }

    function saveConsent(consent) {
        const value = encodeURIComponent(JSON.stringify({ version: config.consent.policyVersion, analytics: Boolean(consent.analytics), marketing: Boolean(consent.marketing) }));
        document.cookie = `${encodeURIComponent(config.consent.cookieName)}=${value}; Path=/; Max-Age=${maxAge}; SameSite=Lax; Secure`;
    }

    function loadScript(source, attributes) {
        if (document.querySelector(`script[src="${source}"]`)) return;
        const script = document.createElement("script");
        script.src = source;
        script.defer = true;
        Object.entries(attributes || {}).forEach(([name, value]) => script.setAttribute(name, value));
        document.head.appendChild(script);
    }

    function startUmami() {
        if (config.umami && config.umami.enabled) {
            loadScript(config.umami.scriptUrl, { "data-website-id": config.umami.websiteId });
        }
    }

    function startGoogleAnalytics(consent) {
        if (googleAnalyticsStarted || !config.googleAnalytics || !config.googleAnalytics.enabled || !consent.analytics) return;
        window.dataLayer = window.dataLayer || [];
        window.gtag = window.gtag || function () { window.dataLayer.push(arguments); };
        window.gtag("consent", "default", {
            analytics_storage: "granted",
            ad_storage: consent.marketing ? "granted" : "denied",
            ad_user_data: consent.marketing ? "granted" : "denied",
            ad_personalization: consent.marketing ? "granted" : "denied"
        });
        window.gtag("js", new Date());
        window.gtag("config", config.googleAnalytics.measurementId);
        loadScript(`https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(config.googleAnalytics.measurementId)}`);
        googleAnalyticsStarted = true;
    }

    function startProviders(consent) {
        startUmami();
        startGoogleAnalytics(consent || {});
    }

    function showBanner() {
        document.querySelectorAll("[data-privacy-banner]").forEach(element => { element.hidden = false; });
    }

    function hideBanner() {
        document.querySelectorAll("[data-privacy-banner]").forEach(element => { element.hidden = true; });
    }

    function openSettings() {
        document.querySelectorAll("[data-privacy-banner]").forEach(banner => {
            banner.hidden = false;
            banner.querySelectorAll("[data-privacy-categories], [data-privacy-action='save-settings']").forEach(element => { element.hidden = false; });
            const consent = readConsent() || {};
            banner.querySelectorAll("[data-privacy-category]").forEach(input => { input.checked = Boolean(consent[input.dataset.privacyCategory]); });
        });
    }

    document.addEventListener("click", event => {
        const actionElement = event.target.closest("[data-privacy-action]");
        if (!actionElement) return;
        const action = actionElement.dataset.privacyAction;
        if (action === "open-settings") {
            openSettings();
            return;
        }
        if (action === "accept-all" || action === "reject-all" || action === "save-settings") {
            const banner = actionElement.closest("[data-privacy-banner]") || document.querySelector("[data-privacy-banner]");
            const consent = action === "accept-all" ? { analytics: true, marketing: true }
                : action === "reject-all" ? { analytics: false, marketing: false }
                : Object.fromEntries([...banner.querySelectorAll("[data-privacy-category]")].map(input => [input.dataset.privacyCategory, input.checked]));
            saveConsent(consent);
            hideBanner();
            startProviders(consent);
        }
    });

    document.addEventListener("DOMContentLoaded", () => {
        const consent = readConsent();
        if (consentRequired && !consent) showBanner();
        if (consentRequired) document.querySelectorAll("[data-privacy-settings-link]").forEach(element => { element.hidden = false; });
        startProviders(consent || {});
    });
}());
