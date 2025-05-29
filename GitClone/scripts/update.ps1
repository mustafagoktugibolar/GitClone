param(
    [string]$mode = "bump",              # Accepts "bump" (default) or "--no-bump"
    [string]$manualVersion = ""          # Optional: override version manually with --version x.y.z
)

$ScriptPath = $MyInvocation.MyCommand.Path
$ScriptDir = Split-Path $ScriptPath -Parent
$ProjectFile = Join-Path $ScriptDir "..\GitClone.csproj"
$NupkgDir = Join-Path $ScriptDir "..\nupkg"

Write-Host "Reading project file: $ProjectFile"
[xml]$xml = Get-Content $ProjectFile -Raw
$currentProjectVersion = $xml.Project.PropertyGroup.Version

# Determine version components
$versionParts = $currentProjectVersion.Split('.')
$major = $versionParts[0]
$minor = $versionParts[1]
$patch = [int]$versionParts[2]

# Determine final version to use
if ($manualVersion -ne "") {
    $newVersion = $manualVersion
    Write-Host "Using manually specified version: $newVersion"
    $xml.Project.PropertyGroup.Version = $newVersion
    $xml.Save($ProjectFile)
}
elseif ($mode -eq "--no-bump") {
    $newVersion = "$major.$minor.$patch"
    Write-Host "Using existing version: $newVersion"
}
else {
    $newVersion = "$major.$minor." + ($patch + 1)
    $xml.Project.PropertyGroup.Version = $newVersion
    $xml.Save($ProjectFile)
    Write-Host "Version bumped: $currentProjectVersion -> $newVersion"
}
# Clean up old .nupkg files
if (Test-Path $NupkgDir) {
    Get-ChildItem -Path $NupkgDir -Filter "*.nupkg" | Remove-Item -Force
    Write-Host "Old .nupkg files removed from $NupkgDir"
} else {
    New-Item -ItemType Directory -Path $NupkgDir | Out-Null
    Write-Host "Created nupkg output directory: $NupkgDir"
}

# Uninstall existing ilos tool if it exists
$ilosLine = dotnet tool list --global | Select-String -Pattern "ilos"
if ($ilosLine) {
    $columns = $ilosLine.ToString() -split '\s+'
    $installedVersion = $columns[1]
    Write-Host "Uninstalling existing ilos@$installedVersion..."
    dotnet tool uninstall --global ilos
}

# Build and pack the updated version
Write-Host "Packing ilos@$newVersion..."
dotnet pack $ProjectFile -c Release -o $NupkgDir

# Install the packed version as global tool
Write-Host "Installing ilos@$newVersion..."
dotnet tool install --global --add-source $NupkgDir ilos --version $newVersion

Write-Host "ilos@$newVersion installed successfully."
