// Write your JavaScript code.
$(document).ready(function () {
    initForms();
    addDialogs();
    showHideButtons();
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

    //initialize tooltips
    //$(function () {
    //    $('[data-toggle="tooltip"]').tooltip();
    //});

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

    // preface OrgCode with "55-"
    $("#OrgCode").keyup(function () {
        var orgCode = $("#OrgCode").prop("value");
        while (orgCode.charAt(0) === "5" || orgCode.charAt(0) === "-") {
            orgCode = orgCode.substring(1, orgCode.length);
        }
        $("#OrgCode").val("55-" + orgCode);
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
        updateContractContractType()
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

function initLineItemControls() {
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

function addDialogs() {
    // update Contract Status dialog link
    $('#contractStatusDialog').dialog({
        autoOpen: false,
        height: 400,
        width: 800
    });
    $('#updateContractStatusLink').click(function () {
        $('#contractStatusDialog').dialog("open");
    });

    // update LineItem Comment dialog link
    $('#lineItemCommentDialog').dialog({
        autoOpen: false,
        height: 400,
        width: 800
    });
    $('#updateLineItemCommentLink').click(function () {
        $('#lineItemCommentDialog').dialog("open");
    });

    // add ShowCommentDialog
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

    // add FileAttachment dialog link
    $('#fileAttachmentDialog').dialog({
        autoOpen: false,
        height: 400,
        width: 800
    });
    $('#addFileAttachmentLink').click(function () {
        $('#fileAttachmentDialog').dialog("open");
    });

    // add Vendor dialog link
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

    // edit Vendor dialog link
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
                if (ValidateContract()) {
                    SaveContractModal();
                    $(this).dialog("close");
                }
            }
        }
    });
    // ContractSelector is showing when modal is open, so I explicitly show and hide it when ContractDialog is opened/closed
    $("#ContractDialog").on('dialogclose', function (event) {
        $("#ContractSelector").show();
    });

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
                if (ValidateLineItem()) {
                    SaveLineItemModal();
                    $(this).dialog("close");
                    $("#ContractSelector").show();
                }
            }
        },
    }).load("/LineItemGroups/NewLineItemPartial", function () {
        // very important to initForms() to enable date pickers and autocompletes
        initLineItemControls();
    });
}

function showHideButtons() {
    //if user has Admin role show Users link on hamburger menu
    if ($("#UserRoles").val().indexOf("Admin") >= 0 && $("#UsersMenu").length === 0) {
        var usersMenu = "<li id='UsersMenu' name='UsersMenu'><a href='\\Users\\Index'>Users</a></li >";
        $("#HamburgerMenu").append(usersMenu);      
    }

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
    if (!($("#LineItemsPanel").is(":visible"))) {
        return false;
    }
    $("#btnInputFinancialInformation").hide();

    // depending on CurrentStatus and Roles, enable appropriate buttons
    var currentStatus = $("#CurrentStatus").val();
    var encumbranceType = $("#LineItemType").val()
    var roles = $("#UserRoles").val();

    if ((currentStatus === "New" || currentStatus === "Draft") && roles.indexOf("Originator") >= 0) {
        $("#btnEncumbranceDraft").val("Save as Draft");
        $("#btnEncumbranceDraft").show();
        //if Encumbrance Type is Close50 or Close98 then submit directly to CFA Ready
        if (encumbranceType.indexOf("Close")>=0) {
            $("#btnEncumbranceCFM").val("Submit for CFM Input");
            $("#btnEncumbranceCFM").show();
        } else {
            $("#btnEncumbranceFinance").val("Submit to Finance");
            $("#btnEncumbranceFinance").show();
        }
        return false;
    }
    if ((currentStatus === "Finance") && roles.indexOf("Finance Reviewer") >= 0) {
        $("#btnEncumbranceWP").val("Approve to Work Program");
        $("#btnEncumbranceWP").show();
        $("#btnEncumbranceSaveAsIs").show();
        $("#btnEncumbranceRollback").val("Reject back to Originator");
        $("#btnEncumbranceRollback").show();
        $("#btnEncumbranceComplete").val("Update to CFM Complete");
        $("#btnEncumbranceComplete").show();
        return false;
    }
    if ((currentStatus === "Work Program") && roles.indexOf("WP Reviewer") >= 0) {
        $("#btnEncumbranceFinance").val("Reject back to Finance");
        $("#btnEncumbranceFinance").show();
        $("#btnEncumbranceSaveAsIs").show();
        $("#btnEncumbranceCFM").val("Approve to CFM");
        $("#btnEncumbranceCFM").show();
        return false;
    }
    if ((currentStatus === "CFM") && roles.indexOf("CFM Submitter") >= 0) {
        $("#btnEncumbranceRollback").val("Reject back to Originator");
        $("#btnEncumbranceRollback").show();
        $("#btnEncumbranceWP").val("Return to Work Program");
        $("#btnEncumbranceWP").show();
        $("#btnEncumbranceSaveAsIs").show();
        $("#btnEncumbranceComplete").val("Update to CFM Complete");
        $("#btnEncumbranceComplete").show();
        return false;
    }
    if ((currentStatus === "Complete") && roles.indexOf("Finance Reviewer") >= 0) {
        $("#btnEncumbranceRollback").show();
        $("#btnEncumbranceSaveAsIs").show();
        return false;
    }
    $("#noButtonDiv").show();

    // show LineItemsPanel if the contract is selected

    if (($("#ContractID").val() && $("#ContractID").val() > 0) && ($("#LineItemGroupID").val() && $("#LineItemGroupID").val() > 0)) {
        $("#LineItemsPanel").show();
    }
    // ContractSelector is showing when modal is open, so I explicitly show and hide it when LineItemDialog is opened/closed
    $("#LineItemDialog").on('dialogclose', function (event) {
        $("#ContractSelector").show();
    });
}

