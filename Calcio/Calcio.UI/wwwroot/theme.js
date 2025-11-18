window.calcioTheme = (function () {
    const storageKey = 'calcio.theme';
    let dotNetRef = null;
    let mqlDark = null;
    let unsubscribeEnhancedLoad = null;

    function isValidPreference(value) {
        return value === 'Light' || value === 'Dark' || value === 'System';
    }

    function normalizePreference(value) {
        if (value == null) {
            return 'System';
        }
        const v = String(value).trim().toLowerCase();
        if (v === 'light') {
            return 'Light';
        }
        if (v === 'dark') {
            return 'Dark';
        }
        if (v === 'system' || v === 'auto') {
            return 'System';
        }
        return 'System';
    }

    function apply(pref) {
        let effective = pref;
        if (pref === 'System') {
            effective = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'Dark' : 'Light';
        }
        document.documentElement.setAttribute('data-bs-theme', effective.toLowerCase());
    }

    function readStored() {
        const raw = localStorage.getItem(storageKey);
        const normalized = normalizePreference(raw);
        // Guard: ensure persisted value is one of the expected tokens.
        if (!isValidPreference(raw)) {
            try { localStorage.setItem(storageKey, normalized); } catch { /* no-op */ }
        }
        return normalized;
    }

    function onMqChanged() {
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('SystemThemeChanged');
        }
        const stored = readStored();
        // If following the system theme, ensure the applied theme updates too.
        if (stored === 'System') {
            apply('System');
        }
    }

    function onEnhancedLoad() {
        // Enhanced navigation can revert DOM mutations not present in SSR output.
        // Re-apply the current preference after an enhanced page update.
        const stored = readStored();
        apply(stored);
    }

    function getEffectiveTheme() {
        const pref = readStored();
        const effective = pref === 'System'
            ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'Dark' : 'Light')
            : pref;
        return { preference: pref, effective };
    }

    return {
        init: function (ref) {
            dotNetRef = ref;
            const stored = readStored();
            apply(stored);

            // Watch system theme changes when in Auto (System) mode
            mqlDark = window.matchMedia('(prefers-color-scheme: dark)');
            mqlDark.addEventListener('change', onMqChanged);

            // Re-apply theme after Blazor enhanced navigations patch the DOM
            try {
                if (window.Blazor && typeof Blazor.addEventListener === 'function') {
                    // Store unsubscribe (if supported) to detach on dispose
                    unsubscribeEnhancedLoad = Blazor.addEventListener('enhancedload', onEnhancedLoad);
                } else if (typeof document !== 'undefined' && typeof document.addEventListener === 'function') {
                    // Fallback to listen for the DOM event if exposed
                    document.addEventListener('enhancedload', onEnhancedLoad);
                    unsubscribeEnhancedLoad = () => document.removeEventListener('enhancedload', onEnhancedLoad);
                }
            } catch { /* no-op */ }

            return stored;
        },
        setPreference: function (pref) {
            const normalized = normalizePreference(pref);
            try { localStorage.setItem(storageKey, normalized); } catch { /* no-op */ }
            apply(normalized);
        },
        getEffectiveTheme: function () { return getEffectiveTheme(); },
        dispose: function () {
            if (mqlDark) {
                mqlDark.removeEventListener('change', onMqChanged);
                mqlDark = null;
            }
            try {
                if (typeof unsubscribeEnhancedLoad === 'function') {
                    unsubscribeEnhancedLoad();
                }
            } catch { /* no-op */ }
            unsubscribeEnhancedLoad = null;
            dotNetRef = null;
        }
    };
})();

export function init(ref) {
    return window.calcioTheme.init(ref);
}
export function setPreference(pref) {
    return window.calcioTheme.setPreference(pref);
}
export function getEffectiveTheme() {
    return window.calcioTheme.getEffectiveTheme();
}
export function dispose() {
    return window.calcioTheme.dispose();
}
