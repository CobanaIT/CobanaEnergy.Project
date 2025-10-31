const DuplicateAccountChecker = (function() {
    'use strict';
    
    // Configuration storage
    const configs = {};
    
    /**
     * Initialize a duplicate account checker for a specific form
     */
    function init(config) {
        const {
            accountInputSelector,
            modalId,
            loaderSelector,
            controllerEndpoint,
            fields,
            showErrorToast = false
        } = config;
        
        // Validate required parameters
        if (!accountInputSelector || !modalId || !controllerEndpoint || !fields) {
            console.error('DuplicateAccountChecker: Missing required configuration parameters');
            return;
        }
        
        // Store config for this instance
        configs[modalId] = config;
        
        // Set up table headers
        setupTableHeaders(modalId, fields);
        
        // Bind the input event - use event delegation to avoid conflicts
        $(document).on('input', accountInputSelector, function() {
            const account = $(this).val().trim();
            if (/^\d{8}$/.test(account)) {
                checkDuplicateAccount(account, modalId, loaderSelector, showErrorToast);
            }
        });
    }
    
    /**
     * Set up table headers based on field configuration
     */
    function setupTableHeaders(modalId, fields) {
        const headerRow = $(`#${modalId} #duplicateAccountTableHeaders`);
        if (headerRow.length) {
            headerRow.empty();
            fields.forEach(field => {
                headerRow.append(`<th>${field.displayName}</th>`);
            });
        }
    }
    
    /**
     * Check for duplicate accounts
     */
    function checkDuplicateAccount(account, modalId, loaderSelector, showErrorToast) {
        const config = configs[modalId];
        if (!config) {
            console.error(`DuplicateAccountChecker: No configuration found for modal ${modalId}`);
            return;
        }
        
        if (loaderSelector) {
            $(loaderSelector).show();
        }
        
        $.get(`/CheckDuplicateAccount/${config.controllerEndpoint}?account=${account}`)
            .done(function(res) {
                if (loaderSelector) {
                    $(loaderSelector).hide();
                }
                
                if (res.success && res.Data?.length > 0) {
                    displayDuplicateResults(res.Data, modalId, config.fields);
                    $(`#${modalId}`).modal('show');
                }
            })
            .fail(function() {
                if (loaderSelector) {
                    $(loaderSelector).hide();
                }
                
                if (showErrorToast && typeof showToastError === 'function') {
                    showToastError("Error checking account number.");
                } else {
                    console.error('Error checking account number');
                }
            });
    }
    
    /**
     * Display duplicate results in the modal
     */
    function displayDuplicateResults(data, modalId, fields) {
        const tbody = $(`#${modalId} #duplicateAccountResults`);
        if (tbody.length) {
            tbody.empty();
            
            data.forEach(item => {
                const row = $('<tr>');
                fields.forEach(field => {
                    const value = item[field.dataProperty] || '-';
                    row.append(`<td>${value}</td>`);
                });
                tbody.append(row);
            });
            
            // Update count
            const countElement = $(`#${modalId} #accountDuplicateCount`);
            if (countElement.length) {
                countElement.text(`Total: ${data.length} duplicate(s) found`);
            }
        }
    }
    
    return {
        init: init
    };
})();
