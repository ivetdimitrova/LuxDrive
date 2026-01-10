document.getElementById('payment-form').addEventListener('submit', function (e) {
    const btn = document.getElementById('pay-btn');
    btn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...';
    btn.style.opacity = '0.7';
    btn.style.pointerEvents = 'none';
});

document.getElementById('cardNumber').addEventListener('input', function (e) {
    let value = e.target.value.replace(/\D/g, ''); 

    let formattedValue = '';
    for (let i = 0; i < value.length; i++) {
        if (i > 0 && i % 4 === 0) {
            formattedValue += ' ';
        }
        formattedValue += value[i];
    }
    e.target.value = formattedValue;
});

document.getElementById('expiry').addEventListener('input', function (e) {
    let value = e.target.value.replace(/\D/g, ''); 

    if (value.length >= 2) {
        e.target.value = value.substring(0, 2) + '/' + value.substring(2, 4);
    } else {
        e.target.value = value;
    }

    if (e.inputType === 'deleteContentBackward' && this.value.length === 2) {
        this.value = this.value.substring(0, 1);
    }
});

document.getElementById('cvc').addEventListener('input', function (e) {
    e.target.value = e.target.value.replace(/\D/g, '').substring(0, 3);
});