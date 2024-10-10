let nextId = 0;

export default class extends BlazorJSComponents.Component {
    attach() {
        this._id = nextId++;
        console.log(`${this._id}: attach()`);
    }

    setParameters(currentCount) {
        console.log(`${this._id}: setParameters(${currentCount})`);
    }

    getId() {
        return this._id;
    }

    dispose() {
        console.log(`${this._id}: dispose()`);
    }
}
