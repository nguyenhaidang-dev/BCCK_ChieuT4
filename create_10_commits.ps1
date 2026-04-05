$ErrorActionPreference = 'SilentlyContinue'

$initialCommit = git rev-list --max-parents=0 HEAD
git reset $initialCommit

git add .
$files = git ls-files --cached
git reset

$messages = @(
    'Khoi tao cau truc du an va thiet lap ban dau',
    'Xay dung Models va cau hinh Database',
    'Tao cac file Migrations cho SQL Database',
    'Cau hinh cac Services va SignalR Hubs',
    'Xay dung cac APIs va Controllers',
    'Cau hinh moi truong framework cho ung dung Driver',
    'Thiet lap nen tang Mobile cho Android va iOS',
    'Thiet lap cau hinh Desktop va Web',
    'Phat trien giao dien cot loi cho Driver App',
    'Hoan thien tinh nang va tich hop API'
)

$numCommits = 10
$chunkSize = [math]::Ceiling($files.Count / $numCommits)

for ($i = 0; $i -lt $numCommits; $i++) {
    $chunk = $files | Select-Object -Skip ($i * $chunkSize) -First $chunkSize
    if ($chunk) {
        foreach ($f in $chunk) {
            git add $f
        }
        $msg = $messages[$i]
        git commit -qm $msg
    }
}

git add .
$status = git status --porcelain
if ($status) {
    git commit -qm 'Tinh chinh ma nguon cuoi cung'
}

git remote set-url origin https://github.com/nguyenhaidang-dev/BCCK_ChieuT4.git
git push -f -u origin main
Write-Host "Done!"
