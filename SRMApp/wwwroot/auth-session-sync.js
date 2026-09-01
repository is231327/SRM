window.srmAuthSessionSync = (() => {
    let storageHandler = null;

    return {
        subscribe(dotNetReference) {
            if (storageHandler !== null) {
                window.removeEventListener("storage", storageHandler);
            }

            storageHandler = event => {
                if (event.storageArea === window.localStorage && event.key === "auth-session") {
                    dotNetReference.invokeMethodAsync("OnAuthSessionStorageChanged");
                }
            };

            window.addEventListener("storage", storageHandler);
        },

        unsubscribe() {
            if (storageHandler !== null) {
                window.removeEventListener("storage", storageHandler);
                storageHandler = null;
            }
        }
    };
})();
