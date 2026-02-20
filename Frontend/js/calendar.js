// Resource Calendar View Script
let calendarStartDate = null;
let calendarEvents = [];
let calendarWeekCount = 12;

document.addEventListener('DOMContentLoaded', async () => {
    await Auth.init();
    calendarStartDate = Utils.getWeekStartDate(new Date());
    initializeCalendarControls();
    await loadFilters();
    await loadCalendar();
});

function initializeCalendarControls() {
    document.getElementById('btnPrevWeeks').addEventListener('click', () => {
        calendarStartDate = new Date(calendarStartDate);
        calendarStartDate.setDate(calendarStartDate.getDate() - calendarWeekCount * 7);
        loadCalendar();
    });

    document.getElementById('btnNextWeeks').addEventListener('click', () => {
        calendarStartDate = new Date(calendarStartDate);
        calendarStartDate.setDate(calendarStartDate.getDate() + calendarWeekCount * 7);
        loadCalendar();
    });

    document.getElementById('btnToday').addEventListener('click', () => {
        calendarStartDate = Utils.getWeekStartDate(new Date());
        loadCalendar();
    });

    document.getElementById('calDepartmentFilter').addEventListener('change', loadCalendar);
    document.getElementById('calEmployeeFilter').addEventListener('change', loadCalendar);
    document.getElementById('calWeekCount').addEventListener('change', (e) => {
        calendarWeekCount = parseInt(e.target.value);
        loadCalendar();
    });

    document.getElementById('btnExportCalendar').addEventListener('click', async () => {
        try {
            const start = formatDate(calendarStartDate);
            const end = formatDate(new Date(calendarStartDate.getTime() + calendarWeekCount * 7 * 86400000));
            await API.exports.download(API.exports.getAssignmentsUrl(null, start, end), 'calendar_export.csv');
            Utils.showToast('Calendar exported successfully', 'success');
        } catch (error) {
            Utils.showToast('Error exporting calendar', 'error');
        }
    });
}

async function loadFilters() {
    try {
        const [deptResponse, empResponse] = await Promise.all([
            API.request('/dashboard/stats'),
            API.employees.getAll()
        ]);

        // Load departments from employees data
        if (empResponse.success && empResponse.data) {
            const deptSelect = document.getElementById('calDepartmentFilter');
            const empSelect = document.getElementById('calEmployeeFilter');
            const departments = new Map();
            empResponse.data.forEach(emp => {
                departments.set(emp.departmentId, emp.departmentName);
            });
            departments.forEach((name, id) => {
                const opt = document.createElement('option');
                opt.value = id;
                opt.textContent = name;
                deptSelect.appendChild(opt);
            });
            empResponse.data.forEach(emp => {
                const opt = document.createElement('option');
                opt.value = emp.employeeId;
                opt.textContent = `${emp.firstName} ${emp.lastName}`;
                empSelect.appendChild(opt);
            });
        }
    } catch (error) {
        console.error('Error loading filters:', error);
    }
}

async function loadCalendar() {
    const container = document.getElementById('calendarContainer');
    container.innerHTML = '<div class="loading-spinner">Loading calendar...</div>';

    const endDate = new Date(calendarStartDate);
    endDate.setDate(endDate.getDate() + calendarWeekCount * 7);

    const departmentId = document.getElementById('calDepartmentFilter').value || null;
    const employeeId = document.getElementById('calEmployeeFilter').value || null;

    try {
        const response = await API.resources.getCalendarEvents(
            formatDate(calendarStartDate),
            formatDate(endDate),
            departmentId,
            employeeId
        );

        if (response.success && response.data) {
            calendarEvents = response.data;
            renderCalendar();
        }
    } catch (error) {
        console.error('Error loading calendar:', error);
        container.innerHTML = '<p>Error loading calendar data</p>';
    }
}

function renderCalendar() {
    const container = document.getElementById('calendarContainer');

    // Group events by employee
    const employeeMap = new Map();
    calendarEvents.forEach(event => {
        if (!employeeMap.has(event.employeeId)) {
            employeeMap.set(event.employeeId, {
                name: event.employeeName,
                department: event.departmentName,
                capacity: event.capacity,
                events: []
            });
        }
        employeeMap.get(event.employeeId).events.push(event);
    });

    // Generate week headers
    const weeks = [];
    for (let i = 0; i < calendarWeekCount; i++) {
        const weekStart = new Date(calendarStartDate);
        weekStart.setDate(weekStart.getDate() + i * 7);
        weeks.push(weekStart);
    }

    if (employeeMap.size === 0) {
        container.innerHTML = '<div class="empty-state"><p>No resource assignments found for this period</p></div>';
        return;
    }

    let html = '<div class="calendar-scroll"><table class="calendar-table">';
    html += '<thead><tr><th class="cal-employee-header">Employee</th><th class="cal-dept-header">Dept</th>';
    weeks.forEach(w => {
        const label = w.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
        html += `<th class="cal-week-header">${label}</th>`;
    });
    html += '</tr></thead><tbody>';

    employeeMap.forEach((empData, empId) => {
        html += `<tr>`;
        html += `<td class="cal-employee-cell">${escapeHtml(empData.name)}</td>`;
        html += `<td class="cal-dept-cell">${escapeHtml(empData.department)}</td>`;

        weeks.forEach(weekStart => {
            const weekStr = formatDate(weekStart);
            const weekEvents = empData.events.filter(e =>
                formatDate(new Date(e.weekStartDate)) === weekStr
            );

            const totalHours = weekEvents.reduce((sum, e) => sum + e.assignedHours, 0);
            const utilization = empData.capacity > 0 ? (totalHours / empData.capacity * 100) : 0;

            let cellClass = 'cal-cell';
            let bgColor = '';
            if (totalHours > 0) {
                if (utilization > 100) { bgColor = '#8e44ad'; cellClass += ' cal-overallocated'; }
                else if (utilization > 85) { bgColor = '#e74c3c'; cellClass += ' cal-heavy'; }
                else if (utilization > 60) { bgColor = '#f39c12'; cellClass += ' cal-medium'; }
                else { bgColor = '#27ae60'; cellClass += ' cal-light'; }
            }

            const projectNames = weekEvents.map(e => escapeHtml(e.projectName)).join(', ');
            const title = totalHours > 0
                ? `${totalHours.toFixed(1)}h / ${empData.capacity}h (${utilization.toFixed(0)}%)\n${projectNames}`
                : 'No assignments';

            html += `<td class="${cellClass}" title="${title}"`;
            if (bgColor) html += ` style="background-color: ${bgColor}; color: white;"`;
            html += `>`;
            if (totalHours > 0) {
                html += `<div class="cal-hours">${totalHours.toFixed(0)}h</div>`;
                html += `<div class="cal-util">${utilization.toFixed(0)}%</div>`;
            }
            html += '</td>';
        });
        html += '</tr>';
    });

    html += '</tbody></table></div>';
    container.innerHTML = html;
}

function formatDate(date) {
    const d = new Date(date);
    return d.toISOString().split('T')[0];
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
