# Changelog

All notable changes to Foldfinch are documented here. The app embeds this file and shows
what's new after an update.

## [Unreleased]

### Added
- Project scaffold: `Foldfinch.Core`, `Foldfinch.App`, and `Foldfinch.Tests` on .NET 10 + Avalonia 12.
- Light-themed application shell.
- Core PDF engine (`PdfDocumentModel` + `PdfEditor`): open, remove, reorder, rotate, combine, and
  save via PDFsharp, with a page model that spans multiple source files.
- App shell wired to the engine: Open, Add PDF, Save, and Save As via native file pickers, with a
  document summary, status bar, and busy/dirty tracking.
- Page thumbnail grid: pages render to cached thumbnails (PDFtoImage/PDFium), one tile per page in
  document order, respecting per-page rotation.
- Remove and reorder: multi-select (click / Ctrl+click / Shift+click), remove selected pages,
  drag-and-drop reordering, and multi-level undo/redo. Keyboard: Delete, Ctrl+A, Ctrl+Z, Ctrl+Y.
- Combine PDFs: Add PDF appends another file's pages; when a document spans multiple files, each
  page tile shows a colour-coded source chip so it's clear where every page came from.
