# Packaging & releases

Foldfinch ships as [Velopack](https://velopack.io/) installers for Windows and macOS.

## Cutting a release

1. Bump the version in `CHANGELOG.md` (add a `## [vX.Y.Z] - <date>` section) and commit.
2. Tag and push:
   ```
   git tag vX.Y.Z
   git push origin vX.Y.Z
   ```
3. The `Release` workflow (`.github/workflows/release.yml`) builds a self-contained publish for each
   RID (`win-x64`, `osx-arm64`, `osx-x64`), runs `vpk pack`, and uploads the installers to the
   GitHub Release for that tag.

Native PDFium/SkiaSharp binaries are pulled in per-RID by the self-contained publish, so each
platform's installer carries the right ones.

## Auto-update

The app checks for updates on launch and shows a notification bar (notification only — installing is
an explicit action from the **About** dialog). The release feed is read from the
`FOLDFINCH_UPDATE_FEED` environment variable (a directory path or URL). If it is unset, or the app
was not installed via Velopack (e.g. run from the build output), update checks are a no-op.

Verify the update path headlessly with:

```
foldfinch check-update
```

## Regenerating the icons

The logo lives in a single source-of-truth vector, [`foldfinch.svg`](../foldfinch.svg). Every raster
asset — the app/window icon, the toolbar icon, and the README header — is generated from it by the
`tools/IconGen` console tool. After editing `foldfinch.svg`, regenerate and commit the results:

```
powershell tools/gen-icons.ps1   # PowerShell
tools\gen-icons.cmd              # cmd
# or directly: dotnet run --project tools/IconGen -c Release
```

This writes `src/Foldfinch.App/Assets/foldfinch.png` (256×256), `src/Foldfinch.App/Assets/foldfinch.ico`
(multi-resolution 16–256), and `landing-icon.png` (512×512, for the README). The SVG is rendered
through System.Drawing, which only runs on Windows; `tools/IconGen` is intentionally kept out of the
solution build so nothing shipping depends on the renderer.