function updateEncumbranceType() {
    var encumbranceType = $("#LineItemType").val();
    var contractID = $("#ContractID").val();

    displayLineItemsPanelOrMessage();
    setDefaultUserAssignedID();
    if (encumbranceType === "New Contract" && (contractID === "" || contractID === 0)) {
        openContractDialog();
    }
}

function OpenCloseContractDialog(contractID, contractNumber, contractStatus) {
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
            contents += "<td><input type='radio' name='closureType' id='close50' class='radio inline' style='vertical-align: middle; margin: 0px;' /></td><td><label class='radio-inline'> Close Status 50 </label></td>";
            contents += "</tr><tr>";
            contents += "<td><input type='radio' name='closureType' id='close98' class='radio inline' style='vertical-align: middle; margin: 0px;' /></td><td><label class='radio-inline'> Close Status 98 </label></td>";
            contents += "</tr></table><br/>";
            //contents += "<p>To remove line items from this Encumbrance Request, use the <strong>Delete</strong> link for that line in the <strong>Financial Information</strong> section of the form.</p>";
            contents += "I certify that the amounts being released are not required for current and future obligations.";
            contents += "<table><tr><th>&nbsp;</th><th>&nbsp;</th></tr><tr>";
            contents += "<td><input type='radio' name='amountsYesNo' id='amountsYes' /></td><td> Yes</td>";
            contents += "</tr><tr>";
            contents += "<td><input type='radio' name='amountsYesNo' id='amountsNo'  /></td><td> No</td>";
            contents += "</tr><tr>";
            contents += "<td><input type='radio' name='amountsYesNo' id='amountsNA'  /></td><td> N/A</td>";
            contents += "</tr></table><br/>";
            contents += "Comments: <br/>";
            contents += "<input type='textarea' name='ClosureComments' id='ClosureComments' />";
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
                closeContract(closeJson);
                $("#ContractSelector").show();
                $(this).dialog("close");
            }
        },
    });
    $("#CloseContractDialog").dialog("open");
}

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

    closeJson += '"Amounts":"' + amountsYesNo + '",';
    closeJson += '"LineItemGroupID":"' + $("#LineItemGroupID").val() + '",';
    closeJson += '"Comments":"' + $("#ClosureComments").val() + '",';
    closeJson += "\"ClosureType\":\"" + $("#ClosureType").val() + "\",";
    closeJson += "\"ContractOrEncumbrance\":\"" + "Contract" + "\",";
    //closeJson += "\"LineItemGroupID\":\"" + $("#LineItemGroupID").val() + "\",";
    closeJson += "}";
    // return the json string
    return closeJson;
}

