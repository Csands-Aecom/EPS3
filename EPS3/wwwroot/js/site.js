// ready function initializes all pages, runs common scripts
$(document).ready(function () {
    initForms(); 
    initContractControls();
    addDialogs();
    showHideButtons();
    addToDoList();
});

function initForms() {

    //initialize DataTable to be filterable, sortable, and searchable
    //sort by encumbrance ID, descending
    $('[id^=indexTable]').DataTable({ "order": [[1, "desc"]], "pageLength": 50});

    // support for NavBar submenus
    $('ul.dropdown-menu [data-toggle=dropdown]').on('click', function (event) {
        event.preventDefault();
        event.stopPropagation();
        $(this).parent().siblings().removeClass('open');
        $(this).parent().toggleClass('open');
    });

    //hover image for hamburger menu
    $(function () {
        $("#hamburger")
            .mouseover(function () {
                var src = $(this).attr("src").replace("images/Menu_default.png","images/Menu_focus.png");
                $(this).attr("src", src);
            })
            .mouseout(function () {
                var src = $(this).attr("src").replace("images/Menu_focus.png","images/Menu_default.png");
                $(this).attr("src", src);
            });
    });

    //sort messages in descending order
    $('#messageTable').DataTable({
        "order": [[ 0, 'desc']]
    });

    //initialize tabs
    $("#tabs").tabs();

    // make EncumbrancePanel header above EncumbrancePanelBody
    //var panelIndex = $("#EncumbrancePanelBody").css("z-index");
    $("#EncumbrancePanelHeading").css("z-index", "200");
    $("#ContractSelector").css("z-index", "102");
    // TODO: This fails because zIndex is "auto". To make it work, .css has to set zIndex to a numeric value
    // I can add numeric zIndex and manipulate accordingly, but will require much testing.
    //$("#ContractSelector").css("zIndex", $("#EncumbrancePanelBody").css("zIndex") + 1);

    //initialize datepickers
    $(function () {
        $(".datepicker").datepicker({
            changeMonth: true,
            changeYear: true,
            /* fix buggy IE focus functionality */
            fixFocusIE: false,

            /* blur needed to correctly handle placeholder text */
            onSelect: function (dateText, inst) {
                this.fixFocusIE = true;
                $(this).blur().change().focus();
            },
            onClose: function (dateText, inst) {
                this.fixFocusIE = true;
                this.focus();
            }
        });
    });

    //repopulate selection fields on Back
    if ($("#CategorySelector").val() !== "" && $("#CategoryID").val() === "") {
        $("#CategorySelector").val("");
    }
    if ($("#FundSelector").val() !== "" && $("#FundID").val() === "") {
        $("#FundSelector").val("");
    }

    // show Encumbrance Type and Amount
    displayLineItemsPanelOrMessage();

    // make header rows collapse detail rows for Encumbrances
    $(".groupHeader").click(function () {
        $(this).nextUntil("tr.groupHeader").slideToggle(1000);
    });

    // set textbox clear methods
    $("#VendorClear").click(function () {
        $("#VendorSelector").val('');
        $("#VendorIDValidation").show();
    });
    $("#ContractTypeClear").click(function () {
        $("#ContractTypeSelector").val('');
        $("#ContractTypeIDValidation").show();
    });
    $("#OCAClear").click(function () {
        $("#OCASelector").val('');
        $("#OCAIDValidation").show();
    });
    $("#FundClear").click(function () {
        $("#FundSelector").val('');
        $("#FundIDValidation").show();
    });
    $("#CategoryClear").click(function () {
        $("#CategorySelector").val('');
        $("#CategoryIDValidation").show();
    });

    //Contract (for LineItemGroups/Manage)
    $("#ContractSelector").blur(function (event, ui) {
        // if the value is "NEW" then open the Contract Dialog
        var contractNumber = $("#ContractSelector").val().toUpperCase();
        if (contractNumber === "NEW") {
            if ($("#SuppressNewScript").length > 0) {
                $("#ContractID").trigger('change');
            } else {
                openContractDialog();
            }
        }
    }).autocomplete({
        source: function (request, response) {
            $.ajax({
                autoFocus: true,
                url: "/LineItemGroups/ListContracts",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    var counter = 0;
                    var lastSelector = "";
                    var lastID = 0;
                    response($.map(data, function (item) {
                        var ContractSelector = item.contractNumber;
                        lastSelector = ContractSelector;
                        lastID = item.contractID;
                        counter++;
                        $("#ContractIDValidation").hide();
                        return { label: item.contractNumber, value: item.contractNumber, ContractID: item.contractID };
                    }));
                    // if autocomplete has a single match, select it
                    if (counter === 1) {
                        $("#ContractSelector").val(lastSelector);
                        $("#ContractID").val(lastID);
                        // show the ContractPanel
                        if ($("#ContractPanel")) {
                            showContractPanel(lastID);
                        }
                        if ($("#LineItemsPanel")) {
                            displayLineItemsPanelOrMessage();
                        }
                    }
                }
            });
        },
        select: function (event, ui) {
            $("#ContractSelector").val(ui.item.label);
            $("#ContractID").val(ui.item.ContractID);
            if ($("#SuppressNewScript").length > 0) {
                $("#ContractID").trigger('change');
                return false;
            }
            // show the ContractPanel
            if ($("#ContractPanel").length > 0) {
                showContractPanel(ui.item.ContractID);
                if ($("#ContractSelector").val().toUpperCase() === "NEW") {
                    $("#ContractNumber").val("NEW");
                }
            }
            if ($("#LineItemsPanel").length > 0) {
                displayLineItemsPanelOrMessage();
            }
            return false;
        }
    });

    if ($("#ContractPanel")) {
        showContractPanel($("#ContractID").val());
    }


    // set Encumbrance Total
    setEncumbranceTotal();
    setContractAmountTotal();
   
} // *** end of initForms which is the body of the ready function ***

function initContractControls() {
    //datepicker 
    $(function () {
        $(".datepicker").datepicker({
            changeMonth: true,
            changeYear: true,
            /* fix buggy IE focus functionality */
            fixFocusIE: false,

            /* blur needed to correctly handle placeholder text */
            onSelect: function (dateText, inst) {
                this.fixFocusIE = true;
                $(this).blur().change().focus();
            },
            onClose: function (dateText, inst) {
                this.fixFocusIE = true;
                this.focus();
            }
        });
    });
    //contract selector)
    $("#ContractSelector").blur(function (event, ui) {
        // if the value is "NEW" then open the Contract Dialog
        var contractNumber = $("#ContractSelector").val().toUpperCase();
        if (contractNumber === "NEW") {
            if ($("#SuppressNewScript").length > 0) {
                $("#ContractID").trigger('change');
            } else {
                openContractDialog();
            }
        }
    }).autocomplete({
        source: function (request, response) {
            $.ajax({
                autoFocus: true,
                url: "/LineItemGroups/ListContracts",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    var counter = 0;
                    var lastSelector = "";
                    var lastID = 0;
                    response($.map(data, function (item) {
                        var ContractSelector = item.contractNumber;
                        lastSelector = ContractSelector;
                        lastID = item.contractID;
                        counter++;
                        $("#ContractIDValidation").hide();
                        return { label: item.contractNumber, value: item.contractNumber, ContractID: item.contractID };
                    }));
                    // if autocomplete has a single match, select it
                    if (counter === 1) {
                        $("#ContractSelector").val(lastSelector);
                        $("#ContractID").val(lastID);
                        // show the ContractPanel
                        if ($("#ContractPanel")) {
                            showContractPanel(lastID);
                        }
                        if ($("#LineItemsPanel")) {
                            displayLineItemsPanelOrMessage();
                        }
                    }
                }
            });
        },
        select: function (event, ui) {
            $("#ContractSelector").val(ui.item.label);
            $("#ContractID").val(ui.item.ContractID);
            if ($("#SuppressNewScript").length > 0) {
                $("#ContractID").trigger('change');
                return false;
            }
            // show the ContractPanel
            if ($("#ContractPanel").length > 0) {
                showContractPanel(ui.item.ContractID);
                if ($("#ContractSelector").val().toUpperCase() === "NEW") {
                    $("#ContractNumber").val("NEW");
                }
            }
            if ($("#LineItemsPanel").length > 0) {
                displayLineItemsPanelOrMessage();
            }
            return false;
        }
    });

    //Contract Types
    $("#ContractTypeSelector").autocomplete({
        source: function (request, response) {
            $.ajax({
                autoFocus: true,
                url: "/Contracts/ListContractTypes",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    var counter = 0;
                    var lastSelector = "";
                    var lastID = 0;
                    response($.map(data, function (item) {
                        var contractTypeSelector = item.contractTypeCode + " - " + item.contractTypeName;
                        lastSelector = contractTypeSelector;
                        lastID = item.contractTypeID;
                        counter++;
                        $("#ContractTypeIDValidation").hide();
                        return { label: contractTypeSelector, value: item.contractTypeSelector, contractTypeID: item.contractTypeID, accessKey: item.contractTypeID };
                    }));
                    // if autocomplete has a single match, select it
                    if (counter === 1) {
                        $("#ContractTypeSelector").val(lastSelector);
                        $("#ContractTypeID").val(lastID);
                    }
                }
            });
        },
        select: function (event, ui) {
            $("#ContractTypeSelector").val(ui.item.label);
            $("#ContractTypeID").val(ui.item.contractTypeID);
            return false;
        }
    });

    $("#ContractTypeSelector").bind('blur', function () {
        updateContractContractType();
    });

    //Vendors
    $("#VendorSelector").autocomplete({
        source: function (request, response) {
            $.ajax({
                autoFocus: true,
                url: "/Contracts/ListVendors",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    var counter = 0;
                    var lastSelector = "";
                    var lastID = 0;
                    response($.map(data, function (item) {
                        var vendorSelector = item.vendorCode + " - " + item.vendorName;
                        lastSelector = vendorSelector;
                        lastID = item.vendorID;
                        counter++;
                        $("#VendorIDValidation").hide();
                        return { label: vendorSelector, value: item.vendorSelector, vendorID: item.vendorID };
                    }));
                    // if autocomplete has a single match, select it
                    if (counter === 1) {
                        $("#VendorSelector").val(lastSelector);
                        $("#VendorID").val(lastID);
                    }
                }
            });
        },
        select: function (event, ui) {
            $("#VendorSelector").val(ui.item.label);
            $("#VendorID").val(ui.item.vendorID);
            enableEditVendor();
            return false;
        }
    });
    $("#VendorSelector").bind('blur', function () {
        updateContractVendor();
    });
}

// /LineItemGroups/Manage adds these dialogs
function addDialogs() {

    // add ShowCommentDialog to /LineItemGroups/Manage to show Line Item comments
    $("#CommentsDisplayDialog").dialog({
        autoOpen: false,
        height: 200,
        width: 400,
        buttons: {
            "Okay": function () {
                $(this).dialog("close");
            }
        }
    });
    
    // add Add Vendor dialog and link to /LineItemGroups/Manage
    $('#addVendorDialog').dialog({
        autoOpen: false,
        height: 350,
        width: 800,
        close: function (event, ui) {
            $("#VendorSelector").show();
            $("#ContractTypeSelector").show();
        }
    });
    $('#addVendorLink').click(function () {
        $('#addVendorDialog').dialog("open");
    });

    // add Edit Vendor dialog and link to /LineItemGroups/Manage
    $('#editVendorDialog').dialog({
        autoOpen: false,
        height: 350,
        width: 800,
        close: function (event, ui) {
            $("#VendorSelector").show();
            $("#ContractTypeSelector").show();
        }
    });
    $('#editVendorLink').click(function () {
        $('#editVendorDialog').dialog("open");
    });

    // add Contract dialog to /LineItemGroups/Manage
    $("#ContractDialog").dialog({
        autoOpen: false,
        width: 1200,
        resizable: false,
        title: 'Contract Information',
        modal: true,
        open: function (event, ui) {
            var url = "/LineItemGroups/NewContractPartial";
            if ($("#ContractID").val() > 0) {
                url = "/LineItemGroups/NewContractPartial/" + $("#ContractID").val();
            }
            $(this).load(url, function () {
                initContractControls();
                $("#ContractNumber").focus();
            });
            //Contract Types
            $("#ContractTypeSelector").zIndex = $("#ContractDialog").zIndex + 1;
            //Vendors
            $("#VendorSelector").zIndex = $("#ContractDialog").zIndex + 1;
            $("#ContractNumber").focus();
        },
        buttons: {
            "Cancel": function () {
                $(this).dialog("close");
            },
            "Save Contract": function () {
                if (validateContractDialog()) {
                    saveContractModal();
                    $(this).dialog("close");
                }
            }
        }
    });
    // ContractSelector is showing when modal is open, so I explicitly show and hide it when ContractDialog is opened/closed
    $("#ContractDialog").on('dialogclose', function (event) {
        $("#ContractSelector").show();
    });

     // add LineItem dialog to /LineItemGroups/Manage
   $("#LineItemDialog").dialog({
        autoOpen: false,
        width: 1200,
        resizable: false,
        title: 'Financial Information',
        modal: true,
        open: function (event, ui) {
            // put autocorrect controls on top of dialog
            $("#CategorySelector").zIndex = $("#LineItemDialog").zIndex + 1;
            $("#OCASelector").zIndex = $("#LineItemDialog").zIndex + 1;
            $("#FundSelector").zIndex = $("#LineItemDialog").zIndex + 1;
        },
        buttons: {
            "Cancel": function () {
                $(this).dialog("close");
                $("#ContractSelector").show();
            },
            "Save Line": function () {
                if (validateLineItem()) {
                    saveLineItemModal();
                    $(this).dialog("close");
                    $("#ContractSelector").show();
                }
            }
        }
    }).load("/LineItemGroups/NewLineItemPartial", function () {
        // very important to initForms() to enable date pickers and autocompletes
        initLineItemControls();
    });
}

