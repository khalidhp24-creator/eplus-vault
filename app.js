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
  let currentLang = localStorage.getItem("eplus_lang_v1") || "en";
  let toastTimeout = null;

  // Bilingual UI Dictionary (en / ar)
  const I18N = {
    en: {
      siteTitle: "≡ EPLUS Software Center",
      pageTitleSuffix: "Software & Essential Files",
      siteBadge: "Directory",
      siteSubtitle:
        "Essential software packages, database upgrades, printer drivers, system utilities, and direct MediaFire archives.",
      rootFolderLabel: "Open Main MediaFire Folder",
      rootFolderTitle: "Open full #EPLUS MediaFire repository",
      toolkitBtn: "System Toolkit",
      toolkitTitle: "Download ePlus System Toolkit (Windows, run as Administrator)",
      helpBtn: "Help?",
      helpTitle: "Download Arabic guide for System Toolkit (24 commands)",
      logIn: "Log In",
      logInTitle: "Click to Log In and Unlock Catalog",
      logOut: "Log Out",
      logOutTitle: "Authenticated (Click to Log Out)",
      permTitle: "Permanent Direct Downloads",
      permBadge: "Hosted on Website",
      permDesc: "Instant high-speed local downloads · No waiting",
      cardHanyTitle: "HANY 2000 to 2008 Tool",
      cardSqlTitle: "SQL Server 2008 R2",
      cardVideoTitle: "SQL 2008 Training",
      pillVideoMp4: "Video MP4",
      pillWinTheme: "Win Theme",
      searchLocked: "Click or press / to log in and unlock directory...",
      searchUnlocked: (count) =>
        `Search ${count} files, drivers, SQL, Eplus versions, or passwords...`,
      tabAll: "All",
      viewGrid: "Grid",
      viewGridTitle: "Grid View",
      viewList: "List",
      viewListTitle: "Compact List View",
      actionDownload: "Download",
      actionDirectDownload: "Direct Download",
      actionOpenFolder: "Open Folder",
      actionMediafireMirror: "MediaFire Mirror",
      actionMediafireCloud: "MediaFire Cloud",
      actionDirectMediafire: "Direct MediaFire",
      actionChangelog: "Changelog",
      mediafireFolder: "MediaFire Folder",
      openFolderTooltip: (name) => `Open ${name} folder on MediaFire`,
      itemSingular: "item",
      itemPlural: "items",
      noteLabel: "Description / Note",
      fileSizeTitle: "File Size",
      pwCopyTitle: "Click to copy archive password",
      pwCopyLabel: (pw) => `password (${pw})`,
      shaCopyTitle: (hash) => `Click to copy full SHA-256: ${hash}`,
      shaCopyLabel: "SHA-256 checksum",
      copyLinkTitle: "Copy direct link",
      copyLinkLabel: (name) => `link for ${name}`,
      copiedToast: (label) => `Copied ${label} to clipboard`,
      emptyTitle: (q) => `No files found matching "${q}"`,
      emptySub: 'Try searching a different keyword or switching the category tab to "All".',
      footerTitle: "≡ EPLUS Software & Essential Files Directory",
      footerHint: "To add or update files, edit data.js",
      authTitle: "Restricted Catalog Access",
      authSubtitle: "Enter the master passcode to unlock search and view the full file catalog.",
      authPlaceholder: "Enter passcode",
      authSubmit: "Unlock Catalog",
      authErrEmpty: "Please enter the passcode.",
      authErrInvalid: "Incorrect passcode. Please try again.",
      toastUnlocked: "Access granted — Catalog unlocked!",
      toastLocked: "Logged out — Catalog locked.",
      confirmLogout: "Are you sure you want to log out and lock the catalog?",
      themeToLight: "Switch to light mode",
      themeToDark: "Switch to dark mode",
      catScrollLeft: "Scroll left",
      catScrollRight: "Scroll right",
      categories: {}
    },
    ar: {
      siteTitle: "≡ مركز برامج EPLUS",
      pageTitleSuffix: "البرامج والملفات الأساسية",
      siteBadge: "الدليل",
      siteSubtitle:
        "الحزم البرمجية الأساسية، وتحديثات قواعد البيانات، وتعريفات الطابعات، وأدوات صيانة النظام، وروابط التحميل المباشرة.",
      rootFolderLabel: "فتح مجلد MediaFire الرئيسي",
      rootFolderTitle: "فتح مستودع #EPLUS الكامل على MediaFire",
      toolkitBtn: "أداة النظام",
      toolkitTitle: "تحميل أداة ePlus System Toolkit (لويندوز — تشغيل كمسؤول)",
      helpBtn: "مساعدة؟",
      helpTitle: "تحميل دليل شرح أداة System Toolkit بالعربية (24 أمر)",
      logIn: "تسجيل الدخول",
      logInTitle: "اضغط لتسجيل الدخول وفتح دليل الملفات",
      logOut: "تسجيل الخروج",
      logOutTitle: "تم التحقق (اضغط لتسجيل الخروج)",
      permTitle: "التحميلات المباشرة السريعة",
      permBadge: "مُستضاف على الموقع",
      permDesc: "تحميل مباشر فائق السرعة · بدون انتظار",
      cardHanyTitle: "أداة الترقية HANY 2000 إلى 2008",
      cardSqlTitle: "خادم SQL Server 2008 R2",
      cardVideoTitle: "فيديو تدريب SQL 2008",
      pillVideoMp4: "فيديو MP4",
      pillWinTheme: "ثيم ويندوز",
      searchLocked: "اضغط هنا أو اضغط / لتسجيل الدخول وفتح دليل الملفات...",
      searchUnlocked: (count) =>
        `ابحث في ${count} ملف، أو تعريفات الطابعات، أو SQL، أو إصدارات Eplus، أو كلمات المرور...`,
      tabAll: "الكل",
      viewGrid: "شبكة",
      viewGridTitle: "عرض الشبكة",
      viewList: "قائمة",
      viewListTitle: "عرض القائمة المضغوط",
      actionDownload: "تحميل",
      actionDirectDownload: "تحميل مباشر",
      actionOpenFolder: "فتح المجلد",
      actionMediafireMirror: "رابط MediaFire بديل",
      actionMediafireCloud: "سحابة MediaFire",
      actionDirectMediafire: "رابط MediaFire مباشر",
      actionChangelog: "سجل التحديث",
      mediafireFolder: "مجلد MediaFire",
      openFolderTooltip: (name) => `فتح مجلد ${name} على MediaFire`,
      itemSingular: "ملف",
      itemPlural: "ملفات",
      noteLabel: "الوصف / ملاحظة",
      fileSizeTitle: "حجم الملف",
      pwCopyTitle: "اضغط لنسخ كلمة مرور الأرشيف",
      pwCopyLabel: (pw) => `كلمة المرور (${pw})`,
      shaCopyTitle: (hash) => `اضغط لنسخ بصمة SHA-256 الكاملة: ${hash}`,
      shaCopyLabel: "بصمة التحقق SHA-256",
      copyLinkTitle: "نسخ الرابط المباشر",
      copyLinkLabel: (name) => `رابط ${name}`,
      copiedToast: (label) => `تم نسخ ${label} إلى الحافظة`,
      emptyTitle: (q) => `لا توجد ملفات مطابقة للبحث "${q}"`,
      emptySub: 'جرّب البحث بكلمة أخرى أو التبديل إلى تبويب "الكل".',
      footerTitle: "≡ دليل برامج وملفات EPLUS الأساسية",
      footerHint: "لإضافة أو تحديث الملفات، قم بتعديل data.js",
      authTitle: "الدخول إلى دليل الملفات المحمي",
      authSubtitle: "أدخل رمز المرور الرئيسي لفتح البحث وعرض فهرس الملفات بالكامل.",
      authPlaceholder: "أدخل رمز المرور",
      authSubmit: "فتح الدليل",
      authErrEmpty: "يرجى إدخال رمز المرور.",
      authErrInvalid: "رمز المرور غير صحيح، يرجى المحاولة مرة أخرى.",
      toastUnlocked: "تم السماح بالدخول — تم فتح دليل الملفات!",
      toastLocked: "تم تسجيل الخروج — تم قفل دليل الملفات.",
      confirmLogout: "هل أنت متأكد من تسجيل الخروج وقفل دليل الملفات؟",
      themeToLight: "التبديل إلى الوضع الفاتح",
      themeToDark: "التبديل إلى الوضع الداكن",
      catScrollLeft: "تمرير لليسار",
      catScrollRight: "تمرير لليمين",
      categories: {
        "whats-new": {
          name: "ما الجديد (سجل التحديثات والإصدارات)",
          description: "سجل التغييرات وملاحظات الإصدارات التفصيلية لكل تحديث من تحديثات برنامج e-Plus الصيدلي."
        },
        "eplus-db": {
          name: "EPLUS وقواعد البيانات",
          description: "تحديثات برنامج EPLUS الأساسية، وأدوات الإدارة، وحزم الترقية إلى SQL Server 2008 R2، وقوالب قواعد البيانات النظيفة."
        },
        "eplus-core": {
          name: "مجلد Eplus الأساسي",
          description: "جميع الملفات داخل مجلد Eplus الرئيسي (SQL 2000 و SP4، وإصدارات E-plus، و VNC، و Radmin VPN، وسكربتات الإعدادات الإقليمية)."
        },
        "eplus-116": {
          name: "إصدار E-plus 13.0.116",
          description: "ملفات حزمة E-plus+13.0.116 ومجلداتها الفرعية (Admin Support و DBCheckerService و Replication والفاتورة الإلكترونية)."
        },
        utilities: {
          name: "الأدوات الأساسية والشبكة",
          description: "إصلاحات مشاركة الطابعات، وأدوات الشبكة و SMB، وفحص المنافذ، وأدوات الصيانة السريعة."
        },
        "software-os": {
          name: "البرامج وأنظمة التشغيل (ISO)",
          description: "حزم التثبيت، وأدوات استعادة النظام، وبرامج حرق الفلاشات، وصور أنظمة ويندوز ISO."
        },
        "printer-drivers": {
          name: "تعريفات وأدوات الطابعات",
          description: "تعريفات طابعات الفواتير والباركود (Xprinter و Zebra و Bixolon و Epson و Star و Honeywell و GoDEX)."
        },
        "other-tools": {
          name: "أدوات وموارد إضافية",
          description: "مجلدات SQL الكاملة، والأدوات المساعدة المباشرة، وملفات الماسحات الضوئية والباركود من مجلد OTHER."
        },
        future2010: {
          name: "برنامج FUTURE2010",
          description: "جميع ملفات وحزم وإصدارات FUTURE2010 وملفات الإعداد والتشغيل."
        },
        folders: {
          name: "مجلدات MediaFire الفرعية الأخرى",
          description: "روابط مباشرة لبقية المجلدات الفرعية داخل مستودع #EPLUS على MediaFire."
        },
        media: {
          name: "أرشيف الصوتيات والوسائط",
          description: "التلاوات الصوتية وحزم الوسائط المضغوطة."
        }
      }
    }
  };

  function t(key) {
    const dict = I18N[currentLang] || I18N.en;
    return dict[key] !== undefined ? dict[key] : I18N.en[key];
  }

  function getCategoryText(cat) {
    if (currentLang === "ar" && I18N.ar.categories[cat.id]) {
      return I18N.ar.categories[cat.id];
    }
    return { name: cat.name, description: cat.description || "" };
  }

  function translateActionLabel(rawLabel, isFolder) {
    if (currentLang !== "ar") {
      return rawLabel || (isFolder ? t("actionOpenFolder") : t("actionDownload"));
    }
    switch (rawLabel) {
      case "Download":
        return t("actionDownload");
      case "Direct Download":
        return t("actionDirectDownload");
      case "Open Folder":
        return t("actionOpenFolder");
      case "MediaFire Mirror":
        return t("actionMediafireMirror");
      case "MediaFire Cloud":
        return t("actionMediafireCloud");
      case "Direct MediaFire":
        return t("actionDirectMediafire");
      case "Changelog":
        return t("actionChangelog");
      default:
        return isFolder ? t("actionOpenFolder") : t("actionDownload");
    }
  }

  // Master Passcode Auth Gate (SHA-256 hash of 'eplus2026')
  const MASTER_HASH = "abb48cfb7c3ef837d77c518a26ef8bcf949b82d74ca1b8c5db3623c8fbfe205e";
  const AUTH_STORAGE_KEY = "eplus_auth_verified_v1";
  let isAuthenticated = localStorage.getItem(AUTH_STORAGE_KEY) === "true";

  // DOM Elements
  const brandTitleEl = document.getElementById("brand-title");
  const brandBadgeEl = document.getElementById("brand-badge");
  const heroTitleEl = document.getElementById("hero-title");
  const heroSubtitleEl = document.getElementById("hero-subtitle");
  const rootFolderLinkEl = document.getElementById("root-folder-link");
  const rootFolderLabelEl = document.getElementById("root-folder-label");
  const toolkitDownloadBtn = document.getElementById("toolkit-download-btn");
  const toolkitHelpBtn = document.getElementById("toolkit-help-btn");
  const langToggleBtn = document.getElementById("lang-toggle-btn");
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
  const authToggleBtn = document.getElementById("auth-toggle-btn");
  const authBtnLabel = document.getElementById("auth-btn-label");
  const authModalBackdrop = document.getElementById("auth-modal-backdrop");
  const authModalCloseBtn = document.getElementById("auth-modal-close-btn");
  const authModalCard = authModalBackdrop ? authModalBackdrop.querySelector(".auth-modal-card") : null;
  const authForm = document.getElementById("auth-form");
  const authPasscodeInput = document.getElementById("auth-passcode-input");
  const authVisibilityBtn = document.getElementById("auth-visibility-btn");
  const authErrorMsg = document.getElementById("auth-error-msg");

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

  const SKIPPED_EXTENSIONS = [".txt", ".xls", ".xlsx"];

  function isSkippedFile(item) {
    if (!item) return false;
    const filename = String(item.filename || "").trim().toLowerCase();
    const name = String(item.name || "").trim().toLowerCase();
    const url = String(item.url || "").trim().toLowerCase().split("?")[0];
    return SKIPPED_EXTENSIONS.some(
      (ext) =>
        filename.endsWith(ext) ||
        name.endsWith(ext) ||
        url.endsWith(ext) ||
        url.endsWith(ext + "/file")
    );
  }

  function matchesSearch(item, q) {
    if (isSkippedFile(item)) return false;
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
    const msg = t("copiedToast")(label);
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(() => {
        showToast(msg);
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
        showToast(msg);
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
      const themeLabel = currentTheme === "dark" ? t("themeToLight") : t("themeToDark");
      themeToggleBtn.setAttribute("aria-label", themeLabel);
      themeToggleBtn.setAttribute("title", themeLabel);
    }
  }

  // Apply Language (en / ar)
  function applyLanguage(lang) {
    currentLang = lang === "ar" ? "ar" : "en";
    localStorage.setItem("eplus_lang_v1", currentLang);
    document.documentElement.setAttribute("lang", currentLang);
    document.documentElement.setAttribute("dir", currentLang === "ar" ? "rtl" : "ltr");

    if (langToggleBtn) {
      const enSign = langToggleBtn.querySelector('[data-lang-sign="en"]');
      const arSign = langToggleBtn.querySelector('[data-lang-sign="ar"]');
      if (enSign) enSign.classList.toggle("active", currentLang === "en");
      if (arSign) arSign.classList.toggle("active", currentLang === "ar");
    }

    document.querySelectorAll("[data-i18n]").forEach((el) => {
      const key = el.getAttribute("data-i18n");
      const val = t(key);
      if (typeof val === "string") {
        el.textContent = val;
      }
    });

    const titleText = t("siteTitle");
    if (brandTitleEl) brandTitleEl.textContent = titleText;
    if (heroTitleEl) heroTitleEl.textContent = titleText;
    document.title = `${titleText} — ${t("pageTitleSuffix")}`;
    if (brandBadgeEl) brandBadgeEl.textContent = t("siteBadge");
    if (heroSubtitleEl) heroSubtitleEl.textContent = t("siteSubtitle");
    if (rootFolderLabelEl) rootFolderLabelEl.textContent = t("rootFolderLabel");
    if (rootFolderLinkEl) rootFolderLinkEl.title = t("rootFolderTitle");
    if (toolkitDownloadBtn) toolkitDownloadBtn.title = t("toolkitTitle");
    if (toolkitHelpBtn) toolkitHelpBtn.title = t("helpTitle");
    document.querySelectorAll(".perm-card-help-btn").forEach((btn) => {
      btn.title = t("helpTitle");
    });
    if (viewGridBtn) viewGridBtn.title = t("viewGridTitle");
    if (viewListBtn) viewListBtn.title = t("viewListTitle");
    if (catScrollLeftBtn) catScrollLeftBtn.title = t("catScrollLeft");
    if (catScrollRightBtn) catScrollRightBtn.title = t("catScrollRight");
    if (authPasscodeInput) authPasscodeInput.placeholder = t("authPlaceholder");

    applyTheme(currentTheme);
    updateAuthUI();

    if (isAuthenticated) {
      renderTabs();
      renderDirectory();
      applyView(currentView);
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
    const maxScroll = Math.max(0, categoryTabsEl.scrollWidth - categoryTabsEl.clientWidth);
    const pos = Math.abs(categoryTabsEl.scrollLeft);
    if (document.documentElement.dir === "rtl") {
      if (catScrollRightBtn) catScrollRightBtn.disabled = pos <= 2;
      if (catScrollLeftBtn) catScrollLeftBtn.disabled = pos >= maxScroll - 2;
    } else {
      if (catScrollLeftBtn) catScrollLeftBtn.disabled = pos <= 2;
      if (catScrollRightBtn) catScrollRightBtn.disabled = pos >= maxScroll - 2;
    }
  }

  // Render Category Filter Tabs
  function renderTabs() {
    const prevScrollLeft = categoryTabsEl ? categoryTabsEl.scrollLeft : 0;
    const matchingFiles = config.files.filter((f) => matchesSearch(f, searchQuery));
    const totalCount = matchingFiles.length;

    let html = `
      <button type="button" class="tab-btn ${activeCategory === "all" ? "active" : ""}" data-cat="all">
        <span>${escapeHtml(t("tabAll"))}</span>
        <span class="tab-count mono">${totalCount}</span>
      </button>
    `;

    config.categories.forEach((cat) => {
      const count = matchingFiles.filter((f) => f.category === cat.id).length;
      if (count === 0 && !searchQuery) return;
      const isActive = activeCategory === cat.id ? "active" : "";
      const catText = getCategoryText(cat);
      html += `
        <button type="button" class="tab-btn ${isActive}" data-cat="${escapeHtml(cat.id)}">
          <span>${escapeHtml(catText.name)}</span>
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
    const actionLabel = translateActionLabel(item.urlLabel, isFolder);

    // Metadata Pills (Size, Password, Checksum)
    let metaPillsHtml = "";

    if (item.size && item.size !== "Subfolder") {
      metaPillsHtml += `<span class="meta-pill mono" title="${escapeHtml(t("fileSizeTitle"))}">${escapeHtml(item.size)}</span>`;
    }

    if (item.password) {
      metaPillsHtml += `
        <button type="button" class="meta-pill pw-pill mono" data-copy="${escapeHtml(item.password)}" data-copy-label="${escapeHtml(t("pwCopyLabel")(item.password))}" title="${escapeHtml(t("pwCopyTitle"))}">
          ${ICONS.key}
          <span>PW: ${escapeHtml(item.password)}</span>
        </button>
      `;
    }

    if (item.checksum) {
      const shortHash = item.checksum.slice(0, 8) + "…";
      metaPillsHtml += `
        <button type="button" class="meta-pill mono" data-copy="${escapeHtml(item.checksum)}" data-copy-label="${escapeHtml(t("shaCopyLabel"))}" title="${escapeHtml(t("shaCopyTitle")(item.checksum))}">
          ${ICONS.hash}
          <span>SHA256: ${escapeHtml(shortHash)}</span>
        </button>
      `;
    }

    // Mirror links
    let mirrorsHtml = "";
    if (Array.isArray(item.mirrors) && item.mirrors.length > 0) {
      mirrorsHtml = item.mirrors
        .map((m) => {
          const mirrorUrl = typeof m === "string" ? m : m.url;
          const mirrorRawLabel = typeof m === "string" ? "MediaFire Mirror" : m.label;
          const mirrorLabel = translateActionLabel(mirrorRawLabel, false);
          return `
          <a href="${escapeHtml(mirrorUrl)}" target="_blank" rel="noopener noreferrer" class="btn-mirror" title="${escapeHtml(mirrorLabel)}">
            ${ICONS.download}
            <span>${escapeHtml(mirrorLabel)}</span>
          </a>
        `;
        })
        .join("");
    }

    const floatingNoteHtml = item.description
      ? `
        <div class="floating-note" role="tooltip">
          <span class="floating-note-label mono">${escapeHtml(t("noteLabel"))}</span>
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
            <button type="button" class="btn-copy-link" data-copy="${escapeHtml(item.url.startsWith("http") ? item.url : window.location.origin + window.location.pathname.replace(/[^/]*$/, '') + item.url)}" data-copy-label="${escapeHtml(t("copyLinkLabel")(item.name))}" title="${escapeHtml(t("copyLinkTitle"))}">
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

      const catText = getCategoryText(cat);
      const itemsHtml = items.map(renderFileItem).join("");
      const folderLinkHtml = cat.folderUrl
        ? `<a href="${escapeHtml(cat.folderUrl)}" target="_blank" rel="noopener noreferrer" class="section-folder-link mono" title="${escapeHtml(t("openFolderTooltip")(catText.name))}">${ICONS.external} <span>${escapeHtml(t("mediafireFolder"))}</span></a>`
        : "";

      const countWord = items.length === 1 ? t("itemSingular") : t("itemPlural");

      sectionsHtml += `
        <section class="category-section" id="section-${escapeHtml(cat.id)}">
          <div class="section-header">
            <div class="section-title-wrap">
              <h2 class="section-title">${escapeHtml(catText.name)}</h2>
              ${catText.description ? `<span class="section-desc">${escapeHtml(catText.description)}</span>` : ""}
            </div>
            <div class="section-meta-right">
              ${folderLinkHtml}
              <span class="section-count mono">${items.length} ${escapeHtml(countWord)}</span>
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
          <div class="empty-search-title">${escapeHtml(t("emptyTitle")(escapeHtml(searchQuery)))}</div>
          <div class="empty-search-sub">${escapeHtml(t("emptySub"))}</div>
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
  // SHA-256 helper using subtle crypto API with fallback
  async function sha256Hex(str) {
    if (window.crypto && crypto.subtle && window.TextEncoder) {
      try {
        const buffer = new TextEncoder().encode(str);
        const hashBuffer = await crypto.subtle.digest("SHA-256", buffer);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        return hashArray.map((b) => b.toString(16).padStart(2, "0")).join("");
      } catch (err) {
        // Fall back below
      }
    }
    // Simple fast fallback hash for compatibility
    return (str === "eplus2026") ? MASTER_HASH : "invalid";
  }

  function updateAuthUI() {
    if (isAuthenticated) {
      document.body.classList.remove("unauthenticated");
      if (authToggleBtn) {
        authToggleBtn.classList.add("authenticated");
        authToggleBtn.title = t("logOutTitle");
        const lockIcon = authToggleBtn.querySelector(".auth-lock-icon");
        const unlockIcon = authToggleBtn.querySelector(".auth-unlock-icon");
        if (lockIcon) lockIcon.style.display = "none";
        if (unlockIcon) unlockIcon.style.display = "inline-block";
      }
      if (authBtnLabel) authBtnLabel.textContent = t("logOut");
      if (searchInputEl) {
        searchInputEl.placeholder = t("searchUnlocked")(config.files.length);
        searchInputEl.readOnly = false;
      }
    } else {
      document.body.classList.add("unauthenticated");
      if (authToggleBtn) {
        authToggleBtn.classList.remove("authenticated");
        authToggleBtn.title = t("logInTitle");
        const lockIcon = authToggleBtn.querySelector(".auth-lock-icon");
        const unlockIcon = authToggleBtn.querySelector(".auth-unlock-icon");
        if (lockIcon) lockIcon.style.display = "inline-block";
        if (unlockIcon) unlockIcon.style.display = "none";
      }
      if (authBtnLabel) authBtnLabel.textContent = t("logIn");
      if (searchInputEl) {
        searchInputEl.placeholder = t("searchLocked");
        searchInputEl.readOnly = true;
      }
    }
  }

  function openAuthModal() {
    if (authModalBackdrop) {
      authModalBackdrop.hidden = false;
      if (authErrorMsg) {
        authErrorMsg.style.display = "none";
        authErrorMsg.textContent = "";
      }
      if (authPasscodeInput) {
        authPasscodeInput.value = "";
        setTimeout(() => authPasscodeInput.focus(), 80);
      }
    }
  }

  function closeAuthModal() {
    if (authModalBackdrop) {
      authModalBackdrop.hidden = true;
      if (authPasscodeInput) authPasscodeInput.blur();
    }
  }

  async function handleAuthSubmit() {
    if (!authPasscodeInput) return;
    const passcode = authPasscodeInput.value.trim();
    if (!passcode) {
      showAuthError(t("authErrEmpty"));
      return;
    }

    const hashed = await sha256Hex(passcode);
    if (hashed === MASTER_HASH || passcode === "eplus2026") {
      isAuthenticated = true;
      localStorage.setItem(AUTH_STORAGE_KEY, "true");
      updateAuthUI();
      closeAuthModal();
      showToast(t("toastUnlocked"));
      renderTabs();
      renderDirectory();
      applyView(currentView);
      if (searchInputEl) {
        searchInputEl.focus();
      }
    } else {
      showAuthError(t("authErrInvalid"));
    }
  }

  function showAuthError(msg) {
    if (authErrorMsg) {
      authErrorMsg.textContent = msg;
      authErrorMsg.style.display = "block";
    }
    if (authModalCard) {
      authModalCard.classList.remove("shake");
      void authModalCard.offsetWidth; // Force reflow
      authModalCard.classList.add("shake");
    }
    if (authPasscodeInput) {
      authPasscodeInput.select();
    }
  }

  function handleLogout() {
    isAuthenticated = false;
    localStorage.removeItem(AUTH_STORAGE_KEY);
    updateAuthUI();
    showToast(t("toastLocked"));
  }

  function init() {
    // 0. Exclude skipped file extensions (.txt, .xls, .xlsx) from catalog & search
    if (Array.isArray(config.files)) {
      config.files = config.files.filter((f) => !isSkippedFile(f));
    }

    // 1. Auth Toggle Button in Topbar
    if (authToggleBtn) {
      authToggleBtn.addEventListener("click", () => {
        if (isAuthenticated) {
          if (confirm(t("confirmLogout"))) {
            handleLogout();
          }
        } else {
          openAuthModal();
        }
      });
    }

    // 2. Language Switcher Button in Topbar (℮ / ؏)
    if (langToggleBtn) {
      langToggleBtn.addEventListener("click", () => {
        applyLanguage(currentLang === "ar" ? "en" : "ar");
      });
    }

    // 3. Auth Modal Dialog Events
    if (authModalCloseBtn) {
      authModalCloseBtn.addEventListener("click", closeAuthModal);
    }
    if (authModalBackdrop) {
      authModalBackdrop.addEventListener("click", (e) => {
        if (e.target === authModalBackdrop) {
          closeAuthModal();
        }
      });
    }
    if (authForm) {
      authForm.addEventListener("submit", (e) => {
        e.preventDefault();
        handleAuthSubmit();
      });
    }
    if (authVisibilityBtn && authPasscodeInput) {
      authVisibilityBtn.addEventListener("click", () => {
        const isPwd = authPasscodeInput.type === "password";
        authPasscodeInput.type = isPwd ? "text" : "password";
        const iconEye = authVisibilityBtn.querySelector(".icon-eye");
        const iconEyeOff = authVisibilityBtn.querySelector(".icon-eye-off");
        if (iconEye) iconEye.style.display = isPwd ? "none" : "block";
        if (iconEyeOff) iconEyeOff.style.display = isPwd ? "block" : "none";
      });
    }

    if (rootFolderLinkEl && config.site.rootFolderUrl) {
      rootFolderLinkEl.href = config.site.rootFolderUrl;
    }

    applyLanguage(currentLang);

    // Search Bar Gate: intercept clicks and inputs when unauthenticated
    if (searchInputEl) {
      searchInputEl.addEventListener("click", (e) => {
        if (!isAuthenticated) {
          e.preventDefault();
          openAuthModal();
        }
      });

      searchInputEl.addEventListener("keydown", (e) => {
        if (!isAuthenticated) {
          if (e.key !== "Tab") {
            e.preventDefault();
            openAuthModal();
          }
        }
      });

      searchInputEl.addEventListener("input", () => {
        if (!isAuthenticated) {
          searchInputEl.value = "";
          openAuthModal();
          return;
        }
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

    // Keyboard shortcut '/' or 'Ctrl+K' to focus search bar or trigger auth
    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape" && authModalBackdrop && !authModalBackdrop.hidden) {
        closeAuthModal();
        return;
      }

      if (!searchInputEl) return;
      const isInputFocused =
        document.activeElement === searchInputEl ||
        ["INPUT", "TEXTAREA"].includes(document.activeElement?.tagName);

      if ((e.key === "/" && !isInputFocused) || ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k")) {
        e.preventDefault();
        if (!isAuthenticated) {
          openAuthModal();
          return;
        }
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
          if (remoteConfig.site) {
            remoteConfig.site.title = "≡ EPLUS Software Center";
          }
          remoteConfig.files = remoteConfig.files.filter((f) => !isSkippedFile(f));
          if (remoteConfig.files.length >= config.files.length) {
            config = remoteConfig;
            window.SITE_CONFIG = remoteConfig;
            if (isAuthenticated) {
              if (searchInputEl) {
                searchInputEl.placeholder = t("searchUnlocked")(config.files.length);
              }
              renderTabs();
              renderDirectory();
              applyView(currentView);
            }
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
