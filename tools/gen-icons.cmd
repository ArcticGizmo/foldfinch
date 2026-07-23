@echo off
rem Regenerates every raster icon asset from the source-of-truth SVG (foldfinch.svg).
rem
rem   src/Foldfinch.App/Assets/foldfinch.png   256x256 PNG  (window + toolbar icon)
rem   src/Foldfinch.App/Assets/foldfinch.ico   multi-res ICO (.exe ApplicationIcon)
rem   landing-icon.png                          512x512 PNG  (README header)
rem
rem Run this after editing foldfinch.svg, then commit the regenerated assets.

dotnet run --project "%~dp0IconGen" -c Release
