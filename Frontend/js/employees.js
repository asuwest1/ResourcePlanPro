// Employees Page Script
let allEmployees = [];
let filteredEmployees = [];

document.addEventListener('DOMContentLoaded', async () => {
    await Auth.init();
    await loadEmployees();
    initializeFilters();
    initializeSearch();
    initializeExport();
});

async function loadEmployees() {
    try {
        const response = await API.employees.getAll(false);
        
        if (response.success && response.data) {
            allEmployees = response.data;
            filteredEmployees = [...allEmployees];
            populateDepartmentFilter();
            renderEmployees();
        }
    } catch (error) {
        console.error('Error loading employees:', error);
        document.getElementById('employeesContainer').innerHTML = 
            '<p class="text-center">Error loading employees</p>';
    }
}

function populateDepartmentFilter() {
    const departments = [...new Map(
        allEmployees.map(e => [e.departmentId, { id: e.departmentId, name: e.departmentName }])
    ).values()];

    const select = document.getElementById('departmentFilter');
    let html = '<option value="">All Departments</option>';
    departments.forEach(dept => {
        html += `<option value="${dept.id}">${Utils.escapeHtml(dept.name)}</option>`;
    });
    select.innerHTML = html;
}

function renderEmployees() {
    const container = document.getElementById('employeesContainer');
    
    if (filteredEmployees.length === 0) {
        container.innerHTML = '<div class="empty-state"><p>No employees found</p></div>';
        return;
    }
    
    let html = '<div class="employees-grid">';
    
    filteredEmployees.forEach(employee => {
        const statusClass = employee.isActive ? 'active' : 'inactive';
        html += `
            <div class="employee-card ${statusClass}">
                <div class="employee-header">
                    <div class="employee-avatar">
                        ${employee.firstName.charAt(0)}${employee.lastName.charAt(0)}
                    </div>
                    <div class="employee-info">
                        <h3>${Utils.escapeHtml(employee.fullName)}</h3>
                        <p>${Utils.escapeHtml(employee.jobTitle)}</p>
                    </div>
                </div>
                <div class="employee-details">
                    <div class="detail-row">
                        <span class="label">Department:</span>
                        <span>${Utils.escapeHtml(employee.departmentName)}</span>
                    </div>
                    <div class="detail-row">
                        <span class="label">Email:</span>
                        <span>${Utils.escapeHtml(employee.email)}</span>
                    </div>
                    <div class="detail-row">
                        <span class="label">Hours/Week:</span>
                        <span>${employee.hoursPerWeek}</span>
                    </div>
                    ${employee.skills ? `
                    <div class="detail-row">
                        <span class="label">Skills:</span>
                        <span>${Utils.escapeHtml(employee.skills)}</span>
                    </div>` : ''}
                </div>
            </div>
        `;
    });
    
    html += '</div>';
    container.innerHTML = html;
}

function initializeFilters() {
    document.getElementById('departmentFilter').addEventListener('change', applyFilters);
    document.getElementById('statusFilter').addEventListener('change', applyFilters);
}

function initializeSearch() {
    document.getElementById('employeeSearch').addEventListener('input', 
        Utils.debounce(applyFilters, 300));
}

function applyFilters() {
    const department = document.getElementById('departmentFilter').value;
    const status = document.getElementById('statusFilter').value;
    const search = document.getElementById('employeeSearch').value.toLowerCase();
    
    filteredEmployees = allEmployees.filter(emp => {
        if (department && emp.departmentId !== parseInt(department)) return false;
        if (status !== '' && emp.isActive !== (status === 'true')) return false;
        if (search && !emp.fullName.toLowerCase().includes(search) && 
            !emp.email.toLowerCase().includes(search)) return false;
        return true;
    });
    
    renderEmployees();
}

function initializeExport() {
    const btn = document.getElementById('btnExportEmployees');
    if (btn) {
        btn.addEventListener('click', async () => {
            try {
                await API.exports.download(API.exports.getEmployeesUrl(), 'employees.csv');
                Utils.showToast('Employees exported successfully', 'success');
            } catch (error) {
                Utils.showToast('Error exporting employees', 'error');
            }
        });
    }
}
