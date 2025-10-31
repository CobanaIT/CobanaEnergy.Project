/**
 * Enhanced Brokerage Manager - Handles dynamic field population based on Department and Source
 * This script manages the brokerage selection, Ofgem ID auto-population, Department auto-selection,
 * and dynamic field population based on Department and Source selection
 */

class BrokerageManager {
    constructor(options = {}) {
        this.brokerageSelectId = options.brokerageSelectId || 'brokerage';
        this.ofgemIdInputId = options.ofgemIdInputId || 'ofgemId';
        this.departmentSelectId = options.departmentSelectId || 'department';
        this.isEditMode = options.isEditMode || false;
        this.currentBrokerageId = options.currentBrokerageId || null;
        this.currentDepartment = options.currentDepartment || null;
        this.currentSource = options.currentSource || null; // Track current source selection
        
        // NEW: Model values for edit mode
        this.modelValues = options.modelValues || null;
        
        this.applyDisabledStyling();
        this.init();
    }

    init() {
        this.loadBrokerages();
        this.bindEvents();
        
        // Note: setCurrentBrokerage is now called in loadBrokerages() after options are loaded
        // to ensure proper sequencing and avoid race conditions
    }

    /**
     * Load active brokerages from the sectors table
     */
    loadBrokerages() {
        const $brokerageSelect = $(`#${this.brokerageSelectId}`);
        if (!$brokerageSelect.length) {
            console.warn('Brokerage select element not found:', this.brokerageSelectId);
            return;
        }

        // Clear existing options except the first one
        $brokerageSelect.find('option:not(:first)').remove();

        // Fetch brokerages from the server
        $.ajax({
            url: '/Sector/GetActiveSectors',
            type: 'GET',
            data: { sectorType: 'Brokerage' },
            success: (response) => {
                if (response.success && response.Data && response.Data.Sectors) {
                    const brokerages = response.Data.Sectors;
                    
                    brokerages.forEach(brokerage => {
                        const $option = $('<option>', {
                            value: brokerage.SectorId,
                            text: brokerage.Name
                        }).attr({
                            'data-ofgem-id': brokerage.OfgemID || '',
                            'data-department': brokerage.Department || ''
                        });
                        
                        $brokerageSelect.append($option);
                    });
                    
                    // Now that brokerages are loaded, set the current brokerage if in edit mode
                    if (this.isEditMode && this.currentBrokerageId !== null && this.currentBrokerageId !== undefined) {
                        this.setCurrentBrokerage();
                    } else {
                    }
                } else {
                    console.warn('Invalid response format:', response);
                }
            },
            error: (xhr, status, error) => {
                console.error('Failed to load brokerages:', error);
                console.error('Status:', status);
                console.error('Response:', xhr.responseText);
                this.showError('Failed to load brokerages. Please refresh the page.');
            }
        });
    }

    /**
     * Bind event handlers
     */
    bindEvents() {
        $(`#${this.brokerageSelectId}`).on('change', (e) => this.handleBrokerageChange(e));
        $('#source').on('change', (e) => this.handleSourceChange(e));
        $('#collaboration').on('change', (e) => this.handleCollaborationChange(e));
    }

    /**
     * Handle brokerage selection change
     */
    handleBrokerageChange(event) {
        const $selectedOption = $(event.target).find('option:selected');
        const $ofgemIdInput = $(`#${this.ofgemIdInputId}`);
        const $departmentSelect = $(`#${this.departmentSelectId}`);

        // Reset all dynamic fields and dropdowns when brokerage changes
        this.hideAllDynamicFields();
        this.hideAllSourceFields();
        this.resetAllDropdownValues();

        if ($selectedOption.length && $selectedOption.val()) {
            // Auto-populate Ofgem ID
            if ($ofgemIdInput.length) {
                $ofgemIdInput.val($selectedOption.attr('data-ofgem-id') || '');
                // Make the OfgemID field look disabled
                $ofgemIdInput.prop('readonly', true);
                $ofgemIdInput.addClass('disabled-field');
            }

            // Auto-populate Department
            if ($departmentSelect.length) {
                const department = $selectedOption.attr('data-department') || '';
                this.populateDepartmentDropdown(department);
                this.handleDepartmentChange(department);
            }
        } else {
            // Clear fields if no brokerage selected
            if ($ofgemIdInput.length) {
                $ofgemIdInput.val('');
                // Re-enable the OfgemID field
                $ofgemIdInput.prop('readonly', false);
                $ofgemIdInput.removeClass('disabled-field');
            }
            if ($departmentSelect.length) {
                this.clearDepartmentDropdown();
            }
        }
    }