function closeContract(jsonString) {
    $.ajax({
        url: "/LineItemGroups/CloseContract",
        type: "POST",
        dataType: "json",
        data: { closeContract: jsonString },
        success: function (data) {
            var results = JSON.parse(data);
            
        }
    });
}

function displayLineItemsPanelOrMessage() {
    // $("#AddedInfoDiv").html();
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
        $("#EncHeaderEncID").html("Encumbrance: <h4>" + groupID + "</h4>");
    }
    var contractID = $("#ContractID").val();

    setEncumbranceTotal();
    //$("#EncHeaderEncAmount").html("Amount: <h4>" + getEncumbranceAmount() + "</h4>");

    if (contractID > 0 && encumbranceType
        && encumbranceType.length > 0 && encumbranceType !== "None"
        && groupID > 0) {
        $("#LineItemsPanel").show();
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
    }
}
function collapseSection() {
    $(this).nextUntil("tr.groupHeader").toggle();
}

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
    for (var i = currentYear - 3; i < currentYear + 10; i++) {
        if (selectedValue && (i + 1) === selectedValue) {
            list += '\n<option selected value="' + (i + 1) + '" >' + i + ' - ' + (i+1) + '</option>';
        } else {
            list += '\n<option value="' + (i+1) + '" >' + i + ' - ' + (i+1) + '</option>';
        }
    }
    $('#FiscalYear').val(selectedValue);
    fiscalYear.append(list);
    //TODO: Fix this code to use @model. currentRequest is not valid for codebase
    /*
    if (currentRequest.RequestId === -1 || currentRequest.RequestId === 1) {
        fiscalYear.val(currentYear);
    }
    else {
        var date = new Date(@Model.fiscalYear);
        var year = date.getFullYear();
        fiscalYear.val(year);
    }
    */
}
function openContractStatusDialog() {
    $("#contractStatusDialog").dialog("open");
}
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

function openLineItemCommentDialog() {
    $("#lineItemCommentDialog").dialog("open");
}
function addLineItemComment() {
    // make sure all values are populated
    // submit to /ContractStatus/Create controller
    $("LineItemCommentForm").submit();
    // close dialog and return focus to /Contracts/Edit
}

function opendeleteEncumbranceDialog(id) {
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

function openEncumbranceSubmissionDialog(submitTo, wpUsers) {
    var titleText = (submitTo === "Draft") ? "Save Encumbrance" : "Submit Encumbrance to " + submitTo;
    var buttonText = (submitTo === "Draft") ? "Save" : "Submit";
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
            } else if (submitTo === "Finance") {
                defaultComment = "Submitted to Finance.";
            } else if (submitTo === "Work Program") {
                defaultComment = "Please review and approve for Work Program.";
            } else if (submitTo === "CFM") {
                defaultComment = "";
            } else if (submitTo === "Complete") {
                notifyChecked = "checked";
                defaultComment = "Input to CFM.";
            }
            if (submitTo === currentStatus) {
                defaultComment = "";
                notifyChecked = "";
            }

            // for Work Program, show ItemReduced and AmountReduced
            if (currentStatus === "Work Program") {
                var itemReduced = "<strong>Item Reduced:</strong><br/><input type='text' name='itemReduced' id='itemReduced' /><br/>";
                var amountReduced = "<strong>Amount Reduced:</strong><br/>$<input type='text' name='amountReduced' id='amountReduced' /><br/>";
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
                    SaveEncumbrance(commentJson);
                }
            }
        },
    });
    $("#SubmissionDialog").dialog("open");
    if ($("#itemReduced").length > 0) { $("#itemReduced").focus(); } else { $("#commentText").focus(); }
}

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
        
        var cleanAmount = parseFloat($("#amountReduced").val().replace(/,/g, "").replace("$", "").replace("(", "-").replace(")", ""));
        if (isNaN(cleanAmount))
        {
            jsonString += "\"amountReduced\" : \"\", ";
        } else {
            jsonString += "\"amountReduced\" : \"" + cleanAmount + "\", ";
        }
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

