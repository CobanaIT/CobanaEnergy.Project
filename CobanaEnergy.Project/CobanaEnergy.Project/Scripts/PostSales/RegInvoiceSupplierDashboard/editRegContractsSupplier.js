$(document).ready(function () {
    let table;
    let selectedEmails = [];

    const selectConfigs = {
        '#contractstatus': 'Select Contract Status'
    };

    for (const [selector, placeholder] of Object.entries(selectConfigs)) {
        $(selector).select2({
            placeholder: placeholder,
            allowClear: true,
            width: '100%'
        });
    }

    function parseToDateObject(dateStr) {
        if (!dateStr) return null;

        // yyyy-MM-dd
        if (/^\d{4}-\d{2}-\d{2}$/.test(dateStr)) {
            const [y, m, d] = dateStr.split('-');
            return new Date(y, m - 1, d);
        }

        // Handle MM/dd/yyyy and dd/MM/yyyy formats
        if (/^\d{2}[\/-]\d{2}[\/-]\d{4}$/.test(dateStr)) {
            const [first, second, year] = dateStr.split(/\/|-/);
            const firstNum = parseInt(first);
            const secondNum = parseInt(second);

            // Smart detection: if first > 12, it's day (dd/MM/yyyy)
            if (firstNum > 12) {
                return new Date(year, secondNum - 1, firstNum); // dd/MM/yyyy
            }
            // If second > 12, it's day (MM/dd/yyyy)
            else if (secondNum > 12) {
                return new Date(year, firstNum - 1, secondNum); // MM/dd/yyyy
            }
            // Ambiguous case: check your backend format
            // C# typically returns MM/dd/yyyy for US locale
            else {
                return new Date(year, firstNum - 1, secondNum); // Default to MM/dd/yyyy
            }
        }

        const parsed = new Date(dateStr);
        return isNaN(parsed) ? null : parsed;
    }


    // For INPUT type="date"
    function formatDateForInput(dateStr) {
        if (!dateStr) return '';
        const d = parseToDateObject(dateStr);
        if (!(d instanceof Date) || isNaN(d)) return '';


        const yyyy = d.getFullYear();
        const mm = String(d.getMonth() + 1).padStart(2, '0');
        const dd = String(d.getDate()).padStart(2, '0');
        return `${yyyy}-${mm}-${dd}`;
    }

    // For LABEL / EXPORT / DISPLAY
    function formatDateForLabel(dateStr) {
        const d = parseToDateObject(dateStr);
        if (!d) return '-';
        const yyyy = d.getFullYear();
        const mm = String(d.getMonth() + 1).padStart(2, '0');
        const dd = String(d.getDate()).padStart(2, '0');
        return `${dd}-${mm}-${yyyy}`; // ✅ Human-readable display
    }



    function initTable() {
        table = $('#editRegSupplierContractTable').DataTable({
            serverSide: false,
            processing: true,
            autoWidth: false,
            fixedColumns: { leftColumns: 2 },
            ajax: {
                url: '/RegInvoiceSupplierDashboard/GetRegSupplierContracts',
                type: 'GET',
                dataSrc: function (json) {
                    console.log('Received data:', json); // Debug log

                    // Handle array response
                    if (Array.isArray(json)) {
                        console.log('Data is array, count:', json.length);
                        return json;
                    }

                    // Handle object with data property
                    if (json && json.data && Array.isArray(json.data)) {
                        return json.data;
                    }

                    console.error('Invalid data format:', json);
                    return [];
                },
                error: function (xhr, error, code) {
                    console.error('AJAX Error:', { xhr, error, code });
                    showToastError('Failed to load contract data. Please refresh the page.');
                }
            },
            dom: `
                <"row mb-2 align-items-end justify-content-between"
                    <"col-auto d-flex flex-wrap align-items-end gap-3" <"date-filter-container"> >
                    <"col-auto d-flex align-items-center gap-2" B >
                >
                <"row mb-2"
                    <"col-sm-12 col-md-6 d-flex align-items-center" l >
                    <"col-sm-12 col-md-6 text-end" f >
                >
                rt
                <"row mt-2"
                    <"col-sm-12 col-md-5" i >
                    <"col-sm-12 col-md-7" p >
                >
            `,
            buttons: [
                {
                    extend: 'excelHtml5',
                    text: '<i class="fas fa-file-excel me-2"></i> Export Excel',
                    className: 'btn btn-success btn-sm dt-btn',
                    title: 'Reg Supplier Contracts',
                    filename: 'RegSupplierContracts',
                    exportOptions: {
                        columns: ':visible:not(:lt(2))',
                        orthogonal: 'export'
                    }
                },
                {
                    extend: 'pdfHtml5',
                    text: '<i class="fas fa-file-pdf me-2"></i> Export PDF',
                    className: 'btn btn-danger btn-sm dt-btn',
                    title: 'Reg Supplier Contracts',
                    orientation: 'landscape',
                    pageSize: 'A3',
                    exportOptions: {
                        columns: ':visible:not(:lt(2))',
                        orthogonal: 'export'
                    },
                    customize: function (doc) {
                        doc.defaultStyle.fontSize = 3;
                        doc.styles.tableHeader.fontSize = 4;
                        doc.content[1].table.widths =
                            Array(doc.content[1].table.body[0].length + 1).join('*').split('');
                    }
                }
            ],
            columns: [
                {
                    data: null, orderable: false, render: function (row) {
                        const url = `/StatusDashboard/EditContractPostSales/${encodeURIComponent(row.EId)}?type=${encodeURIComponent(row.ContractType || 'Electric')}`;
                        let iconClass = 'fas fa-pencil-alt';
                        let buttonClass = 'btn btn-sm btn-info';

                        if (row.MPAN) {
                            iconClass = 'fas fa-bolt';
                            buttonClass = 'btn btn-sm btn-primary';
                        } else if (row.MPRN) {
                            iconClass = 'fas fa-fire';
                            buttonClass = 'btn btn-sm btn-danger';
                        }

                        return `<a class="${buttonClass}" href="${url}" target="_blank">
                                <i class="${iconClass}"></i>
                            </a>`;
                    }
                },
                {
                    data: null, orderable: false, render: function (row) {
                        return `
                        <div class="text-center">
                            <button class="btn btn-sm btn-success save-row" data-eid="${row.EId}" disabled>
                                <span class="btn-text"><i class="bi bi-save2"></i></span>
                                <span class="spinner-border spinner-border-sm d-none" role="status" aria-hidden="true"></span>
                            </button>
                        </div>
                    `;
                    }
                },
                { data: 'BusinessName', className: 'wrap-text', render: (d, t, r, m) => renderWithCenterDash(d, t, r, m) },
                { data: 'PostCode', render: (d, t, r, m) => renderWithCenterDash(d, t, r, m) },
                { data: 'MPAN', render: (d, t, r, m) => renderWithCenterDash(d, t, r, m) },
                { data: 'MPRN', render: (d, t, r, m) => renderWithCenterDash(d, t, r, m) },
                { data: 'InputDate', render: d => d ? formatDateForLabel(d) : '<span class="center-dash">-</span>' },
                {
                    data: 'StartDate', render: function (d, type, row) {
                        if (type === 'export') return d || '-';
                        return `<input type="date" class="form-control form-control-sm editable-input startdate" data-eid="${row.EId}" data-field="StartDate" value="${formatDateForInput(d)}" />`;
                    }
                },
                {
                    data: 'CED', render: function (d, type, row) {
                        if (type === 'export') return d || '-';
                        return `<input type="date" class="form-control form-control-sm editable-input ced" data-eid="${row.EId}" data-field="CED" value="${formatDateForInput(d)}" />`;
                    }
                },
                {
                    data: 'CED_COT', render: function (d, type, row) {
                        if (type === 'export') return d || '-';
                        return `<input type="date" class="form-control form-control-sm editable-input cedcot" data-eid="${row.EId}" data-field="CED_COT" value="${formatDateForInput(d)}" />`;
                    }
                },
                { data: 'Duration', render: (d, t, r, m) => renderWithCenterDash(d, t, r, m) },
                {
                    data: 'ContractStatus', render: function (d, type, row) {
                        const options = AccountDropdownOptions.statusDashboardContractStatus || [];
                        if (type === 'export') return d || '-';
                        let html = `<select class="form-select form-select-sm contract-status" data-eid="${row.EId}" data-field="ContractStatus">`;
                        html += `<option value="">${d ?? '-'}</option>`;
                        options.forEach(o => {
                            const sel = (o === d) ? 'selected' : '';
                            html += `<option value="${o}" ${sel}>${o}</option>`;
                        });
                        html += `</select>`;
                        return html;
                    }
                },
                { data: 'ContractNotes', render: (d, t, r, m) => renderWithCenterDash(d, t, r, m) },
                { data: 'ContractType', visible: false },
                { data: 'SupplierId', visible: false },
                { data: 'SupplierName', visible: false }
            ],
            pageLength: 25,
            order: [[6, 'desc']], // Sort by InputDate column
            language: {
                processing: '<div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div>',
                emptyTable: 'No contract data available',
                zeroRecords: 'No matching records found'
            },
            drawCallback: function () {
                // No query dropdown logic needed
            }
        });

        // Enable column resizing with CORRECT table ID
        if (typeof enableColumnResizing === 'function') {
            enableColumnResizing('#editRegSupplierContractTable');
        }
    }


    function handleCenterDash(td, value) {
        if (value === null || value === undefined || value === "" || value === "-") {
            $(td).addClass("text-center");
        } else {
            $(td).removeClass("text-center");
        }
    }

    function renderWithCenterDash(value, type, row, meta, formatter) {
        // Skip non-display types (for export/sort)
        if (type !== "display") return value;

        // Get current <td> after render (slight delay to ensure DOM created)
        setTimeout(() => {
            const cell = $(`#editRegSupplierContractTable`).DataTable().cell(meta.row, meta.col).node();
            if (!cell) return;

            if (value === "-" || value === null || value === undefined || value === "") {
                $(cell).addClass("text-center");
            } else {
                $(cell).removeClass("text-center");
            }
        }, 0);

        // Format or return dash
        if (value === null || value === undefined || value === "") return "-";
        const formatted = formatter ? formatter(value) : value;
        return formatted === "-" ? "-" : formatted;
    }


    $('#editRegSupplierContractTable').on('change keyup', '.editable, .editable-input, .contract-status, .query-type-dropdown', function () {
        const $row = $(this).closest('tr');
        $row.find('.save-row').prop('disabled', false); // sirf usi row ka save enable karo
    });

    $('#editRegSupplierContractTable').on('click', '.save-row', function () {
        const $btn = $(this);
        const $row = $btn.closest('tr');
        const eid = $btn.data('eid');
        const rowDataTable = table.row($row).data();

        // Collect row data
        const rowData = {
            EId: eid,
            InputDate: $row.find('.inputDate').val(),
            StartDate: $row.find('.startdate').val(),
            CED: $row.find('.ced').val(),
            COTDate: $row.find('.cedcot').val(),
            ContractStatus: $row.find('.contract-status').val(),
            ContractType: rowDataTable.ContractType,
            Duration: rowDataTable.Duration
        };

        // Disable all save buttons while this one is processing
        $('#editRegSupplierContractTable .save-row').prop('disabled', true);

        $btn.prop('disabled', true);
        $btn.find('.btn-text').text('');
        $btn.find('.spinner-border').removeClass('d-none');

        $.ajax({
            url: '/RegInvoiceSupplierDashboard/UpdatePostSalesRow',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(rowData),
            success: function (res) {
                if (res.success) {
                    showToastSuccess("Row updated successfully!");
                    table.ajax.reload(null, false);
                } else {
                    showToastError("Failed to update row.");
                }
            },
            error: function () {
                showToastError("Server error occurred.");
            },
            complete: function () {
                // After request finishes, re-enable all Save buttons except this row (disable after save)
                $('#editRegSupplierContractTable .save-row').prop('disabled', true);
                // Optional: only this row's save reset, others stay enabled if edited
                $btn.prop('disabled', true);
                //$btn.find('.btn-text').text('Save');
                $btn.find('.btn-text').html('<i class="bi bi-save me-1"></i>');
                $btn.find('.spinner-border').addClass('d-none');
            }
        });
    });

    // Init flows

    $(function () {
        populateDropdown("contractstatus", AccountDropdownOptions.statusDashboardContractStatus, $('#contractstatus').data('current'));

        initTable();
        FilterModule.init(table);

    });

    function populateDropdown(id, values, current) {
        const $el = $('#' + id);
        if (!$el.length) return;

        let placeholder = "Select " + id.replace(/([A-Z])/g, ' $1').trim();
        $el.empty().append(`<option value="">${placeholder}</option>`);
        if (Array.isArray(values)) {
            values.forEach(v => {
                const selected = v === current ? 'selected' : '';
                $el.append(`<option value="${v}" ${selected}>${v}</option>`);
            });
        }
    }

    // Copy All
    $(document).on("click", "#copyAllBtn", function () {
        if (selectedEmails.length) {
            navigator.clipboard.writeText(selectedEmails.join(";"));
            showToastSuccess("All emails copied!");
        }
    });

    // Copy single
    $(document).on("click", ".copy-single", function () {
        const email = $(this).data("email");
        navigator.clipboard.writeText(email);
        showToastSuccess(`Copied: ${email}`);
    });


    // When Contract Status changes
    $(document).on("change", ".contract-status", function () {
        const $row = $(this).closest("tr");
        const selectedStatus = $(this).val();
        const $objectionDateInput = $row.find(".objectiondate");

        if (selectedStatus && selectedStatus.toLowerCase().includes("objection")) {
            const today = new Date().toISOString().split("T")[0];
            $objectionDateInput.val(today).trigger("updateByStatus");
        }
    });

    // When Objection Date changes by user
    $(document).on("change", ".objectiondate", function () {
        const $row = $(this).closest("tr");
        const dateVal = $(this).val();
        const $contractStatusSelect = $row.find(".contract-status");

        if (dateVal) {
            const hasObjection = $contractStatusSelect.find("option[value='Objection']").length > 0;
            if (hasObjection) {
                $contractStatusSelect.val("Objection").trigger("updateByDate");
            }
        }
    });


});

