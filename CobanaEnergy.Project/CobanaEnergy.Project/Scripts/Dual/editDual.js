$(document).ready(async function () {
    if (!$('#editDualForm').length) return;
    
    // Always verify/acquire lock on page load
    const contractId = $('#eid').val();
    if (contractId) {
        await verifyOrAcquireLock(contractId);
    }
    
    // $('#electricCommsType, #gasCommsType').prop('disabled', true);

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

    // Populate dropdowns with current values
    populateDropdowns();

    function populateDropdowns() {
        // Populate Electric Sales Type
        const $electricSalesType = $('#electricSalesType');
        if ($electricSalesType.length) {
            const currentValue = $electricSalesType.data('current');
            if (currentValue) {
                $electricSalesType.val(currentValue);
            }
        }

        // Populate Electric Sales Type Status
        const $electricSalesTypeStatus = $('#electricSalesTypeStatus');
        if ($electricSalesTypeStatus.length) {
            const currentValue = $electricSalesTypeStatus.data('current');
            if (currentValue) {
                $electricSalesTypeStatus.val(currentValue);
            }
        }

        // Populate Gas Sales Type
        const $gasSalesType = $('#gasSalesType');
        if ($gasSalesType.length) {
            const currentValue = $gasSalesType.data('current');
            if (currentValue) {
                $gasSalesType.val(currentValue);
            }
        }

        // Populate Gas Sales Type Status
        const $gasSalesTypeStatus = $('#gasSalesTypeStatus');
        if ($gasSalesTypeStatus.length) {
            const currentValue = $gasSalesTypeStatus.data('current');
            if (currentValue) {
                $gasSalesTypeStatus.val(currentValue);
            }
        }

        // Populate Electric Comms Type
        const $electricCommsType = $('#electricCommsType');
        if ($electricCommsType.length) {
            const currentValue = $electricCommsType.data('current');
            if (currentValue) {
                $electricCommsType.val(currentValue);
            }
        }

        // Populate Gas Comms Type
        const $gasCommsType = $('#gasCommsType');
        if ($gasCommsType.length) {
            const currentValue = $gasCommsType.data('current');
            if (currentValue) {
                $gasCommsType.val(currentValue);
            }
        }
    }

    // Note: Dynamic field population is now handled automatically by BrokerageManager
    // when modelValues are provided in edit mode

    function populateDropdown(id, values, currentValue) {
        const $select = $('#' + id);
        const displayName = id.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase()).trim();
        $select.empty().append(`<option value="">Select ${displayName}</option>`);
        values.forEach(v => {
            const selected = v === currentValue ? 'selected' : '';
            $select.append(`<option value="${v}" ${selected}>${v}</option>`);
        });
    }

    populateDropdown("department", DropdownOptions.department, $('#department').data('current'));
    populateDropdown("source", DropdownOptions.source, $('#source').data('current'));
    populateDropdown("electricSalesType", DropdownOptions.salesType, $('#electricSalesType').data('current'));
    populateDropdown("electricSalesStatus", DropdownOptions.salesTypeStatus, $('#electricSalesStatus').data('current'));
    populateDropdown("electricCommsType", DropdownOptions.supplierCommsType, $('#electricCommsType').data('current'));
    populateDropdown("electricPreSalesStatus", DropdownOptions.preSalesStatus, $('#electricPreSalesStatus').data('current'));

    populateDropdown("gasSalesType", DropdownOptions.salesType, $('#gasSalesType').data('current'));
    populateDropdown("gasSalesStatus", DropdownOptions.salesTypeStatus, $('#gasSalesStatus').data('current'));
    populateDropdown("gasCommsType", DropdownOptions.supplierCommsType, $('#gasCommsType').data('current'));
    populateDropdown("gasPreSalesStatus", DropdownOptions.preSalesStatus, $('#gasPreSalesStatus').data('current'));

    const electricCurrentSupplier = $('#electricSupplier').data('current');
    const electricCurrentProduct = $('#electricProduct').data('current');

    const gasCurrentSupplier = $('#gasSupplier').data('current');
    const gasCurrentProduct = $('#gasProduct').data('current');

    handleCommsType('#electricProduct', '#electricCommsType');
    handleCommsType('#gasProduct', '#gasCommsType');

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
        
        // Check if current suppliers are still valid for the new sector
        const currentElectricSupplierId = $('#electricSupplier').val();
        const currentGasSupplierId = $('#gasSupplier').val();
        
        if (window.currentSectorSuppliers) {
            if (currentElectricSupplierId) {
                const isElectricSupplierValid = window.currentSectorSuppliers.some(s => s.Id == currentElectricSupplierId);
                if (!isElectricSupplierValid) {
                    console.warn('Current electric supplier is not available in the selected sector');
                    showToastWarning("Current electric supplier is not available in the selected sector. Please contact support if you need to change the supplier.");
                }
            }
            
            if (currentGasSupplierId) {
                const isGasSupplierValid = window.currentSectorSuppliers.some(s => s.Id == currentGasSupplierId);
                if (!isGasSupplierValid) {
                    console.warn('Current gas supplier is not available in the selected sector');
                    showToastWarning("Current gas supplier is not available in the selected sector. Please contact support if you need to change the supplier.");
                }
            }
        }
    });

    // Load suppliers for the current sector on page load
    const currentSectorId = $('#brokerage').val();
    if (currentSectorId) {
        loadSuppliersBySectorEdit(currentSectorId);
    }

    $('#electricProduct').on('change', function () {
        handleCommsType('#electricProduct', '#electricCommsType');
    });

    $('#gasProduct').on('change', function () {
        handleCommsType('#gasProduct', '#gasCommsType');
    });

    function loadSnapshotProducts(supplierId, targetId, selectedId, products) {
        const $target = $(targetId);
        const $comms = (targetId === '#electricProduct') ? $('#electricCommsType') : $('#gasCommsType');

        if (!products || !products.length) {
            $target.empty().append('<option value="">Select Product</option>').prop('disabled', false);
            $comms.empty().append('<option value="">Select Supplier Comms Type</option>').prop('disabled', false);
            return;
        }

        $target.prop('disabled', false).empty().append('<option value="">Select Product</option>');
        products.forEach(p => {
            const comms = p.SupplierCommsType ?? '';
            $target.append(`<option value="${p.Id}" data-comms="${comms}" ${p.Id == selectedId ? 'selected' : ''}>${p.ProductName}</option>`);
        });

        handleCommsType(targetId, $comms);
    }

    function handleCommsType(productSelector, commsSelector) {
        const selectedOption = $(productSelector).find('option:selected');
        const commsType = selectedOption.data('comms') ?? '';
        const $comms = $(commsSelector);
        const current = $comms.data('current');

        $comms.empty().append('<option value="">Select Supplier Comms Type</option>');

        DropdownOptions.supplierCommsType.forEach(v => {
            const selected = commsType ? v === commsType : v === current ? 'selected' : '';
            $comms.append(`<option value="${v}" ${selected}>${v}</option>`);
        });

        if (commsType) {
            $comms.val(commsType).addClass('highlight-temp');
            setTimeout(() => {
                $comms.removeClass('highlight-temp');
            }, 1000);
        }

        $comms.prop('disabled', true);
    }

    $('.form-control, .form-select').on('input change', function () {
        if (this.checkValidity()) {
            $(this).removeClass('is-invalid');
        }
    });

    $('#editDualForm').on('submit', async function (e) {
        e.preventDefault();
        let hasInvalid = false;

        $(this).find('.form-control, .form-select').each(function () {
            if (!this.checkValidity()) {
                $(this).addClass('is-invalid');
                hasInvalid = true;
            }
        });

        if (hasInvalid) {
            const $first = $(this).find(':invalid').first();
            $first.focus();
            showToastWarning("Please fix the highlighted fields.");
            return;
        }

        const model = getDualModel();

        const $btn = $(this).find('button[type="submit"]');
        $btn.prop('disabled', true).text('Updating...');

        const $electricUplift = $('#electricUplift');
        const $gasUplift = $('#gasUplift');

        const isElectricValid = await validateUpliftAgainstSupplierLimitElectric($('#electricUplift'), $('#electricSupplier'), $('#eid').val());
        if (!isElectricValid) {
            $electricUplift.focus();
            $btn.prop('disabled', false).text('Update Dual Contract');
            return;
        }

        const isGasValid = await validateUpliftAgainstSupplierLimitGas($('#gasUplift'), $('#gasSupplier'), $('#eid').val());
        if (!isGasValid) {
            $gasUplift.focus();
            $btn.prop('disabled', false).text('Update Dual Contract');
            return;
        }

        $.ajax({
            url: '/Dual/EditDual',
            method: 'POST',
            data: JSON.stringify(model),
            contentType: 'application/json',
            success: function (res) {
                if (res.success) {
                    showToastSuccess("Dual contract updated successfully!");
                    const d = res.Data;

                    $('#department').val(d.Department);
                    $('#agent').val(d.Agent);
                    $('#source').val(d.Source);
                    $('#introducer').val(d.Introducer);
                    $('#subIntroducer').val(d.SubIntroducer);
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
                    $('#inputDateElectric').val(d.ElectricInputDate);
                    $('#inputDateGas').val(d.GasInputDate);
                    $('#emProcessor').val(d.EMProcessor);
                    $('#sortCode').val(d.SortCode);
                    $('#accountNumber').val(d.AccountNumber);

                    $('#mpan').val(d.MPAN);
                    $('#mprn').val(d.MPRN);

                    $('#electricCurrentSupplier').val(d.ElectricCurrentSupplier);
                    $('#electricSalesType').val(d.ElectricSalesType);
                    $('#electricSalesStatus').val(d.ElectricSalesTypeStatus);
                    $('#electricDuration').val(d.ElectricDuration);
                    $('#electricUplift').val(d.ElectricUplift);
                    $('#electricInputEAC').val(d.ElectricInputEAC);
                    $('#electricStandingCharge').val(d.ElectricStandingCharge);
                    $('#dayRate').val(d.ElectricDayRate);
                    $('#nightRate').val(d.ElectricNightRate);
                    $('#eveWeekendRate').val(d.ElectricEveWeekendRate);
                    $('#electricOtherRate').val(d.ElectricOtherRate);
                    $('#electricSupplier').val(d.ElectricSupplierId);
                    $('#electricProduct').val(d.ElectricProductId);
                    $('#electricCommsType').val(d.ElectricSupplierCommsType);
                    $('#electricPreSalesStatus').val(d.ElectricPreSalesStatus);
                    $('#electricPreSalesFollowUpDate').val(d.ElectricPreSalesFollowUpDate);

                    $('#gasCurrentSupplier').val(d.GasCurrentSupplier);
                    $('#gasSalesType').val(d.GasSalesType);
                    $('#gasSalesStatus').val(d.GasSalesTypeStatus);
                    $('#gasDuration').val(d.GasDuration);
                    $('#gasUplift').val(d.GasUplift);
                    // $('#gasInputAQ').val(d.GasInputAQ);
                    $('#gasStandingCharge').val(d.GasStandingCharge);
                    $('#gasUnitRate').val(d.GasUnitRate);
                    $('#gasSupplier').val(d.GasSupplierId);
                    $('#gasProduct').val(d.GasProductId);
                    $('#gasCommsType').val(d.GasSupplierCommsType);
                    $('#gasPreSalesStatus').val(d.GasPreSalesStatus);
                    $('#gasPreSalesFollowUpDate').val(d.GasPreSalesFollowUpDate);

                    $('#contractChecked').prop('checked', d.ContractChecked);
                    $('#contractAudited').prop('checked', d.ContractAudited);
                    $('#terminated').prop('checked', d.Terminated);
                    $('#topLine').val(d.TopLine);
                    $('#electricInitialStartDate').val(d.ElectricInitialStartDate);
                    $('#gasInitialStartDate').val(d.GasInitialStartDate);
                    $('#gasInputEAC').val(d.GasInputEAC);
                    $('#gasOtherRate').val(d.GasOtherRate);
                    $('#contractNotes').val(d.ContractNotes);

                    $('#emProcessor').val(d.EMProcessor);

                    loadLogs(d.EId);
                }
                else {
                    showToastError(res.message);
                }
            },
            error: function () {
                showToastError("Unexpected error occurred.");
            },
            complete: function () {
                $btn.prop('disabled', false).text('Update Dual Contract');
            }
        });
    });

    function getDualModel() {
        return {
            EId: $('#eid').val(),
            Department: $('#department').val(),
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
            ElectricInputDate: $('#inputDateElectric').val(),
            GasInputDate: $('#inputDateGas').val(),
            ElectricInitialStartDate: $('#electricInitialStartDate').val(),
            GasInitialStartDate: $('#gasInitialStartDate').val(),
            ElectricDuration: $('#electricDuration').val(),
            GasDuration: $('#gasDuration').val(),
            ElectricUplift: $('#electricUplift').val(),
            GasUplift: $('#gasUplift').val(),
            ElectricInputEAC: $('#electricInputEAC').val(),
            GasInputEAC: $('#gasInputEAC').val(),
            DayRate: $('#dayRate').val(),
            NightRate: $('#nightRate').val(),
            EveWeekendRate: $('#eveWeekendRate').val(),
            ElectricOtherRate: $('#electricOtherRate').val(),
            GasUnitRate: $('#gasUnitRate').val(),
            GasOtherRate: $('#gasOtherRate').val(),
            ElectricStandingCharge: $('#electricStandingCharge').val(),
            GasStandingCharge: $('#gasStandingCharge').val(),
            SortCode: $('#sortCode').val(),
            AccountNumber: $('#accountNumber').val(),
            MPAN: $('#mpan').val(),
            MPRN: $('#mprn').val(),
            ElectricSupplierId: $('#electricSupplier').val(),
            ElectricProductId: $('#electricProduct').val(),
            GasSupplierId: $('#gasSupplier').val(),
            GasProductId: $('#gasProduct').val(),
            ElectricSalesType: $('#electricSalesType').val(),
            ElectricSalesTypeStatus: $('#electricSalesStatus').val(),
            ElectricSupplierCommsType: $('#electricCommsType').val(),
            ElectricPreSalesStatus: $('#electricPreSalesStatus').val(),
            ElectricPreSalesFollowUpDate: $('#electricPreSalesFollowUpDate').val() || null,
            ElectricCurrentSupplier: $('#electricCurrentSupplier').val(),
            GasCurrentSupplier: $('#gasCurrentSupplier').val(),
            ElectricDayRate: $('#dayRate').val(),
            ElectricNightRate: $('#nightRate').val(),
            ElectricEveWeekendRate: $('#eveWeekendRate').val(),
            TopLine: $('#topLine').val(),
            GasSalesType: $('#gasSalesType').val(),
            GasSalesTypeStatus: $('#gasSalesStatus').val(),
            GasSupplierCommsType: $('#gasCommsType').val(),
            GasPreSalesStatus: $('#gasPreSalesStatus').val(),
            GasPreSalesFollowUpDate: $('#gasPreSalesFollowUpDate').val() || null,
            ContractChecked: $('#contractChecked').is(':checked'),
            ContractAudited: $('#contractAudited').is(':checked'),
            Terminated: $('#terminated').is(':checked'),
            EMProcessor: $('#emProcessor').val(),
            ContractNotes: $('#contractNotes').val(),
            
            // Brokerage Details
            BrokerageId: $('#brokerage').val() || null,
            OfgemId: $('#ofgemId').val() || null,
            
            // Dynamic Department-based fields
            Source: $('#source').val(),
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
    }
    function loadLogs(eid) {

        const $logContainer = $('#dualLogsContainer');
        $logContainer.html('<div class="text-muted">Loading logs...</div>');

        $.get(`/Dual/GetLogsForDualContract?eid=${eid}`, function (res) {
            if (res.success && res.Data.length > 0) {
                const html = res.Data.map(log => `
                    <div class="log-entry">
                        <div class="log-date">${log.ActionDate}</div>
                        <p class="log-field"><span class="log-label">User:</span> <span class="log-value">${log.Username}</span></p>
                        <p class="log-field"><span class="log-label">Status:</span> <span class="log-value">${log.PreSalesStatusType}</span></p>
                        <p class="log-field"><span class="log-label">Message:</span> <span class="log-value"><strong>${log.Message}</strong></span></p>
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

    const eid = $('#eid').val();
    if (eid) loadLogs(eid);

    $('#mpan').on('input', function () {
        const val = $(this).val().trim();
        if (/^\d{13}$/.test(val)) {
            $('#mpanLoader').show();
            $.get(`/Electric/CheckDuplicateMpan?mpan=${val}`, function (res) {
                $('#mpanLoader').hide();
                if (res.success && res.Data) {
                    const d = res.Data;
                    $('#duplicateDualMpanModalEdit tbody').html(`<tr><td>${d.Agent}</td><td>${d.BusinessName}</td><td>${d.CustomerName}</td><td>${d.InputDate}</td><td>${d.PreSalesStatus}</td><td>${d.Duration}</td></tr>`);
                    $('#duplicateDualMpanModalEdit').modal('show');
                }
            }).fail(() => $('#mpanLoader').hide());
        }
    });

    $('#mprn').on('input', function () {
        const val = $(this).val().trim();
        if (/^\d{6,10}$/.test(val)) {
            $('#mprnLoader').show();
            $.get(`/Gas/CheckDuplicateMprn?mprn=${val}`, function (res) {
                $('#mprnLoader').hide();
                if (res.success && res.Data) {
                    const d = res.Data;
                    $('#duplicateDualMprnModalEdit tbody').html(`<tr><td>${d.Agent}</td><td>${d.BusinessName}</td><td>${d.CustomerName}</td><td>${d.InputDate}</td><td>${d.PreSalesStatus}</td><td>${d.Duration}</td></tr>`);
                    $('#duplicateDualMprnModalEdit').modal('show');
                }
            }).fail(() => $('#mprnLoader').hide());
        }
    });

    $('#accountNumber').on('input', function () {
        const acc = $(this).val().trim();
        if (/^\d{8}$/.test(acc)) {
            $('#accountLoader').show();
            $.get(`/CheckDuplicateAccount/CheckDuplicateAccountUnified?account=${acc}`, function (res) {
                $('#accountLoader').hide();
                if (res.success && res.Data?.length > 0) {
                    const tbody = $('#duplicateAccountModalDualEdit tbody');
                    tbody.empty();
                    res.Data.forEach(r => {
                        tbody.append(`<tr><td>${r.Agent}</td><td>${r.BusinessName}</td><td>${r.CustomerName}</td><td>${r.InputDate}</td><td>${r.PreSalesStatus}</td><td>${r.Duration}</td><td>${r.SortCode}</td><td>${r.AccountNumber}</td></tr>`);
                    });
                    $('#duplicateAccountModalDualEdit').modal('show');
                }
            }).fail(() => $('#accountLoader').hide());
        }
    });

    $('#electricUplift').on('blur', async function () {
        await validateUpliftAgainstSupplierLimitElectric($('#electricUplift'), $('#electricSupplier'), $('#eid').val());
    });

    $('#gasUplift').on('blur', async function () {
        await validateUpliftAgainstSupplierLimitGas($('#gasUplift'), $('#gasSupplier'), $('#eid').val());
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