// for all pages, we want to update the Hamburger menu for Admin users
function updateHamburgerMenuForAdmin() {
    //if user has Admin role show Users link on hamburger menu
    if ($("#UserRoles").length > 0 && $("#UserRoles").val().indexOf("Admin") >= 0 && $("#UsersMenu").length === 0) {
        var usersMenu = "<li id='UsersMenu' name='UsersMenu'><a href='\\Users\\Index'>Users</a></li >";
        $("#HamburgerMenu").append(usersMenu);
        var fundsMenu = "<li id='FundsMenu' name='FundsMenu'><a href='\\Funds\\Index'>Funds</a></li >";
        $("#HamburgerMenu").append(fundsMenu);
        // No categories for now
        //var categoriesMenu = "<li id='CategoriesMenu' name='CategoriesMenu'><a href='\\Categories\\Index'>Categories</a></li>";
        //$("#HamburgerMenu").append(categoriesMenu);      
    }
}
function addToDoList() {
    // If a user has Finance, WP, or CFM role
    // Add a list of links to encumbrance requests that are waiting for review to the ToDoList menu item
    var listString = "<a class='dropdown-toggle' role='button' data-toggle='dropdown' aria-expanded='false'>";
    listString += "To Do List <span class='caret'></span></a>";
    listString += "<ul class='dropdown dropdown-menu' role='menu' id='ToDoDropdown' name='ToDoDropdown'>";
    if ($("#UserRoles").val().indexOf("Finance") >= 0) {
        getToDoItems("Finance");
    }
    if ($("#UserRoles").val().indexOf("WP") >= 0) {
        getToDoItems("Work Program");
    }
    if ($("#UserRoles").val().indexOf("CFM") >= 0) {
        getToDoItems("CFM");
    }
    listString += "</ul>";
    $("#ToDoList").append(listString);
}

function getToDoItems(status) {
    var statusName = status.replace(" ", ""); // remove space from status for id label and selector
    var listString = "";
    $.ajax({
        url: "/LineItemGroups/GetEncumbranceIDsByStatus",
        type: "GET",
        datatype: "json",
        data: { status: JSON.stringify(status) },
        success: function (data) {
            // <a href="#menupos1" class="list-group-item" data-toggle="collapse" data-parent="#ToDoList">
            listString += "<li style='text-align:left' name = 'ToDo_" + statusName + "' id  = 'ToDo_" + statusName + "' class='list-group-item' data-parent='#ToDoList' onmouseover='showHideToDoList(\"" + statusName + "_SubMenu\")'><b>" + status + "</b></li>";
            //listString += "<li style='text-align:left'><a href='#'>" + status + "</a></li>";
            // TODO: Make status name clickable to show/hide list items
            listString += "<div name='" + statusName + "_SubMenu' id= '" + statusName + "_SubMenu' class='collapse list-group-submenu'>";
            //listString += "<div class='collapse list-group-submenu' id='submenu1'>";
            if (data.length === 0) { listString += "<li style='text-align:left'>No items</li>"; }
            for (var i = 0; i<data.length; i++) {
                listString += "<li style='text-align:left' class='list-group-item sub-sub-item'>";
                listString += "<a class='dropdown-item' role='button' target='_self' href='/LineItemGroups/Manage/" + data[i] + "'>Encumbrance #" + data[i] + "</a></li>";
            }
            //listString += "</div>";
            $("#ToDoDropdown").append(listString);
        },
        error: function () {
            listString += "<li style='text-align:left'><b>" + status + "</b></li>";
            listString += "<li style='text-align:left'>No items</li>";
            $("#ToDoDropdown").append(listString);
        }
    });
}

function showHideToDoList(listName) {
    // show/hide the named div in the ToDo List
    if ($("#" + listName).hasClass("collapse")) {
        $("#" + listName).removeClass("collapse");
    } else {
        $("#" + listName).addClass("collapse");
    }
}

// /LineItemGroups/Manage
// based on encumbrance CurrentStatus and user Roles show and hide buttons to allow actions
function showHideButtons() {
    updateHamburgerMenuForAdmin();
    //collapse panels if not Originator
    if ($("#UserRoles").val() && $("#UserRoles").val().indexOf("Originator") < 0) {
        toggleEncumbrancePanel();
        toggleContractPanel();
    }

    // hide all buttons
    $("[id^='btnEncumbrance']").each(function () {
        $(this).hide();
    });
    $("#noButtonDiv").hide();
    if ($("#ContractID").val() && $("#ContractID").val() > 0) {
        $("#OpenContractInformationDiv").hide();
        $("#OpenContractInformationSpan").hide();
    }
    if (!$("#LineItemsPanel").is(":visible")) {
        return false;
    }
    $("#btnInputFinancialInformation").hide();

    // depending on CurrentStatus and Roles, enable appropriate buttons
    var currentStatus = $("#CurrentStatus").val();
    var encumbranceType = $("#LineItemType").val();
    var roles = $("#UserRoles").val();

    if ((currentStatus === "New" || currentStatus === "Draft") && roles.indexOf("Originator") >= 0) {
        $("#btnEncumbranceDraft").val("Save as Draft");
        $("#btnEncumbranceDraft").show();
        // Rule change: no special treatment here for Close50 or Close98 5/15/2019
        //if Encumbrance Type is Close50 or Close98 then submit directly to CFA Ready
        /*if (encumbranceType.indexOf("Close")>=0) {
            $("#btnEncumbranceCFM").val("Submit for CFM Input");
            $("#btnEncumbranceCFM").show();
        } else { */
            $("#btnEncumbranceFinance").val("Submit to Finance");
            $("#btnEncumbranceFinance").show();
        //}
        return false;
    }
    if (currentStatus === "Finance" && roles.indexOf("Finance Reviewer") >= 0) {
        $("#btnEncumbranceWP").val("Approve to Work Program");
        $("#btnEncumbranceWP").show();
        $("#btnEncumbranceSaveAsIs").show();
        $("#btnEncumbranceRollback").val("Reject back to Originator");
        $("#btnEncumbranceRollback").show();
        $("#btnEncumbranceComplete").val("Update to CFM Complete");
        $("#btnEncumbranceComplete").show();
        return false;
    }
    if (currentStatus === "Work Program" && roles.indexOf("WP Reviewer") >= 0) {
        $("#btnEncumbranceFinance").val("Reject back to Finance");
        $("#btnEncumbranceFinance").show();
        $("#btnEncumbranceSaveAsIs").show();
        $("#btnEncumbranceCFM").val("Approve to CFM");
        $("#btnEncumbranceCFM").show();
        return false;
    }
    if (currentStatus === "CFM" && roles.indexOf("CFM Submitter") >= 0) {
        $("#btnEncumbranceRollback").val("Reject back to Originator");
        $("#btnEncumbranceRollback").show();
        $("#btnEncumbranceWP").val("Return to Work Program");
        $("#btnEncumbranceWP").show();
        $("#btnEncumbranceSaveAsIs").show();
        $("#btnEncumbranceComplete").val("Update to CFM Complete");
        $("#btnEncumbranceComplete").show();
        return false;
    }
    if (currentStatus === "Complete" && roles.indexOf("Finance Reviewer") >= 0) {
        $("#btnEncumbranceRollback").show();
        $("#btnEncumbranceSaveAsIs").show();
        return false;
    }
    $("#noButtonDiv").show();

    // show LineItemsPanel if the contract is selected
    if ($("#ContractID").val() && $("#ContractID").val() > 0 && ($("#LineItemGroupID").val() && $("#LineItemGroupID").val() > 0)) {
        $("#LineItemsPanel").show();

        // show FileAttachmentsPanel if it exists
        if ($("#FileAttachmentsPanel").length !== undefined && $("#FileAttachmentsPanel").length > 0) {
            $("#FileAttachmentsPanel").show();
        }
    }
    // ContractSelector is showing when modal is open, so I explicitly show and hide it when LineItemDialog is opened/closed
    $("#LineItemDialog").on('dialogclose', function (event) {
        $("#ContractSelector").show();
    });
}

// called when LineItemType is changed in /LineItemGroups/Manage 
function updateEncumbranceType() {
    var encumbranceType = $("#LineItemType").val();
    var contractID = $("#ContractID").val();

    displayLineItemsPanelOrMessage();
    setDefaultUserAssignedID();
    if (encumbranceType === "New Contract" && (contractID === "" || contractID === 0)) {
        openContractDialog();
    }
}

// called by updateEncumbranceType() for /LineItemGroups/Manage
function setDefaultUserAssignedID() {
    var encumbranceType = $("#LineItemType").val();
    var prefix = "";
    $("#AmendedIDDiv").hide();
    if (encumbranceType === "Renewal") {
        prefix = "RNW#";
        $("#RenewalEndingDate").show();
    }
    if (encumbranceType === "Supplemental") {
        prefix = "SUP#";
    }
    if (encumbranceType === "LOA") {
        prefix = "LOA#";
        //$("#AmendedIDDiv").show();
    }
    if (encumbranceType === "Amendment") {
        prefix = "AMD#";
    }
    if (encumbranceType === "Amendment to LOA") {
        prefix = "AMD#";
        $("#AmendedIDDiv").show();
    }
    if (encumbranceType === "Correction") {
        $("#AmendedIDDiv").show();
    }
    if (prefix.length > 0 && $("#UserAssignedID").length && $("#UserAssignedID").val().length === 0) {
        $("#UserAssignedID").val(prefix); // at client request, changed from (prefix + encNumber);
    }
    if (encumbranceType === "Advertisement") {
        $("#AdvertisementAdDate").show();
        $("#AdvertisementLetDate").show();
    } else {
        $("#AdvertisementAdDate").hide();
        $("#AdvertisementLetDate").hide();
    }
    if (encumbranceType === "Award") {
        displayMessage("Please update the contract to reflect the awarded amount and vendor.");
        //$("#AwardBanner").show(); // award banner is only included in the page if it loads with LineItemType = Award
    }
}


// Display read-only Contract panel in /LineItemGroups/Manage
function showContractPanel(contractID) {
    // use the contractID to fetch the corresponding ExtendedContract and display it in the ContractPanel
    $.ajax({
        url: "/Contracts/GetDisplayContract",
        type: "POST",
        dataType: "json",
        data: { contractID: contractID },
        success: function (data) {
            var result = JSON.parse(data);
            populateContractPanel(result);
        }
    });
}

function populateContractPanel(contract) {
    //write contract details to ContractPanel
    if (!contract) { return false; }
    $("#ContractTitle").text("Contract - " + contract.ContractNumber);
    $("#EncHeaderContract").html("Contract: <h4><a href='/Contracts/Details/" + contract.ContractID + "'>" + contract.ContractNumber + "</a></h4>");
    var roles = $("#UserRoles").val();
    if (roles.indexOf("Originator") >= 0 || roles.indexOf("Finance Reviewer") >= 0) {
        $("#EditContractLink").html("<a href= 'javascript:openContractDialogExisting(" + contract.ContractID + ")'>Edit Contract Information</a> ");
    }
    var contractHtml = "";
    contractHtml += "<div class='row'>";
    contractHtml += "<div class='col-sm-4'><strong>Originated by:</strong> <a href='mailto:" + contract.OriginatorEmail + "'>" + contract.OriginatorName + "</a> (" + contract.OriginatorLogin + ") " + contract.OriginatorPhone + "</div>";
    contractHtml += "<div class='col-sm-3'><strong>Created Date:</strong> " + contract.CreatedDate + "</div>";
    contractHtml += "<div class='col-sm-3'><strong>Modified Date:</strong> " + contract.ModifiedDate + "</div>";
    contractHtml += "</div><div class='row'>";
    contractHtml += "<div class='col-sm-2'><dl><dt>Contract Number:</dt><dd> " + contract.ContractNumber;
    contractHtml += "<input type='hidden' name='ContractID' id='ContractID' value='" + contract.ContractID + "'/>";
    contractHtml += "</dd></dl></div>";
    contractHtml += "<div class='col-sm-2'><dl><dt> Contract Type:</dt><dd> " + contract.ContractTypeName + "</dd></dl></div>";
    contractHtml += "<div class='col-sm-2'><dl><dt> Is Renewable?:</dt><dd> " + contract.ContractRenewable + "</dd></dl></div>";
    contractHtml += "</div><div class='row'>";
    contractHtml += "<div class='col-sm-2'><dl><dt> Contract Initial Amount:</dt><dd> " + contract.FormattedContractInitialAmount + "</dd></dl></div>";
    contractHtml += "<div class='col-sm-2'><dl><dt> Contract Begin Date:</dt><dd> " + contract.FormattedBeginningDate + "</dd></dl></div>";
    contractHtml += "<div class='col-sm-3'><dl><dt> Contract Procurement:</dt><dd> " + contract.ProcurementName + "</dd></dl></div>";
    contractHtml += "<div class='col-sm-3'><dl><dt> <input type='hidden' id='Compensation' name='Compensation' value='" + contract.CompensationID + "'/> Contract Funding Terms:</dt><dd> " + contract.CompensationName + "</dd></dl></div>";
    contractHtml += "</div><div class='row'>";
    contractHtml += "<div class='col-sm-2'><dl><dt> Maximum LOA Amount:</dt><dd> " + contract.FormattedMaxLoaAmount + "</dd></dl></div>";
    contractHtml += "<div class='col-sm-2'><dl><dt> <input type='hidden' id='EndingFY' name='EndingFY' value='" + getEndingFY(contract.FormattedEndingDate) + "'/> Contract End Date:</dt><dd> " + contract.FormattedEndingDate + "</dd></dl></div>";
    contractHtml += "<div class='col-sm-3'><dl><dt> Vendor:</dt><dd> " + contract.VendorName + "</dd></dl></div>";
    contractHtml += "</div><div class='row'>";
    contractHtml += "<div class='col-sm-2'><dl><dt> <input type='hidden' id='BudgetCeiling' name='BudgetCeiling' value='" + contract.BudgetCeiling + "'/> Budget Ceiling:</dt> <dd> " + contract.FormattedBudgetCeiling + "</dd></dl ></div > ";
    contractHtml += "<div class='col-sm-2'><dl><dt> Service End Date:</dt><dd> " + contract.FormattedServiceEndingDate + "</dd></dl></div>";
    contractHtml += "<div class='col-sm-3'><dl><dt> Recipient:</dt><dd> " + contract.RecipientName + "</dd></dl></div>";
    contractHtml += "</div><div class='row'>";
    var description = contract.DescriptionOfWork ? contract.DescriptionOfWork : "";
    contractHtml += "<div class='col-sm-4'><dl><dt> Description of Work:</dt><dd> " + description + "</dd></dl></div>";
    contractHtml += "</div>";

    setEncumbranceTotal();

    $("#ContractPanelBody").html(contractHtml);
    $("#ContractPanel").show();
}

// called by populateContractPanel in /LineItemGroups/Manage
function getEndingFY(formattedDate) {
    var m = formattedDate.indexOf("/");
    var n = formattedDate.lastIndexOf("/");
    var year = formattedDate.substring(n + 1);
    var month = formattedDate.substring(m + 1, n);
    var FY = year;
    if (month > 7) { FY++; }
    return FY;
}

/*** CLOSE CONTRACT METHODS ***/

