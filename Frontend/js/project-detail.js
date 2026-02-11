// Project Detail Page Script
let currentProject = null;
let laborRequirements = [];
let currentWeek = null;
let currentDepartment = null;

document.addEventListener('DOMContentLoaded', async () => {
    await Auth.init();
    
    const urlParams = new URLSearchParams(window.location.search);
    const projectId = urlParams.get('id');
    
    if (!projectId) {
        Utils.showToast('Project ID not specified', 'error');
        window.location.href = 'projects.html';
        return;
    }
    
    await loadProject(projectId);
    initializeTabs();
    initializeEventHandlers();
});

async function loadProject(projectId) {
    try {
        const response = await API.projects.getById(projectId);
        
        if (response.success && response.data) {
            currentProject = response.data;
            updateProjectHeader();
            await loadProjectData();
        } else {
            Utils.showToast('Project not found', 'error');
            window.location.href = 'projects.html';
        }
    } catch (error) {
        console.error('Error loading project:', error);
        Utils.showToast('Error loading project', 'error');
    }
}

function updateProjectHeader() {
    document.getElementById('projectName').textContent = currentProject.projectName;
    
    const info = `${currentProject.priority} Priority | ${currentProject.status}`;
    document.getElementById('projectInfo').textContent = info;
}

async function loadProjectData() {
    await Promise.all([
        loadPlanningGrid(),
        loadOverview(),
        populateWeekSelector(),
        populateDepartmentSelector()
    ]);
}

// ============================================
// Planning Grid
// ============================================
async function loadPlanningGrid() {
    const gridContainer = document.getElementById('planningGrid');
    
    try {
        const response = await API.resources.getRequirements(currentProject.projectId);
        
        if (response.success && response.data) {
            laborRequirements = response.data;
            renderPlanningGrid();
            calculateTotals();
        }
    } catch (error) {
        console.error('Error loading planning grid:', error);
        gridContainer.innerHTML = '<p>Error loading planning grid</p>';
    }
}

function renderPlanningGrid() {
    const gridContainer = document.getElementById('planningGrid');
    
    // Get unique weeks and departments
    const weeks = [...new Set(laborRequirements.map(r => r.weekStartDate))].sort();
    const departments = [...new Set(laborRequirements.map(r => r.departmentName))].sort();
    
    if (weeks.length === 0 || departments.length === 0) {
        gridContainer.innerHTML = '<p>No labor requirements defined yet. Add departments and define weekly hours.</p>';
        return;
    }
    
    // Build grid
    let html = '<table class="labor-grid"><thead><tr><th>Department</th>';
    
    weeks.forEach((week, idx) => {
        const date = new Date(week);
        html += `<th>Week ${idx + 1}<br>${date.getMonth() + 1}/${date.getDate()}</th>`;
    });
    
    html += '<th>Total</th></tr></thead><tbody>';
    
    // Add rows for each department
    departments.forEach(dept => {
        html += `<tr><td class="dept-name">${escapeHtml(dept)}</td>`;
        
        let deptTotal = 0;
        weeks.forEach(week => {
            const req = laborRequirements.find(r => 
                r.departmentName === dept && r.weekStartDate === week
            );
            
            const hours = req ? req.requiredHours : 0;
            const staffingPct = req ? req.staffingPercentage : 0;
            const cellClass = getCellClass(staffingPct);
            
            deptTotal += hours;
            
            html += `<td class="editable-cell ${cellClass}" 
                        data-dept="${escapeHtml(dept)}" 
                        data-week="${week}"
                        data-requirement-id="${req ? req.requirementId : 0}">
                        <input type="number" value="${hours}" min="0" step="0.5" 
                               class="hour-input" />
                    </td>`;
        });
        
        html += `<td class="total-cell">${deptTotal.toFixed(1)}</td></tr>`;
    });
    
    // Add totals row
    html += '<tr class="totals-row"><td>Week Total</td>';
    weeks.forEach(week => {
        const weekTotal = laborRequirements
            .filter(r => r.weekStartDate === week)
            .reduce((sum, r) => sum + r.requiredHours, 0);
        html += `<td>${weekTotal.toFixed(1)}</td>`;
    });
    
    const grandTotal = laborRequirements.reduce((sum, r) => sum + r.requiredHours, 0);
    html += `<td class="grand-total">${grandTotal.toFixed(1)}</td></tr>`;
    
    html += '</tbody></table>';
    
    gridContainer.innerHTML = html;
    
    // Add event listeners to inputs
    gridContainer.querySelectorAll('.hour-input').forEach(input => {
        input.addEventListener('change', handleHourChange);
    });
}

