# Foldfinch — Implementation Plan

A cross-platform desktop app for basic PDF operations: **remove pages**, **combine
multiple PDFs into one**, and **reorder pages graphically**. Built to mirror the
conventions of the sibling `sprig` app.

---

## 1. Decisions locked in

| Area | Decision |
| --- | --- |
| **Platforms** | Windows + macOS (structure stays portable; Linux can be added later with no code change, only a runtime + CI target). |
| **UI stack** | .NET 10, Avalonia 12, `CommunityToolkit.Mvvm`, Fluent theme, Inter font — same as sprig. |
| **Theme** | **Light mode** (PDFs are usually white). Light palette defined in `App.axaml`. |
| **PDF manipulation** | **PDFsharp 6** (MIT) — merge, remove, reorder, rotate, save. |
| **PDF rendering** | **PDFtoImage 5** (MIT) → **PDFium** (BSD-3-Clause) + **SkiaSharp** (MIT) for page thumbnails. |
| **v1 operations** | Remove pages, combine PDFs, reorder pages, **rotate pages**. |
| **Deferred** | Extract/split to a new PDF, encrypted/password-protected PDFs (documented as future milestones). |
| **Distribution** | Velopack auto-update + a `release.yml` GitHub Actions workflow (matches sprig). |

### Licensing note (all permissive — safe to distribute a closed-source app)

| Component | License | Notes |
| --- | --- | --- |
| PDFsharp 6.2.x | **MIT** | No dual-license, no commercial threshold. |
| PDFtoImage 5.x | **MIT** | Thin wrapper over the two below. |
| PDFium (native) | **BSD-3-Clause** | Google/Chromium PDF engine, shipped as native binaries per-RID. |
| SkiaSharp | **MIT** | Also Avalonia's own render backend. |
| Avalonia / CommunityToolkit.Mvvm / Velopack | MIT | Same as sprig. |

A `THIRD-PARTY-NOTICES.md` will collect these attributions (done in M0).

---

## 2. Project layout (mirrors sprig)

```
foldfinch/
├─ foldfinch.slnx
├─ run.bat                         # dotnet run --project src\Foldfinch.App
├─ CHANGELOG.md                    # embedded into the app (see sprig)
├─ THIRD-PARTY-NOTICES.md
├─ README.md
├─ docs/
│  └─ implementation-plan.md       # this file
├─ .github/workflows/release.yml
├─ src/
│  ├─ Foldfinch.Core/              # PDF logic — NO Avalonia reference
│  │  ├─ Pdf/PdfDocumentModel.cs   # in-memory model: ordered list of page refs
│  │  ├─ Pdf/PdfEditor.cs          # remove / combine / reorder / rotate / save
│  │  ├─ Pdf/PageRef.cs            # (source file, source index, rotation)
│  │  ├─ Pdf/IPdfRenderer.cs       # thumbnail rendering abstraction
│  │  └─ Foldfinch.Core.csproj
│  └─ Foldfinch.App/               # Avalonia MVVM UI
│     ├─ App.axaml (+ .cs)         # light theme + ViewLocator
│     ├─ Program.cs                # Velopack hook + `render` command
│     ├─ AppServices.cs            # composition root
│     ├─ ViewLocator.cs
│     ├─ Rendering/                # PdfiumRenderer + HeadlessRenderer
│     ├─ ViewModels/
│     └─ Views/
└─ tests/
   └─ Foldfinch.Tests/            # xUnit, references Core + App
```

**Golden rule (from sprig):** all real logic lives in `Foldfinch.Core` and is unit-tested;
the App layer only binds view-models to Core and runs blocking calls off the UI thread via
`AppServices.RunAsync`.

---

## 3. Domain model

The editor is document-centric. The user opens one or more source PDFs; the working set is
an **ordered list of `PageRef`** that can span multiple source documents.

```
PageRef        { SourceFileId, SourcePageIndex, Rotation (0/90/180/270) }
PdfDocumentModel { IReadOnlyList<SourceFile> Sources; List<PageRef> Pages }
```

- **Remove** = drop `PageRef`s from the list.
- **Reorder** = move `PageRef`s within the list.
- **Combine** = append another source's pages as new `PageRef`s.
- **Rotate** = mutate a `PageRef.Rotation`.
- **Save** = PDFsharp walks the ordered `PageRef` list, importing each page from its source
  (applying rotation) into a fresh output document.

This model makes every operation a pure list transform (trivially testable), with a single
save step that touches PDFsharp. Rendering is keyed on `(SourceFileId, SourcePageIndex)` so
thumbnails cache well and survive reordering.

---

## 4. Milestones

Each milestone is independently shippable/demoable and ends with a green build + tests.
A headless PNG render (`foldfinch-gui render ./captures`) is the visual acceptance gate for
every UI milestone, exactly like sprig.

### M0 — Scaffold & conventions  *(foundation)*
- Create `.slnx`, four projects, `.gitignore`, `.gitattributes`, `run.bat`, `README.md`,
  `CHANGELOG.md`, `THIRD-PARTY-NOTICES.md`, `app.manifest`.
