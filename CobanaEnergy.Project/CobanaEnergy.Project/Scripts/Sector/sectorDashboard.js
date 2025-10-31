$(document).ready(function () {
    // Get the actual number of columns in the table
    var columnCount = $('#sectorTable thead th').length;
    var hasActionColumn = $('#sectorTable thead th:first').text().trim() === 'Edit';
    
    // Calculate column indices dynamically
    var sectorTypeColumnIndex = columnCount - 1; // Last column is always Sector Type
    var supplierColumnIndex = hasActionColumn ? 5 : 4; // Supplier column position depends on Action column
    
    // Initialize DataTable with ReconciliationsDashboard-style configuration
    var sectorTable = $('#sectorTable').DataTable({
        responsive: true,
        paging: true,
        ordering: true,
        searching: true,
        order: [[hasActionColumn ? 1 : 0, 'asc']], // Sort by Name column (adjust index based on Action column)
        columnDefs: [
            { targets: sectorTypeColumnIndex, visible: false } // Hide Sector Type column
        ],
        autoWidth: false,
        dom: 'lfrtip'
    });

    // Enable column resizing
    enableColumnResizing('#sectorTable');

    // Handle sector type filter change
    $('#sectorTypeFilter').on('change', function () {
        var selectedType = $(this).val();
        filterSectorsByType(selectedType);
        toggleSupplierColumn(selectedType);
    });

    // Handle edit button clicks using event delegation (more reliable than inline onclick)
    $(document).on('click', '.edit-btn', function(e) {
        e.preventDefault();
        var sectorId = $(this).data('sector-id');
        editSector(sectorId);
    });

    // Toggle Supplier column visibility based on sector type
    function toggleSupplierColumn(sectorType) {
        if (sectorType === "Brokerage") {
            // Show Supplier column
            sectorTable.column(supplierColumnIndex).visible(true);
        } else {
            // Hide Supplier column
            sectorTable.column(supplierColumnIndex).visible(false);
        }
    }

    // Filter sectors by sector type
    function filterSectorsByType(sectorType) {
        if (sectorType === "") {
            // Show all sectors
            sectorTable.search('').draw();
        } else {
            // Filter by sector type using custom filter function
            $.fn.dataTable.ext.search.push(function(settings, data, dataIndex) {
                var sectorTypeInRow = data[sectorTypeColumnIndex]; // Sector Type column index
                if (sectorType === "" || sectorTypeInRow === sectorType) {
                    return true;
                }
                return false;
            });
            sectorTable.draw();
            // Remove the custom filter after applying it
            $.fn.dataTable.ext.search.pop();
        }
    }

    // Show toast notification (for future use)
    function showToast(message, type) {
        if (typeof showToastSuccess === 'function' && typeof showToastError === 'function') {
            if (type === 'success') {
                showToastSuccess(message);
            } else if (type === 'error') {
                showToastError(message);
            } else if (type === 'warning') {
                showToastWarning(message);
            }
        } else {
            // Fallback to console if toast functions not available
            console.log(type === 'success' ? '✓ ' : type === 'error' ? '✗ ' : '⚠ ', message);
        }
    }

    // Handle edit sector button click
    window.editSector = function(sectorId) {
        // Check if sectorId is valid
        if (!sectorId || sectorId === 'null' || sectorId === 'undefined' || sectorId === '') {
            console.error('Invalid sector ID:', sectorId);
            alert('Invalid sector ID. Please try again.');
            return;
        }
        
        // Trim any whitespace
        sectorId = sectorId.toString().trim();
        
        // Redirect to edit page
        var editUrl = '/Sector/Edit/' + encodeURIComponent(sectorId);
        //window.location.href = editUrl;
        window.open(editUrl, '_blank');
    };

    // Initialize with "All Sectors" selected
    $('#sectorTypeFilter').val('');
    
    // Apply initial filter and hide Supplier column by default
    filterSectorsByType("");
    toggleSupplierColumn(""); // Hide Supplier column initially
});