    /**
     * Handle department change
     */
    handleDepartmentChange(department) {
        this.hideAllDynamicFields();
        
        if (!department) return;

        switch (department.toLowerCase()) {
            case 'in house':
                this.showInHouseFields();
                this.populateSourceDropdown(['Data', 'Referral Partners', 'Self-Gen', 'Cobana RNW']);
                break;
            case 'brokers':
                this.showBrokersFields();
                this.populateSourceDropdown(['Sub Broker', 'N/A']);
                break;
            case 'introducers':
                this.showIntroducersFields();
                this.populateSourceDropdown(['Sub Introducer', 'N/A']);
                break;
        }
    }

    /**
     * Handle source change
     */
    handleSourceChange(event) {
        const newSource = $(event.target).val();
        const previousSource = this.currentSource; // Track previous source
        
        // Reset dropdowns based on previous source before hiding fields
        //this.resetDropdownsForPreviousSource(previousSource);
        
        // Hide all source fields
        this.hideAllSourceFields("sourceDropdown");
        
        // Update current source
        this.currentSource = newSource;
        
        if (!newSource) return;

        switch (newSource.toLowerCase()) {
            case 'data':
                this.showDataSourceFields();
                this.loadLeadGenerators(() => this.populateModelValues());
                break;
            case 'referral partners':
                this.showReferralSourceFields();
                this.loadReferralPartners(() => {
                    this.loadSubReferralPartners(() => {
                        if (this.isEditMode) {
                            this.populateModelValues();
                        }
                    });
                });
                break;
            case 'self-gen':
                this.showSelfGenSourceFields();
                this.loadReferralPartners(() => {
                    this.loadSubReferralPartners(() => {
                        if (this.isEditMode) {
                            this.populateModelValues();
                        }
                    });
                });
                break;
            case 'cobana rnw':
                this.showCobanaRnwSourceFields();
                this.loadReferralPartners(() => {
                    this.loadSubReferralPartners(() => {
                        if (this.isEditMode) {
                            this.populateModelValues();
                        }
                    });
                });
                break;
            case 'sub broker':
                this.showSubBrokerSourceFields();
                this.loadSubBrokerages(() => this.populateModelValues());
                break;
            case 'sub introducer':
                this.showSubIntroducerSourceFields();
                this.loadSubIntroducers(() => this.populateModelValues());
                break;
        }
    }

    /**
     * Handle collaboration change
     */
    handleCollaborationChange(event) {
        const collaboration = $(event.target).val();
        const $leadGeneratorField = $('#leadGeneratorField');
        const $leadGenerator = $('#leadGenerator');
        const $referralPartnerField = $('#referralPartnerField');
        const $subReferralPartnerField = $('#subReferralPartnerField');
        
        // Get current source to determine which fields to show
        const currentSource = $('#source').val();
        
        if (collaboration === 'Lead Generator') {
            // For Data source, show Lead Generator field
            if (currentSource && currentSource.toLowerCase() === 'data') {
                $leadGeneratorField.show();
                $leadGenerator.prop('disabled', false);
                this.loadLeadGenerators(() => this.populateModelValues(false));
                $referralPartnerField.hide();
                $subReferralPartnerField.hide();
            }
            // For Self-Gen and Cobana RNW sources, show Lead Generator field
            else if (currentSource && (currentSource.toLowerCase() === 'self-gen' || currentSource.toLowerCase() === 'cobana rnw')) {
                $leadGeneratorField.show();
                $leadGenerator.prop('disabled', false);
                this.loadLeadGenerators(() => this.populateModelValues(false));
                $referralPartnerField.hide();
                $subReferralPartnerField.hide();
            }
        } else if (collaboration === 'Referral Partner') {
            // For Self-Gen and Cobana RNW sources, show Referral Partner fields
            if (currentSource && (currentSource.toLowerCase() === 'self-gen' || currentSource.toLowerCase() === 'cobana rnw')) {
                $leadGeneratorField.hide();
                $leadGenerator.prop('disabled', true);
                $leadGenerator.val('');
                $referralPartnerField.show();
                $subReferralPartnerField.show();
                this.loadReferralPartners(() => {
                    this.loadSubReferralPartners(() => this.populateModelValues(false));
                });
            }
        } else {
            // Hide all additional fields when N/A is selected
            $leadGeneratorField.hide();
            $leadGenerator.prop('disabled', true);
            $leadGenerator.val('');
            $referralPartnerField.hide();
            $subReferralPartnerField.hide();
        }
    }

