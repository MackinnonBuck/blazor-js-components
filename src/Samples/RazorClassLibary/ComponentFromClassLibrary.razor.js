export default class extends BlazorJSComponents.Component {
    attach() {
        this._currentCount = 0;
    }

    setParameters({ button, count }) {
        this._countElement = count;
        this.setEventListener(button, 'click', () => {
            this._currentCount++;
            this.render();
        });
        this.render();
    }

    render() {
        this._countElement.innerText = `Current count: ${this._currentCount}`;
    }
}
