(function () {
    "use strict";

    const configElement = document.getElementById("syntax-circus-tracking-config");
    if (!configElement) return;

    const config = JSON.parse(configElement.textContent);
    const googleAnalyticsEnabled = Boolean(config.googleAnalytics && config.googleAnalytics.enabled);
    const googleTagManagerEnabled = Boolean(config.googleTagManager && config.googleTagManager.enabled);
    const consentRequired = googleAnalyticsEnabled || googleTagManagerEnabled;
    const maxAge = config.consent.cookieLifetimeDays * 86400;
    let googleAnalyticsStarted = false;
    let googleTagManagerStarted = false;

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
        if (document.querySelector(`script[src="${source}"]`)) return false;
        const script = document.createElement("script");
        script.src = source;
        script.async = true;
        Object.entries(attributes || {}).forEach(([name, value]) => script.setAttribute(name, value));
        document.head.appendChild(script);
        return true;
    }

    function startUmami() {
        if (config.umami && config.umami.enabled) {
            loadScript(config.umami.scriptUrl, { "data-website-id": config.umami.websiteId });
        }
    }

    function ensureGoogleDataLayer() {
        window.dataLayer = window.dataLayer || [];
        window.gtag = window.gtag || function () { window.dataLayer.push(arguments); };
    }

    function consentState(consent) {
        return {
            analytics_storage: consent.analytics ? "granted" : "denied",
            ad_storage: consent.marketing ? "granted" : "denied",
            ad_user_data: consent.marketing ? "granted" : "denied",
            ad_personalization: consent.marketing ? "granted" : "denied"
        };
    }

    function initializeGoogleConsent() {
        if (!consentRequired) return;
        ensureGoogleDataLayer();
        window.gtag("consent", "default", consentState({ analytics: false, marketing: false }));
    }

    function updateGoogleConsent(consent) {
        if (!consentRequired) return;
        ensureGoogleDataLayer();
        window.gtag("consent", "update", consentState(consent));
    }

    function cookieDomainVariants() {
        const hostname = window.location.hostname;
        if (!hostname || hostname === "localhost" || /^\d+(?:\.\d+){3}$/.test(hostname)) return [null];

        const domains = [null];
        const labels = hostname.split(".");
        for (let index = 0; index < labels.length - 1; index += 1) {
            domains.push(labels.slice(index).join("."));
        }

        return [...new Set(domains)];
    }

    function expireCookie(name) {
        cookieDomainVariants().forEach(domain => {
            const domainAttribute = domain ? `; Domain=${domain}` : "";
            document.cookie = `${name}=; Path=/; Max-Age=0; SameSite=Lax; Secure${domainAttribute}`;
        });
    }

    function deleteGoogleCookies(consent) {
        const analyticsCookie = /^_(?:ga(?:_|$)|gid$|gat(?:_|$)|dc_gtm_)/;
        const marketingCookie = /^_(?:gac_|gcl_)/;

        document.cookie.split("; ").forEach(row => {
            const separatorIndex = row.indexOf("=");
            const name = separatorIndex < 0 ? row : row.slice(0, separatorIndex);
            if ((!consent.analytics && analyticsCookie.test(name)) || (!consent.marketing && marketingCookie.test(name))) {
                expireCookie(name);
            }
        });
    }

    function startGoogleAnalytics(consent) {
        if (googleAnalyticsStarted || !googleAnalyticsEnabled || !consent.analytics) return;
        ensureGoogleDataLayer();
        window.gtag("js", new Date());
        window.gtag("config", config.googleAnalytics.measurementId);
        loadScript(`https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(config.googleAnalytics.measurementId)}`);
        googleAnalyticsStarted = true;
    }

    function startGoogleTagManager(consent) {
        if (googleTagManagerStarted || !googleTagManagerEnabled || (!consent.analytics && !consent.marketing)) return;
        ensureGoogleDataLayer();
        window.dataLayer.push({ "gtm.start": new Date().getTime(), event: "gtm.js" });
        loadScript(`https://www.googletagmanager.com/gtm.js?id=${encodeURIComponent(config.googleTagManager.containerId)}`);
        googleTagManagerStarted = true;
    }

    function publishGoogleTagManagerConsent(consent) {
        if (!googleTagManagerEnabled || !googleTagManagerStarted) return;
        window.dataLayer.push({
            event: "syntax_circus_consent_update",
            syntaxCircusConsent: {
                analytics: Boolean(consent.analytics),
                marketing: Boolean(consent.marketing)
            }
        });
    }

    function applyConsentAndStartProviders(consent) {
        updateGoogleConsent(consent);
        deleteGoogleCookies(consent);
        startGoogleAnalytics(consent);
        startGoogleTagManager(consent);
        publishGoogleTagManagerConsent(consent);
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
            applyConsentAndStartProviders(consent);
        }
    });

    initializeGoogleConsent();

    document.addEventListener("DOMContentLoaded", () => {
        const consent = readConsent();
        if (consentRequired && !consent) showBanner();
        if (consentRequired) document.querySelectorAll("[data-privacy-settings-link]").forEach(element => { element.hidden = false; });
        startUmami();
        if (consent) applyConsentAndStartProviders(consent);
    });
}());
