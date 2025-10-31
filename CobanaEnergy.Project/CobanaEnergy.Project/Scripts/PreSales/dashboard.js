$(document).ready(function () {

    loadContractTable();
    loadSupplierStats();
    setInterval(loadSupplierStats, 120000);
});

function loadContractTable() {
    const $table = $('#contractTable tbody');

    $table.html('<tr><td colspan="9" class="text-center text-secondary">Loading...</td></tr>');

    $.get('/PreSales/GetAllContracts', function (res) {
        if (!res.success || !res.Data) {
            $table.html('<tr><td colspan="9" class="text-danger fw-bold text-center">No contracts found.</td></tr>');
            showToastWarning("No contract records found.");
            return;
        }

        $table.empty();
        res.Data.forEach(r => {
            let controller;
            if (r.Type == 'Electric') {
                controller = 'Electric/ElectricEdit';
            }
            if (r.Type == 'Gas') {
                controller = 'Gas/EditGas';
            }
            if (r.Type == 'Dual') {
                controller = 'Dual/EditDual';
            }

            let iconClass, buttonClass;
            switch(r.Type) {
                case 'Electric':
                    iconClass = 'fas fa-bolt';
                    buttonClass = 'btn btn-sm contract-edit-btn btn-primary';
                    break;
                case 'Gas':
                    iconClass = 'fas fa-fire';
                    buttonClass = 'btn btn-sm contract-edit-btn btn-danger';
                    break;
                case 'Dual':
                    iconClass = 'fas fa-plug';
                    buttonClass = 'btn btn-sm contract-edit-btn btn-secondary';
                    break;
                default:
                    iconClass = 'fas fa-pencil-alt';
                    buttonClass = 'btn btn-sm contract-edit-btn';
            }
            
            const row = `
                            <tr>
                                <td><button class="${buttonClass}" 
                                           data-eid="${r.EId}" 
                                           data-type="${r.Type}" 
                                           data-controller="${controller}" 
                                           title="Edit">
                                    <i class="${iconClass}"></i>
                                    </button></td>
                                <td>${r.Agent}</td>
                                <td>${r.MPAN || '-'}</td>
                                <td>${r.MPRN || '-'}</td>
                                <td>${r.BusinessName}</td>
                                <td>${r.CustomerName}</td>
                                <td>${r.InputDate ?? '-'}</td>
                                <td style="display:none">${r.SortableDate ?? '1900-01-01'}</td>
                                <td>${r.PreSalesStatus ?? '-'}</td>
                                <td>
                                      <span class="truncate-cell">
                                        ${(r.Notes && r.Notes.length > 50) ? r.Notes.substring(0, 50) + '…' : (r.Notes || '-')}
                                      </span>
                               </td>
                            </tr>`;
            $table.append(row);
        });

        const table = $('#contractTable').DataTable({
            paging: true,
            searching: true,
            ordering: true,
            order: [[7, 'desc']],
            columnDefs: [
                { targets: 6, orderData: [7] }, // Sort InputDate column using SortableDate data
                { targets: 7, visible: false, orderable: true } 
            ],
            info: true,
            responsive: true,
            autoWidth: false
        });

        // Enable column resizing
        enableColumnResizing('#contractTable');

        // Attach click handlers to edit buttons
        $(document).off('click', '.contract-edit-btn').on('click', '.contract-edit-btn', function(e) {
            e.preventDefault();
            const eid = $(this).data('eid');
            const contractType = $(this).data('type');
            const controller = $(this).data('controller');
            
            LockContract(eid, contractType, controller);
        });

    }).fail(function () {
        $table.html('<tr><td colspan="9" class="text-danger fw-bold text-center">Error loading contracts.</td></tr>');
        showToastError("Error loading dashboard data.");
    });
}
function loadSupplierStats() {
    const $panel = $('#supplier-stats-content');
    const now = new Date();
    const hour = now.getHours();

    if (hour < 8 || hour >= 19) {
        $panel.html('<div class="text-muted text-center w-100">Supplier stats are only visible from 08:00 to 19:00.</div>');
        return;
    }

    $panel.html('<div class="text-center text-secondary w-100">Loading supplier stats...</div>');

    $.get('/PreSales/GetSupplierContractStats', function (res) {
        if (!res.success || !res.Data || res.Data.length === 0) {
            $panel.html('<div class="text-danger text-center w-100">No stats found.</div>');
            return;
        }

        const grouped = {};
        res.Data.forEach(item => {
            if (!grouped[item.SupplierName]) grouped[item.SupplierName] = [];
            grouped[item.SupplierName].push(item);
        });

        let html = '';

        Object.keys(grouped).forEach(supplier => {
            html += `<div class="supplier-block mb-4">
                <h6 class="supplier-name">${supplier}</h6>`;
            grouped[supplier].forEach(stat => {
                html += `<div class="status-line">
                    <strong class="supplier-status"><span class="badge bg-warning"> Status: ${stat.PreSalesStatus} </span></strong>
                    <div class="badge-group">
                        <span class="badge bg-success">Gas: ${stat.GasCount}</span>
                        <span class="badge bg-primary">Electric: ${stat.ElectricCount}</span>
                    </div>
                 </div>`;
            });
            html += `</div>`;
        });

        $panel.html(html);
    }).fail(function () {
        $panel.html('<div class="text-danger text-center w-100">Failed to load stats.</div>');
        showToastError("Error loading supplier stats.");
    });
}

