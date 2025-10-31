window.FilterModule = (function () {


    const selectConfigs = {
        '#supplierFilter': 'All Suppliers',
        '#departmentFilter': 'Select Department',
        '#brokerageFilter': 'Select Brokerage',
        '#brokerageStaffFilter': 'Select Staff',
        '#subBrokerageFilter': 'Select Sub Brokerage',
        '#closerFilter': 'Select Closer',
        '#leadGenFilter': 'Select Lead Generator',
        '#introducerFilter': 'Select Introducer',
        '#subIntroducerFilter': 'Select Sub Introducer',
        '#referralFilter': 'Select Referral',
        '#subReferralFilter': 'Select Sub Refferal'
    };

    for (const [selector, placeholder] of Object.entries(selectConfigs)) {
        $(selector).select2({
            placeholder: placeholder,
            allowClear: true,
            width: '100%'
        });
    }


    function init(tableInstance) {
        setupDepartmentCascade(tableInstance);
    }

    function setupDepartmentCascade(table) {
        // hide all initially
        $("#brokerage-area, #brokerage-staff-area, #subbrokerage-area, #inhouse-closer-area, #leadgen-area, #referral-area, #sub-referral-area, #introducer-area, #subintroducer-area").hide();

        $('#departmentFilter').on('change', function () {
            const val = $(this).val();
            $("#brokerage-area, #brokerage-staff-area, #subbrokerage-area, #inhouse-closer-area, #leadgen-area, #referral-area,  #sub-referral-area, #introducer-area, #subintroducer-area").hide();

            if (val === 'Brokers') {
                $('#brokerage-area').show();
                $.getJSON(`/Common/Sectors?sectorType=Brokerage`).done(data => fillDropdown("#brokerageFilter", data));
            }
            else if (val === 'In House') {
                $('#inhouse-closer-area').show();
                showDropdownLoader("#closerFilter");

                $.getJSON(`/Common/Closers`).done(data => fillDropdown("#closerFilter", data)).fail(() => fillSelect2Dropdown("#brokerageFilter", []));
                // Refferal and Lead Generators
                $('#leadGenFilter, #referralFilter').empty().append('<option value="">All</option>');
                $('#leadgen-area, #referral-area').show();

                showDropdownLoader("#leadGenFilter");
                showDropdownLoader("#referralFilter");

                $.getJSON(`/Common/LeadGenerators`).done(data => fillDropdown("#leadGenFilter", data)).fail(() => fillSelect2Dropdown("#leadGenFilter", []));
                $.getJSON(`/Common/ReferralPartners`).done(data => fillDropdown("#referralFilter", data)).fail(() => fillSelect2Dropdown("#referralFilter", []));

            }
            else if (val === 'Introducers') {
                $('#introducer-area').show();
                showDropdownLoader("#introducerFilter");
                $.getJSON(`/Common/Introducers`).done(data => fillDropdown("#introducerFilter", data)).fail(() => fillSelect2Dropdown("#introducerFilter", []));
            }
            if (table) table.ajax.reload();
        });

        $('#brokerageFilter').on('change', function () {
            const id = $(this).val();
            $('#brokerageStaffFilter, #subBrokerageFilter').empty().append('<option value="">All</option>');
            if (id) {
                $('#brokerage-staff-area, #subbrokerage-area').show();
                showDropdownLoader("#brokerageStaffFilter");
                showDropdownLoader("#subBrokerageFilter");

                $.getJSON(`/Common/BrokerageStaff?brokerageId=${id}`).done(data => fillDropdown("#brokerageStaffFilter", data)).fail(() => fillSelect2Dropdown("#introducerFilter", []));
                $.getJSON(`/Common/SubBrokerages?brokerageId=${id}`).done(data => fillDropdown("#subBrokerageFilter", data)).fail(() => fillSelect2Dropdown("#introducerFilter", []));
            } else {
                $('#brokerage-staff-area, #subbrokerage-area').hide();
            }
            if (table) table.ajax.reload();
        });

        $('#brokerageStaffFilter, #subBrokerageFilter, #supplierFilter, #contractstatus,#paymentStatusAcc, #startDateFilter, #endDateFilter').on('change', () => {
            if (table) table.ajax.reload();
        });

        $('#closerFilter').on('change', function () {
            if (table) table.ajax.reload();
        });

        $('#leadGenFilter').on('change', () => {
            if (table) table.ajax.reload();
        });

        $('#referralFilter').on('change', function () {
            const id = $(this).val();
            $('#sub-referral-area').show();
            showDropdownLoader("#subReferralFilter");

            $.getJSON(`/Common/SubReferralPartners?referralId=${id}`).done(data => fillDropdown("#subReferralFilter", data)).fail(() => fillSelect2Dropdown("#subReferralFilter", []));
            if (table) table.ajax.reload();
        });

        $('#introducerFilter').on('change', function () {
            const id = $(this).val();
            $('#subIntroducerFilter').empty().append('<option value="">All</option>');
            if (id) {
                $('#subintroducer-area').show();
                $.getJSON(`/Common/SubIntroducers?introducerId=${id}`).done(data => fillDropdown("#subIntroducerFilter", data)).fail(() => fillSelect2Dropdown("#subIntroducerFilter", []));
            } else {
                $('#subintroducer-area').hide();
            }
            if (table) table.ajax.reload();
        });

        $('#subIntroducerFilter').on('change', () => {
            if (table) table.ajax.reload();
        });
    }

    function fillDropdown(selector, data, defaultText = "All") {

        const $ddl = $(selector);
        const $container = $ddl.next('.select2-container');

        $container.removeClass('loading');
        $ddl.prop('disabled', false);

        $ddl.empty().append(`<option value="">${defaultText}</option>`);
        if (Array.isArray(data.Data)) {
            data.Data.forEach(item => {
                $ddl.append(new Option(item.Name, item.Id));
            });
        }

        $ddl.trigger('change.select2');

    }


    function showDropdownLoader(selector) {
        const $ddl = $(selector);
        const $container = $ddl.next('.select2-container');

        $ddl.prop('disabled', true);
        $container.addClass('loading');

        $container.find('.select2-selection__rendered').text('Loading...');
    }

    return {
        init: init
    };

})();
