$(document).ready(async function () {

    if (!$('#createElectricForm').length) return;

    const token = $('input[name="__RequestVerificationToken"]').val();
    if (token) {
        $.ajaxSetup({ headers: { 'RequestVerificationToken': token } });
    }

    $('#productSelect, #supplierCommsType').prop('disabled', true);
    function populateDropdown(id, values) {
        const $select = $('#' + id);
        const displayName = id.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase()).trim();
        $select.empty().append(`<option value="">Select ${displayName}</option>`);
        values.forEach(v => $select.append(`<option value="${v}">${v}</option>`));
    }

    populateDropdown("department", DropdownOptions.department);
    populateDropdown("source", DropdownOptions.source);
    populateDropdown("salesType", DropdownOptions.salesType);
    populateDropdown("salesTypeStatus", DropdownOptions.salesTypeStatus);
    populateDropdown("supplierCommsType", DropdownOptions.supplierCommsType);
    populateDropdown("preSalesStatus", DropdownOptions.preSalesStatus);

    // Initialize supplier dropdown as disabled
    const $supplier = $('#supplierSelect');
    $supplier.empty().append('<option value="">Select Supplier</option>').prop('disabled', true);

    // Function to load suppliers based on selected sector
    function loadSuppliersBySector(sectorId) {
        if (!sectorId) {
            $supplier.empty().append('<option value="">Select Supplier</option>').prop('disabled', true);
            return;
        }

        $supplier.prop('disabled', true).empty().append('<option>Loading suppliers...</option>');

        $.get(`/Supplier/GetActiveSuppliersBySector?sectorId=${sectorId}`, function (res) {
            $supplier.empty().append('<option value="">Select Supplier</option>');

            if (res.success && res.Data.length > 0) {
                res.Data.forEach(s => {
                    $supplier.append(`<option value="${s.Id}">${s.Name}</option>`);
                });
                $supplier.prop('disabled', false);
            } else {
                $supplier.append('<option disabled>No Suppliers found</option>');
                $supplier.prop('disabled', true);
            }
        }).fail(function() {
            $supplier.empty().append('<option value="">Select Supplier</option>');
            $supplier.append('<option disabled>Error loading suppliers</option>');
            $supplier.prop('disabled', true);
        });
    }

    // Listen for brokerage (sector) selection changes
    $('#brokerage').on('change', function() {
        const sectorId = $(this).val();
        loadSuppliersBySector(sectorId);
        
        // Reset product and comms dropdowns when sector changes
        $('#productSelect').prop('disabled', true).empty().append('<option value="">Select Product</option>');
        $('#supplierCommsType').prop('disabled', true).empty().append('<option value="">Select supplierCommsType</option>');
    });


    $('#supplierSelect').change(function () {
        const supplierId = $(this).val();
        const $product = $('#productSelect');
        const $comms = $('#supplierCommsType');

        $product.prop('disabled', true).empty().append('<option>Loading...</option>');
        $comms.prop('disabled', true).empty().append('<option>Loading...</option>');

        if (!supplierId) {
            $product.empty().append('<option value="">Select Product</option>').prop('disabled', true);
            $comms.empty().append('<option value="">Select supplierCommsType</option>').prop('disabled', true);
            return;
        }

        $.get(`/Supplier/GetProductsBySupplier?supplierId=${supplierId}`, function (res) {
            $product.empty().append('<option value="">Select Product</option>');
            $comms.empty().append('<option value="">Select supplierCommsType</option>');

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
    });

    $('#productSelect').on('change', function () {
        const selectedOption = $(this).find('option:selected');
        const commsType = selectedOption.data('comms') ?? '';
        const $commsSelect = $('#supplierCommsType');

        $commsSelect.empty().append('<option value="">Select supplierCommsType</option>');
        DropdownOptions.supplierCommsType.forEach(v => {
            $commsSelect.append(`<option value="${v}">${v}</option>`);
        });

        if (commsType) {
            $commsSelect.val(commsType).addClass('highlight-temp');
            setTimeout(() => {
                $commsSelect.removeClass('highlight-temp');
            }, 1000);
        }
        $commsSelect.prop('disabled', true);
    });



    restoreDefaultFields();

    $('#createElectricForm').on('submit', async function (e) {
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
        $btn.prop('disabled', true).text('Submitting...');

        const $uplift = $('#uplift');
        const $supplier = $('#supplierSelect');
        const isValid = await validateUpliftAgainstSupplierLimit($uplift, $supplier, 'Electric');
        if (!isValid) {
            $uplift.focus();
            $btn.prop('disabled', false).text('Create Contract');
            return;
        }

        $.ajax({
            url: '/Electric/CreateElectric',
            method: 'POST',
            data: JSON.stringify(model),
            contentType: 'application/json',
            success: function (res) {
                if (res.success) {
                    showToastSuccess("Contract created successfully!");
                    $('#createElectricForm')[0].reset();
                    restoreDefaultFields();
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
                $btn.prop('disabled', false).text('Create Contract');
            }
        });
    });

    $(document).on('input change', '.form-control, .form-select', function () {
        if (this.checkValidity()) {
            $(this).removeClass('is-invalid');
        }
    });

    $('#mpan').on('input', function () {
        const mpan = $(this).val().trim();

        if (/^\d{13}$/.test(mpan)) {
            $('#mpanLoader').show();

            checkDuplicateMPAN(mpan);
            populateMPANRelationData(mpan);
        }
    });

    $('#accountNumber').on('input', function () {
        const acc = $(this).val().trim();

        if (/^\d{8}$/.test(acc)) {
            $('#accountLoader').show();

            $.get(`/CheckDuplicateAccount/CheckDuplicateAccountUnified?account=${acc}`, function (res) {
                $('#accountLoader').hide();

                if (res.success && res.Data?.length > 0) {
                    const tbody = $('#duplicateAccountModal tbody');
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

                    $('#duplicateAccountModal').modal('show');
                }
            }).fail(function () {
                $('#accountLoader').hide();
                showToastError("Error checking account number.");
            });
        }
    });

    function restoreDefaultFields() {
        const today = new Date().toLocaleDateString('en-GB');
        $('#inputDate').val(today);

        const defaultProcessor = $('#emProcessor').data('default') || 'Presales Team';
        $('#emProcessor').val(defaultProcessor);
    }

    $('#uplift').on('blur', async function () {
        await validateUpliftAgainstSupplierLimit($('#uplift'), $('#supplierSelect'), 'Electric');
    });

    $('#supplierSelect').on('change', async function () {
        await validateUpliftAgainstSupplierLimit($('#uplift'), $('#supplierSelect'), 'Electric');
    });
});
