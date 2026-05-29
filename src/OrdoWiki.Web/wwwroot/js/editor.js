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

    // Wraps the current selection with prefix/suffix. If nothing is selected,
    // inserts `prefix + placeholder + suffix` and selects the placeholder so
    // the user can start typing to replace it.
    wrapSelection: (elementId, prefix, suffix, placeholder) => {
        const ta = document.getElementById(elementId);
        if (!ta) return;
        const start = ta.selectionStart ?? 0;
        const end = ta.selectionEnd ?? 0;
        const selected = ta.value.substring(start, end);
        const before = ta.value.substring(0, start);
        const after = ta.value.substring(end);
        const inner = selected.length > 0 ? selected : (placeholder ?? "");
        ta.value = before + prefix + inner + suffix + after;
        ta.focus();
        if (selected.length > 0) {
            ta.selectionStart = start + prefix.length;
            ta.selectionEnd = end + prefix.length;
        } else {
            ta.selectionStart = start + prefix.length;
            ta.selectionEnd = start + prefix.length + inner.length;
        }
        ta.dispatchEvent(new Event("input", { bubbles: true }));
    },

    // Adds a line prefix (e.g. "- ", "> ") to every line touched by the
    // selection. When `numbered` is true, the prefix is "1. ", "2. ", ...
    prefixLines: (elementId, prefix, numbered) => {
        const ta = document.getElementById(elementId);
        if (!ta) return;
        const start = ta.selectionStart ?? 0;
        const end = ta.selectionEnd ?? 0;
        const lineStart = ta.value.lastIndexOf("\n", start - 1) + 1;
        let lineEnd = ta.value.indexOf("\n", end);
        if (lineEnd === -1) lineEnd = ta.value.length;
        const block = ta.value.substring(lineStart, lineEnd);
        const lines = block.length === 0 ? [""] : block.split("\n");
        const newLines = lines.map((l, i) => (numbered ? `${i + 1}. ` : prefix) + l);
        const newBlock = newLines.join("\n");
        const firstPrefixLen = numbered ? "1. ".length : prefix.length;
        ta.value = ta.value.substring(0, lineStart) + newBlock + ta.value.substring(lineEnd);
        ta.focus();
        ta.selectionStart = start + firstPrefixLen;
        ta.selectionEnd = end + (newBlock.length - block.length);
        ta.dispatchEvent(new Event("input", { bubbles: true }));
    },

    // Replaces any existing heading prefix on the current line with the given
    // one (e.g. "## "). Idempotent — clicking H2 twice doesn't keep growing #s.
    setHeading: (elementId, prefix) => {
        const ta = document.getElementById(elementId);
        if (!ta) return;
        const start = ta.selectionStart ?? 0;
        const end = ta.selectionEnd ?? 0;
        const lineStart = ta.value.lastIndexOf("\n", start - 1) + 1;
        let lineEnd = ta.value.indexOf("\n", end);
        if (lineEnd === -1) lineEnd = ta.value.length;
        const line = ta.value.substring(lineStart, lineEnd);
        const stripped = line.replace(/^(#{1,6}\s+)/, "");
        const newLine = prefix + stripped;
        ta.value = ta.value.substring(0, lineStart) + newLine + ta.value.substring(lineEnd);
        ta.focus();
        const delta = newLine.length - line.length;
        ta.selectionStart = start + delta;
        ta.selectionEnd = end + delta;
        ta.dispatchEvent(new Event("input", { bubbles: true }));
    },

    // Inserts a block of text, padding with blank lines so the result is
    // separated from surrounding paragraphs. Used for tables, code blocks,
    // horizontal rules, etc.
    insertBlock: (elementId, text) => {
        const ta = document.getElementById(elementId);
        if (!ta) return;
        const start = ta.selectionStart ?? 0;
        const end = ta.selectionEnd ?? 0;
        const before = ta.value.substring(0, start);
        const after = ta.value.substring(end);

        let leading = "";
        if (before.length > 0 && !before.endsWith("\n\n")) {
            leading = before.endsWith("\n") ? "\n" : "\n\n";
        }
        let trailing = "";
        if (after.length === 0) {
            trailing = "\n";
        } else if (!after.startsWith("\n\n")) {
            trailing = after.startsWith("\n") ? "\n" : "\n\n";
        }

        const insert = leading + text + trailing;
        ta.value = before + insert + after;
        ta.focus();
        const cursor = before.length + leading.length + text.length;
        ta.selectionStart = ta.selectionEnd = cursor;
        ta.dispatchEvent(new Event("input", { bubbles: true }));
    },

    // Inserts a link. If text is selected, it becomes the link label and the
    // url placeholder gets selected so the user can paste/type the URL. If
    // nothing is selected, the label placeholder gets selected instead.
    insertLink: (elementId, labelPlaceholder, urlPlaceholder) => {
        const ta = document.getElementById(elementId);
        if (!ta) return;
        const start = ta.selectionStart ?? 0;
        const end = ta.selectionEnd ?? 0;
        const selected = ta.value.substring(start, end);
        const before = ta.value.substring(0, start);
        const after = ta.value.substring(end);
        const label = selected.length > 0 ? selected : labelPlaceholder;
        const url = urlPlaceholder;
        const insert = `[${label}](${url})`;
        ta.value = before + insert + after;
        ta.focus();
        if (selected.length > 0) {
            // Select the URL placeholder so the user can paste over it.
            const urlStart = before.length + 1 + label.length + 2;
            ta.selectionStart = urlStart;
            ta.selectionEnd = urlStart + url.length;
        } else {
            // Select the label placeholder first.
            const labelStart = before.length + 1;
            ta.selectionStart = labelStart;
            ta.selectionEnd = labelStart + label.length;
        }
        ta.dispatchEvent(new Event("input", { bubbles: true }));
    },

    // Inserts a footnote reference at the cursor and appends `[^N]: ...` to
    // the end of the document. Picks the next free `N` by scanning for any
    // existing `[^N]:` definitions.
    insertFootnote: (elementId, defaultText) => {
        const ta = document.getElementById(elementId);
        if (!ta) return;

        // Find the highest existing footnote number, default to 0 if none.
        const defs = ta.value.matchAll(/^\[\^(\d+)\]:/gm);
        let next = 1;
        for (const m of defs) {
            const n = parseInt(m[1], 10);
            if (n >= next) next = n + 1;
        }

        const marker = `[^${next}]`;
        const start = ta.selectionStart ?? 0;
        const end = ta.selectionEnd ?? 0;
        const before = ta.value.substring(0, start);
        const after = ta.value.substring(end);

        // Insert the reference at the cursor.
        let next_value = before + marker + after;

        // Append the definition at the bottom, separated by a blank line.
        const trailingNewlines = next_value.endsWith("\n\n") ? "" : next_value.endsWith("\n") ? "\n" : "\n\n";
        const definition = `[^${next}]: ${defaultText}`;
        next_value = next_value + trailingNewlines + definition;

        ta.value = next_value;
        ta.focus();
        // Place caret right after the inserted marker so they can keep typing.
        const newCursor = before.length + marker.length;
        ta.selectionStart = ta.selectionEnd = newCursor;
        ta.dispatchEvent(new Event("input", { bubbles: true }));
    },

    // Inserts arbitrary text at a captured (start, end) range without going
    // through C# string manipulation. Used by the image upload flow, where
    // the cursor was captured before a dialog stole focus.
    insertAtRange: (elementId, start, end, text) => {
        const ta = document.getElementById(elementId);
        if (!ta) return;
        const before = ta.value.substring(0, start);
        const after = ta.value.substring(end);
        ta.value = before + text + after;
        ta.focus();
        ta.selectionStart = ta.selectionEnd = start + text.length;
        ta.dispatchEvent(new Event("input", { bubbles: true }));
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

        // Tab indents with 2 spaces (not a tab character — CommonMark treats a
        // leading tab as an indented code block). Multi-line selections indent
        // every touched line. Shift+Tab dedents; if there's nothing to dedent
        // it falls through to the default behavior so focus can still leave the
        // textarea.
        const INDENT = "  ";
        ta.addEventListener("keydown", (e) => {
            if (e.key !== "Tab") return;
            const start = ta.selectionStart;
            const end = ta.selectionEnd;
            const lineStart = ta.value.lastIndexOf("\n", start - 1) + 1;
            let lineEnd = ta.value.indexOf("\n", end);
            if (lineEnd === -1) lineEnd = ta.value.length;
            const block = ta.value.substring(lineStart, lineEnd);
            const lines = block.split("\n");
            const selectionSpansLines = lines.length > 1;

            if (e.shiftKey) {
                const stripCounts = lines.map((l) => {
                    if (l.startsWith(INDENT)) return INDENT.length;
                    if (l.startsWith("\t")) return 1;
                    if (l.startsWith(" ")) return 1;
                    return 0;
                });
                const removedTotal = stripCounts.reduce((a, b) => a + b, 0);
                if (removedTotal === 0) return;
                e.preventDefault();
                const newBlock = lines.map((l, i) => l.substring(stripCounts[i])).join("\n");
                ta.value = ta.value.substring(0, lineStart) + newBlock + ta.value.substring(lineEnd);
                ta.selectionStart = Math.max(lineStart, start - stripCounts[0]);
                ta.selectionEnd = end - removedTotal;
                ta.dispatchEvent(new Event("input", { bubbles: true }));
                return;
            }

            e.preventDefault();
            if (selectionSpansLines) {
                const newBlock = lines.map((l) => INDENT + l).join("\n");
                ta.value = ta.value.substring(0, lineStart) + newBlock + ta.value.substring(lineEnd);
                ta.selectionStart = start + INDENT.length;
                ta.selectionEnd = end + INDENT.length * lines.length;
            } else {
                const before = ta.value.substring(0, start);
                const after = ta.value.substring(end);
                ta.value = before + INDENT + after;
                ta.selectionStart = ta.selectionEnd = start + INDENT.length;
            }
            // MudTextField binds via the input event with debounce; dispatching it
            // here keeps the C# side in sync.
            ta.dispatchEvent(new Event("input", { bubbles: true }));
        });
    }
};
