
// ----------------------------------------------------------------------------
// Organization Details Page
// ----------------------------------------------------------------------------

function setupStripe() {
    var stripe = Stripe('pk_live_OyZdMh0UpYPCgR8ecp4Hxq9D'); // pk_test_TsCIFZTygn9DYAzEY3ElV2Ph
    var elements = stripe.elements();

    // Create an instance of the card Element.
    var card = elements.create('card');

    // Add an instance of the card Element into the `card-element` <div>.
    card.mount('#card-element');

    card.addEventListener('change', function (event) {
        var displayError = document.getElementById('card-errors');
        if (event.error) {
            displayError.textContent = event.error.message;
        } else {
            displayError.textContent = '';
        }
    });

    // Create a source or display an error when the form is submitted.
    var form = document.getElementById('payment-form');
    form.addEventListener('submit', function (event) {
        event.preventDefault();

        stripe.createSource(card).then(function (result) {
            if (result.error) {
                // Inform the customer that there was an error.
                var errorElement = document.getElementById('card-errors');
                errorElement.textContent = result.error.message;
            } else {
                // Send the source to your server.
                stripeSourceHandler(result.source);
            }
        });
    });
}

function stripeSourceHandler(source) {
    // Invoke the action on the Blazor page
    DotNet.invokeMethodAsync("Brizbee.Dashboard", 'InvokeUpdateSourceId', source.Id);
}

// ----------------------------------------------------------------------------
// Kiosk Pages
// ----------------------------------------------------------------------------

function getPlatformDetails() {
	// Built-in platform detection
	var browserName = platform.name; // 'Safari'
	var browserVersion = platform.version; // '5.1'
	var operatingSystemName = platform.os.family; // 'iOS'
	var operatingSystemVersion = platform.os.version + (platform.os.architecture === 64 ? ' 64-bit' : ''); // 5.0

	return {
		BrowserName: browserName,
		BrowserVersion: browserVersion,
		OperatingSystemName: operatingSystemName,
		OperatingSystemVersion: operatingSystemVersion
	};
}

// ----------------------------------------------------------------------------
// Any Page
// ----------------------------------------------------------------------------

function focusElement(id) {
    var elem = document.getElementById(id);
    elem.focus();
}
