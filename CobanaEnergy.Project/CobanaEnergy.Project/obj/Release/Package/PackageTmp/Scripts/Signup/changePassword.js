$(document).ready(function () {

    $('#userDropdown').select2({
        placeholder: "Select User",
        width: '100%',
        dropdownAutoWidth: true,
        dropdownParent: $('.form-container')
    });



    $('#changePasswordForm').submit(function (e) {
        e.preventDefault();

        var $btn = $(this).find('button[type="submit"]');
        $btn.html('Updating...').prop('disabled', true);

        $.ajax({
            url: $(this).attr('action'),
            type: 'POST',
            data: $(this).serialize(),
            success: function (response) {
                if (response.success) {
                    showToastSuccess(response.message);
                    $('#userDropdown').val(null).trigger('change');
                    $('#changePasswordForm')[0].reset();
                } else {
                    showToastError(response.message);
                }
            },
            error: function () {
                showToastError("An unexpected error occurred.");
            },
            complete: function () {
                $btn.html('Update Password').prop('disabled', false);
            }
        });
    });
});
