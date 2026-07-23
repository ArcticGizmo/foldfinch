#!/usr/bin/env pwsh
# Regenerates every raster icon asset from the source-of-truth SVG (foldfinch.svg).
#
#   src/Foldfinch.App/Assets/foldfinch.png   256x256 PNG  (window + toolbar icon)
#   src/Foldfinch.App/Assets/foldfinch.ico   multi-res ICO (.exe ApplicationIcon)
#   landing-icon.png                          512x512 PNG  (README header)
#
# The SVG is rendered through System.Drawing, which only runs on Windows.
# Run this after editing foldfinch.svg, then commit the regenerated assets.

$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot 'IconGen'
dotnet run --project $proj -c Release
