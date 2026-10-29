/**
 * ============================================================================
 * #EPLUS DIRECTORY APPLICATION LOGIC (app.js)
 * ============================================================================
 */

(function () {
  "use strict";

  let config = window.SITE_CONFIG;
  if (!config) {
    console.error("SITE_CONFIG not found. Ensure data.js is loaded before app.js.");
    return;
  }

  // Supabase Live Catalog Sync Endpoint (Public Read-Only Anon Key)
  const SUPABASE_CATALOG_URL =
    "https://urdyjiatqzdvkjwiruop.supabase.co/rest/v1/eplus_vault_config?id=eq.main&select=payload";
  const SUPABASE_ANON_KEY =
    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InVyZHlqaWF0cXpkdmtqd2lydW9wIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODg1ODM1OTAsImV4cCI6MjEwNDE1OTU5MH0.Agbn6e-iyygekRCHhqDklHnt_svgePv5pnAwQEOb0dA";

  // State
  let activeCategory = "all";
  let searchQuery = "";
  let currentView = localStorage.getItem("eplus_view") || config.site.defaultView || "grid";
  let currentTheme = localStorage.getItem("eplus_theme_v2") || config.site.defaultTheme || "light";
  let toastTimeout = null;

  // DOM Elements
  const brandTitleEl = document.getElementById("brand-title");
  const brandBadgeEl = document.getElementById("brand-badge");
  const heroTitleEl = document.getElementById("hero-title");
  const heroSubtitleEl = document.getElementById("hero-subtitle");
  const rootFolderLinkEl = document.getElementById("root-folder-link");
  const rootFolderLabelEl = document.getElementById("root-folder-label");
  const searchInputEl = document.getElementById("search-input");
  const searchClearBtn = document.getElementById("search-clear-btn");
  const categoryTabsEl = document.getElementById("category-tabs");
  const catScrollLeftBtn = document.getElementById("cat-scroll-left");
  const catScrollRightBtn = document.getElementById("cat-scroll-right");
  const directoryContentEl = document.getElementById("directory-content");
  const viewGridBtn = document.getElementById("view-grid-btn");
  const viewListBtn = document.getElementById("view-list-btn");
  const themeToggleBtn = document.getElementById("theme-toggle-btn");
  const toastEl = document.getElementById("toast");
  const toastMessageEl = document.getElementById("toast-message");

  // SVG Icons
  const ICONS = {
    sun: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="4"/><path d="M12 2v2"/><path d="M12 20v2"/><path d="m4.93 4.93 1.41 1.41"/><path d="m17.66 17.66 1.41 1.41"/><path d="M2 12h2"/><path d="M20 12h2"/><path d="m6.34 17.66-1.41 1.41"/><path d="m19.07 4.93-1.41 1.41"/></svg>`,
    moon: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z"/></svg>`,
    folder: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z"/></svg>`,
    database: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M3 5V19A9 3 0 0 0 21 19V5"/><path d="M3 12A9 3 0 0 0 21 12"/></svg>`,
    wrench: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/></svg>`,
    disc: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><circle cx="12" cy="12" r="2"/></svg>`,
    archive: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="20" height="5" x="2" y="3" rx="1"/><path d="M4 8v11a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8"/><path d="M10 12h4"/></svg>`,
    download: `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" x2="12" y1="15" y2="3"/></svg>`,
    external: `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M15 3h6v6"/><path d="M10 14 21 3"/><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/></svg>`,
    key: `<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m15.5 7.5 2.3 2.3a1 1 0 0 0 1.4 0l2.1-2.1a1 1 0 0 0 0-1.4L19 4"/><path d="m21 2-9.6 9.6"/><circle cx="7.5" cy="15.5" r="5.5"/></svg>`,
    hash: `<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="4" x2="20" y1="9" y2="9"/><line x1="4" x2="20" y1="15" y2="15"/><line x1="10" x2="8" y1="3" y2="21"/><line x1="16" x2="14" y1="3" y2="21"/></svg>`,
    copy: `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="14" height="14" x="8" y="8" rx="2" ry="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/></svg>`
  };

  function getCategoryIcon(categoryId, isFolderItem) {
    if (isFolderItem) return ICONS.folder;
    switch (categoryId) {
      case "eplus-db":
      case "eplus-core":
      case "eplus-116":
      case "future2010":
        return ICONS.database;
      case "utilities":
      case "other-tools":
        return ICONS.wrench;
      case "software-os":
        return ICONS.disc;
      case "printer-drivers":
        return ICONS.archive;
      case "folders":
        return ICONS.folder;
      default:
        return ICONS.archive;
    }
  }

  function escapeHtml(str) {
    if (!str) return "";
    return String(str)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#039;");
  }

  function matchesSearch(item, q) {
    if (!q) return true;
    const terms = q.toLowerCase().trim().split(/\s+/);
    const haystack = [
      item.name || "",
      item.filename || "",
      item.description || "",
      item.tag || "",
      item.password || "",
      item.category || ""
    ]
      .join(" ")
      .toLowerCase();

    return terms.every((term) => haystack.includes(term));
  }

  function showToast(message) {
    if (!toastEl || !toastMessageEl) return;
    toastMessageEl.textContent = message;
    toastEl.classList.add("visible");
    if (toastTimeout) clearTimeout(toastTimeout);
    toastTimeout = setTimeout(() => {
      toastEl.classList.remove("visible");
    }, 2200);
  }

  function copyToClipboard(text, label) {
    if (!text) return;
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(() => {
        showToast(`Copied ${label} to clipboard`);
      });
    } else {
      const textarea = document.createElement("textarea");
      textarea.value = text;
      textarea.style.position = "fixed";
      textarea.style.opacity = "0";
      document.body.appendChild(textarea);
      textarea.select();
      try {
        document.execCommand("copy");
        showToast(`Copied ${label} to clipboard`);
      } catch (err) {
        console.error("Copy failed", err);
      }
      document.body.removeChild(textarea);
    }
  }

  // Apply Theme
  function applyTheme(theme) {
    currentTheme = theme === "dark" ? "dark" : "light";
    document.documentElement.setAttribute("data-theme", currentTheme);
    localStorage.setItem("eplus_theme_v2", currentTheme);
    if (themeToggleBtn) {
      themeToggleBtn.innerHTML = currentTheme === "dark" ? ICONS.sun : ICONS.moon;
      themeToggleBtn.setAttribute(
        "aria-label",
        currentTheme === "dark" ? "Switch to light mode" : "Switch to dark mode"
      );
      themeToggleBtn.setAttribute(
        "title",
        currentTheme === "dark" ? "Switch to light mode" : "Switch to dark mode"
      );
    }
  }

  // Apply View (Grid / List)
  function applyView(view) {
    currentView = view === "list" ? "list" : "grid";
    localStorage.setItem("eplus_view", currentView);

    if (viewGridBtn && viewListBtn) {
      viewGridBtn.classList.toggle("active", currentView === "grid");
      viewListBtn.classList.toggle("active", currentView === "list");
    }

    const containers = directoryContentEl.querySelectorAll(".items-container");
    containers.forEach((container) => {
      container.classList.toggle("grid-view", currentView === "grid");
      container.classList.toggle("list-view", currentView === "list");
    });
  }

  function updateCategoryScrollButtons() {
    if (!categoryTabsEl) return;
    const maxScrollLeft = categoryTabsEl.scrollWidth - categoryTabsEl.clientWidth;
    if (catScrollLeftBtn) {
      catScrollLeftBtn.disabled = categoryTabsEl.scrollLeft <= 2;
    }
    if (catScrollRightBtn) {
      catScrollRightBtn.disabled = categoryTabsEl.scrollLeft >= maxScrollLeft - 2;
    }
  }

  // Render Category Filter Tabs
  function renderTabs() {
    const prevScrollLeft = categoryTabsEl ? categoryTabsEl.scrollLeft : 0;
    const matchingFiles = config.files.filter((f) => matchesSearch(f, searchQuery));
    const totalCount = matchingFiles.length;

    let html = `
      <button type="button" class="tab-btn ${activeCategory === "all" ? "active" : ""}" data-cat="all">
        <span>All</span>
        <span class="tab-count mono">${totalCount}</span>
      </button>
    `;

    config.categories.forEach((cat) => {
      const count = matchingFiles.filter((f) => f.category === cat.id).length;
      if (count === 0 && !searchQuery) return;
      const isActive = activeCategory === cat.id ? "active" : "";
      html += `
        <button type="button" class="tab-btn ${isActive}" data-cat="${escapeHtml(cat.id)}">
          <span>${escapeHtml(cat.name)}</span>
          <span class="tab-count mono">${count}</span>
        </button>
      `;
    });

    categoryTabsEl.innerHTML = html;
    categoryTabsEl.scrollLeft = prevScrollLeft;
    updateCategoryScrollButtons();

    categoryTabsEl.querySelectorAll(".tab-btn").forEach((btn) => {
      btn.addEventListener("click", () => {
        if (categoryTabsEl.dataset.dragged === "true") return;
        activeCategory = btn.getAttribute("data-cat") || "all";
        renderTabs();
        renderDirectory();
        const activeBtn = categoryTabsEl.querySelector(".tab-btn.active");
        if (activeBtn && activeBtn.scrollIntoView) {
          activeBtn.scrollIntoView({ behavior: "smooth", inline: "nearest", block: "nearest" });
        }
      });
    });
  }

  // Render Single File Card / Row
  function renderFileItem(item) {
    const isFolder = item.category === "folders" || item.size === "Subfolder";
    const actionIcon = isFolder ? ICONS.external : ICONS.download;
    const actionLabel = item.urlLabel || (isFolder ? "Open Folder" : "Download");

    // Metadata Pills (Size, Password, Checksum)
    let metaPillsHtml = "";

    if (item.size && item.size !== "Subfolder") {
      metaPillsHtml += `<span class="meta-pill mono" title="File Size">${escapeHtml(item.size)}</span>`;
    }

    if (item.password) {
      metaPillsHtml += `
        <button type="button" class="meta-pill pw-pill mono" data-copy="${escapeHtml(item.password)}" data-copy-label="password (${escapeHtml(item.password)})" title="Click to copy archive password">
          ${ICONS.key}
          <span>PW: ${escapeHtml(item.password)}</span>
        </button>
      `;
    }

    if (item.checksum) {
      const shortHash = item.checksum.slice(0, 8) + "…";
      metaPillsHtml += `
        <button type="button" class="meta-pill mono" data-copy="${escapeHtml(item.checksum)}" data-copy-label="SHA-256 checksum" title="Click to copy full SHA-256: ${escapeHtml(item.checksum)}">
          ${ICONS.hash}
          <span>SHA256: ${escapeHtml(shortHash)}</span>
        </button>
      `;
    }

    // Mirror links
    let mirrorsHtml = "";
    if (Array.isArray(item.mirrors) && item.mirrors.length > 0) {
      mirrorsHtml = item.mirrors
        .map(
          (m) => `
          <a href="${escapeHtml(m.url)}" target="_blank" rel="noopener noreferrer" class="btn-mirror" title="${escapeHtml(m.label)}">
            ${ICONS.download}
            <span>${escapeHtml(m.label)}</span>
          </a>
        `
        )
        .join("");
    }

    const floatingNoteHtml = item.description
      ? `
        <div class="floating-note" role="tooltip">
          <span class="floating-note-label mono">Description / Note</span>
          ${escapeHtml(item.description)}
        </div>
      `
      : "";

    const noteBadgeHtml = item.description
      ? `
        <span class="note-indicator" aria-hidden="true">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M12 16v-4"/><path d="M12 8h.01"/></svg>
        </span>
      `
      : "";

    return `
      <article class="file-item" data-category="${escapeHtml(isFolder ? "folders" : item.category)}">
        ${floatingNoteHtml}
        <div class="item-top">
          <div class="item-header-row">
            <div class="item-title-group">
              <span class="item-icon">${getCategoryIcon(item.category, isFolder)}</span>
              <div>
                <h3 class="item-name">${escapeHtml(item.name)}</h3>
                ${item.filename ? `<div class="item-filename mono">${escapeHtml(item.filename)}</div>` : ""}
              </div>
            </div>
            <div class="item-badges-right">
              ${noteBadgeHtml}
              ${item.tag ? `<span class="item-tag mono">${escapeHtml(item.tag)}</span>` : ""}
            </div>
          </div>
          ${metaPillsHtml ? `<div class="item-meta-row">${metaPillsHtml}</div>` : ""}
        </div>

        <div class="item-actions">
          <div class="action-links-group">
            <a href="${escapeHtml(item.url)}" ${item.url.startsWith("http") ? 'target="_blank" rel="noopener noreferrer"' : `download="${escapeHtml(item.filename || "")}"`} class="btn-primary">
              ${actionIcon}
              <span>${escapeHtml(actionLabel)}</span>
            </a>
            ${mirrorsHtml}
            <button type="button" class="btn-copy-link" data-copy="${escapeHtml(item.url.startsWith("http") ? item.url : window.location.origin + window.location.pathname.replace(/[^/]*$/, '') + item.url)}" data-copy-label="link for ${escapeHtml(item.name)}" title="Copy direct link">
              ${ICONS.copy}
            </button>
          </div>
        </div>
      </article>
    `;
  }

  // Render Grouped Directory Sections
  function renderDirectory() {
    const viewClass = currentView === "list" ? "list-view" : "grid-view";
    const categoriesToShow =
      activeCategory === "all"
        ? config.categories
        : config.categories.filter((c) => c.id === activeCategory);

    let sectionsHtml = "";

    categoriesToShow.forEach((cat) => {
      const items = config.files.filter(
        (f) => f.category === cat.id && matchesSearch(f, searchQuery)
      );
      if (items.length === 0) return;

      const itemsHtml = items.map(renderFileItem).join("");
      const folderLinkHtml = cat.folderUrl
        ? `<a href="${escapeHtml(cat.folderUrl)}" target="_blank" rel="noopener noreferrer" class="section-folder-link mono" title="Open ${escapeHtml(cat.name)} folder on MediaFire">${ICONS.external} <span>MediaFire Folder</span></a>`
        : "";

      sectionsHtml += `
        <section class="category-section" id="section-${escapeHtml(cat.id)}">
          <div class="section-header">
            <div class="section-title-wrap">
              <h2 class="section-title">${escapeHtml(cat.name)}</h2>
              ${cat.description ? `<span class="section-desc">${escapeHtml(cat.description)}</span>` : ""}
            </div>
            <div class="section-meta-right">
              ${folderLinkHtml}
              <span class="section-count mono">${items.length} ${items.length === 1 ? "item" : "items"}</span>
            </div>
          </div>
          <div class="items-container ${viewClass}">
            ${itemsHtml}
          </div>
        </section>
      `;
    });

    if (!sectionsHtml) {
      sectionsHtml = `
        <div class="empty-search-state">
          <div class="empty-search-title">No files found matching "${escapeHtml(searchQuery)}"</div>
          <div class="empty-search-sub">Try searching a different keyword or switching the category tab to "All".</div>
        </div>
      `;
    }

    directoryContentEl.innerHTML = sectionsHtml;

    // Bind copy buttons
    directoryContentEl.querySelectorAll("[data-copy]").forEach((btn) => {
      btn.addEventListener("click", (e) => {
        e.preventDefault();
        const text = btn.getAttribute("data-copy");
        const label = btn.getAttribute("data-copy-label") || "text";
        copyToClipboard(text, label);
      });
    });
  }

  // Initialize Site
  function init() {
    if (brandTitleEl && config.site.title) {
      brandTitleEl.textContent = config.site.title;
      document.title = `${config.site.title} — Software & Essential Files`;
    }
    if (brandBadgeEl && config.site.badge) {
      brandBadgeEl.textContent = config.site.badge;
    }
    if (heroTitleEl && config.site.title) {
      heroTitleEl.textContent = config.site.title;
    }
    if (heroSubtitleEl && config.site.subtitle) {
      heroSubtitleEl.textContent = config.site.subtitle;
    }
    if (rootFolderLinkEl && config.site.rootFolderUrl) {
      rootFolderLinkEl.href = config.site.rootFolderUrl;
    }
    if (rootFolderLabelEl && config.site.rootFolderLabel) {
      rootFolderLabelEl.textContent = config.site.rootFolderLabel;
    }

    applyTheme(currentTheme);
    renderTabs();
    renderDirectory();
    applyView(currentView);

    // Search Input Listeners
    if (searchInputEl) {
      searchInputEl.addEventListener("input", () => {
        searchQuery = searchInputEl.value;
        if (searchClearBtn) {
          searchClearBtn.hidden = !searchQuery;
        }
        renderTabs();
        renderDirectory();
      });
    }

    if (searchClearBtn && searchInputEl) {
      searchClearBtn.addEventListener("click", () => {
        searchInputEl.value = "";
        searchQuery = "";
        searchClearBtn.hidden = true;
        searchInputEl.focus();
        renderTabs();
        renderDirectory();
      });
    }

    // Keyboard shortcut '/' or 'Ctrl+K' to focus search bar
    document.addEventListener("keydown", (e) => {
      if (!searchInputEl) return;
      const isInputFocused =
        document.activeElement === searchInputEl ||
        ["INPUT", "TEXTAREA"].includes(document.activeElement?.tagName);

      if ((e.key === "/" && !isInputFocused) || ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k")) {
        e.preventDefault();
        searchInputEl.focus();
        searchInputEl.select();
      } else if (e.key === "Escape" && document.activeElement === searchInputEl) {
        if (searchInputEl.value) {
          searchInputEl.value = "";
          searchQuery = "";
          if (searchClearBtn) searchClearBtn.hidden = true;
          renderTabs();
          renderDirectory();
        } else {
          searchInputEl.blur();
        }
      }
    });

    // Theme & View Listeners
    if (themeToggleBtn) {
      themeToggleBtn.addEventListener("click", () => {
        applyTheme(currentTheme === "dark" ? "light" : "dark");
      });
    }

    if (viewGridBtn) {
      viewGridBtn.addEventListener("click", () => applyView("grid"));
    }

    if (viewListBtn) {
      viewListBtn.addEventListener("click", () => applyView("list"));
    }

    // Horizontal Category Bar Scroll Controls (Buttons, Mouse Wheel, Drag)
    if (categoryTabsEl) {
      categoryTabsEl.addEventListener("scroll", updateCategoryScrollButtons, { passive: true });
      window.addEventListener("resize", updateCategoryScrollButtons);

      if (catScrollLeftBtn) {
        catScrollLeftBtn.addEventListener("click", () => {
          categoryTabsEl.scrollBy({ left: -260, behavior: "smooth" });
        });
      }

      if (catScrollRightBtn) {
        catScrollRightBtn.addEventListener("click", () => {
          categoryTabsEl.scrollBy({ left: 260, behavior: "smooth" });
        });
      }

      // Allow vertical mouse wheel over the category bar to scroll it horizontally
      categoryTabsEl.addEventListener(
        "wheel",
        (e) => {
          if (Math.abs(e.deltaY) > Math.abs(e.deltaX)) {
            const maxScroll = categoryTabsEl.scrollWidth - categoryTabsEl.clientWidth;
            if (maxScroll > 0) {
              e.preventDefault();
              categoryTabsEl.scrollLeft += e.deltaY;
            }
          }
        },
        { passive: false }
      );

      // Click-and-drag horizontal scrolling on desktop
      let isDown = false;
      let startX = 0;
      let scrollStart = 0;

      categoryTabsEl.addEventListener("mousedown", (e) => {
        isDown = true;
        categoryTabsEl.dataset.dragged = "false";
        startX = e.pageX - categoryTabsEl.offsetLeft;
        scrollStart = categoryTabsEl.scrollLeft;
      });

      window.addEventListener("mouseup", () => {
        if (!isDown) return;
        isDown = false;
        categoryTabsEl.classList.remove("dragging");
        setTimeout(() => {
          categoryTabsEl.dataset.dragged = "false";
        }, 50);
      });

      categoryTabsEl.addEventListener("mousemove", (e) => {
        if (!isDown) return;
        const x = e.pageX - categoryTabsEl.offsetLeft;
        const walk = x - startX;
        if (Math.abs(walk) > 5) {
          categoryTabsEl.dataset.dragged = "true";
          categoryTabsEl.classList.add("dragging");
          e.preventDefault();
          categoryTabsEl.scrollLeft = scrollStart - walk;
        }
      });
    }

    // Sync full catalog from Supabase if remote has newer/more items
    fetch(SUPABASE_CATALOG_URL, {
      headers: {
        apikey: SUPABASE_ANON_KEY,
        Authorization: `Bearer ${SUPABASE_ANON_KEY}`
      }
    })
      .then((res) => (res.ok ? res.json() : null))
      .then((rows) => {
        if (Array.isArray(rows) && rows[0] && rows[0].payload && Array.isArray(rows[0].payload.files)) {
          const remoteConfig = rows[0].payload;
          if (remoteConfig.files.length >= config.files.length) {
            config = remoteConfig;
            window.SITE_CONFIG = remoteConfig;
            if (searchInputEl) {
              searchInputEl.placeholder = `Search ${config.files.length} files, drivers, SQL, Eplus versions, or passwords...`;
            }
            renderTabs();
            renderDirectory();
            applyView(currentView);
          }
        }
      })
      .catch(() => {
        // Offline or local fallback already rendered from data.js
      });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
