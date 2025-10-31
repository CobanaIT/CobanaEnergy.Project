$(document).ready(async function () {

    function getEIdFromUrl() {
        const segments = window.location.pathname.replace(/\/$/, '').split('/');
        return segments[segments.length - 1];
    }

    function loadSupplierEAC(supplierCommsType) {

        var duration = $("#durationElectric").val() || $("#durationGas").val();

        if (duration) {
            duration = Math.ceil(parseFloat(duration));
        } else {
            duration = 0;
        }

        const select = document.getElementById("eacYear");
        select.innerHTML = '<option value="">Select Option</option>';

        const paymentStatus = document.getElementById("paymentStatus");
        paymentStatus.innerHTML = '<option value="">Select Option</option>';

        populateDropdown("paymentStatus", AccountDropdownOptions.paymentStatusCorona, $('#paymentStatus').data('current'));

        if (supplierCommsType && supplierCommsType.toLowerCase() === "quarterly") {

            let awaitingInvoiceOption = document.createElement("option");
            awaitingInvoiceOption.value = "EDF Awaiting Invoice";
            awaitingInvoiceOption.textContent = "EDF Awaiting Invoice";
            paymentStatus.appendChild(awaitingInvoiceOption);

            for (let year = 1; year <= duration; year++) {
                let suffix;
                if (year % 10 === 1 && year % 100 !== 11) suffix = "st";
                else if (year % 10 === 2 && year % 100 !== 12) suffix = "nd";
                else if (year % 10 === 3 && year % 100 !== 13) suffix = "rd";
                else suffix = "th";

                // 4 quarters per year
                for (let qtr = 1; qtr <= 4; qtr++) {
                    // Build the display text: "EAC 1st Year - Qtr 1"
                    let text = `EAC ${year}${suffix} Year - Qtr ${qtr}`;

                    // Create option for the first select
                    let option1 = document.createElement("option");
                    option1.value = text;
                    option1.textContent = text;
                    select.appendChild(option1);

                    // Create option for the second select
                    let option2 = document.createElement("option");
                    option2.value = text;
                    option2.textContent = text;
                    paymentStatus.appendChild(option2);
                }
            }
           
        } else {
            const eacOptions = [
                "1ST YEAR EAC-INITIAL",
                "1ST YEAR EAC-FINAL",
                "2ND YEAR EAC-INITIAL",
                "2ND YEAR EAC-FINAL",
                "3RD YEAR EAC-INITIAL",
                "3RD YEAR EAC-FINAL",
                "4TH YEAR EAC-INITIAL",
                "4TH YEAR EAC-FINAL",
                "5TH YEAR EAC-INITIAL",
                "5TH YEAR EAC-FINAL"
            ];
            eacOptions.forEach(optionText => {
                let option = document.createElement("option");
                option.value = optionText;
                option.textContent = optionText;
                select.appendChild(option);
            });

        }

    }

    const eid = getEIdFromUrl();
    $("#eid").val(eid);

    $(function () {
        populateDropdown("department", DropdownOptions.department, $('#department').data('current'));
        populateDropdown("salesTypeElectric", DropdownOptions.salesType, $('#salesTypeElectric').data('current'));
        populateDropdown("salesTypeGas", DropdownOptions.salesType, $('#salesTypeGas').data('current'));
        populateDropdown("supplierCommsTypeElectric", DropdownOptions.supplierCommsType, $('#supplierCommsTypeElectric').data('current'));
        populateDropdown("supplierCommsTypeGas", DropdownOptions.supplierCommsType, $('#supplierCommsTypeGas').data('current'));
        populateDropdown("contractStatus", AccountDropdownOptions.contractStatus, $('#contractStatus').data('current'));
        populateDropdown("paymentStatus", AccountDropdownOptions.paymentStatusCorona, $('#paymentStatus').data('current'));

        // Comms Type
        const commsType = $("#gasContract").val() === "true"
            ? $("#supplierCommsTypeGas").val()
            : $("#supplierCommsTypeElectric").val();

        calculatePaymentDate();
        loadSupplierEAC(commsType);
        loadEacLogs();
    });

    function populateDropdown(id, values, current) {
        const $el = $('#' + id);
        if (!$el.length) return;

        let placeholder;

        if (id.includes("supplierCommsType")) {
            placeholder = "Select Comms Type";
        } else if (id.includes("salesType")) {
            placeholder = "Select Sales Type";
        } else {
            placeholder = "Select " + id.replace(/([A-Z])/g, ' $1').trim();
        }

        $el.empty().append(`<option value="">${placeholder}</option>`);
        if (Array.isArray(values)) {
            values.forEach(v => {
                const selected = v === current ? 'selected' : '';
                $el.append(`<option value="${v}" ${selected}>${v}</option>`);
            });
        }
    }

    $("#eacLogForm").on("submit", function (e) {
        e.preventDefault();
        const $btn = $(this).find('button[type="submit"]');

        if (!validateForm($(this))) return;

        const payload = getEacPayload();

        $btn.prop("disabled", true).html('<i class="fas fa-spinner fa-spin me-1"></i> Saving...');

        $.ajax({
            url: '/EDFContract/SaveEacLog',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function (res) {
                if (res.success) {
                    showToastSuccess("EAC Log saved successfully.");
                    $("#eacLogForm")[0].reset();

                    let finalEac = res.Data?.[0]?.FinalEac ?? 0;
                    let decimalPart = finalEac.toString().split('.')[1];
                    if (decimalPart) {
                        if (decimalPart.length > 2) {
                            finalEac = Math.round(finalEac * 100) / 100;
                        }
                    } else {
                        finalEac = Number(finalEac).toFixed(2);
                    }
                    $("#finalEac").val(finalEac);

                    renderEacLogs(res.Data);
                } else {
                    showToastError(res.message || "Failed to save EAC Log.");
                }
            },
            error: function (xhr) {
                showToastError(xhr.responseJSON?.message || xhr.statusText || "Server error while saving EAC Log.");
            },
            complete: function () {
                $btn.prop("disabled", false).html('Save EAC Log');
            }
        });
    });

    function loadEacLogs() {
        if (!eid) return;
        $.get(`/EDFContract/GetEacLogs?eid=${eid}&contractType=${$("#contractType").val()}`, function (res) {
            if (!res.success || !res.Data || !res.Data.length) {
                $("#bgbInvoiceLogsContainer").html('<span class="text-muted">No logs yet. Save EAC entries to view them here.</span>');
                return;
            }
            renderEacLogs(res.Data);
        });
    }

    function renderEacLogs(logs) {
        const $panel = $("#bgbInvoiceLogsContainer");
        if (!logs || !logs.length) {
            $panel.html('<span class="text-muted">No logs yet. Save EAC entries to view them here.</span>');
            return;
        }

        const html = logs.map(log => `
        <div class="log-entry">
            <div class="log-date">${escapeHtml(log.Timestamp)}</div>
            <div class="log-field"><span class="log-label">Year:</span> ${escapeHtml(log.EacYear)}</div>
            <div class="log-field"><span class="log-label">EAC Value:</span> ${escapeHtml(log.EacValue)}</div>
            <div class="log-field"><span class="log-label">Supplier AVG. EAC:</span> ${Number(log.FinalEac).toFixed(2)}</div>
            <div class="log-field"><span class="log-label">Invoice No:</span> ${escapeHtml(log.InvoiceNo)}</div>
            <div class="log-field"><span class="log-label">Invoice Date:</span> ${escapeHtml(log.InvoiceDate)}</div>
            <div class="log-field"><span class="log-label">Payment Date:</span> ${escapeHtml(log.PaymentDate)}</div>
            <div class="log-field"><span class="log-label">Invoice (£):</span> ${escapeHtml(log.InvoiceAmount)}</div>
            <div class="log-field"><span class="log-label">MPAN:</span> ${escapeHtml(log.MPAN || "N/A")}</div>
            <div class="log-field"><span class="log-label">MPRN:</span> ${escapeHtml(log.MPRN || "N/A")}</div>
        </div>
    `).join('');
        $panel.html(html);
    }

    $("#exportInvoiceLogsBtn").on("click", function () {
        $.get(`/EDFContract/GetEacLogs?eid=${eid}&contractType=${$("#contractType").val()}`, function (res) {
            if (!res.success || !res.Data?.length) {
                showToastWarning("No logs to export.");
                return;
            }
            const exportData = res.Data;
            const data = [
                ["Year", "EAC Value", "Supplier AVG. EAC", "Invoice No", "Invoice Date", "Payment Date", "Invoice (£)", "Supplier EAC D19", "MPAN", "MPRN"],
                ...exportData.map(log => [
                    log.EacYear, log.EacValue, log.FinalEac, log.InvoiceNo, log.InvoiceDate,
                    log.PaymentDate, log.InvoiceAmount, log.MPAN || "N/A", log.MPRN || "N/A"
                ])
            ];
            const ws = XLSX.utils.aoa_to_sheet(data);
            const wb = XLSX.utils.book_new();
            XLSX.utils.book_append_sheet(wb, ws, "InvoiceLogs");
            XLSX.writeFile(wb, "EDF_InvoiceLogs.xlsx");
        })
            .fail(function (xhr) {
                showToastError("Error fetching logs for export.");
            });
    });
    //Calculate Payment date on change
    $("#invoiceDate").on("change", function () {
        const invoiceDateStr = $(this).val();
        if (!invoiceDateStr) return;

        const paymentDate = calculatePaymentDate(invoiceDateStr);
        if (paymentDate) {
            $("#paymentDate").val(paymentDate);
        }
    });

    function calculatePaymentDate(invoiceDateStr) {
        const invoiceDate = new Date(invoiceDateStr);
        if (isNaN(invoiceDate.getTime())) return "";

        // Add 30 days
        let targetDate = new Date(invoiceDate.getTime());
        targetDate.setDate(targetDate.getDate() + 30);

        // Format as YYYY-MM-DD
        const yyyy = targetDate.getFullYear();
        const mm = String(targetDate.getMonth() + 1).padStart(2, '0');
        const dd = String(targetDate.getDate()).padStart(2, '0');
        return `${yyyy}-${mm}-${dd}`;
    }

    $("#editEDFContractForm").on("submit", async function (e) {
        e.preventDefault();
        const $btn = $(this).find('button[type="submit"]');
        $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i> Updating...');

        const eid = $('#eid').val();

        try {
            // Validate Uplifts
            if ($('#upliftElectric').length) {
                const $upliftElectric = $('#upliftElectric');
                const isValidElectric = await window.validateUpliftAgainstSupplierLimitElectric(
                    $upliftElectric, "N/A", eid
                );
                if (!isValidElectric) {
                    $upliftElectric.focus();
                    $btn.prop('disabled', false).html('Update Contract');
                    return;
                }
            }

            if ($('#upliftGas').length) {
                const $upliftGas = $('#upliftGas');
                const isValidGas = await window.validateUpliftAgainstSupplierLimitGas(
                    $upliftGas, "N/A", eid
                );
                if (!isValidGas) {
                    $upliftGas.focus();
                    $btn.prop('disabled', false).html('Update Contract');
                    return;
                }
            }

            if (!validateForm($(this))) {
                $btn.prop('disabled', false).html('Update Contract');
                return;
            }

            const token = $('input[name="__RequestVerificationToken"]').val();
            const payload = {
                EId: $('#eid').val(),
                HasElectricDetails: $('#upliftElectric').length > 0,
                HasGasDetails: $('#upliftGas').length > 0,
                UpliftElectric: $('#upliftElectric').val(),
                UpliftGas: $('#upliftGas').val(),
                SupplierCommsTypeElectric: $('#supplierCommsTypeElectric').val(),
                CommissionElectric: $('#CommissionElectric').val(),
                SupplierCommsTypeGas: $('#supplierCommsTypeGas').val(),
                CommissionGas: $('#CommissionGas').val(),
                ContractNotes: $('#contractNotes').val(),
                DurationElectric: $('#durationElectric').val(),
                DurationGas: $('#durationGas').val(),
                contractStatus: $('#contractStatus').val(),
                paymentStatus: $('#paymentStatus').val(),
                OtherAmount: $('#otherAmount').val(),
                StartDate: $('#startDate').val(),
                Ced: $('#ced').val(),
                CedCOT: $('#cedCOT').val(),
                CotLostConsumption: $('#cotLostConsumption').val(),
                CobanaDueCommission: $('#cobanaDueCommission').val(),
                CobanaFinalReconciliation: $('#cobanaFinalReconciliation').val(),
                CommissionFollowUpDate: $('#commissionFollowUpDate').val(),
                SupplierCobanaInvoiceNotes: $('#supplierCobanaInvoiceNotes').val()
            };
     
            $.ajax({
                url: '/EDFContract/UpdateContract',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(payload),
                success: function (res) {
                    if (res.success) {
                        showToastSuccess("Contract updated successfully.");
                        setTimeout(function () {
                            window.location.reload();
                        }, 1000);
                    } else {
                        showToastError(res.message || "Failed to update contract.");
                    }
                },
                error: function () {
                    showToastError("Server error while updating contract.");
                },
                complete: function () {
                    $btn.prop('disabled', false).html('Update Contract');
                }
            });
        } catch (err) {
            showToastError("Unexpected error during validation.");
            $btn.prop('disabled', false).html('Update Contract');
        }
    });

    //CED
    function calculateCED() {
        const startDateStr = $("#startDate").val();
        const durationStr = $("#durationElectric").val() || $("#durationGas").val();
        if (!startDateStr || !durationStr) return;

        const startDate = new Date(startDateStr);
        const duration = parseInt(durationStr, 10);
        if (isNaN(startDate.getTime()) || isNaN(duration)) return;

        const cedDate = new Date(startDate);
        cedDate.setFullYear(cedDate.getFullYear() + duration);
        cedDate.setDate(cedDate.getDate() - 1);

        const yyyy = cedDate.getFullYear();
        const mm = String(cedDate.getMonth() + 1).padStart(2, '0');
        const dd = String(cedDate.getDate()).padStart(2, '0');

        $("#ced").val(`${yyyy}-${mm}-${dd}`);
    }

    // Run on change of Start Date or Duration
    $("#startDate").on("change keyup", calculateCED);

    $("#supplierCommsTypeElectric, #supplierCommsTypeGas").on("change", function () {
        const commsType = $(this).val();
        loadSupplierEAC(commsType);
    });

    function validateForm($form) {
        let valid = true;
        $form.find('[required]').each(function () {
            const $field = $(this);
            if (!$field.val().trim()) {
                $field.addClass('is-invalid');
                valid = false;
            } else {
                $field.removeClass('is-invalid');
            }
        });
        if (!valid) {
            showToastWarning("Please fill all required fields.");
            $form.find('.is-invalid:first')[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
        return valid;
    }

    function getContractTypeFromUrl() {
        const params = new URLSearchParams(window.location.search);
        const type = params.get('type');
        if (/^\d{13}$/.test(type)) return "Electric";
        if (/^\d{6,10}$/.test(type)) return "Gas";
        return "Unknown";
    }

    const contractType = getContractTypeFromUrl();
    $("#contractType").val(contractType);

    function getEacPayload() {
        return {
            EId: $("#eid").val(),
            ContractType: $("#contractType").val(),
            EacYear: $("#eacYear").val(),
            EacValue: $("#eacValue").val(),
            FinalEac: $("#finalEac").val(),
            InvoiceNo: $("#invoiceNo").val(),
            InvoiceDate: $("#invoiceDate").val(),
            PaymentDate: $("#paymentDate").val(),
            SupplierCommsTypeElectric: $("#supplierCommsTypeElectric").val(),
            SupplierCommsTypeGas: $("#supplierCommsTypeGas").val(),
            InvoiceAmount: $("#invoiceAmount").val(),
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        };
    }

    function escapeHtml(text) {
        return $('<div>').text(text).html();
    }

    $('#upliftElectric').on('blur', async function () {
        await validateUpliftAgainstSupplierLimitElectric($('#upliftElectric'), "N/A", $('#eid').val());
    });
    $('#upliftGas').on('blur', async function () {
        await window.validateUpliftAgainstSupplierLimitGas(
            $('#upliftGas'),
            "N/A",
            $('#eid').val()
        );
    });


    function toggleCotLostConsumption() {
        if ($.trim($("#cedCOT").val()) !== "") {
            $("#cotLostConsumptionWrapper").show();
            // When cotLostConsumptionWrapper is shown, supplier EAC fields should not be required
            $("#eacYear").removeAttr('required');
            $("#eacValue").removeAttr('required');
        } else {
            $("#cotLostConsumptionWrapper").hide();
            // When cotLostConsumptionWrapper is hidden, supplier EAC fields should be required
            $("#eacYear").attr('required', 'required');
            $("#eacValue").attr('required', 'required');
        }
    }

    // Run on page load
    toggleCotLostConsumption();

    // Run when user changes the date
    $("#cedCOT").on("change", toggleCotLostConsumption);


});