    /**
     * Show In House fields
     */
    showInHouseFields() {
        $('#inHouseFields').show();
        $('#brokersFields').hide();
        $('#introducersFields').hide();
        
        this.loadClosers(() => this.populateModelValues());
    }

    /**
     * Show Brokers fields
     */
    showBrokersFields() {
        $('#brokersFields').show();
        $('#inHouseFields').hide();
        $('#introducersFields').hide();
        
        // Get the selected brokerage (SectorID) and load only its staff
        const sel = this.getSelectedBrokerageData();
        const sectorId = sel ? parseInt(sel.sectorId, 10) : 0;
        this.loadBrokerageStaff(sectorId, () => this.populateModelValues());
    }

    /**
     * Show Introducers fields
     */
    showIntroducersFields() {
        $('#introducersFields').show();
        $('#inHouseFields').hide();
        $('#brokersFields').hide();
        
        this.loadIntroducers(() => this.populateModelValues());
    }

    /**
     * Show Data source fields
     */
    showDataSourceFields() {
        $('#dataSourceFields').show();
        $('#leadGeneratorField').show();
        $('#referralPartnerField').hide();
        $('#subReferralPartnerField').hide();
        $('#subBrokerageField').hide();
        $('#subIntroducerField').hide();
        
        // Populate collaboration dropdown
        this.populateCollaborationDropdown();
    }

    /**
     * Show Referral source fields
     */
    showReferralSourceFields() {
        $('#referralPartnerField').show();
        $('#subReferralPartnerField').show();
        $('#dataSourceFields').hide();
        $('#leadGeneratorField').hide();
        $('#subBrokerageField').hide();
        $('#subIntroducerField').hide();
        
        // Populate collaboration dropdown
        this.populateCollaborationDropdown();
    }

    /**
     * Show Sub Broker source fields
     */
    showSubBrokerSourceFields() {
        $('#subBrokerageField').show();
        $('#dataSourceFields').hide();
        $('#leadGeneratorField').hide();
        $('#referralPartnerField').hide();
        $('#subReferralPartnerField').hide();
        $('#subIntroducerField').hide();
        
        // Populate collaboration dropdown
        this.populateCollaborationDropdown();
    }

    /**
     * Show Sub Introducer source fields
     */
    showSubIntroducerSourceFields() {
        $('#subIntroducerField').show();
        $('#dataSourceFields').hide();
        $('#leadGeneratorField').hide();
        $('#referralPartnerField').hide();
        $('#subReferralPartnerField').hide();
        $('#subBrokerageField').hide();
        
        // Populate collaboration dropdown
        this.populateCollaborationDropdown();
    }

    /**
     * Show Self-Gen source fields
     */
    showSelfGenSourceFields() {
        $('#dataSourceFields').show();
        $('#leadGeneratorField').hide();
        $('#referralPartnerField').hide();
        $('#subReferralPartnerField').hide();
        $('#subBrokerageField').hide();
        $('#subIntroducerField').hide();
        
        // Populate collaboration dropdown
        this.populateCollaborationDropdown();
    }

    /**
     * Show Cobana RNW source fields
     */
    showCobanaRnwSourceFields() {
        $('#dataSourceFields').show();
        $('#leadGeneratorField').hide();
        $('#referralPartnerField').hide();
        $('#subReferralPartnerField').hide();
        $('#subBrokerageField').hide();
        $('#subIntroducerField').hide();
        
        // Populate collaboration dropdown
        this.populateCollaborationDropdown();
    }

    /**
     * Hide all dynamic fields
     */
    hideAllDynamicFields() {
        $('#inHouseFields').hide();
        $('#brokersFields').hide();
        $('#introducersFields').hide();
    }

