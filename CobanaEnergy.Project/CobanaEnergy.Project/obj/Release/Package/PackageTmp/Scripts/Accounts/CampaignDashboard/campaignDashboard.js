$(document).ready(function () {
    var table = $('#campaignTable').DataTable({
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
    enableColumnResizing('#campaignTable');

    var excelButton = new $.fn.dataTable.Buttons(table, {
        buttons: [
            {
                extend: 'excelHtml5',
                text: 'Export',
                title: '',
                filename: function () {
                    let filename = 'Campaign';
                    
                    const supplierName = $('#supplierFilter option:selected').text();
                    if (supplierName && supplierName !== 'All Suppliers') {
                        filename += `_${supplierName.replace(/[^a-zA-Z0-9]/g, '_')}`;
                    }
                    
                    const today = new Date();
                    filename += `_${today.toISOString().split('T')[0]}`;
                    
                    return filename;
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


    $('#supplierFilter').change(function () {

        loadCompaigns();

        const supplierId = $(this).val();
        const $campaign = $('#campaignFilter');

        $campaign.prop('disabled', true).empty().append('<option>Loading...</option>');

        if (!supplierId) {
            $campaign.empty().append('<option value="">Select Campaign</option>').prop('disabled', true);
            return;
        }

        $.get(`/Campaign/GetCampaignBySupplier?supplierId=${supplierId}`, function (res) {
            $campaign.empty().append('<option value="">Select Campaign</option>');

            if (res.success && res.Data.length > 0) {
                res.Data.forEach(c => {
                    $campaign.append(`<option value="${c.CId}">${c.CampaignName}</option>`);
                });
            } else {
                $campaign.append('<option disabled>No campaigns found</option>');
            }

            $campaign.prop('disabled', false);
        });
    });



    $('#campaignFilter').on('change', function () {
        loadCompaigns();
    });


    function loadCompaigns() {
        const supplierId = $('#supplierFilter').val();
        const cId = $('#campaignFilter').val();

        $.ajax({
            url: '/Campaign/GetCampaignList',
            type: 'POST',
            data: {
                supplierId: supplierId,
                cId: cId,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (res) {
                table.clear();

                if (res.success) {
                    const campaigns = res.Data.Campaigns || [];

                    campaigns.forEach(contract => {
                        table.row.add([
                            contract.BusinessName,
                            contract.Number ?? '',
                            contract.CreatedAt,
                            contract.Bonus
                        ]);
                    });

                    $('#contractsCount').text(res.Data.Count || "0");
                    $('#totalBonus').text(res.Data.TotalBonus || "0");
                    $('#targetAchieved').text(
                        res.Data.TargetAchieved === false ? "No" :
                            res.Data.TargetAchieved === true ? "Yes" :
                                "N/A"
                    );
                } else {
                    showToastError(res.message || "No campaigns found.");
                    $('#contractsCount').text("0");
                    $('#totalBonus').text("0");
                    $('#targetAchieved').text("N/A");
                }
                table.draw();
            },
            error: function () {
                table.clear().draw();
                $('#contractsCount').text("0");
                $('#totalBonus').text("0");
                $('#targetAchieved').text("N/A");
                showToastError("Error loading campaign data.");
            }
        });
    }


    loadCompaigns();
});