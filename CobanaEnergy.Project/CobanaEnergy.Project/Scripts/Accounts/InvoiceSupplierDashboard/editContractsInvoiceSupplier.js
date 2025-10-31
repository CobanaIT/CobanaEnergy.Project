$(function () {
    // Setup DataTable, disable sorting on the Edit column (first col, index 0)
    $('#editContractsTable').DataTable({
        responsive: true,
        paging: true,
        ordering: true,
        searching: true,
        order: [],
        autoWidth: false,
        columnDefs: [
            { orderable: false, targets: 0 } // No sort on 'Edit'
        ]
    });

    // Enable column resizing
    enableColumnResizing('#editContractsTable');
});
