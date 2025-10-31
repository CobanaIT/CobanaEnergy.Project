$('#suppliersTable').DataTable({
    ajax: {
        url: '/Supplier/GetActiveSuppliers',
        type: 'GET',
        dataSrc: function (json) {
            if (!json.success) {
                showToastError(json.message);
                return [];
            }
            return json.Data;
        }
    },
    columns: [
        { data: 'SupplierName', visible: false }, 
        { data: 'ProductName' },
        { data: 'Commission' },
    ],
    rowGroup: {
        dataSrc: 'SupplierName',
        startRender: function (rows, group) {
            const supplierId = rows.data()[0].SupplierId;
            const supplierLink = rows.data()[0].Link;

            return `
        <div class="d-flex justify-content-between align-items-center w-100 px-2">
            <span class="fw-bold group-title">
            Supplier Name: ${group}
            ${supplierLink ? `<a href="${supplierLink}" target="_blank" class="supplier-visit-link ms-2">(Visit Supplier)</a>` : ''}
            </span>
            <a href="/Supplier/EditSupplier/${supplierId}" class="btn-edit mt-2 mt-md-0" title="Edit Supplier">
                <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" fill="currentColor" class="bi bi-pencil" viewBox="0 0 16 16">
                    <path d="M12.146.146a.5.5 0 0 1 .708 0l3 3a.5.5 0 0 1 0 .708l-10 10a.5.5 0 0 1-.168.11l-5 2a.5.5 0 0 1-.65-.65l2-5a.5.5 0 0 1 .11-.168l10-10z"/>
                </svg>
                Edit
            </a>
        </div>
    `;
        }
    },
    responsive: true,
    paging: true,
    ordering: true,
    dom: "<'row mb-2'" +
        "<'col-md-6 d-flex align-items-center dataTable-left'l>" +
        "<'col-md-6 d-flex flex-column align-items-end dataTable-right'" +
        "<'mb-2 dt-buttons-container'B>" +
        "f" +
        ">" +
        ">" +
        "<'row mb-3'><'row'<'col-12 table-responsive'tr>>" +
        "<'row mt-2'<'col-md-6'i><'col-md-6 text-end'p>>",
    buttons: [
        { extend: 'copy', className: 'btn btn-sm btn-secondary me-1' },
        { extend: 'csv', className: 'btn btn-sm btn-secondary me-1' },
        { extend: 'excel', className: 'btn btn-sm btn-secondary me-1' },
        { extend: 'pdf', className: 'btn btn-sm btn-secondary me-1' },
        { extend: 'print', className: 'btn btn-sm btn-secondary' }
    ]
});