﻿// Write your JavaScript code.
$(document).ready(function () {
    initForms();
    addDialogs();
    showHideButtons();
});

function initForms() {

    //initialize DataTable to be filterable, sortable, and searchable
    $('[id^=indexTable]').DataTable();

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
                var src = $(this).attr("src").replace("images/Menu_gray.png","images/Menu_white.png");
                $(this).attr("src", src);
            })
            .mouseout(function () {
                var src = $(this).attr("src").replace("images/Menu_white.png","images/Menu_gray.png");
                $(this).attr("src", src);
            });
    });

    //sort messages in descending order
    $('#messageTable').DataTable({
        "order": [[ 0, 'desc']]
    });

    //initialize tabs
    $("#tabs").tabs();

    //initialize datepickers
    $(function () {
        $(".datepicker").datepicker({
            changeMonth: true,
            changeYear: true
        });
    });

    //repopulate selection fields on Back
    if ($("#CategorySelector").val() !== "" && $("#CategoryID").val() === "") {
        //$("#CategorySelector").autocomplete("search"); // won't execute in page load
        $("#CategorySelector").val("");
    }
    if ($("#FundSelector").val() !== "" && $("#FundID").val() === "") {
        //$("#FundSelector").autocomplete("search"); // won't execute in page load
        $("#FundSelector").val("");
    }

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

    //initialize autocomplete for EO from existing in LineItems
    $("#ExpansionObject").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/LineItems/ListEOs",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    response($.map(data, function (item) {
                        return { label: item.expansionObject, value: item.expansionObject };
                    }));
                }
            });
        }
    });

    //initialize autocomplete for Work Activity from existing in LineItems
    $("#WorkActivity").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/LineItems/ListWorkActivities",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    response($.map(data, function (item) {
                        return { label: item, value: item };
                    }));
                }
            });
        }
    });

    // Flair Object autocomplete from existing in LineItems
    $("#FlairObject").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/LineItems/ListFlairObj",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    response($.map(data, function (item) {
                        return { label: item, value: item };
                    }));
                }
            });
        }
    });

    //FinProjNumber autocomplete from existing in LineItems
    $("#FinancialProjectNumber").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/LineItems/ListFinProjNums",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    response($.map(data, function (item) {
                        return { label: item, value: item };
                    }));
                }
            });
        }
    });

    //Vendors
    $("#VendorSelector").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/Contracts/ListVendors",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    response($.map(data, function (item) {
                        var vendorSelector = item.vendorCode + " - " + item.vendorName;
                        $("#VendorIDValidation").hide();
                        return { label: vendorSelector, value: item.vendorSelector, vendorID: item.vendorID };
                    }));
                }
            });
        },
        select: function (event, ui) {
            $("#VendorSelector").val(ui.item.label);
            $("#VendorID").val(ui.item.vendorID);
            return false;
        }
    });

    //Contract Types
    $("#ContractTypeSelector").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/Contracts/ListContractTypes",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    response($.map(data, function (item) {
                        var contractTypeSelector = item.contractTypeCode + " - " + item.contractTypeName;
                        $("#ContractTypeIDValidation").hide();
                        return { label: contractTypeSelector, value: item.contractTypeSelector, contractTypeID: item.contractTypeID };
                    }));
                }
            });
        },
        select: function (event, ui) {
            $("#ContractTypeSelector").val(ui.item.label);
            $("#ContractTypeID").val(ui.item.contractTypeID);
            return false;
        }
    });

    //OCA
    $("#OCASelector").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/LineItems/ListOCAs",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    response($.map(data, function (item) {
                        var OCASelector = item.ocaCode + " - " + item.ocaName;
                        $("#OCAIDValidation").hide();
                        return { label: OCASelector, value: item.ocaSelector, OCAID: item.ocaid };
                    }));
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
                url: "/LineItems/ListFunds",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    response($.map(data, function (item) {
                        var FundSelector = item.fundCode + " - " + item.fundDescription;
                        $("#FundIDValidation").hide();
                        return { label: FundSelector, value: item.fundSelector, FundID: item.fundID };
                    }));
                }
            });
        },
        select: function (event, ui) {
            $("#FundSelector").val(ui.item.label);
            $("#FundID").val(ui.item.FundID);
            return false;
        }
    });

    //Category
    $("#CategorySelector").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/LineItems/ListCategories",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    response($.map(data, function (item) {
                        var CategorySelector = item.categoryCode + " - " + item.categoryName;
                        $("#CategoryIDValidation").hide();
                        return { label: CategorySelector, value: item.categorySelector, CategoryID: item.categoryID };
                    }));
                }
            });
        },
        select: function (event, ui) {
            $("#CategorySelector").val(ui.item.label);
            $("#CategoryID").val(ui.item.CategoryID);
            return false;
        }
    });

    //Contract (for LineItemGroups/Manage)
    $("#ContractSelector").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/LineItemGroups/ListContracts",
                type: "POST",
                dataType: "json",
                data: { searchString: request.term },
                success: function (data) {
                    response($.map(data, function (item) {
                        var ContractSelector = item.contractNumber;
                        $("#ContractIDValidation").hide();
                        return { label: item.contractNumber, value: item.contractNumber, ContractID: item.contractID };
                    }));
                }
            });
        },
        select: function (event, ui) {
            $("#ContractSelector").val(ui.item.label);
            $("#ContractID").val(ui.item.ContractID);
            // show the ContractPanel
            if ($("#ContractPanel")) {
                showContractPanel(ui.item.ContractID);
            }
            if ($("#LineItemsPanel")) {
                $("#LineItemsPanel").show();
            }
            return false;
        }
    });

    if ($("#ContractPanel")) {
        showContractPanel($("#ContractID").val());
    }

    $("#FiscalYearList").change(function () {
        $("#FiscalYear").val($("#FiscalYearList").val());
        $("#LineItem_FiscalYear").val($("#FiscalYearList").val());
        return false;
    }).change();

    // set Encumbrance Total
    setEncumbranceTotal();
    setContractAmountTotal();
   
} // *** end of initForms which is the body of the ready function ***

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
        width: 800
    });
    $('#addVendorLink').click(function () {
        $('#addVendorDialog').dialog("open");
    });

    $("#ContractDialog").dialog({
        autoOpen: false,
        width: 1200,
        resizable: false,
        title: 'New Contract',
        modal: true,
        open: function (event, ui) {
            var url = "/LineItemGroups/NewContractPartial";
            $(this).load(url, function () {
                // very important to initForms() to enable date pickers and autocompletes
                initForms();
            });
            //Contract Types
            $("#ContractTypeSelector").zIndex = $("#ContractDialog").zIndex + 1;
            //Vendors
            $("#VendorSelector").zIndex = $("#ContractDialog").zIndex + 1;
        },
        buttons: {
            "Close": function () {
                $(this).dialog("close");
            },
            "Save Contract": function () {
                if (ValidateContract()) {
                    SaveContractModal();
                    $(this).dialog("close");
                }
            }
        },
    });
    // ContractSelector is showing when modal is open, so I explicitly show and hide it when ContractDialog is opened/closed
    $("#ContractDialog").on('dialogclose', function (event) {
        $("#ContractSelector").show();
    });

    $("#LineItemDialog").dialog({
        autoOpen: false,
        width: 1200,
        resizable: false,
        title: 'New Line Item',
        modal: true,
        open: function (event, ui) {
            // put autocorrect controls on top of dialog
            $("#CategorySelector").zIndex = $("#LineItemDialog").zIndex + 1;
            $("#OCASelector").zIndex = $("#LineItemDialog").zIndex + 1;
            $("#FundSelector").zIndex = $("#LineItemDialog").zIndex + 1;
        },
        buttons: {
            "Close": function () {
                $(this).dialog("close");
            },
            "Save Line": function () {
                if (ValidateLineItem()) {
                    SaveLineItemModal();
                    $(this).dialog("close");
                }
            }
        },
    }).load("/LineItemGroups/NewLineItemPartial", function () {
        // very important to initForms() to enable date pickers and autocompletes
        initForms();
    });
}