function openFileAttachmentDialog() {
    $("#fileAttachmentDialog").dialog("open");
}
function addFileAttachment() {
    // make sure all values are populated
    // submit to /ContractStatus/Create controller
    $("FileAttachmentForm").submit();
    // close dialog and return focus to /Contracts/Edit
}

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

function openAttachment(filename) {
    // TODO: retrieve the file
    // let the browser open or download the attachment
}
function submitEncumbrance(groupID, newStatus) {
    var userID = $("#Contract_UserID").val();
    var userName = $("#UserName_" + groupID).val();
    var contractID = $("#ContractID_" + groupID).val();
    var contractNumber = $("#ContractNumber_" + groupID).val();
    var encumbranceType = $("#LineItemType_" + groupID).val();
    var description = $("#Description_" + groupID).val();
    var flairAmend = $("#FlairAmendmentID_" + groupID).val();
    var userAmend = $("#UserAssignedID_" + groupID).val();
    var lineAmend = $("#AmendedLineItemID_" + groupID).val();
    var line6s = $("#LineID6S_" + groupID).val();

    var Contract = {};
    Contract.ContractID = contractID;
    var wpIDs = [];
    var WPReviewers = $('[id ^="wp_"][id $="_' + groupID + '"]:checked').each(function () {
        wpIDs.push($(this).val());
    });

    EncumbranceRequestViewModel = {};

    var LineItemGroups = [];
    LineItemGroup = {};
    LineItemGroup.GroupID = groupID;
    LineItemGroup.ContractID = contractID;
    LineItemGroup.LastEditedUserID = userID;
    LineItemGroup.OriginatorUserID = userID;
    LineItemGroup.LineItemType = encumbranceType;
    LineItemGroup.FlairAmendmentID = flairAmend;
    LineItemGroup.UserAssignedID = userAmend;
    LineItemGroup.AmendedLineItemID = lineAmend;
    LineItemGroups.push(LineItemGroup);

    var Statuses = [];
    LineItemGroupStatus = {};
    LineItemGroupStatus.GroupID = groupID;
    LineItemGroupStatus.CurrentStatus = newStatus;
    LineItemGroupStatus.Description = description;
    Statuses.push(LineItemGroupStatus);

    EncumbranceRequestViewModel.Contract = Contract;
    EncumbranceRequestViewModel.LineItemGroups = LineItemGroups;
    EncumbranceRequestViewModel.Statuses = Statuses;
    EncumbranceRequestViewModel.WpRecipients = wpIDs;

    jQuery.ajaxSettings.traditional = true;
    $.ajax({
        type: "POST",
        ContentType: "application/json; charset=utf-8",
        dataType: 'html',
        data: { encumbrance : JSON.stringify(EncumbranceRequestViewModel) },
        url: '/LineItemGroups/Update/',
        success: function (response) {
            // replace writeable elements in LineItemGroup record with text values
            response = response.replace(/\"/g, "");
            response = response.replace(/'/g, '"');
            var responsetext = JSON.parse(response);
            var newRow =
                "<td id='EncumbranceToggle_" + groupID + "'>" +
                "<a href = &quot;javascript: toggleEncumbrance('" + groupID + "')&quot; id = 'toggleLink_" + groupID + "' > Hide encumbrance items</a>" +
                "</td >" +
                "<td> <strong>Contract: </strong>" + contractNumber + "<br/>" +
                "<strong>Encumbrance (6s): </strong>" + line6s + "</td>" +
                "<td> <strong>Encumbrance Type: </strong>" + responsetext.LineItemType + "</td>" +
                "<td> <strong>Status: </strong>" + newStatus + "</td>" +
                "<td colspan = '2'><strong>FLAIR ID: </strong>" + responsetext.FlairAmendmentID + "<br/>" +
                "<strong>User Assigned ID: </strong>" + responsetext.UserAssignedID + "<br/>" +
                "<strong>Corrects FLAIR ID: </strong>" + responsetext.AmendedLineItemID + " </td>" +
                "<td colspan = '2'> <strong>Last Updated: </strong> " + getFormattedDateNow() + "<br/> by " + userName + "</td > ";
            if (comment && comment.length > 0) {
                newRow += "<td colspan = '3'> <strong>Description:</strong>" + description + " </td > ";
            }else {
                newRow += "<td colspan = '3'>  </td > ";
            }
            newRow += "<td colspan = '2'> Successfully updated! </td>";
            var headerRow = $("#groupHeader_" + groupID).html(newRow);
            // show confirmation that LineItemGroup was submitted (replace button with acknowledgment)
        }
    });
}

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
function reloadContractList() {
    if ($("#includeAll").is(":checked")) {
        window.location.replace("/Contracts/ListAll")
    } else {
        window.location.replace("/Contracts/List")
    }
}

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

function showContractHistory(contractID) {
    // fetch all ContractStatus records for contractID and display them in a table in the contractHistoryDiv
    // change the Show History link text to Hide History
    var ContractObject = {};
    ContractObject.ID = contractID;
    jQuery.ajaxSettings.traditional = true;
    $.ajax({
        type: "POST",
        ContentType: "application/json; charset=utf-8",
        dataType: 'html',
        data: { contractInfo: JSON.stringify(ContractObject) },
        url: '/Contracts/GetHistory',
        success: function (data) {
            $("#showHistoryLink").html("<a href='javascript: hideContractHistory(" + contractID + ")' id='showHistoryLink'>Hide Contract History</a>");
            var statusTable = "<table class='table'><thead><tr><th>User</th><th>Status</th><th>Date</th><th>Comment</th></tr></thead><tbody>";
            var parsedData = JSON.parse(data);
            $.each(parsedData, function (index) {
                var status = parsedData[index];
                var thisDate = new Date(status.submittalDate);
                var formattedDateTime = "" + thisDate.getMonth() + "/" + thisDate.getDate() + "/" + thisDate.getFullYear() + " " + thisDate.getHours() + ":" + thisDate.getMinutes() + ":" + thisDate.getSeconds();
                //var formattedDateTime = thisDate.toDateString() + " " +  thisDate.toTimeString();

                statusTable = statusTable + "<tr><td>" + status.user.firstName + " " + status.user.lastName + "</td> <td>" + status.currentStatus + "</td> <td>" + formattedDateTime + "</td> <td>" + status.comments + "</td></tr> ";
            });
            statusTable = statusTable + "</tbody></table>";
            $("#contractHistoryDiv").html(statusTable);
        }
    });
}
function hideContractHistory(contractID) {
    // remove table from contractHistoryDiv
    // change the Hide History link text to Show History

    $("#contractHistoryDiv").html("");
    $("#contractHistoryHeaderDiv").html("<a href='javascript: showContractHistory(" + contractID + ")' id='showHistoryLink'>Show History</a>");
}

function updateBudgetCeiling() {
    // if Compensation is 3, 4, or 5, then Budget Ceiling cannot be $0
    var comp = $("#CompensationID").val() || $("#Contract_CompensationID").val();
    if (comp === null) {
        comp = $("#Contract_CompensationID").val();
    }
    if ((comp === "3" || comp === "4" || comp === "5") && (!$("#BudgetCeiling").val() || $("#BudgetCeiling").val() < 1 )){
        $("#budgetCeilingMessage").text("A Budget Ceiling greater than $0 is required.");
    } else {
        $("#budgetCeilingMessage").text("");
    }
}
function updateOCAMessage() {
    // if State Program is 5, OCA must be a Right of Way value
    var sp = $("#StateProgramID").val();
    if (sp === 5) {
        $("#OCAMessage").text("Please select a Right of Way value for OCA.");
    } else {
        $("#OCAMessage").text("");
    }
}


function toggleCommentHistory(groupID) {
    if ($("#commentHistoryToggle").text() === "Show Encumbrance History") {
        $(".groupStatus").removeClass("hidden");
        $("#commentHistoryToggle").text("Hide Encumbrance History");
    } else {
        $(".groupStatus").addClass("hidden");
        $("#commentHistoryToggle").text("Show Encumbrance History");
    }
}

function toggleEncumbranceHistory(groupID) {
    if ($("#encumbranceHistoryToggle_" + groupID).text() === "Show Encumbrance History") {
        $(".groupStatus_" + groupID).removeClass("hidden");
        $("#encumbranceHistoryToggle_" + groupID).text("Hide Encumbrance History");
    } else {
        $(".groupStatus_" + groupID).addClass("hidden");
        $("#encumbranceHistoryToggle_" + groupID).text("Show Encumbrance History");
    }
}

function toggleLineHistory(lineID) {
    if ($("#lineHistoryToggle_" + lineID).text() === "Show Line Comments") {
        $(".lineStatus_" + lineID).removeClass("hidden");
        $("#lineHistoryToggle_" + lineID).text("Hide Line Comments");
    } else {
        $(".lineStatus_" + lineID).addClass("hidden");
        $("#lineHistoryToggle_" + lineID).text("Show Line Comments");
    }
}

function validateContractDollars() {
    if ($("#ContractTotal").val() === null || $("#ContractTotal").val().length < 1) { $("#ContractTotal").val("0"); }
    if ($("#MaxLoaAmount").val() === null || $("#MaxLoaAmount").val().length < 1) { $("#MaxLoaAmount").val("0"); }
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
    }
    displayLineItemsPanelOrMessage();
    return false;
}

function getEncumbranceAmount(encumbranceID) {
    if (encumbranceID > 0)
    {
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
        return formatCurrency(amount)
    } else {
        return formatCurrency(0.00);
    }
}

function setDefaultUserAssignedID(){
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
    if (prefix.length > 0 && $("#UserAssignedID").val().length === 0) {
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

function getLineOrder() {
    // return number of LineItemTable rows + 1
    var maxRow = 1
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
    if ($("#LineNumber") && !($("#LineNumber").val())) {
        $("#LineNumber").val(lineOrder);
    }

    $("#ContractSelector").hide();

    $("#CategorySelector").autocomplete("option", "appendTo", "#LineItemDialog");
    $("#OCASelector").autocomplete("option", "appendTo", "#LineItemDialog");
    $("#FundSelector").autocomplete("option", "appendTo", "#LineItemDialog");
    //callback();
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
    $("#FinancialInformationFormPanel #FlairAmendmentID").val(lineItem.FlairAmendmentID)
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


function SaveContractModal() {
    // javascript model of the Contract object  populated it from the dialog
    var Contract = {};
    Contract.BeginningDate = $("#BeginningDate").val();
    Contract.BudgetCeiling = $("#BudgetCeiling").val();
    Contract.CompensationID = $("#CompensationID").val();
    Contract.ContractID = $("#ContractID").val();
    if (Contract.ContractID === null || Contract.ContractID === "") { Contract.ContractID = 0; }
    Contract.ContractNumber = $("#ContractNumber").val();
    Contract.ContractTotal = $("#ContractTotal").val();
    Contract.ContractTypeID = $("#ContractTypeID").val(); // blank
    Contract.CurrentStatus = $("#CurrentStatus").val();
    Contract.DescriptionOfWork = $("#DescriptionOfWork").val();
    Contract.EndingDate = $("#EndingDate").val();
    Contract.IsRenewable = 0;
    if ($("#IsRenewable1").is(":checked")){ Contract.IsRenewable = 1; }; 
    Contract.MaxLoaAmount = $("#MaxLoaAmount").val();
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
            }
        }
    });
}

