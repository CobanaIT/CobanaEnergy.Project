$(document).ready(function () {
    let table;
    let allFollowUpDates = [];
    let selectedDate = "";

    const selectConfigs = {
        '#contractstatus': 'Select Contract Status',
        '#paymentStatusAcc': 'Select Payment Status',
    };

    for (const [selector, placeholder] of Object.entries(selectConfigs)) {
        $(selector).select2({
            placeholder: placeholder,
            allowClear: true,
            width: '100%'
        });
    }


    function dataTableInit() {
        table = $('#accountMasterTable').DataTable({
            serverSide: true,
            processing: true,
            ajax: {
                url: '/AccountMasterDashboard/GetContracts',
                type: 'POST',
                data: function (d) {
                    return $.extend({}, d, {
                        Supplier: $('#supplierFilter').val(),
                        ContractStatus: $('#contractstatus').val(),
                        PaymentStatus: $('#paymentStatusAcc').val(),
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
                }
            },
            pageLength: 25,
            order: [[6, 'desc']],
            dom:
                '<"row mb-2"<"col-sm-12 text-end"B>>' +
                '<"row mb-2"<"col-sm-6"l><"col-sm-6"f>>' +
                'rtip',
            buttons: [
                {
                    extend: 'excelHtml5',
                    text: '<i class="fas fa-file-excel me-2"></i> Export Excel',
                    className: 'btn btn-success btn-sm dt-btn',
                    title: '',
                    filename: function() {
                        let filename = 'AccountMasterDashboard';
                        
                        const contractStatus = $('#contractstatus').val();
                        const paymentStatus = $('#paymentStatusAcc').val();
                        
                        if (contractStatus) {
                            filename += `_${contractStatus}`;
                        }
                        
                        if (paymentStatus) {
                            filename += `_${paymentStatus}`;
                        }
                        
                        return filename;
                    },
                    exportOptions: {
                        columns: ':visible:not(:first-child)' 
                    }
                },
                {
                    extend: 'pdfHtml5',
                    text: '<i class="fas fa-file-pdf me-2"></i> Export PDF',
                    className: 'btn btn-danger btn-sm dt-btn',
                    title: 'AccountMaster',
                    orientation: 'landscape',
                    pageSize: 'A3',
                    filename: function() {
                        let filename = 'AccountMasterDashboard';
                        
                        const contractStatus = $('#contractstatus').val();
                        const paymentStatus = $('#paymentStatusAcc').val();
                        
                        if (contractStatus) {
                            filename += `_${contractStatus}`;
                        }
                        
                        if (paymentStatus) {
                            filename += `_${paymentStatus}`;
                        }
                        
                        return filename;
                    },
                    exportOptions: {
                        columns: ':visible:not(:first-child)' 
                    },
                    customize: function (doc) {
                        doc.defaultStyle.fontSize = 5;
                        doc.styles.tableHeader.fontSize = 6;
                        
                        // More robust table width setting
                        if (doc.content && doc.content.length > 1) {
                            for (let i = 0; i < doc.content.length; i++) {
                                if (doc.content[i].table && doc.content[i].table.body && doc.content[i].table.body.length > 0) {
                                    const colCount = doc.content[i].table.body[0].length;
                                    doc.content[i].table.widths = Array(colCount).fill('*');
                                    break;
                                }
                            }
                        }
                    }
                }
            ],
            columns: [
                {
                    data: 'EId',
                    orderable: false,
                    searchable: false,
					render: function (data, type, row) {
						if (type !== 'display') return data;

						const mp = encodeURIComponent(row.MPXN || '');
						const supplierId = row.SupplierId || '';
						const id = row.EId;
						const controller = row.Controller;
						const action = row.Action;

						if (!controller || !action) {
							return '<span class="text-muted">-</span>';
						}

						const url = `/${controller}/${action}/${id}?supplierId=${supplierId}&type=${mp}`;
						
						// Determine contract type and styling
						let iconClass, buttonClass;
						switch(row.ContractType) {
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
						
						return `<a class="${buttonClass}" href="${url}" target="_blank" title="Edit"><i class="${iconClass}"></i></a>`;
					}
                },
                { data: 'SupplierName' },
                { data: 'BusinessName' },
                { data: 'MPXN' },
                { data: 'InputEAC' },
                { data: 'SupplierEAC' },
                {
                    data: 'InputDate',
                    render: d => d ? formatDateLabel(d) : '-'
                },
                {
                    data: 'StartDate',
                    render: d => d ? formatDateLabel(d) : '-'
                },
                {
                    data: 'CED',
                    render: d => d ? formatDateLabel(d) : '-'
                },
                {
                    data: 'COTDate',
                    render: d => d ? formatDateLabel(d) : '-'
                },
                { data: 'ContractStatus' },
                { data: 'PaymentStatus' },
                { data: 'PreviousInvoiceNumbers' },
                {
                    data: 'AccountsNotes',
                    render: function (data, type, row) {
                        if (!data) return '-';
                        return `<span class="truncate-cell">
                           ${data.length > 50 ? data.substring(0, 50) + '…' : data}
                         </span>`;
                    }
                },
                { data: 'CommissionForecast', render: d => d ?? '-' },
                { data: 'CobanaDueCommission', render: d => d ?? '-' },
                { data: 'CobanaPaidCommission', render: d => d ?? '-' },
                { data: 'CobanaFinalReconciliation', render: d => d ?? '-' }
            ],
            drawCallback: function () {
                $('#accountMasterTable tbody tr').each(function () {
                    $(this).find('td').each(function (idx) {
                        // Skip the first column (Edit button) and any cell that already has child elements (like icons/buttons)
                        if (idx === 0) return;
                        if ($(this).children().length > 0) return;
                        if ($(this).text().trim() === '') $(this).text('-');
                    });
                });
            }
        });

        // Enable column resizing
        enableColumnResizing('#accountMasterTable');
    }

    $(function () {
        populateDropdown("departmentFilter", DropdownOptions.department, $('#departmentFilter').data('current'));
        populateDropdown("contractstatus", AccountDropdownOptions.contractStatus, $('#contractstatus').data('current'));
        populateDropdown("paymentStatusAcc", AccountDropdownOptions.paymentStatus, $('#paymentStatusAcc').data('current'));

        dataTableInit();

        FilterModule.init(table);
       
    });

    function formatDateLabel(dateStr) {
        if (!dateStr || dateStr === "N/A" || dateStr === "-") return "-";

        try {
            let d;

            // Handle formats like 22-09-25, 22/09/25, 22-09-2025, 2025-09-19, etc.
            if (/^\d{2}[-/]\d{2}[-/]\d{2,4}$/.test(dateStr)) {
                const parts = dateStr.split(/[-/]/);
                let day = parts[0];
                let month = parts[1];
                let year = parts[2];

                // If year is 2 digits, assume 20xx
                if (year.length === 2) year = "20" + year;

                d = new Date(`${year}-${month}-${day}`);
            }
            // ISO format
            else if (/^\d{4}-\d{2}-\d{2}/.test(dateStr)) {
                d = new Date(dateStr);
            } else {
                d = new Date(dateStr);
            }

            if (isNaN(d)) return "-";

            const dd = String(d.getDate()).padStart(2, "0");
            const mm = String(d.getMonth() + 1).padStart(2, "0");
            const yyyy = d.getFullYear();

            return `${dd}-${mm}-${yyyy}`;
        } catch {
            return "-";
        }
    }


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

});
