// Fragment-only links inside rendered markdown (e.g. footnote refs/back-refs)
// resolve against `<base href="/">` instead of the current page, so the
// browser/Blazor would navigate to `/#fnref-1` instead of scrolling on the
// current page. Intercept the click and scroll to the target element.
//
// Registered with capture: true so we run BEFORE Blazor's enhanced-nav click
// handler (which is bubble-phase) and can stop it.
document.addEventListener("click", (e) => {
    if (e.defaultPrevented || e.button !== 0) return;
    if (e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;

    const link = e.target.closest("a[href]");
    if (!link) return;
    if (!link.closest(".markdown-body")) return;

    const href = link.getAttribute("href");
    if (!href || !href.startsWith("#") || href === "#") return;

    const id = decodeURIComponent(href.slice(1));
    const target = document.getElementById(id);
    if (!target) return;

    e.preventDefault();
    e.stopImmediatePropagation();
    target.scrollIntoView({ behavior: "smooth", block: "start" });

    // Keep the fragment in the address bar without triggering navigation. Use
    // the canonical current URL so the back button takes the user back to the
    // pre-click position on the same page.
    const url = new URL(window.location.href);
    url.hash = id;
    history.pushState(null, "", url);
}, { capture: true });

console.debug("[ordo] markdown anchor handler attached");