    /**
     * Hide all source fields
     */
    hideAllSourceFields(mode) {
        $('#dataSourceFields').hide();
        $('#leadGeneratorField').hide();
        $('#referralPartnerField').hide();
        $('#subReferralPartnerField').hide();
        $('#subBrokerageField').hide();
        $('#subIntroducerField').hide();

        if (mode == "sourceDropdown") {
            this.resetSourceDropdownValues();
        }
        else {
            this.resetAllDropdownValues();
        }
    }

    /**
     * Load closers
     */
    loadClosers(callback = null) {
        $.ajax({
            url: '/Sector/GetActiveSectors',
            type: 'GET',
            data: { sectorType: 'Closer' },
            success: (response) => {
                if (response.success && response.Data && response.Data.Sectors) {
                    const $closerSelect = $('#closer');
                    $closerSelect.find('option:not(:first)').remove();
                    
                    // Add N/A option first
                    $closerSelect.append('<option value="-1">N/A</option>');
                    
                    response.Data.Sectors.forEach(closer => {
                        const $option = $('<option>', {
                            value: closer.SectorId,
                            text: closer.Name
                        });
                        $closerSelect.append($option);
                    });
                    
                    // Execute callback if provided
                    if (callback && typeof callback === 'function') {
                        callback();
                    }
                }
            }
        });
    }

    /**
     * Load referral partners
     */
    loadReferralPartners(callback = null) {
        $.ajax({
            url: '/Sector/GetActiveSectors',
            type: 'GET',
            data: { sectorType: 'Referral' },
            success: (response) => {
                if (response.success && response.Data && response.Data.Sectors) {
                    const $referralSelect = $('#referralPartner');
                    $referralSelect.find('option:not(:first)').remove();
                    
                    // Add N/A option first
                    $referralSelect.append('<option value="-1">N/A</option>');
                    
                    response.Data.Sectors.forEach(referral => {
                        const $option = $('<option>', {
                            value: referral.SectorId,
                            text: referral.Name
                        });
                        $referralSelect.append($option);
                    });
                    
                    // Execute callback if provided
                    if (callback && typeof callback === 'function') {
                        callback();
                    }
                }
            }
        });
    }

    /**
     * Load sub referral partners
     */
    loadSubReferralPartners(callback = null) {
        $.ajax({
            url: '/Sector/GetActiveSubSectors',
            type: 'GET',
            data: { subSectorType: 'SubReferral' },
            success: (response) => {
                if (response.success && response.Data && response.Data.SubSectors) {
                    const $subReferralSelect = $('#subReferralPartner');
                    $subReferralSelect.find('option:not(:first)').remove();
                    
                    // Add N/A option first
                    $subReferralSelect.append('<option value="-1">N/A</option>');
                    
                    response.Data.SubSectors.forEach(subReferral => {
                        const $option = $('<option>', {
                            value: subReferral.SubSectorId,
                            text: subReferral.Name
                        });
                        $subReferralSelect.append($option);
                    });
                    
                    // Execute callback if provided
                    if (callback && typeof callback === 'function') {
                        callback();
                    }
                }
            }
        });
    }

    /**
     * Load brokerage staff
     */
    loadBrokerageStaff(sectorId, callback = null) {

        const $brokerageStaffSelect = $('#brokerageStaff');
        $brokerageStaffSelect.find('option:not(:first)').remove();

        if (!sectorId || sectorId <= 0) {
            return; 
        }


        $.ajax({
            url: '/Sector/GetActiveSubSectors',
            type: 'GET',
            data: { subSectorType: 'BrokerageStaff', sectorId: sectorId },
            success: (response) => {
                if (response.success && response.Data && response.Data.SubSectors) {
                    const $brokerageStaffSelect = $('#brokerageStaff');
                    $brokerageStaffSelect.find('option:not(:first)').remove();
                    
                    // Add N/A option first
                    $brokerageStaffSelect.append('<option value="-1">N/A</option>');
                    
                    response.Data.SubSectors.forEach(staff => {
                        const $option = $('<option>', {
                            value: staff.SubSectorId,
                            text: staff.Name
                        });
                        $brokerageStaffSelect.append($option);
                    });
                    
                    // Execute callback if provided
                    if (callback && typeof callback === 'function') {
                        callback();
                    }
                }
            },
            error: (xhr, status, error) => {
                console.error('Failed to load brokerage staff:', error);
            }
        });
    }

