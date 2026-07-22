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
