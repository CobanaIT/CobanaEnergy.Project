$(document).ready(function () {
    // Initialize DataTable on the server-rendered table
    $('#userTable').DataTable({
        paging: true,
        searching: true,
        ordering: true,
        order: [[1, 'asc']], // Sort by username
        info: true,
        responsive: true,
        autoWidth: false
    });

    // Enable column resizing
    enableColumnResizing('#userTable');
});

function editUserProfile(userId) {
    // Navigate to Update User page
    window.location.href = '/Account/UpdateUser?userId=' + encodeURIComponent(userId);
}
