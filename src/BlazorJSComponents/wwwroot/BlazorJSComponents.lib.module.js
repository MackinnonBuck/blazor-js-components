import { Component } from '/_content/BlazorJSComponents/component.mjs';

let nextJSComponentId = 1;
const jsComponentsById = {};
const jsComponentIdsByKey = {};
const jsComponentTypesBySrc = {};

//const jsElementReferenceCollectionKey = "__jsElemRefCollectionId";
const jsElementReferenceScopeKey = "__jsScope";

async function importJSComponent(src) {
    if (src.startsWith("./")) {
        src = new URL(src.substring(2), document.baseURI).toString();
    }

    const { default: TComponent } = await import(src);
    return TComponent?.prototype instanceof Component
        ? TComponent
        : null;
}

export function beforeWebStart(options) {
    if (!options.blazorJSComponents?.noGlobalProperties) {
        // For convenience, allow the JS component type to be accessed globally.
        globalThis.BlazorJSComponents = {
            Component,
        };
    }
}

export function afterWebStarted(blazor) {
    async function getOrCreateJSComponent(instanceId, src, key) {
        instanceId = instanceId || jsComponentIdsByKey[key];
        const entry = jsComponentsById[instanceId];
        if (entry) {
            // The instance exists, so we'll attempt to return it.
            const { key: oldKey, src: oldSrc } = entry;
            if (key && oldKey === key && oldSrc === src) {
                // There's no ambiguity in which instance we're interested in,
                // so return the existing instance.
                delete entry.pendingDisposal;
                return instanceId;
            } else {
                disposeJSComponent(instanceId);
            }
        }

        let TComponent = jsComponentTypesBySrc[src];
        if (TComponent === undefined) {
            // The module has not been imported, so we'll do that now.
            TComponent = await importJSComponent(src);
            jsComponentTypesBySrc[src] = TComponent;
        }

        if (TComponent === null) {
            // Not a JS component module.
            return 0;
        }

        const newInstance = new TComponent();
        const newInstanceId = nextJSComponentId++;

        if (key !== null && key !== undefined) {
            jsComponentIdsByKey[key] = newInstanceId;
        }

        jsComponentsById[newInstanceId] = {
            key,
            src,
            TComponent,
            instance: newInstance,
        };

        newInstance.attach?.(blazor);

        return newInstanceId;
    }

    function disposeJSComponent(instanceId) {
        const entry = jsComponentsById[instanceId];
        if (!entry) {
            // Probably already disposed - do nothing.
            return;
        }

        entry.instance._dispose();
        delete jsComponentsById[instanceId];
        delete jsComponentIdsByKey[entry.key];
    }

    function getJSComponentInstance(instanceId) {
        const entry = jsComponentsById[instanceId];
        if (!entry) {
            throw new Error(`Could not find JS component with ID ${instanceId}`);
        }
        return entry.instance;
    }

    function setJSComponentParameters(instanceId, args) {
        const instance = getJSComponentInstance(instanceId);
        instance.setParameters?.(...(args || []));
    }

    function invokeJSComponentMethod(instanceId, identifier, args) {
        const instance = getJSComponentInstance(instanceId);
        const method = instance[identifier];
        if (!method) {
            throw new Error(`The JS component does not define method with name '${identifier}'`);
        }
        return method.apply(instance, args);
    }

    function reviveJSComponentArgs(key, value) {
        if (value && typeof value === "object" && value.hasOwnProperty(jsElementReferenceScopeKey)) {
            const collectionId = value[jsElementReferenceScopeKey];
            const elements = document.querySelectorAll(`[data-ref|="${collectionId}"]`);
            const result = {};
            for (const element of elements) {
                const attributeValue = element.getAttribute('data-ref');
                const startIndex = attributeValue.indexOf('-') + 1;
                const elementRefId = attributeValue.substring(startIndex);
                result[elementRefId] = element;
            }

            return result;
        }

        return value;
    }

    globalThis.__blazorScript = {
        getOrCreateJSComponent,
        setJSComponentParameters,
        invokeJSComponentMethod,
        disposeJSComponent,
    };

    globalThis.DotNet.attachReviver(reviveJSComponentArgs);

    let isNavigating = false;
    blazor.addEventListener('enhancednavigationstart', () => {
        isNavigating = true;
    });
    blazor.addEventListener('enhancednavigationend', () => {
        isNavigating = false;
    });

    customElements.define('bl-script', class extends HTMLElement {
        static observedAttributes = ['inst'];

        async attributeChangedCallback(name, oldValue, newValue) {
            if (name !== 'inst') {
                return;
            }

            const src = this.getAttribute('src');
            const key = this.getAttribute('key');

            if (!src) {
                throw new Error("Expected the 'src' attribute to be defined.");
            }

            this._instanceId = await getOrCreateJSComponent(this._instanceId, src, key);

            if (this._instanceId) {
                let args;
                const argsElement = document.getElementById(`bl-args-${newValue}`);
                if (argsElement) {
                    args = JSON.parse(argsElement.textContent, reviveJSComponentArgs);
                    argsElement.textContent = ''; // Clean up the DOM a bit.
                } else {
                    args = [];
                }
                setJSComponentParameters(this._instanceId, args);
            }
        }

        disconnectedCallback() {
            if (!this._instanceId) {
                return;
            }

            const key = this.getAttribute('key');
            const mayBeInteractive = this.hasAttribute('int');

            if (!isNavigating && key && mayBeInteractive) {
                const entry = jsComponentsById[this._instanceId];
                entry.pendingDisposal = true;

                setTimeout(() => {
                    const entry = jsComponentsById[this._instanceId];
                    if (entry?.pendingDisposal) {
                        disposeJSComponent(this._instanceId);
                    }
                }, 3000);
            } else {
                disposeJSComponent(this._instanceId);
            }
        }
    });
}