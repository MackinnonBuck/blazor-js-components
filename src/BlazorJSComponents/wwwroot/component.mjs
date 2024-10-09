export class Component {
    setEventListener(element, type, listener, options) {
        if (typeof listener === 'string') {
            listener = this._resolveMethodListener(listener);
        }

        this._registeredEventListeners ??= new Map();
        let entriesForElement = this._registeredEventListeners.get(element);
        if (entriesForElement) {
            const existingEntry = entriesForElement[type];
            if (existingEntry) {
                element.removeEventListener(type, existingEntry.listener, existingEntry.options);
            }
        } else {
            entriesForElement = {};
            this._registeredEventListeners.set(element, entriesForElement);
        }

        entriesForElement[type] = { listener, options };
        element.addEventListener(type, listener, options);
    }

    removeEventListener(element, type) {
        if (!this._registeredEventListeners) {
            return;
        }

        const listenersForElement = this._registeredEventListeners.get(element);
        if (!listenersForElement) {
            return;
        }

        const entry = listenersForElement[type];
        if (!entry) {
            return;
        }

        element.removeEventListener(type, entry.listener, entry.options);
        delete listenersForElement[type];

        for (let _ in listenersForElement) {
            // This might seem strange, but it's an efficient way to
            // check if there are any properties in an object:
            // https://stackoverflow.com/a/59787784
            return;
        }

        // If there are no listeners left, remove the element from the map.
        this._registeredEventListeners.delete(element);
    }

    clearEventListeners() {
        if (!this._registeredEventListeners) {
            return;
        }

        for (const [element, entry] of this._registeredEventListeners) {
            for (const [type, { listener, options }] of Object.entries(entry)) {
                element.removeEventListener(type, listener, options);
            }
        }

        this._registeredEventListeners.clear();
        this._registeredEventListeners = null;
    }

    _resolveMethodListener(name) {
        this._resolvedMethodListeners ??= {};
        const cachedListener = this._resolvedMethodListeners[name];
        if (cachedListener) {
            return cachedListener;
        }

        const method = this[name];
        if (!method) {
            throw new Error(`The JS component has no method '${name}'`);
        }

        const listener = method.bind(this);
        this._resolvedMethodListeners[name] = listener;
        return listener;
    }

    _dispose() {
        this.dispose?.();
        this.clearEventListeners();
    }
}
