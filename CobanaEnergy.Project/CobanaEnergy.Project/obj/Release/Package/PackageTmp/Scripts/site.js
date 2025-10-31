// site.js
// Automatically include anti-forgery token in all Ajax requests
$(document).ready(async function () {
    var token = $('input[name="__RequestVerificationToken"]').val();
    if (token) {
        $.ajaxSetup({
            headers: {
                'RequestVerificationToken': token
            }
        });
    }

    $(document).ajaxError(function (e, xhr) {
        if (xhr.status === 401) {
            showToastWarning("Your session has expired. Redirecting to login...");
            setTimeout(() => {
                window.location.href = '/Account/Login';
            }, 1500);
        }
    });

    window.validateUpliftAgainstSupplierLimit = async function ($upliftInput, $supplierSelect, fuelType) {
        const upliftVal = parseFloat($upliftInput.val());
        const supplierId = $supplierSelect.val();
        const token = $('input[name="__RequestVerificationToken"]').val();

        if (!$upliftInput.length || !$supplierSelect.length || !supplierId || isNaN(upliftVal)) {
            return true;
        }

        try {
            const res = await $.ajax({
                url: `/Supplier/GetActiveUpliftForSupplier?supplierId=${supplierId}&fuelType=${fuelType}`,
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'RequestVerificationToken': token
                }
            });

            if (res.success && res.Data != null) {
                const maxUplift = parseFloat(res.Data);

                const upliftFixed = Number(upliftVal.toFixed(6));
                const maxFixed = Number(maxUplift.toFixed(6));

                if (upliftFixed > maxFixed) {
                    showToastWarning(`Entered uplift (${upliftFixed}) exceeds max allowed (${maxFixed}) for this supplier.`);
                    $upliftInput.addClass('is-invalid');
                    return false;
                } else {
                    $upliftInput.removeClass('is-invalid');
                }
            }

            return true;
        } catch (err) {
            showToastError("Could not validate uplift limit.");
            return true;
        }
    };

    window.validateUpliftAgainstSupplierLimitElectric = async function ($upliftInput, $supplierSelect, eid) {
        const upliftVal = parseFloat($upliftInput.val());
        const token = $('input[name="__RequestVerificationToken"]').val();

        if (!$upliftInput.length || !$supplierSelect.length || !eid || isNaN(upliftVal)) {
            return true;
        }

        try {
            const res = await $.ajax({
                url: `/Supplier/GetSnapshotMaxUpliftElectric?eid=${eid}`,
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'RequestVerificationToken': token
                }
            });

            if (res.success && res.Data != null) {
                const maxUplift = parseFloat(res.Data);

                const upliftFixed = Number(upliftVal.toFixed(6));
                const maxFixed = Number(maxUplift.toFixed(6));

                if (upliftFixed > maxFixed) {
                    showToastWarning(`Entered uplift (${upliftFixed}) exceeds max allowed (${maxFixed}) for this contract snapshot.`);
                    $upliftInput.addClass('is-invalid');
                    return false;
                } else {
                    $upliftInput.removeClass('is-invalid');
                }
            }

            return true;
        } catch (err) {
            showToastError("Could not validate uplift limit from snapshot.");
            return true;
        }
    };

    window.validateUpliftAgainstSupplierLimitGas = async function ($upliftInput, $supplierSelect, eid) {
        const upliftVal = parseFloat($upliftInput.val());
        const token = $('input[name="__RequestVerificationToken"]').val();

        if (!$upliftInput.length || !$supplierSelect.length || !eid || isNaN(upliftVal)) {
            return true;
        }

        try {
            const res = await $.ajax({
                url: `/Supplier/GetSnapshotMaxUpliftGas?eid=${eid}`,
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'RequestVerificationToken': token
                }
            });

            if (res.success && res.Data != null) {
                const maxUplift = parseFloat(res.Data);

                const upliftFixed = Number(upliftVal.toFixed(6));
                const maxFixed = Number(maxUplift.toFixed(6));

                if (upliftFixed > maxFixed) {
                    showToastWarning(`Entered uplift (${upliftFixed}) exceeds max allowed (${maxFixed}) for this contract snapshot.`);
                    $upliftInput.addClass('is-invalid');
                    return false;
                } else {
                    $upliftInput.removeClass('is-invalid');
                }
            }

            return true;
        } catch (err) {
            showToastError("Could not validate uplift limit from snapshot.");
            return true;
        }
    };

    //Invoice Supplier Dashboard Popup
    $(document).on('click', '#openInvoiceSupplierDashboard', function (e) {
        e.preventDefault();

        $.ajax({
            url: '/InvoiceSupplierDashboard/InvoiceSupplierPopup',
            type: 'GET',
            success: function (html) {
                $('body').append(html);
                $('#invoiceUploadModal').modal('show');

                $('#invoiceUploadModal').on('hidden.bs.modal', function () {
                    $(this).remove();
                });
            },
            error: function () {
                showToastError("Failed to load Invoice Supplier popup.");
            }
        });
    });

    //nav
    $(document).ready(function () {
        $(document).on('click', '.dropdown-submenu > a', function (e) {
            e.preventDefault();
            e.stopPropagation();

            var $parentDropdown = $(this).closest('.dropdown-menu');
            var $submenu = $(this).next('.dropdown-menu');

            $parentDropdown.find('.dropdown-submenu .dropdown-menu').not($submenu).removeClass('show').hide();
            $submenu.toggleClass('show').toggle();
        });

        $('.dropdown-submenu').on('mouseenter', function () {
            $(this).children('.dropdown-menu').addClass('show').stop(true, true).fadeIn(150);
        }).on('mouseleave', function () {
            $(this).children('.dropdown-menu').removeClass('show').stop(true, true).fadeOut(150);
        });
        $(document).on('click', '.dropdown-submenu .dropdown-item', function (e) {
            e.stopPropagation();
        });
    });


});

