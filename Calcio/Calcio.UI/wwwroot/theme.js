window.calcioTheme = (function () {
    const storageKey = 'calcio.theme';
    let dotNetRef = null;
    let mqlDark = null;

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
        if (stored === 'System') {
            apply('System');
        }
    }

    return {
        init: function (ref) {
            dotNetRef = ref;
            const stored = readStored();
            apply(stored);
            mqlDark = window.matchMedia('(prefers-color-scheme: dark)');
            mqlDark.addEventListener('change', onMqChanged);
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
