// TipTap-backed WYSIWYG editor, loaded on demand from esm.sh so we don't need
// a bundler in this repo. The public shape (`window.ordoWysiwyg`) is intentionally
// small so a future swap to CKEditor 5 or another editor can drop into the same
// contract: init/getContent/setContent/destroy — nothing TipTap-specific leaks
// out to the Blazor side.
(() => {
    const TIPTAP_VERSION = "2.11.7";
    const cdn = (pkg) => `https://esm.sh/${pkg}@${TIPTAP_VERSION}`;

    const editors = new Map();
    let modulesPromise = null;

    const loadModules = () => {
        if (modulesPromise) return modulesPromise;
        modulesPromise = Promise.all([
            import(cdn("@tiptap/core")),
            import(cdn("@tiptap/starter-kit")),
            import(cdn("@tiptap/extension-link")),
            import(cdn("@tiptap/extension-image")),
            import(cdn("@tiptap/extension-table")),
            import(cdn("@tiptap/extension-table-row")),
            import(cdn("@tiptap/extension-table-cell")),
            import(cdn("@tiptap/extension-table-header")),
            import(cdn("@tiptap/extension-task-list")),
            import(cdn("@tiptap/extension-task-item")),
            import(cdn("@tiptap/extension-text-align")),
            import(cdn("@tiptap/extension-underline")),
            import(cdn("@tiptap/extension-text-style")),
            import(cdn("@tiptap/extension-color")),
            import(cdn("@tiptap/extension-highlight")),
        ]).then(([core, starter, link, image, table, row, cell, header, taskList, taskItem, textAlign, underline, textStyle, color, highlight]) => ({
            Editor: core.Editor,
            StarterKit: starter.default ?? starter.StarterKit,
            Link: link.default ?? link.Link,
            Image: image.default ?? image.Image,
            Table: table.default ?? table.Table,
            TableRow: row.default ?? row.TableRow,
            TableCell: cell.default ?? cell.TableCell,
            TableHeader: header.default ?? header.TableHeader,
            TaskList: taskList.default ?? taskList.TaskList,
            TaskItem: taskItem.default ?? taskItem.TaskItem,
            TextAlign: textAlign.default ?? textAlign.TextAlign,
            Underline: underline.default ?? underline.Underline,
            TextStyle: textStyle.default ?? textStyle.TextStyle,
            Color: color.default ?? color.Color,
            Highlight: highlight.default ?? highlight.Highlight,
        }));
        return modulesPromise;
    };

    // Word-on-Windows paste is full of `mso-*` inline styles and `<o:p>` tags.
    // Strip the worst offenders before ProseMirror parses — the server-side
    // sanitizer catches the rest on save, but this keeps the editor view clean.
    const cleanPasteHtml = (html) => {
        return html
            .replace(/<!--[\s\S]*?-->/g, "")
            .replace(/<o:p[^>]*>[\s\S]*?<\/o:p>/gi, "")
            .replace(/<\/?xml[^>]*>/gi, "")
            .replace(/style="[^"]*mso-[^"]*"/gi, (m) => {
                const cleaned = m.replace(/[^;"]*mso-[^;"]*;?/gi, "").replace(/style="\s*"/, "");
                return cleaned || "";
            })
            .replace(/class="?Mso[^"\s>]*"?/gi, "");
    };

    window.ordoWysiwyg = {
        init: async (elementId, initialHtml, dotnetRef) => {
            const host = document.getElementById(elementId);
            if (!host) return;

            if (editors.has(elementId)) {
                editors.get(elementId).destroy();
                editors.delete(elementId);
            }

            const M = await loadModules();

            const editor = new M.Editor({
                element: host,
                content: initialHtml || "",
                extensions: [
                    M.StarterKit.configure({ heading: { levels: [1, 2, 3, 4] } }),
                    M.Underline,
                    M.Link.configure({ openOnClick: false, autolink: true, linkOnPaste: true }),
                    M.Image.configure({ inline: false, allowBase64: true }),
                    M.Table.configure({ resizable: true }),
                    M.TableRow,
                    M.TableCell,
                    M.TableHeader,
                    M.TaskList,
                    M.TaskItem.configure({ nested: true }),
                    M.TextAlign.configure({ types: ["heading", "paragraph"] }),
                    M.TextStyle,
                    M.Color,
                    M.Highlight.configure({ multicolor: true }),
                ],
                editorProps: {
                    attributes: {
                        class: "tiptap-editor markdown-body",
                    },
                    transformPastedHTML: cleanPasteHtml,
                },
                onUpdate: ({ editor }) => {
                    if (!dotnetRef) return;
                    const html = editor.getHTML();
                    dotnetRef.invokeMethodAsync("OnContentChangedAsync", html).catch(() => {});
                },
            });

            editors.set(elementId, editor);
        },

        getContent: (elementId) => {
            const editor = editors.get(elementId);
            return editor ? editor.getHTML() : "";
        },

        setContent: (elementId, html) => {
            const editor = editors.get(elementId);
            if (editor) editor.commands.setContent(html || "", false);
        },

        exec: (elementId, command, ...args) => {
            const editor = editors.get(elementId);
            if (!editor) return;
            const chain = editor.chain().focus();
            if (typeof chain[command] === "function") {
                chain[command](...args).run();
            }
        },

        destroy: (elementId) => {
            const editor = editors.get(elementId);
            if (editor) {
                editor.destroy();
                editors.delete(elementId);
            }
        },
    };
})();
