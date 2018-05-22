// Write your JavaScript code.
$(document).ready(function () {

    //initialize DataTable to be filterable, sortable, and searchable
    $('#indexTable').DataTable();

    //initialize datepickers
    $(function () {
        $(".datepicker").datepicker({
        });
    });

    //initialize tooltips
    $(function () {
        $('[data-toggle="tooltip"]').tooltip();
    });

    // set textbox clear methods
    $("#VendorClear").click(function () {
        $("#VendorSelector").val('');
    });
    $("#ContractTypeClear").click(function () {
        $("#ContractTypeSelector").val('');
    });

    // preface OrgCode with "55-"
    $("#OrgCode").keyup(function () {
        var orgCode = $("#OrgCode").prop("value");
        while (orgCode.charAt(0) === "5" || orgCode.charAt(0) === "-") {
            orgCode = orgCode.substring(1, orgCode.length);
        }
        $("#OrgCode").val("55-"+orgCode);
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
                        return { label: item.workActivity, value: item.workActivity };
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
                        return { label: item.flairObject, value: item.flairObject };
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
                        return { label: item.financialProjectNumber, value: item.financialProjectNumber };
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
                        return { label: OCASelector, value: item.ocaSelector, OCAID: item.ocaid };
                    }));
                }
            });
        },
        select: function (event, ui) {
            $("#OCASelector").val(ui.item.label);
            $("#OCAID").val(ui.item.oCAID);
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
                        return { label: FundSelector, value: item.fundSelector, FundID: item.fundID };
                    }));
                }
            });
        },
        select: function (event, ui) {
            $("#FundSelector").val(ui.item.label);
            $("#FundID").val(ui.item.fundID);
            return false;
        },
        messages: {
            noResults: "", results: ""
        }
    });

}); // end of ready function

function populateFiscalYearList(selectedValue) {
    var fiscalYear = $('#FiscalYearList');
    var currentYear = new Date().getFullYear();
    var currentMonth = new Date().getMonth();
    if (currentMonth < 6) {
        currentYear = currentYear - 1;
    }
    var list = '';
    if (!selectedValue) {
        selectedValue = currentYear;
    }
    for (var i = currentYear - 3; i < currentYear + 10; i++) {
        if (selectedValue && i === selectedValue) {
            list += '\n<option selected value="' + i + '" >' + i + ' - ' + (i + 1) + '</option>';
        } else {
            list += '\n<option value="' + i + '" >' + i + ' - ' + (i + 1) + '</option>';
        }
    }

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
