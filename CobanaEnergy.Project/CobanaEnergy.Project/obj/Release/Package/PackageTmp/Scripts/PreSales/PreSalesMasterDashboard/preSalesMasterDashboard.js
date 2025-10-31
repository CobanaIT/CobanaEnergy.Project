$(document).ready(function () {
    let masterTable;
    let statusCountRefreshTimeout;
    
    // Heartbeat management for popup
    let currentLockedContract = null;
    let heartbeatInterval = null;

    // Initialize the dashboard
    initializeDashboard();

    function initializeDashboard() {
        initializeDataTable();
        bindEvents();
    }

    function initializeDataTable() {
        masterTable = $('#preSalesMasterTable').DataTable({
            processing: true,
            serverSide: true,
            ajax: {
                url: '/PreSalesMasterDashboard/GetPreSalesMasterContracts',
                type: 'POST',
                data: function (d) {
                    d.__RequestVerificationToken = $('input[name="__RequestVerificationToken"]').val();
                    d.inputDateFrom = $('#inputDateFrom').val();
                    d.inputDateTo = $('#inputDateTo').val();
                    d.supplierId = $('#supplierFilter').val();
                },
                error: function (xhr, error, thrown) {
                    console.error('DataTable AJAX error:', error);
                    showAlert('Error loading data: ' + thrown, 'danger');
                }
            },
            columns: [
                {
                    data: null,
                    orderable: false,
                    searchable: false,
                    render: function (data, type, row) {
                        let iconClass, buttonClass;
                        switch(row.Type) {
                            case 'Electric':
                                iconClass = 'fas fa-bolt';
                                buttonClass = 'btn btn-sm btn-primary';
                                break;
                            case 'Gas':
                                iconClass = 'fas fa-fire';
                                buttonClass = 'btn btn-sm btn-danger';
                                break;
                            case 'Dual':
                                iconClass = 'fas fa-plug';
                                buttonClass = 'btn btn-sm btn-secondary';
                                break;
                            default:
                                iconClass = 'fas fa-pencil-alt';
                                buttonClass = 'btn btn-sm';
                        }
        return `<button class="${buttonClass}" onclick="editContract('${row.EId}', '${row.Type}')">
                    <i class="${iconClass}"></i>
                </button>`;
                    }
                },
                {
                    data: 'Agent',
                    render: function (data, type, row) {
                        return data || '-';
                    }
                },
                {
                    data: 'BusinessName',
                    render: function (data, type, row) {
                        return data || '-';
                    }
                },
                {
                    data: 'CustomerName',
                    render: function (data, type, row) {
                        return data || '-';
                    }
                },
                {
                    data: 'PostCode',
                    render: function (data, type, row) {
                        return data || '-';
                    }
                },
                {
                    data: 'InputDate',
                    render: function (data, type, row) {
                        return data || '-';
                    }
                },
                {
                    data: 'Duration',
                    render: function (data, type, row) {
                        return data || '-';
                    }
                },
                {
                    data: 'Uplift',
                    render: function (data, type, row) {
                        return data || '-';
                    }
                },
                {
                    data: 'InputEAC',
                    render: function (data, type, row) {
                        return data || '-';
                    }
                },
                {
                    data: 'Email',
                    render: function (data, type, row) {
                        return data || '-';
                    }
                },
                {
                    data: 'Supplier',
                    render: function (data, type, row) {
                        return data || '-';
                    }
                },
                {
                    data: 'ContractNotes',
                    render: function (data, type, row) {
                        return `<span class="truncate-cell">
                                    ${(data && data.length > 50) ? data.substring(0, 50) + '…' : (data || '-')}
                                </span>`;
                    }
                },
                {
                    data: 'SortableDate',
                    visible: false
                }
            ],
            order: [[12, 'desc']], // Sort by SortableDate descending
            pageLength: 10,
            lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
            language: {
                processing: '<div class="loading-spinner"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>',
                emptyTable: 'No contracts found',
                zeroRecords: 'No contracts match your search criteria',
                info: 'Showing _START_ to _END_ of _TOTAL_ contracts',
                infoEmpty: 'Showing 0 to 0 of 0 contracts',
                infoFiltered: '(filtered from _MAX_ total contracts)',
                lengthMenu: 'Show _MENU_ contracts per page',
                search: 'Search:',
                paginate: {
                    first: 'First',
                    last: 'Last',
                    next: 'Next',
                    previous: 'Previous'
                }
            },
            responsive: true,
            autoWidth: false
        });

        // Enable column resizing
        enableColumnResizing('#preSalesMasterTable');

        // Add event handler to refresh counts after table is drawn
        masterTable.on('draw.dt', function() {
            refreshStatusCounts();
        });
    }


    function bindEvents() {
        $('#applyFilters').on('click', function () {
            validateDateRange();
            masterTable.ajax.reload();
        });

        $('#clearFilters').on('click', function () {
            clearFilters();
            masterTable.ajax.reload();
        });

        // Allow Enter key to apply filters
        $('#inputDateFrom, #inputDateTo, #supplierFilter').on('keypress', function (e) {
            if (e.which === 13) { // Enter key
                validateDateRange();
                masterTable.ajax.reload();
            }
        });
    }

    function refreshStatusCounts() {
        // Clear any existing timeout to prevent multiple rapid calls
        if (statusCountRefreshTimeout) {
            clearTimeout(statusCountRefreshTimeout);
        }

        // Add a small delay to prevent too many rapid AJAX calls
        statusCountRefreshTimeout = setTimeout(function() {
            // Show loading indicator
            $('#submittedCount').html('<i class="fas fa-spinner fa-spin"></i>');
            $('#rejectedCount').html('<i class="fas fa-spinner fa-spin"></i>');
            $('#pendingCount').html('<i class="fas fa-spinner fa-spin"></i>');

            $.ajax({
                url: '/PreSalesMasterDashboard/GetFilteredContractStatusCounts',
                type: 'POST',
                data: {
                    inputDateFrom: $('#inputDateFrom').val(),
                    inputDateTo: $('#inputDateTo').val(),
                    supplierId: $('#supplierFilter').val(),
                    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                },
                success: function (response) {
                    if (response.success) {
                        $('#submittedCount').text(response.submittedCount);
                        $('#rejectedCount').text(response.rejectedCount);
                        $('#pendingCount').text(response.pendingCount);
                    } else {
                        console.error('Error refreshing status counts:', response.message);
                        // Reset to previous values on error
                        $('#submittedCount').text('0');
                        $('#rejectedCount').text('0');
                        $('#pendingCount').text('0');
                    }
                },
                error: function (xhr, status, error) {
                    console.error('AJAX error refreshing status counts:', error);
                    // Reset to previous values on error
                    $('#submittedCount').text('0');
                    $('#rejectedCount').text('0');
                    $('#pendingCount').text('0');
                }
            });
        }, 300); // 300ms delay
    }

    function validateDateRange() {
        const fromDate = $('#inputDateFrom').val();
        const toDate = $('#inputDateTo').val();

        if (fromDate && !toDate) {
            showAlert('Please select both "From" and "To" dates or clear the date filters.', 'warning');
            return false;
        }

        if (fromDate && toDate) {
            const from = new Date(fromDate);
            const to = new Date(toDate);

            if (from > to) {
                showAlert('"From" date cannot be later than "To" date.', 'warning');
                return false;
            }
        }

        return true;
    }

    function clearFilters() {
        $('#inputDateFrom').val('');
        $('#inputDateTo').val('');
        $('#supplierFilter').val('');
    }

    function showAlert(message, type) {
        const alertHtml = `
            <div class="alert alert-${type} alert-dismissible fade show" role="alert">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>
        `;
        
        // Remove existing alerts
        $('.alert').remove();
        
        // Add new alert at the top of the dashboard
        $('.master-dashboard').prepend(alertHtml);
        
        // Auto-dismiss after 5 seconds
        setTimeout(function () {
            $('.alert').fadeOut();
        }, 5000);
    }

    // Global function for editing contracts
    window.editContract = function (eId, contractType) {
        // First, try to lock the contract
        $.ajax({
            url: '/PreSales/LockContract',
            type: 'POST',
            data: { eid: eId },
            success: function(lockResponse) {
                if (lockResponse.success) {
                    // Lock acquired, start heartbeat and load popup content
                    currentLockedContract = eId;
                    startHeartbeat(eId);
                    loadContractNotesModal(eId, contractType);
                } else {
                    // Lock failed, show error message
                    showToastError(lockResponse.message || 'Failed to lock contract');
                }
            },
            error: function() {
                showToastError('Error occurred while locking contract.', 'danger');
            }
        });
    };

    function loadContractNotesModal(eId, contractType) {
        $.ajax({
            url: '/PreSalesMasterDashboard/GetContractNotesOnly',
            type: 'POST',
            data: {
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                eId: eId,
                contractType: contractType
            },
            success: function (response) {
                if (response.success) {
                    populateEditModal(response.Data);
                    $('#editContractNotesOnlyModal').modal('show');
                } else {
                    // Failed to load data, stop heartbeat and unlock the contract
                    stopHeartbeat();
                    unlockContract(eId);
                    showAlert('Error loading contract details: ' + response.message, 'danger');
                }
            },
            error: function () {
                // Failed to load data, stop heartbeat and unlock the contract
                stopHeartbeat();
                unlockContract(eId);
                showAlert('Error loading contract details', 'danger');
            }
        });
    }

    function populateEditModal(data) {
        $('#eid').val(data.EId);
        $('#contractType').val(data.ContractType);
        $('#contractNotes').val(data.ContractNotes);
        
        // Show/hide dual contract message
        if (data.IsDualContract) {
            $('.dual-contract-message').removeClass('d-none');
        } else {
            $('.dual-contract-message').addClass('d-none');
        }
    }

    // Handle edit form submission
    $('#editContractNotesOnlyForm').on('submit', function (e) {
        e.preventDefault();
        
        const formData = {
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
            EId: $('#eid').val(),
            ContractType: $('#contractType').val(),
            ContractNotes: $('#contractNotes').val()
        };

        $.ajax({
            url: '/PreSalesMasterDashboard/UpdateContractNotesOnly',
            type: 'POST',
            data: formData,
            success: function (response) {
                if (response.success) {
                    showAlert('Contract notes updated successfully!', 'success');
                    $('#editContractNotesOnlyModal').modal('hide');
                    masterTable.ajax.reload();
                } else {
                    showAlert('Error updating contract: ' + response.message, 'danger');
                }
            },
            error: function () {
                showAlert('Error updating contract', 'danger');
            }
        });
    });

    // Unlock contract function
    function unlockContract(eId) {
        if (!eId) return;
        
        $.ajax({
            url: '/PreSales/UnlockContract',
            type: 'POST',
            data: { eid: eId },
            success: function(response) {
                console.log('Contract unlocked:', response.message);
            },
            error: function() {
                console.log('Failed to unlock contract');
            }
        });
    }

    // Heartbeat functions for popup lock management
    function startHeartbeat(eId) {
        if (heartbeatInterval) {
            clearInterval(heartbeatInterval);
        }
        
        // Send heartbeat every 30 seconds
        heartbeatInterval = setInterval(function() {
            sendHeartbeat(eId);
        }, 30000);
        
        console.log('Heartbeat started for contract:', eId);
    }
    
    function stopHeartbeat() {
        if (heartbeatInterval) {
            clearInterval(heartbeatInterval);
            heartbeatInterval = null;
            currentLockedContract = null;
            console.log('Heartbeat stopped');
        }
    }
    
    function sendHeartbeat(eId) {
        if (!eId) return;
        
        $.ajax({
            url: '/PreSales/HeartbeatLock',
            type: 'POST',
            data: { eid: eId },
            success: function(response) {
                if (response.success) {
                    console.log('Heartbeat sent successfully for contract:', eId);
                } else {
                    console.warn('Heartbeat failed for contract:', eId, response.message);
                    // If heartbeat fails, the lock might have been lost
                    // Show warning to user but don't force close modal
                    showAlert('Warning: Contract lock may have expired. Please save your changes soon.', 'warning');
                }
            },
            error: function() {
                console.error('Heartbeat error for contract:', eId);
                // Network error - show warning but don't force close
                showAlert('Warning: Unable to maintain contract lock. Please save your changes soon.', 'warning');
            }
        });
    }

    // Reset form and unlock contract when modal is hidden
    $('#editContractNotesOnlyModal').on('hidden.bs.modal', function () {
        const eId = $('#eid').val();
        if (eId) {
            stopHeartbeat();
            unlockContract(eId);
        }
        $('#editContractNotesOnlyForm')[0].reset();
        $('.dual-contract-message').addClass('d-none');
    });
    
    // Cleanup heartbeat on page unload
    $(window).on('beforeunload pagehide', function() {
        if (currentLockedContract) {
            stopHeartbeat();
            // Try to unlock the contract (may not complete due to page unload)
            unlockContract(currentLockedContract);
        }
    });
});
