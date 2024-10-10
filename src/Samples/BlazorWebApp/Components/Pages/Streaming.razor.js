export default class extends BlazorJSComponents.Component {
    attach() {
        this._setParametersCount = 0;
    }

    setParameters(elements) {
        this._setParametersCount++;
        console.log(`setParameters count: ${this._setParametersCount}`);

        for (const element of Object.values(elements)) {
            element.innerText = "JS content";
        }
    }
}
