// Write your JavaScript code.
$(document).ready(function () {

    //initialize DataTable to be filterable, sortable, and searchable
    $('#indexTable').DataTable();

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
    if ($("#CategorySelector").val() != "" && $("#CategoryID").val() === "") {
        //$("#CategorySelector").autocomplete("search"); // won't execute in page load
        $("#CategorySelector").val("");
    }
    if ($("#FundSelector").val() != "" && $("#FundID").val() === "") {
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
        },
        messages: {
            noResults: "", results: ""
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
        },
        messages: {
            noResults: "", results: ""
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
        },
        messages: {
            noResults: "", results: ""
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
        },
        messages: {
            noResults: "", results: ""
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
        },
        messages: {
            noResults: "", results: ""
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
        },
        messages: {
            noResults: "", results: ""
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
        },
        messages: {
            noResults: "", results: ""
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
        },
        messages: {
            noResults: "", results: ""
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
        },
        messages: {
            noResults: "", results: ""
        }
    });

    $("#FiscalYearList").change(function() {
        $("#FiscalYear").val($("#FiscalYearList").val());
        return false;
    }).change();

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
}); // end of ready function

function collapseSection() {
    alert("clicked");
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
function addVendor() {
    // make sure all values are populated
    // submit to /ContractStatus/Create controller
    $("AddVendorForm").submit();
    // close dialog and return focus to /Contracts/Edit
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
    var comment = $("#Comments_" + groupID).val();
    var flairAmend = $("#FlairAmendmentID_" + groupID).val();
    var userAmend = $("#UserAssignedID_" + groupID).val();
    var lineAmend = $("#AmendedLineItemID_" + groupID).val();

    var Contract = {};
    Contract.ContractID = contractID;
    var wpIDs = [];
    var WPReviewers = $('[id ^="wp_"][id $="_' + groupID + '"]:checked').each(function () {
        wpIDs.push($(this).val())
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
    LineItemGroupStatus.Comments = comment;
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
                "<strong>Encumbrance: </strong>" + groupID + "</td>" +
                "<td> <strong>Encumbrance Type: </strong>" + responsetext.LineItemType + "</td>" +
                "<td> <strong>Status: </strong>" + newStatus + "</td>" +
                "<td colspan = '2'><strong>FLAIR ID: </strong>" + responsetext.FlairAmendmentID + "<br/>" +
                "<strong>User Assigned ID: </strong>" + responsetext.UserAssignedID + "<br/>" +
                "<strong>Corrects FLAIR ID: </strong>" + responsetext.AmendedLineItemID + " </td>" +
                "<td colspan = '2'> <strong>Last Updated: </strong> " + getFormattedDateNow() + "<br/> by " + userName + "</td > " +
                "<td colspan = '2'> Successfully updated! </td>" +
                "<td colspan = '2' > <strong>Comments:</strong>" + comment + " </td > ";
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
            $("#UpdateSuccess").text(data);
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
    reloadForm.submit(); 
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
    var comp = $("#CompensationID").val();
    if (comp == 3 || comp == 4 || comp == 5) {
        $("#budgetCeilingMessage").text("A Budget Ceiling greater than $0 is required.");
    } else {
        $("#budgetCeilingMessage").text("");
    }
}
function updateOCAMessage() {
    // if State Program is 5, OCA must be a Right of Way value
    var sp = $("#StateProgramID").val();
    if (sp == 5) {
        $("#OCAMessage").text("Please select a Right of Way value for OCA.");
    } else {
        $("#OCAMessage").text("");
    }
}

function toggleEncumbranceHistory(groupID) {
    if ($("#encumbranceHistoryToggle_" + groupID).text() == "Show Encumbrance History") {
        $(".groupStatus_" + groupID).removeClass("hidden");
        $("#encumbranceHistoryToggle_" + groupID).text("Hide Encumbrance History");
    } else {
        $(".groupStatus_" + groupID).addClass("hidden");
        $("#encumbranceHistoryToggle_" + groupID).text("Show Encumbrance History");
    }
}

function toggleLineHistory(lineID) {
    if ($("#lineHistoryToggle_" + lineID).text() == "Show Line Comments") {
        $(".lineStatus_" + lineID).removeClass("hidden");
        $("#lineHistoryToggle_" + lineID).text("Hide Line Comments");
    } else {
        $(".lineStatus_" + lineID).addClass("hidden");
        $("#lineHistoryToggle_" + lineID).text("Show Line Comments");
    }
}

function validateContract() {
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
    return false;
}