    /**
     * Load introducers
     */
    loadIntroducers(callback = null) {
        $.ajax({
            url: '/Sector/GetActiveSectors',
            type: 'GET',
            data: { sectorType: 'Introducer' },
            success: (response) => {
                if (response.success && response.Data && response.Data.Sectors) {
                    const $introducerSelect = $('#introducer');
                    $introducerSelect.find('option:not(:first)').remove();
                    
                    // Add N/A option first
                    $introducerSelect.append('<option value="-1">N/A</option>');
                    
                    response.Data.Sectors.forEach(introducer => {
                        const $option = $('<option>', {
                            value: introducer.SectorId,
                            text: introducer.Name
                        });
                        $introducerSelect.append($option);
                    });
                    
                    // Execute callback if provided
                    if (callback && typeof callback === 'function') {
                        callback();
                    }
                }
            }
        });
    }

    /**
     * Load sub brokerages
     */
    loadSubBrokerages(callback = null) {
        $.ajax({
            url: '/Sector/GetActiveSubSectors',
            type: 'GET',
            data: { subSectorType: 'SubBrokerage' },
            success: (response) => {
                if (response.success && response.Data && response.Data.SubSectors) {
                    const $subBrokerageSelect = $('#subBrokerage');
                    $subBrokerageSelect.find('option:not(:first)').remove();
                    
                    // Add N/A option first
                    $subBrokerageSelect.append('<option value="-1">N/A</option>');
                    
                    response.Data.SubSectors.forEach(subBrokerage => {
                        const $option = $('<option>', {
                            value: subBrokerage.SubSectorId,
                            text: subBrokerage.Name
                        });
                        $subBrokerageSelect.append($option);
                    });
                    
                    // Execute callback if provided
                    if (callback && typeof callback === 'function') {
                        callback();
                    }
                }
            }
        });
    }

    /**
     * Load sub introducers
     */
    loadSubIntroducers(callback = null) {
        $.ajax({
            url: '/Sector/GetActiveSubSectors',
            type: 'GET',
            data: { subSectorType: 'SubIntroducer' },
            success: (response) => {
                if (response.success && response.Data && response.Data.SubSectors) {
                    const $subIntroducerSelect = $('#subIntroducer');
                    $subIntroducerSelect.find('option:not(:first)').remove();
                    
                    // Add N/A option first
                    $subIntroducerSelect.append('<option value="-1">N/A</option>');
                    
                    response.Data.SubSectors.forEach(subIntroducer => {
                        const $option = $('<option>', {
                            value: subIntroducer.SubSectorId,
                            text: subIntroducer.Name
                        });
                        $subIntroducerSelect.append($option);
                    });
                    
                    // Execute callback if provided
                    if (callback && typeof callback === 'function') {
                        callback();
                    }
                }
            }
        });
    }

    /**
     * Load lead generators
     */
    loadLeadGenerators(callback = null) {
        $.ajax({
            url: '/Sector/GetActiveSectors',
            type: 'GET',
            data: { sectorType: 'Leads Generator' },
            success: (response) => {
                if (response.success && response.Data && response.Data.Sectors) {
                    const $leadGeneratorSelect = $('#leadGenerator');
                    $leadGeneratorSelect.find('option:not(:first)').remove();
                    
                    // Add N/A option first
                    $leadGeneratorSelect.append('<option value="-1">N/A</option>');
                    
                    response.Data.Sectors.forEach(leadGenerator => {
                        const $option = $('<option>', {
                            value: leadGenerator.SectorId,
                            text: leadGenerator.Name
                        });
                        $leadGeneratorSelect.append($option);
                    });
                    
                    // Execute callback if provided
                    if (callback && typeof callback === 'function') {
                        callback();
                    }
                }
            }
        });
    }

    /**
     * Populate department dropdown with the selected department
     */
    populateDepartmentDropdown(selectedDepartment) {
        const $departmentSelect = $(`#${this.departmentSelectId}`);
        if (!$departmentSelect.length) return;

        // Clear existing options except the first one
        $departmentSelect.find('option:not(:first)').remove();

        if (selectedDepartment) {
            // Add the selected department as an option
            const $option = $('<option>', {
                value: selectedDepartment,
                text: selectedDepartment,
                selected: true
            });
            $departmentSelect.append($option);
            
            // Make the department field look disabled and non-changeable
            $departmentSelect.prop('disabled', true);
            $departmentSelect.addClass('disabled-field');
        }
    }

