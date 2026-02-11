// Dashboard Page Script
document.addEventListener('DOMContentLoaded', async () => {
    // Check authentication
    await Auth.init();
    
    // Initialize dashboard
    initializeDashboard();
});

async function initializeDashboard() {
    try {
        // Set welcome message
        const user = Auth.getUser();
        if (user) {
            document.getElementById('welcomeMessage').textContent = `Welcome back, ${user.firstName} ${user.lastName}`;
        }
        
        // Set current week
        const weekStart = Utils.getWeekStartDate();
        document.getElementById('currentWeek').textContent = `Week of ${Utils.formatDate(weekStart)}`;
        
        // Load dashboard data
        await Promise.all([
            loadQuickStats(),
            loadProjects(),
            loadResourceTimeline()
        ]);
        
    } catch (error) {
        console.error('Dashboard initialization error:', error);
        Utils.showToast('Error loading dashboard data', 'error');
    }
}

async function loadQuickStats() {
    try {
        const response = await API.dashboard.getStats();
        
        if (response.success && response.data) {
            const stats = response.data;
            document.getElementById('activeProjects').textContent = stats.activeProjects;
            document.getElementById('totalEmployees').textContent = stats.totalEmployees;
            document.getElementById('avgUtilization').textContent = `${stats.averageUtilization}%`;
            document.getElementById('overallocatedCount').textContent = stats.overallocatedEmployees;
            document.getElementById('understaffedCount').textContent = stats.understaffedProjects;
        }
    } catch (error) {
        console.error('Error loading quick stats:', error);
    }
}

async function loadProjects() {
    const projectsGrid = document.getElementById('projectsGrid');
    
    try {
        const response = await API.projects.getAll();
        
        if (response.success && response.data) {
            const projects = response.data;
            
            if (projects.length === 0) {
                projectsGrid.innerHTML = '<p class="text-center">No active projects found</p>';
                return;
            }
            
            projectsGrid.innerHTML = projects.map(project => createProjectCard(project)).join('');
            
            // Add click handlers
            projectsGrid.querySelectorAll('.project-card').forEach(card => {
                card.addEventListener('click', () => {
                    const projectId = card.dataset.projectId;
                    window.location.href = `pages/project-detail.html?id=${projectId}`;
                });
            });
        }
    } catch (error) {
        console.error('Error loading projects:', error);
        projectsGrid.innerHTML = '<p class="text-center">Error loading projects</p>';
    }
}

function createProjectCard(project) {
    const statusClass = project.currentWeekStatus.toLowerCase();
    const progressPercent = project.currentWeekRequiredHours > 0 
        ? (project.currentWeekAssignedHours / project.currentWeekRequiredHours * 100) 
        : 0;
    
    const statusIcon = statusClass === 'green' ? '🟢' : statusClass === 'yellow' ? '🟡' : '🔴';
    
    return `
        <div class="project-card status-${statusClass}" data-project-id="${project.projectId}">
            <h3>${escapeHtml(project.projectName)}</h3>
            <div class="progress-bar">
                <div class="progress-fill" style="width: ${progressPercent}%"></div>
            </div>
            <div class="project-meta">
                <div>
                    <span>Status: ${statusIcon}</span>
                </div>
                <div>
                    <span>This Week:</span>
                    <strong>${Utils.formatHours(project.currentWeekAssignedHours)}/${Utils.formatHours(project.currentWeekRequiredHours)}</strong>
                </div>
                <div>
                    <span>Departments:</span>
                    <strong>${project.departmentCount}</strong>
                </div>
            </div>
            <button class="btn btn-sm btn-primary btn-block" onclick="event.stopPropagation(); window.location.href='pages/project-detail.html?id=${project.projectId}'">
                View Details
            </button>
        </div>
    `;
}

async function loadResourceTimeline() {
    const timelineContainer = document.getElementById('resourceTimeline');
    
    try {
        const response = await API.resources.getTimeline();
        
        if (response.success && response.data) {
            const timeline = response.data;
            
            // Group by department
            const departments = {};
            timeline.forEach(item => {
                if (!departments[item.departmentName]) {
                    departments[item.departmentName] = [];
                }
                departments[item.departmentName].push(item);
            });
            
            // Get unique weeks
            const weeks = [...new Set(timeline.map(t => t.weekNumber))].sort((a, b) => a - b);
            
            // Build table
            let html = '<table class="timeline-table"><thead><tr><th>Department</th>';
            weeks.forEach(weekNum => {
                const weekData = timeline.find(t => t.weekNumber === weekNum);
                if (weekData) {
                    const weekDate = new Date(weekData.weekStart);
                    html += `<th>W${weekNum + 1}<br>${weekDate.getMonth() + 1}/${weekDate.getDate()}</th>`;
                }
            });
            html += '</tr></thead><tbody>';
            
            // Add department rows
            Object.keys(departments).sort().forEach(deptName => {
                html += `<tr><td>${escapeHtml(deptName)}</td>`;
                const deptData = departments[deptName];
                weeks.forEach(weekNum => {
                    const weekData = deptData.find(d => d.weekNumber === weekNum);
                    const loadLevel = weekData ? weekData.loadLevel.toLowerCase() : 'light';
                    const utilization = weekData ? weekData.utilizationPercentage.toFixed(0) : '0';
                    html += `<td class="timeline-cell ${loadLevel}">${utilization}%</td>`;
                });
                html += '</tr>';
            });
            
            html += '</tbody></table>';
            timelineContainer.innerHTML = html;
        }
    } catch (error) {
        console.error('Error loading resource timeline:', error);
        timelineContainer.innerHTML = '<p class="text-center">Error loading timeline</p>';
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
