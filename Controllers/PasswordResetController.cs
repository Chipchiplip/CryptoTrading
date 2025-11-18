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
    <title>Reset Password - CryptoTrade</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}

        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Oxygen', 'Ubuntu', 'Cantarell', sans-serif;
            background: #000000;
            color: #ffffff;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 1rem;
        }}

        .container {{
            width: 100%;
            max-width: 28rem;
        }}

        .header {{
            text-align: center;
            margin-bottom: 2rem;
        }}

        .logo-wrapper {{
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 0.75rem;
            margin-bottom: 1.5rem;
        }}

        .logo {{
            width: 3rem;
            height: 3rem;
            background: linear-gradient(135deg, #10b981 0%, #059669 100%);
            border-radius: 0.625rem;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.5rem;
        }}

        .brand {{
            font-size: 1.5rem;
            font-weight: 700;
            background: linear-gradient(135deg, #10b981 0%, #059669 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
        }}

        h1 {{
            font-size: 1.875rem;
            font-weight: 700;
            margin-bottom: 0.5rem;
            line-height: 1.2;
        }}

        .subtitle {{
            color: #9ca3af;
            font-size: 0.875rem;
        }}

        .card {{
            background: #111111;
            border: 1px solid #374151;
            border-radius: 0.625rem;
            padding: 2rem;
            margin-bottom: 1.5rem;
        }}

        .requirements-box {{
            background: rgba(16, 185, 129, 0.1);
            border: 1px solid rgba(16, 185, 129, 0.3);
            border-radius: 0.625rem;
            padding: 1rem;
            margin-bottom: 1.5rem;
        }}

        .requirements-title {{
            font-weight: 600;
            font-size: 0.875rem;
            margin-bottom: 0.75rem;
            color: #10b981;
        }}

        .requirement-item {{
            display: flex;
            align-items: center;
            gap: 0.5rem;
            font-size: 0.8125rem;
            color: #d1d5db;
            margin-bottom: 0.5rem;
        }}

        .requirement-item:last-child {{
            margin-bottom: 0;
        }}

        .requirement-icon {{
            width: 1rem;
            height: 1rem;
            border-radius: 50%;
            background: #1f2937;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 0.625rem;
            flex-shrink: 0;
        }}

        .requirement-icon.met {{
            background: #10b981;
            color: #000000;
        }}

        .form-group {{
            margin-bottom: 1.5rem;
        }}

        label {{
            display: block;
            font-size: 0.875rem;
            font-weight: 500;
            margin-bottom: 0.5rem;
            color: #e5e7eb;
        }}

        .input-wrapper {{
            position: relative;
        }}

        .input-icon {{
            position: absolute;
            left: 0.75rem;
            top: 50%;
            transform: translateY(-50%);
            color: #9ca3af;
            pointer-events: none;
        }}

        .toggle-password {{
            position: absolute;
            right: 0.75rem;
            top: 50%;
            transform: translateY(-50%);
            background: none;
            border: none;
            color: #9ca3af;
            cursor: pointer;
            padding: 0.25rem;
            display: flex;
            align-items: center;
            justify-content: center;
            transition: color 0.2s;
            width: auto;
            z-index: 10;
        }}

        .toggle-password:hover {{
            color: #ffffff;
        }}

        input {{
            width: 100%;
            padding: 0.75rem 2.5rem;
            background: #1f2937;
            border: 1px solid #374151;
            border-radius: 0.625rem;
            color: #ffffff;
            font-size: 0.875rem;
            transition: all 0.2s;
        }}

        input::placeholder {{
            color: #6b7280;
        }}

        input:focus {{
            outline: none;
            border-color: #10b981;
            box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
        }}

        button[type='submit'] {{
            width: 100%;
            padding: 0.75rem 1rem;
            background: #10b981;
            color: #000000;
            border: none;
            border-radius: 0.625rem;
            font-size: 0.875rem;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 0.5rem;
        }}

        button[type='submit']:hover:not(:disabled) {{
            background: #059669;
        }}

        button[type='submit']:disabled {{
            opacity: 0.5;
            cursor: not-allowed;
        }}

        .spinner {{
            width: 1rem;
            height: 1rem;
            border: 2px solid #000000;
            border-top-color: transparent;
            border-radius: 50%;
            animation: spin 0.6s linear infinite;
        }}

        @keyframes spin {{
            to {{ transform: rotate(360deg); }}
        }}

        .alert {{
            padding: 1rem;
            border-radius: 0.625rem;
            margin-top: 1.5rem;
            font-size: 0.875rem;
            line-height: 1.5;
        }}

        .alert-error {{
            background: rgba(239, 68, 68, 0.1);
            border: 1px solid rgba(239, 68, 68, 0.5);
            color: #ef4444;
        }}

        .alert-success {{
            background: rgba(16, 185, 129, 0.1);
            border: 1px solid rgba(16, 185, 129, 0.5);
            color: #10b981;
        }}

        .alert-title {{
            font-weight: 600;
            margin-bottom: 0.5rem;
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }}

        .success-icon {{
            width: 4rem;
            height: 4rem;
            background: rgba(16, 185, 129, 0.1);
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            margin: 0 auto 1rem;
            font-size: 2rem;
        }}

        .link {{
            color: #10b981;
            text-decoration: none;
            font-weight: 600;
            transition: color 0.2s;
        }}

        .link:hover {{
            color: #059669;
            text-decoration: underline;
        }}

        .footer-text {{
            text-align: center;
            margin-top: 1.5rem;
            color: #9ca3af;
            font-size: 0.875rem;
        }}

        .footer-text a {{
            color: #10b981;
            text-decoration: none;
        }}

        .footer-text a:hover {{
            text-decoration: underline;
        }}

        /* SVG Icons */
        .icon-lock {{
            width: 1.125rem;
            height: 1.125rem;
        }}

        .icon-eye {{
            width: 1.125rem;
            height: 1.125rem;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo-wrapper'>
                <div class='logo'>🔐</div>
                <span class='brand'>CryptoTrade</span>
            </div>
            <h1>Reset Your Password</h1>
            <p class='subtitle'>Create a new secure password for your account</p>
        </div>

        <div class='card'>
            <div class='requirements-box' id='requirementsBox'>
                <div class='requirements-title'>Password Requirements</div>
                <div class='requirement-item' id='req-length'>
                    <span class='requirement-icon'>✓</span>
                    <span>At least 8 characters</span>
                </div>
                <div class='requirement-item' id='req-uppercase'>
                    <span class='requirement-icon'>✓</span>
                    <span>Contains uppercase letter (A-Z)</span>
                </div>
                <div class='requirement-item' id='req-lowercase'>
                    <span class='requirement-icon'>✓</span>
                    <span>Contains lowercase letter (a-z)</span>
                </div>
                <div class='requirement-item' id='req-number'>
                    <span class='requirement-icon'>✓</span>
                    <span>Contains number (0-9)</span>
                </div>
            </div>

            <form id='resetForm'>
                <div class='form-group'>
                    <label for='newPassword'>New Password</label>
                    <div class='input-wrapper'>
                        <svg class='input-icon icon-lock' fill='none' stroke='currentColor' viewBox='0 0 24 24'>
                            <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z'/>
                        </svg>
                        <input type='password' id='newPassword' placeholder='Enter new password' required>
                        <button type='button' class='toggle-password' onclick='togglePassword(""newPassword"", this)'>
                            <svg class='icon-eye' fill='none' stroke='currentColor' viewBox='0 0 24 24'>
                                <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M15 12a3 3 0 11-6 0 3 3 0 016 0z'/>
                                <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z'/>
                            </svg>
                        </button>
                    </div>
                </div>

                <div class='form-group'>
                    <label for='confirmPassword'>Confirm Password</label>
                    <div class='input-wrapper'>
                        <svg class='input-icon icon-lock' fill='none' stroke='currentColor' viewBox='0 0 24 24'>
                            <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z'/>
                        </svg>
                        <input type='password' id='confirmPassword' placeholder='Re-enter new password' required>
                        <button type='button' class='toggle-password' onclick='togglePassword(""confirmPassword"", this)'>
                            <svg class='icon-eye' fill='none' stroke='currentColor' viewBox='0 0 24 24'>
                                <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M15 12a3 3 0 11-6 0 3 3 0 016 0z'/>
                                <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z'/>
                            </svg>
                        </button>
                    </div>
                </div>

                <button type='submit' id='submitBtn'>
                    Reset Password
                </button>
            </form>

            <div id='status'></div>
        </div>

        <div class='footer-text'>
            Remember your password? <a href='http://localhost:3000/login'>Back to Login</a>
        </div>
    </div>

    <script>
        const API_BASE = window.location.origin;
        const email = '{email}';
        const token = '{token}';

        // Password visibility toggle
        function togglePassword(inputId, button) {{
            const input = document.getElementById(inputId);
            const isPassword = input.type === 'password';
            input.type = isPassword ? 'text' : 'password';

            button.innerHTML = isPassword
                ? `<svg class='icon-eye' fill='none' stroke='currentColor' viewBox='0 0 24 24'><path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21'/></svg>`
                : `<svg class='icon-eye' fill='none' stroke='currentColor' viewBox='0 0 24 24'><path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M15 12a3 3 0 11-6 0 3 3 0 016 0z'/><path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z'/></svg>`;
        }}

        // Password validation
        const passwordInput = document.getElementById('newPassword');
        passwordInput.addEventListener('input', () => {{
            const password = passwordInput.value;

            const requirements = [
                {{ id: 'req-length', met: password.length >= 8 }},
                {{ id: 'req-uppercase', met: /[A-Z]/.test(password) }},
                {{ id: 'req-lowercase', met: /[a-z]/.test(password) }},
                {{ id: 'req-number', met: /[0-9]/.test(password) }}
            ];

            requirements.forEach(req => {{
                const el = document.getElementById(req.id);
                const icon = el.querySelector('.requirement-icon');
                if (req.met) {{
                    icon.classList.add('met');
                }} else {{
                    icon.classList.remove('met');
                }}
            }});
        }});

        // Form submission
        document.getElementById('resetForm').addEventListener('submit', async (e) => {{
            e.preventDefault();

            const newPassword = document.getElementById('newPassword').value;
            const confirmPassword = document.getElementById('confirmPassword').value;
            const statusDiv = document.getElementById('status');
            const submitBtn = document.getElementById('submitBtn');

            // Validate passwords match
            if (newPassword !== confirmPassword) {{
                statusDiv.innerHTML = `
                    <div class='alert alert-error'>
                        <div class='alert-title'>
                            <svg style='width: 1.125rem; height: 1.125rem;' fill='none' stroke='currentColor' viewBox='0 0 24 24'>
                                <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'/>
                            </svg>
                            Passwords do not match
                        </div>
                        Please make sure both password fields are identical.
                    </div>
                `;
                return;
            }}

            // Validate password requirements
            if (newPassword.length < 8 || !/[A-Z]/.test(newPassword) || !/[a-z]/.test(newPassword) || !/[0-9]/.test(newPassword)) {{
                statusDiv.innerHTML = `
                    <div class='alert alert-error'>
                        <div class='alert-title'>
                            <svg style='width: 1.125rem; height: 1.125rem;' fill='none' stroke='currentColor' viewBox='0 0 24 24'>
                                <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'/>
                            </svg>
                            Password requirements not met
                        </div>
                        Please ensure your password meets all the requirements listed above.
                    </div>
                `;
                return;
            }}

            // Show loading state
            submitBtn.disabled = true;
            submitBtn.innerHTML = `<div class='spinner'></div> Resetting Password...`;
            statusDiv.innerHTML = '';

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
                    document.getElementById('resetForm').style.display = 'none';
                    document.getElementById('requirementsBox').style.display = 'none';
                    statusDiv.innerHTML = `
                        <div class='alert alert-success'>
                            <div class='success-icon'>
                                <svg style='width: 2rem; height: 2rem; color: #10b981;' fill='none' stroke='currentColor' viewBox='0 0 24 24'>
                                    <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M5 13l4 4L19 7'/>
                                </svg>
                            </div>
                            <div class='alert-title'>Password Reset Successful!</div>
                            <div style='text-align: center; margin-top: 0.75rem;'>
                                Your password has been updated successfully.<br>
                                You can now log in with your new password.
                            </div>
                            <div style='text-align: center; margin-top: 1rem;'>
                                <a href='http://localhost:3000/login' class='link'>Continue to Login →</a>
                            </div>
                        </div>
                    `;
                }} else {{
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = 'Reset Password';
                    statusDiv.innerHTML = `
                        <div class='alert alert-error'>
                            <div class='alert-title'>
                                <svg style='width: 1.125rem; height: 1.125rem;' fill='none' stroke='currentColor' viewBox='0 0 24 24'>
                                    <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'/>
                                </svg>
                                Password Reset Failed
                            </div>
                            <div style='margin-top: 0.5rem;'>
                                ${{data.message || 'An unknown error occurred.'}}<br>
                                <small style='color: #9ca3af; margin-top: 0.5rem; display: block;'>
                                    The reset link may have expired or been already used. Please request a new password reset.
                                </small>
                            </div>
                        </div>
                    `;
                }}
            }} catch (error) {{
                submitBtn.disabled = false;
                submitBtn.innerHTML = 'Reset Password';
                statusDiv.innerHTML = `
                    <div class='alert alert-error'>
                        <div class='alert-title'>
                            <svg style='width: 1.125rem; height: 1.125rem;' fill='none' stroke='currentColor' viewBox='0 0 24 24'>
                                <path stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'/>
                            </svg>
                            Connection Error
                        </div>
                        <div style='margin-top: 0.5rem;'>
                            Unable to connect to the server. Please check your internet connection and try again.
                        </div>
                    </div>
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

