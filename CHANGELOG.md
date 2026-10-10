# Changelog

## 1.9.2 — 2026-10-10

- Reserved explicit footer height in the supported-GPU, game-catalogue, and discovery dialogs so action buttons remain fully inside the window.
- Added visual regression checks that validate every dialog button against both the window and each parent container.
- Rebuilt the lower-left language control with a clear **Language** label, selected value, bordered field, and always-visible down-arrow menu.
- Added real `PrintWindow` captures for all three dialogs to catch non-client/layout clipping.

## 1.9.1 — 2026-10-10

- Localized **Check Selected Game Online**: Turkish UI now opens Google with Turkish language/region settings and Turkish compatibility/setup terms; English UI keeps an English query.
- Gave inactive sidebar and secondary action buttons a visible light fill and border while preserving purple for the active/primary state.
- Added regression checks for both localized search URLs and the visual distinction of inactive navigation buttons.

## 1.9.0 — 2026-10-10

- Expanded automatic discovery beyond Steam and Epic to Xbox/Microsoft Store, GOG, EA app, Ubisoft Connect, Battle.net, Rockstar, Amazon Games, Riot Games, itch.io, and common local game-library roots.
- Added Xbox `MicrosoftGame.config` parsing with secure XML settings and executable validation.
- Added GOG-specific and Windows installed-program registry discovery for launcher-managed games.
- Added orphaned/copy-installed Steam `common` folder discovery even when no current appmanifest exists.
- Added a persistent **Scan Folder** action for launcher-free, portable, and custom-location game libraries.
- Added false-positive filters for launchers, engines, editors, anti-cheat services, SDKs, redistributables, and helper executables.
- Improved Epic executable selection so launcher EXEs are rejected in favor of the real x64 render executable.
- Verified the expanded scan against the local machine: Steam, Epic, and Riot games were found while Unreal Engine, Quixel/Fab tools, Riot Vanguard, and Ubisoft anti-cheat were excluded.
- Rebalanced the selected-game area to 40% and the game library to 60% of the available content height.
- Removed the manual artwork-refresh action; official Steam CDN artwork now resolves and caches automatically.
- Added executable metadata and installation-folder aliases to Steam artwork matching.
- Added automatically generated local covers using the game title and executable icon when Steam artwork is unavailable.
- Added minimum-window regression checks for both artwork settings, library height, and all three action labels.

## 1.8.0 — 2026-10-10

- Removed binary rounded Win32 regions and translucent edge pens that produced black, broken card borders at real Windows DPI scales.
- Made rounded controls paint their parent surface before rendering, eliminating the black hooks/lines around buttons seen in the live application.
- Added an actual Win32 `PrintWindow` visual regression capture in addition to the existing bitmap preview.
- Added progressive background loading for missing library artwork with a two-request concurrency limit.
- Added official Steam CDN fallback paths used by SteamDB without scraping SteamDB, whose policy disallows automated crawling.
- Made a new game scan restore missing Steam AppIDs on games already in the library.

## 1.7.0 — 2026-10-10

- Rebuilt the main launcher composition to match the supplied references more closely.
- Moved the selected game into a full-width top hero with identity, installation state, Frame Gen settings, and vertical launcher actions.
- Moved the library below the hero and expanded it to a responsive 3–5 column artwork gallery.
- Replaced the stacked library heading/search layout with a compact single-row launcher toolbar.
- Added compact card sizing for 980×680 while keeping titles, status, MFG level, and scrolling available.
- Fixed artwork clipping so wide images cannot paint behind card titles and status text.

## 1.6.0 — 2026-10-09

- Replaced the dark theme with a modern light launcher palette across the main window and all dialogs.
- Added a light Windows title bar, white sidebar, pale cards and inputs, and high-contrast typography.
- Kept full-color game artwork visible beneath a left-to-right readability wash in the hero surface.
- Restored visible vertical scroll indicators now that native light scrollbars match the interface.
- Updated secondary and destructive actions for correct contrast without dark or reddish borders.

## 1.5.0 — 2026-10-09

- Rebuilt the game library as a responsive visual card grid inspired by modern desktop launchers.
- Reworked the selected-game surface into a wider hero presentation with cached artwork visible during refresh.
- Made Launch Game a full-width primary action, with Install/Update and Remove Frame Gen Files on a stable second row.
- Verified all action labels at 980×680 and 1360×820 window sizes.
- Removed reddish accent borders and replaced gradient-heavy buttons with clean flat surfaces.
- Added resilient Steam artwork fallback: background raw → background → header image → existing local cache.
- Kept card scrolling functional while hiding bright native scrollbars.

## 1.4.2 — 2026-10-09

- Fixed mouse-wheel and Page Up/Page Down scrolling in the game library while retaining the dark, scrollbar-free surface.
- Prefer Steam's high-resolution wide background artwork and fall back to the header or existing cache when required.
- Confined wide artwork to a proportional hero band instead of heavily zooming it across the full details card.
- Prevented long game names and executable paths from expanding the details panel or pushing controls outside the window.
- Stabilized the artwork refresh and bottom action button sizes at the minimum supported window size.

## 1.4.1 — 2026-10-07

- Increased the library area to nearly half of the workspace and rebalanced the hero panel.
- Fixed rounded-card clipping and inconsistent corner edges.
- Replaced bright native search fields with rounded dark search controls.
- Removed bright native scrollbars from the library and catalogue surfaces while preserving mouse-wheel and keyboard scrolling.
- Improved library column proportions, row spacing, button sizing, source-folder presentation, and responsive action layout.
- Separated informational accents from installation states with calmer blue, purple, green, amber, and red semantics.
- Added Windows 11 caption, border, and title text colors for a more consistent dark frame.

## 1.4.0 — 2026-10-07

- Rebuilt the full application shell as a game launcher instead of a conventional utility window.
- Added a persistent left navigation rail for the library, catalogue, supported GPUs, package controls, safety warning, and language selector.
- Added a searchable game library card with responsive columns and larger rows.
- Expanded the selected-game artwork into a full hero panel with launcher-style overlays and prominent actions.
- Added purple/indigo ambient gradients, glass-like rounded cards, updated list selection styling, and consistent spacing.
- Restyled the game discovery, supported-GPU, and Frame Generation catalogue windows to match the launcher theme.
- Kept full action labels visible at the minimum 1100×720 window size.

## 1.3.0 — 2026-10-07

- Redesigned the main window with a richer dark palette, rounded gradient buttons, clearer status colors, and improved spacing.
- Added an optional selected-game hero background sourced from Steam artwork.
- Added safe HTTPS host validation, image validation, local artwork caching, refresh control, and an offline fallback background.
- Preserved full Install / Update, Remove Frame Gen Files, and Launch Game labels at the minimum supported window width.
- Added persisted artwork preference and retained Steam AppIDs found during automatic discovery.
- Updated Turkish and English documentation with artwork privacy and cache details.

## 1.2.0 — 2026-09-24

- Added a consistent dark theme to the main window and dialogs.
- Added a responsive action area: full button labels stack vertically on narrow windows and return to one row when space is available.
- Kept the full **Install / Update**, **Remove Frame Gen Files**, and **Launch Game** labels visible.
- Added automatic Steam/Epic game discovery and render-EXE selection.
- Added searchable supported-GPU and Frame Generation game windows.
- Added Turkish and English localization.
- Added install, hash verification, repair, backup, removal, and original-file restoration workflows.
- Added explicit online/anti-cheat warnings and online compatibility verification prompts.
- Added Assassin's Creed Black Flag Resynced to the informational catalog and clarified that the catalog is never authoritative.
