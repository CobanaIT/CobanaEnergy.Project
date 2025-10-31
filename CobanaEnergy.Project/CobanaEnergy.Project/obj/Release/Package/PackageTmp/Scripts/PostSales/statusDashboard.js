
$(document).ready(function () {
    let table;
    let selectedEmails = [];
    let selectedType = "";
    let emailSubject = "";
    let emailBody = "";

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

        // dd/MM/yyyy or dd-MM-yyyy
        if (/^\d{2}[\/-]\d{2}[\/-]\d{4}$/.test(dateStr)) {
            const [d, m, y] = dateStr.split(/\/|-/);
            return new Date(y, m - 1, d);
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
        table = $('#statusDashboardTable').DataTable({
            serverSide: true,
            processing: true,
            autoWidth: false,
            fixedColumns: { leftColumns: 3 },
            ajax: {
                url: '/StatusDashboard/GetPostSalesContracts',
                type: 'POST',
                data: function (d) {
                    return $.extend({}, d, {
                        Supplier: $('#supplierFilter').val(),
                        ContractStatus: $('#contractstatus').val(),
                        DateFrom: $('#startDateFilter').val(),
                        DateTo: $('#endDateFilter').val(),
                        Department: $('#departmentFilter').val(),
                        BrokerageId: $('#brokerageFilter').val(),
                        StaffId: $('#brokerageStaffFilter').val(),
                        SubBrokerageId: $('#subBrokerageFilter').val(),
                        CloserId: $('#closerFilter').val(),
                        LeadGeneratorId: $('#leadGenFilter').val(),
                        ReferralPartnerId: $('#referralFilter').val(),
                        SubReferralPartnerId: $('#subReferralFilter').val(),
                        IntroducerId: $('#introducerFilter').val(),
                        SubIntroducerId: $('#subIntroducerFilter').val(),
                        Search: d.search
                    });
                },
                dataSrc: function (json) {
                    $('#contractCount').text(json.contractCount);

                    const $container = $('#statusSummaryContainer');
                    $container.empty();

                    if (json.statusSummary && json.statusSummary.length > 0) {
                        json.statusSummary.forEach(s => {
                            const isColorLight = (hex) => {
                                const c = hex.replace('#', '');
                                const r = parseInt(c.substr(0, 2), 16);
                                const g = parseInt(c.substr(2, 2), 16);
                                const b = parseInt(c.substr(4, 2), 16);
                                const brightness = (r * 299 + g * 587 + b * 114) / 1000;
                                return brightness > 160; // threshold
                            };

                            const textColor = isColorLight(s.ColorCode) ? '#333' : '#fff';
                            const badge = `
                                  <div class="status-count-box d-inline-block m-1">
                                      <span class="badge rounded-pill"
                                            style="
                                                background-color:${s.ColorCode};
                                                color:${textColor};
                                                padding:8px 14px;
                                                font-size:0.9rem;
                                                font-weight:600;
                                                letter-spacing:0.3px;
                                                text-shadow: 0 1px 2px rgba(0,0,0,0.15);
                                                box-shadow: 0 1px 3px rgba(0,0,0,0.15);
                                                transition: transform 0.2s ease, box-shadow 0.2s ease;
                                            "
                                            onmouseover="this.style.transform='scale(1.05)'; this.style.boxShadow='0 2px 6px rgba(0,0,0,0.25)';"
                                            onmouseout="this.style.transform='scale(1)'; this.style.boxShadow='0 1px 3px rgba(0,0,0,0.15)';">
                                          ${s.ContractStatus}: <strong>${s.Count}</strong>
                                      </span>
                                  </div>`;
                            $container.append(badge);
                            //const badge = `
                            // <div class="status-count-box d-inline-block m-1">
                            //     <span class="badge rounded-pill"
                            //           style="
                            //               background-color:${s.ColorCode};
                            //               color:#fff;
                            //               padding:8px 14px;
                            //               font-size:0.9rem;
                            //               font-weight:600;
                            //               letter-spacing:0.3px;
                            //               text-shadow: 0 1px 2px rgba(0,0,0,0.3);
                            //               box-shadow: 0 1px 3px rgba(0,0,0,0.2);
                            //           ">
                            //         ${s.ContractStatus}: <strong>${s.Count}</strong>
                            //     </span>
                            // </div>`;
                            // $container.append(badge);
                         });
                    } else {
                        $container.html('<small class="text-muted">No contract status summary available.</small>');
                    }



                    return json.data;
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
                    title: '',
                    filename: 'Status Dashboard',
                    exportOptions: {
                        columns: ':visible:not(:lt(3))',
                        orthogonal: 'export'
                    }
                },
                {
                    extend: 'pdfHtml5',
                    text: '<i class="fas fa-file-pdf me-2"></i> Export PDF',
                    className: 'btn btn-danger btn-sm dt-btn',
                    title: 'Status Dashboard',
                    orientation: 'landscape',
                    pageSize: 'A3',
                    exportOptions: {
                        columns: ':visible:not(:lt(3))',
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

            initComplete: function () {
                const $filterContainer = $('.date-filter-container');
                const $html = $(`
                <div class="d-flex flex-wrap align-items-end gap-3">
                    <div>
                        <label for="startDateFilter" class="form-label fw-semibold mb-0 small">Input Date From</label>
                        <input type="date" id="startDateFilter" class="form-control form-control-sm" />
                    </div>
                    <div>
                        <label for="endDateFilter" class="form-label fw-semibold mb-0 small">Input Date To</label>
                        <input type="date" id="endDateFilter" class="form-control form-control-sm" />
                    </div>
                </div>
                `);
                $filterContainer.html($html);
                $('#startDateFilter, #endDateFilter').off('change.dateFilters').on('change.dateFilters', function () {
                    table.ajax.reload(null, false);
                });
            },

            columns: [
                {
                    data: null, orderable: false, render: function (row) {
                        const url =  `/StatusDashboard/EditContractPostSales/${encodeURIComponent(row.EId)}?type=${encodeURIComponent(row.ContractType)}`;
                        let iconClass, buttonClass;
                        switch (row.ContractType) {
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
                {
                    data: null, orderable: false, render: function (row) {
                        return `
                        <div class="email-button-group">
                            <button class="btn send-email" 
                                data-eid="${row.EId}" 
                                data-emails="${row.EmailList || ''}" 
                                data-type="Supplier" data-subject="${row.EmailSubject || ''}" data-body="${row.EmailBody || ''}">
                                <i class="bi bi-envelope-fill"></i> 
                            </button>
                            <button class="btn send-email" 
                                data-eid="${row.EId}" 
                                data-emails="${row.AgentEmail || ''}" 
                                data-type="Agent" data-subject="${row.EmailSubject || ''}" data-body="${row.EmailBody || ''}">
                                <i class="bi bi-person-fill"></i>
                            </button>
                        </div>
                    `;
                    }
                },
                { data: 'Agent', render: (d, t, r, m) => renderWithCenterDash(d, t, r, m) },
                { data: 'Collaboration', render: (d, t, r, m) => renderWithCenterDash(d, t, r, m) },
                { data: 'CobanaSalesType', render: (d, t, r, m) => renderWithCenterDash(d, t, r, m) },
                { data: 'MPXN', render: (d, t, r, m) => renderWithCenterDash(d, t, r, m) },
                {
                    data: 'SupplierName', render: function (d, t, r, m) {
                        if (r.Link) {
                            return `
                            <div class="d-flex justify-content-between align-items-center">
                                <span>${d || '-'}</span>
                                <a href="${r.Link}" target="_blank" class="text-decoration-none text-primary ms-2">
                                    <i class="bi bi-link-45deg"></i>
                                </a>
                            </div>
                        `;
                        } else {
                            return renderWithCenterDash(d, t, r, m);
                        }
                    }
                },
                { data: 'BusinessName', className: 'wrap-text', render: (d, t, r, m) => renderWithCenterDash(d, t, r, m) },
                { data: 'InputDate', render: d => d ? formatDateForLabel(d) : '<span class="center-dash">-</span>' },
                { data: 'PostCode' },
                {
                    data: 'Email', className: 'email-column', render: function (d, type, row) {
                        if (type === 'export') return d || '<span class="center-dash">-</span>';
                        return `<input type="text" class="form-control form-control-sm editable-input email" data-eid="${row.EId}" data-field="Email" value="${escapeHtml(d)}" />`;
                    }
                },
                {
                    data: 'StartDate', render: function (d, type, row) {
                        if (type === 'export') return d || '<span class="center-dash">-</span>';
                        return `<input type="date" class="form-control form-control-sm editable-input startdate" data-eid="${row.EId}" data-field="StartDate" value="${formatDateForInput(d)}" />`;
                    }
                },
                {
                    data: 'CED', render: function (d, type, row) {
                        if (type === 'export') return d || '<span class="center-dash">-</span>';
                        return `<input type="date" class="form-control form-control-sm editable-input ced" data-eid="${row.EId}" data-field="CED" value="${formatDateForInput(d)}" />`;
                    }
                },
                {
                    data: 'COTDate', render: function (d, type, row) {
                        if (type === 'export') return d || '<span class="center-dash">-</span>';
                        return `<input type="date" class="form-control form-control-sm editable-input cot" data-eid="${row.EId}" data-field="COTDate" value="${formatDateForInput(d)}" />`;
                    }
                },
                {
                    data: 'ContractStatus', render: function (d, type, row) {
                        const options = AccountDropdownOptions.statusDashboardContractStatus;
                        if (type === 'export') return d || '<span class="center-dash">-</span>';
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
                {
                    data: 'ObjectionDate', render: function (d, type, row) {
                        if (type === 'export') return d || '<span class="center-dash">-</span>';
                        return `<input type="date" class="form-control form-control-sm editable-input objectiondate" data-eid="${row.EId}" data-field="ObjectionDate" value="${formatDateForInput(d)}" />`;
                    }
                },
                {
                    data: 'ObjectionCount',
                    className: 'text-center',
                    render: function (d) {
                        return d && d !== "-" ? d : `<span class="center-dash">-</span>`;
                    }
                },
                {
                    data: 'QueryType', className: 'querytype-column', title: 'Query Type',
                    render: function (data, type, row) {
                        if (type === 'export') return data || '<span class="center-dash">-</span>';
                        if (type === 'display') {
                            return `<select class="form-select query-type-dropdown w-100" data-supplier="${row.SupplierName}" data-selected="${data || ''}" data-id="${row.SupplierName}">
                            <option value="${data}">${data}</option>
                        </select>`;
                        }
                        return data;
                    }
                },
                {
                    data: 'ContractNotes',
                    render: function (data, type) {
                        if (!data) return '<span class="center-dash">-</span>';
                        return `<span class="truncate-cell">${data.length > 50 ? data.substring(0, 50) + '…' : data}</span>`;
                    },
                    createdCell: function (td, cellData) {
                        handleCenterDash(td, cellData);
                    }
                },
                { data: 'Duration', visible: false },
                { data: 'ContractType', visible: false },
                { data: 'EmailList', visible: false },
                { data: 'ContractId', visible: false }
            ],

            pageLength: 25,
            order: [[9, 'desc']],
            drawCallback: function () {
                GetQueryDropDownData();

                $('.query-type-dropdown').off('change').on('change', function () {
                    const $dropdown = $(this);
                    const val = $dropdown.val();
                    const $row = $dropdown.closest('tr');
                    const $btns = $row.find('.send-email');

                    if (!val || val === "" || val === "Select Query Type") {
                        $btns.prop('disabled', true);
                    } else {
                        $btns.prop('disabled', false);
                    }
                });

                $('.query-type-dropdown').each(function () {
                    const $dropdown = $(this);
                    const val = $dropdown.val();
                    const $row = $dropdown.closest('tr');
                    const $btns = $row.find('.send-email');
                    if (!val || val === "-" || val === "Select Query Type") {
                        $btns.prop('disabled', true);
                    } else {
                        $btns.prop('disabled', false);
                    }
                });
            }
        });

        // Enable column resizing
        enableColumnResizing('#statusDashboardTable');
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
            const cell = $(`#statusDashboardTable`).DataTable().cell(meta.row, meta.col).node();
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


    $('#statusDashboardTable').on('change keyup', '.editable, .editable-input, .contract-status, .query-type-dropdown', function () {
        const $row = $(this).closest('tr');
        $row.find('.save-row').prop('disabled', false); // sirf usi row ka save enable karo
    });

    $('#statusDashboardTable').on('click', '.save-row', function () {
        const $btn = $(this);
        const $row = $btn.closest('tr');
        const eid = $btn.data('eid');
        const rowDataTable = table.row($row).data();

        // Collect row data
        const rowData = {
            EId: eid,
            Email: $row.find('.email').val(),
            ContractId: rowDataTable.ContractId,
            StartDate: $row.find('.startdate').val(),
            CED: $row.find('.ced').val(),
            COTDate: $row.find('.cot').val(),
            ContractStatus: $row.find('.contract-status').val(),
            ObjectionDate: $row.find('.objectiondate').val(),
            ContractType: rowDataTable.ContractType,
            Duration: rowDataTable.Duration,
            QueryType: $row.find('.query-type-dropdown').val()
        };

        // Disable all save buttons while this one is processing
        $('#statusDashboardTable .save-row').prop('disabled', true);

        $btn.prop('disabled', true);
        $btn.find('.btn-text').text('');
        $btn.find('.spinner-border').removeClass('d-none');

        $.ajax({
            url: '/StatusDashboard/UpdatePostSalesRow',
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
                $('#statusDashboardTable .save-row').prop('disabled', true);
                // Optional: only this row's save reset, others stay enabled if edited
                $btn.prop('disabled', true);
                //$btn.find('.btn-text').text('Save');
                $btn.find('.btn-text').html('<i class="bi bi-save me-1"></i>');
                $btn.find('.spinner-border').addClass('d-none');
            }
        });
    });


    function GetQueryDropDownData() {
        const supplierDropdowns = {};

        // Group dropdowns by supplier
        $('.query-type-dropdown').each(function () {
            const supplier = $(this).data('supplier');
            if (!supplierDropdowns[supplier]) {
                supplierDropdowns[supplier] = [];
            }
            supplierDropdowns[supplier].push($(this));
        });

        // Ab har unique supplier ke liye ek hi request
        Object.keys(supplierDropdowns).forEach(supplier => {
            $.ajax({
                url: '/StatusDashboard/GetQueryTypes',
                type: 'GET',
                data: { supplier: supplier },
                success: function (options) {
                    supplierDropdowns[supplier].forEach($dropdown => {
                        let selectedVal = ($dropdown.data('selected') || "").trim();
                        $dropdown.empty();

                        if (!selectedVal || selectedVal === "-" || selectedVal === "") {
                            $dropdown.append(`<option value="" selected>Select Query Type</option>`);
                        }

                        options.forEach(opt => {
                            const optTrim = (opt || "").trim();
                            const selected = (optTrim.toLowerCase() === selectedVal.toLowerCase()) ? "selected" : "";
                            $dropdown.append(`<option value="${optTrim}" ${selected}>${optTrim}</option>`);
                        });

                        // Agar selectedVal backend ke options me nahi hai
                        if (selectedVal && selectedVal !== "-" && !$dropdown.val()) {
                            $dropdown.append(`<option value="${selectedVal}" selected>${selectedVal}</option>`);
                        }

                        // Page load ke time button disable/enable check
                        const $row = $dropdown.closest('tr');
                        const $btns = $row.find('.send-email');
                        if (!selectedVal || selectedVal === "-" || selectedVal === "Select Query Type") {
                            $btns.prop('disabled', true);
                        } else {
                            $btns.prop('disabled', false);
                        }
                    });
                }
            });
        });

    }


    function escapeHtml(text) {
        if (!text) return '';
        return text.replace(/[&<>"'\/]/g, function (s) {
            return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;', '/': '&#x2F;' })[s];
        });
    }

    // Init flows

    $(function () {

        populateDropdown("departmentFilter", DropdownOptions.department, $('#departmentFilter').data('current'));
        populateDropdown("contractstatus", AccountDropdownOptions.statusDashboardContractStatus, $('#contractstatus').data('current'));
        populateDropdown("paymentStatusAcc", AccountDropdownOptions.paymentStatus, $('#paymentStatusAcc').data('current'));

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

    // auto refresh on filters change
    $('#contractStatusFilter, #supplierFilter, #startDateFilter, #endDateFilter').on('change', function () {
        if (table) table.ajax.reload(null, false);
    });


    $(document).on("click", ".send-email", function () {
        const emails = $(this).data("emails");
        selectedType = ($(this).data("type") || "").toLowerCase();
        emailSubject = ($(this).data("subject") || "").toLowerCase();
        emailBody = ($(this).data("body") || "").toLowerCase();

        selectedEmails = (emails && emails !== '-')
            ? (Array.isArray(emails) ? emails : emails.split(","))
            : [];
        const $container = $("#emailListContainer");
        $container.empty();

        if (selectedEmails.length === 0) {
            $container.append(`<tr><td colspan="2" class="text-muted text-center">No emails found</td></tr>`);
        } else {
            selectedEmails.forEach((e, i) => {
                $container.append(`
                <tr>
                    <td>${e.trim()}</td>
                    <td class="text-center">
                        <button class="btn btn-sm btn-outline-primary copy-single" data-email="${e.trim()}">
                            <i class="bi bi-clipboard"></i>
                        </button>
                    </td>
                </tr>
            `);
            });
        }

        $("#emailModal").modal("show");
    });

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

    // Send via Outlook
    $(document).on("click", "#sendEmailBtn", function () {
        if (!selectedEmails.length) {
            showToastError("No emails to send!");
            return;
        }

        if (!emailSubject || !emailBody) {
            showToastError(`Please save before email to ${selectedType}`);
            return;
        }

        const to = selectedEmails.join(";");
        var emailBody = "";
        if (selectedType === "Agent") {
            window.location.href = `mailto:${to}?subject=${emailSubject}&body=${emailBody}`;
        } else {
            to = "";
            window.location.href = `mailto:${to}?subject=${emailSubject}&body=${emailBody}`;
        }

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