function getCellClass(staffingPct) {
    if (staffingPct === 0) return '';
    if (staffingPct < 60) return 'understaffed-severe';
    if (staffingPct < 85) return 'understaffed-moderate';
    if (staffingPct > 110) return 'overstaffed';
    return 'staffed-good';
}

function handleHourChange(event) {
    const input = event.target;
    const cell = input.closest('.editable-cell');
    const hours = parseFloat(input.value) || 0;
    
    // Update the cell's data
    cell.dataset.hours = hours;
    
    // Recalculate totals
    calculateTotals();
}

function calculateTotals() {
    const gridContainer = document.getElementById('planningGrid');
    const inputs = gridContainer.querySelectorAll('.hour-input');
    
    let total = 0;
    inputs.forEach(input => {
        total += parseFloat(input.value) || 0;
    });
    
    document.getElementById('totalProjectHours').textContent = total.toFixed(1);
    
    // Calculate average
    const weekCount = new Set(
        Array.from(inputs).map(i => i.closest('.editable-cell').dataset.week)
    ).size;
    
    const avg = weekCount > 0 ? total / weekCount : 0;
    document.getElementById('avgWeeklyHours').textContent = avg.toFixed(1);
}

async function saveLaborRequirements() {
    const gridContainer = document.getElementById('planningGrid');
    const cells = gridContainer.querySelectorAll('.editable-cell');
    
    const requirements = [];
    cells.forEach(cell => {
        const input = cell.querySelector('.hour-input');
        const hours = parseFloat(input.value) || 0;
        
        if (hours > 0) {
            requirements.push({
                departmentId: getDepartmentIdByName(cell.dataset.dept),
                weekStartDate: cell.dataset.week,
                requiredHours: hours
            });
        }
    });
    
    if (requirements.length === 0) {
        Utils.showToast('No hours to save', 'error');
        return;
    }
    
    try {
        const response = await API.resources.bulkSaveRequirements({
            projectId: currentProject.projectId,
            requirements: requirements
        });
        
        if (response.success) {
            Utils.showToast('Labor requirements saved successfully', 'success');
            await loadPlanningGrid();
        }
    } catch (error) {
        console.error('Error saving requirements:', error);
        Utils.showToast('Error saving requirements', 'error');
    }
}

function getDepartmentIdByName(name) {
    // This would need to be loaded from the API
    // For now, we'll use a simple lookup
    const deptMap = {
        'Engineering': 1,
        'UX Design': 2,
        'QA Testing': 3,
        'Marketing': 4,
        'Operations': 5,
        'Data Analytics': 6
    };
    return deptMap[name] || 0;
}

// ============================================
// Resource Assignment
// ============================================
async function populateWeekSelector() {
    const selector = document.getElementById('weekSelector');
    
    // Generate weeks from project start to end
    const start = new Date(currentProject.startDate);
    const end = new Date(currentProject.endDate);
    
    let current = Utils.getWeekStartDate(start);
    const endWeek = Utils.getWeekStartDate(end);
    
    let html = '<option value="">Select week...</option>';
    let weekNum = 1;
    
    while (current <= endWeek) {
        const dateStr = current.toISOString().split('T')[0];
        html += `<option value="${dateStr}">Week ${weekNum} - ${Utils.formatDate(current)}</option>`;
        current = new Date(current.setDate(current.getDate() + 7));
        weekNum++;
    }
    
    selector.innerHTML = html;
}

async function populateDepartmentSelector() {
    const selector = document.getElementById('departmentSelector');
    
    // Get unique departments from labor requirements
    const departments = [...new Set(laborRequirements.map(r => ({
        id: r.departmentId,
        name: r.departmentName
    })))];
    
    let html = '<option value="">Select department...</option>';
    departments.forEach(dept => {
        html += `<option value="${dept.id}">${escapeHtml(dept.name)}</option>`;
    });
    
    selector.innerHTML = html;
}

async function loadResourceAssignment() {
    if (!currentWeek || !currentDepartment) {
        return;
    }
    
    await Promise.all([
        loadRequirements(),
        loadAssignedEmployees(),
        loadAvailableEmployees()
    ]);
}

