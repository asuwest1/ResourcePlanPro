// Authentication Module
const Auth = {
    // Check if user is authenticated
    isAuthenticated() {
        return !!this.getToken();
    },
    
    // Get stored token
    getToken() {
        return localStorage.getItem(CONFIG.TOKEN_KEY);
    },
    
    // Set token
    setToken(token) {
        localStorage.setItem(CONFIG.TOKEN_KEY, token);
    },
    
    // Get stored user
    getUser() {
        const userJson = localStorage.getItem(CONFIG.USER_KEY);
        return userJson ? JSON.parse(userJson) : null;
    },
    
    // Set user
    setUser(user) {
        localStorage.setItem(CONFIG.USER_KEY, JSON.stringify(user));
    },
    
    // Clear authentication
    clearAuth() {
        localStorage.removeItem(CONFIG.TOKEN_KEY);
        localStorage.removeItem(CONFIG.USER_KEY);
    },
    
    // Login
    async login(username, password) {
        try {
            const response = await fetch(`${CONFIG.API_BASE_URL}/auth/login`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ username, password })
            });
            
            const data = await response.json();
            
            if (!response.ok) {
                throw new Error(data.message || 'Login failed');
            }
            
            if (data.success && data.token) {
                this.setToken(data.token);
                this.setUser(data.user);
                return data;
            } else {
                throw new Error(data.message || 'Login failed');
            }
        } catch (error) {
            console.error('Login error:', error);
            throw error;
        }
    },
    
    // Logout
    async logout() {
        try {
            const token = this.getToken();
            if (token) {
                await fetch(`${CONFIG.API_BASE_URL}/auth/logout`, {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${token}`
                    }
                });
            }
        } catch (error) {
            console.error('Logout error:', error);
        } finally {
            this.clearAuth();
            window.location.href = 'login.html';
        }
    },
    
    // Validate token
    async validateToken() {
        try {
            const token = this.getToken();
            if (!token) {
                return false;
            }
            
            const response = await fetch(`${CONFIG.API_BASE_URL}/auth/validate`, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
            
            if (!response.ok) {
                this.clearAuth();
                return false;
            }
            
            const user = await response.json();
            this.setUser(user);
            return true;
        } catch (error) {
            console.error('Token validation error:', error);
            this.clearAuth();
            return false;
        }
    },
    
    // Redirect to login if not authenticated
    requireAuth() {
        if (!this.isAuthenticated()) {
            window.location.href = 'login.html';
            return false;
        }
        return true;
    },
    
    // Initialize authentication on page load
    async init() {
        // If on login page and already authenticated, redirect to dashboard
        if (window.location.pathname.includes('login.html') && this.isAuthenticated()) {
            window.location.href = 'index.html';
            return;
        }
        
        // If not on login page, require authentication
        if (!window.location.pathname.includes('login.html')) {
            if (!this.requireAuth()) {
                return;
            }
            
            // Validate token
            const isValid = await this.validateToken();
            if (!isValid) {
                window.location.href = 'login.html';
            }
        }
    }
};

// Initialize user menu and logout functionality
document.addEventListener('DOMContentLoaded', () => {
    // Set up user menu
    const user = Auth.getUser();
    if (user) {
        const userNameElement = document.getElementById('userName');
        if (userNameElement) {
            userNameElement.textContent = user.fullName || user.username;
        }
    }
    
    // User menu dropdown
    const userMenuBtn = document.getElementById('userMenuBtn');
    const userDropdown = document.getElementById('userDropdown');
    if (userMenuBtn && userDropdown) {
        userMenuBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            const isVisible = userDropdown.style.display === 'block';
            userDropdown.style.display = isVisible ? 'none' : 'block';
        });
        
        document.addEventListener('click', () => {
            userDropdown.style.display = 'none';
        });
    }
    
    // Logout link
    const logoutLink = document.getElementById('logoutLink');
    if (logoutLink) {
        logoutLink.addEventListener('click', async (e) => {
            e.preventDefault();
            await Auth.logout();
        });
    }
});
