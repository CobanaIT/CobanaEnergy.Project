$(document).ready(function () {
    $('#loginForm').submit(function (e) {
        e.preventDefault();
        const $btn = $(this).find('button[type="submit"]');
        $btn.prop('disabled', true).html('Logging in...');

        $.ajax({
            url: $(this).attr('action'),
            type: 'POST',
            data: $(this).serialize(),
            success: function (response) {
                if (response.success) {
                    //showToastSuccess(response.message);
                    window.location.href = response.Data.redirectUrl;
                    //if (response.data && response.Data.redirectUrl) {
                    //    setTimeout(() => {
                    //        window.location.href = response.Data.redirectUrl;
                    //    }, 1200);
                    //}
                } else {
                    showToastError(response.message);
                }
            },
            error: function () {
                showToastError('An unexpected error occurred.');
            },
            complete: function () {
                $btn.prop('disabled', false).html('Login');
            }
        });
    });
});
