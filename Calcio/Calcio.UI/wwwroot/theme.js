window.calcioTheme = (function () {
    const storageKey = 'calcio.theme';
    let dotNetRef = null;
    let mqlDark = null;
    let unsubscribeEnhancedLoad = null;

    function computeEffective(pref) {
        let effective = pref;
        if (pref === 'System') {
            const matchesDark = (mqlDark && typeof mqlDark.matches === 'boolean')
                ? mqlDark.matches
                : window.matchMedia('(prefers-color-scheme: dark)').matches;
            effective = matchesDark ? 'Dark' : 'Light';
        }
        return effective.toLowerCase();
    }

    function apply(pref) {
        const effective = computeEffective(pref);
        document.documentElement.setAttribute('data-bs-theme', effective);
    }

    function readStored() {
        try {
            return localStorage.getItem(storageKey) || 'System';
        } catch {
            return 'System';
        }
    }

    function writeStored(pref) {
        try {
            localStorage.setItem(storageKey, pref);
        } catch {
            // ignore storage failures (e.g., private mode)
        }
    }

    function syncToStored() {
        const expected = computeEffective(readStored());
        const current = document.documentElement.getAttribute('data-bs-theme');
        if (current !== expected) {
            isSyncing = true;
            document.documentElement.setAttribute('data-bs-theme', expected);
            isSyncing = false;
        }
    }

    function attachObservers() {
        // Observe changes to the data-bs-theme attribute and ensure it stays in sync with storage
        if (themeObserver) {
            themeObserver.disconnect();
        }
        themeObserver = new MutationObserver(() => {
            if (isSyncing) {
                return;
            }
            syncToStored();
        });
        themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] });

        // Observe if the root element gets replaced (enhanced navigation), then re-attach and sync
        if (rootObserver) {
            rootObserver.disconnect();
        }
        rootObserver = new MutationObserver(() => {
            // Re-attach observers to the (potentially) new documentElement
            // Use a microtask to allow the DOM to settle
            queueMicrotask(() => {
                if (document && document.documentElement) {
                    attachObservers();
                    syncToStored();
                }
            });
        });
        // Observing the document for child list changes is sufficient to detect root swaps in enhanced nav
        rootObserver.observe(document, { childList: true, subtree: false });
    }

    function onMqChanged() {
        if (dotNetRef) {
            const matchesDark = (mqlDark && typeof mqlDark.matches === 'boolean')
                ? mqlDark.matches
                : window.matchMedia('(prefers-color-scheme: dark)').matches;
            const mode = matchesDark ? 'Dark' : 'Light';
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

            // Prepare media query before first apply to avoid creating multiple MediaQueryList instances
            mqlDark = window.matchMedia('(prefers-color-scheme: dark)');
            mqlDark.addEventListener('change', onMqChanged);

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
            writeStored(pref);
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

export function init(ref){
    return window.calcioTheme.init(ref);
}
export function setPreference(pref){
    return window.calcioTheme.setPreference(pref);
}
export function dispose(){
    return window.calcioTheme.dispose();
}
