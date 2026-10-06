(() => {
    window.agirePrintElement = (elementId, title) => {
        const source = document.getElementById(elementId);
        if (!source) return false;

        const printWindow = window.open("", "_blank", "popup,width=1100,height=800");
        if (!printWindow) return false;

        const direction = document.documentElement.dir || "rtl";
        const language = document.documentElement.lang || "ar";
        const printable = source.cloneNode(true);
        printable.removeAttribute("id");
        const style = `
            @page { size: 297mm 210mm; margin: 12mm; }
            * { box-sizing: border-box; }
            body { margin: 0; color: #172126; background: #fff; font-family: "Traditional Arabic", "Arabic Typesetting"; font-size: 12pt; direction: ${direction}; }
            .official-report { width: 100%; color: #172126; background: #fff; font-family: "Traditional Arabic", "Arabic Typesetting"; font-size: 10pt; }
            .official-report-header { display: flex; justify-content: space-between; gap: 18px; padding: 0 0 10pt; border-bottom: 2px solid #147e76; }
            .official-report-brand { color: #147e76; font-weight: 700; }
            .official-report-header h2 { margin: 3pt 0 0; color: #172f3a; font-size: 17pt; }
            .official-report-meta { display: grid; gap: 2pt; text-align: end; color: #536873; font-size: 8pt; }
            .official-report-section { margin-top: 12pt; overflow: visible; }
            .official-report-section h3 { margin: 0 0 5pt; color: #123b56; font-size: 11pt; break-after: avoid; }
            .official-report-table { width: 100%; border-collapse: collapse; color: #172126; font: 8pt "Traditional Arabic", "Arabic Typesetting"; }
            .official-report-table thead { display: table-header-group; }
            .official-report-table th, .official-report-table td { padding: 4pt; border: 1px solid #111; text-align: start; vertical-align: middle; }
            .official-report-table th { background: #e7ecec; color: #111; font-weight: 700; }
            .official-report-table tbody tr:nth-child(even) { background: #f2f6f6; }
            .official-report-table tfoot th, .official-report-table tfoot td { background: #e7ecec; color: #111; font-weight: 700; }
            .official-report-table tr { break-inside: avoid; }
            .official-report-footer { display: flex; justify-content: space-between; gap: 12pt; margin-top: 12pt; padding-top: 7pt; border-top: 1px solid #83969d; color: #536873; font-size: 7pt; }
            .customer-print-sheet { width: 100%; padding: 0; }
            .customer-report-titlebar { display: flex; justify-content: space-between; align-items: center; gap: 20px; padding: 5px 2px 10px; border-bottom: 1px solid #111; }
            .report-kicker { color: #287f79; font-size: 9pt; font-weight: 700; }
            .customer-report-titlebar h2 { margin: 5px 0 0; color: #111; font-size: 17pt; }
            .customer-report-issued { display: grid; gap: 4px; text-align: end; color: #111; }
            .customer-report-issued small, .customer-report-table td small { color: #333; font-size: 8pt; }
            .customer-report-issued b { font-family: "Traditional Arabic", "Arabic Typesetting"; font-variant-numeric: tabular-nums; }
            .customer-report-table-wrap { width: 100%; overflow: visible; }
            .customer-report-table { width: 100%; border-collapse: collapse; table-layout: fixed; font: 9pt "Traditional Arabic", "Arabic Typesetting"; }
            .customer-report-table thead { display: table-header-group; }
            .customer-report-table th { padding: 5px 4px; border: 1px solid #111; background: #e7ecec; color: #111; font-weight: 700; text-align: center; }
            .customer-report-table td { padding: 5px 4px; border: 1px solid #111; color: #111; vertical-align: middle; text-align: center; white-space: normal; }
            .customer-report-table td:nth-child(2) { text-align: start; }
            .customer-report-table td:nth-child(2) b, .customer-report-table td small { display: block; }
            .customer-report-table td small { margin-top: 3px; }
            .customer-report-table tbody tr:nth-child(even) { background: #f4f6f6; }
            .customer-report-table tfoot th, .customer-report-table tfoot td { background: #e7ecec; font-weight: 700; }
            .customer-report-table tr { break-inside: avoid; }
            .status-pill { display: inline-block; padding: 3px 7px; border-radius: 99px; background: #e7f3ef; color: #277158; }
            .status-pill.due { background: #fbefdf; color: #966525; }
            .customer-report-table .status-pill { display: inline; padding: 0; border-radius: 0; background: transparent; color: #111; }
            .customer-report-table .status-pill.due { background: transparent; color: #111; }
            .customer-report-footer { display: flex; justify-content: space-between; gap: 12px; margin-top: 10px; padding-top: 8px; border-top: 1px solid #cbd5d7; color: #536268; font-size: 7pt; }
        `;

        printWindow.document.open();
        printWindow.document.write(`<!doctype html><html lang="${language}" dir="${direction}"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${escapeHtml(title)}</title><style>${style}</style></head><body></body></html>`);
        printWindow.document.close();
        printWindow.document.body.appendChild(printable);
        printWindow.addEventListener("afterprint", () => printWindow.close(), { once: true });
        printWindow.focus();
        window.setTimeout(() => printWindow.print(), 250);
        return true;
    };

    const escapeHtml = value => String(value).replace(/[&<>"']/g, character => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    })[character]);

    const syncButton = button => {
        const isDark = document.documentElement.dataset.theme === "dark";
        const label = isDark ? button.dataset.lightLabel : button.dataset.darkLabel;
        const labelElement = button.querySelector(".theme-toggle-label");
        if (labelElement) labelElement.textContent = label;
        const icon = button.querySelector(".theme-toggle-icon");
        if (icon) icon.dataset.state = isDark ? "sun" : "moon";
        button.setAttribute("aria-label", label);
        button.setAttribute("title", label);
    };
    const setTheme = (isDark, button) => {
        const theme = isDark ? "dark" : "light";
        document.documentElement.dataset.theme = theme;
        try { localStorage.setItem("hawdh-theme", theme); } catch { }
        document.cookie = `hawdh-theme=${theme}; Max-Age=31536000; Path=/; SameSite=Lax${location.protocol === "https:" ? "; Secure" : ""}`;
        if (button) syncButton(button);
    };

    const sidebarPreferenceKey = "agire-sidebar-collapsed";
    const syncSidebarButton = (button, collapsed) => {
        const label = collapsed ? button.dataset.showLabel : button.dataset.hideLabel;
        button.classList.toggle("collapsed", collapsed);
        button.setAttribute("aria-expanded", String(!collapsed));
        if (label) {
            button.setAttribute("aria-label", label);
            button.setAttribute("title", label);
        }
    };
    const syncAllSidebarButtons = (collapsed) => {
        document.querySelectorAll("[data-sidebar-toggle]").forEach(button => syncSidebarButton(button, collapsed));
    };
    const applySidebarPreference = () => {
        const shell = document.querySelector(".app-shell");
        const button = document.querySelector("[data-sidebar-toggle]");
        if (!shell || !button) return;
        let collapsed = false;
        try { collapsed = localStorage.getItem(sidebarPreferenceKey) === "true"; } catch { }
        shell.classList.toggle("sidebar-collapsed", collapsed);
        syncAllSidebarButtons(collapsed);
    };

    document.querySelectorAll(".theme-toggle").forEach(syncButton);
    const updateClocks = () => {
        const now = new Date();
        const locale = document.documentElement.lang || undefined;
        const date = new Intl.DateTimeFormat(locale, { weekday: "long", day: "numeric", month: "long", year: "numeric" }).format(now);
        const time = new Intl.DateTimeFormat(locale, { hour: "2-digit", minute: "2-digit", hour12: false }).format(now);
        document.querySelectorAll("[data-live-clock]").forEach(clock => {
            const dateNode = clock.querySelector("[data-clock-date]");
            const timeNode = clock.querySelector("[data-clock-time]");
            if (dateNode) dateNode.querySelector("[data-clock-date-full]").textContent = date;
            if (timeNode) timeNode.textContent = time;
            clock.dateTime = now.toISOString();
        });
    };
    updateClocks();
    window.setInterval(updateClocks, 15000);
    const logoTargets = ".agency-logo, .brand-lockup, .mobile-agency-lockup, .auth-brand-lockup";
    document.addEventListener("dragstart", event => {
        if (event.target instanceof Element && event.target.closest(logoTargets)) event.preventDefault();
    }, true);
    document.addEventListener("contextmenu", event => {
        if (event.target instanceof Element && event.target.closest(logoTargets)) {
            event.preventDefault();
            event.stopPropagation();
        }
    }, true);
    document.addEventListener("click", event => {
        const button = event.target.closest(".theme-toggle");
        if (button) setTheme(document.documentElement.dataset.theme !== "dark", button);

        const sidebarButton = event.target.closest("[data-sidebar-toggle]");
        if (sidebarButton) {
            const shell = sidebarButton.closest(".app-shell");
            if (!shell) return;
            const collapsed = !shell.classList.contains("sidebar-collapsed");
            shell.classList.toggle("sidebar-collapsed", collapsed);
            syncAllSidebarButtons(collapsed);
            try { localStorage.setItem(sidebarPreferenceKey, String(collapsed)); } catch { }
        }
    });
    applySidebarPreference();
    document.addEventListener("enhancedload", applySidebarPreference);
})();
