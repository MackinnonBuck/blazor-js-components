import { Component } from './_content/BlazorJSComponents/component.mjs';
import Ajv from 'https://esm.sh/ajv@4.11.8';

export default class extends Component {
    setParameters({ validationErrorList }, schema) {
        const ajv = new Ajv({
            allErrors: true,
            coerceTypes: true,
        });

        this.validate = ajv.compile(schema);
        this.inputDataByName = {};
        this.validationErrorList = validationErrorList;
        this.form = this.validationErrorList.closest('form');
        this.setEventListener(this.form, 'submit', 'onSubmit');

        for (const element of this.form.elements) {
            if (!(element instanceof HTMLButtonElement) && element.hasAttribute('name')) {
                const name = element.getAttribute('name');
                const dotIndex = name.indexOf('.');
                if (dotIndex < 0) {
                    // Probably not a model property.
                    continue;
                }

                // Respect initial styling returned by the server.
                const modified = element.className.indexOf('modified') >= 0;

                const propertyName = name.substring(dotIndex + 1);
                this.inputDataByName[name] = {
                    element,
                    propertyName,
                    modified,
                };

                this.setEventListener(element, 'change', 'onChange');
            }
        }
    }

    onChange(ev) {
        const name = ev.target.getAttribute('name');
        this.inputDataByName[name].modified = true;
        this.applyValidation();
    }

    onSubmit(ev) {
        for (const inputData of Object.values(this.inputDataByName)) {
            inputData.modified = true;
        }

        if (!this.applyValidation(/* skipModifiedValidStylingOnSuccess */ true)) {
            ev.preventDefault();
        }
    }

    applyValidation(skipModifiedValidStylingOnSuccess) {
        for (const element of this.form.getElementsByClassName('validation-errors')) {
            // Clear any existing validation messages, including those rendered by
            // the server.
            element.replaceChildren();
        }

        const data = new FormData(this.form);
        const model = {};
        const remainingInputNamesByPropertyName = {};
        for (let [name, value] of data) {
            const inputData = this.inputDataByName[name];
            if (!inputData) {
                continue;
            }

            remainingInputNamesByPropertyName[inputData.propertyName] = name;

            if (value === '') {
                continue;
            }

            model[inputData.propertyName] = value;
        }

        const success = this.validate(model);
        if (!success) {
            for (const error of this.validate.errors) {
                let propertyName;
                let messagePrefix;
                if (error.dataPath) {
                    propertyName = error.dataPath.substring(1);
                    messagePrefix = propertyName + ' ';
                } else if (error.params.missingProperty) {
                    propertyName = error.params.missingProperty;
                    messagePrefix = 'Form '; // ...should have required property...
                } else {
                    console.log(`Could not determine property for error ${error}`);
                    continue;
                }

                const name = remainingInputNamesByPropertyName[propertyName];
                const inputData = this.inputDataByName[name];
                if (!inputData.modified) {
                    continue;
                }

                delete remainingInputNamesByPropertyName[propertyName];

                inputData.element.className = 'modified invalid';

                const li = document.createElement('li');
                li.className = "validation-message";
                li.innerText = messagePrefix + error.message;
                this.validationErrorList.appendChild(li);
            }
        }

        if (!(success && skipModifiedValidStylingOnSuccess)) {
            for (const inputName of Object.values(remainingInputNamesByPropertyName)) {
                const inputData = this.inputDataByName[inputName];
                if (inputData.modified) {
                    inputData.element.className = 'modified valid';
                }
            }
        }

        return success;
    }
}