function SaveLineItemModal() {
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
            SaveEncumbrance(getDefaultSaveComment());
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
        lineItem.FlairAmendmentID = $("#FinancialInformationFormPanel #FlairAmendmentID").val()
    } else {
        lineItem.FlairAmendmentID = "";
    }
    if ($("#FinancialInformationFormPanel #LineID6S") && $("#FinancialInformationFormPanel #LineID6S").val()) {
        lineItem.LineID6S = $("#FinancialInformationFormPanel #LineID6S").val()
    } else {
        lineItem.LineID6S = "";
    }
    if ($("#FinancialInformationFormPanel #UserAssignedID") && $("#FinancialInformationFormPanel #UserAssignedID").val()) {
        lineItem.UserAssignedID = $("#FinancialInformationFormPanel #UserAssignedID").val()
    } else {
        lineItem.UserAssignedID = "";
    }
    if ($("#FinancialInformationFormPanel #AmendedLineItemID") && $("#FinancialInformationFormPanel #AmendedLineItemID").val()) {
        lineItem.AmendedLineItemID = $("#FinancialInformationFormPanel #AmendedLineItemID").val()
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
    lineItem.Amount = $("#Amount").val();
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
                displayLineItemPanelMessage("Line Item number " + result.LineNumber + " updated.")
            } else {
                $("#LineItemsTableBody:last-child").append(newRow);
                displayLineItemPanelMessage("Line Item number " + result.LineNumber + " added.")
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
 
    if ((lineItem.OrgCode).indexOf("55-") < 0) { lineItem.OrgCode = "55-" + lineItem.OrgCode; }
    tableText += "<tr class='groupItem' id='" + rowID + "'>";
    tableText += "<td id='" + itemKey + "_LineItemNumber'>" + lineItem.LineItemNumber + "<input type='hidden' id='" + itemKey + "_LineItemID' value='" + lineItem.LineItemID + "'/> <br />";
    if ($("#UserRoles").val().indexOf("Originator") >= 0 || ($("#UserRoles").val().indexOf("Finance") >= 0 && $("#CurrentStatus").val().indexOf("Finance") >= 0) || ($("#UserRoles").val().indexOf("CFM") >= 0 && $("#CurrentStatus").val().indexOf("Complete") >= 0)) {
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
        tableText += "<br/> <span title=\"" + lineItem.Comments + "\">Comments</span>"
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
    var cleanAmount = parseFloat(lineItem.Amount.replace(/,/g, "").replace("$", "").replace("(", "-").replace(")", ""));
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


function formatDate(dateString) {
    return dateString.mm + "/" + dateString.dd + "/" + dateString.yyyy
}

function formatDateTime(datetimeString) {
    return datetimeString.mm + "/" + datetimeString.dd + "/" + datetimeString.yyyy + " " + datetimeString.getHours() + ":" + datetimeString.getMinutes()
}

function formatDecimal(amount) {
    //const formatter = new Intl.NumberFormat('en-US', {
    //    style: 'decimal',
    //    minimumFractionDigits: 2,
    //});
    //return formatter.format(amount);
    if (amount === null) { return 0.00; }
    if (typeof (amount) === "string") {
        nAmount = parseFloat(amount)
    } else {
        nAmount = amount
    }
    return nAmount.toFixed(2);
}

function formatCurrency(amount) {
    const formatter = new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        minimumFractionDigits: 2
    });
    return formatter.format(amount);
}
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
    var description = (contract.DescriptionOfWork) ? contract.DescriptionOfWork : "";
    contractHtml += "<div class='col-sm-4'><dl><dt> Description of Work:</dt><dd> " + description + "</dd></dl></div>";
    contractHtml += "</div>";
    
    setEncumbranceTotal();

    $("#ContractPanelBody").html(contractHtml);
    $("#ContractPanel").show();
}
function getEndingFY(formattedDate) {
    var m = formattedDate.indexOf("/")
    var n = formattedDate.lastIndexOf("/");
    var year = formattedDate.substring(n + 1);
    var month = formattedDate.substring(m + 1, n)
    var FY = year;
    if (month > 7) { FY++; }
    return FY;
}

