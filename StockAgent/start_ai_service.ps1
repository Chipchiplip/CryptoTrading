# PowerShell script to start AI Recommendation Service
Write-Host "Starting AI Recommendation Service..." -ForegroundColor Green
Write-Host "Service will run on http://localhost:8000" -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Host ""

cd $PSScriptRoot
# Use Python to run app.py directly (app.py has reload_excludes configured)
python app.py

