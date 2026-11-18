// Темы DumaLens: переключение data-theme + localStorage + системная тема; заполнение слайдеров.
(function () {
    var root = document.documentElement;
    function apply(t) {
        root.setAttribute('data-theme', t);
        try { localStorage.setItem('dl-theme', t); } catch (e) { }
    }
    var init = 'light';
    try {
        init = localStorage.getItem('dl-theme')
            || (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    } catch (e) { }
    root.setAttribute('data-theme', init);
    window.dlTheme = {
        get: function () { return root.getAttribute('data-theme') || 'light'; },
        set: apply,
        toggle: function () { apply(root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark'); }
    };
    function paintSlider(s) {
        var v = s.value;
        s.style.background = 'linear-gradient(to right, var(--accent) ' + v + '%, var(--border) ' + v + '%)';
    }
    function formatPct(value) {
        var v = Math.round(parseFloat(value) * 10) / 10;
        if (isNaN(v)) { return value + '%'; }
        return v.toString().replace('.', ',') + '%';
    }
    function paintSliders() {
        var sliders = document.querySelectorAll('input[type=range].slider');
        for (var i = 0; i < sliders.length; i++) { paintSlider(sliders[i]); }
    }
    window.dlPaintSliders = paintSliders;
    document.addEventListener('DOMContentLoaded', function () {
        paintSliders();
        // Во время перетаскивания обновляем заполнение и текст процента клиентски (без серверного ре-рендера);
        // фиксация значения в Blazor — по @onchange (отпускание).
        document.addEventListener('input', function (e) {
            var s = e.target;
            if (s && s.matches && s.matches('input[type=range].slider')) {
                paintSlider(s);
                var label = s.parentElement ? s.parentElement.querySelector('.slider-val') : null;
                if (label) {
                    // Мутируем существующий текстовый узел (nodeValue), а не textContent: замена узла
                    // отсоединяет его от diff-дерева Blazor, и последующий ре-рендер (пресеты) не обновляет видимый текст.
                    var pct = formatPct(s.value);
                    if (label.firstChild) { label.firstChild.nodeValue = pct; } else { label.textContent = pct; }
                }
            }
        });
    });
})();