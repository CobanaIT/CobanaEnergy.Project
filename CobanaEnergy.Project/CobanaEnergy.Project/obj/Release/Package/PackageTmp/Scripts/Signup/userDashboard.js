$(document).ready(function () {


    $('select[multiple]').select2({
        placeholder: "Select Role(s)",
        allowClear: true,
        width: '100%',
        dropdownAutoWidth: true,
        dropdownParent: $('.form-container')
    });

    var table = $('#userTable').DataTable({
        responsive: true,
        paging: true,
        ordering: true,
        searching: true,
        order: [],
        autoWidth: false,
        dom: 'lfrtip',
        columnDefs: [
            { orderable: false, targets: 0 }
        ]
    });

    // Enable column resizing
    enableColumnResizing('#userTable');

    var excelButton = new $.fn.dataTable.Buttons(table, {
        buttons: [
            {
                extend: 'excelHtml5',
                text: 'Export',
                title: 'UserName',
                filename: function () {
                    const today = new Date();
                    return 'UserList_' + today.toISOString().split('T')[0];
                },
                exportOptions: {
                    columns: ':visible:not(:first-child)'
                }
            }
        ]
    });

    table.buttons(excelButton, false);

    $('#exportExcelBtn').on('click', function () {
        table.button('.buttons-excel').trigger();
    });


    function loadUsers() {

        $.ajax({
            url: '/Account/GetUserList',
            type: 'POST',
            data: {
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (res) {
                table.clear();

                if (res.success) {
                    const userlist = res.Data || [];

                    userlist.forEach(contract => {

                        // Roles as badges
                        const roles = Array.isArray(contract.Roles) ? contract.Roles : [];
                        const roleLabels = roles
                            .map(role => `<span class="badge bg-primary me-1">${role}</span>`)
                            .join('');

                        // Status badge
                        const statusBadgeClass = contract.Status === "Active" ? "bg-primary" : "bg-danger";
                        const statusLabel = `<span class="badge ${statusBadgeClass} me-1">${contract.Status}</span>`;

                        // Online status dot
                        const isOnline = contract.OnlineStatus === "Online";
                        const onlineDot = `
                            <span class="d-inline-flex align-items-center">
                                <span class="dot ${isOnline ? 'bg-success' : 'bg-secondary'} rounded-circle me-1" 
                                      style="width: 15px; height: 15px; display: inline-block;"></span>
                            </span>`;

                        // Add row
                        const rowNode = table.row.add([
                            `<button class="btn btn-sm edit-btn edit-user" type="button" data-userid="${contract.UserId}" title="Edit"><i class="fas fa-pencil-alt"></i></button>`,
                            `<input type="checkbox" name="SelectedUser" value="${contract.UserId}" />`,
                            contract.UserName,
                            roleLabels,
                            statusLabel,
                            onlineDot
                        ]).draw(false).node();


                        $(rowNode).attr('data-userid', contract.UserId);
                    });

                    
                } else {
                    showToastError(res.message || "No Users found.");
                }
                $('#saveBtn').prop('disabled', true);
                $('#checkAll').prop('checked', false);
                table.draw();
            },
            error: function () {
                table.clear().draw();
                $('#saveBtn').prop('disabled', true);
                $('#checkAll').prop('checked', false);
                showToastError("Error loading user listing.");
            }
        });
    }


    $('#saveBtn').on('click', function () {
        const selectedUsers = $('input[name="SelectedUser"]:checked').map(function () {
            const row = $(this).closest('tr');
            return {
                UserId: $(this).val(),
            };
        }).get();

        if (selectedUsers.length === 0) {
            showToastWarning("Please select at least one user to update.");
            return;
        }

        $.ajax({
            url: '/Account/UpdateUsers',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                users: selectedUsers,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            }),
            success: function (res) {
                if (res.success) {
                    showToastSuccess(res.message || "users updated successfully.");
                    loadUsers();
                } else {
                    showToastError(res.message || "Failed to update users.");
                }
            },
            error: function () {
                showToastError("Error updating users.");
            }
        });
    });

    loadUsers();

    $(document).on('click', '.edit-user', function (e) {
        e.preventDefault();
        const userId = $(this).data('userid');
        openEditUserModal(userId);
    });

    function openEditUserModal(userId) {
        $.ajax({
            url: '/Account/GetUserDetails',
            type: 'GET',
            data: { userId: userId },
            success: function (res) {
                if (res.success) {
                    const user = res.Data;
                    // Fill modal fields
                    $('#UserId').val(user.UserId);
                    $('#UserName').val(user.UserName);

                    // Populate roles dropdown
                    let options = '';
                    user.AllRoles.forEach(role => {
                        const selected = user.Roles.includes(role) ? 'selected' : '';
                        options += `<option value="${role}" ${selected}>${role}</option>`;
                    });
                    $('#roles').html(options);
                    // Open modal
                    $('#editUserModal').modal('show');

                } else {
                    showToastError(res.message || "Failed to load user details.");
                }
            },
            error: function () {
                showToastError("Error loading user details.");
            }
        });
    }

    $('#checkAll').on('change', function () {
        const checked = this.checked;
        $('input[name="SelectedUser"]').prop('checked', checked).trigger('change');
    });


    $('#updateUserForm').on('submit', function (e) {
        e.preventDefault();

        const userId = $('#UserId').val();
        const selectedRoles = $('#roles').val(); // multiple select array

        $.ajax({
            url: '/Account/UpdateUserRoles',
            type: 'POST',
            data: {
                userId: userId,
                roles: selectedRoles,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (res) {
                if (res.success) {
                    $('#editUserModal').modal('hide');
                    loadUsers(); // refresh datatable
                    showToastSuccess("User roles updated successfully.");
                } else {
                    showToastError(res.message || "Failed to update roles.");
                }
            },
            error: function () {
                showToastError("Error updating roles.");
            }
        });
    });

    $(document).on('change', 'input[name="SelectedUser"]', function () {
        const total = $('input[name="SelectedUser"]').length;
        const checked = $('input[name="SelectedUser"]:checked').length;
        $('#saveBtn').prop('disabled', checked === 0);
        $('#checkAll').prop('checked', total === checked);
    });

});