function showHideButtons() {
    // hide all buttons
    $("[id^='btnEncumbrance']").each(function () {
        $(this).hide();
    });
    $("#noButtonDiv").hide();
    // depending on CurrentStatus and Roles, enable appropriate buttons
    var currentStatus = $("#CurrentStatus").val();
    var roles = $("#UserRoles").val();

    if ((currentStatus === "New" || currentStatus === "Draft") && roles.indexOf("Originator") >= 0) {
        $("#btnEncumbranceDraft").show();
        $("#btnEncumbranceFinance").show();
        return false;
    }
    if ((currentStatus === "Finance") && roles.indexOf("FinanceReviewer") >= 0) {
        $("#btnEncumbranceWP").show();
        $("#btnEncumbranceRollback").show();
        return false;
    }
    if ((currentStatus === "Work Program") && roles.indexOf("WPReviewer") >= 0) {
        $("#btnEncumbranceFinance").show();
        $("#btnEncumbranceCFM").show();
        return false;
    }
    if ((currentStatus === "CFM") && roles.indexOf("CFMSubmitter") >= 0) {
        $("#btnEncumbranceRollback").show();
        $("#btnEncumbranceComplete").show();
        return false;
    }
    if ((currentStatus === "Complete") && roles.indexOf("FinanceReviewer") >= 0) {
        $("#btnEncumbranceRollback").show();
        return false;
    }
    $("#noButtonDiv").show();
}
// ContractSelector is showing when modal is open, so I explicitly show and hide it when LineItemDialog is opened/closed
$("#LineItemDialog").on('dialogclose', function (event) {
    $("#ContractSelector").show();
});

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
function updateContractStatus() {
    // make sure all values are populated
    // submit to /ContractStatus/Create controller
    $("ContractStatusForm").submit();
    // close dialog and return focus to /Contracts/Edit
}
function openAddVendorDialog() {
    $("#addVendorDialog").dialog("open");
}