// Open the dialog to Close a Contract
// called from /LineItemGroups/Manage and from /Contracts/Details
function openCloseContractDialog(contractID, contractNumber, contractStatus) {
    var titleText = "Request to Close Contract " + contractNumber;
    $("#CloseContractDialog").dialog({
        autoOpen: false,
        width: 600,
        resizable: false,
        title: titleText,
        modal: true,
        open: function (event, ui) {
            $("#ContractSelector").hide();
            $(this).html("");
            var contents = "<p>Please select a closure type: </p>";
            contents += "<table><tr><th>&nbsp;</th><th>&nbsp;</th></tr><tr>";
            contents += "<td><input type='radio' name='closureType' id='close50' class='radio inline' style='vertical-align: middle; margin: 0px;' /></td><td><label class='radio-inline'> Close Status 50, Executed Contract </label></td>";
            contents += "</tr><tr>";
            contents += "<td><input type='radio' name='closureType' id='close98' class='radio inline' style='vertical-align: middle; margin: 0px;' /></td><td><label class='radio-inline'> Close Status 98, Unexecuted Contract </label></td>";
            contents += "</tr></table><br/>";
            //contents += "<p>To remove line items from this Encumbrance Request, use the <strong>Delete</strong> link for that line in the <strong>Financial Information</strong> section of the form.</p>";
            contents += "<p>I certify that the amounts being released are not required for current and future obligations.</p>";
            contents += "<table><tr><th>&nbsp;</th><th>&nbsp;</th></tr><tr>";
            contents += "<td><input type='radio' name='amountsYesNo' id='amountsYes' /></td><td> Yes</td>";
            contents += "</tr><tr>";
            contents += "<td><input type='radio' name='amountsYesNo' id='amountsNo'  /></td><td> No</td>";
            contents += "</tr></table><br/>";
            contents += "Comments: <br/>";
            contents += "<input type='textarea' name='ClosureComments' id='ClosureComments' /><br />";
            contents += "<div name='WarnMessage' id='WarnMessage'></div>";
            contents += "<input type='hidden' name='CloseContractID' id='CloseContractID' value='" + contractID + "'>";
            $(this).html(contents);
        },
        buttons: {
            "Cancel": function () {
                $("#ContractSelector").show();
                $(this).dialog("close");
            },
            "Complete": function () {
                var closeJson = getClosingDetails();
                // if validation fails, closeJson is empty string
                if (closeJson !== "") {
                    closeContract(closeJson);
                    $("#ContractSelector").show();
                    $(this).dialog("close");
                }
            }
        }
    });
    $("#CloseContractDialog").dialog("open");
}

// get details for Closing a Contract
function getClosingDetails() {
    var closeJson = "";
    // read all values from the dialog into the json string
    closeJson += "{";
    closeJson += '"ContractID": "' + $("#CloseContractID").val() + '",';
    var closureType = "";
    if ($("#close50").is(":checked")) {
        closureType = "CloseContract50";
    }
    if ($("#close98").is(":checked")) {
        closureType = "CloseContract98";
    }

    closeJson += '"ActionItemType":"' + closureType + '",';
    var amountsYesNo = "";
    if ($("#amountsYes").is(":checked")) {
        amountsYesNo = "yes";
    }
    if ($("#amountsNo").is(":checked")) {
        amountsYesNo = "No";
    }
    if ($("#amountsNA").is(":checked")) {
        amountsYesNo = "NA";
    }

    // Validation
    var warnMsg = "";
    if (closureType === "") {
        // no Type selection
        warnMsg += "Please select a closure type. <br />";
    }
    if (amountsYesNo === "") {
        // no amounts verification
        warnMsg += "Please verify that amounts are not required for current or future obligations. <br />";
    }
    if (amountsYesNo === "No") {
        // no amounts verification
        warnMsg += "This contract cannot be closed at this time. <br />";
    }
    if (warnMsg !== "") {
        // Validation fails. Show warnMsg and return to form
        $("#WarnMessage").html("<font color='red'>" + warnMsg + "</font>");
        return "";
    }
    var groupID;
    if ($("#LineItemGroupID").length > 0 && $("#LineItemGroupID").val() !== null && $("#LineItemGroupID").val() !== undefined) {
        $("#LineItemGroupID").val();
    } else { groupID = 0; }

    closeJson += '"Amounts":"' + amountsYesNo + '",';
    closeJson += '"LineItemGroupID":"' + groupID + '",';
    closeJson += '"Comments":"' + $("#ClosureComments").val() + '",';
    closeJson += "\"ClosureType\":\"" + closureType + "\",";
    closeJson += "\"ContractOrEncumbrance\":\"" + "Contract" + "\",";
    closeJson += "}";
    // return the json string
    return closeJson;
}

// AJAX call to the server to close the contract
function closeContract(jsonString) {
    $.blockUI({ message: "<h4>Closing Contract...</h4>", timeout: 3000 });
    $.ajax({
        url: "/LineItemGroups/CloseContract",
        type: "POST",
        dataType: "json",
        data: { closeContract: jsonString },
        success: function (data) {
            var results = JSON.parse(data);
            // remove Close Contract link
            $("#CloseContractLink").remove();
            // show red Closed message
            var newTitle = $("#ContractTitle").html() + "<font color = 'red'>&nbsp;&nbsp;&nbsp; Closed</font>";
            $("#ContractTitle").html(newTitle);
            // redirect to List page
            window.location.href = "/LineItemGroups/List";
        }
    });
}

/*** END OF -- CLOSE CONTRACT METHODS -- ***/

// Update values in Encumbrance Header in /LineItemGroups/Manage
function displayLineItemsPanelOrMessage() {
    var encumbranceType = $("#LineItemType").val();
    var encumbranceStatus = $("#GroupStatus").val();
    //update encumbrance panel header
    if (encumbranceType !== null && encumbranceType !== undefined && encumbranceType.length > 0 && encumbranceType !== "None") {
        $("#EncHeaderEncType").html("Type: <h4>" + encumbranceType + "</h4>");
    }
    if (encumbranceStatus !== null && encumbranceStatus !== undefined && encumbranceStatus.length > 0) {
        $("#EncHeaderEncStatus").html("Status: <h4>" + encumbranceStatus + "</h4>");
    }
    var groupID = $("#LineItemGroupID").val();
    if (groupID === "") { groupID = 0; }
    if (groupID > 0) {
        var icon = "";
        if ($("#AttachmentCount").length > 0 && $("#AttachmentCount").val() !== undefined && $("#AttachmentCount").val() >0) {
            icon = "<span title='This encumbrance request has at least one file attachment.'><img src='~/images/Icons/EPS_Clip.png' alt='Attach' height='16' width='20' /></span>";
        }
        $("#EncHeaderEncID").html("Encumbrance: <h4>" + groupID + "&nbsp; &nbsp; &nbsp;" + icon + "</h4>");
    }
    var contractID = $("#ContractID").val();

    setEncumbranceTotal();
    //$("#EncHeaderEncAmount").html("Amount: <h4>" + getEncumbranceAmount() + "</h4>");

    if (contractID > 0 && encumbranceType
        && encumbranceType.length > 0 && encumbranceType !== "None"
        && groupID > 0) {
        $("#LineItemsPanel").show();
        if ($("#FileAttachmentsPanel").length !== undefined && $("#FileAttachmentsPanel").length > 0) {
            $("#FileAttachmentsPanel").show();
        }
        showHideButtons();
    } else {
        $("#messageSpan").text("Click \"Input Financial Information\" to open Financial Information panel.");
    }

    // For advertisement, show specialty fields for LineItemGroups based on LineItemType
    $("#AdvertisementAdDate").hide();
    $("#AdvertisementLetDate").hide();
    $("#AmendedIDDiv").hide();
    $("#RenewalEndingDate").hide();
    $("#AmendedLineItemDef").hide();
    $("#AmendedFlairIDDef").hide();
    if (encumbranceType === "Advertisement") {
        $("#AdvertisementAdDate").show();
        $("#AdvertisementLetDate").show();
    } else if (encumbranceType === "Correction") {
        $("#AmendedIDDiv").show();
        $("#AmendedLineItemDef").hide();
        $("#AmendedFlairIDDef").show();
    } else if (encumbranceType === "Amendment to LOA") {
        $("#AmendedIDDiv").show();
        $("#AmendedLineItemDef").show();
        $("#AmendedFlairIDDef").show();
    } else if (encumbranceType === "Renewal") {
        $("#RenewalEndingDate").show();
    } else if (encumbranceType !== undefined && encumbranceType.indexOf("Close") >= 0) {
        showCloseLineItemTypeDialog(encumbranceType);
    }
}

// Called to update Encumbrance header information in //LineItemGroups/Manage
function getEncumbranceAmount(encumbranceID) {
    if (encumbranceID > 0) {
        var EncumbranceInfo = {};
        EncumbranceInfo.groupID = encumbranceID;
        $.ajax({
            type: "POST",
            ContentType: "application/json; charset=utf-8",
            dataType: 'html',
            data: { encumbranceInfo: JSON.stringify(EncumbranceInfo) },
            url: '/LineItemGroups/GetEncumbranceAmount/',
            success: function (response) { }
        });
        return formatCurrency(amount);
    } else {
        return formatCurrency(0.00);
    }
}


/***  ADD/EDIT VENDOR METHODS ***/
// Dialog is called from /Contracts/Create and /Contracts/Edit
// Panel is called from Contract Information dialog in /LineItemGroups/Manage
function openAddVendorDialog() {
    $("#addVendorDialog").dialog("open");
    $("#addVendorDialog #VendorCode").bind("blur", function () {
        setVendorValidationMessage($("#addVendorDialog #vendorMessage"), this.value);
    });
    $("#VendorSelector").hide();
    $("#ContractTypeSelector").hide();
}

function openAddVendorPanel() {
    $("#addVendorPanel").show();
    $("#addVendorPanel #VendorCode").bind("blur", function () {
        setVendorValidationMessage($("#addVendorPanel #vendorMessage"), this.value);
    });
}
function hideAddVendorPanel() {
    $("#addVendorPanel").hide();
}
function addVendor() {
    // make sure all values are populated
    // submit to /ContractStatus/Create controller
    $("AddVendorForm").submit();
    // close dialog and return focus to parent form
}
function closeAddVendorDialog() {
    $("#addVendorDialog").dialog("close");
    $("#VendorSelector").show();
    $("#ContractTypeSelector").show();
}
function openEditVendorPanel() {
    $("#editVendorPanel").show();
    //populate the dialog form
    $("#editVendorPanel #VendorID").val($("#VendorID").val());
    var vendor = $("#VendorSelector").val();
    $("#editVendorPanel #VendorCode").bind("blur", function () {
        setVendorValidationMessage($("#editVendorPanel #vendorMessage"), this.value);
    });
    var dashIndex = vendor.indexOf("-");
    var vendorCode = vendor.substring(0, dashIndex - 1).trim();
    var vendorName = vendor.substring(dashIndex + 1, vendor.length).trim();
    $("#editVendorPanel #VendorCode").val(vendorCode);
    $("#editVendorPanel #VendorName").val(vendorName);
}
function hideEditVendorPanel() {
    $("#editVendorPanel").hide();
}
function setVendorValidationMessage(vendorMessageDiv, vCode) {
    if (validateVendorCode(vCode)) {
        vendorMessageDiv.text("");
    } else {
        vendorMessageDiv.text("Vendor Number must be 'F' or 'S' plus 12 digits.");
    }
}
function addNewVendor() {
    if (!validateVendorCode($("#VendorCode").val())) {
        return false;
    }
    if ($("#VendorName").val().length < 1) { return false; }
    Vendor = {};
    Vendor.VendorName = $("#VendorName").val();
    Vendor.VendorCode = $("#VendorCode").val();
    $.ajax({
        url: "/Vendors/AddNewVendor",
        type: "POST",
        dataType: "json",
        data: { vendor: JSON.stringify(Vendor) },
        success: function (data) {
            var results = JSON.parse(data);
            $("#VendorID").val(results.VendorID);
            $("#VendorSelector").val(results.VendorCode + " - " + results.VendorName);
            $("#addVendorPanel").hide();
            $("#addVendorDialog").dialog("close");
            $("#VendorSelector").show();
        }
    });
}
function validateVendorCode(vCode) {
    // VendorCode must be "F" or "S" plus 12 numeric digits
    var pattern = /^(F|S)\d{12}/;
    if (vCode.length === 13 && vCode.match(pattern)){ return true; }
    return false;
}
function enableEditVendor() {
    $("#editVendorLink").show();
}
function openEditVendorDialog() {
    $("#editVendorDialog").dialog("open");
    $("#editVendorDialog #VendorCode").bind("blur", function () {
        setVendorValidationMessage($("#editVendorDialog #vendorMessage"), this.value);
    });
    $("#VendorSelector").hide();
    $("#ContractTypeSelector").hide();

    //populate the dialog form
    $("#editVendorDialog #VendorID").val($("#VendorID").val());
    var vendor = $("#VendorSelector").val();
    var dashIndex = vendor.indexOf("-");
    var vendorCode = vendor.substring(0, dashIndex - 1).trim();
    var vendorName = vendor.substring(dashIndex + 1, vendor.length).trim();
    $("#editVendorDialog #VendorCode").val(vendorCode);
    $("#editVendorDialog #VendorName").val(vendorName);
}
function closeEditedVendor() {
    $("#editVendorDialog").dialog("close");
    $("#editVendorPanel").hide();
    $("#VendorSelector").show();
    $("#ContractTypeSelector").show();
}
function saveEditedVendor() {
    if (!validateVendorCode($("VendorCode").val())) { return false; }
    if ($("VendorName").val().length < 1) { return false; }
    $("#editVendorDialog").dialog("close");
    $("#editVendorPanel").hide();
    $("EditVendorForm").submit();
    $("#VendorSelector").show();
    $("#ContractTypeSelector").show();
}
function updateVendor(source) {
    Vendor = {};
    if (source === "panel") {
        Vendor.VendorID = $("#editVendorPanel #VendorID").val();
        Vendor.VendorName = $("#editVendorPanel #VendorName").val();
        Vendor.VendorCode = $("#editVendorPanel #VendorCode").val();
    }
    if (source === "dialog") {
        Vendor.VendorID = $("#editVendorDialog #VendorID").val();
        Vendor.VendorName = $("#editVendorDialog #VendorName").val();
        Vendor.VendorCode = $("#editVendorDialog #VendorCode").val();
    }
    if (!validateVendorCode(Vendor.VendorCode)) { return false; }
    if (Vendor.VendorName.length < 1) { return false; }
    $.ajax({
        url: "/Vendors/UpdateVendor",
        type: "POST",
        dataType: "json",
        data: { vendor: JSON.stringify(Vendor) },
        success: function (data) {
            var results = JSON.parse(data);
            $("#VendorID").val(results.VendorID);
            $("#VendorSelector").val(results.VendorCode + " - " + results.VendorName);
            $("#editVendorPanel").hide();
            $("#editVendorDialog").dialog("close");
            $("#VendorSelector").show();
        }
    });
}
/*** END OF -- ADD/EDIT VENDOR METHODS -- ***/

