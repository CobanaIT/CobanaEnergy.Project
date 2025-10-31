/**
 * Sector Form Manager - Consolidated JavaScript for Create and Edit Sector pages
 * This file contains all shared functions to eliminate code duplication
 * 
 * END DATE HANDLING:
 * - Create Mode: All end dates are non-required. Backend will save MAX DATE value from database.
 * - Edit Mode: Loads saved values, allows user selection, all end dates are non-required.
 * - Business logic remains the same for edit mode, only required validation is removed.
 * 
 * BACKEND REQUIREMENT:
 * - When creating sectors, if any end date field is empty/null, the backend should set it to 
 *   the MAX DATE value from the database (e.g., '9999-12-31' or similar maximum date).
 * - This ensures all sectors have valid end dates for business logic while keeping them optional for users.
 */

class SectorFormManager {
    constructor(options = {}) {
        this.isEditMode = options.isEditMode || false;
        this.currentSectorType = '';
        this.modelData = options.modelData || null; // Store model data for edit mode
        this.suppliers = []; // Store suppliers data
        this.init();
    }

    init() {
        this.bindEvents();
        this.initializeForm();
    }

    bindEvents() {
        // Sector type change
        $('#sectorType').on('change', this.handleSectorTypeChange.bind(this));
        
        // Global event handlers for dynamically added elements
        this.bindGlobalEventHandlers();
        
        // Initialize duplicate account checker for all account number fields
        this.initializeDuplicateAccountChecker();
        
        // Initialize Select2 for all multiple select dropdowns
        this.initializeSelect2();
    }

    initializeForm() {
        // Populate department dropdown from DropdownOptions
        this.populateDepartmentDropdown();
        
        // If edit mode, load current sector type sections
        if (this.isEditMode) {
            const currentSectorType = $('#sectorType').val();
            if (currentSectorType) {
                // IMPORTANT: Set currentSectorType for edit mode
                this.currentSectorType = currentSectorType;
                
                this.loadDynamicSections(currentSectorType);
                this.toggleConditionalFields(currentSectorType);
                this.toggleBankAndTaxSections();
            }
        } else {
            // Create mode: ensure department is not required initially
            $('#department').prop('required', false);
            $('#department').prop('selectedIndex', 0);
        }
    }

    handleSectorTypeChange(e) {
        const selectedType = $(e.target).val();
        
        // IMPORTANT: Set currentSectorType FIRST before any other operations
        this.currentSectorType = selectedType;
        
        if (this.isEditMode) {
            // Edit mode: load sections and handle conditional fields
            this.loadDynamicSections(selectedType);
            this.toggleConditionalFields(selectedType);
            
            // Show/hide Bank Details and Company Tax sections based on data
            this.toggleBankAndTaxSections();
        } else {
            // Create mode: show/hide sections and handle additional logic
            if (selectedType) {
                $('#commonFieldsSection').show();
                $('#bankDetailsSection').show();
                $('#companyTaxSection').show();
                
                // Handle additional fields for Brokerage and Introducers
                if (selectedType === 'Brokerage' || selectedType === 'Introducer') {
                    $('#additionalFieldsSection').show();
                    // Make department required when visible
                    $('#department').prop('required', true);
                    
                    // Handle sector suppliers field for Brokerage
                    if (selectedType === 'Brokerage') {
                        $('#sectorSuppliers').closest('.row').show();
                        // Now currentSectorType is set, so this will work
                        this.populateSuppliersDropdown();
                    } else {
                        $('#sectorSuppliers').closest('.row').hide();
                        $('#sectorSuppliers').val(null).trigger('change');
                    }
                } else {
                    $('#additionalFieldsSection').hide();
                    $('#department').val('');
                    $('#ofgemId').val('');
                    // Remove required attribute when hidden to prevent validation errors
                    $('#department').prop('required', false);
                    // Hide sector suppliers field
                    $('#sectorSuppliers').closest('.row').hide();
                    $('#sectorSuppliers').val(null).trigger('change');
                }
                
                this.loadDynamicSections(selectedType);
                $('#submitBtn').prop('disabled', false);
            } else {
                // Hide all sections if no type selected
                $('#commonFieldsSection').hide();
                $('#additionalFieldsSection').hide();
                $('#bankDetailsSection').hide();
                $('#companyTaxSection').hide();
                $('#dynamicSectionsContainer').empty();
                $('#submitBtn').prop('disabled', true);
            }
        }
    }

    loadDynamicSections(sectorType) {
        const container = $('#dynamicSectionsContainer');
        container.empty();
        
        switch(sectorType) {
            case 'Brokerage':
                this.loadBrokerageSections();
                break;
            case 'Closer':
                this.loadCloserSections();
                break;
            case 'Leads Generator':
                this.loadLeadGeneratorSections();
                break;
            case 'Referral Partner':
                this.loadReferralPartnerSections();
                break;
            case 'Introducer':
                this.loadIntroducerSections();
                break;
        }
    }

    // Toggle conditional fields based on sector type
    toggleConditionalFields(sectorType) {
        this.currentSectorType = sectorType;
        const conditionalFields = $('#conditionalFields');
        
        if (sectorType === 'Introducer' || sectorType === 'Brokerage') {
            $('#additionalFieldsSection').show();
            conditionalFields.removeClass('hide').addClass('show');
        } else {
            conditionalFields.removeClass('show').addClass('hide');
        }
        
        // Handle sector suppliers field visibility
        if (sectorType === 'Brokerage') {
            $('#sectorSuppliers').closest('.row').show();
            this.populateSuppliersDropdown();
        } else {
            $('#sectorSuppliers').closest('.row').hide();
            $('#sectorSuppliers').val(null).trigger('change');
        }
    }

    // Toggle Bank Details and Company Tax sections based on data availability
    toggleBankAndTaxSections() {
        if (!this.isEditMode || !this.modelData) return;
        
        // Show Bank Details section if there's bank data
        const hasBankData = this.modelData.BankDetails && 
                           (this.modelData.BankDetails.BankName || 
                            this.modelData.BankDetails.AccountName || 
                            this.modelData.BankDetails.AccountNumber);
        
        if (hasBankData) {
            $('#bankDetailsSection').show();
        } else {
            $('#bankDetailsSection').hide();
        }
        
        // Show Company Tax section if there's tax data
        const hasTaxData = this.modelData.CompanyTaxInfo && 
                          (this.modelData.CompanyTaxInfo.CompanyRegistration || 
                           this.modelData.CompanyTaxInfo.VATNumber);
        
        if (hasTaxData) {
            $('#companyTaxSection').show();
        } else {
            $('#companyTaxSection').hide();
        }
    }

    // Generic method to check if a collection has valid data
    hasValidData(collectionName, primaryField, secondaryFields = []) {
        if (!this.modelData || !this.modelData[collectionName]) return false;
        
        return this.modelData[collectionName].some(item => {
            // Primary field must have value
            if (!item[primaryField] || item[primaryField].toString().trim() === '') {
                return false;
            }
            
            // At least one secondary field must have value (if specified)
            if (secondaryFields.length > 0) {
                return secondaryFields.some(field => {
                    const value = item[field];
                    return value && value.toString().trim() !== '';
                });
            }
            
            return true;
        });
    }

    // Load Brokerage specific sections
    loadBrokerageSections() {
        const container = $('#dynamicSectionsContainer');
        
        // Brokerage Commission Section - Only show fields when there's valid data
        container.append(`
            <div class="form-section brokerage-commission-section">
                <h4>Brokerage Commission & Payment</h4>
                <div id="brokerageCommissionContainer">
                    ${this.isEditMode && this.modelData && this.modelData.BrokerageCommissions && 
                      this.modelData.BrokerageCommissions.length > 0 && 
                      this.hasValidData('BrokerageCommissions', 'Commission', ['PaymentTerms', 'CommissionType']) 
                        ? this.generateBrokerageCommissionFieldsForEdit() 
                        : ''}
                </div>
                <button type="button" class="btn btn-secondary add-commission" data-type="brokerage">+ Add Commission</button>
            </div>
        `);
        
        // Brokerage Staff Section - Only show fields when there's valid data
        container.append(`
            <div class="form-section brokerage-staff-section">
                <h4>Brokerage Staff</h4>
                <div id="brokerageStaffContainer">
                    ${this.isEditMode && this.modelData && this.modelData.BrokerageStaff && 
                      this.modelData.BrokerageStaff.length > 0 && 
                      this.hasValidData('BrokerageStaff', 'BrokerageStaffName', ['Email', 'Mobile', 'Landline']) 
                        ? this.generateBrokerageStaffFieldsForEdit() 
                        : ''}
                </div>
                <button type="button" class="btn btn-secondary add-staff" data-type="brokerage">+ Add Staff Member</button>
            </div>
        `);
        
        // Sub Brokerage Section - Only show fields when there's valid data
        container.append(`
            <div class="form-section sub-brokerage-section">
                <h4>Sub Brokerage</h4>
                <div id="subBrokerageContainer">
                    ${this.isEditMode && this.modelData && this.modelData.SubBrokerages && 
                      this.modelData.SubBrokerages.length > 0 && 
                      this.hasValidData('SubBrokerages', 'SubBrokerageName', ['Email', 'Mobile', 'Landline']) 
                        ? this.generateSubBrokerageFieldsForEdit() 
                        : ''}
                </div>
                <button type="button" class="btn btn-secondary add-subsection" data-type="subBrokerage">+ Add Sub Brokerage</button>
            </div>
        `);
    }

    // Load Closer specific sections
    loadCloserSections() {
        const container = $('#dynamicSectionsContainer');
        
        container.append(`
            <div class="form-section closer-commission-section">
                <h4>Closer Commission & Payment</h4>
                <div id="closerCommissionContainer">
                    ${this.isEditMode && this.modelData && this.modelData.CloserCommissions && 
                      this.modelData.CloserCommissions.length > 0 && 
                      this.hasValidData('CloserCommissions', 'Commission', ['PaymentTerms', 'CommissionType']) 
                        ? this.generateCloserCommissionFieldsForEdit() 
                        : ''}
                </div>
                <button type="button" class="btn btn-secondary add-commission" data-type="closer">+ Add Commission</button>
            </div>
        `);
    }

    // Load Lead Generator specific sections
    loadLeadGeneratorSections() {
        const container = $('#dynamicSectionsContainer');
        
        container.append(`
            <div class="form-section lead-generator-commission-section">
                <h4>Lead Generator Commission & Payment</h4>
                <div id="leadGeneratorCommissionContainer">
                    ${this.isEditMode && this.modelData && this.modelData.LeadGeneratorCommissions && 
                      this.modelData.LeadGeneratorCommissions.length > 0 && 
                      this.hasValidData('LeadGeneratorCommissions', 'LeadGeneratorCommission', ['PaymentTerms', 'CommissionType']) 
                        ? this.generateLeadGeneratorCommissionFieldsForEdit() 
                        : ''}
                </div>
                <button type="button" class="btn btn-secondary add-commission" data-type="leadGenerator">+ Add Commission</button>
            </div>
        `);
    }

    // Load Referral Partner specific sections
    loadReferralPartnerSections() {
        const container = $('#dynamicSectionsContainer');
        
        // Referral Partner Commission Section - Only show fields when there's valid data
        container.append(`
            <div class="form-section referral-partner-commission-section">
                <h4>Referral Partner Commission & Payment</h4>
                <div id="referralPartnerCommissionContainer">
                    ${this.isEditMode && this.modelData && this.modelData.ReferralPartnerCommissions && 
                      this.modelData.ReferralPartnerCommissions.length > 0 && 
                      this.hasValidData('ReferralPartnerCommissions', 'ReferralPartnerCommission', ['PaymentTerms', 'CommissionType']) 
                        ? this.generateReferralPartnerCommissionFieldsForEdit() 
                        : ''}
                </div>
                <button type="button" class="btn btn-secondary add-commission" data-type="referralPartner">+ Add Commission</button>
            </div>
        `);
        
        // Sub Referral Section - Only show fields when there's valid data
        container.append(`
            <div class="form-section sub-referral-section">
                <h4>Sub Referral</h4>
                <div id="subReferralContainer">
                    ${this.isEditMode && this.modelData && this.modelData.SubReferrals && 
                      this.modelData.SubReferrals.length > 0 && 
            this.hasValidData('SubReferrals', 'SubReferralPartnerName', ['Email', 'Mobile', 'Landline']) 
                        ? this.generateSubReferralFieldsForEdit() 
                        : ''}
                </div>
                <button type="button" class="btn btn-secondary add-subsection" data-type="subReferral">+ Add Sub Referral</button>
            </div>
        `);
    }