    /**
     * Populate source dropdown with options
     */
    populateSourceDropdown(options) {
        const $sourceSelect = $('#source');
        if (!$sourceSelect.length) return;

        // Clear existing options except the first one
        $sourceSelect.find('option:not(:first)').remove();

        options.forEach(option => {
            const $optionElement = $('<option>', {
                value: option,
                text: option
            });
            $sourceSelect.append($optionElement);
        });
    }

    /**
     * Populate collaboration dropdown with options
     */
    populateCollaborationDropdown() {
        const $collaborationSelect = $('#collaboration');
        if (!$collaborationSelect.length) return;

        // Clear existing options except the first one
        $collaborationSelect.find('option:not(:first)').remove();

        // Get current source to determine which options to show
        const currentSource = $('#source').val();

        // Add Lead Generator and N/A options
        $collaborationSelect.append('<option value="Lead Generator">Lead Generator</option>');
        
        // For Self-Gen and Cobana RNW sources, also add Referral Partner option
        if (currentSource && (currentSource.toLowerCase() === 'self-gen' || currentSource.toLowerCase() === 'cobana rnw')) {
            $collaborationSelect.append('<option value="Referral Partner">Referral Partner</option>');
        }
        
        $collaborationSelect.append('<option value="-1">N/A</option>');
    }

    /**
     * Clear department dropdown
     */
    clearDepartmentDropdown() {
        const $departmentSelect = $(`#${this.departmentSelectId}`);
        if (!$departmentSelect.length) return;

        // Clear existing options except the first one
        $departmentSelect.find('option:not(:first)').remove();
        $departmentSelect.prop('selectedIndex', 0);
        
        // Re-enable the department field
        $departmentSelect.prop('disabled', false);
        $departmentSelect.removeClass('disabled-field');
    }

    /**
     * Reset all dropdown values to default
     */
    resetAllDropdownValues() {
        // Reset source dropdown
        const $sourceSelect = $('#source');
        if ($sourceSelect.length) {
            $sourceSelect.prop('selectedIndex', 0);
        }
        
        // Reset collaboration dropdown
        const $collaborationSelect = $('#collaboration');
        if ($collaborationSelect.length) {
            $collaborationSelect.prop('selectedIndex', 0);
        }
        
        // Reset lead generator dropdown
        const $leadGeneratorSelect = $('#leadGenerator');
        if ($leadGeneratorSelect.length) {
            $leadGeneratorSelect.prop('selectedIndex', 0);
        }
        
        // Reset referral partner dropdown
        const $referralPartnerSelect = $('#referralPartner');
        if ($referralPartnerSelect.length) {
            $referralPartnerSelect.prop('selectedIndex', 0);
        }
        
        // Reset sub referral partner dropdown
        const $subReferralPartnerSelect = $('#subReferralPartner');
        if ($subReferralPartnerSelect.length) {
            $subReferralPartnerSelect.prop('selectedIndex', 0);
        }
        
        // Reset sub brokerage dropdown
        const $subBrokerageSelect = $('#subBrokerage');
        if ($subBrokerageSelect.length) {
            $subBrokerageSelect.prop('selectedIndex', 0);
        }
        
        // Reset sub introducer dropdown
        const $subIntroducerSelect = $('#subIntroducer');
        if ($subIntroducerSelect.length) {
            $subIntroducerSelect.prop('selectedIndex', 0);
        }
        
        // Reset closer dropdown
        const $closerSelect = $('#closer');
        if ($closerSelect.length) {
            $closerSelect.prop('selectedIndex', 0);
        }
        
        // Reset brokerage staff dropdown
        const $brokerageStaffSelect = $('#brokerageStaff');
        if ($brokerageStaffSelect.length) {
            $brokerageStaffSelect.prop('selectedIndex', 0);
        }
        
        // Reset introducer dropdown
        const $introducerSelect = $('#introducer');
        if ($introducerSelect.length) {
            $introducerSelect.prop('selectedIndex', 0);
        }
    }