// For the autocomplete selectors on the Contract Information dialog
// ID values are in hidden fields that get populated when the autocomplete selectors change
function updateContractVendor() {
    if ($("#Contract_VendorID")) {
        $("#Contract_VendorID").val($("#VendorID").val());
    }
}
function updateContractContractType() {
    if ($("#Contract_ContractTypeID")) {
        $("#Contract_ContractTypeID").val($("#ContractTypeID").val());
    }
}
function showCloseLineItemTypeDialog(encumbranceType) {
    $("#closeLineItemTypeDialog").dialog({
        autoOpen: false,
        height: 200,
        width: 400,
        resizable: false,
        title: encumbranceType + " Encumbrance Type",
        modal: true,
        open: function (event, ui) {
            $("#closeLineItemTypeDialog").html("");
            var ContractID = $("#ContractID").val();
            var dialogContent = "";
            dialogContent += "<p>You have selected Encumbrance Type " + encumbranceType + ". ";
            dialogContent += "This encumbrance request will not close the Contract. Click <strong>Okay</strong> to continue.</p>";
            dialogContent += "For information on closing a contract, <a href='../video/EPS_Demo_Close_Contract.wmv'>watch this video.</a></p>";
            $("#closeLineItemTypeDialog").html(dialogContent);
        },
        buttons: {
            "Okay": function () {
                $(this).dialog("close");
            }
        }
    });
    $("#closeLineItemTypeDialog").dialog("open");
}

// Open dialog to Delete the current encumbrance
// currently only called from /LineItemGroups/Manage
function openDeleteEncumbranceDialog(id) {
    $("#deleteEncumbranceDialog").dialog({
        autoOpen: false,
        width: 600,
        resizable: false,
        title: "Permanently delete Encumbrance Request #" + id,
        modal: true,
        open: function (event, ui) {
            $(this).html("");
            var deleteMessage = "<p>Would you like to permanently delete Encumbrance Request #" + id + "?</p>";
            $(this).append(deleteMessage);
        },
        buttons: {
            "Cancel": function () {
                $(this).dialog("close");
            },
            "Delete": function () {
                window.open("/LineitemGroups/Delete?id=" + id, "_self");
                $(this).dialog("close");
            }
        }
    });
    $("#deleteEncumbranceDialog").dialog("open");
}

/***  SUBMIT ENCUMBRANCE METHODS ***/

// Open dialog to Submit the current encumbrance
// called from /LineItemGroups/Manage when submit/reject/save buttons are clicked
function openEncumbranceSubmissionDialog(submitTo, wpUsers) {
    var titleText = submitTo === "Draft" ? "Save Encumbrance" : "Submit Encumbrance to " + submitTo;
    var buttonText = submitTo === "Draft" ? "Save" : "Submit";
    $("#SubmissionDialog").dialog({
        autoOpen: false,
        width: 600,
        resizable: false,
        title: titleText,
        modal: true,
        open: function (event, ui) {
            $("#ContractSelector").hide();
            $(this).html("");
            var defaultComment = "";
            var currentStatus = $("#CurrentStatus").val();
            // Client feedback prefers not to notify by default
            var notifyChecked = ""; // "checked";
            if (submitTo === "Draft") {
                defaultComment = "Saved as Draft.";
                if ($("#NoAttachmentComment").val() !== undefined && $("#NoAttachmentComment").val().length > 0) {
                    defaultComment = "Missing Required File Attachment: " + $("#NoAttachmentComment").val();
                }
            } else if (submitTo === "Finance") {
                defaultComment = "Submitted to Finance.";
                if ($("#NoAttachmentComment").val() !== undefined && $("#NoAttachmentComment").val().length > 0) {
                    defaultComment = "Missing Required File Attachment: " + $("#NoAttachmentComment").val();
                }
            } else if (submitTo === "Work Program") {
                defaultComment = "Please review and approve for Work Program.";
            } else if (submitTo === "CFM") {
                defaultComment = "";
            } else if (submitTo === "Complete") {
                defaultComment = "Input to CFM.";
            }
            if (submitTo === currentStatus) {
                defaultComment = "";
                notifyChecked = "";
            }

            // for Work Program, show ItemReduced and AmountReduced
            if (currentStatus === "Work Program") {
                var itemReduced = "<strong>Item Reduced:</strong><br/><input type='text' name='itemReduced' id='itemReduced' /><br/>";
                var amountReduced = "<strong>Amount Reduced:</strong><br/><input type='text' name='amountReduced' id='amountReduced' /><br/>";
                $(this).append(itemReduced);
                $(this).append(amountReduced);
            }

            // add a comment textarea
            var newStatusInput = "<input type= 'hidden' name='newStatus' id='newStatus' value = '" + submitTo + "' />";
            $(this).append(newStatusInput);
            var commentBox = "<strong>Comments:</strong><br/><textarea id='commentText' name='commentText' cols='50' rows='4'>" + defaultComment + "</textarea><br />";
            $(this).append(commentBox);
            // if CurrentStatus is Draft and submitTo is Finance, add a checkbox (checked) to send a receipt to the Originator
            if ((currentStatus === "Draft" || currentStatus === "New") && submitTo === "Finance") {
                var receiptBox = "<input type='checkbox' id='receiptBox' name='receiptBox' checked /> Send a submission receipt to the originator. <br />";
                $(this).append(receiptBox);
            } else {
                // add a checkbox to send a notification to the originator
                var notifyOriginatorBox = "<input type='checkbox' id='notifyBox' name='notifyBox' " + notifyChecked + " /> Notify the originator of this update. <br />";
                $(this).append(notifyOriginatorBox);
            }
            // if CurrentStatus is Finance and submitTo is WorkProgram, add a set of checkboxes to select WP recipients
            if (submitTo === "Work Program") {
                var wpBox = "<div name='wpRecipients' id='wpRecipients'>";
                wpBox += "Select the Work Program reviewers to be notified: <br/>";
                //var recips = [];
                for (var i in wpUsers) {
                    var wpUser = wpUsers[i];
                    //recips.push(wpUsers[i]);
                    // properties from json are rendered in lower case
                    wpBox += "<input type='checkbox' id='wpUser_" + wpUser.userID + "' name='wpUser_" + wpUser.userID + "' value=" + wpUser.userID + " /> " + wpUser.firstName + " " + wpUser.lastName + " <br />";
                }
                wpBox += "</div>";
                $(this).append(wpBox);
            }
            // add a message div
            $(this).append("<div id='validationDiv'></div>");
        },
        buttons: {
            "Cancel": function () {
                $("#ContractSelector").show();
                $(this).dialog("close");
            },
            "Complete": function () {
                if (getSubmissionValidation()) {
                    $("#ContractSelector").show();
                    $(this).dialog("close");
                    var commentJson = getSubmissionDetails();
                    // Watch for BlockUI throwing an error in IE.
                    $.blockUI({message : "<h4>Submitting...</h4>", timeout : 3000});
                    saveEncumbrance(commentJson);
                }
            }
        }
    });
    $("#SubmissionDialog").dialog("open");
    if ($("#itemReduced").length > 0) { $("#itemReduced").focus(); } else { $("#commentText").focus(); }
}

// called from /LineItemGroups/Manage when submitting encumbrance if submitting for WP Review
// AJAX call to /Users/GetWPUsers
function getWPUsers(status) {
    $.ajax({
        type: "POST",
        ContentType: "application/json; charset=utf-8",
        dataType: 'html',
        url: '/Users/GetWPUsers/',
        success: function (response) {
 
            var wpUsers = JSON.parse(response);
            openEncumbranceSubmissionDialog(status, wpUsers);
        }
    });
}

function getSubmissionDetails() {
    // make a json object from the information in the SubmissionDialog and return it
    var jsonString = "{";
    jsonString += "\"status\" : \"" + $("#newStatus").val() + "\", "; 
    jsonString += "\"userID\" : " + $("#UserID").val() + ", ";
    if ($("#receiptBox").is(":checked")) {
        jsonString += "\"receipt\" : \"true\", ";
    }
    if ($("#notifyBox").is(":checked")) {
        jsonString += "\"notify\" : \"true\", ";
    }
    if ($("#wpRecipients").length > 0) {
        jsonString += "\"wpIDs\" : [";
        $("[id^='wpUser_']").each(function () {
            if ($(this).is(":checked")) {
                jsonString += $(this).val() + ", ";
            }
        });
        // replace last comma with close bracket
        jsonString = jsonString.replace(/,\s*$/, "");
        jsonString += "], ";
    }
    if ($("#itemReduced") && $("#itemReduced").val()) {
        jsonString += "\"itemReduced\" : \"" + $("#itemReduced").val() + "\", ";
    }
    if ($("#amountReduced") && $("#amountReduced").val()) {
        jsonString += "\"amountReduced\" : \"" + $("#amountReduced").val() + "\", ";
    }
    var commentText = $("#commentText").val();
    //if (commentText.length > 0) { commentText = commentText.replace(/'/g, "&#39;"); }
    jsonString += "\"comments\" : \"" + commentText + "\"";
    jsonString += "}";

    return jsonString;
}

function getSubmissionValidation() {
    var valid = true;
    // perform validation and return false if no wp recipients are selected
    if ($("#wpRecipients").length > 0) {
        valid = false;
        $("[id^='wpUser_']").each(function () {
            if ($(this).is(":checked")) {
                valid = true;
            }
        });
        if (!valid) {
            $("#validationDiv").html("<p><font color='red'>Please select at least one Work Program reviewer.</font></p>");
        }
    }
    return valid;
}


/*** END OF -- SUBMIT ENCUMBRANCE METHODS -- ***/

function toggleDiv(divID, linkID) {
    $("#" + divID).toggle();
    var linkText = $("#" + linkID).text();
    if (linkText.indexOf("Collapse") > -1) {
        linkText = linkText.replace("Collapse", "Expand");
    } else if (linkText.indexOf("Expand") > -1) {
        linkText = linkText.replace("Expand", "Collapse");
    }
    $("#" + linkID).text(linkText);
}

function toggleEncumbrance(header) {
    $("#groupHeader_" + header).nextUntil("tr.groupHeader").slideToggle(1000);
    var linkText = $("#toggleLink_" + header).text();
    if (linkText === "Show encumbrance items") {
        $("#toggleLink_" + header).text("Hide encumbrance items");
    } else {
        if (linkText === "Hide encumbrance items") $("#toggleLink_" + header).text("Show encumbrance items");
    }
}

function toggleEncumbrancePanel() {
    if ($("#EncumbranceToggleDiv").text().indexOf("Collapse") >= 0) {
        $("#EncumbrancePanelBody").addClass("collapse");
        $("#EncumbranceToggleDiv").html("<a href='javascript:toggleEncumbrancePanel()'>Expand</a>");
    } else {
        $("#EncumbrancePanelBody").removeClass("collapse");
        $("#EncumbranceToggleDiv").html("<a href='javascript:toggleEncumbrancePanel()'>Collapse</a>");
    }
}

function toggleContractPanel() {
    if ($("#ContractToggleDiv").text().indexOf("Collapse") >= 0) {
        $("#ContractPanelBody").addClass("collapse");
        $("#ContractToggleDiv").html("<a href='javascript:toggleContractPanel()'>Expand</a>");
    } else {
        $("#ContractPanelBody").removeClass("collapse");
        $("#ContractToggleDiv").html("<a href='javascript:toggleContractPanel()'>Collapse</a>");
    }
}

function toggleLineItemsPanel() {
    if ($("#LineItemsToggleDiv").text().indexOf("Collapse") >= 0) {
        $("#LineItemsPanelBody").addClass("collapse");
        $("#LineItemsToggleDiv").html("<a href='javascript:toggleLineItemsPanel()'>Expand</a>");
    } else {
        $("#LineItemsPanelBody").removeClass("collapse");
        $("#LineItemsToggleDiv").html("<a href='javascript:toggleLineItemsPanel()'>Collapse</a>");
    }
}

/*** USERS METHODS ***/
// Users/Edit updates list of roles when user changes role selection for User object
function concatenateSelectedRoles() {
    var hiddenField = $("#userRoles");
    var listOfRoles = "";
    $(":checkbox").each(function () {
        if (this.checked) {
            listOfRoles = listOfRoles + this.value;
        }
    });
    hiddenField.val(listOfRoles);
}

function toggleDisabledUsers() {
    if ($("#showDisabledCheckbox").is(":checked")) {
        $(".disabledUser").show();
    } else {
        $(".disabledUser").hide();
    }
}

function updateUserIsDisabled() {
    if ($("#reEnable").is(":checked")) {
        $("#User_IsDisabled").val(1);
    } else {
        $("#User_IsDisabled").val(0);
    }
}

/*** END OF -- USERS METHODS -- ***/

function toggleCommentHistory(groupID) {
    if ($("#commentHistoryToggle").text() === "Show Encumbrance History") {
        $(".groupStatus").removeClass("hidden");
        $("#commentHistoryToggle").text("Hide Encumbrance History");
    } else {
        $(".groupStatus").addClass("hidden");
        $("#commentHistoryToggle").text("Show Encumbrance History");
    }
}

/***  CONTRACT DIALOG METHODS  ****/

function openContractDialog() {
    // Open for New Contract. If Contract ID is assigned or Contract Panel is populated, clear it out.
    clearContract();
    //Open the dialog
    $("#ContractDialog").dialog("open");
    $("#ContractSelector").hide();

    $("#ContractTypeSelector").autocomplete("option", "appendTo", "#ContractDialog");
    $("#VendorSelector").autocomplete("option", "appendTo", "#ContractDialog");
    if ($("#ContractSelector").val() === "NEW") { $("#ContractNumber").val("NEW"); }
    $("#ContractNumber").focus();
}

function clearContract() {
    $("#ContractID").val(0);
    $("#ContractSelector").val("");
    $("#ContractPanelBody").html("");
    $("#ContractTitle").text("Contract");
    $("#EncHeaderContract").html("");
    $("#ContractPanel").hide();
}

function openContractDialogExisting(id) {
    $("#ContractDialog").dialog("open");
    $("#ContractSelector").hide();
    $("#ContractTypeSelector").autocomplete("option", "appendTo", "#ContractDialog");
    $("#VendorSelector").autocomplete("option", "appendTo", "#ContractDialog");
    $("#ContractNumber").focus();
}

