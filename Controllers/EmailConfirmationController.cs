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
    <title>Email Confirmation - Crypto Trading</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}

        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 20px;
        }}

        .container {{
            background: white;
            border-radius: 20px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            max-width: 600px;
            width: 100%;
            padding: 50px;
            text-align: center;
        }}

        .icon {{
            font-size: 80px;
            margin-bottom: 20px;
        }}

        .icon.loading {{
            animation: spin 1s linear infinite;
        }}

        @keyframes spin {{
            0% {{ transform: rotate(0deg); }}
            100% {{ transform: rotate(360deg); }}
        }}

        h1 {{
            color: #333;
            font-size: 32px;
            margin-bottom: 20px;
        }}

        .message {{
            color: #666;
            font-size: 18px;
            line-height: 1.6;
            margin-bottom: 30px;
        }}

        .status {{
            padding: 20px;
            border-radius: 10px;
            margin: 30px 0;
            font-size: 16px;
        }}

        .status.loading {{
            background: #d1ecf1;
            border: 1px solid #bee5eb;
            color: #0c5460;
        }}

        .status.success {{
            background: #d4edda;
            border: 1px solid #c3e6cb;
            color: #155724;
        }}

        .status.error {{
            background: #f8d7da;
            border: 1px solid #f5c6cb;
            color: #721c24;
        }}

        .btn {{
            display: inline-block;
            padding: 15px 40px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            text-decoration: none;
            border-radius: 8px;
            font-weight: bold;
            font-size: 16px;
            transition: all 0.3s;
            margin-top: 20px;
        }}

        .btn:hover {{
            transform: translateY(-2px);
            box-shadow: 0 5px 15px rgba(102, 126, 234, 0.4);
        }}

        .details {{
            background: #f8f9fa;
            padding: 20px;
            border-radius: 10px;
            margin-top: 30px;
            text-align: left;
        }}

        .details h3 {{
            color: #333;
            margin-bottom: 15px;
            font-size: 18px;
        }}

        .details p {{
            color: #666;
            font-size: 14px;
            margin: 8px 0;
        }}

        .details code {{
            background: white;
            padding: 2px 8px;
            border-radius: 4px;
            color: #667eea;
            font-size: 13px;
        }}

        .spinner {{
            border: 4px solid #f3f3f3;
            border-top: 4px solid #667eea;
            border-radius: 50%;
            width: 50px;
            height: 50px;
            animation: spin 1s linear infinite;
            margin: 20px auto;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div id='content'>
            <div class='icon loading'>⏳</div>
            <h1>Confirming Your Email...</h1>
            <div class='spinner'></div>
            <div class='status loading'>
                <strong>Please wait</strong><br>
                We're verifying your email address
            </div>
        </div>
    </div>

    <script>
        const API_BASE = 'http://localhost:5000';
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
                <div class='icon'>✅</div>
                <h1>Email Confirmed!</h1>
                <p class='message'>
                    Your email address has been successfully verified.<br>
                    You can now log in to your account.
                </p>
                <div class='status success'>
                    <strong>✓ Verification Complete</strong><br>
                    Your account is now active
                </div>
                <a href='/auth-test.html' class='btn'>Go to Login</a>
                <div class='details'>
                    <h3>📧 Confirmed Email:</h3>
                    <p><code>${{email}}</code></p>
                    <h3 style='margin-top: 20px;'>🎯 Next Steps:</h3>
                    <p>1. Click ""Go to Login"" button above</p>
                    <p>2. Enter your email and password</p>
                    <p>3. Start trading cryptocurrencies!</p>
                </div>
            `;
        }}

        function showError(title, message) {{
            document.getElementById('content').innerHTML = `
                <div class='icon'>❌</div>
                <h1>${{title}}</h1>
                <p class='message'>${{message}}</p>
                <div class='status error'>
                    <strong>⚠️ Verification Failed</strong><br>
                    Please check the error details below
                </div>
                <div class='details'>
                    <h3>🔍 Possible Reasons:</h3>
                    <p>• The confirmation link has expired (24 hours)</p>
                    <p>• The link has already been used</p>
                    <p>• Invalid or corrupted token</p>
                    <p>• Email address not found</p>
                    <h3 style='margin-top: 20px;'>💡 What to do:</h3>
                    <p>1. Try registering again with a new account</p>
                    <p>2. Contact support if the problem persists</p>
                    <p>3. Check if you're using the latest confirmation email</p>
                </div>
                <a href='/auth-test.html' class='btn'>Back to Registration</a>
            `;
        }}
    </script>
</body>
</html>";

        return Content(html, "text/html");
    }
}

