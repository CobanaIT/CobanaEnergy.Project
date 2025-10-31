$(document).ready(function () {
    $(document).on('shown.bs.modal', '#invoiceUploadModal', function () {
        setTimeout(function () {
            if ($('#SupplierId').data('select2')) {
                $('#SupplierId').select2('destroy');
            }
            $('#SupplierId').select2({
                dropdownParent: $('#invoiceUploadModal'),
                width: '100%',
                placeholder: "Select Supplier",
                allowClear: true
            });
        }, 10);
    });

    $(document).on('click', '#browseFileBtn', function () {
        $('#InvoiceFile').trigger('click');
    });

    $(document).on('change', '#InvoiceFile, #SupplierId', function () {
        validateUploadReady();
    });

    $(document).on('click', '#removeSelectedFile', function () {
        $('#InvoiceFile').val('');
        resetFileUI();
        validateUploadReady();
    });

    function resetFileUI() {
        $('#fileStatus').addClass('d-none');
        $('#selectedFileName').text('');
        $('#selectedFileSize').text('');
    }

    function validateUploadReady() {
        const supplierId = $('#SupplierId').val();
        const file = $('#InvoiceFile')[0].files[0];

        if (file) {
            $('#selectedFileName').text(file.name);
            $('#selectedFileSize').text(`(${(file.size / 1024).toFixed(2)} KB)`);
            $('#fileStatus').removeClass('d-none');
        } else {
            resetFileUI();
        }

        if (file && supplierId && supplierId !== "" && supplierId !== "0") {
            $('#uploadBtn').prop('disabled', false);
        } else {
            $('#uploadBtn').prop('disabled', true);
        }
    }

    $(document).on('submit', '#invoiceUploadForm', function (e) {
        e.preventDefault();
        let formData = new FormData(this);
        let $uploadBtn = $('#uploadBtn');

        $uploadBtn.prop('disabled', true).text('Uploading...');

        $.ajax({
            url: $(this).attr('action'),
            type: 'POST',
            data: formData,
            contentType: false,
            processData: false,
            success: function (res) {
                $uploadBtn.prop('disabled', false).text('Upload');
                if (res.success) {
                    window.location.href = res.Data.redirectUrl;

                } else {
                    showToastError(res.message);
                    resetFileUI();
                    validateUploadReady();
                }
            },
            error: function () {
                $uploadBtn.prop('disabled', false).text('Upload');
                showToastError("Failed to upload file. Please try again.");
                resetFileUI();
                validateUploadReady();
            }
        });
    });

    $(document).on('hidden.bs.modal', '#invoiceUploadModal', function () {
        resetFileUI();
        $('#InvoiceFile').val('');
        $('#SupplierId').val('').trigger('change');
        $('#uploadBtn').prop('disabled', true);
    });
});
