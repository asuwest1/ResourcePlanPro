// API Configuration
const CONFIG = {
    API_BASE_URL: 'https://localhost:7001/api', // Update with your API URL
    API_TIMEOUT: 30000,
    TOKEN_KEY: 'resourceplan_token',
    USER_KEY: 'resourceplan_user',
    
    // Date format
    DATE_FORMAT: 'YYYY-MM-DD',
    DISPLAY_DATE_FORMAT: 'MMM DD, YYYY',
    
    // Pagination
    DEFAULT_PAGE_SIZE: 20,
    
    // Resource status colors
    STATUS_COLORS: {
        Green: '#27ae60',
        Yellow: '#f39c12',
        Red: '#e74c3c'
    },
    
    // Priority levels
    PRIORITIES: ['Low', 'Medium', 'High', 'Critical'],
    
    // Project statuses
    PROJECT_STATUSES: ['Planning', 'Active', 'OnHold', 'Completed', 'Cancelled']
};

// Utility functions
const Utils = {
    // Format date
    formatDate(date, format = CONFIG.DISPLAY_DATE_FORMAT) {
        if (!date) return '';
        const d = new Date(date);
        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        const month = months[d.getMonth()];
        const day = d.getDate();
        const year = d.getFullYear();
        return `${month} ${day}, ${year}`;
    },
    
    // Get week start date (Monday)
    getWeekStartDate(date = new Date()) {
        const d = new Date(date);
        const day = d.getDay();
        const diff = d.getDate() - day + (day === 0 ? -6 : 1);
        return new Date(d.setDate(diff));
    },
    
    // Format hours
    formatHours(hours) {
        return `${parseFloat(hours).toFixed(1)} hrs`;
    },
    
    // Get status class
    getStatusClass(status) {
        return `status-${status.toLowerCase()}`;
    },
    
    // Show toast notification
    showToast(message, type = 'info') {
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.textContent = message;
        toast.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 1rem 1.5rem;
            background-color: ${type === 'success' ? '#27ae60' : type === 'error' ? '#e74c3c' : '#3498db'};
            color: white;
            border-radius: 4px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.2);
            z-index: 10000;
            animation: slideIn 0.3s ease-out;
        `;
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.style.animation = 'slideOut 0.3s ease-out';
            setTimeout(() => document.body.removeChild(toast), 300);
        }, 3000);
    },
    
    // Debounce function
    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
};

// Add CSS animations
const style = document.createElement('style');
style.textContent = `
    @keyframes slideIn {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    
    @keyframes slideOut {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100%);
            opacity: 0;
        }
    }
`;
document.head.appendChild(style);
