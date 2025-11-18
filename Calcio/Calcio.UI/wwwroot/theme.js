window.calcioTheme = (function () {
    const storageKey = 'calcio.theme';
    let dotNetRef = null;
    let mqlDark = null;
    let unsubscribeEnhancedLoad = null;

    function apply(pref) {
        let effective = pref;
        if (pref === 'System') {
            effective = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'Dark' : 'Light';
        }
        document.documentElement.setAttribute('data-bs-theme', effective.toLowerCase());
    }

    function readStored() {
        return localStorage.getItem(storageKey) || 'System';
    }

    function onMqChanged() {
        if (dotNetRef) {
            const mode = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'Dark' : 'Light';
            dotNetRef.invokeMethodAsync('SystemThemeChanged', mode);
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
            localStorage.setItem(storageKey, pref);
            apply(pref);
        },
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
export function dispose() {
    return window.calcioTheme.dispose();
}
