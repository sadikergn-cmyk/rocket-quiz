#!/usr/bin/env bash
# Publishes the local WebGL build (Game/Builds/WebGL) to the gh-pages branch as a single commit.
# Build first: Tools > Build RocketQuiz WebGL, or
#   unity command eval 'return WebGLBuilder.Build();' --project-path Game
# Usage: bash tools/publish_webgl.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BUILD="$ROOT/Game/Builds/WebGL"
REMOTE="$(git -C "$ROOT" remote get-url origin)"
SHA="$(git -C "$ROOT" rev-parse --short HEAD)"

[ -f "$BUILD/index.html" ] || { echo "No build at $BUILD"; exit 1; }

STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT
cp -r "$BUILD"/. "$STAGE"/
touch "$STAGE/.nojekyll"

cd "$STAGE"
git init -q -b gh-pages
git config core.autocrlf false
git config user.name "$(git -C "$ROOT" config user.name)"
git config user.email "$(git -C "$ROOT" config user.email)"
git add -A
git commit -q -m "WebGL build $SHA (yerel)"
git push -q -f "$REMOTE" gh-pages
echo "Yayinlandi: https://sadikergn-cmyk.github.io/rocket-quiz/ (1-2 dk icinde guncellenir)"
