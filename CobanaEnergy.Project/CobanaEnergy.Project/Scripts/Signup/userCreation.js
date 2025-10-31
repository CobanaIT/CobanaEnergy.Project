$(document).ready(function () {

    $('select[multiple]').select2({
        placeholder: "Select Role(s)",
        allowClear: true,
        width: '100%',
        dropdownAutoWidth: true,
        dropdownParent: $('.form-container')
    });


    $('#createUserForm').submit(function (e) {
        e.preventDefault();

        var $form = $(this);
        var $submitBtn = $form.find('button[type="submit"]');
        var originalBtnText = $submitBtn.html();
        var formData = $form.serialize();

        $submitBtn.prop('disabled', true).html('Creating...');


        $.ajax({
            url: $(this).attr('action'),
            type: 'POST',
            data: formData,
            success: function (response) {
                if (response.success) {
                    showToastSuccess('User created successfully!');
                    $form[0].reset();
                    $('select[multiple]').val(null).trigger('change');
                    // window.location.href = response.redirectUrl; 
                } else {

                    showToastError('Failed to create user: ' + response.message);
                }
            },
            error: function (response) {

                showToastError('Error while creating user: ' + response.message);
            },
            complete: function () {
                $submitBtn.prop('disabled', false).html(originalBtnText);
            }
        });
    });

});