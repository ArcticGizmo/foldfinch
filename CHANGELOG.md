# Changelog

All notable changes to Foldfinch are documented here. The app embeds this file and shows
what's new after an update.

## [v0.1.0] - 2026-07-22

### Added
- Project scaffold: `Foldfinch.Core`, `Foldfinch.App`, and `Foldfinch.Tests` on .NET 10 + Avalonia 12.
- Light-themed application shell.
- Core PDF engine (`PdfDocumentModel` + `PdfEditor`): open, remove, reorder, rotate, combine, and
  save via PDFsharp, with a page model that spans multiple source files.
- App shell wired to the engine: Add PDF and Save As via native file pickers, with a status bar and
  busy/dirty tracking.
- Page thumbnail grid: pages render to cached thumbnails (PDFtoImage/PDFium), one tile per page in
  document order, respecting per-page rotation.
- Remove and reorder: multi-select (click / Ctrl+click / Shift+click), remove selected pages,
  drag-and-drop reordering with a live drop-position indicator, and multi-level undo/redo.
  Keyboard: Delete, Ctrl+A, Ctrl+Z, Ctrl+Y.
- Combine PDFs: a single Add PDF flow both opens the first file and combines more (no separate
  Open); add one or several files at once. A central Add PDF button appears when nothing is open,
  and a faint "+" before every page (and after the last) brightens on hover to insert PDF(s) at
  that exact spot. When a document spans multiple files,
  each page tile shows a colour-coded source chip so it's clear where every page came from.
- Rotate pages: rotate the selection clockwise/counter-clockwise (toolbar or Ctrl+R / Ctrl+Shift+R);
  thumbnails show rotated pages at the correct aspect ratio and the saved file reflects the rotation.
- Safer saving: Save As always prompts for a destination, so opened source files are never modified
  in place. Closing with unsaved changes prompts to save, discard, or cancel.
- Friendlier errors: password-protected and damaged PDFs report a clear message instead of failing.
- Packaging: Velopack-based installers for Windows and macOS via a tagged GitHub release, an
  update-available notice on launch, an About dialog with a manual update check, and a "what's new"
  window after updating. App icon added.
