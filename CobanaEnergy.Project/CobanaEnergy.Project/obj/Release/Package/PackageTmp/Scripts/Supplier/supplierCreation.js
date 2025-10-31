$(document).ready(function () {

    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
    }

    function populateSupplierCommsDropdown() {
        const options = DropdownOptions.supplierCommsType || [];

        $('.product-comms-type').each(function () {
            if ($(this).children('option').length <= 1) {
                options.forEach(val => {
                    $(this).append(`<option value="${val}">${val}</option>`);
                });
            }
        });
    }

    addContact();
    addProduct();
    addUplift();

    //$('.add-uplift').click(addUplift);
    $('.add-contact').click(addContact);
    $('.add-product').click(addProduct);
    $('.add-uplift').click(function () {
        addUplift();
    });

    $('#supplierForm').submit(function (e) {
        e.preventDefault();

        const $saveButton = $(this).find('button[type="submit"]');
        const originalText = $saveButton.html();

        $saveButton.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span>Saving...');

        const uplifts = [];
        $('#upliftContainer .uplift-row').each(function () {
            uplifts.push({
                FuelType: $(this).find('.uplift-fuel').val(),
                Uplift: $(this).find('.uplift-value').val(),
                StartDate: $(this).find('.uplift-start').val(),
                EndDate: $(this).find('.uplift-end').val()
            });
        });

        const contacts = [];
        $('#contactContainer .contact-row').each(function () {
            contacts.push({
                ContactName: $(this).find('.contact-name').val(),
                Role: $(this).find('.contact-role').val(),
                PhoneNumber: $(this).find('.contact-phone').val(),
                Email: $(this).find('.contact-email').val(),
                Notes: $(this).find('.contact-notes').val()
            });
        });

        const products = [];
        $('#productContainer .product-row').each(function () {
            products.push({
                ProductName: $(this).find('.product-name').val(),
                StartDate: $(this).find('.product-start').val(),
                EndDate: $(this).find('.product-end').val(),
                Commission: $(this).find('.product-commission').val(),
                SupplierCommsType: $(this).find('.product-comms-type').val()
            });
        });

        const data = {
            Name: $('#supplierName').val(),
            Link: $('#supplierLink').val(),
            Contacts: contacts,
            Products: products,
            Uplifts: uplifts
        };

        const formToken = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '/Supplier/SupplierCreation',
            type: 'POST',
            data: JSON.stringify(data),
            contentType: 'application/json',
            headers: {
                'RequestVerificationToken': formToken
            },
            success: function (response) {
                if (response.success) {
                    showToastSuccess("Supplier created successfully!");
                    $('#supplierForm')[0].reset();
                    $('#contactContainer').empty();
                    $('#productContainer').empty();
                    $('#upliftContainer').empty();
                    //addContact();
                    //addProduct();
                    //addUplift(); 
                    setTimeout(function () {
                        window.location.href = response.Data.redirectUrl;
                    }, 1000);
                } else {
                    showToastError(response.message);
                }
            },
            error: function () {
                showToastError("An unexpected error occurred.");
            },
            complete: function () {
                $saveButton.prop('disabled', false).html(originalText);
            }
        });
    });

    function addUplift(preselectedFuel = "") {
        const upliftRow = `
    <div class="uplift-row row gx-2 mb-3">
        <div class="col-md-2">
            <select class="form-control uplift-fuel" required>
                <option value="" disabled ${preselectedFuel ? "" : "selected"}>Select Fuel</option>
                <option value="Electric" ${preselectedFuel === "Electric" ? "selected" : ""}>Electric</option>
                <option value="Gas" ${preselectedFuel === "Gas" ? "selected" : ""}>Gas</option>
            </select>
        </div>
        <div class="col-md-2">
            <input type="text" class="form-control uplift-value" placeholder="Uplift" required pattern="^[0-9]*[.]?[0-9]+$" title="Enter decimal value">
        </div>
        <div class="col-md-3">
            <input type="datetime-local" class="form-control uplift-start" required>
        </div>
        <div class="col-md-3">
            <input type="datetime-local" class="form-control uplift-end" required>
        </div>
        <div class="col-md-1">
            <button type="button" class="btn btn-danger btn-sm remove-row">X</button>
        </div>
    </div>`;
        $('#upliftContainer').append(upliftRow);
    }
    function addContact() {
        const contact = `
        <div class="contact-row row gx-2 mb-3">
            <div class="col-md-2"><input type="text" class="form-control contact-name" placeholder="Contact Name" required></div>
            <div class="col-md-2"><input type="text" class="form-control contact-role" placeholder="Role"></div>
            <div class="col-md-2"><input type="text" class="form-control contact-phone" placeholder="Phone" maxlength="11" inputmode="numeric" pattern="^[0-9]{11}$" title="Enter up to 11 digits"></div>
            <div class="col-md-2"><input type="email" class="form-control contact-email" placeholder="Email"></div>
            <div class="col-md-3">
            <textarea class="form-control contact-notes" placeholder="Notes" rows="1" style="resize: vertical;"></textarea>
            </div>
            <div class="col-md-1"><button type="button" class="btn btn-danger btn-sm remove-row">X</button></div>
        </div>`;
        $('#contactContainer').append(contact);
    }
    function addProduct() {
        const product = `
        <div class="product-row row gx-2 gy-2 mb-3">
            <div class="col-md-3"><input type="text" class="form-control product-name" placeholder="Product Name" required></div>
            <div class="col-md-2">
                <select class="form-control product-comms-type" required>
                    <option value="" disabled selected>Select Comms</option>
                </select>
            </div>
            <div class="col-md-2"><input type="date" class="form-control product-start" placeholder="Start Date"  required></div>
            <div class="col-md-2"><input type="date" class="form-control product-end" placeholder="End Date" disabled required></div>
            <div class="col-md-2"><input type="text" class="form-control product-commission" placeholder="Commission"  required></div>
            <div class="col-md-1"><button type="button" class="btn btn-danger btn-sm remove-row">X</button></div>
        </div>`;

        $('#productContainer').append(product);

        populateSupplierCommsDropdown();
    }

    $(document).on('click', '.remove-row', function () {
        $(this).closest('.row').slideUp(200, function () {
            $(this).remove();
        });
    });

    $(document).on('change', '.product-start', function () {
        const row = $(this).closest('.product-row');
        const startDate = row.find('.product-start').val();
        const endDateInput = row.find('.product-end');

        if (startDate) {
            endDateInput.prop('disabled', false);
            endDateInput.attr('min', startDate);
        } else {
            endDateInput.prop('disabled', true);
            endDateInput.val('');
            endDateInput.removeAttr('min');
        }
    });

    $(document).on('input', '.contact-phone', function () {
        this.value = this.value.replace(/[^0-9]/g, '').slice(0, 11);
    });

    $(document).on('change', '.uplift-start', function () {
        const row = $(this).closest('.uplift-row');
        const startDate = row.find('.uplift-start').val();
        const endDateInput = row.find('.uplift-end');

        if (startDate) {
            endDateInput.prop('disabled', false);
            endDateInput.attr('min', startDate);
        } else {
            endDateInput.prop('disabled', true);
            endDateInput.val('');
            endDateInput.removeAttr('min');
        }
    });

    $(document).on('change', '.uplift-end', function () {
        const row = $(this).closest('.uplift-row');
        const start = row.find('.uplift-start').val();
        const end = $(this).val();

        if (start && end && new Date(end) <= new Date(start)) {
            showToastWarning("End Date must be greater than Start Date for uplift.");
            $(this).val('');
        }
    });

    $(document).on('change', '.uplift-fuel', function () {
        const selectedFuel = $(this).val();
        const now = new Date();
        const row = $(this).closest('.uplift-row');

        if (!selectedFuel) return;

        let hasConflict = false;

        $('#upliftContainer .uplift-row').each(function () {
            const $fuel = $(this).find('.uplift-fuel');
            const $end = $(this).find('.uplift-end');
            const isSameRow = $(this)[0] === row[0];

            const fuel = $fuel.val();
            const end = $end.val();

            if (
                !isSameRow &&
                fuel === selectedFuel &&
                (!end || new Date(end) > now)
            ) {
                hasConflict = true;
                return false;
            }
        });

        if (hasConflict) {
            showToastWarning(`An active or incomplete ${selectedFuel} uplift already exists.`);
            $(this).val('');
        }
    });

    function hasActiveUplift(fuelType) {
        const now = new Date();
        let isActive = false;

        $('#upliftContainer .uplift-row').each(function () {
            const fuel = $(this).find('.uplift-fuel').val();
            const end = $(this).find('.uplift-end').val();

            if (fuel === fuelType && end && new Date(end) > now) {
                isActive = true;
                return false;
            }
        });

        return isActive;
    }

});
