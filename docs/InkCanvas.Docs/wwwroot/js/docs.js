// Tldraw.Blazor Documentation JS
window.TldrawDocs = {
    // Copy to clipboard
    copyToClipboard: function (text) {
        navigator.clipboard.writeText(text).catch(function () {
            // Fallback
            var ta = document.createElement('textarea');
            ta.value = text;
            document.body.appendChild(ta);
            ta.select();
            document.execCommand('copy');
            document.body.removeChild(ta);
        });
    },

    // Highlight code block (called from Blazor)
    highlightBlock: function (element) {
        if (typeof Prism !== 'undefined' && element) {
            Prism.highlightElement(element);
        }
    },

    // Init copy buttons
    initCopyButtons: function () {
        // Already handled by Blazor component
    }
};

// Theme toggle
function toggleDocsTheme() {
    var html = document.documentElement;
    var current = html.getAttribute('data-theme');
    var next = current === 'dark' ? 'light' : 'dark';
    html.setAttribute('data-theme', next);
    localStorage.setItem('tldraw-docs-theme', next);
}

// Load saved theme
(function () {
    var saved = localStorage.getItem('tldraw-docs-theme');
    if (saved) document.documentElement.setAttribute('data-theme', saved);
})();
