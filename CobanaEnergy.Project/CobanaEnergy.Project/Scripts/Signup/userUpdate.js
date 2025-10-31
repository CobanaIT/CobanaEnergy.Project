$(document).ready(function () {
    $('#updateUserForm').on('submit', function (e) {
        e.preventDefault();

        const formData = $(this).serialize();
        const submitButton = $(this).find('button[type="submit"]');
        const originalText = submitButton.text();

        // Show loading state
        submitButton.prop('disabled', true).text('Updating...');

        $.ajax({
            url: '/Account/UpdateUser',
            type: 'POST',
            data: formData,
            success: function (response) {
                if (response.success) {
                    showToastSuccess(response.message || 'User updated successfully!');
                    if (response.data && response.data.redirectUrl) {
                        setTimeout(() => {
                            window.location.href = response.data.redirectUrl;
                        }, 1500);
                    }
                } else {
                    showToastError(response.message || 'Failed to update user.');
                }
            },
            error: function (xhr, status, error) {
                console.error('User update error:', error);
                showToastError('An unexpected error occurred. Please try again.');
            },
            complete: function () {
                // Reset button state
                submitButton.prop('disabled', false).text(originalText);
            }
        });
    });

    // Input validation for extension number
    $('input[name="ExtensionNumber"]').on('input', function () {
        const value = $(this).val();
        const numericValue = value.replace(/[^0-9]/g, '');
        if (value !== numericValue) {
            $(this).val(numericValue);
        }
    });
});
