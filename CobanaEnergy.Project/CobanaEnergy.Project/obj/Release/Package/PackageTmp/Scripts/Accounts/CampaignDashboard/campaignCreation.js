$(document).ready(function () {

    $('#createCampaignForm').submit(function (e) {
        e.preventDefault();

        var $form = $(this);
        var $submitBtn = $form.find('button[type="submit"]');
        var originalBtnText = $submitBtn.html();
        var formData = $form.serialize();

        $submitBtn.prop('disabled', true).html('Creating...');

        $.ajax({
            url: $form.attr('action'),
            type: 'POST',
            data: formData,
            success: function (response) {
                if (response.success) {
                    showToastSuccess('Campaign created successfully!');
                    $form[0].reset();
                } else {
                    showToastError('Failed to create campaign: ' + response.message);
                }
            },
            error: function (response) {
                showToastError('Error while creating campaign: ' + response.message);
            },
            complete: function () {
                $submitBtn.prop('disabled', false).html(originalBtnText);
            }
        });
    });



    $.get('/Supplier/GetActiveSuppliersForDropdown', function (res) {
        const $supplier = $('#supplierSelect');
        $supplier.empty().append('<option value="">Select Supplier</option>');

        if (res.success && res.Data.length > 0) {
            res.Data.forEach(s => {
                $supplier.append(`<option value="${s.Id}">${s.Name}</option>`);
            });
        } else {
            $supplier.append('<option disabled>No suppliers found</option>');
        }
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

    $(document).on('change', '#StartDate, #EndDate', function () {
        const start = $('#StartDate').val();
        const end = $('#EndDate').val();

        if (start) {
            $('#EndDate').attr('min', start);
        } else {
            $('#EndDate').removeAttr('min');
        }

        if (end) {
            $('#StartDate').attr('max', end);
        } else {
            $('#StartDate').removeAttr('max');
        }
    });

});