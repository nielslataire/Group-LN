@modeltype SettingsModel 
@Code
    ViewData("Title") = "Instellingen"
End Code
@section PageStyle
<link rel="stylesheet" href="~/vendor/admin/magnific-popup/magnific-popup.css" />
<link rel="stylesheet" href="~/vendor/admin/select2/select2-bootstrap.css" />


end section
<div class="row">
    <div class="col-md-12">
        <div class="tabs">
            <ul class="nav nav-tabs">
                <li class="active">
                    <a href="#BankAccounts" data-toggle="tab"><i class="fa fa-bank"></i> Bankrekeningen</a>
                </li>
                @*<li>
                    <a href="#recent" data-toggle="tab">Recent</a>
                </li>*@
            </ul>
            <div class="tab-content">
                <div id="BankAccounts" class="tab-pane active">
                    <p>Popular</p>
                    <p>Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitat.</p>
                </div>
                @*<div id="recent" class="tab-pane">
                    <p>Recent</p>
                    <p>Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitat.</p>
                </div>*@
            </div>
        </div>
    </div>

    </div>
    
@section scripts



<script>
    $(window).load(function () {

        @If Not TempData("Message") Is Nothing Then
@<text>

        new PNotify({
            title: '@TempData("MessageTitle")',
            text: '@TempData("Message")',
            type: '@TempData("MessageType")'
        });
        </text>
        End If
    });
    
  
</script>

<script src="~/vendor/admin/pnotify/pnotify.custom.js"></script>
<script src="~/scripts/admin/ui-elements/examples.modals.js"></script>
end section