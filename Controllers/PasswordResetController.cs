using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("[controller]")]
public class PasswordResetController : ControllerBase
{
    [HttpGet("/reset-password")]
    public IActionResult ResetPasswordPage([FromQuery] string email, [FromQuery] string token)
    {
        var html = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reset Password - Crypto Trading</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}

        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #dc3545 0%, #c82333 100%);
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
        }}

        h1 {{
            color: #333;
            font-size: 32px;
            margin-bottom: 10px;
            text-align: center;
        }}

        .subtitle {{
            color: #666;
            font-size: 16px;
            text-align: center;
            margin-bottom: 30px;
        }}

        .form-group {{
            margin-bottom: 20px;
        }}

        label {{
            display: block;
            color: #555;
            font-weight: 600;
            margin-bottom: 8px;
            font-size: 14px;
        }}

        input {{
            width: 100%;
            padding: 12px 15px;
            border: 2px solid #e0e0e0;
            border-radius: 8px;
            font-size: 14px;
            transition: all 0.3s;
        }}

        input:focus {{
            outline: none;
            border-color: #dc3545;
            box-shadow: 0 0 0 3px rgba(220, 53, 69, 0.1);
        }}

        button {{
            width: 100%;
            padding: 14px;
            background: #dc3545;
            color: white;
            border: none;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.3s;
            margin-top: 10px;
        }}

        button:hover {{
            background: #c82333;
            transform: translateY(-2px);
            box-shadow: 0 5px 15px rgba(220, 53, 69, 0.4);
        }}

        button:disabled {{
            opacity: 0.6;
            cursor: not-allowed;
            transform: none;
        }}

        .status {{
            padding: 15px;
            border-radius: 8px;
            margin-top: 20px;
            font-size: 14px;
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

        .status.info {{
            background: #d1ecf1;
            border: 1px solid #bee5eb;
            color: #0c5460;
        }}

        .info-box {{
            background: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            border-radius: 5px;
            margin-bottom: 20px;
            font-size: 13px;
            color: #856404;
        }}

        .link {{
            color: #667eea;
            text-decoration: none;
            font-weight: 600;
        }}

        .link:hover {{
            text-decoration: underline;
        }}

        .icon {{
            text-align: center;
            font-size: 60px;
            margin-bottom: 20px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>🔒</div>
        <h1>Reset Your Password</h1>
        <p class='subtitle'>Enter your new password below</p>

        <div class='info-box'>
            <strong>⚠️ Password Requirements:</strong><br>
            • At least 8 characters<br>
            • At least 1 uppercase letter<br>
            • At least 1 lowercase letter<br>
            • At least 1 number
        </div>

        <form id='resetForm'>
            <div class='form-group'>
                <label>New Password</label>
                <input type='password' id='newPassword' placeholder='Enter new password' required>
            </div>

            <div class='form-group'>
                <label>Confirm New Password</label>
                <input type='password' id='confirmPassword' placeholder='Re-enter password' required>
            </div>

            <button type='submit'>Reset Password</button>
        </form>

        <div id='status'></div>
    </div>

    <script>
        const API_BASE = 'http://localhost:5000';
        const email = '{email}';
        const token = '{token}';

        document.getElementById('resetForm').addEventListener('submit', async (e) => {{
            e.preventDefault();

            const newPassword = document.getElementById('newPassword').value;
            const confirmPassword = document.getElementById('confirmPassword').value;
            const statusDiv = document.getElementById('status');

            if (newPassword !== confirmPassword) {{
                statusDiv.className = 'status error';
                statusDiv.innerHTML = '<strong>❌ Passwords do not match!</strong>';
                return;
            }}

            statusDiv.className = 'status info';
            statusDiv.innerHTML = 'Resetting password...';

            try {{
                const response = await fetch(`${{API_BASE}}/api/auth/reset-password`, {{
                    method: 'POST',
                    headers: {{
                        'Content-Type': 'application/json',
                    }},
                    body: JSON.stringify({{
                        email: decodeURIComponent(email),
                        token: decodeURIComponent(token),
                        newPassword: newPassword,
                        confirmPassword: confirmPassword
                    }})
                }});

                const data = await response.json();

                if (response.ok) {{
                    statusDiv.className = 'status success';
                    statusDiv.innerHTML = `
                        <strong>✅ Password Reset Successful!</strong><br><br>
                        Your password has been updated successfully.<br><br>
                        <a href='/auth-test.html' class='link'>→ Click here to login with your new password</a>
                    `;
                    
                    document.getElementById('resetForm').style.display = 'none';
                }} else {{
                    statusDiv.className = 'status error';
                    statusDiv.innerHTML = `
                        <strong>❌ Password Reset Failed</strong><br><br>
                        ${{data.message || 'Unknown error occurred'}}<br><br>
                        <small>The reset link may have expired or been already used.</small>
                    `;
                }}
            }} catch (error) {{
                statusDiv.className = 'status error';
                statusDiv.innerHTML = `
                    <strong>❌ Connection Error</strong><br><br>
                    Could not connect to the server. Please try again later.
                `;
                console.error('Error:', error);
            }}
        }});
    </script>
</body>
</html>";

        return Content(html, "text/html");
    }
}

