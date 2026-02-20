// Reports, Charts, and Notifications Page Script
let allConflicts = [];
let filteredConflicts = [];
let reportData = null;

document.addEventListener('DOMContentLoaded', async () => {
    await Auth.init();
    initializeTabs();
    initializeFilters();
    initializeNotifications();
    initializeExport();
    await loadConflicts();
});

// Tab Management
function initializeTabs() {
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
            btn.classList.add('active');
            const tab = document.getElementById('tab-' + btn.dataset.tab);
            if (tab) tab.classList.add('active');

            if (btn.dataset.tab === 'charts' && !reportData) {
                loadReportData();
            }
            if (btn.dataset.tab === 'notifications') {
                loadNotificationHistory();
            }
        });
    });
}

// Conflicts
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
        document.getElementById('conflictsContainer').innerHTML = '<p>Error loading conflicts</p>';
    }
}

function updateSummary() {
    const overallocated = allConflicts.filter(c => c.conflictType === 'OverallocatedEmployee').length;
    const understaffed = allConflicts.filter(c => c.conflictType === 'UnderstaffedProject').length;
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
        const color = priority === 'High' ? '#e74c3c' : priority === 'Medium' ? '#f39c12' : '#3498db';
        html += `
            <div class="priority-bar-container">
                <div class="priority-label"><span>${priority}</span><span>${count}</span></div>
                <div class="priority-bar">
                    <div class="priority-bar-fill" style="width: ${percent}%; background-color: ${color}"></div>
                </div>
            </div>`;
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
        const icon = conflict.conflictType === 'OverallocatedEmployee' ? '&#9888;' : '&#10071;';
        const typeLabel = conflict.conflictType === 'OverallocatedEmployee' ? 'Over-allocated Employee' : 'Understaffed Project';
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
                        <div class="detail-item"><label>Department:</label><span>${escapeHtml(conflict.departmentName)}</span></div>
                        <div class="detail-item"><label>Week:</label><span>${Utils.formatDate(conflict.weekStartDate)}</span></div>
                        <div class="detail-item"><label>Variance:</label><span class="variance">${conflict.variance.toFixed(1)} hours</span></div>
                        <div class="detail-item"><label>Utilization:</label><span>${conflict.utilizationPercentage.toFixed(0)}%</span></div>
                    </div>
                    <div class="conflict-description">
                        <p>${escapeHtml(conflict.description)}</p>
                        ${conflict.affectedProjects ? `<p class="affected-projects"><strong>Projects:</strong> ${escapeHtml(conflict.affectedProjects)}</p>` : ''}
                    </div>
                </div>
                <div class="conflict-actions">
                    <button class="btn btn-sm btn-outline view-conflict-btn" data-type="${escapeHtml(conflict.conflictType)}" data-id="${parseInt(conflict.entityId, 10)}">View Details</button>
                </div>
            </div>`;
    });
    html += '</div>';
    container.innerHTML = html;

    // Bind view-conflict buttons via addEventListener (avoid inline onclick)
    container.querySelectorAll('.view-conflict-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            viewConflictDetails(btn.dataset.type, parseInt(btn.dataset.id, 10));
        });
    });
}

function initializeFilters() {
    const conflictFilter = document.getElementById('conflictFilter');
    const priorityFilter = document.getElementById('priorityFilter');
    if (conflictFilter) conflictFilter.addEventListener('change', applyFilters);
    if (priorityFilter) priorityFilter.addEventListener('change', applyFilters);
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
        Utils.showToast('View employee workload for more details', 'info');
    } else {
        window.location.href = `project-detail.html?id=${id}`;
    }
}

// Advanced Charts
async function loadReportData() {
    try {
        const response = await API.reports.getData();
        if (response.success && response.data) {
            reportData = response.data;
            renderDepartmentUtilChart();
            renderProjectStatusChart();
            renderWeeklyTrendChart();
            renderTopEmployees();
            renderSkillDemandChart();
        }
    } catch (error) {
        console.error('Error loading report data:', error);
        Utils.showToast('Error loading chart data', 'error');
    }
}

function renderDepartmentUtilChart() {
    const canvas = document.getElementById('deptUtilChart');
    if (!canvas || !reportData.departmentUtilization) return;
    const ctx = canvas.getContext('2d');
    const data = reportData.departmentUtilization;
    const padding = { top: 20, right: 20, bottom: 60, left: 60 };
    const w = canvas.width - padding.left - padding.right;
    const h = canvas.height - padding.top - padding.bottom;

    ctx.clearRect(0, 0, canvas.width, canvas.height);

    if (!data.length) { ctx.fillText('No data', canvas.width / 2, canvas.height / 2); return; }

    const barWidth = Math.min(50, w / data.length - 10);
    const maxVal = Math.max(100, ...data.map(d => d.utilizationPercentage));

    data.forEach((dept, i) => {
        const x = padding.left + (i * (w / data.length)) + (w / data.length - barWidth) / 2;
        const barH = (dept.utilizationPercentage / maxVal) * h;
        const y = padding.top + h - barH;

        const color = dept.utilizationPercentage > 100 ? '#e74c3c'
            : dept.utilizationPercentage > 80 ? '#f39c12' : '#27ae60';

        ctx.fillStyle = color;
        ctx.fillRect(x, y, barWidth, barH);

        ctx.fillStyle = '#2c3e50';
        ctx.font = '11px Arial';
        ctx.textAlign = 'center';
        ctx.fillText(`${dept.utilizationPercentage.toFixed(0)}%`, x + barWidth / 2, y - 5);

        ctx.save();
        ctx.translate(x + barWidth / 2, canvas.height - 5);
        ctx.rotate(-Math.PI / 4);
        ctx.textAlign = 'right';
        ctx.font = '10px Arial';
        ctx.fillText(dept.departmentName, 0, 0);
        ctx.restore();
    });

    // Y axis
    ctx.strokeStyle = '#ddd';
    ctx.beginPath();
    ctx.moveTo(padding.left, padding.top);
    ctx.lineTo(padding.left, padding.top + h);
    ctx.stroke();
    for (let i = 0; i <= 4; i++) {
        const val = (maxVal / 4) * i;
        const y = padding.top + h - (val / maxVal) * h;
        ctx.fillStyle = '#7f8c8d';
        ctx.font = '10px Arial';
        ctx.textAlign = 'right';
        ctx.fillText(`${val.toFixed(0)}%`, padding.left - 5, y + 3);
        ctx.strokeStyle = '#eee';
        ctx.beginPath();
        ctx.moveTo(padding.left, y);
        ctx.lineTo(canvas.width - padding.right, y);
        ctx.stroke();
    }
}

function renderProjectStatusChart() {
    const canvas = document.getElementById('projectStatusChart');
    if (!canvas || !reportData.projectStatusDistribution) return;
    const ctx = canvas.getContext('2d');
    const data = reportData.projectStatusDistribution;
    const total = data.reduce((sum, d) => sum + d.count, 0);

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    if (!data.length || total === 0) { ctx.fillText('No data', canvas.width / 2, canvas.height / 2); return; }

    const colors = { Active: '#27ae60', Planning: '#3498db', OnHold: '#f39c12', Completed: '#95a5a6', Cancelled: '#e74c3c' };
    const cx = canvas.width / 2 - 60;
    const cy = canvas.height / 2;
    const radius = Math.min(cx, cy) - 20;
    let startAngle = -Math.PI / 2;

    data.forEach(item => {
        const sliceAngle = (item.count / total) * 2 * Math.PI;
        ctx.beginPath();
        ctx.moveTo(cx, cy);
        ctx.arc(cx, cy, radius, startAngle, startAngle + sliceAngle);
        ctx.closePath();
        ctx.fillStyle = colors[item.status] || '#bdc3c7';
        ctx.fill();
        ctx.strokeStyle = '#fff';
        ctx.lineWidth = 2;
        ctx.stroke();
        startAngle += sliceAngle;
    });

    // Legend
    let ly = 30;
    data.forEach(item => {
        ctx.fillStyle = colors[item.status] || '#bdc3c7';
        ctx.fillRect(canvas.width - 140, ly, 12, 12);
        ctx.fillStyle = '#2c3e50';
        ctx.font = '12px Arial';
        ctx.textAlign = 'left';
        ctx.fillText(`${item.status} (${item.count})`, canvas.width - 122, ly + 11);
        ly += 22;
    });
}

function renderWeeklyTrendChart() {
    const canvas = document.getElementById('weeklyTrendChart');
    if (!canvas || !reportData.weeklyTrends) return;
    const ctx = canvas.getContext('2d');
    const data = reportData.weeklyTrends;
    const padding = { top: 30, right: 80, bottom: 50, left: 60 };
    const w = canvas.width - padding.left - padding.right;
    const h = canvas.height - padding.top - padding.bottom;

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    if (!data.length) return;

    const maxUtil = Math.max(100, ...data.map(d => d.utilizationPercentage));
    const maxConflicts = Math.max(5, ...data.map(d => d.conflictCount));

    // Grid
    ctx.strokeStyle = '#eee';
    for (let i = 0; i <= 4; i++) {
        const y = padding.top + (h / 4) * i;
        ctx.beginPath();
        ctx.moveTo(padding.left, y);
        ctx.lineTo(canvas.width - padding.right, y);
        ctx.stroke();
        ctx.fillStyle = '#7f8c8d';
        ctx.font = '10px Arial';
        ctx.textAlign = 'right';
        ctx.fillText(`${(maxUtil - (maxUtil / 4) * i).toFixed(0)}%`, padding.left - 5, y + 3);
    }

    // Utilization line
    ctx.strokeStyle = '#3498db';
    ctx.lineWidth = 2;
    ctx.beginPath();
    data.forEach((d, i) => {
        const x = padding.left + (i / (data.length - 1 || 1)) * w;
        const y = padding.top + h - (d.utilizationPercentage / maxUtil) * h;
        if (i === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
    });
    ctx.stroke();

    // Fill under utilization
    ctx.globalAlpha = 0.1;
    ctx.fillStyle = '#3498db';
    ctx.lineTo(padding.left + w, padding.top + h);
    ctx.lineTo(padding.left, padding.top + h);
    ctx.closePath();
    ctx.fill();
    ctx.globalAlpha = 1;

    // Conflict bars
    data.forEach((d, i) => {
        const x = padding.left + (i / (data.length - 1 || 1)) * w;
        if (d.conflictCount > 0) {
            const barH = (d.conflictCount / maxConflicts) * h * 0.3;
            ctx.fillStyle = 'rgba(231, 76, 60, 0.4)';
            ctx.fillRect(x - 8, padding.top + h - barH, 16, barH);
        }
    });

    // Points and labels
    data.forEach((d, i) => {
        const x = padding.left + (i / (data.length - 1 || 1)) * w;
        const y = padding.top + h - (d.utilizationPercentage / maxUtil) * h;
        ctx.fillStyle = '#3498db';
        ctx.beginPath();
        ctx.arc(x, y, 4, 0, Math.PI * 2);
        ctx.fill();

        ctx.fillStyle = '#7f8c8d';
        ctx.font = '9px Arial';
        ctx.textAlign = 'center';
        const weekLabel = new Date(d.weekStart).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
        ctx.fillText(weekLabel, x, padding.top + h + 15);
    });

    // Legend
    ctx.fillStyle = '#3498db';
    ctx.fillRect(canvas.width - 70, 10, 12, 3);
    ctx.fillStyle = '#2c3e50';
    ctx.font = '10px Arial';
    ctx.textAlign = 'left';
    ctx.fillText('Utilization', canvas.width - 55, 14);
    ctx.fillStyle = 'rgba(231, 76, 60, 0.4)';
    ctx.fillRect(canvas.width - 70, 24, 12, 12);
    ctx.fillStyle = '#2c3e50';
    ctx.fillText('Conflicts', canvas.width - 55, 34);
}

function renderTopEmployees() {
    const container = document.getElementById('topEmployeesContainer');
    if (!reportData.topUtilizedEmployees) return;
    const data = reportData.topUtilizedEmployees.filter(e => e.assignedHours > 0);
    if (!data.length) { container.innerHTML = '<p>No employee data</p>'; return; }

    let html = '<div class="employee-util-list">';
    data.forEach(emp => {
        const color = emp.utilizationPercentage > 100 ? '#e74c3c'
            : emp.utilizationPercentage > 80 ? '#f39c12' : '#27ae60';
        const width = Math.min(100, emp.utilizationPercentage);
        html += `
            <div class="employee-util-item">
                <div class="employee-util-info">
                    <span class="employee-util-name">${escapeHtml(emp.employeeName)}</span>
                    <span class="employee-util-dept">${escapeHtml(emp.departmentName)}</span>
                </div>
                <div class="employee-util-bar-container">
                    <div class="employee-util-bar" style="width: ${width}%; background: ${color};"></div>
                </div>
                <span class="employee-util-pct" style="color: ${color}">${emp.utilizationPercentage.toFixed(0)}%</span>
            </div>`;
    });
    html += '</div>';
    container.innerHTML = html;
}

function renderSkillDemandChart() {
    const canvas = document.getElementById('skillDemandChart');
    if (!canvas || !reportData.skillDemand) return;
    const ctx = canvas.getContext('2d');
    const data = reportData.skillDemand.slice(0, 10);
    const padding = { top: 20, right: 20, bottom: 80, left: 50 };
    const w = canvas.width - padding.left - padding.right;
    const h = canvas.height - padding.top - padding.bottom;

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    if (!data.length) { ctx.fillText('No data', canvas.width / 2, canvas.height / 2); return; }

    const maxVal = Math.max(...data.map(d => d.employeesWithSkill));
    const barWidth = Math.min(40, w / data.length - 8);

    data.forEach((skill, i) => {
        const x = padding.left + (i * (w / data.length)) + (w / data.length - barWidth) / 2;
        const barH = (skill.employeesWithSkill / maxVal) * h;
        const y = padding.top + h - barH;

        const hue = (i * 30) % 360;
        ctx.fillStyle = `hsl(${hue}, 60%, 50%)`;
        ctx.fillRect(x, y, barWidth, barH);

        ctx.fillStyle = '#2c3e50';
        ctx.font = '11px Arial';
        ctx.textAlign = 'center';
        ctx.fillText(skill.employeesWithSkill, x + barWidth / 2, y - 5);

        ctx.save();
        ctx.translate(x + barWidth / 2, canvas.height - 5);
        ctx.rotate(-Math.PI / 3);
        ctx.textAlign = 'right';
        ctx.font = '10px Arial';
        ctx.fillText(skill.skill, 0, 0);
        ctx.restore();
    });
}

// Notifications
function initializeNotifications() {
    const sendBtn = document.getElementById('btnSendNotification');
    if (sendBtn) sendBtn.addEventListener('click', showNotifModal);

    const closeBtn = document.getElementById('closeNotifModal');
    const cancelBtn = document.getElementById('cancelNotifModal');
    const confirmBtn = document.getElementById('confirmSendNotif');
    if (closeBtn) closeBtn.addEventListener('click', hideNotifModal);
    if (cancelBtn) cancelBtn.addEventListener('click', hideNotifModal);
    if (confirmBtn) confirmBtn.addEventListener('click', sendNotificationFromModal);

    const formBtn = document.getElementById('btnSendNotifForm');
    if (formBtn) formBtn.addEventListener('click', sendNotificationFromForm);
}

function showNotifModal() {
    document.getElementById('notifModal').style.display = 'flex';
}

function hideNotifModal() {
    document.getElementById('notifModal').style.display = 'none';
}

async function sendNotificationFromModal() {
    const emails = document.getElementById('modalNotifEmails').value
        .split(',').map(e => e.trim()).filter(e => e);
    const includeOver = document.getElementById('modalIncludeOveralloc').checked;
    const includeUnder = document.getElementById('modalIncludeUnderstaff').checked;
    await sendNotification(emails, includeOver, includeUnder);
    hideNotifModal();
}

async function sendNotificationFromForm() {
    const emails = document.getElementById('notifEmails').value
        .split(',').map(e => e.trim()).filter(e => e);
    const includeOver = document.getElementById('notifOverallocation').checked;
    const includeUnder = document.getElementById('notifUnderstaffing').checked;
    await sendNotification(emails, includeOver, includeUnder);
}

async function sendNotification(emails, includeOverallocations, includeUnderstaffing) {
    try {
        const response = await API.notifications.sendConflictReport({
            recipientEmails: emails,
            includeOverallocations,
            includeUnderstaffing
        });
        if (response.success) {
            Utils.showToast(response.message || 'Notification processed successfully', 'success');
            loadNotificationHistory();
        }
    } catch (error) {
        Utils.showToast('Error sending notification', 'error');
    }
}

async function loadNotificationHistory() {
    const container = document.getElementById('notificationHistory');
    if (!container) return;
    try {
        const response = await API.notifications.getHistory();
        if (response.success && response.data) {
            if (response.data.length === 0) {
                container.innerHTML = '<p>No notifications sent yet</p>';
                return;
            }
            let html = '<table class="data-table"><thead><tr><th>Date</th><th>Type</th><th>Recipients</th><th>Subject</th><th>Status</th></tr></thead><tbody>';
            response.data.forEach(n => {
                const statusClass = n.status === 'Sent' ? 'status-green' : n.status === 'Queued' ? 'status-yellow' : 'status-red';
                html += `<tr>
                    <td>${Utils.formatDate(n.createdDate)}</td>
                    <td>${escapeHtml(n.notificationType)}</td>
                    <td>${escapeHtml(n.recipientEmail)}</td>
                    <td>${escapeHtml(n.subject)}</td>
                    <td><span class="badge ${statusClass}">${n.status}</span></td>
                </tr>`;
            });
            html += '</tbody></table>';
            container.innerHTML = html;
        }
    } catch (error) {
        container.innerHTML = '<p>Error loading notification history</p>';
    }
}

// Export
function initializeExport() {
    const btn = document.getElementById('btnExportConflicts');
    if (btn) {
        btn.addEventListener('click', async () => {
            try {
                await API.exports.download(API.exports.getConflictsUrl(), 'conflicts.csv');
                Utils.showToast('Conflicts exported successfully', 'success');
            } catch (error) {
                Utils.showToast('Error exporting conflicts', 'error');
            }
        });
    }
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
