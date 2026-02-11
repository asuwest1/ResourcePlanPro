// API Client Module
const API = {
    // Make authenticated request
    async request(endpoint, options = {}) {
        const token = Auth.getToken();
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };
        
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }
        
        const config = {
            ...options,
            headers
        };
        
        try {
            const response = await fetch(`${CONFIG.API_BASE_URL}${endpoint}`, config);
            
            // Handle 401 Unauthorized
            if (response.status === 401) {
                Auth.clearAuth();
                window.location.href = 'login.html';
                throw new Error('Unauthorized');
            }
            
            const data = await response.json();
            
            if (!response.ok) {
                throw new Error(data.message || 'API request failed');
            }
            
            return data;
        } catch (error) {
            console.error('API request error:', error);
            throw error;
        }
    },
    
    // Dashboard endpoints
    dashboard: {
        async get() {
            return await API.request('/dashboard');
        },
        async getConflicts() {
            return await API.request('/dashboard/conflicts');
        },
        async getStats() {
            return await API.request('/dashboard/stats');
        }
    },
    
    // Projects endpoints
    projects: {
        async getAll(managerId = null) {
            const query = managerId ? `?managerId=${managerId}` : '';
            return await API.request(`/projects${query}`);
        },
        async getById(id) {
            return await API.request(`/projects/${id}`);
        },
        async create(project) {
            return await API.request('/projects', {
                method: 'POST',
                body: JSON.stringify(project)
            });
        },
        async update(id, project) {
            return await API.request(`/projects/${id}`, {
                method: 'PUT',
                body: JSON.stringify(project)
            });
        },
        async delete(id) {
            return await API.request(`/projects/${id}`, {
                method: 'DELETE'
            });
        }
    },
    
    // Resources endpoints
    resources: {
        async getRequirements(projectId, weekStartDate = null) {
            const query = weekStartDate 
                ? `?projectId=${projectId}&weekStartDate=${weekStartDate}` 
                : `?projectId=${projectId}`;
            return await API.request(`/resources/requirements${query}`);
        },
        async saveRequirement(requirement) {
            return await API.request('/resources/requirements', {
                method: 'POST',
                body: JSON.stringify(requirement)
            });
        },
        async bulkSaveRequirements(requirements) {
            return await API.request('/resources/requirements/bulk', {
                method: 'POST',
                body: JSON.stringify(requirements)
            });
        },
        async getAvailableEmployees(departmentId, weekStartDate, minHours = 0) {
            const query = `?departmentId=${departmentId}&weekStartDate=${weekStartDate}&minAvailableHours=${minHours}`;
            return await API.request(`/resources/available-employees${query}`);
        },
        async getAssignments(projectId, weekStartDate = null) {
            const query = weekStartDate 
                ? `?projectId=${projectId}&weekStartDate=${weekStartDate}` 
                : `?projectId=${projectId}`;
            return await API.request(`/resources/assignments${query}`);
        },
        async createAssignment(assignment) {
            return await API.request('/resources/assignments', {
                method: 'POST',
                body: JSON.stringify(assignment)
            });
        },
        async updateAssignment(id, assignment) {
            return await API.request(`/resources/assignments/${id}`, {
                method: 'PUT',
                body: JSON.stringify(assignment)
            });
        },
        async deleteAssignment(id) {
            return await API.request(`/resources/assignments/${id}`, {
                method: 'DELETE'
            });
        },
        async getTimeline(startDate = null, weekCount = 12) {
            const query = startDate 
                ? `?startDate=${startDate}&weekCount=${weekCount}` 
                : `?weekCount=${weekCount}`;
            return await API.request(`/resources/timeline${query}`);
        }
    },
    
    // Employees endpoints
    employees: {
        async getAll(includeInactive = false) {
            const query = includeInactive ? '?includeInactive=true' : '';
            return await API.request(`/employees${query}`);
        },
        async getById(id) {
            return await API.request(`/employees/${id}`);
        },
        async getByDepartment(departmentId) {
            return await API.request(`/employees/department/${departmentId}`);
        },
        async getWorkload(id, weekStartDate) {
            return await API.request(`/employees/${id}/workload?weekStartDate=${weekStartDate}`);
        }
    }
};
