(() => {
    const editor = document.getElementById("description-editor");
    const source = document.getElementById("description-source");
    if (!editor || !source) return;

    editor.innerHTML = source.value;
    editor.hidden = false;
    source.hidden = true;
    document.querySelectorAll("[data-editor-command]").forEach(button => {
        button.addEventListener("mousedown", event => event.preventDefault());
        button.addEventListener("click", () => {
            editor.focus();
            const command = button.dataset.editorCommand;
            document.execCommand(command, false, button.dataset.editorValue || null);
            source.value = editor.innerHTML;
        });
    });
    editor.addEventListener("input", () => { source.value = editor.innerHTML; });
    editor.closest("form").addEventListener("submit", () => { source.value = editor.innerHTML; });
})();
