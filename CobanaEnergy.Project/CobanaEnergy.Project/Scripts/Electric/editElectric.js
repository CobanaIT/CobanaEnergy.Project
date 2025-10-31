$(document).ready(async function () {

    if (!$('#editElectricForm').length) return;
    
    // Always verify/acquire lock on page load
    const contractId = $('#eid').val();
    if (contractId) {
        await verifyOrAcquireLock(contractId);
    }
    
    //$('#productSelect, #supplierCommsType').prop('disabled', true); 

    const token = $('input[name="__RequestVerificationToken"]').val();
    if (token) {
        $.ajaxSetup({ headers: { 'RequestVerificationToken': token } });
    }

    // Initialize BrokerageManager for edit mode
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

    for (const id in DropdownOptions) {
        populateDropdown(id, DropdownOptions[id]);
    }


    function populateDropdown(id, values) {
        const $el = $('#' + id);
        if (!$el.length) return;

        const current = $el.data('current');
        const displayName = id.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase()).trim();
        $el.empty().append(`<option value="">Select ${displayName}</option>`);
        values.forEach(val => {
            const selected = val === current ? 'selected' : '';
            $el.append(`<option value="${val}" ${selected}>${val}</option>`);
        });
    }

    // Note: Dynamic field population is now handled automatically by BrokerageManager
    // when modelValues are provided in edit mode

    // Function to load suppliers based on selected sector (for edit mode)
    function loadSuppliersBySectorEdit(sectorId) {
        if (!sectorId) {
            return;
        }

        $.get(`/Supplier/GetActiveSuppliersBySector?sectorId=${sectorId}`, function (res) {
            if (res.success && res.Data.length > 0) {
                // Store the suppliers for potential future use
                window.currentSectorSuppliers = res.Data;
                console.log('Suppliers loaded for sector:', res.Data.length);
            } else {
                window.currentSectorSuppliers = [];
                console.log('No suppliers found for sector');
            }
        }).fail(function() {
            window.currentSectorSuppliers = [];
            console.log('Error loading suppliers for sector');
        });
    }

    // Listen for brokerage (sector) selection changes in edit mode
    $('#brokerage').on('change', function() {
        const sectorId = $(this).val();
        loadSuppliersBySectorEdit(sectorId);
        
        // Check if current supplier is still valid for the new sector
        const currentSupplierId = $('#supplierSelect').val();
        if (currentSupplierId && window.currentSectorSuppliers) {
            const isCurrentSupplierValid = window.currentSectorSuppliers.some(s => s.Id == currentSupplierId);
            if (!isCurrentSupplierValid) {
                console.warn('Current supplier is not available in the selected sector');
                // Optionally show a warning to the user
                showToastWarning("Current supplier is not available in the selected sector. Please contact support if you need to change the supplier.");
            }
        }
    });

    // Load suppliers for the current sector on page load
    const currentSectorId = $('#brokerage').val();
    if (currentSectorId) {
        loadSuppliersBySectorEdit(currentSectorId);
    }

    $('#productSelect').on('change', function () {
        const selectedOption = $(this).find('option:selected');
        const commsType = selectedOption.data('comms') ?? '';
        const $commsSelect = $('#supplierCommsType');

        $commsSelect.empty().append('<option value="">Select Supplier Comms Type</option>');
        DropdownOptions.supplierCommsType.forEach(v => {
            const selected = v === commsType ? 'selected' : '';
            $commsSelect.append(`<option value="${v}" ${selected}>${v}</option>`);
        });

        if (commsType) {
            $commsSelect.val(commsType).addClass('highlight-temp');
            setTimeout(() => {
                $commsSelect.removeClass('highlight-temp');
            }, 1000);
        }

        $commsSelect.prop('disabled', true);
    });

    $('#editElectricForm').on('submit', async function (e) {
        e.preventDefault();

        let hasInvalid = false;

        $(this).find('.form-control, .form-select').each(function () {
            $(this).removeClass('is-invalid');
            if (!this.checkValidity()) {
                $(this).addClass('is-invalid');
                hasInvalid = true;
            }
        });

        if (hasInvalid) {
            const $first = $(this).find(':invalid').first();
            $first.focus();
            showToastWarning("Please fill all required fields.");
            return;
        }

        const model = {
            EId: $('#eid').val(),
            Department: $('#department').val(),
            Source: $('#source').val(),
            SalesType: $('#salesType').val(),
            SalesTypeStatus: $('#salesTypeStatus').val(),
            BusinessName: $('#businessName').val(),
            CustomerName: $('#customerName').val(),
            BusinessDoorNumber: $('#businessDoorNumber').val(),
            BusinessHouseName: $('#businessHouseName').val(),
            BusinessStreet: $('#businessStreet').val(),
            BusinessTown: $('#businessTown').val(),
            BusinessCounty: $('#businessCounty').val(),
            PostCode: $('#postCode').val(),
            PhoneNumber1: $('#phoneNumber1').val(),
            PhoneNumber2: $('#phoneNumber2').val(),
            EmailAddress: $('#emailAddress').val(),
            InitialStartDate: $('#initialStartDate').val(),
            Duration: $('#duration').val(),
            Uplift: $('#uplift').val(),
            InputEAC: $('#inputEAC').val(),
            DayRate: $('#dayRate').val(),
            NightRate: $('#nightRate').val(),
            EveWeekendRate: $('#eveWeekendRate').val(),
            OtherRate: $('#otherRate').val(),
            StandingCharge: $('#standingCharge').val(),
            SortCode: $('#sortCode').val(),
            AccountNumber: $('#accountNumber').val(),
            TopLine: $('#topLine').val(),
            MPAN: $('#mpan').val(),
            CurrentSupplier: $('#currentSupplier').val(),
            SupplierId: $('#supplierSelect').val(),
            ProductId: $('#productSelect').val(),
            SupplierCommsType: $('#supplierCommsType').val(),
            PreSalesStatus: $('#preSalesStatus').val(),
            PreSalesFollowUpDate: $('#preSalesFollowUpDate').val() || null,
            EMProcessor: $('#emProcessor').val(),
            ContractChecked: $('#contractChecked').is(':checked'),
            ContractAudited: $('#contractAudited').is(':checked'),
            Terminated: $('#terminated').is(':checked'),
            ContractNotes: $('#contractNotes').val(),
            
            // Brokerage Details
            BrokerageId: $('#brokerage').val() || null,
            OfgemId: $('#ofgemId').val() || null,
            
            // Dynamic Department-based fields
            CloserId: $('#closer').val() === '-1' ? 0 : ($('#closer').val() || null),
            ReferralPartnerId: $('#referralPartner').val() === '-1' ? 0 : ($('#referralPartner').val() || null),
            SubReferralPartnerId: $('#subReferralPartner').val() === '-1' ? 0 : ($('#subReferralPartner').val() || null),
            BrokerageStaffId: $('#brokerageStaff').val() === '-1' ? 0 : ($('#brokerageStaff').val() || null),
            IntroducerId: $('#introducer').val() === '-1' ? 0 : ($('#introducer').val() || null),
            SubIntroducerId: $('#subIntroducer').val() === '-1' ? 0 : ($('#subIntroducer').val() || null),
            SubBrokerageId: $('#subBrokerage').val() === '-1' ? 0 : ($('#subBrokerage').val() || null),
            Collaboration: $('#collaboration').val() === '-1' ? 'N/A' : ($('#collaboration').val() || null),
            LeadGeneratorId: $('#leadGenerator').val() === '-1' ? 0 : ($('#leadGenerator').val() || null)
        };

        const $btn = $(this).find('button[type="submit"]');
        $btn.prop('disabled', true).text('Updating...');

        const $uplift = $('#uplift');
        const $supplier = $('#supplierSelect');
        const eid = $('#eid').val();
        const isValid = await validateUpliftAgainstSupplierLimitElectric($uplift, $supplier, eid);
        if (!isValid) {
            $uplift.focus();
            $btn.prop('disabled', false).text('Update Contract');
            return;
        }

        $.ajax({
            url: '/Electric/ElectricEdit',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            success: function (res) {
                if (res.success) {
                    showToastSuccess("Electric contract updated successfully!");

                    const d = res.Data;

                    $('#department').val(d.Department);
                    $('#source').val(d.Source);
                    $('#salesType').val(d.SalesType);
                    $('#salesTypeStatus').val(d.SalesTypeStatus);
                    $('#businessName').val(d.BusinessName);
                    $('#customerName').val(d.CustomerName);
                    $('#businessDoorNumber').val(d.BusinessDoorNumber);
                    $('#businessHouseName').val(d.BusinessHouseName);
                    $('#businessStreet').val(d.BusinessStreet);
                    $('#businessTown').val(d.BusinessTown);
                    $('#businessCounty').val(d.BusinessCounty);
                    $('#postCode').val(d.PostCode);
                    $('#phoneNumber1').val(d.PhoneNumber1);
                    $('#phoneNumber2').val(d.PhoneNumber2);
                    $('#emailAddress').val(d.EmailAddress);
                    $('#initialStartDate').val(d.InitialStartDate);
                    $('#duration').val(d.Duration);
                    $('#uplift').val(d.Uplift);
                    $('#inputEAC').val(d.InputEAC);
                    $('#dayRate').val(d.DayRate);
                    $('#nightRate').val(d.NightRate);
                    $('#eveWeekendRate').val(d.EveWeekendRate);
                    $('#otherRate').val(d.OtherRate);
                    $('#standingCharge').val(d.StandingCharge);
                    $('#topLine').val(d.TopLine);
                    $('#mpan').val(d.MPAN);
                    $('#currentSupplier').val(d.CurrentSupplier);
                    $('#supplierId').val(d.SupplierId);
                    $('#productId').val(d.ProductId);
                    $('#supplierCommsType').val(d.SupplierCommsType);
                    $('#preSalesStatus').val(d.PreSalesStatus);
                    $('#preSalesFollowUpDate').val(d.PreSalesFollowUpDate);
                    $('#contractNotes').val(d.ContractNotes);
                    $('#sortCode').val(d.SortCode);
                    $('#accountNumber').val(d.AccountNumber);
                    $('#inputDate').val(d.InputDate);
                    $('#emProcessor').val(d.EMProcessor);
                    
                    // Set new fields
                    $('#brokerage').val(d.BrokerageId);
                    $('#ofgemId').val(d.OfgemId);
                    $('#closer').val(d.CloserId);
                    $('#referralPartner').val(d.ReferralPartnerId);
                    $('#subReferralPartner').val(d.SubReferralPartnerId);
                    $('#brokerageStaff').val(d.BrokerageStaffId);
                    $('#introducer').val(d.IntroducerId);
                    $('#subIntroducer').val(d.SubIntroducerId);
                    $('#subBrokerage').val(d.SubBrokerageId);
                    $('#collaboration').val(d.Collaboration);
                    $('#leadGenerator').val(d.LeadGeneratorId);

                    $('#contractChecked').prop('checked', d.ContractChecked);
                    $('#contractAudited').prop('checked', d.ContractAudited);
                    $('#terminated').prop('checked', d.Terminated);

                    loadLogs(); // reload
                    //setTimeout(() => location.reload());

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

    $(document).on('input change', '.form-control, .form-select', function () {
        if (this.checkValidity()) {
            $(this).removeClass('is-invalid');
        }
    });

    function loadLogs() {
        const eid = $('#eid').val();
        const $logContainer = $('#electricLogsContainer');

        $logContainer.html('<div class="text-muted">Loading logs...</div>');

        $.get(`/Electric/GetLogsForElectricContract?eid=${eid}`, function (res) {
            if (res.success && res.Data.length > 0) {
                const html = res.Data.map(log => `
                   <div class="log-entry">
                      <div class="log-date">${log.ActionDate}</div>

                      <p class="log-field">
                        <span class="log-label">User:</span>
                        <span class="log-value">${log.Username}</span>
                      </p>

                      <p class="log-field">
                        <span class="log-label">Status:</span>
                        <span class="log-value">${log.PreSalesStatusType}</span>
                      </p>

                      <p class="log-field">
                        <span class="log-label">Message:</span>
                        <span class="log-value"><strong>${log.Message}</strong></span>
                      </p>
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

    loadLogs();

    $('#mpan').on('input', function () {
        const mpan = $(this).val().trim();

        if (/^\d{13}$/.test(mpan)) {
            $('#mpanLoader').show();

            $.get(`/Electric/CheckDuplicateMpan?mpan=${mpan}`, function (res) {
                $('#mpanLoader').hide();

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
                    </tr>
                `);
                    $('#duplicateMpanModalEdit').modal('show');
                }
            }).fail(function () {
                $('#mpanLoader').hide();
                showToastError("Error checking MPAN.");
            });
        }
    });

    $('#accountNumber').on('input', function () {
        const acc = $(this).val().trim();

        if (/^\d{8}$/.test(acc)) {
            $('#accountLoader').show();

            $.get(`/CheckDuplicateAccount/CheckDuplicateAccountUnified?account=${acc}`, function (res) {
                $('#accountLoader').hide();

                if (res.success && res.Data?.length > 0) {
                    const tbody = $('#duplicateAccountModalEdit tbody');
                    tbody.empty();

                    res.Data.forEach(r => {
                        tbody.append(`
                        <tr>
                            <td>${r.Agent || 'N/A'}</td>
                            <td>${r.BusinessName}</td>
                            <td>${r.CustomerName}</td>
                            <td>${r.InputDate}</td>
                            <td>${r.PreSalesStatus}</td>
                            <td>${r.Duration}</td>
                            <td>${r.SortCode}</td>
                            <td>${r.AccountNumber}</td>
                        </tr>
                    `);
                    });

                    $('#duplicateAccountModalEdit').modal('show');
                }
            }).fail(function () {
                $('#accountLoader').hide();
                showToastError("Error checking account number.");
            });
        }
    });

    $('#uplift').on('blur', async function () {
        const eid = $('#eid').val();
        await validateUpliftAgainstSupplierLimitElectric($('#uplift'), $('#supplierSelect'), eid);
    });

    $('#supplierSelect').on('change', async function () {
        const eid = $('#eid').val();
        await validateUpliftAgainstSupplierLimitElectric($('#uplift'), $('#supplierSelect'), eid);
    });

    // Generic contract unlock functionality
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