function findMatchingContract() {
    var contractNumber = $("#ContractNumber").val();
    contractNumber = contractNumber.toUpperCase();
    if (contractNumber.length > 0) {
        $.ajax({
            autoFocus: true,
            url: "/LineItemGroups/ExactMatchContract",
            type: "POST",
            dataType: "json",
            data: { searchString: contractNumber },
            success: function (data) {
                var contractList = "";
                $.map(data, function (item) {
                    contractList += item.contractNumber + "(ID=" + item.contractID + "), ";
                });
                contractList = contractList.substr(0, contractList.length - 2);
                if (contractList.length > 0 && contractNumber.toUpperCase() !== "NEW") {
                    displayContractMessage("A contract with the Contract Number " + contractNumber + " already exists.");
                    $("#DuplicateContract").val("true");
                } else {
                    displayContractMessage("");
                    $("#DuplicateContract").val("false");
                }
            }
        });
    }
}
// called from Contract Information dialog and from /Contracts/Create and /Contracts/Edit
function updateBudgetCeiling() {
    // if Compensation is 3, 4, or 5, then Budget Ceiling cannot be $0
    var comp = $("#CompensationID").val() || $("#Contract_CompensationID").val();
    if (comp === null) {
        comp = $("#Contract_CompensationID").val();
    }
    if ((comp === "3" || comp === "4" || comp === "5") && (!$("#BudgetCeiling").val() || $("#BudgetCeiling").val() < 1)) {
        $("#budgetCeilingMessage").text("A Budget Ceiling greater than $0 is required.");
    } else {
        $("#budgetCeilingMessage").text("");
    }
}

// Contract validation for /Contracts/Create
// Most of the validation is handled by Razor validation
function validateCreateContract() {
    var canSubmit = true;
    if ($("#VendorID").val() === null || $("#VendorID").val() === undefined || $("#VendorID").val().length < 1) { $("#VendorID").val("1"); } // set Vendor to AD if not set
    // validate ContractType
    if ($("#ContractTypeID").val() === null || $("#ContractTypeID").val() === undefined || $("#ContractTypeID").val().length < 1) {
        var failString = "Please select a Contract Type before submittting.";
        $("#ContractTypeValidation").html("<font color='red'>" + failString + "</font>");
        canSubmit = false;
    } else {
        $("#ContractTypeValidation").html();
    }
    // validate dollar amounts
    // ContractTotal is not set, it gets calculated later
    if ($("#ContractTotal").val() === null || $("#ContractTotal").val() === undefined || $("#ContractTotal").val().length < 1) { $("#ContractTotal").val("0.00"); } // set ContractTotal to 0 if not set
    if ($("#MaxLoaAmount").val() === null || $("#MaxLoaAmount").val() === undefined || $("#MaxLoaAmount").val().length < 1) { $("#MaxLoaAmount").val("0.00"); } // set MaxLOA to 0 if not set
    if ($("#CompensationID").val() === "4") {
        if ($("#BudgetCeiling").val() === "0" || $("#BudgetCeiling").val() === "0.00") {
            $("#BudgetCeiling").val("");
            $("#BudgetCeiling").focus();
        }
    } else {
        if ($("#BudgetCeiling").val() === null || $("#BudgetCeiling").val().length < 1) { $("#BudgetCeiling").val("0"); }
    }
    if ($("#ContractTotal").val() > $("#BudgetCeiling").val()) {
        // warn that budget ceiling must be greater than contract total
        var warnString = "Budget Ceiling must be more than Contract Initial Amount.";
        $("#budgetCeilingMessage").text(warnString);
        canSubmit = false;
    }
    if (canSubmit) {
        $("#createContractForm").submit();
    }
    displayLineItemsPanelOrMessage();
    return false;
}

// Validation for NewPartialContract form (Contract Information dialog) in /LineItemGroups/Manage
// Validation for this form is comprehensive vs. /Contracts/Create page that use Razor validation
function validateContractDialog() {
    // Check all required fields on the Contract form for valid values
    var isErrorFree = true; // set to false when an error is found
    var msg = "";
    displayContractMessage(msg);
    if ($("#DuplicateContract").val() === "true") {
        msg += "The Contract Number must be unique. <br/>";
        isErrorFree = false;
    }

    if (!$("#ContractTypeID").val()) {
        msg += "Please select a Contract Type. <br/>";
        isErrorFree = false;
    }
    if (!($("#IsRenewable0").is(':checked') || $("#IsRenewable1").is(':checked'))) {
        $("#IsRenewable1").prop("checked", true);
    }
    if (!$("#ContractTotal").val()) {
        $("#ContractTotal").val(0.0);
    }
    if (!$("#MaxLoaAmount").val()) {
        $("#MaxLoaAmount").val(0.0);
    }
    if (!$("#BudgetCeiling").val()) {
        $("#BudgetCeiling").val(0.0);
    }
    updateBudgetCeiling();
    if (!$("#BeginningDate").val()) {
        msg += "Please select a Beginning Date. <br/>";
        isErrorFree = false;
    }
    if (!$("#EndingDate").val()) {
        msg += "Please select an Ending Date. <br/>";
        isErrorFree = false;
    }
    // Per Lorna: Service End Date is not required.
    if (!$("#ServiceEndingDate").val()) {
        //msg += "Please select a Service Ending Date. <br/>";
        //isErrorFree = false;
        $("#ServiceEndingDate").val("01/01/1999");
    }
    if (!$("#ProcurementID").val()) {
        msg += "Please select a Procurement value. <br/>";
        isErrorFree = false;
    }
    if (!$("#CompensationID").val()) {
        msg += "Please select Contract Funding Terms. <br/>";
        isErrorFree = false;
    }
    if (!$("#VendorID").val()) {
        msg += "Please select a Vendor. <br/>";
        isErrorFree = false;
    }
    if (!$("#RecipientID").val()) {
        msg += "Please select a Recipient. <br/>";
        isErrorFree = false;
    }
    // use displayMessage() to show validation message.
    displayContractMessage(msg);
    return isErrorFree;
}

function saveContractModal() {
    // javascript model of the Contract object  populated it from the dialog
    var Contract = {};
    Contract.BeginningDate = $("#BeginningDate").val();
    Contract.BudgetCeiling = formatDecimal($("#BudgetCeiling").val());
    Contract.CompensationID = $("#CompensationID").val();
    Contract.ContractID = $("#ContractID").val();
    if (Contract.ContractID === null || Contract.ContractID === "") { Contract.ContractID = 0; }
    Contract.ContractNumber = $("#ContractNumber").val();
    //Contract.ContractTotal = formatDecimal($("#ContractTotal").val());  // Contract Total is calculated, not set.
    Contract.ContractTypeID = $("#ContractTypeID").val(); // blank
    Contract.CurrentStatus = $("#CurrentStatus").val();
    Contract.DescriptionOfWork = $("#DescriptionOfWork").val();
    Contract.EndingDate = $("#EndingDate").val();
    Contract.IsRenewable = 0;
    if ($("#IsRenewable1").is(":checked")) { Contract.IsRenewable = 1; }
    Contract.MaxLoaAmount = formatDecimal($("#MaxLoaAmount").val());
    Contract.ModifiedDate = $("#ModifiedDate").val(); // set in save method
    Contract.ProcurementID = $("#ProcurementID").val();
    Contract.RecipientID = $("#RecipientID").val();
    Contract.UserID = $("#UserID").val();
    Contract.ServiceEndingDate = $("#ServiceEndingDate").val();
    Contract.VendorID = $("#VendorID").val(); // undefined

    // Submit the Contract to the database with ajax
    $.ajax({
        url: "/Contracts/AddNewContract",
        type: "POST",
        dataType: "json",
        data: { contract: JSON.stringify(Contract) },
        success: function (data) {
            var result = JSON.parse(data);
            // return the completed Contract object to the calling form and use it to populate ContractPanel div
            populateContractPanel(result);

            $("#ContractSelector").val(result.ContractNumber);
            $("#ContractID").val(result.ContractID);
            if ($("#ContractID").val() && $("#ContractID").val() > 0 && $("#LineItemGroupID").val() && $("#LineItemGroupID").val() > 0) {
                $("#LineItemsPanel").show();
                // show FileAttachmentsPanel if it exists
                if ($("#FileAttachmentsPanel").length !== undefined && $("#FileAttachmentsPanel").length > 0) {
                    $("#FileAttachmentsPanel").show();
                }
            }
        }
    });
}
/***  END OF -- CONTRACT DIALOG METHODS -- ****/


/***  LINE ITEM DIALOG METHODS ****/

function getLineOrder() {
    // return number of LineItemTable rows + 1
    var maxRow = 1;
    $("[id^='row_item']").each(function () {
        maxRow++;
    });
    return maxRow;
}

function openLineItemDialog(callback) {
    var userID = $("#UserID").val();
    var contractID = $("#ContractID").val();
    var lineItemGroupID = $("#LineItemGroupID").val();
    var lineOrder = getLineOrder();
    if (!contractID) {
        //showComment("Please select or add a contract before adding or editing a Line Item.")
        return;
    }
    if (!lineItemGroupID) {
        //showComment("Please Save As Draft before adding a Line Item.")
        return;
    }
    $("#LineItemDialog").dialog("open");
    if ($("#LineNumber") && !$("#LineNumber").val()) {
        $("#LineNumber").val(lineOrder);
    }

    $("#ContractSelector").hide();

    $("#CategorySelector").autocomplete("option", "appendTo", "#LineItemDialog");
    $("#OCASelector").autocomplete("option", "appendTo", "#LineItemDialog");
    $("#FundSelector").autocomplete("option", "appendTo", "#LineItemDialog");
    bindCurrencyField();
    //callback();
}

// OCA validation message for ROW state program value
function updateOCAMessage() {
    // if State Program is 5, OCA must be a Right of Way value
    var sp = $("#StateProgramID").val();
    if (sp === 5) {
        $("#OCAMessage").text("Please select a Right of Way value for OCA.");
    } else {
        $("#OCAMessage").text("");
    }
}

// in LineItem dialog, called from /LineItemGroups/Manage
// This ensure the Org Code always begins with "55-"
function bindOrgCodeKeyup() {
    // preface OrgCode with "55-"
    $("#OrgCode").keyup(function () {
        var orgCode = $("#OrgCode").prop("value");
        while (orgCode.charAt(0) === "5" || orgCode.charAt(0) === "-") {
            orgCode = orgCode.substring(1, orgCode.length);
        }
        $("#OrgCode").val("55-" + orgCode);
    });
}

// in LineItems dialog, prepare the Fiscal Year dropdown
function populateFiscalYearList(selectedValue) {
    var fiscalYear = $('#FiscalYearList');
    var currentYear = new Date().getFullYear();
    var currentMonth = new Date().getMonth();
    if (currentMonth > 5) {
        currentYear = currentYear + 1;
    }
    var list = '';
    if (!selectedValue) {
        selectedValue = currentYear;
    }
    for (var i = currentYear - 5; i < currentYear + 10; i++) {
        if (selectedValue && (i + 1) === selectedValue) {
            list += '\n<option selected value="' + (i + 1) + '" >' + i + ' - ' + (i + 1) + '</option>';
        } else {
            list += '\n<option value="' + (i + 1) + '" >' + i + ' - ' + (i + 1) + '</option>';
        }
    }
    $('#FiscalYear').val(selectedValue);
    fiscalYear.append(list);
}

function initLineItemControls() {
    bindOrgCodeKeyup();
    //Category
    $("#CategorySelector").autocomplete({
        source: function (request, response) {
            $.ajax({
                autoFocus: true,
                url: "/LineItems/ListCategories",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    var counter = 0;
                    var lastSelector = "";
                    var lastID = 0;
                    response($.map(data, function (item) {
                        var CategorySelector = item.categoryCode + " - " + item.categoryName;
                        lastSelector = CategorySelector;
                        lastID = item.categoryID;
                        counter++;
                        $("#CategoryIDValidation").hide();
                        return { label: CategorySelector, value: item.categorySelector, CategoryID: item.categoryID };
                    }));
                    // if autocomplete has a single match, select it
                    if (counter === 1) {
                        $("#CategorySelector").val(lastSelector);
                        $("#CategoryID").val(lastID);
                    }
                }
            });
        },
        select: function (event, ui) {
            $("#CategorySelector").val(ui.item.label);
            $("#CategoryID").val(ui.item.CategoryID);
            return false;
        }
    });

    //OCA
    $("#OCASelector").autocomplete({
        source: function (request, response) {
            $.ajax({
                autoFocus: true,
                url: "/LineItems/ListOCAs",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    var counter = 0;
                    var lastSelector = "";
                    var lastID = 0;
                    response($.map(data, function (item) {
                        var OCASelector = item.ocaCode + " - " + item.ocaName;
                        lastSelector = OCASelector;
                        lastID = item.ocaid;
                        counter++;
                        $("#OCAIDValidation").hide();
                        return { label: OCASelector, value: item.ocaSelector, OCAID: item.ocaid };
                    }));
                    // if autocomplete has a single match, select it
                    if (counter === 1) {
                        $("#OCASelector").val(lastSelector);
                        $("#OCAID").val(lastID);
                    }
                }
            });
        },
        select: function (event, ui) {
            $("#OCASelector").val(ui.item.label);
            $("#OCAID").val(ui.item.OCAID);
            return false;
        }
    });


    //Fund
    $("#FundSelector").autocomplete({
        source: function (request, response) {
            $.ajax({
                autoFocus: true,
                url: "/LineItems/ListFunds",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    var counter = 0;
                    var lastSelector = "";
                    var lastID = 0;
                    response($.map(data, function (item) {
                        var FundSelector = item.fundCode + " - " + item.fundDescription;
                        $("#FundIDValidation").hide();
                        lastSelector = FundSelector;
                        lastID = item.fundID;
                        counter++;
                        return { label: FundSelector, value: item.fundSelector, FundID: item.fundID };
                    }));
                    // if autocomplete has a single match, select it
                    if (counter === 1) {
                        $("#FundSelector").val(lastSelector);
                        $("#FundID").val(lastID);
                    }
                }
            });
        },
        select: function (event, ui) {
            $("#FundSelector").val(ui.item.label);
            $("#FundID").val(ui.item.FundID);
            return false;
        }
    });

    //FY list
    $("#FiscalYearList").change(function () {
        $("#FiscalYear").val($("#FiscalYearList").val());
        $("#LineItem_FiscalYear").val($("#FiscalYearList").val());
        return false;
    }).change();
}

