#!/bin/bash

PROJECT_FILE="../GitClone.csproj"
NUPKG_DIR="../nupkg"

CURRENT_VERSION=$(grep -oE "<Version>[0-9]+\.[0-9]+\.[0-9]+</Version>" "$PROJECT_FILE" | grep -oE "[0-9]+\.[0-9]+\.[0-9]+")
IFS='.' read -r MAJOR MINOR PATCH <<< "$CURRENT_VERSION"
NEW_VERSION="$MAJOR.$MINOR.$((PATCH + 1))"

echo "📦 Updating version: $CURRENT_VERSION → $NEW_VERSION"
sed -i '' "s/<Version>$CURRENT_VERSION<\/Version>/<Version>$NEW_VERSION<\/Version>/" "$PROJECT_FILE"

dotnet pack "$PROJECT_FILE" -c Release -o "$NUPKG_DIR"
dotnet tool uninstall --global ilos
dotnet tool install --global --add-source "$NUPKG_DIR" ilos

echo "✅ ilos@$NEW_VERSION installed successfully!"