/**
 * Enable Excel-like column resizing for DataTables
 * @param {string} tableSelector - jQuery selector for the table
 * 
 * Usage: Call this function after initializing your DataTable
 * Example: 
 *   var table = $('#myTable').DataTable({ ... });
 *   enableColumnResizing('#myTable');
 */
window.enableColumnResizing = function(tableSelector) {
    const $table = $(tableSelector);
    const $headers = $table.find('thead th');
    
    let resizing = false;
    let startX = 0;
    let startWidth = 0;
    let $currentHeader = null;
    let columnIndex = -1;
    let justFinishedResizing = false;
    let wasDragging = false;

    // Prevent sort click after resize OR when clicking in resize zone
    $headers.on('click', function(e) {
        if (justFinishedResizing) {
            e.stopImmediatePropagation();
            e.preventDefault();
            justFinishedResizing = false;
            return false;
        }
        
        // Also prevent sort if clicking directly in the resize zone
        const rect = this.getBoundingClientRect();
        const offsetX = e.clientX - rect.left;
        const isResizeZone = rect.width - offsetX <= 15;
        
        if (isResizeZone) {
            e.stopImmediatePropagation();
            e.preventDefault();
            return false;
        }
    });

    // Add mousedown event to header cells
    $headers.each(function(index) {
        const $th = $(this);
        
        // Skip last column (no resize handle)
        if (index === $headers.length - 1) return;

        $th.on('mousedown', function(e) {
            // Check if clicking near the right edge (resize zone)
            const rect = this.getBoundingClientRect();
            const offsetX = e.clientX - rect.left;
            const isResizeZone = rect.width - offsetX <= 15;

            if (isResizeZone) {
                e.preventDefault();
                e.stopPropagation();
                
                resizing = true;
                wasDragging = false;
                startX = e.pageX;
                startWidth = $th.outerWidth();
                $currentHeader = $th;
                columnIndex = $th.index();
                
                $table.addClass('is-resizing');
                $('body').css('cursor', 'col-resize');
                $('body').css('user-select', 'none');
            }
        });
    });

    // Global mousemove event
    $(document).on('mousemove', function(e) {
        if (!resizing) return;

        e.preventDefault();
        const diff = e.pageX - startX;
        
        // If mouse moved more than 3px, consider it a drag
        if (Math.abs(diff) > 3) {
            wasDragging = true;
        }
        
        // Calculate minimum width based on padding
        const headerPaddingLeft = parseInt($currentHeader.css('padding-left')) || 0;
        const headerPaddingRight = parseInt($currentHeader.css('padding-right')) || 0;
        const totalPadding = headerPaddingLeft + headerPaddingRight;
        const minWidth = Math.max(30, totalPadding + 10); // Minimum 30px total width
        
        const newWidth = Math.max(minWidth, startWidth + diff);
        
        // Update header width
        $currentHeader.css('width', newWidth + 'px');
        $currentHeader.css('min-width', newWidth + 'px');
        $currentHeader.css('max-width', newWidth + 'px');

        // Update corresponding body cells
        $table.find('tbody tr').each(function() {
            $(this).find('td').eq(columnIndex).css({
                'width': newWidth + 'px',
                'min-width': newWidth + 'px',
                'max-width': newWidth + 'px'
            });
        });
    });

    // Global mouseup event
    $(document).on('mouseup', function(e) {
        if (resizing) {
            resizing = false;
            $table.removeClass('is-resizing');
            $('body').css('cursor', '');
            $('body').css('user-select', '');
            
            // If we were actually dragging, prevent the next click
            if (wasDragging) {
                justFinishedResizing = true;
                // Reset the flag after a short delay as a safety measure
                setTimeout(function() {
                    justFinishedResizing = false;
                }, 300);
            }
            
            $currentHeader = null;
            columnIndex = -1;
            wasDragging = false;
        }
    });

    // Change cursor on hover over resize zone
    $headers.on('mousemove', function(e) {
        if (resizing) return;
        
        const rect = this.getBoundingClientRect();
        const offsetX = e.clientX - rect.left;
        const isResizeZone = rect.width - offsetX <= 15;
        
        if (isResizeZone && $(this).index() !== $headers.length - 1) {
            $(this).css('cursor', 'col-resize');
        } else {
            $(this).css('cursor', '');
        }
    });

    $headers.on('mouseleave', function() {
        if (!resizing) {
            $(this).css('cursor', '');
        }
    });

    // Add title tooltips to cells with truncated text for better UX
    addTitleTooltips($table);
};

/**
 * Add native browser tooltips (title attribute) to table cells
 * Shows full text on hover when content is truncated
 * @param {jQuery} $table - The table jQuery object
 */
function addTitleTooltips($table) {
    $table.find('tbody td').each(function() {
        const $cell = $(this);
        const text = $cell.text().trim();
        
        // Only add title if text is not empty and potentially truncated
        if (text && text.length > 20) {
            $cell.attr('title', text);
        }
    });
    
    // Re-add tooltips when table is redrawn (pagination, sorting, etc.)
    if ($.fn.DataTable && $.fn.DataTable.isDataTable($table)) {
        $table.on('draw.dt', function() {
            $table.find('tbody td').each(function() {
                const $cell = $(this);
                const text = $cell.text().trim();
                
                if (text && text.length > 20) {
                    $cell.attr('title', text);
                }
            });
        });
    }
}