function editLineItem(lineItemID, isDuplicate) {
    var lineItem = JSON.parse($("#json_item_" + lineItemID).val());
    openLineItemDialog();

    // populate the form in the callback
    var orgCode = lineItem.OrgCode;
    if (orgCode.indexOf("55-") < 0) {
        $("#OrgCode").val("55-" + lineItem.OrgCode);
    } else {
        $("#OrgCode").val(lineItem.OrgCode);
    }
    var amount = parseFloat(formatDecimal(lineItem.Amount));
    $("#Amount").val(amount);
    $("#FiscalYearList").val(lineItem.FiscalYear);
    $("#CategorySelector").val(lineItem.Category.CategorySelector);
    $("#CategoryID").val(lineItem.CategoryID);
    $("#FundSelector").val(lineItem.Fund.FundSelector);
    $("#FundID").val(lineItem.FundID);
    $("#FinancialInformationFormPanel #FlairAmendmentID").val(lineItem.FlairAmendmentID);
    $("#FlairObject").val(lineItem.FlairObject);
    $("#FinancialInformationFormPanel #LineID6S").val(lineItem.LineID6S);
    $("#OCASelector").val(lineItem.OCA.OCASelector);
    $("#OCAID").val(lineItem.OCAID);
    $("#StateProgramName").val(lineItem.StateProgram.ProgramSelector);
    $("#StateProgramID").val(lineItem.StateProgramID);
    $("#FinancialProjectNumber").val(lineItem.FinancialProjectNumber);
    $("#WorkActivity").val(lineItem.WorkActivity);
    $("#Comments").val(lineItem.Comments);
    $("#ExpansionObject").val(lineItem.ExpansionObject);
    if (isDuplicate) {
        // #LineNumber val gets set in the callback
        getNextLineNumber("{\"groupID\" : " + lineItem.LineItemGroupID + "}");
        $("#LineItemNumber").text("");
        $("#LineItemID").val(0);
        $("#LineNumber").val(getLineOrder());
    } else {
        $("#LineItemID").val(lineItem.LineItemID);
        $("#LineItemNumber").text(lineItem.LineItemID);
        $("#LineNumber").val(lineItem.LineNumber);
    }
    populateFiscalYearList(lineItem.FiscalYear);
    showHideNegativeAmountOptions();
}

function getNextLineNumber(groupID) {
    $.ajax({
        url: "/LineItemGroups/GetNextLineNumber",
        type: "POST",
        dataType: "json",
        data: { groupInfo : groupID },
        success: function (data) {
            var result = JSON.parse(data);
            var nextLine = parseInt(result) + 1;
           // $("#LineNumber").val(nextLine);
        },
        fail: function (data) {
            return 0;
        }
    });
}

function deleteLineItem(lineItemID) {
    $.ajax({
        url: "/LineItems/DeleteLineItem",
        type: "POST",
        dataType: "json",
        data: { lineItemID: lineItemID },
        success: function (data) {
            var result = JSON.parse(data);
            showComment(result.success, "Financial Line deleted");
            // remove the line item from the table
            $("#row_item_" + lineItemID).remove();
            setEncumbranceTotal();        },
        fail: function (data) {
            var result = JSON.parse(data);
            showComment(result.fail, "Financial Line not deleted");
        }
    });
}

function validateLineItem() {
    // Check all required fields on the LineItem form for valid values
    var isErrorFree = true; // set to false when an error is found
    var msg = "";

    var orgCode = $("#OrgCode").val();
    if (!orgCode) {
        msg += "Please enter a valid Organization Code. <br/>";
        isErrorFree = false;
    } else {
        if (orgCode.indexOf("55-") >= 0 && orgCode.length !== 12
            || orgCode.indexOf("55-") < 0 && orgCode.length !== 9) {
            msg += "The Organization Code must be \"55-\" followed by 9 digits. <br/>";
            isErrorFree = false;
        }
    }
    var finProjNum = $("#FinancialProjectNumber").val();
    if (!finProjNum) {
        msg += "Please enter an 11 character Financial Project Number. <br/>";
        isErrorFree = false;
    } else {
        if (finProjNum.length !== 11) {
            msg += "The Financial Project Number must be 11 characters long. <br/>";
            isErrorFree = false;
        }
    }
    if (!$("#StateProgramID").val()) {
        msg += "Please select a State Program. <br/>";
        isErrorFree = false;
    }
    if (!$("#CategoryID").val()) {
        msg += "Please select a Category. <br/>";
        isErrorFree = false;
    }
    var workActivity = $("#WorkActivity").val();
    if (!workActivity) {
        msg += "Please enter a three digit Work Activity. <br/>";
        isErrorFree = false;
    } else {
        if (workActivity.length !== 3) {
            msg += "The Work Activity must be three digits long. <br/>";
            isErrorFree = false;
        }
    }
    if (!$("#OCAID").val()) {
        msg += "Please select a valid OCA. <br/>";
        isErrorFree = false;
    }
    var eo = $("#ExpansionObject").val();
    if (!eo) {
        msg += "Please enter a two character Expansion Option. <br/>";
        isErrorFree = false;
    } else {
        if (eo.length !== 2) {
            msg += "The EO must be two characters long. <br/>";
            isErrorFree = false;
        }
    }
    var objCode = $("#FlairObject").val();
    if (!objCode) {
        msg += "Please select a FLAIR Object Code. <br/>";
        isErrorFree = false;
    } else {
        if (objCode.length !== 6) {
            msg += "The FLAIR Object Code must be six digits long. <br/>";
            isErrorFree = false;
        }
    }
    if (!$("#FundID").val()) {
        msg += "Please select a valid Fund. <br/>";
        isErrorFree = false;
    }
    if (!$("#Amount").val()) {
        $("#Amount").val(0.0);
    } else if ($("#Amount").val() < 0) {
        // require FLAIR ID and 6sID when Amount is negative
        if (!$("#FlairAmendmentID").val()) {
            msg += "A FLAIR Amendment ID is required for negative Amounts. <br/>";
            isErrorFree = false;
        }
        if (!$("#LineID6S").val()) {
            msg += "A 6s line ID is required for negative Amounts. <br/>";
            isErrorFree = false;
        }
    }
    // use displayMessage() to show validation message.
    displayLineItemMessage(msg);
    return isErrorFree;
}


function saveLineItemModal() {
    // build a javascript model of the LineItem object and populate it from the form
    var lineItem = {};
    // populate information from parent objects
    if ($("#UserID")) {
        lineItem.UserID = $("#UserID").val();
    }
    if ($("#ContractID")) {
        lineItem.ContractID = $("#ContractID").val();
    }
    if ($("#LineItemGroupID")) {
        if ($("#LineItemGroupID").val() === 0) {
            saveEncumbrance(getDefaultSaveComment());
        }
        lineItem.LineItemGroupID = $("#LineItemGroupID").val();
    }

    // these properties are identical to the line item group
    // populate them from the Create page, if they have values
    if ($("#LineItemType") && $("#LineItemType").val().length > 0) {
        lineItem.LineItemType = $("#LineItemType").val();
    } else {
        lineItem.LineItemType = "New";
    }
    if ($("#FinancialInformationFormPanel #FlairAmendmentID") && $("#FinancialInformationFormPanel #FlairAmendmentID").val()) {
        lineItem.FlairAmendmentID = $("#FinancialInformationFormPanel #FlairAmendmentID").val();
    } else {
        lineItem.FlairAmendmentID = "";
    }
    if ($("#FinancialInformationFormPanel #LineID6S") && $("#FinancialInformationFormPanel #LineID6S").val()) {
        lineItem.LineID6S = $("#FinancialInformationFormPanel #LineID6S").val();
    } else {
        lineItem.LineID6S = "";
    }
    if ($("#FinancialInformationFormPanel #UserAssignedID") && $("#FinancialInformationFormPanel #UserAssignedID").val()) {
        lineItem.UserAssignedID = $("#FinancialInformationFormPanel #UserAssignedID").val();
    } else {
        lineItem.UserAssignedID = "";
    }
    if ($("#FinancialInformationFormPanel #AmendedLineItemID") && $("#FinancialInformationFormPanel #AmendedLineItemID").val()) {
        lineItem.AmendedLineItemID = $("#FinancialInformationFormPanel #AmendedLineItemID").val();
    } else {
        lineItem.AmendedLineItemID = "";
    }
    // populate these properties from the LineItemDialog
    lineItem.LineItemID = $("#LineItemID").val();
    if (!$("#LineNumber").val()) {
        lineItem.LineNumber = 0;
    } else {
        lineItem.LineNumber = $("#LineNumber").val();
    }
    lineItem.Amount = formatDecimal($("#Amount").val());
    lineItem.FiscalYear = $("#FiscalYearList").val();
    lineItem.OrgCode = $("#OrgCode").val();
    lineItem.CategoryName = $("#CategorySelector").val();
    lineItem.CategoryID = $("#CategoryID").val();
    lineItem.FundName = $("#FundSelector").val();
    lineItem.FundID = $("#FundID").val();
    lineItem.OcaName = $("#OCASelector").val();
    lineItem.OcaID = $("#OCAID").val();
    lineItem.StateProgramName = $("#StateProgramID").text();
    lineItem.StateProgramID = $("#StateProgramID").val();
    lineItem.ExpansionObject = $("#ExpansionObject").val();
    lineItem.FinancialProjectNumber = $("#FinancialProjectNumber").val();
    lineItem.FlairObject = $("#FlairObject").val();
    lineItem.WorkActivity = $("#WorkActivity").val();
    lineItem.Comments = $("#Comments").val();
    if (!lineItem.LineItemID) {
        lineItem.LineItemID = 0;
    }
    if (!lineItem.LineItemGroupID) {
        lineItem.LineItemGroupID = 0;
    }
    if (!lineItem.ContractID) {
        lineItem.ContractID = 0;
        if ($("#ContractID").val() > 0) {
            lineItem.ContractID = $("#ContractID").val();
        }
    }

    // Submit the Contract to the database with ajax
    $.ajax({
            url: "/LineItems/AddNewLineItem",
            type: "POST",
            dataType: "json",
        data: { lineItem: JSON.stringify(lineItem) },
        success: function (data) {
            var result = JSON.parse(data);
            // return the completed Contract object to the calling form and use it to populate ContractPanel div
            var newRow = getNewLineItemRow(result);
            if ($("#row_item_" + result.LineItemID).html()) {
                $("#row_item_" + result.LineItemID).replaceWith(newRow);
                displayLineItemPanelMessage("Line Item number " + result.LineNumber + " updated.");
            } else {
                $("#LineItemsTableBody:last-child").append(newRow);
                displayLineItemPanelMessage("Line Item number " + result.LineNumber + " added.");
            }
            // reset the encumbrance total amount
            setEncumbranceTotal();
            setContractAmountTotal();
            clearLineItemsDialog();
            // show the line items table
            $("#LineItemsTable").show();
        }
    });
}