async function loadRequirements() {
    const panel = document.getElementById('requirementsPanel');
    
    const req = laborRequirements.find(r => 
        r.weekStartDate === currentWeek && 
        r.departmentId === parseInt(currentDepartment)
    );
    
    if (!req) {
        panel.innerHTML = '<p>No labor requirements defined for this week/department</p>';
        return;
    }
    
    const remaining = req.requiredHours - req.assignedHours;
    const pct = req.staffingPercentage;
    
    let statusClass = 'status-good';
    let statusText = 'Adequately Staffed';
    
    if (pct < 60) {
        statusClass = 'status-critical';
        statusText = 'Critically Understaffed';
    } else if (pct < 85) {
        statusClass = 'status-warning';
        statusText = 'Understaffed';
    } else if (pct > 110) {
        statusClass = 'status-warning';
        statusText = 'Overstaffed';
    }
    
    panel.innerHTML = `
        <div class="requirement-summary ${statusClass}">
            <div class="req-item">
                <label>Required Hours:</label>
                <strong>${req.requiredHours.toFixed(1)}</strong>
            </div>
            <div class="req-item">
                <label>Assigned Hours:</label>
                <strong>${req.assignedHours.toFixed(1)}</strong>
            </div>
            <div class="req-item">
                <label>Remaining:</label>
                <strong>${remaining.toFixed(1)}</strong>
            </div>
            <div class="req-item">
                <label>Status:</label>
                <strong>${statusText}</strong>
            </div>
            <div class="progress-bar">
                <div class="progress-fill" style="width: ${Math.min(pct, 100)}%"></div>
            </div>
        </div>
    `;
}

async function loadAssignedEmployees() {
    const panel = document.getElementById('assignedPanel');
    
    try {
        const response = await API.resources.getAssignments(
            currentProject.projectId, 
            currentWeek
        );
        
        if (response.success && response.data) {
            const assignments = response.data.filter(a => {
                // Filter by department
                const emp = a.employeeId; // Would need employee dept lookup
                return true; // For now, show all
            });
            
            if (assignments.length === 0) {
                panel.innerHTML = '<p>No employees assigned yet</p>';
                return;
            }
            
            let html = '<div class="assignment-list">';
            assignments.forEach(assignment => {
                html += `
                    <div class="assignment-item">
                        <div class="assignment-info">
                            <strong>${escapeHtml(assignment.employeeName)}</strong>
                            <span class="assignment-hours">${assignment.assignedHours} hrs</span>
                        </div>
                        <div class="assignment-actions">
                            <button class="btn-icon" onclick="editAssignment(${assignment.assignmentId})" 
                                    title="Edit">✏️</button>
                            <button class="btn-icon" onclick="deleteAssignment(${assignment.assignmentId})" 
                                    title="Remove">🗑️</button>
                        </div>
                    </div>
                `;
            });
            html += '</div>';
            
            panel.innerHTML = html;
        }
    } catch (error) {
        console.error('Error loading assignments:', error);
        panel.innerHTML = '<p>Error loading assignments</p>';
    }
}

async function loadAvailableEmployees() {
    const panel = document.getElementById('availablePanel');
    
    try {
        const response = await API.resources.getAvailableEmployees(
            parseInt(currentDepartment),
            currentWeek,
            0
        );
        
        if (response.success && response.data) {
            const employees = response.data;
            
            if (employees.length === 0) {
                panel.innerHTML = '<p>No available employees</p>';
                return;
            }
            
            let html = '<div class="employee-list">';
            employees.forEach(emp => {
                html += `
                    <div class="employee-item">
                        <div class="employee-info">
                            <strong>${escapeHtml(emp.firstName)} ${escapeHtml(emp.lastName)}</strong>
                            <div class="employee-meta">
                                <span>${escapeHtml(emp.jobTitle)}</span>
                                <span>Available: ${emp.availableHours.toFixed(1)} hrs</span>
                            </div>
                        </div>
                        <button class="btn btn-sm btn-primary" 
                                onclick="openAssignmentModal(${emp.employeeId}, '${escapeHtml(emp.firstName)} ${escapeHtml(emp.lastName)}', ${emp.availableHours})">
                            Assign
                        </button>
                    </div>
                `;
            });
            html += '</div>';
            
            panel.innerHTML = html;
        }
    } catch (error) {
        console.error('Error loading available employees:', error);
        panel.innerHTML = '<p>Error loading employees</p>';
    }
}

// ============================================
// Assignment Modal
// ============================================
function openAssignmentModal(employeeId, employeeName, availableHours) {
    document.getElementById('modalEmployeeId').value = employeeId;
    document.getElementById('modalEmployeeName').value = employeeName;
    document.getElementById('modalAvailableHours').value = `${availableHours.toFixed(1)} hours`;
    document.getElementById('modalWeekStart').value = currentWeek;
    document.getElementById('modalAssignedHours').value = '';
    document.getElementById('modalAssignedHours').max = availableHours;
    document.getElementById('modalNotes').value = '';
    
    document.getElementById('assignmentModal').style.display = 'flex';
}

