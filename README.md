<h1 align="center">Foldfinch</h1>
<p align="center">
 <img src="./landing-icon.png" width="150"  />
</p>

<p align="center">
<strong>PDF chores made easy.</strong>
</p>

<br>

- **Remove pages** from a PDF
- **Combine** multiple PDFs into one
- **Reorder pages** graphically (drag and drop)
- **Rotate pages**

Built with .NET 10 and [Avalonia](https://avaloniaui.net/). Runs on Windows and macOS.
Light theme by default — PDFs are usually white.

## Running

```
dotnet run --project src/Foldfinch.App
```

or on Windows, `run.bat`.

## Layout

| Project | Purpose |
| --- | --- |
| `src/Foldfinch.Core` | PDF logic (open, remove, combine, reorder, rotate, save). No UI dependency. |
| `src/Foldfinch.App` | Avalonia MVVM desktop UI. |
| `tests/Foldfinch.Tests` | xUnit tests. |

See [`docs/implementation-plan.md`](docs/implementation-plan.md) for the milestone plan.

## Credits

PDF manipulation by [PDFsharp](https://www.pdfsharp.net/) (MIT), rendering by
[PDFtoImage](https://github.com/sungaila/PDFtoImage) → PDFium (BSD-3-Clause) + SkiaSharp (MIT).
See [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).