    // Load Introducer specific sections
    loadIntroducerSections() {
        const container = $('#dynamicSectionsContainer');
        
        // Introducer Commission Section - Only show fields when there's valid data
        container.append(`
            <div class="form-section introducer-commission-section">
                <h4>Introducer Commission & Payment</h4>
                <div id="introducerCommissionContainer">
                    ${this.isEditMode && this.modelData && this.modelData.IntroducerCommissions && 
                      this.modelData.IntroducerCommissions.length > 0 && 
                      this.hasValidData('IntroducerCommissions', 'Commission', ['PaymentTerms', 'CommissionType']) 
                        ? this.generateIntroducerCommissionFieldsForEdit() 
                        : ''}
                </div>
                <button type="button" class="btn btn-secondary add-commission" data-type="introducer">+ Add Commission</button>
            </div>
        `);
        
        // Sub Introducer Section - Only show fields when there's valid data
        container.append(`
            <div class="form-section sub-introducer-section">
                <h4>Sub Introducer</h4>
                <div id="subIntroducerContainer">
                    ${this.isEditMode && this.modelData && this.modelData.SubIntroducers && 
                      this.modelData.SubIntroducers.length > 0 && 
                      this.hasValidData('SubIntroducers', 'SubIntroducerName', ['Email', 'Mobile', 'Landline']) 
                        ? this.generateSubIntroducerFieldsForEdit() 
                        : ''}
                </div>
                <button type="button" class="btn btn-secondary add-subsection" data-type="subIntroducer">+ Add Sub Introducer</button>
            </div>
        `);
    }

