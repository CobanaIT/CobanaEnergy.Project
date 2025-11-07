//#region Autocomplete Dropdown for MPAN
$(function () {
    initAutoCompleteDropdown({
        inputSelector: "#mpan",
        suggestionSelector: "#mpanSuggestions",
        loaderSelector: "#mpanLoader",
        url: "/Electric/GetMPANs",
        pattern: /^\d+$/,
        getDisplayValue: item => item, // simple string
        onSelect: mpan => {
            checkDuplicateMPAN(mpan);
            populateMPANRelationData(mpan);
        }
    });
});
function checkDuplicateMPAN(mpan) {
    $.get(`/Electric/CheckDuplicateMpan?mpan=${mpan}`, function (res) {
        $('#mpanLoader').hide();

        if (res.success && res.Data) {
            const d = res.Data;
            $('#duplicateMpanModal tbody').html(`
                        <tr>
                            <td>${d.Agent || 'N/A'}</td>
                            <td>${d.BusinessName}</td>
                            <td>${d.CustomerName}</td>
                            <td>${d.InputDate}</td>
                            <td>${d.PreSalesStatus}</td>
                            <td>${d.Duration}</td>
                        </tr>
                    `);
            $('#duplicateMpanModal').modal('show');
        }
    }).fail(function () {
        $('#mpanLoader').hide();
        showToastError("Error checking MPAN.");
    });
}
function populateMPANRelationData(mpan) {
    $.get(`/Electric/GetMPANRelationalData?mpan=${mpan}`, function (res) {
        if (res.success && res.Data) {
            $('#businessDoorNumber').val(res.Data.BusinessDoorNumber);
            $('#businessHouseName').val(res.Data.BusinessHouseName);
            $('#businessStreet').val(res.Data.BusinessStreet);
            $('#businessTown').val(res.Data.BusinessTown);
            $('#businessCounty').val(res.Data.BusinessCounty);
            $('#postCode').val(res.Data.PostCode);
        }
        else if (!res.success) {
            showToastError(res.message);
        }
    }).fail(function () {
        showToastError("Error occurred while fetching MPAN relational data.");
    });
}

//#endregion


//#region Autocomplete Dropdown for MPRN
$(function () {
    initAutoCompleteDropdown({
        inputSelector: "#mprn",
        suggestionSelector: "#mprnSuggestions",
        loaderSelector: "#mprnLoader",
        url: "/Gas/GetMPRNs",
        pattern: /^\d+$/,
        getDisplayValue: item => item, // simple string
        onSelect: mprn => {
            checkDuplicateMPRN(mprn);
            if (window.location.pathname ==='/Gas/CreateGas') // Populate relational data only on create gas contract page.
                populateMPRNRelationalData(mprn);
        }
    });
});
function checkDuplicateMPRN(mprn) {
    $.get(`/Gas/CheckDuplicateMprn?mprn=${mprn}`, function (res) {
        $('#mprnLoader').hide();

        if (res.success && res.Data) {
            const d = res.Data;
            $('#duplicateMprnModal tbody').html(`
                        <tr>
                            <td>${d.Agent || 'N/A'}</td>
                            <td>${d.BusinessName}</td>
                            <td>${d.CustomerName}</td>
                            <td>${d.InputDate}</td>
                            <td>${d.PreSalesStatus}</td>
                            <td>${d.Duration}</td>
                        </tr>
                    `);
            $('#duplicateMprnModal').modal('show');
        }
    }).fail(function () {
        $('#mprnLoader').hide();
        showToastError("Error checking MPRN.");
    });
}
function populateMPRNRelationalData(mprn) {
    $.get(`/Gas/GetMPRNRelationalData?mprn=${mprn}`, function (res) {
        if (res.success && res.Data) {
            $('#businessDoorNumber').val(res.Data.BusinessDoorNumber);
            $('#businessHouseName').val(res.Data.BusinessHouseName);
            $('#businessStreet').val(res.Data.BusinessStreet);
            $('#businessTown').val(res.Data.BusinessTown);
            $('#businessCounty').val(res.Data.BusinessCounty);
            $('#postCode').val(res.Data.PostCode);
        }
        else if (!res.success) {
            showToastError(res.message);
        }
    }).fail(function () {
        showToastError("Error occurred while fetching MPRN relational data.");
    });
}


//#endregion


//#region Autocomplete Dropdown for Business Name
$(function () {
    initAutoCompleteDropdown({
        inputSelector: "#businessName",
        suggestionSelector: "#bnameSuggestions",
        loaderSelector: "#bnameLoader",
        url: "/Electric/SearchBusinessesByNamePrefix",
        pattern: /^[A-Za-z0-9\s!@#$%^&*()_\-+=\[{\]};:'",.<>/?\\|`~]*$/,
        renderItem: item => `<div class="detail-item">
                                    <div class="fw-bold">${item.BusinessName}</div>
                                    <div class="text-muted small">${item.CustomerName}</div>
                                </div>`,
        getDisplayValue: item => item.BusinessName,
        onSelect: businessContactInfo => {
            $("#customerName").val(businessContactInfo.CustomerName)
            $("#phoneNumber1").val(businessContactInfo.PhoneNumber1)
            $("#phoneNumber2").val(businessContactInfo.PhoneNumber2)
            $("#emailAddress").val(businessContactInfo.EmailAddress)
        }
    });
});

//#endregion


//#region Autocomplete Dropdown for Customer Name
$(function () {
    initAutoCompleteDropdown({
        inputSelector: "#customerName",
        suggestionSelector: "#cnameSuggestions",
        loaderSelector: "#cnameLoader",
        url: "/Electric/SearchBusinessesByCustomerNamePrefix",
        pattern: /^[A-Za-z0-9\s!@#$%^&*()_\-+=\[{\]};:'",.<>/?\\|`~]*$/,
        renderItem: item => `<div class="detail-item">
                                    <div class="fw-bold">${item.CustomerName}</div>
                                    <div class="text-muted small">${item.EmailAddress}</div>
                                  </div>`,
        getDisplayValue: item => item.CustomerName,
        onSelect: businessContactInfo => {
            $("#phoneNumber1").val(businessContactInfo.PhoneNumber1)
            $("#phoneNumber2").val(businessContactInfo.PhoneNumber2)
            $("#emailAddress").val(businessContactInfo.EmailAddress)
        }
    });
});

//#endregion