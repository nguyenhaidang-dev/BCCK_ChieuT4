git reset HEAD~1
git add .
$files = git ls-files --cached
git reset

$chunkSize = [math]::Ceiling($files.Count / 100)
if ($chunkSize -le 0) { $chunkSize = 1 }

$commitCount = 0
for ($i = 0; $i -lt $files.Count; $i += $chunkSize) {
    $chunk = $files | Select-Object -Skip $i -First $chunkSize
    foreach ($f in $chunk) {
        git add $f
    }
    $fName = Split-Path $chunk[0] -Leaf
    if ([string]::IsNullOrWhiteSpace($fName)) { $fName = "files" }
    git commit -qm "Refactor and update $fName"
    $commitCount++
}

Write-Host "Created $commitCount commits. Pushing to origin..."
git push -f -u origin main
Write-Host "Done!"