// Track active heartbeats (simplified)
let activeHeartbeats = new Map();

/**
 * Generic function to lock a contract and navigate to edit page
 * @param {string} eid - Contract ID
 * @param {string} contractType - Type of contract (Electric, Gas, Dual)  
 * @param {string} controller - Controller path for edit page
 */
function LockContract(eid, contractType, controller) {
    if (!eid || !contractType || !controller) {
        showToastError("Invalid contract information.");
        return;
    }

    // Show loading state
    const $btn = $(`.contract-edit-btn[data-eid="${eid}"]`);
    const originalText = $btn.html();
    $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Locking...');

    $.ajax({
        url: '/PreSales/LockContract',
        type: 'POST',
        data: { eid: eid },
        success: function (response) {
            if (response.success) {
                // Lock acquired successfully, open edit page
                const editUrl = `/${controller}?eid=${eid}`;
                const childWindow = window.open(editUrl, '_blank');
                
                // Start simple heartbeat monitoring
                startHeartbeatMonitoring(eid, childWindow);
                
            } else {
                // Lock failed, show error message
                showToastError(response.message || 'Failed to lock contract');
            }
        },
        error: function () {
            showToastError("Unexpected error occurred while locking contract.");
        },
        complete: function () {
            // Restore button state
            $btn.prop('disabled', false).html(originalText);
        }
    });
}

/**
 * Starts simple heartbeat monitoring for a contract
 * @param {string} eid - Contract ID
 * @param {Window} childWindow - Reference to the child window
 */
function startHeartbeatMonitoring(eid, childWindow) {
    console.log(`Starting heartbeat for contract ${eid} (every 30 seconds)`);
    
    // 1. Start heartbeat - every 30 seconds
    const heartbeatTimer = setInterval(function() {
        sendHeartbeat(eid);
    }, 30 * 1000); // 30 seconds
    
    // 2. Simple window check - every 10 seconds (less aggressive)
    const windowTimer = setInterval(function() {
        if (childWindow.closed) {
            console.log(`Window closed for contract ${eid}`);
            stopHeartbeatMonitoring(eid);
        }
    }, 10000); // Every 10 seconds
    
    // Store timers
    activeHeartbeats.set(eid, { 
        heartbeatTimer: heartbeatTimer,
        windowTimer: windowTimer,
        window: childWindow
    });
}

/**
 * Sends heartbeat to server to keep lock active
 * @param {string} eid - Contract ID
 */
function sendHeartbeat(eid) {
    $.ajax({
        url: '/PreSales/HeartbeatLock',
        type: 'POST',
        data: { eid: eid },
        success: function(response) {
            if (!response.success) {
                console.warn(`Heartbeat failed for ${eid}: ${response.message}`);
                // Stop heartbeat if lock is lost
                stopHeartbeatMonitoring(eid);
            }
        },
        error: function() {
            console.warn(`Heartbeat request failed for ${eid}`);
            // Stop heartbeat on error
            stopHeartbeatMonitoring(eid);
        }
    });
}

/**
 * Stops heartbeat monitoring and unlocks contract
 * @param {string} eid - Contract ID
 */
function stopHeartbeatMonitoring(eid) {
    const timers = activeHeartbeats.get(eid);
    if (!timers) return;
    
    console.log(`Stopping heartbeat for contract ${eid}`);
    
    // Clear timers
    clearInterval(timers.heartbeatTimer);
    clearInterval(timers.windowTimer);
    
    // Unlock contract
    unlockContract(eid);
    
    // Remove from tracking
    activeHeartbeats.delete(eid);
}

/**
 * Unlocks a contract on the server
 * @param {string} eid - Contract ID
 */
function unlockContract(eid) {
    $.ajax({
        url: '/PreSales/UnlockContract',
        type: 'POST',
        data: { eid: eid },
        success: function(response) {
            console.log(`Contract ${eid} unlocked`);
        },
        error: function() {
            console.warn(`Failed to unlock contract ${eid}`);
        }
    });
}

// Cleanup when dashboard closes
$(window).on('beforeunload', function() {
    activeHeartbeats.forEach(function(timers, eid) {
        stopHeartbeatMonitoring(eid);
    });
});