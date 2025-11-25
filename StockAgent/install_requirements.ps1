# Script to find Python and install requirements

Write-Host "Searching for Python installation..." -ForegroundColor Cyan

# Try different Python commands
$pythonCommands = @(
    "python",
    "python3",
    "py",
    "C:\Users\nhuut\AppData\Local\Programs\Python\Python314\python.exe",
    "C:\Users\nhuut\AppData\Local\Programs\Python\Python312\python.exe",
    "C:\Users\nhuut\AppData\Local\Microsoft\WindowsApps\python3.12.exe",
    "C:\Program Files\Python312\python.exe",
    "C:\Program Files\Python314\python.exe"
)

$pythonPath = $null

foreach ($cmd in $pythonCommands) {
    try {
        $version = & $cmd --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Found Python: $cmd" -ForegroundColor Green
            Write-Host "Version: $version" -ForegroundColor Green
            $pythonPath = $cmd
            break
        }
    }
    catch {
        # Continue to next command
    }
}

if ($pythonPath) {
    Write-Host "`nInstalling requirements..." -ForegroundColor Cyan
    & $pythonPath -m pip install --upgrade pip
    & $pythonPath -m pip install -r requirements.txt
    Write-Host "`nDone!" -ForegroundColor Green
}
else {
    Write-Host "Python not found! Please:" -ForegroundColor Red
    Write-Host "1. Close this terminal" -ForegroundColor Yellow
    Write-Host "2. Open a NEW PowerShell terminal" -ForegroundColor Yellow
    Write-Host "3. Run: python --version" -ForegroundColor Yellow
    Write-Host "4. If it works, run: pip install -r requirements.txt" -ForegroundColor Yellow
}
