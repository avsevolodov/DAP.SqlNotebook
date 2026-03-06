(function () {
	'use strict';
	var MERMAID_CDN = 'https://cdnjs.cloudflare.com/ajax/libs/mermaid/11.4.0/mermaid.min.js';
	var loaded = false;
	var loadPromise = null;

	function loadMermaid() {
		if (typeof mermaid !== 'undefined') {
			loaded = true;
			return Promise.resolve();
		}
		if (loadPromise) return loadPromise;
		loadPromise = new Promise(function (resolve, reject) {
			var script = document.createElement('script');
			script.src = MERMAID_CDN;
			script.crossOrigin = 'anonymous';
			script.onload = function () {
				loaded = true;
				mermaid.initialize({ startOnLoad: false, theme: 'neutral' });
				resolve();
			};
			script.onerror = reject;
			document.head.appendChild(script);
		});
		return loadPromise;
	}

	function run(id) {
		var el = document.getElementById(id);
		if (!el) return Promise.resolve();
		var code = (el.textContent || el.innerText || '').trim();
		if (!code) return Promise.resolve();
		return loadMermaid().then(function () {
			return mermaid.run({ nodes: [el], suppressErrors: true });
		}).catch(function () {});
	}

	window.MermaidDiagram = { run: run };
	window.sqlNotebookMermaidRun = run;
})();
