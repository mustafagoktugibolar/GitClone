$projectFile = "..\GitClone.csproj"
$nupkgDir = "..\nupkg"

[xml]$xml = Get-Content $projectFile -Raw
$currentVersion = $xml.Project.PropertyGroup.Version

$versionParts = $currentVersion.Split('.')
$major = $versionParts[0]
$minor = $versionParts[1]
$patch = [int]$versionParts[2] + 1
$newVersion = "$major.$minor.$patch"

Write-Host "Updating version: $currentVersion -> $newVersion"

$xml.Project.PropertyGroup.Version = $newVersion
$xml.Save($projectFile)

dotnet pack $projectFile -c Release -o $nupkgDir

Write-Host "Uninstalling old version..."
dotnet tool uninstall --global ilos

Write-Host "Installing ilos@$newVersion..."
dotnet tool install --global --add-source $nupkgDir ilos

Write-Host "ilos@$newVersion installed successfully!"
