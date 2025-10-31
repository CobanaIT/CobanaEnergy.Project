

$(document).ready(async function () {

    let selectedEmails = [];
    let selectedType = "";
    let emailSubject = "";
    let emailBody = "";


    if (!$('#editGasForm').length) return;

    // --- lock ---
    const contractId = $('#eid').val();
    if (contractId) await verifyOrAcquireLock(contractId);

    // --- antiforgery ---
    const token = $('input[name="__RequestVerificationToken"]').val();
    if (token) $.ajaxSetup({ headers: { 'RequestVerificationToken': token } });


    const brokerageManager = new BrokerageManager({
        isEditMode: true,
        currentBrokerageId: $('#brokerage').data('current'),
        currentDepartment: $('#department').data('current'),
        currentSource: $('#source').data('current'), // NEW: Pass current source value
        modelValues: {
            closerId: $('#closer').data('current'),
            referralPartnerId: $('#referralPartner').data('current'),
            subReferralPartnerId: $('#subReferralPartner').data('current'),
            brokerageStaffId: $('#brokerageStaff').data('current'),
            introducerId: $('#introducer').data('current'),
            subIntroducerId: $('#subIntroducer').data('current'),
            subBrokerageId: $('#subBrokerage').data('current'),
            collaboration: $('#collaboration').data('current'),
            leadGeneratorId: $('#leadGenerator').data('current')
        }
    });

    // --- dropdown seeds ---
    for (const id in DropdownOptions) populateDropdown(id, DropdownOptions[id]);

    function populateDropdown(id, values) {

        const $el = $('#' + id);
        if (!$el.length) return;
        const current = $el.data('current');
        const displayName = id.replace(/([A-Z])/g, ' $1').replace(/^./, s => s.toUpperCase()).trim();
        $el.empty().append(`<option value="">Select ${displayName}</option>`);
        values.forEach(val => {
            const selected = val === current ? 'selected' : '';
            $el.append(`<option value="${val}" ${selected}>${val}</option>`);
        });
    }



    // --- query types (call once) ---
    GetQueryDropDownData();
    function GetQueryDropDownData() {
        const supplier = $('#supplierName').val() || '';
        if (!supplier) return;

        $.ajax({
            url: '/StatusDashboard/GetQueryTypes',
            type: 'GET',
            data: { supplier },
            success: function (options) {
                const $dropdown = $('#queryType');
                const selectedVal = (
                    $dropdown.data('current') ||
                    $dropdown.attr('value') ||
                    $dropdown.val() || ''
                ).trim();

                const uniqueOptions = [...new Set((options || [])
                    .map(o => (o || '').trim())
                    .filter(Boolean))];

                $dropdown.empty().append('<option value="">Select Query Type</option>');
                uniqueOptions.forEach(opt => {
                    const sel = selectedVal && opt.toLowerCase() === selectedVal.toLowerCase() ? 'selected' : '';
                    $dropdown.append(`<option value="${opt}" ${sel}>${opt}</option>`);
                });

                if (selectedVal && !$dropdown.val()) {
                    $dropdown.append(`<option value="${selectedVal}" selected>${selectedVal}</option>`);
                }

                $dropdown.trigger('change');
            }
        });
    }

    // --- sector suppliers (unchanged) ---
    function loadSuppliersBySectorEdit(sectorId) {
        if (!sectorId) return;
        $.get(`/Supplier/GetActiveSuppliersBySector?sectorId=${sectorId}`, function (res) {
            if (res.success && res.Data.length > 0) {
                window.currentSectorSuppliers = res.Data;
            } else {
                window.currentSectorSuppliers = [];
            }
        }).fail(function () {
            window.currentSectorSuppliers = [];
        });
    }

    $('#brokerage').on('change', function () {
        const sectorId = $(this).val();
        loadSuppliersBySectorEdit(sectorId);

        const currentSupplierId = $('#supplierSelect').val();
        if (currentSupplierId && window.currentSectorSuppliers) {
            const ok = window.currentSectorSuppliers.some(s => s.Id == currentSupplierId);
            if (!ok) showToastWarning("Current supplier is not available in the selected sector. Please contact support if you need to change the supplier.");
        }
    });

    const currentSectorId = $('#brokerage').val();
    if (currentSectorId) loadSuppliersBySectorEdit(currentSectorId);

    // --- product -> comms type ---
    $('#productSelect').on('change', function () {
        const commsType = $(this).find('option:selected').data('comms') ?? '';
        const $commsSelect = $('#supplierCommsType');

        $commsSelect.empty().append('<option value="">Select Supplier Comms Type</option>');
        DropdownOptions.supplierCommsType.forEach(v => {
            const selected = v === $commsSelect.data('current') || v === commsType ? 'selected' : '';
            $commsSelect.append(`<option value="${v}" ${selected}>${v}</option>`);
        });

        if (commsType) {
            $commsSelect.val(commsType).addClass('highlight-temp');
            setTimeout(() => $commsSelect.removeClass('highlight-temp'), 1000);
        }

        $commsSelect.prop('disabled', true);
    });

    // --- validation styling ---
    $(document).on('input change', '.form-control, .form-select', function () {
        if (this.checkValidity()) $(this).removeClass('is-invalid');
    });

    // --- setup contract status dropdown ---
    populateDropdown("contractStatus", AccountDropdownOptions.statusDashboardContractStatus, $('#contractStatus').data('current'));

    // --- contract type helpers ---
    function isGasContract() {
        const ct = ($('#contractType').val() || '').toString().trim().toLowerCase();
        return ct === 'gas' || ct === '1';
    }

    // --- duplicate check (MPAN/MPRN) ---
    const $supLabel = $('#supplyNumberLabel');
    const $supInput = $('#supplyNumber'); // if you still use #mprn/#mpan, adjust accordingly
    const gas = isGasContract();

    if (gas) {
        $supLabel.text('MPRN');
        $supInput.attr({ pattern: '^\\d{6,10}$', title: 'Please enter 6–10 digit MPRN or N/A', maxlength: '10' });
    } else {
        $supLabel.text('MPAN');
        $supInput.attr({ pattern: '^\\d{13}(\\d{8})?$', title: 'Please enter valid MPAN' }).removeAttr('maxlength');
    }

    $supInput.on('input', function () {
        const val = $(this).val().trim();
        if (gas) {
            if (/^\d{6,10}$/.test(val)) {
                $('#supplyLoader').show();
                $.get(`/Gas/CheckDuplicateMprn?mprn=${val}`, function (res) {
                    $('#supplyLoader').hide();
                    if (res.success && res.Data) {
                        const d = res.Data;
                        $('#duplicateMprnModalEdit tbody').html(`
              <tr>
                <td>${d.Agent || 'N/A'}</td>
                <td>${d.BusinessName}</td>
                <td>${d.CustomerName}</td>
                <td>${d.InputDate}</td>
                <td>${d.PreSalesStatus}</td>
                <td>${d.Duration}</td>
              </tr>`);
                        $('#duplicateMprnModalEdit').modal('show');
                    }
                }).fail(() => $('#supplyLoader').hide());
            }
        } else {
            if (/^\d{13}(\d{8})?$/.test(val)) {
                $('#supplyLoader').show();
                $.get(`/Electric/CheckDuplicateMpan?mpan=${val}`, function (res) {
                    $('#supplyLoader').hide();
                    if (res.success && res.Data) {
                        const d = res.Data;
                        $('#duplicateMpanModalEdit tbody').html(`
              <tr>
                <td>${d.Agent || 'N/A'}</td>
                <td>${d.BusinessName}</td>
                <td>${d.CustomerName}</td>
                <td>${d.InputDate}</td>
                <td>${d.PreSalesStatus}</td>
                <td>${d.Duration}</td>
              </tr>`);
                        $('#duplicateMpanModalEdit').modal('show');
                    }
                }).fail(() => $('#supplyLoader').hide());
            }
        }
    });

    // --- uplift validation: route once, no duplicates ---
    async function validateUpliftAgainstSupplierLimit($uplift, $supplier, eid) {
        if (isGasContract()) {
            if (typeof validateUpliftAgainstSupplierLimitGas === 'function') {
                return await validateUpliftAgainstSupplierLimitGas($uplift, $supplier, eid);
            }
        } else {
            if (typeof validateUpliftAgainstSupplierLimitElectric === 'function') {
                return await validateUpliftAgainstSupplierLimitElectric($uplift, $supplier, eid);
            }
        }
        console.warn('Validator function missing for contract type');
        return true;
    }

    (function bindUpliftValidation() {
        const $uplift = $('#uplift');
        const $supplier = $('#supplierSelect');

        $uplift.on('blur', async function () {
            const eid = $('#eid').val();
            await validateUpliftAgainstSupplierLimit($uplift, $supplier, eid);
        });

        $supplier.on('change', async function () {
            if ($(this).is(':disabled')) return;
            const eid = $('#eid').val();
            await validateUpliftAgainstSupplierLimit($uplift, $supplier, eid);
        });
    })();

    // --- submit (use generic validator; choose the right POST url if needed) ---
    $('#editGasForm').on('submit', function (e) {
        e.preventDefault();

        const dto = {
            EId: $('#eid').val(),
            ContractType: $('#contractType').val(),
            Source: $('#source').val(),
            BusinessName: $('#businessName').val(),
            CustomerName: $('#customerName').val(),
            ContractId: $('#contractId').val(),
            PostCode: $('#postCode').val(),
            PhoneNumber1: $('#phoneNumber1').val(),
            EmailAddress: $('#emailAddress').val(),
            SalesType: $('#salesType').val(),
            SalesTypeStatus: $('#salesTypeStatus').val(),
            Uplift: $('#uplift').val(),
            InputEAC: $('#inputEAC').val(),
            InitialStartDate: $('#initialStartDate').val(),
            CED: $('#ced').val(),
            CEDCOT: $('#cedCot').val(),
            ContractStatus: $('#contractStatus').val(),
            QueryType: $('#queryType').val(),
            FollowUpDate: $('#followUpDate').val(),
            ProspectedSaleDate: $('#prospectedSaleCED').val(),
            ProspectedSaleNotes: $('#prospectedSaleNotes').val(),
            ContractNotes: $('#contractNotes').val(),
            ObjectionDate: $('#objectionDate').val(),
            ReappliedDate: $('#reappliedDate').val()
        };

        const $btn = $(this).find('button[type="submit"]');
        $btn.prop('disabled', true).text('Updating...');

        $.ajax({
            url: '/StatusDashboard/UpdatePostSalesContract',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(dto),
            success: function (res) {
                if (res.success) {
                    showToastSuccess(res.message);
                    loadLogs(); // refresh logs section
                } else {
                    showToastError(res.message);
                }
            },
            error: function () {
                showToastError("Unexpected error occurred.");
            },
            complete: function () {
                $btn.prop('disabled', false).text('Update Contract');
            }
        });
    });

    // --- logs (route by type) ---
    function loadLogs() {
        const eid = $('#eid').val();
        const type = $('#contractType').val();
        const $logContainer = $('#postSalesContractLogs'); // consider renaming generic
        $logContainer.html('<div class="text-muted">Loading logs...</div>');
        const url = `/StatusDashboard/GetLogsPostSalesContract?id=${eid}&type=${type}`;

        $.get(url, function (res) {
            if (res.success && res.Data.length > 0) {
                const html = res.Data.map(log => `
                  <div class="log-entry">
                    <div class="log-date">${log.ActionDate}</div>
                    <p class="log-field"><span class="log-label">User:</span> <span class="log-value">${log.Username}</span></p>
                    <p class="log-field"><span class="log-label">Status:</span> <span class="log-value">${log.ContractStatus}</span></p>
                    <p class="log-field"><span class="log-label">CED:</span> <span class="log-value"><strong>${log.CED}</strong></span></p>
                    <p class="log-field"><span class="log-label">CED COT:</span> <span class="log-value"><strong>${log.COT}</strong></span></p>
                    <p class="log-field"><span class="log-label">Objection Date:</span> <span class="log-value"><strong>${log.ObjectionDate}</strong></span></p>
                    <p class="log-field"><span class="log-label">Query Type:</span> <span class="log-value"><strong>${log.QueryType}</strong></span></p>
                    <p class="log-field"><span class="log-label">Contract Notes:</span> <span class="log-value"><strong>${log.ContractNotes}</strong></span></p>
                  </div>
        `).join('');
                $logContainer.html(html);
            } else {
                $logContainer.html('<div class="text-muted">No logs available.</div>');
            }
        }).fail(() => {
            $logContainer.html('<div class="text-danger">Failed to load logs.</div>');
        });
    }

    $('#contractStatus').on('change', function () {
        const selectedStatus = $(this).val()?.toLowerCase() || '';
        const today = new Date().toISOString().split('T')[0];

        if (selectedStatus.includes('objection')) {
            $('#objectionDate').val(today);
        }

        if (selectedStatus.includes('reapplied') && selectedStatus.includes('awaiting')) {
            $('#reappliedDate').val(today);
        }
    });

    // When Objection Date changes manually
    $('#objectionDate').on('change', function () {
        const dateVal = $(this).val();
        if (dateVal) {
            const $status = $('#contractStatus');
            const hasObjectionOption = $status.find('option').filter(function () {
                const text = $(this).text().toLowerCase();
                const val = $(this).val().toLowerCase();
                return text.includes('objection') || val.includes('objection');
            }).first();

            if (hasObjectionOption.length) {
                $status.val(hasObjectionOption.val());
            }
        }
    });


    $('#reappliedDate').on('change', function () {
        const dateVal = $(this).val();
        if (!dateVal) return;

        const $status = $('#contractStatus');
        const selectedText = ($status.find('option:selected').text() || '').toLowerCase();

        // agar already "reapplied - awaiting confirmation" nahi hai
        if (!(selectedText.includes('reapplied') && selectedText.includes('awaiting'))) {
            const $opt = $status.find('option').filter(function () {
                const val = ($(this).val() || '').toLowerCase();
                const txt = ($(this).text() || '').toLowerCase();
                return val.includes('reapplied') && txt.includes('awaiting');
            }).first();

            if ($opt.length) {
                $status.val($opt.val()).trigger('change');
            }
        }
    });

    $(document).on("click", ".send-email", function () {
        selectedType = ($(this).data("type") || "").toLowerCase();
        var emails = '';
        if (selectedType === "agent") {
            emails = $(this).data("emails");
        } else {
            const emailListValue = $('#emailList').val();
            emails = emailListValue ? emailListValue.split(',').map(e => e.trim()) : [];
        }
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

    loadLogs();

    // --- unlock on leave ---
    setupContractUnlocking();

});



/**
 * Sets up generic contract unlocking when user leaves the page
 */
function setupContractUnlocking() {
    const eid = $('#eid').val();
    if (!eid) return;

    window.addEventListener('beforeunload', function () {
        unlockContractBeacon(eid);
    });

    // Additional cleanup on page hide (iOS Safari support)
    window.addEventListener('pagehide', function () {
        unlockContractBeacon(eid);
    });
}

function unlockContractBeacon(eid) {
    if (!eid) return;

    const url = '/PreSales/UnlockContract';
    const data = new FormData();
    data.append('eid', eid);

    const sent = navigator.sendBeacon(url, data);
}

/**
 * Verify if we have the lock, or acquire it if needed
 * @param {string} contractId - The contract ID to verify/acquire lock for
 */
async function verifyOrAcquireLock(contractId) {
    try {
        // First, check if we already have the lock
        const statusResponse = await $.ajax({
            url: '/PreSales/CheckLockStatus',
            type: 'POST',
            data: { eid: contractId }
        });

        if (statusResponse.success && statusResponse.Data?.hasLock) {
            // We already have the lock - all good!
            console.log('Lock verification successful - we have the lock');
            showToastSuccess('Contract lock verified successfully');
            return;
        }

        // We don't have the lock - try to acquire it
        console.log('No lock found - attempting to acquire lock');
        const lockResponse = await $.ajax({
            url: '/PreSales/LockContract',
            type: 'POST',
            data: { eid: contractId }
        });

        if (lockResponse.success) {
            console.log('Lock acquired successfully');
            showToastSuccess('Contract lock acquired successfully');
        } else {
            console.warn('Failed to acquire lock:', lockResponse.message);
            showToastError('Warning: ' + lockResponse.message + ' You may not be able to save changes.');
        }

    } catch (error) {
        console.error('Error verifying/acquiring lock:', error);
        showToastError('Warning: Unable to verify contract lock. You may not be able to save changes.');
    }
}
