$(document).ready(function () {
    let table;
    let allFollowUpDates = [];
    let selectedDate = "";
    let allFollowUpDatesTyped = [];

    table = $('#calendarTable').DataTable({
        responsive: true,
        processing: true,
        serverSide: true,
        paging: true,
        ordering: true,
        searching: true,
        order: [],
        autoWidth: false,
        ajax: {
            url: '/CalendarDashboard/GetCalendarContracts',
            type: 'POST',
            data: function (d) {
                d.SelectedDate = selectedDate;
            }
        },
        language: {
            emptyTable: "No data available in table",
            zeroRecords: "No matching calendar entries found"
        },
        columns: [
            { data: 'InputDate' },
            { data: 'PaymentStatus' },
            { data: 'ContractStatus' },
            { data: 'PreSalesNotes' },
            { data: 'SupplierCobanaInvoiceNotes' }
        ]
    });

    // Enable column resizing
    enableColumnResizing('#calendarTable');

    loadFollowUpDatesAndInitCalendar();

    function loadFollowUpDatesAndInitCalendar() {
        $.ajax({
            url: '/CalendarDashboard/GetFollowUpDates',
            type: 'POST',
            data: {
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (res) {
                if (res.success && Array.isArray(res.Data)) {
                    allFollowUpDates = res.Data.filter(d => /^\d{4}-\d{2}-\d{2}$/.test(d));
                    if (allFollowUpDates.length === 0) {
                        const today = moment().format("YYYY-MM-DD");
                        allFollowUpDates = [today];
                    }
                    initFlatpickr();
                } else {
                    showToastError("No dates found.");
                }
            },
            error: function () {
                showToastError("Failed to load follow-up dates.");
            }
        });
    }

    function initFlatpickr() {
        flatpickr("#calendarFlatContainer", {
            inline: true,
            dateFormat: "Y-m-d",
            defaultDate: null,
            enable: allFollowUpDates,
            disableMobile: true,
            locale: {
                firstDayOfWeek: 1
            },
            onChange: function (selectedDates, dateStr) {
                selectedDate = dateStr;
                table.ajax.reload();
            },
            onDayCreate: function (_, __, ___, dayElem) {
                const day = dayElem.dateObj?.getDate();
                const month = (dayElem.dateObj?.getMonth() + 1).toString().padStart(2, '0');
                const year = dayElem.dateObj?.getFullYear();
                const formattedDate = `${year}-${month}-${day.toString().padStart(2, '0')}`;

                if (allFollowUpDates.includes(formattedDate)) {
                    dayElem.classList.add("has-followup");
                }
            }
        });
    }
});
