// Login Page Script
document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('loginForm');
    const usernameInput = document.getElementById('username');
    const passwordInput = document.getElementById('password');
    const errorMessage = document.getElementById('errorMessage');
    const loginButtonText = document.getElementById('loginButtonText');
    const loginSpinner = document.getElementById('loginSpinner');
    
    // Check if already authenticated
    if (Auth.isAuthenticated()) {
        window.location.href = 'index.html';
        return;
    }
    
    // Handle login form submission
    loginForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        const username = usernameInput.value.trim();
        const password = passwordInput.value;
        
        if (!username || !password) {
            showError('Please enter username and password');
            return;
        }
        
        try {
            // Show loading state
            setLoading(true);
            hideError();
            
            // Attempt login
            const result = await Auth.login(username, password);
            
            if (result.success) {
                // Redirect to dashboard
                window.location.href = 'index.html';
            } else {
                showError(result.message || 'Login failed');
            }
        } catch (error) {
            // Show generic message; avoid leaking internal error details
            const userMessage = (error.message && error.message.toLowerCase().includes('unauthorized'))
                ? 'Invalid username or password'
                : 'Login failed. Please try again.';
            showError(userMessage);
        } finally {
            setLoading(false);
        }
    });
    
    function showError(message) {
        errorMessage.textContent = message;
        errorMessage.style.display = 'block';
    }
    
    function hideError() {
        errorMessage.style.display = 'none';
    }
    
    function setLoading(loading) {
        if (loading) {
            loginButtonText.style.display = 'none';
            loginSpinner.style.display = 'inline-block';
            loginForm.querySelectorAll('input').forEach(input => input.disabled = true);
            loginForm.querySelector('button').disabled = true;
        } else {
            loginButtonText.style.display = 'inline';
            loginSpinner.style.display = 'none';
            loginForm.querySelectorAll('input').forEach(input => input.disabled = false);
            loginForm.querySelector('button').disabled = false;
        }
    }
});
