window.ordoEditor = {
    getCursor: (elementId) => {
        const ta = document.getElementById(elementId);
        if (!ta) return { start: 0, end: 0 };
        return { start: ta.selectionStart ?? 0, end: ta.selectionEnd ?? 0 };
    },

    setCursor: (elementId, position) => {
        const ta = document.getElementById(elementId);
        if (!ta) return;
        ta.focus();
        ta.selectionStart = ta.selectionEnd = position;
    },

    attachDropZone: (textareaId, fileInputId) => {
        const ta = document.getElementById(textareaId);
        const fi = document.getElementById(fileInputId);
        if (!ta || !fi) return;
        if (ta.dataset.ordoDropAttached === "1") return;
        ta.dataset.ordoDropAttached = "1";

        const isFileDrag = (e) =>
            e.dataTransfer && Array.from(e.dataTransfer.types || []).includes("Files");

        ta.addEventListener("dragover", (e) => {
            if (!isFileDrag(e)) return;
            e.preventDefault();
            e.dataTransfer.dropEffect = "copy";
        });

        ta.addEventListener("drop", (e) => {
            if (!isFileDrag(e)) return;
            e.preventDefault();

            const file = e.dataTransfer.files && e.dataTransfer.files[0];
            if (!file || !file.type.startsWith("image/")) return;

            // Pipe the dropped file into the hidden Blazor InputFile so its OnChange fires.
            const dt = new DataTransfer();
            dt.items.add(file);
            fi.files = dt.files;
            fi.dispatchEvent(new Event("change", { bubbles: true }));
        });

        // Tab inserts a tab character instead of moving focus out of the textarea.
        // Shift+Tab is left alone so users can still tab backwards out of the field.
        ta.addEventListener("keydown", (e) => {
            if (e.key !== "Tab" || e.shiftKey) return;
            e.preventDefault();
            const start = ta.selectionStart;
            const end = ta.selectionEnd;
            const before = ta.value.substring(0, start);
            const after = ta.value.substring(end);
            ta.value = before + "\t" + after;
            ta.selectionStart = ta.selectionEnd = start + 1;
            // MudTextField binds via the input event with debounce; dispatching it
            // here keeps the C# side in sync.
            ta.dispatchEvent(new Event("input", { bubbles: true }));
        });
    }
};
