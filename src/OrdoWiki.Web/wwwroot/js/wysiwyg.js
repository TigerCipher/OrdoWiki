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
            import(cdn("@tiptap/extension-highlight")),
            import(cdn("@tiptap/extension-font-family")),
        ]).then(([core, starter, link, image, table, row, cell, header, taskList, taskItem, textAlign, underline, textStyle, highlight, fontFamily]) => ({
            Editor: core.Editor,
            Extension: core.Extension,
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
            Highlight: highlight.default ?? highlight.Highlight,
            FontFamily: fontFamily.default ?? fontFamily.FontFamily,
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

    // TipTap doesn't ship a first-party font-size extension. FontFamily's source is
    // the template we're copying: an Extension (not a Mark) that adds a fontSize
    // attribute to the shared textStyle mark via addGlobalAttributes, plus
    // set/unset commands that mutate the textStyle mark directly. Renaming to a
    // new mark and then calling setMark('textStyle', ...) — as an earlier version
    // did — drops the attribute because it lives on a different mark.
    const buildFontSize = (M) => M.Extension.create({
        name: "fontSize",
        addOptions() {
            return { types: ["textStyle"] };
        },
        addGlobalAttributes() {
            return [{
                types: this.options.types,
                attributes: {
                    fontSize: {
                        default: null,
                        parseHTML: (el) => el.style.fontSize?.replace(/['"]+/g, "") || null,
                        renderHTML: (attrs) => {
                            if (!attrs.fontSize) return {};
                            return { style: `font-size: ${attrs.fontSize}` };
                        },
                    },
                },
            }];
        },
        addCommands() {
            return {
                setFontSize: (fontSize) => ({ chain }) =>
                    chain().setMark("textStyle", { fontSize }).run(),
                unsetFontSize: () => ({ chain }) =>
                    chain().setMark("textStyle", { fontSize: null }).removeEmptyTextStyle().run(),
            };
        },
    });

    // Extend Image with a width attribute + a wrapping NodeView that renders drag
    // handles when the image is selected. Keeps the editor a self-contained ESM
    // module — no community package dependency + no bundler required.
    const buildResizableImage = (M) => M.Image.extend({
        addAttributes() {
            return {
                ...this.parent?.(),
                width: {
                    default: null,
                    parseHTML: (el) => el.getAttribute("width") || el.style.width || null,
                    renderHTML: (attrs) => attrs.width ? { style: `width: ${attrs.width}` } : {},
                },
            };
        },
        addNodeView() {
            return ({ node, editor, getPos }) => {
                const wrapper = document.createElement("span");
                wrapper.className = "resizable-image";
                wrapper.style.display = "inline-block";
                wrapper.style.position = "relative";
                wrapper.style.maxWidth = "100%";
                if (node.attrs.width) wrapper.style.width = node.attrs.width;

                const img = document.createElement("img");
                if (node.attrs.src) img.src = node.attrs.src;
                if (node.attrs.alt) img.alt = node.attrs.alt;
                if (node.attrs.title) img.title = node.attrs.title;
                img.style.display = "block";
                img.style.maxWidth = "100%";
                img.style.width = "100%";
                img.draggable = false;
                wrapper.appendChild(img);

                const handle = document.createElement("span");
                handle.className = "resize-handle";
                handle.style.display = "none";
                wrapper.appendChild(handle);

                let isSelected = false;
                const setSelected = (selected) => {
                    isSelected = selected;
                    wrapper.classList.toggle("is-selected", selected);
                    handle.style.display = selected ? "block" : "none";
                };

                // ProseMirror fires selectNode/deselectNode when the atom node is
                // singly selected — perfect trigger for showing/hiding handles.
                wrapper.addEventListener("mousedown", (e) => {
                    if (e.target === handle) return;
                    if (typeof getPos === "function") {
                        editor.commands.setNodeSelection(getPos());
                    }
                });

                let startX = 0;
                let startWidth = 0;
                let dragging = false;

                const onMove = (e) => {
                    if (!dragging) return;
                    const dx = e.clientX - startX;
                    const newWidth = Math.max(60, startWidth + dx);
                    wrapper.style.width = `${newWidth}px`;
                };

                const onUp = () => {
                    if (!dragging) return;
                    dragging = false;
                    document.removeEventListener("mousemove", onMove);
                    document.removeEventListener("mouseup", onUp);

                    if (typeof getPos === "function") {
                        editor.chain()
                            .focus()
                            .setNodeSelection(getPos())
                            .updateAttributes("image", { width: wrapper.style.width })
                            .run();
                    }
                };

                handle.addEventListener("mousedown", (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    dragging = true;
                    startX = e.clientX;
                    startWidth = wrapper.getBoundingClientRect().width;
                    document.addEventListener("mousemove", onMove);
                    document.addEventListener("mouseup", onUp);
                });

                return {
                    dom: wrapper,
                    selectNode: () => setSelected(true),
                    deselectNode: () => setSelected(false),
                    update: (updatedNode) => {
                        if (updatedNode.type.name !== "image") return false;
                        if (updatedNode.attrs.src !== node.attrs.src) return false;
                        wrapper.style.width = updatedNode.attrs.width || "";
                        return true;
                    },
                };
            };
        },
    });

    window.ordoWysiwyg = {
        init: async (elementId, initialHtml, dotnetRef) => {
            const host = document.getElementById(elementId);
            if (!host) return;

            if (editors.has(elementId)) {
                editors.get(elementId).destroy();
                editors.delete(elementId);
            }

            const M = await loadModules();
            const FontSize = buildFontSize(M);
            const ResizableImage = buildResizableImage(M);

            const editor = new M.Editor({
                element: host,
                content: initialHtml || "",
                extensions: [
                    M.StarterKit.configure({ heading: { levels: [1, 2, 3, 4] } }),
                    M.Underline,
                    M.Link.configure({ openOnClick: false, autolink: true, linkOnPaste: true }),
                    ResizableImage.configure({ inline: false, allowBase64: true }),
                    M.Table.configure({ resizable: true }),
                    M.TableRow,
                    M.TableCell,
                    M.TableHeader,
                    M.TaskList,
                    M.TaskItem.configure({ nested: true }),
                    M.TextAlign.configure({ types: ["heading", "paragraph"] }),
                    M.TextStyle,
                    M.Highlight.configure({ multicolor: true }),
                    M.FontFamily,
                    FontSize,
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