function closeAssignmentModal() {
    document.getElementById('assignmentModal').style.display = 'none';
}

async function submitAssignment(event) {
    event.preventDefault();
    
    const employeeId = parseInt(document.getElementById('modalEmployeeId').value);
    const hours = parseFloat(document.getElementById('modalAssignedHours').value);
    const notes = document.getElementById('modalNotes').value;
    
    try {
        const response = await API.resources.createAssignment({
            projectId: currentProject.projectId,
            employeeId: employeeId,
            weekStartDate: currentWeek,
            assignedHours: hours,
            notes: notes
        });
        
        if (response.success) {
            Utils.showToast('Employee assigned successfully', 'success');
            closeAssignmentModal();
            await loadResourceAssignment();
        }
    } catch (error) {
        console.error('Error creating assignment:', error);
        Utils.showToast(error.message || 'Error creating assignment', 'error');
    }
}

async function deleteAssignment(assignmentId) {
    if (!confirm('Remove this employee assignment?')) {
        return;
    }
    
    try {
        const response = await API.resources.deleteAssignment(assignmentId);
        
        if (response.success) {
            Utils.showToast('Assignment removed', 'success');
            await loadResourceAssignment();
        }
    } catch (error) {
        console.error('Error deleting assignment:', error);
        Utils.showToast('Error removing assignment', 'error');
    }
}

// ============================================
// Overview Tab
// ============================================
async function loadOverview() {
    // Update project info
    document.getElementById('projectManager').textContent = currentProject.projectManagerName;
    document.getElementById('startDate').textContent = Utils.formatDate(currentProject.startDate);
    document.getElementById('endDate').textContent = Utils.formatDate(currentProject.endDate);
    document.getElementById('priority').textContent = currentProject.priority;
    document.getElementById('status').textContent = currentProject.status;
    
    const start = new Date(currentProject.startDate);
    const end = new Date(currentProject.endDate);
    const weeks = Math.ceil((end - start) / (7 * 24 * 60 * 60 * 1000));
    document.getElementById('duration').textContent = `${weeks} weeks`;
    
    // Update resource summary
    document.getElementById('deptCount').textContent = currentProject.departmentCount;
    document.getElementById('empCount').textContent = currentProject.employeeCount;
    
    const totalHours = laborRequirements.reduce((sum, r) => sum + r.requiredHours, 0);
    const assignedHours = laborRequirements.reduce((sum, r) => sum + r.assignedHours, 0);
    
    document.getElementById('totalHours').textContent = totalHours.toFixed(1);
    document.getElementById('assignedHours').textContent = assignedHours.toFixed(1);
}

// ============================================
// Event Handlers
// ============================================
function initializeTabs() {
    document.querySelectorAll('.tab-button').forEach(button => {
        button.addEventListener('click', () => {
            // Remove active class from all tabs
            document.querySelectorAll('.tab-button').forEach(b => b.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
            
            // Add active class to clicked tab
            button.classList.add('active');
            const tabId = button.dataset.tab + '-tab';
            document.getElementById(tabId).classList.add('active');
        });
    });
}

function initializeEventHandlers() {
    // Save labor requirements
    document.getElementById('saveLaborRequirements').addEventListener('click', saveLaborRequirements);
    
    // Week and department selectors
    document.getElementById('weekSelector').addEventListener('change', (e) => {
        currentWeek = e.target.value;
        if (currentWeek && currentDepartment) {
            loadResourceAssignment();
        }
    });
    
    document.getElementById('departmentSelector').addEventListener('change', (e) => {
        currentDepartment = e.target.value;
        if (currentWeek && currentDepartment) {
            loadResourceAssignment();
        }
    });
    
    // Assignment form
    document.getElementById('assignmentForm').addEventListener('submit', submitAssignment);
    
    // Delete project
    document.getElementById('deleteProjectBtn').addEventListener('click', async () => {
        if (confirm('Are you sure you want to delete this project? This cannot be undone.')) {
            try {
                const response = await API.projects.delete(currentProject.projectId);
                if (response.success) {
                    Utils.showToast('Project deleted', 'success');
                    window.location.href = 'projects.html';
                }
            } catch (error) {
                Utils.showToast('Error deleting project', 'error');
            }
        }
    });
    
    // Edit project
    document.getElementById('editProjectBtn').addEventListener('click', () => {
        window.location.href = `project-edit.html?id=${currentProject.projectId}`;
    });
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
