# BlazorJSComponents

This library enables the use of:
* Any JavaScript file
* In any Blazor component
* Using any render mode (including static server rendering!)

Here are some of the main features:
* Fetching/importing JavaScript dynamically
* Optionally inferring the script path from a collocated JavaScript file
* Defining ["JS components"](#javascript-components)

## Getting started

Start by installing the package from NuGet:

```sh
dotnet add package BlazorJSComponents --prerelease
```

Next, in your Blazor app's `Program.cs`, add the required services:

```csharp
builder.Services.AddJSComponents();
```

Then, in your `_Imports.razor` file, add a `using` for `BlazorJSComponents`:

```razor
@using BlazorJSComponents
```

If you haven't already, add some JavaScript to your app to be dynamically loaded. For example, add this `hello-world.js` script to your app's `wwwroot` folder:

```js
console.log('Hello, world!');
```

Finally, render a `JS` component from the Blazor component requiring your script:

```razor
<JS Src="./hello-world.js" />
```

When the `JS` component first renders, the referenced script will get dynamically loaded into the document, and `"Hello, world!"` will be logged to the browser console!

> [!NOTE]
> You might notice that `"Hello, world!"` only gets logged once per full page load. This is by design. To define JavaScript that runs in sync with Blazor's lifecycle methods, see [JS Components](#js-components).

## Collocated JS discovery

> [!NOTE]
> See the [official Blazor docs](https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/location-of-javascript?view=aspnetcore-8.0#load-a-script-from-an-external-javascript-file-js-collocated-with-a-component) for information about JS collocation.

When a component has a collocated JS file, it can be discovered automatically without having to hard-code a value for the `Src` parameter. To enable this:

1. Add the `[DiscoverCollocatedJS]` attribute in the component's `.razor` file
2. Replace the `Src` parameter with the `For` parameter, passing `this` as the argument

For example:

```razor
@attribute [DiscoverCollocatedJS]

<JS For="this" />
```

> [!IMPORTANT]
> The `[DiscoverCollocatedJS]` attribute _must_ be placed in the `.razor` file associated with the type of the component passed as the `For` parameter. The `[DiscoverCollocatedJS]` attribute _cannot_ be placed in an `_Imports.razor` file, and it _cannot_ be placed in a code-behind (`.razor.cs`) file.

## JS Components

In addition to dynamically loading JavaScript, `BlazorJSComponents` allows for writing "JS components", which are like Blazor components with a JavaScript implementation. JS components enable the following functionality:

* Implementing Blazor lifecycle events in JavaScript
* Associating JavaScript state with a Blazor component
  * Can be optionally persisted between enhanced navigations and in the transition to interactivity
* Managing JS event listeners on elements rendered from Blazor
* Passing arguments from .NET to JS, including:
  * Element references
  * Complex objects
* Invoking JS component methods from .NET

### Anatomy of a JS component

Here's how a typical JS component might look. Note that all callbacks are optional, but JS components must extend the `BlazorJSComponents.Component` base class.

```js
// Module-level state can be declared here.

export default class extends BlazorJSComponents.Component {
  attach(blazor) {
    // Called when the JS component first gets added to the page.
    // You can use this to initialize JS component state and
    // call methods on the provided 'blazor' object.
    // For example:
    // blazor.addEventListener('enhancedload', ...);
  }

  setParameters(...args) {
    // Called when the parent component re-renders.
    // You can use this to add event listeners to rendered elements
    // or modify content on the page.
  }

  dispose() {
    // Called after the parent component gets disposed.
    // You can use this to perform additional cleanup logic.
  }
}
```

### Basic usage example

Let's make an interactive counter! Start by adding a file called `Counter.razor` (if it doesn't already exist), and give it the following content:

```razor
@page "/counter"

<button>Click me!</button>
<span>Current count: 0</span>
```

Now, let's define some JS logic to increment the count when the button gets clicked. Add a `Counter.razor.js` file in the same directory as `Counter.razor`:

```js
export default class extends BlazorJSComponents.Component {
  // Called when the component gets added to the page.
  attach() {
    this.currentCount = 0;
  }

  // Called when the parent component re-renders.
  setParameters(incrementAmount, { button, label }) {
    this.label = label;
    this.setEventListener(button, 'click', () => {
      this.currentCount += incrementAmount;
      this.render();
    });
    this.render();
  }

  // A helper to apply changes to DOM content.
  // Not called automatically.
  render() {
    this.label.innerText = `Current count: ${this.currentCount}`;
  }
}
```

Here, we define a JS component that maintains its own state (the `currentCount`), and updates the page to reflect it. It expects multiple arguments to be provided from .NET:
* The increment amount
* Rendered elements:
  * The button to be clicked
  * The label displaying the increment amount

> [!NOTE]
> The above `setParameters()` implementation calls `this.setEventListener(button, ...)` instead of `button.addEventListener(...)`. The `setEventListener()` method, which is defined in the `BlazorJSComponents.Component` base class, has the following advantages:
> 1. It ensures that the component only has one listener for a given element and event. Calling `setEventListener()` multiple times on same element/event combination overwrites the previous event listener.
> 2. Upon component disposal, the event listener is automatically removed.

Let's update the Blazor component to render the JS component and pass arguments to it:

```razor
@page "/counter"
@attribute [DiscoverCollocatedJS]
@inject JSElementReferenceScope Refs

<button data-ref="@Refs["button"]">Click me!</button>
<span data-ref="@Refs["label"]">Current count: 0</span>
<JS For="this" Args="[IncrementAmount, Refs]" />

@code {
  const int IncrementAmount = 7;
}
```

Here are the notable changes:
* We inject a `JSElementReferenceScope` into the component to define element references scoped to the current component. This ensures that if there are multiple instances of our component at a time, each JS component receives element references for its correct parent Blazor component.
* Element references are named via the `data-ref` attribute. We use the indexer on the `JSElementReferenceScope` to specify the name of the property in JavaScript representing the element.
* The `Args` parameter lets us specify the list of arguments to pass to the JS component. In this case, we pass the `IncrementAmount` as the first argument, and the `JSElementReferenceScope` as the 2nd argument.

Now we can run the app, navigate to the `/counter` page, and increment the counter!

> [!TIP]
> If you make the Blazor component interactive, you'll notice that the JS counter continues to work, even if the Blazor component re-renders. You can even add a .NET counter right next to the JS component, and they'll both work. Pretty nifty!

## Advanced scenarios

This section contains some additional details relevant to advanced users.

### Persisting JS component state

By default, JS components get reinitialized when their parent component statically re-renders or becomes interactive. To persist JS component state between enhanced page updates or in the transition to interactivity, specify a unique `Key` parameter:

```razor
<JS For="this" Key="MyUniqueKey" />
```

Specifying a `Key` allows the library to map statically-rendered JS components to existing JS components, even if the content on the page changes dramatically between renders.

### Invoking JS component methods from .NET

In interactive scenarios, you can invoke methods on JS components. Here's an example of how this can be done:

```razor
@attribute [DiscoverCollocatedJS]
@rendermode InteractiveServer

<JS For="this" @ref="js" />

@code {
  JS? js;

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender)
    {
      var result = await js!.InvokeVoidAsync<string>("someMethod", "someArg");
    }
  }
}
```

### Alternative method for loading the `Component` base type

By default, the library defines a global `BlazorJSComponents.Component` type on the `window` object so it can be accessed anywhere. An alternative is to import the `Component` type directly:

```js
import { Component } from '/_content/BlazorJSComponents/component.mjs';

export default class extends Component {
  // ...
}
```

If you want to disable the default behavior that sets `window.BlazorJSComponents.Component`, you can do so by providing startup options to `Blazor.start()`:

```html
<script src="_framework/blazor.web.js" autostart="false"></script>
<script>
  Blazor.start({
    jsComponents: {
      disableGlobalProperties: true,
    },
  });
</script>
```
