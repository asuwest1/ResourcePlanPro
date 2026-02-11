// Projects List Page Script
let allProjects = [];
let filteredProjects = [];

document.addEventListener('DOMContentLoaded', async () => {
    await Auth.init();
    
    await loadProjects();
    initializeFilters();
    initializeSearch();
});

async function loadProjects() {
    const container = document.getElementById('projectsContainer');
    
    try {
        const response = await API.projects.getAll();
        
        if (response.success && response.data) {
            allProjects = response.data;
            filteredProjects = [...allProjects];
            renderProjects();
        }
    } catch (error) {
        console.error('Error loading projects:', error);
        container.innerHTML = '<p class="text-center">Error loading projects</p>';
    }
}

function renderProjects() {
    const container = document.getElementById('projectsContainer');
    
    if (filteredProjects.length === 0) {
        container.innerHTML = '<div class="empty-state"><p>No projects found matching your filters</p></div>';
        return;
    }
    
    let html = '<div class="projects-table-container"><table class="data-table"><thead><tr>';
    html += '<th>Project Name</th>';
    html += '<th>Manager</th>';
    html += '<th>Status</th>';
    html += '<th>Priority</th>';
    html += '<th>Start Date</th>';
    html += '<th>End Date</th>';
    html += '<th>Departments</th>';
    html += '<th>Staffing</th>';
    html += '<th>Actions</th>';
    html += '</tr></thead><tbody>';
    
    filteredProjects.forEach(project => {
        const statusClass = project.currentWeekStatus.toLowerCase();
        const statusIcon = statusClass === 'green' ? '🟢' : statusClass === 'yellow' ? '🟡' : '🔴';
        
        html += '<tr>';
        html += `<td><a href="project-detail.html?id=${project.projectId}" class="project-link">${escapeHtml(project.projectName)}</a></td>`;
        html += `<td>${escapeHtml(project.projectManagerName)}</td>`;
        html += `<td><span class="badge badge-${project.status.toLowerCase()}">${project.status}</span></td>`;
        html += `<td><span class="badge badge-priority-${project.priority.toLowerCase()}">${project.priority}</span></td>`;
        html += `<td>${Utils.formatDate(project.startDate)}</td>`;
        html += `<td>${Utils.formatDate(project.endDate)}</td>`;
        html += `<td>${project.departmentCount}</td>`;
        html += `<td>${statusIcon} ${project.currentWeekAssignedHours.toFixed(0)}/${project.currentWeekRequiredHours.toFixed(0)} hrs</td>`;
        html += `<td>
            <button class="btn-icon view-btn" data-id="${parseInt(project.projectId, 10)}" title="View">👁️</button>
            <button class="btn-icon edit-btn" data-id="${parseInt(project.projectId, 10)}" title="Edit">✏️</button>
        </td>`;
        html += '</tr>';
    });
    
    html += '</tbody></table></div>';
    container.innerHTML = html;

    // Attach event listeners instead of inline onclick
    container.querySelectorAll('.view-btn').forEach(btn => {
        btn.addEventListener('click', () => viewProject(parseInt(btn.dataset.id, 10)));
    });
    container.querySelectorAll('.edit-btn').forEach(btn => {
        btn.addEventListener('click', () => editProject(parseInt(btn.dataset.id, 10)));
    });
}

function initializeFilters() {
    document.getElementById('statusFilter').addEventListener('change', applyFilters);
    document.getElementById('priorityFilter').addEventListener('change', applyFilters);
    document.getElementById('viewFilter').addEventListener('change', applyFilters);
}

function initializeSearch() {
    const searchInput = document.getElementById('projectSearch');
    searchInput.addEventListener('input', Utils.debounce(() => {
        applyFilters();
    }, 300));
}

function applyFilters() {
    const status = document.getElementById('statusFilter').value;
    const priority = document.getElementById('priorityFilter').value;
    const view = document.getElementById('viewFilter').value;
    const search = document.getElementById('projectSearch').value.toLowerCase();
    
    const currentUser = Auth.getUser();
    
    filteredProjects = allProjects.filter(project => {
        // Status filter
        if (status && project.status !== status) return false;
        
        // Priority filter
        if (priority && project.priority !== priority) return false;
        
        // View filter
        if (view === 'mine' && project.projectManagerId !== currentUser.userId) return false;
        
        // Search filter
        if (search && !project.projectName.toLowerCase().includes(search)) return false;
        
        return true;
    });
    
    renderProjects();
}

function viewProject(projectId) {
    window.location.href = `project-detail.html?id=${projectId}`;
}

function editProject(projectId) {
    window.location.href = `project-edit.html?id=${projectId}`;
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
