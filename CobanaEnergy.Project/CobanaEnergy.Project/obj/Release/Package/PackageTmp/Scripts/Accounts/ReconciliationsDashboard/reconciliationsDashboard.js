$(document).ready(function () {
    // Register custom sorting type for dd-MM-yy date format
    $.fn.dataTable.ext.type.order['date-dd-mm-yy-pre'] = function (data) {
        if (!data || data === 'N/A' || data === '' || data === '-') {
            return 0;
        }
        var parts = data.split('-');
        if (parts.length === 3) {
            var day = parts[0];
            var month = parts[1];
            var year = parts[2];
            var fullYear = '20' + year;
            return parseInt(fullYear + month + day);
        }
        return 0;
    };

    var table = $('#reconciliationsTable').DataTable({
        responsive: true,
        paging: true,
        ordering: true,
        searching: true,
        order: [],
        autoWidth: false,
        dom: 'lfrtip',
        columnDefs: [
            {
                targets: [4, 5, 6], // StartDate (4), CED (5), CEDCOT (6) columns
                type: 'date-dd-mm-yy'
            }
        ]
    });

    // Enable column resizing
    enableColumnResizing('#reconciliationsTable');

    var excelButton = new $.fn.dataTable.Buttons(table, {
        buttons: [{
            extend: 'excelHtml5',
            text: 'Export',
            title: '',
            filename: function () {
                let filename = 'Reconciliations';
                
                const supplierName = $('#supplierFilter option:selected').text();
                if (supplierName && supplierName !== 'All Suppliers') {
                    filename += `_${supplierName.replace(/[^a-zA-Z0-9]/g, '_')}`;
                }
                
                const today = new Date();
                filename += `_${today.toISOString().split('T')[0]}`;
                
                return filename;
            },
            exportOptions: {
                columns: ':visible'
            }
        }]
    });
    table.buttons(excelButton, false);

    $('#exportExcelBtn').on('click', function () {
        table.button('.buttons-excel').trigger();
    });

    $('#endDateFilter').prop('disabled', true);

    $('#startDateFilter').on('change', function () {
        handleDateValidation();
        loadContracts();
    });

    $('#endDateFilter').on('change', function () {
        if ($(this).val()) loadContracts();
    });

    $('#supplierFilter').on('change', function () {
        loadContracts();
    });

    function handleDateValidation() {
        const startDate = $('#startDateFilter').val();
        const endDate = $('#endDateFilter').val();

        if (startDate) {
            $('#endDateFilter').prop('disabled', false).attr('min', startDate);
            if (endDate && new Date(endDate) < new Date(startDate)) {
                $('#endDateFilter').val('');
                showToastWarning("End Date reset because it was earlier than Start Date.");
            }
        } else {
            $('#endDateFilter').prop('disabled', true).val('');
        }
    }

    function loadContracts() {
        const supplierId = $('#supplierFilter').val();
        const startDate = $('#startDateFilter').val();
        const endDate = $('#endDateFilter').val();

        $.ajax({
            url: '/ReconciliationsDashboard/GetReconciliationContracts',
            type: 'POST',
            data: {
                supplierId: supplierId,
                startDate: startDate,
                endDate: endDate,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (res) {
                table.clear();
                if (res.success) {
                    if (res.Data == null || res.Data.Contracts.length === 0) {
                        showToastError("No contracts found.");
                        table.clear().draw();
                        $('#awaitingInvoiceCount').text("0");
                        return;
                    }
                    res.Data.Contracts.forEach(contract => {
                        table.row.add([
                            contract.BusinessName,
                            contract.MPAN ?? '',
                            contract.MPRN ?? '',
                            contract.InputEAC,
                            contract.StartDate,
                            contract.CED,
                            contract.CEDCOT,
                            contract.PaymentStatus,
                            contract.CobanaFinalReconciliation
                        ]);
                    });
                } else {
                    showToastError(res.message || "No contracts found.");
                    table.clear().draw();
                }
                table.draw();
            },
            error: function () {
                table.clear().draw();
                showToastError("Error loading contracts.");
            }
        });
    }

    loadContracts();
});
