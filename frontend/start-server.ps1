# Simple HTTP Server for Frontend
$port = 3000
$url = "http://localhost:$port"

Write-Host "🚀 Starting Crypto Trading Frontend Server..." -ForegroundColor Green
Write-Host "📡 Port: $port" -ForegroundColor Yellow
Write-Host "🌐 URL: $url" -ForegroundColor Cyan
Write-Host "📊 API: http://localhost:5299" -ForegroundColor Magenta
Write-Host ""

# Create HTTP Listener
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("$url/")

try {
    $listener.Start()
    Write-Host "✅ Server started successfully!" -ForegroundColor Green
    Write-Host "🌐 Open your browser and go to: $url" -ForegroundColor White -BackgroundColor Blue
    Write-Host ""
    Write-Host "Press Ctrl+C to stop the server..." -ForegroundColor Yellow
    
    # Start browser
    Start-Process $url
    
    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $request = $context.Request
        $response = $context.Response
        
        # Add CORS headers
        $response.Headers.Add("Access-Control-Allow-Origin", "*")
        $response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS")
        $response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization")
        
        # Get requested file path
        $path = $request.Url.LocalPath
        if ($path -eq "/") { $path = "/index.html" }
        $filePath = Join-Path $PSScriptRoot $path.TrimStart('/')
        
        if (Test-Path $filePath) {
            $content = Get-Content $filePath -Raw -Encoding UTF8
            
            # Set content type
            $extension = [System.IO.Path]::GetExtension($filePath)
            switch ($extension) {
                ".html" { $response.ContentType = "text/html; charset=utf-8" }
                ".js"   { $response.ContentType = "application/javascript; charset=utf-8" }
                ".css"  { $response.ContentType = "text/css; charset=utf-8" }
                ".json" { $response.ContentType = "application/json; charset=utf-8" }
                default { $response.ContentType = "text/plain; charset=utf-8" }
            }
            
            $buffer = [System.Text.Encoding]::UTF8.GetBytes($content)
            $response.ContentLength64 = $buffer.Length
            $response.OutputStream.Write($buffer, 0, $buffer.Length)
        } else {
            $response.StatusCode = 404
            $notFound = "404 - File Not Found: $path"
            $buffer = [System.Text.Encoding]::UTF8.GetBytes($notFound)
            $response.ContentLength64 = $buffer.Length
            $response.OutputStream.Write($buffer, 0, $buffer.Length)
        }
        
        $response.OutputStream.Close()
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    if ($listener.IsListening) {
        $listener.Stop()
    }
    Write-Host "🛑 Server stopped." -ForegroundColor Yellow
}

