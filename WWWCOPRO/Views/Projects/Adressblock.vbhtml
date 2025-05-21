@modeltype BO.CompanyBO
<div class="col-md-offset-1 col-md-11">
    <p class="text-center"><h5 class="mb-none heading-primary">@Model.Bedrijfsnaam</h5></p>
    <p>
        <ul class="list list-icons">
            <li class="mb-none"><i class="fa fa-map-marker"></i> @Model.Straat @Model.Huisnummer <br /> @Model.Postcode.Postcode @Model.Postcode.Gemeente</li>
            @If Not Model.FormattedTelefoon Is Nothing Then
                @<text>
                    <li class="mb-none"><i class="fa fa-phone"></i> @Model.FormattedTelefoon</li>
                </text>
            End If
            @If Not Model.Email Is Nothing Then
                @<text>
                    <li class="mb-none"><i class="fa fa-envelope"></i> <a href="mailto:@Model.Email">@Model.Email</a></li>
                </text>
            End If
            @If Not Model.URL Is Nothing Then
                @<text>
                    <li class="mb-none"><i class="fa fa-globe"></i> <a href="http://@Model.URL.ToString" target="_blank">@Model.URL</a></li>
                </text>
            End If
        </ul>
    </p>
</div>