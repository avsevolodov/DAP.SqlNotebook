export function removeLoadingIndicator() {
    var progress = document.getElementById('loadingIndicator');
    progress.remove();
}

// Global helpers used from Blazor via JSInterop
if (!window.sqlNotebookDownloadCsv) {
    window.sqlNotebookDownloadCsv = function (path, body) {
        fetch(path, {
            method: "POST",
            body: JSON.stringify(body),
            headers: { "Content-Type": "application/json" },
            credentials: "include"
        })
            .then(function (r) {
                if (!r.ok) throw new Error("Export failed");
                return r.blob();
            })
            .then(function (blob) {
                var a = document.createElement("a");
                a.href = URL.createObjectURL(blob);
                a.download = "export.csv";
                a.click();
                URL.revokeObjectURL(a.href);
            })
            .catch(function (e) {
                console.error(e);
                alert((e && e.message) ? e.message : "Download failed");
            });
    };
}