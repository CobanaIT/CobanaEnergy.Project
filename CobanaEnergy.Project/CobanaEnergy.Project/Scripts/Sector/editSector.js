/**
 * Edit Sector Page - Simplified JavaScript
 * Data is now loaded server-side, JavaScript only handles form management
 */

$(document).ready(function () {
    // Initialize the consolidated sector form manager
    // The form submission is now handled by sectorFormManager.js to avoid double requests
    window.initializeEditSector();
});
