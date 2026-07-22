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

## Regenerating the app icon

The icon assets (`src/Foldfinch.App/Assets/foldfinch.png` and `.ico`) are generated:

```
dotnet run --project src/Foldfinch.App -- gen-icon src/Foldfinch.App/Assets
```
