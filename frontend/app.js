// API Base URL
const API_BASE_URL = 'http://localhost:5299/api';

// Check if running from file:// protocol and show warning
if (window.location.protocol === 'file:') {
    console.warn('⚠️ Running from file:// protocol. CORS may be blocked.');
    console.log('💡 For best experience, serve from HTTP server.');
}

// Store JWT token
let authToken = localStorage.getItem('authToken');

// DOM Elements
const loginForm = document.getElementById('loginForm');
const registerForm = document.getElementById('registerForm');
const responseArea = document.getElementById('responseArea');
const responseAlert = document.getElementById('responseAlert');
const userInfo = document.getElementById('userInfo');
const userDetails = document.getElementById('userDetails');

// Initialize app
document.addEventListener('DOMContentLoaded', function() {
    if (authToken) {
        showUserInfo();
    }
});

// Login Form Handler
loginForm.addEventListener('submit', async function(e) {
    e.preventDefault();
    
    const email = document.getElementById('loginEmail').value;
    const password = document.getElementById('loginPassword').value;
    const twoFactorCode = document.getElementById('twoFactorCode').value;

    const loginData = {
        email: email,
        password: password,
        twoFactorCode: twoFactorCode || null
    };

    try {
        showLoading('Logging in...');
        const response = await fetch(`${API_BASE_URL}/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(loginData)
        });

        const result = await response.json();

        if (response.ok) {
            if (result.accessToken) {
                // Successful login
                authToken = result.accessToken;
                localStorage.setItem('authToken', authToken);
                localStorage.setItem('userInfo', JSON.stringify(result.user));
                
                showSuccess('Login successful!');
                showUserInfo(result.user);
            } else {
                // 2FA required
                showInfo('Please check your email for confirmation or enter 2FA code if enabled.');
            }
        } else {
            showError(result.message || 'Login failed');
        }
    } catch (error) {
        showError('Network error: ' + error.message);
    }
});

// Register Form Handler
registerForm.addEventListener('submit', async function(e) {
    e.preventDefault();
    
    const email = document.getElementById('registerEmail').value;
    const fullName = document.getElementById('fullName').value;
    const password = document.getElementById('registerPassword').value;
    const confirmPassword = document.getElementById('confirmPassword').value;

    if (password !== confirmPassword) {
        showError('Passwords do not match');
        return;
    }

    const registerData = {
        email: email,
        fullName: fullName,
        password: password,
        confirmPassword: confirmPassword
    };

    try {
        showLoading('Creating account...');
        const response = await fetch(`${API_BASE_URL}/auth/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(registerData)
        });

        const result = await response.json();

        if (response.ok) {
            showSuccess('Registration successful! Please check your email to confirm your account.');
            // Switch to login tab
            document.getElementById('login-tab').click();
            // Clear form
            registerForm.reset();
        } else {
            showError(result.message || 'Registration failed');
        }
    } catch (error) {
        showError('Network error: ' + error.message);
    }
});

// Show user info after login
function showUserInfo(user = null) {
    if (!user) {
        user = JSON.parse(localStorage.getItem('userInfo') || '{}');
    }
    
    userDetails.innerHTML = `
        <strong>Email:</strong> ${user.email}<br>
        <strong>Name:</strong> ${user.fullName || 'Not provided'}<br>
        <strong>2FA:</strong> ${user.twoFactorEnabled ? 'Enabled' : 'Disabled'}
    `;
    
    // Hide auth forms and show user info
    document.getElementById('authTabs').style.display = 'none';
    document.getElementById('authTabContent').style.display = 'none';
    userInfo.style.display = 'block';
    responseArea.style.display = 'none';
}

// Logout function
function logout() {
    authToken = null;
    localStorage.removeItem('authToken');
    localStorage.removeItem('userInfo');
    
    // Show auth forms and hide user info
    document.getElementById('authTabs').style.display = 'flex';
    document.getElementById('authTabContent').style.display = 'block';
    userInfo.style.display = 'none';
    responseArea.style.display = 'none';
    
    // Clear forms
    loginForm.reset();
    registerForm.reset();
}

// Utility functions for showing messages
function showResponse(message, type) {
    responseAlert.className = `alert alert-${type}`;
    responseAlert.textContent = message;
    responseArea.style.display = 'block';
    
    // Auto hide after 5 seconds
    setTimeout(() => {
        responseArea.style.display = 'none';
    }, 5000);
}

function showSuccess(message) {
    showResponse(message, 'success');
}

function showError(message) {
    showResponse(message, 'danger');
}

function showInfo(message) {
    showResponse(message, 'info');
}

function showLoading(message) {
    showResponse(message + ' Please wait...', 'primary');
}

// Test API connection on page load
async function testApiConnection() {
    try {
        const response = await fetch(`${API_BASE_URL.replace('/api', '')}/health`);
        if (response.ok) {
            console.log('✅ API connection successful');
        } else {
            console.log('❌ API connection failed');
        }
    } catch (error) {
        console.log('❌ API connection error:', error.message);
    }
}

// Test API on load
testApiConnection();