function openAddVendorPanel() {
    $("#addVendorPanel").show();
}
function hideAddVendorPanel() {
    $("#addVendorPanel").hide();
}
function addVendor() {
    // make sure all values are populated
    // submit to /ContractStatus/Create controller
    $("AddVendorForm").submit();
    // close dialog and return focus to /Contracts/Edit
}
function addNewVendor() {
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
function openEncumbranceSubmissionDialog(submitTo, wpUsers) {
    $("#SubmissionDialog").dialog({
        autoOpen: false,
        width: 600,
        resizable: false,
        title: 'Submit Encumbrance',
        modal: true,
        open: function (event, ui) {
            $("#ContractSelector").hide();
            $(this).html("");
            var defaultComment = "";
            var currentStatus = $("#CurrentStatus").val();
            if (submitTo === "Draft") {
                defaultComment = "Saved as Draft.";
            } else if (submitTo === "Finance") {
                defaultComment = "Submitted to Finance for review.";
            } else if (submitTo === "Work Program") {
                defaultComment = "Submitted to Work Program for review.";
            } else if (submitTo === "CFM") {
                defaultComment = "Submitted for input to CFM.";
            } else if (submitTo === "Complete") {
                defaultComment = "Input into CFM.";
            }
            // add a comment textarea
            var newStatusInput = "<input type= 'hidden' name='newStatus' id='newStatus' value = '" + submitTo + "' />";
            $(this).append(newStatusInput);
            var commentBox = "<strong>Comments:</strong><br/><textarea id='commentText' name='commentText' cols='50' rows='4'>" + defaultComment + "</textarea><br />";
            $(this).append(commentBox);
            // if CurrentStatus is Draft and submitTo is Finance, add a checkbox (checked) to send a receipt to the Originator
            if (currentStatus === "Draft" && submitTo === "Finance") {
                var receiptBox = "<input type='checkbox' id='receiptBox' name='receiptBox' checked /> Send a submission receipt to the originator. <br />";
                $(this).append(receiptBox);
            } else {
            // add a checkbox to send a notification to the originator
                var notifyOriginatorBox = "<input type='checkbox' id='notifyBox' name='notifyBox' checked /> Notify the originator of this update. <br />";
                $(this).append(notifyOriginatorBox);
            }
            // if CurrentStatus is Finance and submitTo is WorkProgram, add a set of checkboxes to select WP recipients
            if (currentStatus === "Finance" && submitTo === "Work Program") {
                var wpBox = "<div name='wpRecipients' id='wpRecipients'>";
                wpBox += "Select the Work Program recipients to be notified: <br/>";
                //var recips = [];
                for (var i in wpUsers) {
                    var wpUser = wpUsers[i];
                    //recips.push(wpUsers[i]);
                    // properties from json are rendered in lower case
                    wpBox += "<input type='checkbox' id='wpUser_" + wpUser.userID + "' name='wpUser_" + wpUser.userID + "' value="+ wpUser.userID +" /> " + wpUser.firstName + " " + wpUser.lastName + " <br />";
                }
                wpBox += "</div>";
                $(this).append(wpBox);
            }
        },
        buttons: {
            "Close": function () {
                $("#ContractSelector").show();
                $(this).dialog("close");
            },
            "Submit": function () {
                $("#ContractSelector").show();
                $(this).dialog("close");
                var commentJson = getSubmissionDetails();
                SaveEncumbrance(commentJson);
            }
        },
    });
    $("#SubmissionDialog").dialog("open");
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
    if ($("#wpRecipients")) {
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
    jsonString += "\"comments\" : \"" + $("#commentText").val() + "\"";
    jsonString += "}";

    return jsonString;
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

function toggleEncumbrance(header) {
    $("#groupHeader_" + header).nextUntil("tr.groupHeader").slideToggle(1000);
    var linkText = $("#toggleLink_" + header).text();
    if (linkText === "Show encumbrance items") {
        $("#toggleLink_" + header).text("Hide encumbrance items");
    } else {
        if (linkText === "Hide encumbrance items") $("#toggleLink_" + header).text("Show encumbrance items");
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

function updateContractStatus() {

    var userID = $("#Contract_UserID").val();
    var contractID = $("#Contract_ContractID").val();
    var newStatus = $("#Contract_CurrentStatus").val();
    var statusComment = $("#ContractStatus_Comments").val();

    var ContractStatusObject = {};
    ContractStatusObject.UserID = userID;
    ContractStatusObject.CurrentStatus = newStatus;
    ContractStatusObject.Comments = statusComment;
    ContractStatusObject.ContractID = contractID;

    jQuery.ajaxSettings.traditional = true;
    $.ajax({
        type: "POST",
        ContentType: "application/json; charset=utf-8",
        dataType: 'html',
        data: { contractStatus : JSON.stringify(ContractStatusObject) },
        url: '/Contracts/UpdateStatus/',
        success: function (data) {
            $("#currentStatusSpan").text(data);
            $("#UpdateSuccess").text("The contract status has been successfully updated.");
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
    if (comp === "3" || comp === "4" || comp === "5") {
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
    return false;
}

function updateAddedInfo() {
   // $("#AddedInfoDiv").html();
    var encumbranceType = $("#LineItemType").val();
    var contractID = $("#ContractID").val();
    if (contractID > 0) {
        getEncumbranceCount(encumbranceType, contractID) + 1;
    }
}

function getEncumbranceCount(encumbranceType, contractID) {
    var EncumbranceInfo = {};
    EncumbranceInfo.encumbranceType = encumbranceType;
    EncumbranceInfo.contractID = contractID;
    $.ajax({
        type: "POST",
        ContentType: "application/json; charset=utf-8",
        dataType: 'html',
        data: { encumbranceInfo: JSON.stringify(EncumbranceInfo) },
        url: '/LineItemGroups/GetEncumbranceCountByType/',
        success: function (response) {

            var encNumber = parseInt(response) + 1;
            var prefix = "";
            $("#AmendedIDDiv").hide();
            if (encumbranceType === "Renewal") {
                prefix = "RNW#";
                $("#NewEndDate").show();
            }
            if (encumbranceType === "Supplemental") {
                prefix = "SUP#";
            }
            if (encumbranceType === "LOA") {
                prefix = "LOA#";
                $("#AmendedIDDiv").show();
            }
            if (encumbranceType === "Amendment") {
                prefix = "AMD#";
                $("#AmendedLOA").show();
            }
            if (prefix.length > 0) {
                $("#UserAssignedID").val(prefix + encNumber);
            }
            if (encumbranceType === "Advertisement") {
                displayMessage("Please add Letting Date to the description.");
            }
            if (encumbranceType === "Award") {
                displayMessage("Please update the contract to reflect the awarded amount and vendor.");
            }
        }
    });
}
function openContractDialog() {
    $("#ContractDialog").dialog("open");
    $("#ContractSelector").hide();

    $("#ContractTypeSelector").autocomplete("option", "appendTo", "#ContractDialog");
    $("#VendorSelector").autocomplete("option", "appendTo", "#ContractDialog");
}

function openContractDialogExisting(id) {
    url = "/LineItemGroups/NewContractPartial/" + id;
    $('#ContractDialog').load(url, function () { initForms(); });
    
    $("#ContractDialog").dialog("open");
    $("#ContractSelector").hide();

    $("#ContractTypeSelector").autocomplete("option", "appendTo", "#ContractDialog");
    $("#VendorSelector").autocomplete("option", "appendTo", "#ContractDialog");
}

function openLineItemDialog(callback) {
    var userID = $("#UserID").val();
    var contractID = $("#ContractID").val();
    var lineItemGroupID = $("#LineItemGroupID").val();
    if (!contractID) {
        alert("Please select or add a contract before adding or editing a Line Item.")
        return;
    }
    if (!lineItemGroupID) {
        alert("Please Save As Draft before adding a Line Item.")
        return;
    }
    $("#LineItemDialog").dialog("open");
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
        $("#FlairObject").val(lineItem.FlairObject);
        $("#OCASelector").val(lineItem.OCA.OCASelector);
        $("#OCAID").val(lineItem.OCAID);
        $("#StateProgramName").val(lineItem.StateProgram.ProgramSelector);
        $("#StateProgramID").val(lineItem.StateProgramID);
        $("#FinancialProjectNumber").val(lineItem.FinancialProjectNumber);
        $("#WorkActivity").val(lineItem.WorkActivity);
        $("#Comments").text(lineItem.Comments);
        $("#ExpansionObject").val(lineItem.ExpansionObject);
        if (isDuplicate) {
            // #LineNumber val gets set in the callback
            getNextLineNumber("{\"groupID\" : " + lineItem.LineItemGroupID + "}");
            $("#LineItemID").val(0);
        } else {
            $("#LineItemID").val(lineItem.LineItemID);
            $("#LineNumber").val(lineItem.LineNumber);
        }
        populateFiscalYearList(lineItem.FiscalYear);
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
            $("#LineNumber").val(nextLine);
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
            alert(result.success);
            // remove the line item from the table
            $("#row_item_" + lineItemID).remove();
        },
        fail: function (data) {
            var result = JSON.parse(data);
            alert(result.fail);
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
    if ($("#IsRenewable1").val()){ Contract.IsRenewable = 1; }; // undefined
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
            if ($("#ContractID").val() && $("#ContractID").val() > 0) {
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
        lineItem.LineItemGroupID = $("#LineItemGroupID").val();
    }

    // these properties are identical to the line item group
    // populate them from the Create page, if they have values
    if ($("#LineItemType") && $("#LineItemType").val().length > 0) {
        lineItem.LineItemType = $("#LineItemType").val();
    } else {
        lineItem.LineItemType = "New";
    }
    if ($("#FlairAmendmentID") && $("#FlairAmendmentID").val()) {
        lineItem.FlairAmendmentID = $("#FlairAmendmentID").val()
    } else {
        lineItem.FlairAmendmentID = "";
    }
    if ($("#UserAssignedID") && $("#UserAssignedID").val()) {
        lineItem.UserAssignedID = $("#UserAssignedID").val()
    } else {
        lineItem.UserAssignedID = "";
    }
    if ($("#AmendedLineItemID") && $("#AmendedLineItemID").val()) {
        lineItem.AmendedLineItemID = $("#AmendedLineItemID").val()
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
                displayMessage("Line Item number " + result.LineNumber + " updated.")
            } else {
                $("#LineItemsTableBody:last-child").append(newRow);
                displayMessage("Line Item number " + result.LineNumber + " added.")
            }
            // reset the encumbrance total amount
            setEncumbranceTotal();
            setContractAmountTotal();
            // show the line items table
            $("#LineItemsTable").show();
        }
    });
}

function getNewLineItemRow(lineItem) {
    var itemKey = "item_" + lineItem.LineItemID;
    var rowID = "row_" + itemKey;
    var tableText = "";
    var FY = parseInt(FiscalYear.value);
    if ((lineItem.OrgCode).indexOf("55-") < 0) { lineItem.OrgCode = "55-" + lineItem.OrgCode; }
    tableText += "<tr class='groupItem' id='" + rowID + "'>";
    tableText += "<td id='" + itemKey + "_LineItemNumber'>" + lineItem.LineItemNumber + "<input type='hidden' id='" + itemKey + "_LineItemID' value='" + lineItem.LineItemID + "'/> <br />";
    if ($("#UserRoles").val().indexOf("Originator") >= 0 ) {
        tableText += "<a href='javascript:editLineItem(" + lineItem.LineItemID + ", false)' > Edit</a > <br />";
        tableText += "<a href='javascript:editLineItem(" + lineItem.LineItemID + ", true)'>Duplicate</a> <br />";
        tableText += "<a href='LineItems/Delete /" + lineItem.LineItemID + "'>Delete</a> <br />";
    }
    tableText += "</td>";
    tableText += "<td>" + lineItem.LineItemID + "</td>";
    tableText += "<td>" + lineItem.OrgCode + "</td>";
    tableText += "<td>" + lineItem.FinancialProjectNumber + "</td>";
    var sp_array = lineItem.StateProgramName.split("-");
    tableText += "<td title='" + sp_array[1].trim() + "'>" + sp_array[0].trim() + "<input type='hidden' id='" + itemKey + "_StateProgramID' value='" + lineItem.StateProgramID + "'/> </td>";
    var cn_array = lineItem.CategoryName.split("-");
    tableText += "<td title='" + cn_array[1].trim() + "'>" + cn_array[0].trim() + "<input type='hidden' id='" + itemKey + "_CategoryID' value='" + lineItem.CategoryID + "'/> </td>";
    tableText += "<td>" + lineItem.WorkActivity + "</td>";
    var oca_array = lineItem.OcaName.split("-");
    tableText += "<td title='" + oca_array[1].trim() + "'>" + oca_array[0].trim() + "<input type='hidden' id='" + itemKey + "_OCAID' value='" + lineItem.OcaID + "'/> </td>";
    tableText += "<td>" + lineItem.EO + "</td>";
    tableText += "<td>" + lineItem.FlairObject + "</td>";
    var fund_array = lineItem.FundName.split("-");
    tableText += "<td title='" + fund_array[1].trim() + "'>" + fund_array[0].trim() + "<input type='hidden' id='" + itemKey + "_FundID' value='" + lineItem.FundID + "'/> </td>";
    tableText += "<td>" + (FY - 1) + " - " + FY + "</td>";
    tableText += "<td>" + lineItem.Amount.toLocaleString() + "<input type='hidden' id='" + itemKey + "_Amount' value='" + lineItem.Amount + "'/>";

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
    lineItem.Amount = parseFloat(lineItem.Amount.replace(",", "").replace("$", ""));
    lineItem.LineNumber = lineItem.LineItemNumber;
    var jsonString = JSON.stringify(lineItem);

    tableText += "<input type='hidden' id='json_" + itemKey + "' value='" + jsonString + "'/> </td>";
    tableText += "</tr>";
    return tableText;
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
    var contractHtml = "";
    contractHtml += "<div class='row'>";
    contractHtml += "<div class='col-sm-4'><strong>Originated by:</strong> <a href='mailto:" + contract.OriginatorEmail + "'>" + contract.OriginatorName + "</a> (" + contract.OriginatorLogin + ") " + contract.OriginatorPhone + "</div>";
    contractHtml += "<div class='col-sm-3'><strong>Created Date:</strong> " + contract.CreatedDate + "</div>";
    contractHtml += "<div class='col-sm-3'><strong>Modified Date:</strong> " + contract.ModifiedDate + "</div>";
    contractHtml += "<div class='col-sm-2'><dl><dt> <a href= 'javascript:openContractDialogExisting(" + contract.ContractID + ")'>Edit Contract</a> </div>";
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
    contractHtml += "<div class='col-sm-3'><dl><dt> Contract Funding Terms:</dt><dd> " + contract.CompensationName + "</dd></dl></div>";
    contractHtml += "</div><div class='row'>";
    contractHtml += "<div class='col-sm-2'><dl><dt> Maximum LOA Amount:</dt><dd> " + contract.FormattedMaxLoaAmount + "</dd></dl></div>";
    contractHtml += "<div class='col-sm-2'><dl><dt> Contract End Date:</dt><dd> " + contract.FormattedEndingDate + "</dd></dl></div>";
    contractHtml += "<div class='col-sm-3'><dl><dt> Vendor:</dt><dd> " + contract.VendorName + "</dd></dl></div>";
    contractHtml += "</div><div class='row'>";
    contractHtml += "<div class='col-sm-2'><dl><dt> Budget Ceiling:</dt><dd> " + contract.FormattedBudgetCeiling + "</dd></dl></div>";
    contractHtml += "<div class='col-sm-2'><dl><dt> Service End Date:</dt><dd> " + contract.FormattedServiceEndingDate + "</dd></dl></div>";
    contractHtml += "<div class='col-sm-3'><dl><dt> Recipient:</dt><dd> " + contract.RecipientName + "</dd></dl></div>";
    contractHtml += "</div><div class='row'>";
    var description = (contract.DescriptionOfWork) ? contract.DescriptionOfWork : "";
    contractHtml += "<div class='col-sm-4'><dl><dt> Description of Work:</dt><dd> " + description + "</dd></dl></div>";
    contractHtml += "</div>";

    $("#ContractPanelBody").html(contractHtml);
    $("#ContractPanel").show();
}

function UpdateGroupStatus(status) {
    if (ValidateEncumbrance()) {
        if (status === "Work Program") {
            var wpUsers = getWPUsers(status);
        } else {
            openEncumbranceSubmissionDialog(status, null);
        }
    }
}

function ValidateEncumbrance() {
    // Check all required fields on the Encumbrance form for valid values
    var isErrorFree = true; // set to false when an error is found
    var msg = "";
    if (!$("#LineItemType").val() || $("#LineItemType").val() === "None") {
        msg += "Please select an Encumbrance Type. <br/>";
        isErrorFree = false;
    }
    if (!$("#ContractID").val()) {
        msg += "Please select or create a Contract. <br/>";
        isErrorFree = false;
    }

    // use displayMessage() to show validation message.
    displayMessage(msg);
    return isErrorFree;
}

function ValidateContract() {
    // Check all required fields on the Contract form for valid values
    var isErrorFree = true; // set to false when an error is found
    var msg = "";
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
    if (!$("#BeginningDate").val()) {
        msg += "Please select a Beginning Date. <br/>";
        isErrorFree = false;
    }
    if (!$("#EndingDate").val()) {
        msg += "Please select an Ending Date. <br/>";
        isErrorFree = false;
    }
    if (!$("#ServiceEndingDate").val()) {
        msg += "Please select a Service Ending Date. <br/>";
        isErrorFree = false;
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
        if ((orgCode.indexOf("55-") >= 0) && (orgCode.length != 12)
            || (orgCode.indexOf("55-") < 0) && (orgCode.length != 9)) {
            msg += "The Organization Code must be \"55-\" followed by 9 digits. <br/>";
            isErrorFree = false;
        }
    }
    var finProjNum = $("#FinancialProjectNumber").val();
    if (!finProjNum) {
        msg += "Please enter an 11 character Financial Project Number. <br/>";
        isErrorFree = false;
    } else {
        if (finProjNum.length != 11) {
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
        if (workActivity.length != 3) {
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
        if (eo.length != 2) {
            msg += "The EO must be two characters long. <br/>";
            isErrorFree = false;
        }
    }
    var objCode = $("#FlairObject").val();
    if (!objCode) {
        msg += "Please select a FLAIR Object Code. <br/>";
        isErrorFree = false;
    } else {
        if (objCode.length != 6) {
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
    encumbrance.LineID6s = $("#LineID6s").val();
    encumbrance.OriginatorUserID = $("#UserID").val();
    encumbrance.isEditable = 1;
    encumbrance.CurrentStatus = $("CurrentStatus").val();
 
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
        data: { lineItemGroup: JSON.stringify(encumbrance), comments : commentJson },
        success: function (data) {
            var result = JSON.parse(data);
            // return the completed Contract object to the calling form and use it to populate ContractPanel div
            updateEncumbrance(result);
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
}

function setEncumbranceTotal() {
    // Add amounts from each record and populate Encumbrance Total span
    var total = 0.00;
    $("[id $= '_Amount']").each(function () {
        var amt = $(this).val();
        amt = amt.replace(",", "");
        amt = amt.replace("$", "");
        total += Number(amt);
    });
    var encumbranceTotal = formatCurrency(total);
    $("#EncumbranceTotal").html("<strong>Encumbrance Total: </strong>" + encumbranceTotal);
}

function setContractAmountTotal() {
    // TODO: get total value of all line items in all encumbrances for this contract
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

function displayLineItemMessage(msg) {
    $("#messageSpanLineItem").html(msg);
}
function displayMessage(msg) {
    $("#messageSpan").html(msg);
}
function displayContractMessage(msg) {
    $("#messageSpanContract").html(msg);
}