    // Generate field functions
    generateBrokerageCommissionFields(index) {
        return `
            <div class="commission-item" data-index="${index}">
                <input type="hidden" name="BrokerageCommissions[${index}].Id" value="" />
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="BrokerageCommissions[${index}].Commission" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="BrokerageCommissions[${index}].PaymentTerms" class="form-select" required>
                                <option value="">Payment Terms *</option>
                                <option value="Weekly">Weekly</option>
                                <option value="Monthly">Monthly</option>
                            </select>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Brokerage Commission Start Date</label>
                            <input type="date" name="BrokerageCommissions[${index}].StartDate" class="form-control" placeholder="Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Brokerage Commission End Date</label>
                            <input type="date" name="BrokerageCommissions[${index}].EndDate" class="form-control" placeholder="End Date (Optional)" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="BrokerageCommissions[${index}].CommissionType" class="form-select" required>
                                <option value="">Commission Type *</option>
                                <option value="Duration">Duration</option>
                                <option value="Annual">Annual</option>
                                <option value="Residual">Residual</option>
                                <option value="As per System">As per System</option>
                            </select>
                        </div>
                    </div>
                </div>
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateBrokerageStaffFields(index) {
        return `
            <div class="staff-item" data-index="${index}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="BrokerageStaff[${index}].BrokerageStaffName" class="form-control" placeholder="Staff Name *" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="BrokerageStaff[${index}].Active" class="form-select">
                                <option value="true" selected>YES</option>
                                <option value="false">NO</option>
                            </select>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Brokerage Staff Start Date</label>
                            <input type="date" name="BrokerageStaff[${index}].StartDate" class="form-control" placeholder="Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Brokerage Staff End Date</label>
                            <input type="date" name="BrokerageStaff[${index}].EndDate" class="form-control" placeholder="End Date (Optional)" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="email" name="BrokerageStaff[${index}].Email" class="form-control" placeholder="Email Address" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="BrokerageStaff[${index}].Landline" class="form-control landline-input" placeholder="Landline (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="BrokerageStaff[${index}].Mobile" class="form-control mobile-input" placeholder="Mobile (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <textarea name="BrokerageStaff[${index}].Notes" class="form-control" placeholder="Notes" rows="2"></textarea>
                        </div>
                    </div>
                </div>
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-staff"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateSubBrokerageFields(index) {
        return `
            <div class="subsection-item" data-index="${index}">
                <h5>Sub Brokerage ${index + 1}</h5>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubBrokerages[${index}].SubBrokerageName" class="form-control" placeholder="Sub Brokerage Name *" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubBrokerages[${index}].OfgemID" class="form-control" placeholder="Ofgem ID" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="SubBrokerages[${index}].Active" class="form-select">
                                <option value="true" selected>YES</option>
                                <option value="false">NO</option>
                            </select>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="email" name="SubBrokerages[${index}].Email" class="form-control" placeholder="Email" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Brokerage Start Date</label>
                            <input type="date" name="SubBrokerages[${index}].StartDate" class="form-control sub-start-date" placeholder="Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Brokerage End Date</label>
                                <input type="date" name="SubBrokerages[${index}].EndDate" class="form-control sub-end-date" placeholder="End Date (Optional)" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].Landline" class="form-control landline-input" placeholder="Landline (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].Mobile" class="form-control mobile-input" placeholder="Mobile (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                        </div>
                    </div>
                </div>
                
                <!-- Sub Brokerage Commission & Payment -->
                <div class="sub-commission-section">
                    <h6>Commission & Payment</h6>
                    <div class="sub-commission-container">
                        ${this.generateSubBrokerageCommissionFields(index, 0)}
                    </div>
                    <button type="button" class="btn btn-secondary btn-sm add-sub-commission" data-subsection-index="${index}">
                        <i class="fas fa-plus me-1"></i>Add Commission
                    </button>
                </div>

                <!-- Sub Brokerage Bank Details -->
                <div class="sub-bank-details">
                    <h6>Bank Details</h6>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.BankName" class="form-control" placeholder="Bank Name" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.BankBranchAddress" class="form-control" placeholder="Bank Branch Address" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.ReceiversAddress" class="form-control" placeholder="Receivers Address" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.AccountName" class="form-control" placeholder="Account Name" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.AccountSortCode" class="form-control" placeholder="Account Sort Code (6 digits)" maxlength="6" pattern="[0-9]{6}" title="Please enter exactly 6 digits" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.AccountNumber" class="form-control" placeholder="Account Number (8 digits)" maxlength="8" pattern="[0-9]{8}" title="Please enter exactly 8 digits" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.IBAN" class="form-control" placeholder="IBAN" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].BankDetails.SwiftCode" class="form-control" placeholder="Swift Code" />
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sub Brokerage Company Tax Info -->
                <div class="sub-company-tax">
                    <h6>Company Tax Information</h6>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].CompanyTaxInfo.CompanyRegistration" class="form-control" placeholder="Company Registration" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].CompanyTaxInfo.VATNumber" class="form-control" placeholder="VAT Number" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-12">
                            <div class="form-group">
                                <textarea name="SubBrokerages[${index}].CompanyTaxInfo.Notes" class="form-control" placeholder="Notes" rows="2"></textarea>
                            </div>
                        </div>
                    </div>
                </div>
                
                <div class="text-end mt-3">
                    <button type="button" class="btn btn-danger btn-sm remove-subsection"><i class="fas fa-trash me-1"></i>Remove Subsection</button>
                </div>
            </div>
        `;
    }

    generateSubBrokerageCommissionFields(subsectionIndex, commissionIndex) {
        return `
            <div class="sub-commission-item" data-subsection-index="${subsectionIndex}" data-commission-index="${commissionIndex}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].SubBrokerageCommission" class="form-control" placeholder="Sub Brokerage Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].BrokerageCommission" class="form-control" placeholder="Brokerage Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Brokerage Commission Start Date</label>
                            <input type="date" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].SubBrokerageStartDate" class="form-control" placeholder="Sub Brokerage Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Brokerage Commission End Date</label>
                            <input type="date" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].SubBrokerageEndDate" class="form-control" placeholder="Sub Brokerage End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Brokerage Commission Start Date</label>
                            <input type="date" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].BrokerageStartDate" class="form-control" placeholder="Brokerage Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Brokerage Commission End Date</label>
                            <input type="date" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].BrokerageEndDate" class="form-control" placeholder="Brokerage End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].PaymentTerms" class="form-select">
                                <option value="">Payment Terms</option>
                                <option value="Weekly">Weekly</option>
                                <option value="Monthly">Monthly</option>
                                <option value="Quarterly">Quarterly</option>
                                <option value="Annually">Annually</option>
                            </select>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].CommissionType" class="form-select">
                                <option value="">Commission Type</option>
                                <option value="Duration">Duration</option>
                                <option value="Annual">Annual</option>
                                <option value="Residual">Residual</option>
                                <option value="As per System">As per System</option>
                            </select>
                        </div>
                    </div>
                </div>
                ${commissionIndex > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-sub-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateCloserCommissionFields(index) {
        return `
            <div class="commission-item" data-index="${index}">
                <input type="hidden" name="CloserCommissions[${index}].Id" value="" />
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="CloserCommissions[${index}].Commission" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="CloserCommissions[${index}].PaymentTerms" class="form-select" required>
                                <option value="">Payment Terms *</option>
                                <option value="Weekly">Weekly</option>
                                <option value="Monthly">Monthly</option>
                            </select>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Closer Commission Start Date</label>
                            <input type="date" name="CloserCommissions[${index}].StartDate" class="form-control" placeholder="Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Closer Commission End Date</label>
                                <input type="date" name="CloserCommissions[${index}].EndDate" class="form-control" placeholder="End Date (Optional)" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="CloserCommissions[${index}].CommissionType" class="form-select" required>
                                <option value="">Commission Type *</option>
                                <option value="Duration">Duration</option>
                                <option value="Annual">Annual</option>
                                <option value="Residual">Residual</option>
                                <option value="As per System">As per System</option>
                            </select>
                        </div>
                    </div>
                </div>
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateLeadGeneratorCommissionFields(index) {
        return `
            <div class="commission-item" data-index="${index}">
                <input type="hidden" name="LeadGeneratorCommissions[${index}].Id" value="" />
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="LeadGeneratorCommissions[${index}].LeadGeneratorCommission" class="form-control" placeholder="Lead Generator Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="LeadGeneratorCommissions[${index}].CloserCommission" class="form-control" placeholder="Closer Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Lead Generator Commission Start Date</label>
                            <input type="date" name="LeadGeneratorCommissions[${index}].LeadGeneratorStartDate" class="form-control" placeholder="Lead Generator Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Lead Generator Commission End Date</label>
                                <input type="date" name="LeadGeneratorCommissions[${index}].LeadGeneratorEndDate" class="form-control" placeholder="Lead Generator End Date (Optional)" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Closer Commission Start Date</label>
                            <input type="date" name="LeadGeneratorCommissions[${index}].CloserStartDate" class="form-control" placeholder="Closer Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Closer Commission End Date</label>
                                <input type="date" name="LeadGeneratorCommissions[${index}].CloserEndDate" class="form-control" placeholder="Closer End Date (Optional)" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="LeadGeneratorCommissions[${index}].PaymentTerms" class="form-select" required>
                                <option value="">Payment Terms *</option>
                                <option value="Weekly">Weekly</option>
                                <option value="Monthly">Monthly</option>
                            </select>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="LeadGeneratorCommissions[${index}].CommissionType" class="form-select" required>
                                <option value="">Commission Type *</option>
                                <option value="Duration">Duration</option>
                                <option value="Annual">Annual</option>
                                <option value="Residual">Residual</option>
                                <option value="As per System">As per System</option>
                            </select>
                        </div>
                    </div>
                </div>
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateReferralPartnerCommissionFields(index) {
        return `
            <div class="commission-item" data-index="${index}">
                <input type="hidden" name="ReferralPartnerCommissions[${index}].Id" value="" />
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="ReferralPartnerCommissions[${index}].ReferralPartnerCommission" class="form-control" placeholder="Referral Partner Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="ReferralPartnerCommissions[${index}].BrokerageCommission" class="form-control" placeholder="Brokerage Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Referral Partner Commission Start Date</label>
                            <input type="date" name="ReferralPartnerCommissions[${index}].ReferralPartnerStartDate" class="form-control" placeholder="Referral Partner Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Referral Partner Commission End Date</label>
                                <input type="date" name="ReferralPartnerCommissions[${index}].ReferralPartnerEndDate" class="form-control" placeholder="Referral Partner End Date (Optional)" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Brokerage Commission Start Date</label>
                            <input type="date" name="ReferralPartnerCommissions[${index}].BrokerageStartDate" class="form-control" placeholder="Brokerage Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Brokerage Commission End Date</label>
                                <input type="date" name="ReferralPartnerCommissions[${index}].BrokerageEndDate" class="form-control" placeholder="Brokerage End Date (Optional)" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="ReferralPartnerCommissions[${index}].PaymentTerms" class="form-select" required>
                                <option value="">Payment Terms *</option>
                                <option value="Weekly">Weekly</option>
                                <option value="Monthly">Monthly</option>
                            </select>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="ReferralPartnerCommissions[${index}].CommissionType" class="form-select" required>
                                <option value="">Commission Type *</option>
                                <option value="Duration">Duration</option>
                                <option value="Annual">Annual</option>
                                <option value="Residual">Residual</option>
                                <option value="As per System">As per System</option>
                            </select>
                        </div>
                    </div>
                </div>
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateSubReferralFields(index) {
        return `
            <div class="subsection-item" data-index="${index}">
                <h5>Sub Referral ${index + 1}</h5>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubReferrals[${index}].SubReferralPartnerName" class="form-control" placeholder="Sub Referral Partner Name *" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="SubReferrals[${index}].Active" class="form-select">
                                <option value="true" selected>YES</option>
                                <option value="false">NO</option>
                            </select>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Referral Start Date</label>
                            <input type="date" name="SubReferrals[${index}].StartDate" class="form-control sub-start-date" placeholder="Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Referral End Date</label>
                                <input type="date" name="SubReferrals[${index}].EndDate" class="form-control sub-end-date" placeholder="End Date (Optional)" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="email" name="SubReferrals[${index}].Email" class="form-control" placeholder="Sub Referral Partner Email" />
                        </div>
                    </div>
                                            <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].Landline" class="form-control landline-input" placeholder="Sub Referral Partner Landline (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                            </div>
                        </div>
                </div>
                <div class="row">
                                            <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].Mobile" class="form-control mobile-input" placeholder="Sub Referral Partner Mobile (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                        </div>
                    </div>
                </div>

                <!-- Sub Referral Bank Details -->
                <div class="sub-bank-details">
                    <h6>Bank Details</h6>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.BankName" class="form-control" placeholder="Bank Name" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.BankBranchAddress" class="form-control" placeholder="Bank Branch Address" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.ReceiversAddress" class="form-control" placeholder="Receivers Address" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.AccountName" class="form-control" placeholder="Account Name" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.AccountSortCode" class="form-control" placeholder="Account Sort Code (6 digits)" maxlength="6" pattern="[0-9]{6}" title="Please enter exactly 6 digits" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.AccountNumber" class="form-control" placeholder="Account Number (8 digits)" maxlength="8" pattern="[0-9]{8}" title="Please enter exactly 8 digits" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.IBAN" class="form-control" placeholder="IBAN" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].BankDetails.SwiftCode" class="form-control" placeholder="Swift Code" />
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sub Referral Company Tax Info -->
                <div class="sub-company-tax">
                    <h6>Company Tax Information</h6>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].CompanyTaxInfo.CompanyRegistration" class="form-control" placeholder="Company Registration" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].CompanyTaxInfo.VATNumber" class="form-control" placeholder="VAT Number" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-12">
                            <div class="form-group">
                                <textarea name="SubReferrals[${index}].CompanyTaxInfo.Notes" class="form-control" placeholder="Notes" rows="2"></textarea>
                            </div>
                        </div>
                    </div>
                </div>
                
                <!-- Sub Referral Commission and Payment Details -->
                <div class="sub-commission-section">
                    <h6>Commission and Payment Details</h6>
                    <div class="sub-commission-container" data-subsection-index="${index}">
                        <div class="sub-commission-item" data-subsection-index="${index}" data-commission-index="0">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <input type="number" name="SubReferrals[${index}].Commissions[0].SubReferralCommission" class="form-control" placeholder="Sub Referral Partner Commission (%)" step="0.01" min="0" max="100" />
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <input type="number" name="SubReferrals[${index}].Commissions[0].ReferralPartnerCommission" class="form-control" placeholder="Referral Partner Commission (%)" step="0.01" min="0" max="100" />
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="form-label small text-muted">Sub Referral Commission Start Date</label>
                                        <input type="date" name="SubReferrals[${index}].Commissions[0].SubReferralStartDate" class="form-control" placeholder="Sub Referral Start Date" />
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="form-label small text-muted">Sub Referral Commission End Date</label>
                                        <input type="date" name="SubReferrals[${index}].Commissions[0].SubReferralEndDate" class="form-control" placeholder="Sub Referral End Date" />
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="form-label small text-muted">Referral Partner Commission Start Date</label>
                                        <input type="date" name="SubReferrals[${index}].Commissions[0].ReferralPartnerStartDate" class="form-control" placeholder="Referral Partner Start Date" />
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="form-label small text-muted">Referral Partner Commission End Date</label>
                                        <input type="date" name="SubReferrals[${index}].Commissions[0].ReferralPartnerEndDate" class="form-control" placeholder="Referral Partner End Date" />
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <select name="SubReferrals[${index}].Commissions[0].PaymentTerms" class="form-select">
                                            <option value="">Payment Terms</option>
                                            <option value="Weekly">Weekly</option>
                                            <option value="Monthly">Monthly</option>
                                            <option value="Quarterly">Quarterly</option>
                                            <option value="Annually">Annually</option>
                                        </select>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <select name="SubReferrals[${index}].Commissions[0].CommissionType" class="form-select">
                                            <option value="">Commission Type</option>
                                            <option value="Duration">Duration</option>
                                            <option value="Annual">Annual</option>
                                            <option value="Residual">Residual</option>
                                            <option value="As per System">As per System</option>
                                        </select>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <button type="button" class="btn btn-secondary btn-sm add-sub-commission" data-subsection-index="${index}">
                        +Add Commission
                    </button>
                </div>
                <div class="text-end mt-3">
                <button type="button" class="btn btn-danger btn-sm remove-subsection"><i class="fas fa-trash me-1"></i>Remove Subsection</button>
                </div>
            </div>
        `;
    }

    generateSubReferralCommissionFields(subsectionIndex, commissionIndex) {
        return `
            <div class="sub-commission-item" data-subsection-index="${subsectionIndex}" data-commission-index="${commissionIndex}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].SubReferralCommission" class="form-control" placeholder="Sub Referral Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].ReferralPartnerCommission" class="form-control" placeholder="Referral Partner Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Referral Commission Start Date</label>
                            <input type="date" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].SubReferralStartDate" class="form-control" placeholder="Sub Referral Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Referral Commission End Date</label>
                            <input type="date" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].SubReferralEndDate" class="form-control" placeholder="Sub Referral End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Referral Partner Commission Start Date</label>
                            <input type="date" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].ReferralPartnerStartDate" class="form-control" placeholder="Referral Partner Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Referral Partner Commission End Date</label>
                            <input type="date" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].ReferralPartnerEndDate" class="form-control" placeholder="Referral Partner End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].PaymentTerms" class="form-select">
                                <option value="">Payment Terms</option>
                                <option value="Weekly">Weekly</option>
                                <option value="Monthly">Monthly</option>
                                <option value="Quarterly">Quarterly</option>
                                <option value="Annually">Annually</option>
                            </select>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].CommissionType" class="form-select">
                                <option value="">Commission Type</option>
                                <option value="Duration">Duration</option>
                                <option value="Annual">Annual</option>
                                <option value="Residual">Residual</option>
                                <option value="As per System">As per System</option>
                            </select>
                        </div>
                    </div>
                </div>
                ${commissionIndex > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-sub-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateSubReferralCommissionFieldsForEdit(subsectionIndex, commissionIndex, commission) {
        return `
            <div class="sub-commission-item" data-subsection-index="${subsectionIndex}" data-commission-index="${commissionIndex}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].SubReferralCommission" value="${commission.SubReferralCommission || ''}" class="form-control" placeholder="Sub Referral Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].ReferralPartnerCommission" value="${commission.ReferralPartnerCommission || ''}" class="form-control" placeholder="Referral Partner Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Referral Commission Start Date</label>
                            <input type="date" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].SubReferralStartDate" value="${commission.SubReferralStartDate || ''}" class="form-control" placeholder="Sub Referral Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Referral Commission End Date</label>
                            <input type="date" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].SubReferralEndDate" value="${commission.SubReferralEndDate || ''}" class="form-control" placeholder="Sub Referral End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Referral Partner Commission Start Date</label>
                            <input type="date" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].ReferralPartnerStartDate" value="${commission.ReferralPartnerStartDate || ''}" class="form-control" placeholder="Referral Partner Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Referral Partner Commission End Date</label>
                            <input type="date" name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].ReferralPartnerEndDate" value="${commission.ReferralPartnerEndDate || ''}" class="form-control" placeholder="Referral Partner End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].PaymentTerms" class="form-select">
                                <option value="">Payment Terms</option>
                                <option value="Weekly" ${commission.PaymentTerms === 'Weekly' ? 'selected' : ''}>Weekly</option>
                                <option value="Monthly" ${commission.PaymentTerms === 'Monthly' ? 'selected' : ''}>Monthly</option>
                                <option value="Quarterly" ${commission.PaymentTerms === 'Quarterly' ? 'selected' : ''}>Quarterly</option>
                                <option value="Annually" ${commission.PaymentTerms === 'Annually' ? 'selected' : ''}>Annually</option>
                            </select>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="SubReferrals[${subsectionIndex}].Commissions[${commissionIndex}].CommissionType" class="form-select">
                                <option value="">Commission Type</option>
                                <option value="Duration" ${commission.CommissionType === 'Duration' ? 'selected' : ''}>Duration</option>
                                <option value="Annual" ${commission.CommissionType === 'Annual' ? 'selected' : ''}>Annual</option>
                                <option value="Residual" ${commission.CommissionType === 'Residual' ? 'selected' : ''}>Residual</option>
                                <option value="As per System" ${commission.CommissionType === 'As per System' ? 'selected' : ''}>As per System</option>
                            </select>
                        </div>
                    </div>
                </div>
                ${commissionIndex > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-sub-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateIntroducerCommissionFields(index) {
        return `
            <div class="commission-item" data-index="${index}">
                <input type="hidden" name="IntroducerCommissions[${index}].Id" value="" />
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="IntroducerCommissions[${index}].Commission" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="IntroducerCommissions[${index}].PaymentTerms" class="form-select" required>
                                <option value="">Payment Terms *</option>
                                <option value="Weekly">Weekly</option>
                                <option value="Monthly">Monthly</option>
                            </select>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Introducer Commission Start Date</label>
                            <input type="date" name="IntroducerCommissions[${index}].StartDate" class="form-control" placeholder="Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Introducer Commission End Date</label>
                                <input type="date" name="IntroducerCommissions[${index}].EndDate" class="form-control" placeholder="End Date (Optional)" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="IntroducerCommissions[${index}].CommissionType" class="form-select" required>
                                <option value="">Commission Type *</option>
                                <option value="Duration">Duration</option>
                                <option value="Annual">Annual</option>
                                <option value="Residual">Residual</option>
                                <option value="As per System">As per System</option>
                            </select>
                        </div>
                    </div>
                </div>
                ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateSubIntroducerFields(index) {
        return `
            <div class="subsection-item" data-index="${index}">
                <h5>Sub Introducer ${index + 1}</h5>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubIntroducers[${index}].SubIntroducerName" class="form-control" placeholder="Sub Introducer Name *" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubIntroducers[${index}].OfgemID" class="form-control" placeholder="Ofgem ID" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="SubIntroducers[${index}].Active" class="form-select">
                                <option value="true" selected>YES</option>
                                <option value="false">NO</option>
                            </select>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="email" name="SubIntroducers[${index}].Email" class="form-control" placeholder="Email" />
                        </div>
                    </div>
                </div>
                <div class="row">
                <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Introducer Start Date</label>
                            <input type="date" name="SubIntroducers[${index}].StartDate" class="form-control sub-start-date" placeholder="Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Introducer End Date</label>
                            <input type="date" name="SubIntroducers[${index}].EndDate" class="form-control sub-end-date" placeholder="End Date (Optional)" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubIntroducers[${index}].Landline" class="form-control landline-input" placeholder="Landline (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="text" name="SubIntroducers[${index}].Mobile" class="form-control mobile-input" placeholder="Mobile (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                        </div>
                    </div>
                </div>
                
                <!-- Sub Introducer Commission & Payment -->
                <div class="sub-commission-section">
                    <h6>Commission & Payment</h6>
                    <div class="sub-commission-container">
                        ${this.generateSubIntroducerCommissionFields(index, 0)}
                    </div>
                    <button type="button" class="btn btn-secondary btn-sm add-sub-commission" data-subsection-index="${index}">
                        <i class="fas fa-plus me-1"></i>Add Commission
                    </button>
                </div>

                <!-- Sub Introducer Bank Details -->
                <div class="sub-bank-details">
                    <h6>Bank Details</h6>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.BankName" class="form-control" placeholder="Bank Name" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.BankBranchAddress" class="form-control" placeholder="Bank Branch Address" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.ReceiversAddress" class="form-control" placeholder="Receivers Address" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.AccountName" class="form-control" placeholder="Account Name" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.AccountSortCode" class="form-control" placeholder="Account Sort Code (6 digits)" maxlength="6" pattern="[0-9]{6}" title="Please enter exactly 6 digits" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.AccountNumber" class="form-control" placeholder="Account Number (8 digits)" maxlength="8" pattern="[0-9]{8}" title="Please enter exactly 8 digits" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.IBAN" class="form-control" placeholder="IBAN" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].BankDetails.SwiftCode" class="form-control" placeholder="Swift Code" />
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sub Introducer Company Tax Info -->
                <div class="sub-company-tax">
                    <h6>Company Tax Information</h6>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].CompanyTaxInfo.CompanyRegistration" class="form-control" placeholder="Company Registration" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].CompanyTaxInfo.VATNumber" class="form-control" placeholder="VAT Number" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-12">
                            <div class="form-group">
                                <textarea name="SubIntroducers[${index}].CompanyTaxInfo.Notes" class="form-control" placeholder="Notes" rows="2"></textarea>
                            </div>
                        </div>
                    </div>
                </div>
                
                <div class="text-end mt-3">
                    <button type="button" class="btn btn-danger btn-sm remove-subsection"><i class="fas fa-trash me-1"></i>Remove Subsection</button>
                </div>
            </div>
        `;
    }

    generateSubIntroducerCommissionFields(subsectionIndex, commissionIndex) {
        return `
            <div class="sub-commission-item" data-subsection-index="${subsectionIndex}" data-commission-index="${commissionIndex}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].SubIntroducerCommission" class="form-control" placeholder="Sub Introducer Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].IntroducerCommission" class="form-control" placeholder="Introducer Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Introducer Commission Start Date</label>
                            <input type="date" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].SubIntroducerStartDate" class="form-control" placeholder="Sub Introducer Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Introducer Commission End Date</label>
                            <input type="date" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].SubIntroducerEndDate" class="form-control" placeholder="Sub Introducer End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Introducer Commission Start Date</label>
                            <input type="date" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].IntroducerStartDate" class="form-control" placeholder="Introducer Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Introducer Commission End Date</label>
                            <input type="date" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].IntroducerEndDate" class="form-control" placeholder="Introducer End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].PaymentTerms" class="form-select">
                                <option value="">Payment Terms</option>
                                <option value="Weekly">Weekly</option>
                                <option value="Monthly">Monthly</option>
                                <option value="Quarterly">Quarterly</option>
                                <option value="Annually">Annually</option>
                            </select>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].CommissionType" class="form-select">
                                <option value="">Commission Type</option>
                                <option value="Duration">Duration</option>
                                <option value="Annual">Annual</option>
                                <option value="Residual">Residual</option>
                                <option value="As per System">As per System</option>
                            </select>
                        </div>
                    </div>
                </div>
                ${commissionIndex > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-sub-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    generateSubIntroducerCommissionFieldsForEdit(subsectionIndex, commissionIndex, commission) {
        return `
            <div class="sub-commission-item" data-subsection-index="${subsectionIndex}" data-commission-index="${commissionIndex}">
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].SubIntroducerCommission" value="${commission.SubIntroducerCommission || ''}" class="form-control" placeholder="Sub Introducer Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <input type="number" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].IntroducerCommission" value="${commission.IntroducerCommission || ''}" class="form-control" placeholder="Introducer Commission (%) *" step="0.01" min="0" max="100" required />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Introducer Commission Start Date</label>
                            <input type="date" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].SubIntroducerStartDate" value="${commission.SubIntroducerStartDate || ''}" class="form-control" placeholder="Sub Introducer Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Sub Introducer Commission End Date</label>
                            <input type="date" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].SubIntroducerEndDate" value="${commission.SubIntroducerEndDate || ''}" class="form-control" placeholder="Sub Introducer End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Introducer Commission Start Date</label>
                            <input type="date" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].IntroducerStartDate" value="${commission.IntroducerStartDate || ''}" class="form-control" placeholder="Introducer Start Date" />
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label small text-muted">Introducer Commission End Date</label>
                            <input type="date" name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].IntroducerEndDate" value="${commission.IntroducerEndDate || ''}" class="form-control" placeholder="Introducer End Date" />
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].PaymentTerms" class="form-select">
                                <option value="">Payment Terms</option>
                                <option value="Weekly" ${commission.PaymentTerms === 'Weekly' ? 'selected' : ''}>Weekly</option>
                                <option value="Monthly" ${commission.PaymentTerms === 'Monthly' ? 'selected' : ''}>Monthly</option>
                                <option value="Quarterly" ${commission.PaymentTerms === 'Quarterly' ? 'selected' : ''}>Quarterly</option>
                                <option value="Annually" ${commission.PaymentTerms === 'Annually' ? 'selected' : ''}>Annually</option>
                            </select>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <select name="SubIntroducers[${subsectionIndex}].Commissions[${commissionIndex}].CommissionType" class="form-select">
                                <option value="">Commission Type</option>
                                <option value="Duration" ${commission.CommissionType === 'Duration' ? 'selected' : ''}>Duration</option>
                                <option value="Annual" ${commission.CommissionType === 'Annual' ? 'selected' : ''}>Annual</option>
                                <option value="Residual" ${commission.CommissionType === 'Residual' ? 'selected' : ''}>Residual</option>
                                <option value="As per System" ${commission.CommissionType === 'As per System' ? 'selected' : ''}>As per System</option>
                            </select>
                        </div>
                    </div>
                </div>
                ${commissionIndex > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-sub-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
            </div>
        `;
    }

    // Edit Mode Field Generation Methods
    generateBrokerageCommissionFieldsForEdit() {
        if (!this.isEditMode || !this.modelData || !this.modelData.BrokerageCommissions || this.modelData.BrokerageCommissions.length === 0) {
            return this.generateBrokerageCommissionFields(0);
        }

        let html = '';
        this.modelData.BrokerageCommissions.forEach((commission, index) => {
            html += `
                <div class="commission-item" data-index="${index}">
                    <input type="hidden" name="BrokerageCommissions[${index}].Id" value="" />
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="number" name="BrokerageCommissions[${index}].Commission" value="${commission.Commission || ''}" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <select name="BrokerageCommissions[${index}].PaymentTerms" class="form-select" required>
                                    <option value="">Payment Terms *</option>
                                    <option value="Weekly" ${commission.PaymentTerms === 'Weekly' ? 'selected' : ''}>Weekly</option>
                                    <option value="Monthly" ${commission.PaymentTerms === 'Monthly' ? 'selected' : ''}>Monthly</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Brokerage Commission Start Date</label>
                                <input type="date" name="BrokerageCommissions[${index}].StartDate" value="${commission.StartDate || ''}" class="form-control" placeholder="Start Date" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Brokerage Commission End Date</label>
                                <input type="date" name="BrokerageCommissions[${index}].EndDate" value="${commission.EndDate || ''}" class="form-control" placeholder="End Date" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <select name="BrokerageCommissions[${index}].CommissionType" class="form-select" required>
                                    <option value="">Commission Type *</option>
                                    <option value="Duration" ${commission.CommissionType === 'Duration' ? 'selected' : ''}>Duration</option>
                                    <option value="Annual" ${commission.CommissionType === 'Annual' ? 'selected' : ''}>Annual</option>
                                    <option value="Residual" ${commission.CommissionType === 'Residual' ? 'selected' : ''}>Residual</option>
                                    <option value="As per System" ${commission.CommissionType === 'As per System' ? 'selected' : ''}>As per System</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
                </div>
            `;
        });
        return html;
    }

    generateBrokerageStaffFieldsForEdit() {
        if (!this.isEditMode || !this.modelData || !this.modelData.BrokerageStaff || this.modelData.BrokerageStaff.length === 0) {
            return this.generateBrokerageStaffFields(0);
        }

        let html = '';
        this.modelData.BrokerageStaff.forEach((staff, index) => {
            html += `
                <div class="staff-item" data-index="${index}">
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="BrokerageStaff[${index}].BrokerageStaffName" value="${staff.BrokerageStaffName || ''}" class="form-control" placeholder="Staff Name *" required />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                                            <select name="BrokerageStaff[${index}].Active" class="form-select">
                                <option value="true" ${staff.Active ? 'selected' : ''}>YES</option>
                                <option value="false" ${!staff.Active ? 'selected' : ''}>NO</option>
                            </select>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Brokerage Staff Start Date</label>
                                <input type="date" name="BrokerageStaff[${index}].StartDate" value="${staff.StartDate || ''}" class="form-control" placeholder="Start Date" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Brokerage Staff End Date</label>
                                <input type="date" name="BrokerageStaff[${index}].EndDate" value="${staff.EndDate || ''}" class="form-control" placeholder="End Date" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="email" name="BrokerageStaff[${index}].Email" value="${staff.Email || ''}" class="form-control" placeholder="Email Address" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="BrokerageStaff[${index}].Landline" value="${staff.Landline || ''}" class="form-control landline-input" placeholder="Landline (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="BrokerageStaff[${index}].Mobile" value="${staff.Mobile || ''}" class="form-control mobile-input" placeholder="Mobile (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <textarea name="BrokerageStaff[${index}].Notes" class="form-control" placeholder="Notes" rows="2">${staff.Notes || ''}</textarea>
                            </div>
                        </div>
                    </div>
                    ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-staff"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
                </div>
            `;
        });
        return html;
    }

    generateSubBrokerageFieldsForEdit() {
        if (!this.isEditMode || !this.modelData || !this.modelData.SubBrokerages || this.modelData.SubBrokerages.length === 0) {
            return this.generateSubBrokerageFields(0);
        }

        let html = '';
        this.modelData.SubBrokerages.forEach((subBrokerage, index) => {
            html += `
                <div class="subsection-item" data-index="${index}">
                    <h5>Sub Brokerage ${index + 1}</h5>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].SubBrokerageName" value="${subBrokerage.SubBrokerageName || ''}" class="form-control" placeholder="Sub Brokerage Name *" required />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].OfgemID" value="${subBrokerage.OfgemID || ''}" class="form-control" placeholder="Ofgem ID" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                                            <select name="SubBrokerages[${index}].Active" class="form-select">
                                <option value="true" ${subBrokerage.Active ? 'selected' : ''}>YES</option>
                                <option value="false" ${!subBrokerage.Active ? 'selected' : ''}>NO</option>
                            </select>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="email" name="SubBrokerages[${index}].Email" value="${subBrokerage.Email || ''}" class="form-control" placeholder="Email" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Sub Brokerage Start Date</label>
                                <input type="date" name="SubBrokerages[${index}].StartDate" value="${subBrokerage.StartDate || ''}" class="form-control" placeholder="Start Date" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Sub Brokerage End Date</label>
                                <input type="date" name="SubBrokerages[${index}].EndDate" value="${subBrokerage.EndDate || ''}" class="form-control" placeholder="End Date" />
                            </div>
                        </div>
                        
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].Landline" value="${subBrokerage.Landline || ''}" class="form-control landline-input" placeholder="Landline (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubBrokerages[${index}].Mobile" value="${subBrokerage.Mobile || ''}" class="form-control mobile-input" placeholder="Mobile (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                            </div>
                        </div>
                    </div>
                    
                    <!-- Sub Brokerage Commission & Payment -->
                    <div class="sub-commission-section">
                        <h6>Commission & Payment</h6>
                        <div class="sub-commission-container">
                            ${this.generateSubBrokerageCommissionFieldsForEdit(index)}
                        </div>
                        <button type="button" class="btn btn-secondary btn-sm add-sub-commission" data-subsection-index="${index}">
                            <i class="fas fa-plus me-1"></i>Add Commission
                        </button>
                    </div>

                    <!-- Sub Brokerage Bank Details -->
                    <div class="sub-bank-details">
                        <h6>Bank Details</h6>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubBrokerages[${index}].BankDetails.BankName" value="${subBrokerage.BankDetails?.BankName || ''}" class="form-control" placeholder="Bank Name" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubBrokerages[${index}].BankDetails.BankBranchAddress" value="${subBrokerage.BankDetails?.BankBranchAddress || ''}" class="form-control" placeholder="Bank Branch Address" />
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubBrokerages[${index}].BankDetails.ReceiversAddress" value="${subBrokerage.BankDetails?.ReceiversAddress || ''}" class="form-control" placeholder="Receivers Address" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubBrokerages[${index}].BankDetails.AccountName" value="${subBrokerage.BankDetails?.AccountName || ''}" class="form-control" placeholder="Account Name" />
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubBrokerages[${index}].BankDetails.AccountSortCode" value="${subBrokerage.BankDetails?.AccountSortCode || ''}" class="form-control" placeholder="Account Sort Code (6 digits)" maxlength="6" pattern="[0-9]{6}" title="Please enter exactly 6 digits" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubBrokerages[${index}].BankDetails.AccountNumber" value="${subBrokerage.BankDetails?.AccountNumber || ''}" class="form-control" placeholder="Account Number (8 digits)" maxlength="8" pattern="[0-9]{8}" title="Please enter exactly 8 digits" />
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubBrokerages[${index}].BankDetails.IBAN" value="${subBrokerage.BankDetails?.IBAN || ''}" class="form-control" placeholder="IBAN" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubBrokerages[${index}].BankDetails.SwiftCode" value="${subBrokerage.BankDetails?.SwiftCode || ''}" class="form-control" placeholder="Swift Code" />
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Sub Brokerage Company Tax Info -->
                    <div class="sub-company-tax">
                        <h6>Company Tax Information</h6>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubBrokerages[${index}].CompanyTaxInfo.CompanyRegistration" value="${subBrokerage.CompanyTaxInfo?.CompanyRegistration || ''}" class="form-control" placeholder="Company Registration" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubBrokerages[${index}].CompanyTaxInfo.VATNumber" value="${subBrokerage.CompanyTaxInfo?.VATNumber || ''}" class="form-control" placeholder="VAT Number" />
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-12">
                                <div class="form-group">
                                    <textarea name="SubBrokerages[${index}].CompanyTaxInfo.Notes" class="form-control" placeholder="Notes" rows="2">${subBrokerage.CompanyTaxInfo?.Notes || ''}</textarea>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="text-end mt-3">
                    <button type="button" class="btn btn-danger btn-sm remove-subsection"><i class="fas fa-trash me-1"></i>Remove Subsection</button>
                </div>
                </div>
            `;
        });
        return html;
    }

    generateSubBrokerageCommissionFieldsForEdit(subsectionIndex) {
        if (!this.isEditMode || !this.modelData || !this.modelData.SubBrokerages || 
            !this.modelData.SubBrokerages[subsectionIndex] || 
            !this.modelData.SubBrokerages[subsectionIndex].Commissions || 
            this.modelData.SubBrokerages[subsectionIndex].Commissions.length === 0) {
            return this.generateSubBrokerageCommissionFields(subsectionIndex, 0);
        }

        let html = '';
        this.modelData.SubBrokerages[subsectionIndex].Commissions.forEach((commission, commissionIndex) => {
            html += `
                <div class="sub-commission-item" data-subsection-index="${subsectionIndex}" data-commission-index="${commissionIndex}">
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="number" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].SubBrokerageCommission" value="${commission.SubBrokerageCommission || ''}" class="form-control" placeholder="Sub Brokerage Commission (%) *" step="0.01" min="0" max="100" required />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="number" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].BrokerageCommission" value="${commission.BrokerageCommission || ''}" class="form-control" placeholder="Brokerage Commission (%) *" step="0.01" min="0" max="100" required />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Sub Brokerage Commission Start Date</label>
                                <input type="date" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].SubBrokerageStartDate" value="${commission.SubBrokerageStartDate || ''}" class="form-control" placeholder="Sub Brokerage Start Date" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Sub Brokerage Commission End Date</label>  
                                <input type="date" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].SubBrokerageEndDate" value="${commission.SubBrokerageEndDate || ''}" class="form-control" placeholder="Sub Brokerage End Date" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Brokerage Commission Start Date</label>
                                <input type="date" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].BrokerageStartDate" value="${commission.BrokerageStartDate || ''}" class="form-control" placeholder="Brokerage Start Date" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Brokerage Commission End Date</label>
                                <input type="date" name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].BrokerageEndDate" value="${commission.BrokerageEndDate || ''}" class="form-control" placeholder="Brokerage End Date" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <select name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].PaymentTerms" class="form-select">
                                    <option value="">Payment Terms</option>
                                    <option value="Weekly" ${commission.PaymentTerms === 'Weekly' ? 'selected' : ''}>Weekly</option>
                                    <option value="Monthly" ${commission.PaymentTerms === 'Monthly' ? 'selected' : ''}>Monthly</option>
                                    <option value="Quarterly" ${commission.PaymentTerms === 'Quarterly' ? 'selected' : ''}>Quarterly</option>
                                    <option value="Annually" ${commission.PaymentTerms === 'Annually' ? 'selected' : ''}>Annually</option>
                                </select>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <select name="SubBrokerages[${subsectionIndex}].Commissions[${commissionIndex}].CommissionType" class="form-select">
                                    <option value="">Commission Type</option>
                                    <option value="Duration" ${commission.CommissionType === 'Duration' ? 'selected' : ''}>Duration</option>
                                    <option value="Annual" ${commission.CommissionType === 'Annual' ? 'selected' : ''}>Annual</option>
                                    <option value="Residual" ${commission.CommissionType === 'Residual' ? 'selected' : ''}>Residual</option>
                                    <option value="As per System" ${commission.CommissionType === 'As per System' ? 'selected' : ''}>As per System</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    ${commissionIndex > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-sub-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
                </div>
            `;
        });
        return html;
    }

    generateCloserCommissionFieldsForEdit() {
        if (!this.isEditMode || !this.modelData || !this.modelData.CloserCommissions || this.modelData.CloserCommissions.length === 0) {
            return this.generateCloserCommissionFields(0);
        }

        let html = '';
        this.modelData.CloserCommissions.forEach((commission, index) => {
            html += `
                <div class="commission-item" data-index="${index}">
                    <input type="hidden" name="CloserCommissions[${index}].Id" value="${commission.Id || ''}" />
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="number" name="CloserCommissions[${index}].Commission" value="${commission.Commission || ''}" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <select name="CloserCommissions[${index}].PaymentTerms" class="form-select" required>
                                    <option value="">Payment Terms *</option>
                                    <option value="Weekly" ${commission.PaymentTerms === 'Weekly' ? 'selected' : ''}>Weekly</option>
                                    <option value="Monthly" ${commission.PaymentTerms === 'Monthly' ? 'selected' : ''}>Monthly</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Closer Commission Start Date</label>
                                <input type="date" name="CloserCommissions[${index}].StartDate" value="${commission.StartDate || ''}" class="form-control" placeholder="Start Date" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Closer Commission End Date</label>
                                <input type="date" name="CloserCommissions[${index}].EndDate" value="${commission.EndDate || ''}" class="form-control" placeholder="End Date" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <select name="CloserCommissions[${index}].CommissionType" class="form-select" required>
                                    <option value="">Commission Type *</option>
                                    <option value="Duration" ${commission.CommissionType === 'Duration' ? 'selected' : ''}>Duration</option>
                                    <option value="Annual" ${commission.CommissionType === 'Annual' ? 'selected' : ''}>Annual</option>
                                    <option value="Residual" ${commission.CommissionType === 'Residual' ? 'selected' : ''}>Residual</option>
                                    <option value="As per System" ${commission.CommissionType === 'As per System' ? 'selected' : ''}>As per System</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
                </div>
            `;
        });
        return html;
    }

    generateLeadGeneratorCommissionFieldsForEdit() {
        if (!this.isEditMode || !this.modelData || !this.modelData.LeadGeneratorCommissions || this.modelData.LeadGeneratorCommissions.length === 0) {
            return this.generateLeadGeneratorCommissionFields(0);
        }

        let html = '';
        this.modelData.LeadGeneratorCommissions.forEach((commission, index) => {
            html += `
                <div class="commission-item" data-index="${index}">
                    <input type="hidden" name="LeadGeneratorCommissions[${index}].Id" value="${commission.Id || ''}" />
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="number" name="LeadGeneratorCommissions[${index}].LeadGeneratorCommission" value="${commission.LeadGeneratorCommission || ''}" class="form-control" placeholder="Lead Generator Commission (%) *" step="0.01" min="0" max="100" required />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="number" name="LeadGeneratorCommissions[${index}].CloserCommission" value="${commission.CloserCommission || ''}" class="form-control" placeholder="Closer Commission (%) *" step="0.01" min="0" max="100" required />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Lead Generator Commission Start Date</label>
                                <input type="date" name="LeadGeneratorCommissions[${index}].LeadGeneratorStartDate" value="${commission.LeadGeneratorStartDate || ''}" class="form-control" placeholder="Lead Generator Start Date" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Lead Generator Commission End Date</label>
                                <input type="date" name="LeadGeneratorCommissions[${index}].LeadGeneratorEndDate" value="${commission.LeadGeneratorEndDate || ''}" class="form-control" placeholder="Lead Generator End Date" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Closer Commission Start Date</label>
                                <input type="date" name="LeadGeneratorCommissions[${index}].CloserStartDate" value="${commission.CloserStartDate || ''}" class="form-control" placeholder="Closer Start Date" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Closer Commission End Date</label>
                                <input type="date" name="LeadGeneratorCommissions[${index}].CloserEndDate" value="${commission.CloserEndDate || ''}" class="form-control" placeholder="Closer End Date" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <select name="LeadGeneratorCommissions[${index}].PaymentTerms" class="form-select" required>
                                    <option value="">Payment Terms *</option>
                                    <option value="Weekly" ${commission.PaymentTerms === 'Weekly' ? 'selected' : ''}>Weekly</option>
                                    <option value="Monthly" ${commission.PaymentTerms === 'Monthly' ? 'selected' : ''}>Monthly</option>
                                </select>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <select name="LeadGeneratorCommissions[${index}].CommissionType" class="form-select" required>
                                    <option value="">Commission Type *</option>
                                    <option value="Duration" ${commission.CommissionType === 'Duration' ? 'selected' : ''}>Duration</option>
                                    <option value="Annual" ${commission.CommissionType === 'Annual' ? 'selected' : ''}>Annual</option>
                                    <option value="Residual" ${commission.CommissionType === 'Residual' ? 'selected' : ''}>Residual</option>
                                    <option value="As per System" ${commission.CommissionType === 'As per System' ? 'selected' : ''}>As per System</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
                </div>
            `;
        });
        return html;
    }

    generateReferralPartnerCommissionFieldsForEdit() {
        if (!this.isEditMode || !this.modelData || !this.modelData.ReferralPartnerCommissions || this.modelData.ReferralPartnerCommissions.length === 0) {
            return this.generateReferralPartnerCommissionFields(0);
        }

        let html = '';
        this.modelData.ReferralPartnerCommissions.forEach((commission, index) => {
            html += `
                <div class="commission-item" data-index="${index}">
                    <input type="hidden" name="ReferralPartnerCommissions[${index}].Id" value="${commission.Id || ''}" />
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="number" name="ReferralPartnerCommissions[${index}].ReferralPartnerCommission" value="${commission.ReferralPartnerCommission || ''}" class="form-control" placeholder="Referral Partner Commission (%) *" step="0.01" min="0" max="100" required />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="number" name="ReferralPartnerCommissions[${index}].BrokerageCommission" value="${commission.BrokerageCommission || ''}" class="form-control" placeholder="Brokerage Commission (%) *" step="0.01" min="0" max="100" required />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Referral Partner Commission Start Date</label>
                                <input type="date" name="ReferralPartnerCommissions[${index}].ReferralPartnerStartDate" value="${commission.ReferralPartnerStartDate || ''}" class="form-control" placeholder="Referral Partner Start Date" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Referral Partner Commission End Date</label>
                                <input type="date" name="ReferralPartnerCommissions[${index}].ReferralPartnerEndDate" value="${commission.ReferralPartnerEndDate || ''}" class="form-control" placeholder="Referral Partner End Date" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Brokerage Commission Start Date</label>
                                <input type="date" name="ReferralPartnerCommissions[${index}].BrokerageStartDate" value="${commission.BrokerageStartDate || ''}" class="form-control" placeholder="Brokerage Start Date" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Brokerage Commission End Date</label>
                                <input type="date" name="ReferralPartnerCommissions[${index}].BrokerageEndDate" value="${commission.BrokerageEndDate || ''}" class="form-control" placeholder="Brokerage End Date" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <select name="ReferralPartnerCommissions[${index}].PaymentTerms" class="form-select" required>
                                    <option value="">Payment Terms *</option>
                                    <option value="Weekly" ${commission.PaymentTerms === 'Weekly' ? 'selected' : ''}>Weekly</option>
                                    <option value="Monthly" ${commission.PaymentTerms === 'Monthly' ? 'selected' : ''}>Monthly</option>
                                </select>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <select name="ReferralPartnerCommissions[${index}].CommissionType" class="form-select" required>
                                    <option value="">Commission Type *</option>
                                    <option value="Duration" ${commission.CommissionType === 'Duration' ? 'selected' : ''}>Duration</option>
                                    <option value="Annual" ${commission.CommissionType === 'Annual' ? 'selected' : ''}>Annual</option>
                                    <option value="Residual" ${commission.CommissionType === 'Residual' ? 'selected' : ''}>Residual</option>
                                    <option value="As per System" ${commission.CommissionType === 'As per System' ? 'selected' : ''}>As per System</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
                </div>
            `;
        });
        return html;
    }

    generateSubReferralFieldsForEdit() {
        if (!this.isEditMode || !this.modelData || !this.modelData.SubReferrals || this.modelData.SubReferrals.length === 0) {
            return this.generateSubReferralFields(0);
        }

        let html = '';
        this.modelData.SubReferrals.forEach((subReferral, index) => {
            html += `
                <div class="subsection-item" data-index="${index}">
                    <h5>Sub Referral ${index + 1}</h5>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].SubReferralPartnerName" value="${subReferral.SubReferralPartnerName || ''}" class="form-control" placeholder="Sub Referral Partner Name *" required />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                                            <select name="SubReferrals[${index}].Active" class="form-select">
                                <option value="true" ${subReferral.Active ? 'selected' : ''}>YES</option>
                                <option value="false" ${!subReferral.Active ? 'selected' : ''}>NO</option>
                            </select>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Sub Referral Start Date</label>
                                <input type="date" name="SubReferrals[${index}].StartDate" value="${subReferral.StartDate || ''}" class="form-control sub-start-date" placeholder="Start Date" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Sub Referral End Date</label>
                                <input type="date" name="SubReferrals[${index}].EndDate" value="${subReferral.EndDate || ''}" class="form-control sub-end-date" placeholder="End Date" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="email" name="SubReferrals[${index}].Email" value="${subReferral.Email || ''}" class="form-control" placeholder="Sub Referral Partner Email" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].Landline" value="${subReferral.Landline || ''}" class="form-control landline-input" placeholder="Sub Referral Partner Landline (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubReferrals[${index}].Mobile" value="${subReferral.Mobile || ''}" class="form-control mobile-input" placeholder="Sub Referral Partner Mobile (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                            </div>
                        </div>
                    </div>

                    <!-- Sub Referral Bank Details -->
                    <div class="sub-bank-details">
                        <h6>Bank Details</h6>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubReferrals[${index}].BankDetails.BankName" value="${subReferral.BankDetails?.BankName || ''}" class="form-control" placeholder="Bank Name" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubReferrals[${index}].BankDetails.BankBranchAddress" value="${subReferral.BankDetails?.BankBranchAddress || ''}" class="form-control" placeholder="Bank Branch Address" />
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubReferrals[${index}].BankDetails.ReceiversAddress" value="${subReferral.BankDetails?.ReceiversAddress || ''}" class="form-control" placeholder="Receivers Address" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubReferrals[${index}].BankDetails.AccountName" value="${subReferral.BankDetails?.AccountName || ''}" class="form-control" placeholder="Account Name" />
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubReferrals[${index}].BankDetails.AccountSortCode" value="${subReferral.BankDetails?.AccountSortCode || ''}" class="form-control" placeholder="Account Sort Code (6 digits)" maxlength="6" pattern="[0-9]{6}" title="Please enter exactly 6 digits" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubReferrals[${index}].BankDetails.AccountNumber" value="${subReferral.BankDetails?.AccountNumber || ''}" class="form-control" placeholder="Account Number (8 digits)" maxlength="8" pattern="[0-9]{8}" title="Please enter exactly 8 digits" />
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubReferrals[${index}].BankDetails.IBAN" value="${subReferral.BankDetails?.IBAN || ''}" class="form-control" placeholder="IBAN" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubReferrals[${index}].BankDetails.SwiftCode" value="${subReferral.BankDetails?.SwiftCode || ''}" class="form-control" placeholder="Swift Code" />
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Sub Referral Company Tax Info -->
                    <div class="sub-company-tax">
                        <h6>Company Tax Information</h6>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubReferrals[${index}].CompanyTaxInfo.CompanyRegistration" value="${subReferral.CompanyTaxInfo?.CompanyRegistration || ''}" class="form-control" placeholder="Company Registration" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubReferrals[${index}].CompanyTaxInfo.VATNumber" value="${subReferral.CompanyTaxInfo?.VATNumber || ''}" class="form-control" placeholder="VAT Number" />
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-12">
                                <div class="form-group">
                                    <textarea name="SubReferrals[${index}].CompanyTaxInfo.Notes" class="form-control" placeholder="Notes" rows="2">${subReferral.CompanyTaxInfo?.Notes || ''}</textarea>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Sub Referral Commission and Payment Details -->
                    <div class="sub-commission-section">
                        <h6>Commission & Payment</h6>
                        <div class="sub-commission-container" data-subsection-index="${index}">
                            ${subReferral.Commissions && subReferral.Commissions.length > 0 ? 
                                subReferral.Commissions.map((commission, commissionIndex) => 
                                    this.generateSubReferralCommissionFieldsForEdit(index, commissionIndex, commission)
                                ).join('') : 
                                this.generateSubReferralCommissionFieldsForEdit(index, 0, {})
                            }
                        </div>
                        <button type="button" class="btn btn-secondary btn-sm add-sub-commission" data-subsection-index="${index}">
                            +Add Commission
                        </button>
                    </div>
                    
                    <div class="text-end mt-3">
                    <button type="button" class="btn btn-danger btn-sm remove-subsection"><i class="fas fa-trash me-1"></i>Remove Subsection</button>
                </div>
                </div>
            `;
        });
        return html;
    }

    generateIntroducerCommissionFieldsForEdit() {
        if (!this.isEditMode || !this.modelData || !this.modelData.IntroducerCommissions || this.modelData.IntroducerCommissions.length === 0) {
            return this.generateIntroducerCommissionFields(0);
        }

        let html = '';
        this.modelData.IntroducerCommissions.forEach((commission, index) => {
            html += `
                <div class="commission-item" data-index="${index}">
                    <input type="hidden" name="IntroducerCommissions[${index}].Id" value="${commission.Id || ''}" />
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="number" name="IntroducerCommissions[${index}].Commission" value="${commission.Commission || ''}" class="form-control" placeholder="Commission (%) *" step="0.01" min="0" max="100" required />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <select name="IntroducerCommissions[${index}].PaymentTerms" class="form-select" required>
                                    <option value="">Payment Terms *</option>
                                    <option value="Weekly" ${commission.PaymentTerms === 'Weekly' ? 'selected' : ''}>Weekly</option>
                                    <option value="Monthly" ${commission.PaymentTerms === 'Monthly' ? 'selected' : ''}>Monthly</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Introducer Commission Start Date</label>
                                <input type="date" name="IntroducerCommissions[${index}].StartDate" value="${commission.StartDate || ''}" class="form-control" placeholder="Start Date" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Introducer Commission End Date</label>
                                <input type="date" name="IntroducerCommissions[${index}].EndDate" value="${commission.EndDate || ''}" class="form-control" placeholder="End Date" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <select name="IntroducerCommissions[${index}].CommissionType" class="form-select" required>
                                    <option value="">Commission Type *</option>
                                    <option value="Duration" ${commission.CommissionType === 'Duration' ? 'selected' : ''}>Duration</option>
                                    <option value="Annual" ${commission.CommissionType === 'Annual' ? 'selected' : ''}>Annual</option>
                                    <option value="Residual" ${commission.CommissionType === 'Residual' ? 'selected' : ''}>Residual</option>
                                    <option value="As per System" ${commission.CommissionType === 'As per System' ? 'selected' : ''}>As per System</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    ${index > 0 ? '<button type="button" class="btn btn-danger btn-sm remove-commission"><i class="fas fa-trash me-1"></i>Remove</button>' : ''}
                </div>
            `;
        });
        return html;
    }

    generateSubIntroducerFieldsForEdit() {
        if (!this.isEditMode || !this.modelData || !this.modelData.SubIntroducers || this.modelData.SubIntroducers.length === 0) {
            return this.generateSubIntroducerFields(0);
        }

        let html = '';
        this.modelData.SubIntroducers.forEach((subIntroducer, index) => {
            html += `
                <div class="subsection-item" data-index="${index}">
                    <h5>Sub Introducer ${index + 1}</h5>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].SubIntroducerName" value="${subIntroducer.SubIntroducerName || ''}" class="form-control" placeholder="Sub Introducer Name *" required />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].OfgemID" value="${subIntroducer.OfgemID || ''}" class="form-control" placeholder="Ofgem ID" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                                            <select name="SubIntroducers[${index}].Active" class="form-select">
                                <option value="true" ${subIntroducer.Active ? 'selected' : ''}>YES</option>
                                <option value="false" ${!subIntroducer.Active ? 'selected' : ''}>NO</option>
                            </select>
                            </div>
                        </div>
                       <div class="col-md-6">
                            <div class="form-group">
                                <input type="email" name="SubIntroducers[${index}].Email" value="${subIntroducer.Email || ''}" class="form-control" placeholder="Email" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                          <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Sub Introducer Start Date</label>
                                <input type="date" name="SubIntroducers[${index}].StartDate" value="${subIntroducer.StartDate || ''}" class="form-control" placeholder="Start Date" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label class="form-label small text-muted">Sub Introducer End Date</label>
                                <input type="date" name="SubIntroducers[${index}].EndDate" value="${subIntroducer.EndDate || ''}" class="form-control" placeholder="End Date" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].Landline" value="${subIntroducer.Landline || ''}" class="form-control landline-input" placeholder="Landline (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <input type="text" name="SubIntroducers[${index}].Mobile" value="${subIntroducer.Mobile || ''}" class="form-control mobile-input" placeholder="Mobile (11 digits)" maxlength="11" pattern="[0-9]{11}" title="Please enter exactly 11 digits" />
                            </div>
                        </div>
                    </div>

                    <!-- Sub Introducer Bank Details -->
                    <div class="sub-bank-details">
                        <h6>Bank Details</h6>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubIntroducers[${index}].BankDetails.BankName" value="${subIntroducer.BankDetails?.BankName || ''}" class="form-control" placeholder="Bank Name" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubIntroducers[${index}].BankDetails.BankBranchAddress" value="${subIntroducer.BankDetails?.BankBranchAddress || ''}" class="form-control" placeholder="Bank Branch Address" />
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubIntroducers[${index}].BankDetails.ReceiversAddress" value="${subIntroducer.BankDetails?.ReceiversAddress || ''}" class="form-control" placeholder="Receivers Address" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubIntroducers[${index}].BankDetails.AccountName" value="${subIntroducer.BankDetails?.AccountName || ''}" class="form-control" placeholder="Account Name" />
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubIntroducers[${index}].BankDetails.AccountSortCode" value="${subIntroducer.BankDetails?.AccountSortCode || ''}" class="form-control" placeholder="Account Sort Code (6 digits)" maxlength="6" pattern="[0-9]{6}" title="Please enter exactly 6 digits" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubIntroducers[${index}].BankDetails.AccountNumber" value="${subIntroducer.BankDetails?.AccountNumber || ''}" class="form-control" placeholder="Account Number (8 digits)" maxlength="8" pattern="[0-9]{8}" title="Please enter exactly 8 digits" />
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubIntroducers[${index}].BankDetails.IBAN" value="${subIntroducer.BankDetails?.IBAN || ''}" class="form-control" placeholder="IBAN" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubIntroducers[${index}].BankDetails.SwiftCode" value="${subIntroducer.BankDetails?.SwiftCode || ''}" class="form-control" placeholder="Swift Code" />
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Sub Introducer Company Tax Info -->
                    <div class="sub-company-tax">
                        <h6>Company Tax Information</h6>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubIntroducers[${index}].CompanyTaxInfo.CompanyRegistration" value="${subIntroducer.CompanyTaxInfo?.CompanyRegistration || ''}" class="form-control" placeholder="Company Registration" />
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <input type="text" name="SubIntroducers[${index}].CompanyTaxInfo.VATNumber" value="${subIntroducer.CompanyTaxInfo?.VATNumber || ''}" class="form-control" placeholder="VAT Number" />
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-12">
                                <div class="form-group">
                                    <textarea name="SubIntroducers[${index}].CompanyTaxInfo.Notes" class="form-control" placeholder="Notes" rows="2">${subIntroducer.CompanyTaxInfo?.Notes || ''}</textarea>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Sub Introducer Commission & Payment -->
                    <div class="sub-commission-section">
                        <h6>Commission & Payment</h6>
                        <div class="sub-commission-container">
                            ${subIntroducer.Commissions && subIntroducer.Commissions.length > 0 
                                ? subIntroducer.Commissions.map((commission, commissionIndex) => this.generateSubIntroducerCommissionFieldsForEdit(index, commissionIndex, commission)).join('') 
                                : this.generateSubIntroducerCommissionFields(index, 0)}
                        </div>
                        <button type="button" class="btn btn-secondary btn-sm add-sub-commission" data-subsection-index="${index}">
                            <i class="fas fa-plus me-1"></i>Add Commission
                        </button>
                    </div>
                    
                    <div class="text-end mt-3">
                    <button type="button" class="btn btn-danger btn-sm remove-subsection"><i class="fas fa-trash me-1"></i>Remove Subsection</button>
                </div>
                </div>
            `;
        });
        return html;
    }

    // Bind global event handlers for dynamically added elements
    bindGlobalEventHandlers() {
        const self = this; // Store reference to the class instance
        
        // Add Commission buttons with debouncing
        $(document).on('click', '.add-commission', function (e) {
            e.preventDefault();
            e.stopPropagation();
            
            const $button = $(this);
            
            // Prevent double-clicking by temporarily disabling the button
            if ($button.prop('disabled') || $button.hasClass('processing')) {
                return false;
            }
            
            // Disable button and add processing class
            $button.prop('disabled', true).addClass('processing');
            const originalText = $button.text();
            $button.text('Adding...');
            
            try {
                const type = $button.data('type');
            const container = self.getCommissionContainer(type);
            const newIndex = container.children().length;
            
                let newCommissionHtml = '';
            switch (type) {
                case 'brokerage':
                        newCommissionHtml = self.generateBrokerageCommissionFields(newIndex);
                    break;
                case 'closer':
                        newCommissionHtml = self.generateCloserCommissionFields(newIndex);
                    break;
                case 'leadGenerator':
                        newCommissionHtml = self.generateLeadGeneratorCommissionFields(newIndex);
                    break;
                case 'referralPartner':
                        newCommissionHtml = self.generateReferralPartnerCommissionFields(newIndex);
                    break;
                case 'introducer':
                        newCommissionHtml = self.generateIntroducerCommissionFields(newIndex);
                    break;
                }
                
                if (newCommissionHtml) {
                    container.append(newCommissionHtml);
                }
            } catch (error) {
                console.error('Error adding commission section:', error);
                if (typeof showToastError === 'function') {
                    showToastError('Failed to add commission section. Please try again.');
                }
            } finally {
                // Re-enable button after a short delay
                setTimeout(() => {
                    $button.prop('disabled', false)
                           .removeClass('processing')
                           .text(originalText);
                }, 500);
            }
        });

        // Add Staff buttons with debouncing
        $(document).on('click', '.add-staff', function (e) {
            e.preventDefault();
            e.stopPropagation();
            
            const $button = $(this);
            
            // Prevent double-clicking
            if ($button.prop('disabled') || $button.hasClass('processing')) {
                return false;
            }
            
            $button.prop('disabled', true).addClass('processing');
            const originalText = $button.text();
            $button.text('Adding...');
            
            try {
                const type = $button.data('type');
            const container = self.getStaffContainer(type);
            const newIndex = container.children().length;
            
                let newStaffHtml = '';
            if (type === 'brokerage') {
                    newStaffHtml = self.generateBrokerageStaffFields(newIndex);
                }
                
                if (newStaffHtml) {
                    container.append(newStaffHtml);
                }
            } catch (error) {
                console.error('Error adding staff section:', error);
                if (typeof showToastError === 'function') {
                    showToastError('Failed to add staff section. Please try again.');
                }
            } finally {
                setTimeout(() => {
                    $button.prop('disabled', false)
                           .removeClass('processing')
                           .text(originalText);
                }, 500);
            }
        });

        // Add Subsection buttons with debouncing
        $(document).on('click', '.add-subsection', function (e) {
            e.preventDefault();
            e.stopPropagation();
            
            const $button = $(this);
            
            // Prevent double-clicking
            if ($button.prop('disabled') || $button.hasClass('processing')) {
                return false;
            }
            
            $button.prop('disabled', true).addClass('processing');
            const originalText = $button.text();
            $button.text('Adding...');
            
            try {
                const type = $button.data('type');
            const container = self.getSubsectionContainer(type);
            const newIndex = container.children().length;
            
                let newSubsectionHtml = '';
            switch (type) {
                case 'subBrokerage':
                        newSubsectionHtml = self.generateSubBrokerageFields(newIndex);
                    break;
                case 'subReferral':
                        newSubsectionHtml = self.generateSubReferralFields(newIndex);
                    break;
                case 'subIntroducer':
                        newSubsectionHtml = self.generateSubIntroducerFields(newIndex);
                    break;
                }
                
                if (newSubsectionHtml) {
                    container.append(newSubsectionHtml);
                }
            } catch (error) {
                console.error('Error adding subsection:', error);
                if (typeof showToastError === 'function') {
                    showToastError('Failed to add subsection. Please try again.');
                }
            } finally {
                setTimeout(() => {
                    $button.prop('disabled', false)
                           .removeClass('processing')
                           .text(originalText);
                }, 500);
            }
        });

        // Add Sub Commission buttons with debouncing
        $(document).on('click', '.add-sub-commission', function (e) {
            e.preventDefault();
            e.stopPropagation();
            
            const $button = $(this);
            
            // Prevent double-clicking by temporarily disabling the button
            if ($button.prop('disabled') || $button.hasClass('processing')) {
                return false;
            }
            
            // Disable button and add processing class
            $button.prop('disabled', true).addClass('processing');
            const originalText = $button.text();
            $button.text('Adding...');
            
            try {
                const subsectionIndex = $button.data('subsection-index');
                const container = $button.siblings('.sub-commission-container');
            const newIndex = container.children().length;
            
            // Determine the type based on the parent section
                const parentSection = $button.closest('.subsection-item');
            const sectionType = parentSection.find('h5').text().toLowerCase();
            
                let newCommissionHtml = '';
            if (sectionType.includes('brokerage')) {
                    newCommissionHtml = self.generateSubBrokerageCommissionFields(subsectionIndex, newIndex);
            } else if (sectionType.includes('referral')) {
                    newCommissionHtml = self.generateSubReferralCommissionFields(subsectionIndex, newIndex);
            } else if (sectionType.includes('introducer')) {
                    newCommissionHtml = self.generateSubIntroducerCommissionFields(subsectionIndex, newIndex);
                }
                
                if (newCommissionHtml) {
                    container.append(newCommissionHtml);
                }
            } catch (error) {
                console.error('Error adding commission section:', error);
                if (typeof showToastError === 'function') {
                    showToastError('Failed to add commission section. Please try again.');
                }
            } finally {
                // Re-enable button after a short delay
                setTimeout(() => {
                    $button.prop('disabled', false)
                           .removeClass('processing')
                           .text(originalText);
                }, 500);
            }
        });

        // Remove buttons
        $(document).on('click', '.remove-commission', function () {
            $(this).closest('.commission-item').remove();
        });

        $(document).on('click', '.remove-staff', function () {
            $(this).closest('.staff-item').remove();
        });

        $(document).on('click', '.remove-subsection', function () {
            const $subsection = $(this).closest('.subsection-item');
            $subsection.remove();
        });

        $(document).on('click', '.remove-sub-commission', function () {
            $(this).closest('.sub-commission-item').remove();
        });

        // Initialize input validation
        this.initializeInputValidation();
        
        // Initialize date validation
        this.initializeDateValidation();
    }

    // Helper functions to get containers
    getCommissionContainer(type) {
        switch (type) {
            case 'brokerage': return $('#brokerageCommissionContainer');
            case 'closer': return $('#closerCommissionContainer');
            case 'leadGenerator': return $('#leadGeneratorCommissionContainer');
            case 'referralPartner': return $('#referralPartnerCommissionContainer');
            case 'introducer': return $('#introducerCommissionContainer');
            default: return null;
        }
    }

    getStaffContainer(type) {
        switch (type) {
            case 'brokerage': return $('#brokerageStaffContainer');
            default: return null;
        }
    }

    getSubsectionContainer(type) {
        switch (type) {
            case 'subBrokerage': return $('#subBrokerageContainer');
            case 'subReferral': return $('#subReferralContainer');
            case 'subIntroducer': return $('#subIntroducerContainer');
            default: return null;
        }
    }

    // Form validation
    validateForm() {
        let isValid = true;
        
        // Check required fields
        if (!$('#sectorType').val()) {
            this.showToastError('Please select a sector type.');
            isValid = false;
        }
        
        if (!$('input[name="Name"]').val().trim()) {
            this.showToastError('Please enter a sector name.');
            isValid = false;
        }
        
        return isValid;
    }

    // Toast notification functions
    showToastSuccess(message) {
        if (typeof showToastSuccess === 'function') {
            showToastSuccess(message);
        } else {
            // Fallback to console if toast function not available
            console.log('✓ ' + message);
        }
    }

    showToastError(message) {
        if (typeof showToastError === 'function') {
            showToastError(message);
        } else {
            // Fallback to console if toast function not available
            console.log('✗ ' + message);
        }
    }

    showToastWarning(message) {
        if (typeof showToastWarning === 'function') {
            showToastWarning(message);
        } else {
            // Fallback to console if toast function not available
            console.log('⚠ ' + message);
        }
    }

    initializeInputValidation() {
        // Bank Details Validation
        $(document).on('input', '#accountSortCode', function() {
            let value = $(this).val();
            // Remove non-digits
            value = value.replace(/\D/g, '');
            // Limit to 6 digits
            value = value.substring(0, 6);
            $(this).val(value);
        });

        $(document).on('input', '#accountNumber', function() {
            let value = $(this).val();
            // Remove non-digits
            value = value.replace(/\D/g, '');
            // Limit to 8 digits
            value = value.substring(0, 8);
            $(this).val(value);
        });

        // Landline and Mobile Validation
        $(document).on('input', '.landline-input, .mobile-input', function() {
            let value = $(this).val();
            // Remove non-digits
            value = value.replace(/\D/g, '');
            // Limit to 11 digits
            value = value.substring(0, 11);
            $(this).val(value);
        });

        // Prevent non-numeric input for numeric fields
        $(document).on('keypress', '#accountSortCode, #accountNumber, .landline-input, .mobile-input', function(e) {
            if (e.which < 48 || e.which > 57) {
                e.preventDefault();
            }
        });
    }

    initializeDateValidation() {
        // Handle start date changes for main sector dates
        $(document).on('blur', '#startDate', function() {
            const startDate = $(this).val();
            const endDateInput = $('#endDate');
            
            if (startDate) {
                endDateInput.attr('min', startDate);
                
                // Clear end date if it's now invalid
                if (endDateInput.val() && new Date(endDateInput.val()) <= new Date(startDate)) {
                    endDateInput.val('');
                    if (typeof showToastWarning === 'function') {
                        showToastWarning('End Date cleared because it was earlier than Start Date.');
                    }
                }
            } else {
                endDateInput.val('').removeAttr('min');
            }
        });
        
        // Handle end date changes for main sector dates
        $(document).on('blur', '#endDate', function() {
            const startDate = $('#startDate').val();
            const endDate = $(this).val();
            
            if (startDate && endDate && new Date(endDate) <= new Date(startDate)) {
                if (typeof showToastWarning === 'function') {
                    showToastWarning('End Date must be greater than Start Date.');
                }
                $(this).val('');
            }
        });

        // Handle dynamic date fields for commissions and staff
        $(document).on('blur', 'input[type="date"]', function() {
            const inputName = $(this).attr('name');
            
            // Check if this is a start date
            if (inputName && inputName.includes('StartDate')) {
                const startDate = $(this).val();
                const row = $(this).closest('.commission-item, .staff-item, .sub-commission-item, .subsection-item');
                
                // Find corresponding end date in the same row
                const endDateInput = row.find('input[type="date"]').filter(function() {
                    const name = $(this).attr('name');
                    return name && name.includes('EndDate') && 
                           name.replace('EndDate', '') === inputName.replace('StartDate', '');
                });
                
                if (endDateInput.length && startDate) {
                    endDateInput.attr('min', startDate);
                    
                    // Clear end date if it's now invalid
                    if (endDateInput.val() && new Date(endDateInput.val()) <= new Date(startDate)) {
                        endDateInput.val('');
                        if (typeof showToastWarning === 'function') {
                            showToastWarning('End Date cleared because it was earlier than Start Date.');
                        }
                    }
                }
            }
            
            // Check if this is an end date
            if (inputName && inputName.includes('EndDate')) {
                const endDate = $(this).val();
                const row = $(this).closest('.commission-item, .staff-item, .sub-commission-item, .subsection-item');
                
                // Find corresponding start date in the same row
                const startDateInput = row.find('input[type="date"]').filter(function() {
                    const name = $(this).attr('name');
                    return name && name.includes('StartDate') && 
                           name.replace('StartDate', '') === inputName.replace('EndDate', '');
                });
                
                if (startDateInput.length && startDateInput.val() && endDate) {
                    if (new Date(endDate) <= new Date(startDateInput.val())) {
                        if (typeof showToastWarning === 'function') {
                            showToastWarning('End Date must be greater than Start Date.');
                        }
                        $(this).val('');
                    }
                }
            }
        });

        // Add blur event handlers to validate date selection after user finishes typing
        $(document).on('blur', 'input[type="date"]', function() {
            const inputName = $(this).attr('name');
            
            if (inputName && inputName.includes('EndDate')) {
                const endDate = $(this).val();
                const row = $(this).closest('.commission-item, .staff-item, .sub-commission-item, .subsection-item');
                
                // Find corresponding start date
                const startDateInput = row.find('input[type="date"]').filter(function() {
                    const name = $(this).attr('name');
                    return name && name.includes('StartDate') && 
                           name.replace('StartDate', '') === inputName.replace('EndDate', '');
                });
                
                if (startDateInput.length && startDateInput.val() && endDate) {
                    const startDate = new Date(startDateInput.val());
                    const endDateObj = new Date(endDate);
                    
                    if (endDateObj <= startDate) {
                        // Reset to empty and show warning
                        $(this).val('');
                        if (typeof showToastWarning === 'function') {
                            showToastWarning('End Date must be greater than Start Date.');
                        }
                    }
                }
            }
        });

        // Add keydown event to prevent manual typing of invalid dates
        $(document).on('blur', 'input[type="date"]', function(e) {
            const inputName = $(this).attr('name');
            
            if (inputName && inputName.includes('EndDate')) {
                const row = $(this).closest('.commission-item, .staff-item, .sub-commission-item, .subsection-item');
                const startDateInput = row.find('input[type="date"]').filter(function() {
                    const name = $(this).attr('name');
                    return name && name.includes('StartDate') && 
                           name.replace('StartDate', '') === inputName.replace('EndDate', '');
                });
                
                if (startDateInput.length && startDateInput.val()) {
                    const startDate = new Date(startDateInput.val());
                    const today = new Date();
                    
                    // Prevent selecting dates before start date or before today
                    const minDate = new Date(Math.max(startDate, today));
                    const minDateStr = minDate.toISOString().split('T')[0];
                    
                    $(this).attr('min', minDateStr);
                }
            }
        });

        $(document).off('submit', '#editSectorForm, #createSectorForm');
        $(document).on('submit', '#editSectorForm, #createSectorForm', function (e) {

            e.preventDefault(); // Always prevent default form submission
            
            // Handle end date logic before form submission
            if (window.sectorFormManager) {
                window.sectorFormManager.handleEndDateLogic();
            }
            
            // Temporarily remove required attribute from hidden fields to prevent validation errors
            const hiddenRequiredFields = $('#additionalFieldsSection:hidden [required]');
            hiddenRequiredFields.prop('required', false);
            
            let hasDateErrors = false;
            
            // Check all date pairs
            $('input[type="date"]').each(function() {
                const inputName = $(this).attr('name');
                
                if (inputName && inputName.includes('EndDate')) {
                    const endDate = $(this).val();
                    const row = $(this).closest('.commission-item, .staff-item, .sub-commission-item, .subsection-item');
                    
                    const startDateInput = row.find('input[type="date"]').filter(function() {
                        const name = $(this).attr('name');
                        return name && name.includes('StartDate') && 
                               name.replace('StartDate', '') === inputName.replace('EndDate', '');
                    });
                    
                    if (startDateInput.length && startDateInput.val() && endDate) {
                        if (new Date(endDate) <= new Date(startDateInput.val())) {
                            hasDateErrors = true;
                            $(this).addClass('is-invalid');
                            
                            if (typeof showToastError === 'function') {
                                showToastError('End Date must be greater than Start Date for ' + inputName);
                            }
                        } else {
                            $(this).removeClass('is-invalid');
                        }
                    }
                }
            });
            
            if (hasDateErrors) {
                if (typeof showToastError === 'function') {
                    showToastError('Please fix the date validation errors before submitting.');
                }
                return false;
            }
            
            // If validation passes, submit via AJAX
            const $form = $(this);
            const formData = new FormData(this);
            const submitButton = $form.find('button[type="submit"]');
            
            // Disable submit button to prevent double submission
            submitButton.prop('disabled', true);
            
            // Show loading state
            const originalText = submitButton.text();
            submitButton.text('Processing...');
            
            $.ajax({
                url: $form.attr('action'),
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                success: function(response) {
                    if (response.success) {
                        // Show success toast
                        if (typeof showToastSuccess === 'function') {
                            showToastSuccess(response.message);
                        } else {
                            alert(response.message);
                        }
                        
                        // Redirect after a short delay to show the toast
                        setTimeout(function() {
                            if (response.redirectUrl) {
                                window.location.href = response.redirectUrl;
                            }
                        }, 1500);
                    } else {
                        // Show error toast
                        const errorMessage = response.message || 'An error occurred';
                        if (typeof showToastError === 'function') {
                            showToastError(errorMessage);
                        } else {
                            alert(errorMessage);
                        }
                        
                        // Log detailed errors if available
                        if (response.errors && response.errors.length > 0) {
                            console.error('Validation errors:', response.errors);
                        }
                    }
                },
                error: function(xhr, status, error) {
                    console.error('AJAX Error:', error);
                    console.error('Response:', xhr.responseText);
                    
                    if (typeof showToastError === 'function') {
                        showToastError('A network error occurred. Please try again.');
                    } else {
                        alert('A network error occurred. Please try again.');
                    }
                },
                complete: function() {
                    // Re-enable submit button and restore text
                    submitButton.prop('disabled', false);
                    submitButton.text(originalText);
                    
                    // Restore required attributes for fields that should be required
                    const currentSectorType = $('#sectorType').val();
                    if (currentSectorType === 'Brokerage' || currentSectorType === 'Introducer') {
                        $('#department').prop('required', true);
                    }
                }
            });
        });
    }

    populateDepartmentDropdown() {
        // Check if DropdownOptions is available and has department data
        if (typeof DropdownOptions !== 'undefined' && DropdownOptions.department) {
            const departmentSelect = $('#department');
            if (departmentSelect.length > 0) {
                // Clear all existing options
                departmentSelect.empty();
                
                // Create array with initial option and department options
                const allOptions = [
                    { value: '', text: 'Select Department *' },
                    ...DropdownOptions.department.map(dept => ({ value: dept, text: dept }))
                ];
                
                // Add all options to the dropdown
                allOptions.forEach(option => {
                    departmentSelect.append(`<option value="${option.value}">${option.text}</option>`);
                });
                
                // Handle selection based on mode and data availability
                if (this.isEditMode && this.modelData && this.modelData.Department) {
                    // Edit mode: Set to existing department value
                    departmentSelect.val(this.modelData.Department);
                } else {
                    departmentSelect.val('');
                }
                
                // Set initial required state based on current sector type
                const currentSectorType = $('#sectorType').val();
                if (currentSectorType === 'Brokerage' || currentSectorType === 'Introducer') {
                    departmentSelect.prop('required', true);
                } else {
                    departmentSelect.prop('required', false);
                }
            }
        }
    }

    /**
     * Populates the sector suppliers dropdown with active suppliers
     * Only called when sector type is 'Brokerage'
     */
    populateSuppliersDropdown() {
        // Add safety check and logging
        
        if (this.currentSectorType === 'Brokerage') {
            $.get('/Supplier/GetActiveSuppliersForDropdown', (res) => {
                if (res.success && res.Data.length > 0) {
                    this.suppliers = res.Data;
                    const $supplierSelect = $('#sectorSuppliers');
                    $supplierSelect.empty();
                    
                    // Create SelectList items for the dropdown
                    const selectList = res.Data.map(supplier => 
                        new Option(supplier.Name, supplier.Id, false, false)
                    );
                    
                    // Add options to the dropdown
                    $supplierSelect.append(selectList);
                    
                    // IMPORTANT: Reinitialize Select2 after adding options
                    $supplierSelect.select2('destroy').select2({
                        placeholder: "Select suppliers",
                        allowClear: true,
                        width: '100%',
                        dropdownAutoWidth: true,
                        dropdownParent: $('.sector-page-container')
                    });
                    
                    // Set selected values in edit mode
                    if (this.isEditMode && this.modelData && this.modelData.SectorSuppliers) {
                        $supplierSelect.val(this.modelData.SectorSuppliers).trigger('change');
                    }
                    
                } else {
                    console.warn('No suppliers data received:', res);
                }
            }).fail((xhr, status, error) => {
                console.error('Failed to fetch suppliers:', error);
                console.error('Response:', xhr.responseText);
            });
        }
    }

    /**
     * Initializes Select2 for all multiple select dropdowns
     */
    initializeSelect2() {
        $('select[multiple]').select2({
            placeholder: "Select option(s)",
            allowClear: true,
            width: '100%',
            dropdownAutoWidth: true,
            dropdownParent: $('.sector-page-container')
        });
    }

    /**
     * Handles end date logic for form submission
     * In create mode, empty end dates will be set to MAX DATE by backend
     * In edit mode, user-selected dates are preserved
     */
    handleEndDateLogic() {
        if (!this.isEditMode) {
            // Create mode: Clear all empty end dates so backend can set MAX DATE
            $('input[type="date"][name*="EndDate"]').each(function() {
                if (!$(this).val() || $(this).val().trim() === '') {
                    $(this).val(''); // Ensure empty value for backend processing
                }
            });
        }
        // Edit mode: Keep user-selected values as-is
    }

    initializeDuplicateAccountChecker() {
        // Check if DuplicateAccountChecker is available
        if (typeof DuplicateAccountChecker === 'undefined') {
            console.warn('DuplicateAccountChecker not loaded');
            return;
        }

        // Initialize duplicate account checker for all account number fields
        // This selector will catch:
        // 1. Main sector account number: #accountNumber or input[name*="BankDetails.AccountNumber"]
        // 2. All subsection account numbers (current and dynamically added)
        DuplicateAccountChecker.init({
            accountInputSelector: 'input[name*="AccountNumber"], #accountNumber',
            modalId: 'duplicateAccountModalSector',
            loaderSelector: '.account-loader', // Optional loader
            controllerEndpoint: 'CheckDuplicateBankAccount', // Use the specific bank account endpoint
            showErrorToast: true,
            fields: [
                { displayName: 'Bank Name', dataProperty: 'BankName' },
                { displayName: 'Account Name', dataProperty: 'AccountName' },
                { displayName: 'Account Number', dataProperty: 'AccountNumber' },
                { displayName: 'Sort Code', dataProperty: 'AccountSortCode' },
                { displayName: 'Branch Address', dataProperty: 'BankBranchAddress' },
                { displayName: 'IBAN', dataProperty: 'IBAN' },
                { displayName: 'Swift Code', dataProperty: 'SwiftCode' }
            ]
        });
    }
}

// Global instance for backward compatibility
window.sectorFormManager = null;

// Initialize function for Create page
window.initializeCreateSector = function() {
    window.sectorFormManager = new SectorFormManager({ isEditMode: false });
};

// Initialize function for Edit page
window.initializeEditSector = function(modelData) {
    window.sectorFormManager = new SectorFormManager({ 
        isEditMode: true, 
        modelData: modelData 
    });
    
    // Initialize conditional fields display based on current sector type
    if (modelData && modelData.SectorType) {
        window.sectorFormManager.toggleConditionalFields(modelData.SectorType);
    }
};

