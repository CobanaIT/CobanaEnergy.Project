$(document).ready(async function () {
    if (!$('#createDualForm').length) return;

    const token = $('input[name="__RequestVerificationToken"]').val();
    if (token) {
        $.ajaxSetup({ headers: { 'RequestVerificationToken': token } });
    }

    $('.form-control, .form-select').on('input change', function () {
        if (this.checkValidity()) {
            $(this).removeClass('is-invalid');
        }
    });

    $('#electricProduct, #electricCommsType, #gasProduct, #gasCommsType').prop('disabled', true);

    function populateDropdown(id, values) {
        const $select = $('#' + id);
        const displayName = id.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase()).trim();
        $select.empty().append(`<option value="">Select ${displayName}</option>`);
        values.forEach(v => $select.append(`<option value="${v}">${v}</option>`));
    }

    populateDropdown("department", DropdownOptions.department);
    populateDropdown("source", DropdownOptions.source);
    populateDropdown("electricSalesType", DropdownOptions.salesType);
    populateDropdown("gasSalesType", DropdownOptions.salesType);
    populateDropdown("electricSalesStatus", DropdownOptions.salesTypeStatus);
    populateDropdown("gasSalesStatus", DropdownOptions.salesTypeStatus);
    populateDropdown("electricCommsType", DropdownOptions.supplierCommsType);
    populateDropdown("gasCommsType", DropdownOptions.supplierCommsType);
    populateDropdown("electricPreSalesStatus", DropdownOptions.preSalesStatus);
    populateDropdown("gasPreSalesStatus", DropdownOptions.preSalesStatus);

    // Initialize supplier dropdowns as disabled
    const $electricSupplier = $('#electricSupplier');
    const $gasSupplier = $('#gasSupplier');
    $electricSupplier.empty().append('<option value="">Select Supplier</option>').prop('disabled', true);
    $gasSupplier.empty().append('<option value="">Select Supplier</option>').prop('disabled', true);

    // Function to load suppliers based on selected sector
    function loadSuppliersBySector(sectorId) {
        if (!sectorId) {
            $electricSupplier.empty().append('<option value="">Select Supplier</option>').prop('disabled', true);
            $gasSupplier.empty().append('<option value="">Select Supplier</option>').prop('disabled', true);
            return;
        }

        $electricSupplier.prop('disabled', true).empty().append('<option>Loading suppliers...</option>');
        $gasSupplier.prop('disabled', true).empty().append('<option>Loading suppliers...</option>');

        $.get(`/Supplier/GetActiveSuppliersBySector?sectorId=${sectorId}`, function (res) {
            $electricSupplier.empty().append('<option value="">Select Supplier</option>');
            $gasSupplier.empty().append('<option value="">Select Supplier</option>');

            if (res.success && res.Data.length > 0) {
                res.Data.forEach(s => {
                    $electricSupplier.append(`<option value="${s.Id}">${s.Name}</option>`);
                    $gasSupplier.append(`<option value="${s.Id}">${s.Name}</option>`);
                });
                $electricSupplier.prop('disabled', false);
                $gasSupplier.prop('disabled', false);
            } else {
                $electricSupplier.append('<option disabled>No Suppliers found</option>');
                $gasSupplier.append('<option disabled>No Suppliers found</option>');
                $electricSupplier.prop('disabled', true);
                $gasSupplier.prop('disabled', true);
            }
        }).fail(function() {
            $electricSupplier.empty().append('<option value="">Select Supplier</option>');
            $gasSupplier.empty().append('<option value="">Select Supplier</option>');
            $electricSupplier.append('<option disabled>Error loading suppliers</option>');
            $gasSupplier.append('<option disabled>Error loading suppliers</option>');
            $electricSupplier.prop('disabled', true);
            $gasSupplier.prop('disabled', true);
        });
    }

    // Listen for brokerage (sector) selection changes
    $('#brokerage').on('change', function() {
        const sectorId = $(this).val();
        loadSuppliersBySector(sectorId);
        
        // Reset product and comms dropdowns when sector changes
        $('#electricProduct').prop('disabled', true).empty().append('<option value="">Select Product</option>');
        $('#gasProduct').prop('disabled', true).empty().append('<option value="">Select Product</option>');
        $('#electricCommsType').prop('disabled', true).empty().append('<option value="">Select Comms Type</option>');
        $('#gasCommsType').prop('disabled', true).empty().append('<option value="">Select Comms Type</option>');
    });

    $('#electricSupplier').change(function () {
        loadProductsAndComms($(this).val(), '#electricProduct', '#electricCommsType');
    });
    $('#gasSupplier').change(function () {
        loadProductsAndComms($(this).val(), '#gasProduct', '#gasCommsType');
    });

    $('#electricProduct').change(function () {
        updateCommsTypeFromProduct('#electricProduct', '#electricCommsType');
    });
    $('#gasProduct').change(function () {
        updateCommsTypeFromProduct('#gasProduct', '#gasCommsType');
    });

    function loadProductsAndComms(supplierId, productSelector, commsSelector) {
        const $product = $(productSelector);
        const $comms = $(commsSelector);

        $product.prop('disabled', true).empty().append('<option>Loading...</option>');
        $comms.prop('disabled', true).empty().append('<option>Loading...</option>');

        if (!supplierId) {
            $product.empty().append('<option value="">Select Product</option>').prop('disabled', true);
            $comms.empty().append('<option value="">Select Comms Type</option>').prop('disabled', true);
            return;
        }

        $.get(`/Supplier/GetProductsBySupplier?supplierId=${supplierId}`, function (res) {
            $product.empty().append('<option value="">Select Product</option>');
            $comms.empty().append('<option value="">Select Comms Type</option>');

            DropdownOptions.supplierCommsType.forEach(v => {
                $comms.append(`<option value="${v}">${v}</option>`);
            });

            if (res.success && res.Data.length > 0) {
                res.Data.forEach(p => {
                    $product.append(`<option value="${p.Id}" data-comms="${p.SupplierCommsType ?? ''}">${p.ProductName}</option>`);
                });
            } else {
                $product.append('<option disabled>No products found</option>');
            }

            $product.prop('disabled', false);
            $comms.prop('disabled', true);
        });
    }

    function updateCommsTypeFromProduct(productSelector, commsSelector) {
        const selectedOption = $(productSelector).find('option:selected');
        const commsType = selectedOption.data('comms') ?? '';
        const $comms = $(commsSelector);

        $comms.empty().append('<option value="">Select Comms Type</option>');
        DropdownOptions.supplierCommsType.forEach(v => {
            $comms.append(`<option value="${v}">${v}</option>`);
        });

        if (commsType) {
            $comms.val(commsType).addClass('highlight-temp');
            setTimeout(() => {
                $comms.removeClass('highlight-temp');
            }, 1000);
        }
        $comms.prop('disabled', true);
    }

    function restoreDefaults() {
        const today = new Date().toLocaleDateString('en-GB');
        $('#inputDate').val(today);
        const defaultProcessor = $('#emProcessor').data('default') || 'Presales Team';
        $('#emProcessor').val(defaultProcessor);
    }

    restoreDefaults();

    $('#createDualForm').on('submit', async function (e) {
        e.preventDefault();

        let hasInvalid = false;
        $(this).find('.form-control, .form-select').each(function () {
            const $field = $(this);
            $field.removeClass('is-invalid');

            if (!this.checkValidity()) {
                $field.addClass('is-invalid');
                hasInvalid = true;
            }
        });

        if (hasInvalid) {
            const $first = $(this).find(':invalid').first();
            $first.focus();
            showToastWarning("Please fill all required fields correctly.");
            return;
        }

        const model = {
            Department: $('#department').val(),
            Source: $('#source').val(),
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
            InputDate: $('#inputDate').val(),
            EMProcessor: $('#emProcessor').val(),
            SortCode: $('#sortCode').val(),
            AccountNumber: $('#accountNumber').val(),

            MPAN: $('#mpan').val(),
            MPRN: $('#mprn').val(),

            ElectricCurrentSupplier: $('#electricCurrentSupplier').val(),
            // ElectricTopLine: $('#electricTopLine').val(),
            ElectricSalesType: $('#electricSalesType').val(),
            ElectricSalesTypeStatus: $('#electricSalesStatus').val(),
            ElectricDuration: $('#electricDuration').val(),
            ElectricUplift: $('#electricUplift').val(),
            ElectricInputEAC: $('#electricInputEAC').val(),
            ElectricStandingCharge: $('#electricStandingCharge').val(),
            ElectricDayRate: $('#dayRate').val(),
            ElectricNightRate: $('#nightRate').val(),
            ElectricEveWeekendRate: $('#eveWeekendRate').val(),
            ElectricOtherRate: $('#electricOtherRate').val(),
            ElectricSupplierId: $('#electricSupplier').val(),
            ElectricProductId: $('#electricProduct').val(),
            ElectricSupplierCommsType: $('#electricCommsType').val(),
            ElectricPreSalesStatus: $('#electricPreSalesStatus').val(),
            ElectricPreSalesFollowUpDate: $('#electricPreSalesFollowUpDate').val() || null,

            GasCurrentSupplier: $('#gasCurrentSupplier').val(),
            GasSalesType: $('#gasSalesType').val(),
            GasSalesTypeStatus: $('#gasSalesStatus').val(),
            GasDuration: $('#gasDuration').val(),
            GasUplift: $('#gasUplift').val(),
            GasInputAQ: $('#gasInputAQ').val(),
            GasStandingCharge: $('#gasStandingCharge').val(),
            GasUnitRate: $('#gasUnitRate').val(),
            GasSupplierId: $('#gasSupplier').val(),
            GasProductId: $('#gasProduct').val(),
            GasSupplierCommsType: $('#gasCommsType').val(),
            GasPreSalesStatus: $('#gasPreSalesStatus').val(),
            GasPreSalesFollowUpDate: $('#gasPreSalesFollowUpDate').val() || null,

            ContractChecked: $('#contractChecked').is(':checked'),
            ContractAudited: $('#contractAudited').is(':checked'),
            Terminated: $('#terminated').is(':checked'),
            TopLine: $('#topLine').val(),
            ElectricInitialStartDate: $('#electricInitialStartDate').val(),
            GasInitialStartDate: $('#gasInitialStartDate').val(),
            GasInputEAC: $('#gasInputEAC').val(),
            GasOtherRate: $('#gasOtherRate').val(),
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
        $btn.prop('disabled', true).text('Submitting...');

        const $electricUplift = $('#electricUplift');
        const $electricSupplier = $('#electricSupplier');
        const $gasUplift = $('#gasUplift');
        const $gasSupplier = $('#gasSupplier');

        const isElectricValid = await validateUpliftAgainstSupplierLimit($electricUplift, $electricSupplier, 'Electric');
        if (!isElectricValid) {
            $electricUplift.focus();
            $btn.prop('disabled', false).text('Create Dual Contract');
            return;
        }

        const isGasValid = await validateUpliftAgainstSupplierLimit($gasUplift, $gasSupplier, 'Gas');
        if (!isGasValid) {
            $gasUplift.focus();
            $btn.prop('disabled', false).text('Create Dual Contract');
            return;
        }

        $.ajax({
            url: '/Dual/CreateDual',
            method: 'POST',
            data: JSON.stringify(model),
            contentType: 'application/json',
            success: function (res) {
                if (res.success) {
                    showToastSuccess("Dual contract created successfully!");
                    $('#createDualForm')[0].reset();
                    restoreDefaults();
                    setTimeout(function () {
                        window.location.href = res.Data.redirectUrl;
                    }, 1000);
                } else {
                    showToastError(res.message);
                }
            },
            error: function () {
                showToastError("An unexpected error occurred.");
            },
            complete: function () {
                $btn.prop('disabled', false).text('Create Dual Contract');
            }
        });
    });

    $('#mpan').on('input', function () {
        const mpan = $(this).val().trim();
        if (/^\d{13}$/.test(mpan)) {
            $('#mpanLoader').show();
            checkDuplicateMPAN(mpan);
            populateMPANRelationData(mpan);
        }
    });

    $('#mprn').on('input', function () {
        const mprn = $(this).val().trim();
        if (/^\d{6,10}$/.test(mprn)) {
            $('#mprnLoader').show();
            checkDuplicateMPRN(mprn);
        }
    });

    $('#accountNumber').on('input', function () {
        const acc = $(this).val().trim();
        if (/^\d{8}$/.test(acc)) {
            $('#accountLoader').show();
            $.get(`/CheckDuplicateAccount/CheckDuplicateAccountUnified?account=${acc}`, function (res) {
                $('#accountLoader').hide();
                if (res.success && res.Data?.length > 0) {
                    const tbody = $('#duplicateAccountModalDual tbody');
                    tbody.empty();
                    res.Data.forEach(r => {
                        tbody.append(`
                            <tr><td>${r.Agent || 'N/A'}</td><td>${r.BusinessName}</td><td>${r.CustomerName}</td><td>${r.InputDate}</td><td>${r.PreSalesStatus}</td><td>${r.Duration}</td><td>${r.SortCode}</td><td>${r.AccountNumber}</td></tr>
                        `);
                    });
                    $('#duplicateAccountModalDual').modal('show');
                }
            }).fail(function () {
                $('#accountLoader').hide();
                showToastError("Error checking account number.");
            });
        }
    });

    $('#electricUplift').on('blur', async function () {
        await validateUpliftAgainstSupplierLimit($('#electricUplift'), $('#electricSupplier'), 'Electric');
    });
    $('#electricSupplier').on('change', async function () {
        await validateUpliftAgainstSupplierLimit($('#electricUplift'), $('#electricSupplier'), 'Electric');
    });

    $('#gasUplift').on('blur', async function () {
        await validateUpliftAgainstSupplierLimit($('#gasUplift'), $('#gasSupplier'), 'Gas');
    });
    $('#gasSupplier').on('change', async function () {
        await validateUpliftAgainstSupplierLimit($('#gasUplift'), $('#gasSupplier'), 'Gas');
    });

});