function SaveInitialEncumbrance() {
    // Call UpdateGroupStatus to save the encumbrance
    // Save silently with no comment dialog
    // Populate the LineItemGroupID and enable Financial Information
    UpdateGroupStatus("Draft", "true");

    // Now that this is saved, hide the Contract Information button
    if ($("#ContractPanel").is(":visible")) {
        $("#OpenContractInformationDiv").hide();
        $("#OpenContractInformationSpan").hide();
    }
    // Open the Financial Information Form
    if (ValidateEncumbrance()) {
        openLineItemDialog();
    }
}

function UpdateGroupStatus(status, silent) {
    if (ValidateEncumbrance()) {
        if (silent === "true") {
            var commentJson = getDefaultSaveComment();
            SaveEncumbrance(commentJson);
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

function ValidateEncumbrance() {
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

    // use displayMessage() to show validation message.
    displaySubmitMessage(msg);
    return isErrorFree;
}

function ValidateContract() {
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

function ValidateLineItem() {
    // Check all required fields on the LineItem form for valid values
    var isErrorFree = true; // set to false when an error is found
    var msg = "";

    var orgCode = $("#OrgCode").val();
    if (!orgCode) {
        msg += "Please enter a valid Organization Code. <br/>";
        isErrorFree = false;
    } else {
        if ((orgCode.indexOf("55-") >= 0) && (orgCode.length !== 12)
            || (orgCode.indexOf("55-") < 0) && (orgCode.length !== 9)) {
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

function SaveEncumbrance(commentJson) {
    var encumbrance = {};
    var groupID = $("#LineItemGroupID").val();
    if (groupID === "") { groupID = 0;}
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
    }else {
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
            if (result.CurrentStatus===("Draft")) {
                displayMessage("Encumbrance successfully saved as draft.");
            } else if (result.CurrentStatus===("Finance")){
                displayMessage("Encumbrance successfully submitted for Finance Review.");
                $("#btnEncumbranceDraft").remove();
                $("#btnEncumbranceFinance").remove();
            } else if (result.CurrentStatus === ("Work Program")) {
                displayMessage("Encumbrance successfully submitted for Work Program Review.");
                $("#btnEncumbranceRollback").remove();
                $("#btnEncumbranceFinance").remove();
                $("#btnEncumbranceWP").remove();
            } else if (result.CurrentStatus === ("CFM")) {
                displayMessage("Encumbrance successfully submitted for CFM Input.");
                $("#btnEncumbranceRollback").remove();
                $("#btnEncumbranceCFM").remove();
                $("#btnEncumbranceWP").remove();
            } else if (result.CurrentStatus === ("Complete")) {
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
    //if (total > 0) {
        $("#EncHeaderEncAmount").html("Amount: <h4>" + encumbranceTotal + "</h4>");
    //}
    var budgetCeiling = $("#BudgetCeiling").val();
    var compensation = $("#Compensation").val();
    var toggle = "hide";
    var compsWithCeilings = "3,4,5,7"
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
function showHideNegativeAmountOptions() {
    //var hasNeg = $("#FinancialInformationFormPanel #Amount").val().indexOf("-");
    var amount = parseFloat($("#FinancialInformationFormPanel #Amount").val().replace(/,/g, "").replace("$", "").replace("(", "-").replace(")", ""));

    if (!amount || amount >= 0) {
        // hide FlairAmendmentID and LineID6S in the LineItems form
        $("#LineItemID6SCell").css('visibility', 'hidden');
        $("#LineItemFlairIDCell").css('visibility', 'hidden');
        // TODO: hide File Attachment tool in the LineItems form
    } else {
        // show FlairAmendmentID and LineID6S in the LineItems form
        $("#LineItemID6SCell").css('visibility', 'visible');
        $("#LineItemFlairIDCell").css('visibility', 'visible');
        // TODO: show File Attachment tool in the LineItems form
    }
}
function updateReceiveEmails() {
    $("#User_ReceiveEmails").val($("#ReceiveEmailsOption").val())
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
function toggleDisabledUsers() {
    if ($("#showDisabledCheckbox").is(":checked"))
    {
        $(".disabledUser").show();
    } else {
        $(".disabledUser").hide();
    }
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
            var awardMessage = "<p>Click Award to create the Award encumbrance request for Contract #" + con + "?</p>";
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

function updateUserIsDisabled() {
    if ($("#reEnable").is(":checked")) {
        $("#User_IsDisabled").val(1)
    } else {
        $("#User_IsDisabled").val(0)
    }
}