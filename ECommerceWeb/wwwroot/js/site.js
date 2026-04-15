(function () {
  'use strict';

  document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.toast[data-toast-auto]').forEach(function (el) {
      try {
        var t = new bootstrap.Toast(el, {
          autohide: true,
          delay: parseInt(el.getAttribute('data-bs-delay') || '4000', 10)
        });
        t.show();
      } catch (e) { /* no bootstrap */ }
    });

    document.querySelectorAll('form[data-loading]').forEach(function (form) {
      form.addEventListener('submit', function () {
        var loader = document.getElementById('page-loader');
        if (loader) loader.classList.remove('d-none');
      });
    });

    initCartTotals();
  });

  function formatVnd(n) {
    return Math.round(n).toLocaleString('vi-VN') + ' đ';
  }

  function initCartTotals() {
    var table = document.querySelector('[data-cart-table]');
    if (!table) return;

    var inputs = table.querySelectorAll('[data-line-qty]');

    function recalc() {
      var sum = 0;
      table.querySelectorAll('[data-cart-line]').forEach(function (row) {
        var price = parseFloat(row.getAttribute('data-unit-price')) || 0;
        var qtyInput = row.querySelector('[data-line-qty]');
        var q = parseInt(qtyInput && qtyInput.value, 10) || 0;
        var line = price * q;
        sum += line;
        var out = row.querySelector('[data-line-total]');
        if (out) out.textContent = formatVnd(line);
      });
      document.querySelectorAll('[data-cart-grand]').forEach(function (el) {
        el.textContent = formatVnd(sum);
      });
    }

    inputs.forEach(function (inp) {
      inp.addEventListener('input', recalc);
      inp.addEventListener('change', recalc);
    });
    recalc();
  }
})();