- Wire `ViewModelBase` / `PageViewModel`, `ViewLocator`, empty `MainWindow` shell.
- **Light palette** in `App.axaml` (`RequestedThemeVariant="Light"`; white `FormBg`, near-white
  `PanelBg`, dark `Fg`, a single accent).
- CI stub: build + test on `windows-latest` and `macos-latest`.
- **Done when:** `dotnet build` and `dotnet test` pass on both OSes; app launches to an empty
  light window.

### M1 — Core PDF engine + tests  *(no UI)*
- `PdfDocumentModel`, `PageRef`, `PdfEditor` with `Remove`, `Combine`, `Reorder`, `Rotate`, `SaveAs`.
- Implement `SaveAs` with PDFsharp (open sources, import pages in order, apply rotation).
- xUnit tests over small fixture PDFs (2–3 pages): verify page counts, order, rotation, and
  round-trip (save → reopen → assert). Fixtures generated in a test helper via PDFsharp.
- **Done when:** all list transforms + save/round-trip are covered and green. This is the
  riskiest correctness surface, so it lands before any UI.

### M2 — App shell & navigation
- `MainWindowViewModel`, single primary page (`EditorViewModel`) + `About`/`Settings` stubs.
- Toolbar: Open, Add PDF, Save, Save As, plus per-page actions (wired to no-ops for now).
- Empty-state ("Open a PDF to get started" drop target).
- **Done when:** headless render shows the light shell + empty state.

### M3 — Page thumbnail rendering
- `IPdfRenderer` → `PdfiumRenderer` (PDFtoImage) producing SkiaSharp bitmaps → Avalonia `Bitmap`.
- Async, cached, off-UI-thread rendering keyed on `(SourceFileId, pageIndex, rotation)`;
  cancellation on document change; bounded concurrency.
- Page grid (`ItemsControl` + `WrapPanel`) showing thumbnails with page numbers.
- **Done when:** opening a fixture PDF shows correct thumbnails; rotating re-renders.

### M4 — Remove + reorder (the graphical core)
- Multi-select on the page grid; **Remove** deletes selected `PageRef`s.
- **Drag-and-drop reorder** within the grid (Avalonia drag/drop), with a drop-indicator.
- Keyboard: arrow-move, Delete, Ctrl+A. Undo/redo of list operations (in-memory command stack).
- **Done when:** remove + reorder work by mouse and keyboard; render captures show both.

### M5 — Combine multiple PDFs
- **Add PDF** appends another document's pages; sources tracked so thumbnails still resolve.
- Visual affordance for source boundaries (subtle divider/label per source file).
- **Done when:** two fixtures combine, reorder across sources, and save to one correct PDF.

### M6 — Rotate, save UX & robustness
- Rotate 90° CW/CCW/180 on selection; thumbnails reflect rotation.
- Save vs Save As; dirty-state tracking + "unsaved changes" guard on close.
- Error handling: corrupt file, zero pages left, locked output file, **encrypted PDF detected**
  → clear message (full password support is a future milestone).
- **Done when:** full remove→combine→reorder→rotate→save flow works end-to-end with sane errors.

### M7 — Packaging & release
- Velopack integration in `Program.cs` (install/update hook) + `UpdateChecker`.
- `release.yml`: build, test, `vpk pack` for `win-x64` and `osx-arm64`/`osx-x64`, publish to a
  GitHub release. Bundle correct PDFium native assets per RID.
- Embedded `CHANGELOG.md` + "what's new" window (port from sprig).
- App icon + `foldfinch.svg`.
- **Done when:** a tagged push produces installable Windows + macOS artifacts that self-update.

---

## 5. Future milestones (documented, out of v1 scope)

- **F1 — Extract / split:** export a page selection to a new PDF; split one PDF into several.
- **F2 — Encrypted PDFs:** password prompt to open protected files; optionally set/remove a
  password on save. (PDFsharp supports both open-with-password and encryption on save.)
- **F3 — Linux target:** add `linux-x64` runtime + CI leg + AppImage/deb via Velopack.
- **F4 — Nice-to-haves:** page range selection syntax, thumbnail zoom, recent-files list,
  optional dark theme toggle.

---

## 6. Risks & mitigations

| Risk | Mitigation |
| --- | --- |
| PDFium native binaries per-platform | Reference `PDFtoImage`'s per-RID native packages; verify in M3 on both OSes; CI packs correct RID assets in M7. |
| Rotation semantics (source rotation + user rotation) | Normalise to absolute rotation in `PageRef`; cover with round-trip tests in M1/M6. |
| Large PDFs → slow thumbnails / memory | Async bounded rendering + LRU thumbnail cache + render-on-scroll; deferred detail in M3. |
| PDFsharp import edge cases (forms, annotations) | v1 targets page-level ops; note lossy cases in docs; add fixtures as found. |
| macOS build/signing in CI | Start unsigned in M7; note notarization as a follow-up (same as sprig's stance). |

---

## 7. Suggested build order

`M0 → M1 → M2 → M3 → M4 → M5 → M6 → M7`, with M1 (Core correctness) deliberately ahead of all
UI. Each milestone is a mergeable PR with tests and a headless render capture attached.
