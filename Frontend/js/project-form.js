// Project Form Script (Create/Edit)
let isEditMode = false;
let projectId = null;
let currentProject = null;

document.addEventListener('DOMContentLoaded', async () => {
    await Auth.init();
    
    // Check if edit mode
    const urlParams = new URLSearchParams(window.location.search);
    projectId = urlParams.get('id');
    isEditMode = !!projectId;
    
    if (isEditMode) {
        document.getElementById('pageTitle').textContent = 'Edit Project';
        document.getElementById('submitText').textContent = 'Update Project';
        await loadProject();
    }
    
    await loadFormData();
    initializeForm();
});

async function loadProject() {
    try {
        const response = await API.projects.getById(projectId);
        if (response.success && response.data) {
            currentProject = response.data;
            populateForm();
        }
    } catch (error) {
        console.error('Error loading project:', error);
        Utils.showToast('Error loading project', 'error');
        window.location.href = 'projects.html';
    }
}

async function loadFormData() {
    await Promise.all([
        loadProjectManagers(),
        loadDepartments()
    ]);
}

async function loadProjectManagers() {
    // This would ideally come from an API endpoint
    // For now, get current user and set as default
    const user = Auth.getUser();
    const select = document.getElementById('projectManagerId');
    
    // In a real implementation, you'd fetch all users with ProjectManager role
    select.innerHTML = `<option value="${user.userId}" selected>${user.fullName}</option>`;
}

async function loadDepartments() {
    const container = document.getElementById('departmentsList');
    
    try {
        const response = await API.employees.getAll();
        
        if (response.success && response.data) {
            const employees = response.data;
            
            // Get unique departments
            const departments = [...new Map(
                employees.map(e => [e.departmentId, {id: e.departmentId, name: e.departmentName}])
            ).values()];
            
            let html = '';
            departments.forEach(dept => {
                const checked = isEditMode && currentProject ? 
                    checkIfDepartmentSelected(dept.id) : false;
                
                html += `
                    <div class="checkbox-item">
                        <label>
                            <input type="checkbox" name="departments" 
                                   value="${dept.id}" ${checked ? 'checked' : ''}>
                            <span>${escapeHtml(dept.name)}</span>
                        </label>
                    </div>
                `;
            });
            
            container.innerHTML = html;
        }
    } catch (error) {
        console.error('Error loading departments:', error);
        container.innerHTML = '<p>Error loading departments</p>';
    }
}

function checkIfDepartmentSelected(deptId) {
    // This would need to be loaded from the project data
    // For now, return false
    return false;
}

function populateForm() {
    if (!currentProject) return;
    
    document.getElementById('projectName').value = currentProject.projectName;
    document.getElementById('description').value = currentProject.description || '';
    document.getElementById('startDate').value = currentProject.startDate.split('T')[0];
    document.getElementById('endDate').value = currentProject.endDate.split('T')[0];
    document.getElementById('priority').value = currentProject.priority;
    document.getElementById('projectManagerId').value = currentProject.projectManagerId;
}

function initializeForm() {
    const form = document.getElementById('projectForm');
    form.addEventListener('submit', handleSubmit);
    
    // Set minimum dates
    const today = new Date().toISOString().split('T')[0];
    const startInput = document.getElementById('startDate');
    const endInput = document.getElementById('endDate');
    
    if (!isEditMode) {
        startInput.min = today;
    }
    
    startInput.addEventListener('change', () => {
        endInput.min = startInput.value;
    });
}

async function handleSubmit(event) {
    event.preventDefault();
    
    const formData = new FormData(event.target);
    const departments = Array.from(
        document.querySelectorAll('input[name="departments"]:checked')
    ).map(cb => parseInt(cb.value));
    
    if (departments.length === 0) {
        Utils.showToast('Please select at least one department', 'error');
        return;
    }
    
    const projectData = {
        projectName: formData.get('projectName'),
        description: formData.get('description'),
        startDate: formData.get('startDate'),
        endDate: formData.get('endDate'),
        priority: formData.get('priority'),
        projectManagerId: parseInt(formData.get('projectManagerId')),
        departmentIds: departments
    };
    
    // Validate dates
    const start = new Date(projectData.startDate);
    const end = new Date(projectData.endDate);
    
    if (end <= start) {
        Utils.showToast('End date must be after start date', 'error');
        return;
    }
    
    setLoading(true);
    
    try {
        let response;
        if (isEditMode) {
            response = await API.projects.update(projectId, projectData);
        } else {
            response = await API.projects.create(projectData);
        }
        
        if (response.success) {
            Utils.showToast(
                isEditMode ? 'Project updated successfully' : 'Project created successfully', 
                'success'
            );
            
            // Navigate to project detail page
            const newProjectId = isEditMode ? projectId : response.data.projectId;
            setTimeout(() => {
                window.location.href = `project-detail.html?id=${newProjectId}`;
            }, 1000);
        }
    } catch (error) {
        console.error('Error saving project:', error);
        Utils.showToast(error.message || 'Error saving project', 'error');
        setLoading(false);
    }
}

function setLoading(loading) {
    const submitBtn = document.getElementById('submitBtn');
    const submitText = document.getElementById('submitText');
    const submitSpinner = document.getElementById('submitSpinner');
    
    if (loading) {
        submitBtn.disabled = true;
        submitText.style.display = 'none';
        submitSpinner.style.display = 'inline-block';
    } else {
        submitBtn.disabled = false;
        submitText.style.display = 'inline';
        submitSpinner.style.display = 'none';
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