    resetSourceDropdownValues() {

        // Reset collaboration dropdown
        const $collaborationSelect = $('#collaboration');
        if ($collaborationSelect.length) {
            $collaborationSelect.prop('selectedIndex', 0);
        }

        // Reset lead generator dropdown
        const $leadGeneratorSelect = $('#leadGenerator');
        if ($leadGeneratorSelect.length) {
            $leadGeneratorSelect.prop('selectedIndex', 0);
        }

        // Reset referral partner dropdown
        const $referralPartnerSelect = $('#referralPartner');
        if ($referralPartnerSelect.length) {
            $referralPartnerSelect.prop('selectedIndex', 0);
        }

        // Reset sub referral partner dropdown
        const $subReferralPartnerSelect = $('#subReferralPartner');
        if ($subReferralPartnerSelect.length) {
            $subReferralPartnerSelect.prop('selectedIndex', 0);
        }

        // Reset sub brokerage dropdown
        const $subBrokerageSelect = $('#subBrokerage');
        if ($subBrokerageSelect.length) {
            $subBrokerageSelect.prop('selectedIndex', 0);
        }

        // Reset sub introducer dropdown
        const $subIntroducerSelect = $('#subIntroducer');
        if ($subIntroducerSelect.length) {
            $subIntroducerSelect.prop('selectedIndex', 0);
        }
    }

    /**
     * Apply disabled field styling
     */
    applyDisabledStyling() {
        // Add CSS for disabled fields
        if (!$('#disabledFieldStyles').length) {
            const $style = $('<style id="disabledFieldStyles">')
                .text(`
                    .disabled-field {
                        background-color: #e9ecef !important;
                        color: #6c757d !important;
                        cursor: not-allowed !important;
                        opacity: 0.65 !important;
                    }
                    .disabled-field:focus {
                        box-shadow: none !important;
                        border-color: #ced4da !important;
                    }
                `);
            $('head').append($style);
        }
    }

    /**
     * Set the current brokerage selection (for edit mode)
     */
    setCurrentBrokerage() {
        const $brokerageSelect = $(`#${this.brokerageSelectId}`);
        
        // Check if currentBrokerageId exists and is not null/undefined (0 is valid)
        if (!$brokerageSelect.length) {
            console.warn('Brokerage select element not found in setCurrentBrokerage');
            return;
        }
        
        if (this.currentBrokerageId === null || this.currentBrokerageId === undefined) {
            console.warn('currentBrokerageId is null or undefined');
            return;
        }

        // Check if the option exists before setting it
        const $option = $brokerageSelect.find(`option[value="${this.currentBrokerageId}"]`);
        if (!$option.length) {
            console.error(`Brokerage option with value ${this.currentBrokerageId} not found in dropdown`);
            return;
        }

        // Set the selected brokerage
        $brokerageSelect.val(this.currentBrokerageId);

        // Trigger change event to populate Ofgem ID and Department
        $brokerageSelect.trigger('change');
        
        // In edit mode, after brokerage change triggers department population,
        // we need to set the source value and trigger dynamic field loading
        if (this.isEditMode && this.currentSource) {
            // Use setTimeout to ensure department change has completed
            setTimeout(() => {
                this.setCurrentSource();
            }, 200);
        }
    }

    /**
     * Set the current source selection (for edit mode)
     */
    setCurrentSource() {
        const $sourceSelect = $('#source');
        
        if (!$sourceSelect.length) {
            console.warn('Source select element not found in setCurrentSource');
            return;
        }
        
        if (!this.currentSource) {
            console.warn('currentSource is null or undefined');
            return;
        }

        // Check if the option exists before setting it
        const $option = $sourceSelect.find(`option[value="${this.currentSource}"]`);
        if (!$option.length) {
            console.error(`Source option with value "${this.currentSource}" not found in dropdown`);
            return;
        }

        // Set the selected source
        $sourceSelect.val(this.currentSource);

        // Trigger change event to show appropriate dynamic fields
        $sourceSelect.trigger('change');
    }

    /**
     * Get selected brokerage data
     */
    getSelectedBrokerageData() {
        const $brokerageSelect = $(`#${this.brokerageSelectId}`);
        const $selectedOption = $brokerageSelect.find('option:selected');
        
        // Check if selectedOption exists and has a value (0 is valid)
        if (!$selectedOption.length || $selectedOption.val() === null || $selectedOption.val() === undefined || $selectedOption.val() === '') {
            return null;
        }

        return {
            sectorId: $selectedOption.val(),
            name: $selectedOption.text(),
            ofgemId: $selectedOption.attr('data-ofgem-id') || '',
            department: $selectedOption.attr('data-department') || ''
        };
    }