function getNewLineItemRow(lineItem) {
    var itemKey = "item_" + lineItem.LineItemID;
    var rowID = "row_" + itemKey;
    var tableText = "";
    var FY = parseInt(lineItem.FiscalYear.substr(0, 4)) + 1;
 
    if (lineItem.OrgCode.indexOf("55-") < 0) { lineItem.OrgCode = "55-" + lineItem.OrgCode; }
    tableText += "<tr class='groupItem' id='" + rowID + "'>";
    tableText += "<td id='" + itemKey + "_LineItemNumber'>" + lineItem.LineItemNumber + "<input type='hidden' id='" + itemKey + "_LineItemID' value='" + lineItem.LineItemID + "'/> <br />";
    if ($("#UserRoles").val().indexOf("Originator") >= 0 || $("#UserRoles").val().indexOf("Finance") >= 0 && $("#CurrentStatus").val().indexOf("Finance") >= 0
        || $("#UserRoles").val().indexOf("CFM") >= 0 && $("#CurrentStatus").val().indexOf("Complete") >= 0) {
        tableText += "<a href='javascript:editLineItem(" + lineItem.LineItemID + ", false)' > Edit</a > <br />";
        tableText += "<a href='javascript:editLineItem(" + lineItem.LineItemID + ", true)'>Duplicate</a> <br />";
        tableText += "<a href='javascript:deleteLineItem(" + lineItem.LineItemID + ")'>Delete</a> <br />";
    }
    tableText += "</td>";
    tableText += "<td>" + lineItem.LineItemID;
    if (lineItem.FlairAmendmentID !== null && lineItem.FlairAmendmentID !== undefined && lineItem.FlairAmendmentID.length > 0) {
        tableText += "<br/>" + lineItem.FlairAmendmentID;
    }
    if (lineItem.LineID6S !== null && lineItem.LineID6S !== undefined && lineItem.LineID6S.length > 0) {
        tableText += "<br/> <strong>6s: </strong>" + lineItem.LineID6S;
    }
    if (lineItem.Comments !== null && lineItem.Comments !== undefined && lineItem.Comments.length > 0) {
        tableText += "<br/> <span title=\"" + lineItem.Comments + "\">Comments</span>";
    }
    tableText += "</td>";
    tableText += "<td>" + lineItem.FinancialProjectNumber + "</td>";
    tableText += "<td>" + (FY - 1) + " - " + FY;
    tableText += "<input type='hidden' id='FY_" + lineItem.LineItemID + "' name='FY_" + lineItem.LineItemID + "' value=" + lineItem.FiscalYear + "'>";
    tableText += "<span id='FYWarning_" + lineItem.LineItemID + "' name='FYWarning_" + lineItem.LineItemID + "' class='FYWarning' style=\"display: none\" title='This item occurs after the ending date of the contract.'>*</span>";
    tableText += "</td>";
    var fund_array = lineItem.FundName.split("-");
    tableText += "<td title='" + fund_array[1].trim() + "'>" + fund_array[0].trim() + "<input type='hidden' id='" + itemKey + "_FundID' value='" + lineItem.FundID + "'/> </td>";
    tableText += "<td>" + lineItem.OrgCode + "</td>";
    var cn_array = lineItem.CategoryName.split("-");
    tableText += "<td title='" + cn_array[1].trim() + "'>" + cn_array[0].trim() + "<input type='hidden' id='" + itemKey + "_CategoryID' value='" + lineItem.CategoryID + "'/> </td>";
    tableText += "<td>" + lineItem.FlairObject + "</td>";
    tableText += "<td>" + lineItem.WorkActivity + "</td>";
    var oca_array = lineItem.OcaName.split("-");
    tableText += "<td title='" + oca_array[1].trim() + "'>" + oca_array[0].trim() + "<input type='hidden' id='" + itemKey + "_OCAID' value='" + lineItem.OcaID + "'/> </td>";
    var sp_array = lineItem.StateProgramName.split("-");
    tableText += "<td title='" + sp_array[1].trim() + "'>" + sp_array[0].trim() + "<input type='hidden' id='" + itemKey + "_StateProgramID' value='" + lineItem.StateProgramID + "'/> </td>";
    tableText += "<td>" + lineItem.EO + "</td>";
    var cleanAmount = formatDecimal(lineItem.Amount);
    tableText += "<td>" + lineItem.Amount + "<input type='hidden' id='" + itemKey + "_Amount' value='" + cleanAmount + "'/>";

    //fixes to lineItem before stringifying
    lineItem.FiscalYear = FY;
    var Category = {};
    Category.CategorySelector = lineItem.CategoryName;
    Category.CategoryID = lineItem.CategoryID;
    lineItem.Category = Category;
    var Fund = {};
    Fund.FundSelector = lineItem.FundName;
    Fund.FundID = lineItem.FundID;
    lineItem.Fund = Fund;
    var OCA = {};
    OCA.OCASelector = lineItem.OcaName;
    OCA.OCAID = lineItem.OcaID;
    lineItem.OCA = OCA;
    lineItem.OCAID = lineItem.OcaID;
    var StateProgram = {};
    StateProgram.ProgramSelector = lineItem.StateProgramName;
    StateProgram.StateProgramID = lineItem.StateProgramID;
    lineItem.StateProgram = StateProgram;
    lineItem.ExpansionObject = lineItem.EO;
    lineItem.Comments = lineItem.Comments.replace(/'/g,"&#39;");
    lineItem.LineNumber = lineItem.LineItemNumber;
    lineItem.Amount = cleanAmount;
    var jsonString = JSON.stringify(lineItem);

    tableText += "<input type='hidden' id='json_" + itemKey + "' value='" + jsonString + "'/> </td>";
    tableText += "</tr>";
    return tableText;
}

function clearLineItemsDialog() {
    $("#LineItemNumber").text("");
    $("#LineItemDialog *").filter(':input').each(function () {
        $(this).val("");
    });
}

/*** END OF -- LINE ITEM DIALOG METHODS -- ****/


function getFormattedDateNow() {
    var today = new Date();
    var dd = today.getDate();
    var mm = today.getMonth() + 1; //January is 0!

    var yyyy = today.getFullYear();
    if (dd < 10) {
        dd = '0' + dd;
    }
    if (mm < 10) {
        mm = '0' + mm;
    }
    var thedate = mm + '/' + dd + '/' + yyyy;
    return thedate;
}

function formatDate(dateString) {
    return dateString.mm + "/" + dateString.dd + "/" + dateString.yyyy;
}

function formatDateTime(datetimeString) {
    // input string looks like this: "2019-06-05T10:10:18.587"
    var dateMatch = datetimeString.match(/(\d{4}-\d{2}-\d{2})/);
    var timeMatch = datetimeString.match(/T(\d{2}):(\d{2}):/);
    var hourNum = parseInt (timeMatch[1]);
    var minNum = parseInt(timeMatch[2]);
    var ampm = "AM";
    var hourString = "00";
    var minString = "00";
    if (hourNum > 12) {
        hourNum -= 12;
        ampm = "PM";
    }
    if (hourNum < 10) {
        hourString = "0" + hourNum;
    } else {
        hourString = hourNum.toString();
    }
    if (hourNum === 0) {
        hourString = "12";
    }
    if (minNum < 10) {
        minString = "0" + minNum;
    } else {
        minString = minNum.toString();
    }
    var timeString = hourString + ":" + minString + " " + ampm;
    // return a string like this: "2019-06-05 10:10 AM"
    return dateMatch[1] + " " + timeString;
}


function formatDecimal(amount) {
    // takes a string or decimal argument
    // returns a decimal formatted to two decimal places
    if (amount === null || amount === undefined) { return 0.00; }
    var nAmount = Number(0);
    if (typeof amount === "string") {
        var sAmount = amount.replace(/,/g, "").replace("$", "").replace("(", "-").replace(")", "");
        nAmount = parseFloat(sAmount);
    } else {
        nAmount = amount;
    }
    return nAmount.toFixed(2);
}

function setDecimal(sourceControl, targetControlname) {
    $("#" + targetControlname).val(formatDecimal(sourceControl.value));
}

function formatCurrency(amount) {
    // takes a decimal and formats it to US Dollars with thousands separators
    const formatter = new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        minimumFractionDigits: 2
    });
    return formatter.format(amount);
}

function bindCurrencyField() {
    // amount input fields allow only numeric, $, comma, decimal point, and minus
    $(".currency").keypress(function (event) {
        if (event.which !== 0) {
            var keyChar = String.fromCharCode(event.keyCode);
            var isAllowedCharacter = keyChar.match(/[\$,\-,\,,\.,\d]/);
            if (isAllowedCharacter === null) { event.preventDefault(); }
        }
    });
}

/***  SAVE LINEITEMGROUP METHODS ***/

// called from /LineItemGroups/Manage to save LineItemGroup
function saveInitialEncumbrance() {
    // Call updateGroupStatus to save the encumbrance
    // Save silently with no comment dialog
    // Populate the LineItemGroupID and enable Financial Information
    updateGroupStatus("Draft", "true");

    // Now that this is saved, hide the Contract Information button
    if ($("#ContractPanel").is(":visible")) {
        $("#OpenContractInformationDiv").hide();
        $("#OpenContractInformationSpan").hide();
    }
    // Open the Financial Information Form
    if (validateEncumbrance()) {
        openLineItemDialog();
    }
}

// called from /LineItemGroups/Manage part of saving a LineItemGroup
function updateGroupStatus(status, silent) {
    if (validateEncumbrance()) {
        if (silent === "true") {
            var commentJson = getDefaultSaveComment();
            saveEncumbrance(commentJson);
            // Now that the Financial Information section is showing, Hide the button
            if ($("#LineItemsPanel").is(":visible")) {
                $("#saveEncumbranceLink").hide();
            }
        } else {
            if (status === "Work Program") {
                var wpUsers = getWPUsers(status);
            } else {
                openEncumbranceSubmissionDialog(status, null);
            }
        }
    }
}

function getDefaultSaveComment() {
    // skip the Encumbrance Submission Dialog
    // return a default comment in a JSON string
    // status is Draft
    var jsonString = "{";
    jsonString += "\"status\" : \"" + 'Draft' + "\", ";
    jsonString += "\"userID\" : " + $("#UserID").val() + ", ";
    jsonString += "\"receipt\" : \"false\", ";
    jsonString += "\"notify\" : \"false\", ";
    jsonString += "\"wpIDs\" : [" + $("#UserID").val() + "] ";
    jsonString += "}";
    return jsonString;
}

function validateEncumbrance() {
    // Check all required fields on the Encumbrance form for valid values
    var isErrorFree = true; // set to false when an error is found
    var status = $("#CurrentStatus").val();
    var msg = "";
    if (!$("#LineItemType").val() || $("#LineItemType").val() === "None") {
        msg += "Please select an Encumbrance Type. <br/>";
        isErrorFree = false;
    } else if ($("#LineItemType").val()==="Advertisement" && (status==="Draft" || status==="Finance")) {
        //Require Advertised Date and Letting Date for Advertisements
        if (!$("#AdvertisedDate").val()) {
            msg += "An Advertised Date is required for an Advertisement encumbrance request. <br/>";
            isErrorFree = false;
        }
        if (!$("#LettingDate").val()) {
            msg += "A Letting Date is required for an Advertisement encumbrance request. <br/>";
            isErrorFree = false;
        }
    } else if ($("#LineItemType").val() === "Renewal" && (status === "Draft" || status === "Finance")) {
        if (!$("#RenewalDate").val()) {
            msg += "A Renewal Ending Date is required for a Renewal encumbrance request. <br/>";
            isErrorFree = false;
        }
    } else if ($("#LineItemType").val() === "Amendment2LOA" && (status === "Draft" || status === "Finance")) {
        if (!$("#AmendedLOAFLAIRID").val()) {
            msg += "A FLAIR ID for the amended LOA is required for an Amendment to LOA encumbrance request. <br/>";
            isErrorFree = false;
        }
    }
    if (!$("#ContractID").val()) {
        msg += "Please select or create a Contract. <br/>";
        isErrorFree = false;
    }
    if ($("#AttachmentIsRequired").val() === "true") {
        // If file attachment is required, verify at least one file is attached 
        // Or a NoAttachmentComment is provided
        // Capture the NoAttachmentComment and append it to the comment in the Submission dialog
        if ($("#AttachmentCount").val() === undefined || parseInt($("#AttachmentCount").val()) === 0) {
            if ($("#NoAttachmentComment").val() === undefined || $("#NoAttachmentComment").val().length < 1) {
                msg += "A file attachment or explanation for a missing file attachment is required for an encumbrance request with a negative amount.";
                isErrorFree = false;
            }
        }
    }

    // use displayMessage() to show validation message.
    displaySubmitMessage(msg);
    return isErrorFree;
}


function saveEncumbrance(commentJson) {
    var encumbrance = {};
    var groupID = $("#LineItemGroupID").val();
    if (groupID === "") { groupID = 0; }
    encumbrance.GroupID = groupID;
    encumbrance.ContractID = $("#ContractID").val();
    encumbrance.Description = $("#Description").val();
    encumbrance.LineItemType = $("#LineItemType").val();
    encumbrance.LastEditedUserID = $("#UserID").val();
    encumbrance.FlairAmendmentID = $("#FlairAmendmentID").val();
    encumbrance.UserAssignedID = $("#UserAssignedID").val();
    encumbrance.LineID6S = $("#LineID6S").val();
    encumbrance.OriginatorUserID = $("#UserID").val();
    encumbrance.isEditable = 1;
    encumbrance.CurrentStatus = $("#CurrentStatus").val();
    if ($("#AmendedLineItemID").val()) {
        encumbrance.AmendedLineItemID = $("#AmendedLineItemID").val();
    }
    if ($("#AmendedFlairLOAID").val()) {
        encumbrance.AmendedFlairLOAID = $("#AmendedFlairLOAID").val();
    }
    if ($("#AdvertisedDate").val()) {
        encumbrance.AdvertisedDate = $("#AdvertisedDate").val();
    }
    if ($("#LettingDate").val()) {
        encumbrance.LettingDate = $("#LettingDate").val();
    }
    if ($("#RenewalDate").val()) {
        encumbrance.RenewalDate = $("#RenewalDate").val();
    }
    if (!encumbrance.CurrentStatus) { encumbrance.CurrentStatus = 'Draft'; }

    var encumbranceType = $("#LineItemType").val();
    if (encumbranceType === 'Advertisement' || encumbranceType === 'Award') {
        encumbrance.IncludesContract = 1;
    } else {
        encumbrance.IncludesContract = 0;
    }
    // Add commentJson string to this json string and submit it to the server for processing
    // Server side needs to send all requested notifications for all statuses
    // Submit the Contract to the database with ajax
    $.ajax({
        url: "/LineItemGroups/AddNewEncumbrance",
        type: "POST",
        dataType: "json",
        data: { lineItemGroup: JSON.stringify(encumbrance), comments: commentJson },
        success: function (data) {
            var result = JSON.parse(data);
            // If the encumbrance is successfully 
            if (result.redirect) {
                var redirectURL = result.redirect;
                window.location.href = redirectURL;
                return false;
            }
            // return the completed Contract object to the calling form and use it to populate ContractPanel div
            updateEncumbrance(result);
            // show LineItems panel
            displayLineItemsPanelOrMessage();
            if (result.CurrentStatus === "Draft") {
                displayMessage("Encumbrance successfully saved as draft.");
            } else if (result.CurrentStatus === "Finance") {
                displayMessage("Encumbrance successfully submitted for Finance Review.");
                $("#btnEncumbranceDraft").remove();
                $("#btnEncumbranceFinance").remove();
            } else if (result.CurrentStatus === "Work Program") {
                displayMessage("Encumbrance successfully submitted for Work Program Review.");
                $("#btnEncumbranceRollback").remove();
                $("#btnEncumbranceFinance").remove();
                $("#btnEncumbranceWP").remove();
            } else if (result.CurrentStatus === "CFM") {
                displayMessage("Encumbrance successfully submitted for CFM Input.");
                $("#btnEncumbranceRollback").remove();
                $("#btnEncumbranceCFM").remove();
                $("#btnEncumbranceWP").remove();
            } else if (result.CurrentStatus === "Complete") {
                displayMessage("Encumbrance successfully marked as input into CFM.");
                $("#btnEncumbranceCFM").remove();
            }
        }
    });
}

function updateEncumbrance(encumbrance) {
    // update the encumbrance form to reflect the saved version of the 
    $("#LineItemGroupID").val(encumbrance.GroupID);
    $("#ContractID").val(encumbrance.ContractID);
    $("#ContractSelector").val(encumbrance.Contract.ContractNumber);
    $("#Description").val(encumbrance.Description);
    $("#FlairAmendmentID").val(encumbrance.FlairAmendmentID);
    $("#UserAssignedID").val(encumbrance.UserAssignedID);
    $("#AmendedLineItemID").val(encumbrance.AmendedLineItemID);
    $("#GroupStatus").val(encumbrance.CurrentStatus);
}

/*** END OF -- SAVE LINEITEMGROUP METHODS -- ***/


function setEncumbranceTotal() {
    // Add amounts from each record and populate Encumbrance Total span
    var total = 0.00;
    $("[id $= '_Amount']").each(function () {
        var amt = $(this).val();
        amt = amt.replace("(", "-");
        amt = amt.replace(")", "");
        amt = amt.replace(",", "");
        amt = amt.replace("$", "");
        total += Number(amt);
    });
    var encumbranceTotal = formatCurrency(total);
    $("#EncumbranceTotal").html("<strong>Encumbrance Total: </strong>" + encumbranceTotal);
    $("#EncumbranceTotalAmount").html("<strong>Encumbrance Total: </strong>" + encumbranceTotal);
    $("#EncHeaderEncAmount").html("Amount: <h4>" + encumbranceTotal + "</h4>");

    updateRequireAttachment(total);

    var budgetCeiling = $("#BudgetCeiling").val();
    var compensation = $("#Compensation").val();
    var toggle = "hide";
    var compsWithCeilings = "3,4,5,7";
    if (compsWithCeilings.indexOf(compensation) > 0 && total > budgetCeiling) { toggle = "show"; }
    toggleBudgetCeilingWarning(toggle);
    showHideFYWarnings();
}

function setContractAmountTotal() {
    // get total value of all line items in all encumbrances for this contract
    // make ajax call to GetContractAmountTotal with contractID as parameter
    var contractID = $("#ContractID").val();
    if (!contractID) { contractID = 0;}
    var contractInfo = {};
    contractInfo.contractID = contractID;
    $.ajax({
        url: "/Contracts/GetContractAmountTotal",
        type: "POST",
        dataType: "json",
        data: { contractInfo: JSON.stringify(contractInfo) },
        success: function (data) {
            var result = JSON.parse(data);
            var totalAmount = formatCurrency(result);
            $("#ContractAmountTotal").html("<strong>Contract Total: </strong>" + totalAmount);
        }
    });
}

