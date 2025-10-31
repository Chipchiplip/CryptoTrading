using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class EmailConfirmationController : ControllerBase
{
    [HttpGet("/confirm-email")]
    public IActionResult ConfirmEmailPage([FromQuery] string email, [FromQuery] string token)
    {
        var html = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Email Confirmation - CryptoTrade</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}

        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: #000000;
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 20px;
            color: #ffffff;
        }}

        .container {{
            background: #1a1d24;
            border: 1px solid #374151;
            border-radius: 12px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.5);
            max-width: 500px;
            width: 100%;
            padding: 48px;
            text-align: center;
        }}

        .logo {{
            width: 64px;
            height: 64px;
            background: #10b981;
            border-radius: 50%;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            margin-bottom: 24px;
        }}

        .logo-text {{
            color: #000000;
            font-size: 20px;
            font-weight: bold;
        }}

        h1 {{
            color: #ffffff;
            font-size: 24px;
            font-weight: bold;
            margin-bottom: 8px;
        }}

        .subtitle {{
            color: #9ca3af;
            font-size: 14px;
            margin-bottom: 24px;
        }}

        .icon-container {{
            width: 64px;
            height: 64px;
            background: rgba(16, 185, 129, 0.1);
            border-radius: 50%;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            margin: 24px auto;
        }}

        .icon {{
            font-size: 32px;
        }}

        .icon-loading {{
            width: 32px;
            height: 32px;
            border: 3px solid rgba(16, 185, 129, 0.2);
            border-top: 3px solid #10b981;
            border-radius: 50%;
            animation: spin 1s linear infinite;
        }}

        @keyframes spin {{
            0% {{ transform: rotate(0deg); }}
            100% {{ transform: rotate(360deg); }}
        }}

        .message {{
            color: #9ca3af;
            font-size: 16px;
            line-height: 1.6;
            margin-bottom: 24px;
        }}

        .status {{
            padding: 16px;
            border-radius: 8px;
            margin: 24px 0;
            font-size: 14px;
            text-align: left;
        }}

        .status.loading {{
            background: rgba(16, 185, 129, 0.1);
            border: 1px solid rgba(16, 185, 129, 0.2);
            color: #10b981;
        }}

        .status.success {{
            background: rgba(16, 185, 129, 0.1);
            border: 1px solid rgba(16, 185, 129, 0.2);
            color: #10b981;
        }}

        .status.error {{
            background: rgba(239, 68, 68, 0.1);
            border: 1px solid rgba(239, 68, 68, 0.2);
            color: #ef4444;
        }}

        .btn {{
            display: inline-block;
            width: 100%;
            padding: 12px 24px;
            background: #10b981;
            color: #000000;
            text-decoration: none;
            border-radius: 8px;
            font-weight: 600;
            font-size: 16px;
            transition: all 0.3s;
            margin-top: 16px;
            border: none;
            cursor: pointer;
        }}

        .btn:hover {{
            background: #059669;
        }}

        .btn-secondary {{
            background: transparent;
            color: #ffffff;
            border: 1px solid #374151;
        }}

        .btn-secondary:hover {{
            background: #374151;
        }}

        .details {{
            background: #111827;
            padding: 16px;
            border-radius: 8px;
            margin-top: 24px;
            text-align: left;
            border: 1px solid #374151;
        }}

        .details h3 {{
            color: #d1d5db;
            margin-bottom: 12px;
            font-size: 14px;
            font-weight: 600;
        }}

        .details p {{
            color: #9ca3af;
            font-size: 14px;
            margin: 8px 0;
        }}

        .details ul {{
            color: #9ca3af;
            font-size: 14px;
            margin: 8px 0;
            padding-left: 20px;
        }}

        .details li {{
            margin: 4px 0;
        }}

        .email-highlight {{
            color: #10b981;
            font-weight: 600;
        }}

        .spinner {{
            border: 3px solid rgba(16, 185, 129, 0.2);
            border-top: 3px solid #10b981;
            border-radius: 50%;
            width: 32px;
            height: 32px;
            animation: spin 1s linear infinite;
            margin: 20px auto;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='logo'>
            <span class='logo-text'>CT</span>
        </div>
        <h1>Verify Your Email</h1>
        <p class='subtitle'>Check your email for verification instructions</p>
        <div id='content'>
            <div class='icon-container'>
                <div class='icon-loading'></div>
            </div>
            <h2 style='color: #ffffff; font-size: 20px; font-weight: bold; margin-bottom: 8px;'>Confirming Your Email...</h2>
            <div class='status loading'>
                <strong>Please wait</strong><br>
                We're verifying your email address
            </div>
        </div>
    </div>

    <script>
        const API_BASE = '';
        const FRONTEND_BASE = window.location.port === '5299' || window.location.port === '7154' 
            ? 'http://localhost:3000' 
            : window.location.origin;
        const email = '{email}';
        const token = '{token}';

        window.addEventListener('DOMContentLoaded', async () => {{
            if (!email || !token) {{
                showError('Invalid confirmation link', 'Email and token parameters are missing.');
                return;
            }}

            try {{
                const response = await fetch(`${{API_BASE}}/api/auth/confirm-email`, {{
                    method: 'POST',
                    headers: {{
                        'Content-Type': 'application/json',
                    }},
                    body: JSON.stringify({{
                        email: decodeURIComponent(email),
                        token: decodeURIComponent(token)
                    }})
                }});

                const data = await response.json();

                if (response.ok) {{
                    showSuccess(email);
                }} else {{
                    showError('Confirmation Failed', data.message || 'Unknown error occurred');
                }}
            }} catch (error) {{
                showError('Connection Error', 'Could not connect to the server. Please try again later.');
                console.error('Error:', error);
            }}
        }});

        function showSuccess(email) {{
            document.getElementById('content').innerHTML = `
                <div class='icon-container'>
                    <div class='icon' style='color: #10b981; font-size: 32px;'>✓</div>
                </div>
                <h2 style='color: #ffffff; font-size: 20px; font-weight: bold; margin-bottom: 8px;'>Email Verified Successfully!</h2>
                <p class='message'>
                    Your account is now active. You can start trading right away.
                </p>
                <div class='status success'>
                    <strong>✓ Verification Complete</strong><br>
                    Your account is now active
                </div>
                <div class='details'>
                    <h3>📧 Confirmed Email:</h3>
                    <p><span class='email-highlight'>${{email}}</span></p>
                    <h3 style='margin-top: 16px;'>🎯 Next Steps:</h3>
                    <ul>
                        <li>Click ""Go to Login"" button below</li>
                        <li>Enter your email and password</li>
                        <li>Start trading cryptocurrencies!</li>
                    </ul>
                </div>
                <a href='#' class='btn' onclick='window.location.href=FRONTEND_BASE + ""/login""; return false;'>Go to Login</a>
            `;
        }}

        function showError(title, message) {{
            document.getElementById('content').innerHTML = `
                <div class='icon-container'>
                    <div class='icon' style='color: #ef4444; font-size: 32px;'>✕</div>
                </div>
                <h2 style='color: #ffffff; font-size: 20px; font-weight: bold; margin-bottom: 8px;'>${{title}}</h2>
                <p class='message'>${{message}}</p>
                <div class='status error'>
                    <strong>⚠️ Verification Failed</strong><br>
                    Please check the error details below
                </div>
                <div class='details'>
                    <h3>🔍 Possible Reasons:</h3>
                    <ul>
                        <li>The confirmation link has expired (24 hours)</li>
                        <li>The link has already been used</li>
                        <li>Invalid or corrupted token</li>
                        <li>Email address not found</li>
                    </ul>
                    <h3 style='margin-top: 16px;'>💡 What to do:</h3>
                    <ul>
                        <li>Try registering again with a new account</li>
                        <li>Contact support if the problem persists</li>
                        <li>Check if you're using the latest confirmation email</li>
                    </ul>
                </div>
                <a href='#' class='btn btn-secondary' onclick='window.location.href=FRONTEND_BASE + ""/""; return false;'>Back to Home</a>
            `;
        }}
    </script>
</body>
</html>";

        return Content(html, "text/html");
    }
}


