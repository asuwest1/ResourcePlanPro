// Reports and Conflicts Page Script
let allConflicts = [];
let filteredConflicts = [];

document.addEventListener('DOMContentLoaded', async () => {
    await Auth.init();
    await loadConflicts();
    initializeFilters();
});

async function loadConflicts() {
    try {
        const response = await API.dashboard.getConflicts();
        
        if (response.success && response.data) {
            allConflicts = response.data;
            filteredConflicts = [...allConflicts];
            
            updateSummary();
            renderPriorityChart();
            renderConflicts();
        }
    } catch (error) {
        console.error('Error loading conflicts:', error);
        document.getElementById('conflictsContainer').innerHTML = 
            '<p>Error loading conflicts</p>';
    }
}

function updateSummary() {
    const overallocated = allConflicts.filter(c => 
        c.conflictType === 'OverallocatedEmployee'
    ).length;
    
    const understaffed = allConflicts.filter(c => 
        c.conflictType === 'UnderstaffedProject'
    ).length;
    
    document.getElementById('overallocatedCount').textContent = overallocated;
    document.getElementById('understaffedCount').textContent = understaffed;
}

function renderPriorityChart() {
    const priorityCount = {
        High: allConflicts.filter(c => c.priority === 'High').length,
        Medium: allConflicts.filter(c => c.priority === 'Medium').length,
        Low: allConflicts.filter(c => c.priority === 'Low').length
    };
    
    const total = priorityCount.High + priorityCount.Medium + priorityCount.Low;
    
    if (total === 0) {
        document.getElementById('priorityChart').innerHTML = '<p>No conflicts found</p>';
        return;
    }
    
    let html = '<div class="priority-bars">';
    
    ['High', 'Medium', 'Low'].forEach(priority => {
        const count = priorityCount[priority];
        const percent = (count / total * 100).toFixed(0);
        const color = priority === 'High' ? '#e74c3c' : 
                     priority === 'Medium' ? '#f39c12' : '#3498db';
        
        html += `
            <div class="priority-bar-container">
                <div class="priority-label">
                    <span>${priority}</span>
                    <span>${count}</span>
                </div>
                <div class="priority-bar">
                    <div class="priority-bar-fill" 
                         style="width: ${percent}%; background-color: ${color}"></div>
                </div>
            </div>
        `;
    });
    
    html += '</div>';
    document.getElementById('priorityChart').innerHTML = html;
}

function renderConflicts() {
    const container = document.getElementById('conflictsContainer');
    
    if (filteredConflicts.length === 0) {
        container.innerHTML = '<div class="empty-state"><p>No conflicts found</p></div>';
        return;
    }
    
    let html = '<div class="conflicts-list">';
    
    filteredConflicts.forEach(conflict => {
        const priorityClass = conflict.priority.toLowerCase();
        const icon = conflict.conflictType === 'OverallocatedEmployee' ? '⚠️' : '❗';
        const typeLabel = conflict.conflictType === 'OverallocatedEmployee' ? 
            'Over-allocated Employee' : 'Understaffed Project';
        
        html += `
            <div class="conflict-card priority-${priorityClass}">
                <div class="conflict-header">
                    <div class="conflict-icon">${icon}</div>
                    <div class="conflict-info">
                        <h4>${escapeHtml(conflict.entityName)}</h4>
                        <p class="conflict-type">${typeLabel}</p>
                    </div>
                    <div class="conflict-priority">
                        <span class="badge badge-${priorityClass}">${conflict.priority}</span>
                    </div>
                </div>
                <div class="conflict-body">
                    <div class="conflict-details">
                        <div class="detail-item">
                            <label>Department:</label>
                            <span>${escapeHtml(conflict.departmentName)}</span>
                        </div>
                        <div class="detail-item">
                            <label>Week:</label>
                            <span>${Utils.formatDate(conflict.weekStartDate)}</span>
                        </div>
                        <div class="detail-item">
                            <label>Variance:</label>
                            <span class="variance">${conflict.variance.toFixed(1)} hours</span>
                        </div>
                        <div class="detail-item">
                            <label>Utilization:</label>
                            <span>${conflict.utilizationPercentage.toFixed(0)}%</span>
                        </div>
                    </div>
                    <div class="conflict-description">
                        <p>${escapeHtml(conflict.description)}</p>
                        ${conflict.affectedProjects ? 
                            `<p class="affected-projects"><strong>Projects:</strong> ${escapeHtml(conflict.affectedProjects)}</p>` 
                            : ''}
                    </div>
                </div>
                <div class="conflict-actions">
                    <button class="btn btn-sm btn-outline" 
                            onclick="viewConflictDetails('${conflict.conflictType}', ${conflict.entityId})">
                        View Details
                    </button>
                </div>
            </div>
        `;
    });
    
    html += '</div>';
    container.innerHTML = html;
}

function initializeFilters() {
    document.getElementById('conflictFilter').addEventListener('change', applyFilters);
    document.getElementById('priorityFilter').addEventListener('change', applyFilters);
}

function applyFilters() {
    const type = document.getElementById('conflictFilter').value;
    const priority = document.getElementById('priorityFilter').value;
    
    filteredConflicts = allConflicts.filter(conflict => {
        if (type && conflict.conflictType !== type) return false;
        if (priority && conflict.priority !== priority) return false;
        return true;
    });
    
    renderConflicts();
}

function viewConflictDetails(type, id) {
    if (type === 'OverallocatedEmployee') {
        // Navigate to employee details (if page exists)
        Utils.showToast('View employee workload for more details', 'info');
    } else {
        // Navigate to project details
        window.location.href = `project-detail.html?id=${id}`;
    }
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