function updateRequireAttachment(encumbranceTotal) {
    // If encumbranceTotal is negative and request is in draft and current user has originator role
    // then require a file attachment or an explanation why there is no file attachment
    if (parseInt(encumbranceTotal) < 0 && $("#UserRoles").val().indexOf("Originator") >= 0
        && ($("#CurrentStatus").val() === "Draft" || $("#CurrentStatus").val() === "New")
        && ($("#NoAttachmentComment").val() === undefined || $("#NoAttachmentComment").val().length < 1)
        && ($("#AttachmentCount").val() === undefined || parseInt($("#AttachmentCount").val()) === 0))
    {
        var warning = "<p><font color='red'>A file attachment is required for an encumbrance request for a negative amount.<br /> ";
        warning += "Please attach a file or provide an explanation why no file is attached:</font></p> ";
        warning += "<input type='text' id='NoAttachmentComment' name='NoAttachmentComment' />";
        $("#AttachmentRequiredWarning").html(warning);
        $("#AttachmentIsRequired").val("true");
    } else {
        $("#AttachmentRequiredWarning").html("");
        $("#AttachmentIsRequired").val("false");
    }
}
function toggleBudgetCeilingWarning(action) {
    // TODO: if contract requires a budget ceiling (Contract Funding in {3, 4, 5, 7}) 
    // and Budget Ceiling > $0
    // and Encumbrance Total > Budget Ceiling, then show the warning
    if (action === "show") {
        $("#budgetCeilingWarningSpan").text("Encumbrance total exceeds Budget Ceiling.");
    } else {
        $("#budgetCeilingWarningSpan").text("");
    }
}

function showHideFYWarnings() {
    var endingFY = $("#EndingFY").val();
    $("[name^='FYWarning_']").each( function(){
        var thisID = $(this).id;
        if (thisID && thisID.length > 0) {
            var lineNum = parseInt(thisID.substring(thisID.indexOf("_") + 1));
            var thisFY = $("#FY_" + lineNum).val();
            // if thisFY > endingFY then show FYWarning_thisID else hide it.
            if (thisFY > endingFY) {
                $(this).show();
            } else {
                $(this).hide();
            }
        }
    });
}

function displayLineItemPanelMessage(msg) {
    $("#messageSpanPanelLineItem").html(msg);
}
function displayLineItemMessage(msg) {
    msg = "<font color='red'>" + msg + "</font>";
    $("#messageSpanLineItem").html(msg);
}
function displayMessage(msg) {
    $("#messageSpan").html(msg);
}
function displayContractMessage(msg) {
    msg = "<font color='red'>" + msg + "</font>";
    $("#messageSpanContract").html(msg);
}
function displaySubmitMessage(msg) {
    msg = "<font color='red'>" + msg + "</font>";
    $("#submitMessageDiv").html(msg);
}
function showComment(text, title) {
    // open small dialog and display text
    $("#CommentsDisplayDialog").css("z-index", "9999");
    $("#CommentsDisplayDialog").text(text);
    $("#CommentsDisplayDialog").dialog("open");
    //alert(text);
}

// NewLineItemPartial  calls this method when Amount changes
function showHideNegativeAmountOptions() {
    //var hasNeg = $("#FinancialInformationFormPanel #Amount").val().indexOf("-");
    var amount = formatDecimal($("#FinancialInformationFormPanel #Amount").val());

    if (!amount || amount >= 0) {
        // hide FlairAmendmentID and LineID6S in the LineItems form
        $("#LineItemID6SCell").css('visibility', 'hidden');
        $("#LineItemFlairIDCell").css('visibility', 'hidden');
    } else {
        // show FlairAmendmentID and LineID6S in the LineItems form
        $("#LineItemID6SCell").css('visibility', 'visible');
        $("#LineItemFlairIDCell").css('visibility', 'visible');
    }
}

function updateReceiveEmails() {
    $("#User_ReceiveEmails").val($("#ReceiveEmailsOption").val());
}


//rudimentary search tools
function validateFindGroupID() {
    if ($("#findGroupID").val().length > 0) {
        $("#findEncumbranceButton").prop("disabled", false);
        $("#findEncumbranceButton").removeClass("disabled");
    } else {
        $("#findEncumbranceButton").prop("disabled", true);
        $("#findEncumbranceButton").addClass("disabled");
    }
}
function validateFindContract() {
    if ($("#ContractID").val().length > 0) {
        $("#findContractButton").prop("disabled", false);
        $("#findContractButton").removeClass("disabled");
    } else {
        $("#findContractButton").prop("disabled", true);
        $("#findContractButton").addClass("disabled");
    }
}
function findEncumbrance() {
    var id = $("#findGroupID").val();
    window.open("/LineItemGroups/Manage?id=" + id, "_self");
}
function findContract() {
    var id = $("#ContractID").val();
    window.open( "/Contracts/Details?id=" + id, "_self");
}

function awardAdvertisement(id, con) {
    $("#awardDialog").dialog({
        autoOpen: false,
        width: 600,
        resizable: false,
        title: "Award Contract " + con,
        modal: true,
        open: function (event, ui) {
            $(this).html("");
            var awardMessage = "<p>Click <strong>Award</strong> to create the Award encumbrance request for Contract #" + con + "?</p>";
            $(this).append(awardMessage);
        },
        buttons: {
            "Cancel": function () {
                $(this).dialog("close");
            },
            "Award": function () {
                window.open("/LineitemGroups/Award?id=" + id, "_self");
                $(this).dialog("close");
            }
        }
    });
    $("#awardDialog").dialog("open");
}


function awardDuplicate(id) {
    $("#awardDialog").dialog({
        autoOpen: false,
        width: 600,
        resizable: false,
        title: "Duplicate Encumbrance Request #" + id,
        modal: true,
        open: function (event, ui) {
            $(this).html("");
            var awardMessage = "<p>Click <strong>Duplicate</strong> to duplicate encumbrance request #" + id + "</p>";
            $(this).append(awardMessage);
        },
        buttons: {
            "Cancel": function () {
                $(this).dialog("close");
            },
            "Duplicate": function () {
                window.open("/LineitemGroups/Amend?id=" + id, "_self");
                $(this).dialog("close");
            }
        }
    });
    $("#awardDialog").dialog("open");
}

function showHistoryDialog(id, status) {
    $.ajax({
        url: "/LineItemGroups/GetHistory/",
        type: "GET",
        datatype: "json",
        data: { groupID: JSON.stringify(id)  },
        success: function (data) {
            var statusTable = makeStatusTable(data);
            $("#historyDialog").dialog({
                autoOpen: false,
                width: 800,
                resizable: false,
                title: "History for Encumbrance Request #" + id,
                modal: true,
                open: function (event, ui) {
                    var userID = $("#CurrentUserID").val();
                    $(this).html("");
                    var historyBody = "<input type='hidden' name='groupID' id='groupID' value='" + id + "'\>";
                    historyBody += "<input type='hidden' name='userID' id='userID' value='" + userID + "'\>";
                    historyBody += "<input type='hidden' name='currentStatus' id='currentStatus' value='" + status + "'\>";
                    historyBody += statusTable;
                    historyBody += "<p><input type='text' name='HistoryComment' id='HistoryComment' placeholder='Add a Comment' onKeyup='enableHistoryComment()'/>";
                    historyBody += "<input type='button' name='SaveCommentButton' id='SaveCommentButton' onClick='submitHistoryComment()' value='Save Comment' disabled/>";
                    historyBody += "<input type='button' name='CloseHistoryButton' id='CloseHistoryButton' onClick='closeHistoryDialog()' value='Close' /></p>";
                    $(this).html(historyBody);
                },
                buttons: {}
            });
            $("#historyDialog").dialog("open");
        }
    });
}

function enableHistoryComment() {
    if (nvl($("#HistoryComment").val(), '') === '') {
        $("#SaveCommentButton").attr("disabled", true);
    } else {
        $("#SaveCommentButton").attr("disabled", false);
    }
}

function submitHistoryComment() {
    // new LineItemGroupStatus object has required properties: {LineItemGroupID, UserID, CurrentStatus, SubmittalDate}
    // and optional properties: { Comments, ItemReduced, AmountReduced }
    // For this submittal, we require a Comment and ignore ItemReduced and AmountReduced. 
    // SubmittalDate is added on the server when it is saved.

    LineItemGroupStatus = {};
    LineItemGroupStatus.LineItemGroupID = $("#groupID").val();
    LineItemGroupStatus.CurrentStatus = $("#currentStatus").val();
    LineItemGroupStatus.Comments = $("#HistoryComment").val();
    LineItemGroupStatus.UserID = $("#CurrentUserID").val();
    LineItemGroupStatus.SubmittalDate = '2001-01-01';
    $.ajax({
        url: "/LineItemGroupStatuses/Add",
        type: "POST",
        datatype: "json",
        data: { lineItemGroupStatus: JSON.stringify(LineItemGroupStatus) },
        success: function (data) {
            addStatusTableRow(data);
        }
    });
}


function closeHistoryDialog() {
    $("#historyDialog").dialog("close");
}

function nvl(value, replacement) {
    if (value === null || value === undefined || value === '') {
        return replacement;
    }
    return value;
}

function makeStatusTable(data) {
    // data is an array of LineItemGroupStatus objects
    // {amountReduced, comments, currentStatus, itemReduced, lineItemGroupID, statusID, submittalDate, user { userID, firstName, lastName, fullName, userLogin}}
    // generate a table that shows each status line: Date, Commenter, Status, Comment, Item Reduced, Amount Reduced
    if (data.length < 1) return "No History records for this encumbrance request.";
    var statusRecord;
    var tableString = "<table class='table table-striped table-bordered'name='HistoryTable' id='HistoryTable'><thead>";
    tableString += "<tr><th>Date</th><th>Commenter</th><th>Status</th><th>Comment</th><th>Item Reduced</th><th>Amount Reduced</th></tr></thead><tbody>";
    for (var i = 0; i < data.length; i++) {
        statusRecord = data[i];
        var statusDate = formatDateTime(statusRecord.submittalDate);

        tableString += "<tr><td>" + statusDate + "</td>";
        tableString += "<td>" + statusRecord.user.fullName + "</td>";
        tableString += "<td>" + statusRecord.currentStatus + "</td>";
        tableString += "<td>" + nvl(statusRecord.comments, '') + "</td>";
        tableString += "<td>" + nvl(statusRecord.itemReduced, '') + "</td>";
        tableString += "<td>" + nvl(statusRecord.amountReduced, '') + "</td></tr> ";
    }
    tableString += "</tbody></table><br />";
    return tableString;
}

function addStatusTableRow(statusRecord) {
    // This method receives a LineItemGroupStatus object in a JSON string
    // it parses the string and adds a row showing the data to the HistoryTable

    var statusDate = formatDateTime(statusRecord.submittalDate);
    var tableString = "";
    tableString += "<tr><td>" + statusDate + "</td>";
    tableString += "<td>" + statusRecord.user.fullName + "</td>";
    tableString += "<td>" + statusRecord.currentStatus + "</td>";
    tableString += "<td>" + nvl(statusRecord.comments, '') + "</td>";
    tableString += "<td>" + nvl(statusRecord.itemReduced, '') + "</td>";
    tableString += "<td>" + nvl(statusRecord.amountReduced, '') + "</td></tr> ";

    $("#HistoryTable").append(tableString);
}
// called from /EncumbranceLookups/Search
function updateSearchAmountCheckboxes() {
    if ($("#ckLineItemAmount").length) {
        $("#IsLineItemAmount").val($("#ckLineItemAmount").is(":checked"));
    }
    if ($("#ckEncumbranceAmount").length) {
        $("#IsEncumbranceAmount").val($("#ckEncumbranceAmount").is(":checked"));
    }
    if ($("#ckContractAmount").length) {
        $("#IsContractAmount").val($("#ckContractAmount").is(":checked"));
    }
}

/***** File Attachment methods *****/
function uploadFile() {
    var uploadfile = $("#FileToUpload").get(0);
    var files = uploadfile.files;
    var formData = new FormData();
    formData.append("FileToUpload", files[0]);
    formData.append("fileGroupID", $("#LineItemGroupID").val());

    $.ajax({
        type: "POST",
        url: '/FileAttachments/UploadFile',
        data: formData,
        processData: false,
        contentType: false,
        dataType: "json",
        success: function (data, textStatus, jqXHR) {
            var results = JSON.parse(data);
            // add new row to FileAttachmentsTable
            var fileID = results.fileID;
            var fileURL = results.fileURL;
            var fileName = results.fileName;
            // if file attachments table is not present, add it
            if ($("#AttachedFilesTable").length === undefined || $("#AttachedFilesTable").length === 0) {
                var table = "<table width='25%' name='AttachedFilesTable' id='AttachedFilesTable'><tbody></tbody></table><br />";
                $("#FlieAttachmentColumn").append(table);
            }
            var newRow = "<tr name='file_" + fileID + "' id='file_" + fileID + "'>";
            newRow += "<td width='50px'><a href='" + fileURL + "' target='_blank'>" + fileName + "</a>";
            newRow += "</td ><td width='25px'><a href='javascript:deleteAttachment(" + fileID + ")'>Delete</a></td></tr>";
            $("#AttachedFilesTable").append(newRow);
            $("#fileMessage").text("");
            if($("#AttachmentCount").val() === undefined)
            {
                var attachmentCount = "<input type='hidden' name='AttachmentCount' id='AttachmentCount' value='0'/>";
                $("#FlieAttachmentColumn").append(attachmentCount);
            }
            $("#AttachmentCount").val(parseInt($("#AttachmentCount").val()) + 1);
        },
        error: function (data, textStatus, jqXHR) {
            //process error msg
        }
    });
}

function deleteAttachment(id) {
    $.ajax({
            autoFocus: true,
            url: "/FileAttachments/Delete",
            type: "POST",
            dataType: "json",
            data: { id: id },
        success: function (data) {
            //remove row from table of files
            var results = JSON.parse(data);
            var fileName = results.fileName;
            $("#file_" + id).remove();
            $("#fileMessage").html("File <em>" + fileName + "</em> deleted.");
            $("#AttachmentCount").val(parseInt($("#AttachmentCount").val()) - 1);
        },
        error: function () {
            $("#fileMessage").text("Unable to delete file.");
        }
    });
}
/***** End File Attachment methods *****/