    /**
     * Reset all fields
     */
    reset() {
        const $brokerageSelect = $(`#${this.brokerageSelectId}`);
        const $ofgemIdInput = $(`#${this.ofgemIdInputId}`);
        const $departmentSelect = $(`#${this.departmentSelectId}`);

        if ($brokerageSelect.length) {
            $brokerageSelect.prop('selectedIndex', 0);
        }
        
        if ($ofgemIdInput.length) {
            $ofgemIdInput.val('');
        }
        
        if ($departmentSelect.length) {
            this.clearDepartmentDropdown();
        }

        this.hideAllDynamicFields();
        $('#source').prop('selectedIndex', 0);
    }

    /**
     * Show error message
     */
    showError(message) {
        console.error(message);
        // Example: show a toast notification or alert
        // alert(message);
    }

    /**
     * Destroy the manager and clean up
     */
    destroy() {
        $(`#${this.brokerageSelectId}`).off('change');
        $('#source').off('change');
        $('#collaboration').off('change');
    }

    /**
     * Populate dynamic fields with model values (for edit mode)
     */
    populateModelValues(handleCollaboration = true) {
        if (!this.modelValues) {
            return;
        }

        // Map model values to field IDs
        const fieldMappings = {
            closerId: '#closer',
            referralPartnerId: '#referralPartner',
            subReferralPartnerId: '#subReferralPartner',
            brokerageStaffId: '#brokerageStaff',
            introducerId: '#introducer',
            subIntroducerId: '#subIntroducer',
            subBrokerageId: '#subBrokerage',
            leadGeneratorId: '#leadGenerator'
        };

        if (handleCollaboration) {
            // Handle collaboration field separately as it's a text field
            const collaborationValue = this.modelValues.collaboration;
            const $collaborationField = $('#collaboration');
            if ($collaborationField.length && collaborationValue) {
                if (collaborationValue === 'N/A') {
                    $collaborationField.val('-1');
                } else {
                    $collaborationField.val(collaborationValue);
                    // Trigger change event to show appropriate dynamic fields
                    $collaborationField.trigger('change');
                }
            }
        }
        // Populate each field if it has a value and the field exists
        Object.entries(fieldMappings).forEach(([modelKey, fieldSelector]) => {
            const modelValue = this.modelValues[modelKey];
            const $field = $(fieldSelector);

            if ($field.length) {
                // Check if the field is a select dropdown
                if ($field.is('select')) {
                    // Special handling for ID fields with 0 value (N/A)
                    if (modelValue === 0 || modelValue === '0') {
                        // Set to N/A option (-1)
                        const $naOption = $field.find('option[value="-1"]');
                        if ($naOption.length) {
                            $field.val('-1');
                        }
                    } else if (modelValue && modelValue !== '' && modelValue !== '0') {
                        // Check if the option exists before setting
                        const $option = $field.find(`option[value="${modelValue}"]`);
                        if ($option.length) {
                            $field.val(modelValue);
                        }
                    }
                }
            }
        });

    }
}

// Auto-initialize for all contract forms
$(document).ready(function() {
    // Initialize for Create Electric form
    if ($('#createElectricForm').length) {
        new BrokerageManager({
            brokerageSelectId: 'brokerage',
            ofgemIdInputId: 'ofgemId',
            departmentSelectId: 'department'
        });
    }

    // Initialize for Create Gas form
    if ($('#createGasForm').length) {
        new BrokerageManager({
            brokerageSelectId: 'brokerage',
            ofgemIdInputId: 'ofgemId',
            departmentSelectId: 'department'
        });
    }

    // Initialize for Create Dual form
    if ($('#createDualForm').length) {
        new BrokerageManager({
            brokerageSelectId: 'brokerage',
            ofgemIdInputId: 'ofgemId',
            departmentSelectId: 'department'
        });
    }

    // Note: Edit forms are now manually initialized in their respective JavaScript files
    // to avoid conflicts and ensure proper data handling
});
