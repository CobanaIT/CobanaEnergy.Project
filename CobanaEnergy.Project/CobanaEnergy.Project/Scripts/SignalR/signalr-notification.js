$(function () {
    const campaignHub = $.connection.notificationHub;

    // Client-side functions
    campaignHub.client.receiveCampaignNotification = function (message) {
        showNotificationToast(message, 'text-bg-info');
    };

    campaignHub.client.forceLogout = function (message) {
        postLogout(message, true);
    };

    campaignHub.client.updateUserStatus = function (userId, status) {
        const $row = $(`#userTable tbody tr[data-userid="${userId}"]`);
        if ($row.length) {
            const $statusCell = $row.find("td").eq(5); // 5th column is OnlineStatus
            const isOnline = status === "Online";
            const dotClass = isOnline ? 'bg-success' : 'bg-secondary';
            const updatedContent = `
            <span class="d-inline-flex align-items-center">
                <span class="dot ${dotClass} rounded-circle me-1" 
                      style="width: 15px; height: 15px; display: inline-block;"></span>
            </span>`;

            $statusCell
                .html(updatedContent); 
        }
    };

    // Logout handler
    $('#logoutForm').on('submit', function (e) {
        e.preventDefault();
        if (campaignHub && campaignHub.connection && campaignHub.connection.state === $.signalR.connectionState.connected) {
            campaignHub.server.notifyLogout().always(function () {
                campaignHub.connection.stop();
                postLogout();
            });
        } else {
            postLogout();
        }
    });

    // Start SignalR connection
    $.connection.hub.start().done(function () {
        console.log("SignalR connected.");
    }).fail(function (error) {
        console.log("SignalR error: ", error);
    });
});


function postLogout(message, forcelogout = false) {
    if (forcelogout) {
        showToast(message, 'text-bg-danger');
    }
    setTimeout(function () {
        var tokenValue = $('input[name="__RequestVerificationToken"]').val();
        if (!tokenValue) {
            console.warn("CSRF token not found. Aborting logout.");
            return;
        }

        var form = document.createElement("form");
        form.method = "POST";
        form.action = "/Account/Logout";

        var token = document.createElement("input");
        token.name = "__RequestVerificationToken";
        token.value = tokenValue;
        token.type = "hidden";
        form.appendChild(token);

        document.body.appendChild(form);
        form.submit();
    }, 1500);
}

function showNotificationToast(message, typeClass) {
    const toastElement = document.getElementById('notificationToast');
    toastElement.className = 'toast align-items-center border-0 ' + typeClass;
    toastElement.querySelector('.toast-body').innerHTML = message;

    const toast = bootstrap.Toast.getOrCreateInstance(toastElement, {
        autohide: false
    });
    toast.show();
}
