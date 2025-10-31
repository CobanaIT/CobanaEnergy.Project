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

    var table = $('#problematicsTable').DataTable({
        responsive: true,
        paging: true,
        ordering: true,
        searching: true,
        order: [],
        autoWidth: false,
        dom: 'lfrtip',
        columnDefs: [
            { orderable: false, targets: 0 },
            {
                targets: [6, 7, 8, 9], // InputDate (6), StartDate (7), CED (8), CEDCOT (9) columns
                type: 'date-dd-mm-yy'
            }
        ]
    });

    // Enable column resizing
    enableColumnResizing('#problematicsTable');

    var excelButton = new $.fn.dataTable.Buttons(table, {
        buttons: [{
            extend: 'excelHtml5',
            text: 'Export',
            title: '',
            filename: function () {
                let filename = 'Problematics';
                
                const supplierName = $('#supplierFilter option:selected').text();
                if (supplierName && supplierName !== 'All Suppliers') {
                    filename += `_${supplierName.replace(/[^a-zA-Z0-9]/g, '_')}`;
                }
                
                const today = new Date();
                filename += `_${today.toISOString().split('T')[0]}`;
                
                return filename;
            },
            exportOptions: {
                columns: ':visible:not(:first-child)'
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

        if (endDate && !startDate) {
            showToastWarning("Please select a Start Date before selecting End Date.");
            $('#endDateFilter').val('');
            return;
        }

        if (startDate && endDate && new Date(endDate) < new Date(startDate)) {
            showToastWarning("End Date cannot be earlier than Start Date.");
            $('#endDateFilter').val('');
            return;
        }

        $.ajax({
            url: '/ProblematicsDashboard/GetProblematicsContracts',
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
                        // Determine contract type and styling
                        let iconClass, buttonClass;
                        switch(contract.ContractType) {
                            case 'Electric':
                                iconClass = 'fas fa-bolt';
                                buttonClass = 'btn btn-sm btn-primary';
                                break;
                            case 'Gas':
                                iconClass = 'fas fa-fire';
                                buttonClass = 'btn btn-sm btn-danger';
                                break;
                            default:
                                iconClass = 'fas fa-pencil-alt';
                                buttonClass = 'btn btn-sm';
                        }
                        
                        table.row.add([
                            `<a href="javascript:void(0)" id="openProblemticDashboardPopup" data-eid="${contract.EId}" data-paymentstatus="${contract.PaymentStatus}" data-contracttype="${contract.ContractType}" class="${buttonClass}" title="Edit"><i class="${iconClass}"></i></a>`,
                            `<input type="checkbox" name="selectedContracts" value="${contract.EId}" />`,
                            contract.BusinessName,
                            contract.MPAN ?? '',
                            contract.MPRN ?? '',
                            contract.InputEAC,
                            contract.InputDate,
                            contract.StartDate,
                            contract.CED,
                            contract.CEDCOT,
                            contract.ContractStatus,
                            contract.PaymentStatus,
                            contract.Invoices,
                            contract.SupplierCobanaInvoiceNotes,
                            contract.CobanaFinalReconciliation
                        ]);
                    });
                } else {
                    showToastError(res.message || "No contracts found.");
                    table.clear().draw();
                    $('#problematicsCount').text("0");
                }
                $('#problematicsCount').text(res.Data.ProblematicsCount || "0");
                $('#saveBtn').prop('disabled', true);
                $('#checkAll').prop('checked', false);
                table.draw();
            },
            error: function () {
                table.clear().draw();
                $('#problematicsCount').text("0");
                $('#saveBtn').prop('disabled', true);
                $('#checkAll').prop('checked', false);
                showToastError("Error loading contracts.");
            }
        });
    }

    $('#checkAll').on('change', function () {
        const checked = this.checked;
        $('input[name="selectedContracts"]').prop('checked', checked).trigger('change');
    });

    $(document).on('change', 'input[name="selectedContracts"]', function () {
        const total = $('input[name="selectedContracts"]').length;
        const checked = $('input[name="selectedContracts"]:checked').length;
        $('#saveBtn').prop('disabled', checked === 0);
        $('#checkAll').prop('checked', total === checked);
    });

    $('#saveBtn').on('click', function () {
        const selectedContracts = $('input[name="selectedContracts"]:checked').map(function () {
            const row = $(this).closest('tr');
            return {
                EId: $(this).val(),
                MPAN: row.find('td:eq(2)').text().trim(),
                MPRN: row.find('td:eq(3)').text().trim()
            };
        }).get();

        if (selectedContracts.length === 0) {
            showToastWarning("Please select at least one contract to update.");
            return;
        }

        $.ajax({
            url: '/ProblematicsDashboard/UpdateFollowUpDates',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                contracts: selectedContracts,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            }),
            success: function (res) {
                if (res.success) {
                    showToastSuccess(res.message || "Follow-up dates updated successfully.");
                    loadContracts();
                } else {
                    showToastError(res.message || "Failed to update follow-up dates.");
                }
            },
            error: function () {
                showToastError("Error updating follow-up dates.");
            }
        });
    });

    $(document).on('click', '#openProblemticDashboardPopup', function (e) {
        e.preventDefault();
        let eid = $(this).data('eid');
        let contractType = $(this).data('contracttype');
        let paymentStatus = $(this).data('paymentstatus');

        $.ajax({
            url: '/ProblematicsDashboard/EditProblematicDashboardPopup',
            data: { eid: eid, contractType: contractType, paymentStatus: paymentStatus },
            type: 'GET',
            data: { eid: eid, contractType: contractType },
            success: function (html) {
                $('body').append(html);

                $('#editCobanaInvoiceNotesModel').modal('show');

                $('#editCobanaInvoiceNotesModel').on('hidden.bs.modal', function () {
                    $(this).remove();
                });
            },
            error: function () {
                showToastError("Failed to load Invoice Supplier popup.");
            }
        });
    });

    $(document).on("submit", "#editCobanaInoviceNotesForm", function (e) {
        e.preventDefault();
        const $form = $(this);
        const $btn = $form.find('button[type="submit"]');

        const payload = {
            eid: $("#eid").val(),
            contractType: $("#contractType").val(),
            supplierCobanaInvoiceNotes: $("#supplierCobanaInvoiceNotes").val(),
            paymentStatus: $("#paymentStatus").val()
        };

        $btn.prop("disabled", true).html('<i class="fas fa-spinner fa-spin me-1"></i> Saving...');

        $.ajax({
            url: '/ProblematicsDashboard/EditProblematicDashboardUpdate',
            type: 'POST',
            contentType: 'application/json',
            headers: {
                'RequestVerificationToken': $form.find('input[name="__RequestVerificationToken"]').val()
            },
            data: JSON.stringify(payload),
            success: function (res) {
                if (res.success) {
                    showToastSuccess(res.message || "Supplier Cobana Invoice Notes updated.");
                    $('#editCobanaInvoiceNotesModel').modal('hide');
                    loadContracts();
                } else {
                    showToastError(res.message || "Failed to update Supplier Cobana Invoice Notes.");
                }
            },
            error: function (xhr) {
                showToastError(xhr.responseJSON?.message || xhr.statusText || "Server error while updating.");
            },
            complete: function () {
                $btn.prop("disabled", false).html('Save');
            }
        });
    });

    loadContracts();
});
