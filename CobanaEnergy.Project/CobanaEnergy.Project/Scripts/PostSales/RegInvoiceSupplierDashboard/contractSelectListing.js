$(function () {
    var table = $('#contractsTable').DataTable({
        responsive: true,
        paging: true,
        ordering: true,
        searching: true,
        order: [],
        autoWidth: false,
        columnDefs: [
            { orderable: false, targets: 0 }
        ]
    });

    // Enable column resizing
    enableColumnResizing('#contractsTable');

    $(document).on('change', 'input[name="selectedContracts"], #checkAll', function () {
        let selected = $('input[name="selectedContracts"]:checked').length > 0;
        $('#continueBtn').prop('disabled', !selected);
    });

    $('#checkAll').on('change', function () {
        $('input[name="selectedContracts"]').prop('checked', this.checked).trigger('change');
    });

    $(document).on('change', 'input[name="selectedContracts"]', function () {
        if (!this.checked) $('#checkAll').prop('checked', false);
    });

    $('#contractSelectionForm').on('submit', function (e) {
        e.preventDefault();

        const $continueBtn = $('#continueBtn');
        $continueBtn.prop('disabled', true);

        const selectedIds = $('input[name="selectedContracts"]:checked')
            .map(function () { return $(this).val(); }).get();

        if (selectedIds.length === 0) {
            showToastError("Please select at least one contract.");
            $continueBtn.prop('disabled', false);
            return;
        }

        $.ajax({
            url: '/RegInvoiceSupplierDashboard/ConfirmRegSelectionSupplier',
            type: 'POST',
            data: $(this).serialize(),
            success: function (res) {
                if (res.success && res.Data.redirectUrl) {
                    window.location.href = res.Data.redirectUrl;
                } else {
                    showToastError(res.message || "Error moving to next step.");
                    $continueBtn.prop('disabled', false);
                }
            },
            error: function (xhr, status, error) {
                console.log("XHR:", xhr);
                console.log("Status:", status);
                console.log("Error:", error);
                showToastError("Failed to continue! ");
                $continueBtn.prop('disabled', false);
            }
        });
    });
});
