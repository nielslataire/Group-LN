@imports bo
@modeltype ProjectSalesCalculatePrice
<script src="~/scripts/autoNumeric/autoNumeric.js"></script>
@Using Html.BeginForm("PrintPrice", "Projecten", FormMethod.Post, New With {.id = "FormPrintPrice", .class = "form-horizontal mb-lg"})
    @<text>
        @Html.HiddenFor(Function(m) m.ProjectId)
        @Html.HiddenFor(Function(m) m.SalesSettings.BaseCertificateCost)
        @Html.HiddenFor(Function(m) m.SalesSettings.ConnectionFees)
        @Html.HiddenFor(Function(m) m.SalesSettings.FixedCertificateCost)
        @Html.HiddenFor(Function(m) m.SalesSettings.MixedVatRegistration)
        @Html.HiddenFor(Function(m) m.SalesSettings.MortageRegistrationCost)
        @Html.HiddenFor(Function(m) m.SalesSettings.RegistrationPercentage)
        @Html.HiddenFor(Function(m) m.SalesSettings.ProjectId)
        @Html.HiddenFor(Function(m) m.SalesSettings.SettingsId)
        @Html.HiddenFor(Function(m) m.SalesSettings.VatPercentage)
        @Html.HiddenFor(Function(m) m.SalesSettings.RegistrationType)
        @Html.HiddenFor(Function(m) m.SalesSettings.SurveyorCost)
        <section class="panel">
            <header class="panel-heading">
                <h2 class="panel-title">Raming aankoopprijs</h2>
            </header>
            <div class="panel-body">

                @*@For Each unit In Model.Units
                        Html.RenderPartial("_UnitWithReductionRow", unit)
                    Next*@
                <Table Class="table mb-none">



                    @For j As Integer = 0 To Model.Units.Count - 1
                        @*Dim parentIdentifier As String = ""
            @Using (Html.BeginCollectionItem("Units", parentIdentifier))*@
                        @<text>
                            <tr Class="active text-weight-semibold">
                                <td colspan="2" width="50%">
                                    @Html.HiddenFor(Function(m) Model.Units(j).Base.Id)
                                    @Html.HiddenFor(Function(m) Model.Units(j).Base.Type.Id)
                                    @Html.HiddenFor(Function(m) Model.Units(j).Base.Type.GroupId)
                                    @Html.HiddenFor(Function(m) Model.Units(j).Base.Type.Name)
                                    @Html.HiddenFor(Function(m) Model.Units(j).Base.Name)
                                    @Html.HiddenFor(Function(m) Model.Units(j).Base.TotalValue)
                                    @Html.HiddenFor(Function(m) Model.Units(j).Base.LandValue)
                                    @Model.Units(j).Base.Type.Name @Model.Units(j).Base.Name
                                </td>
                                <td Class="text-right" width="25%"></td>
                                <td Class="text-right">
                                    @String.Format("{0:C}", Model.Units(j).Base.TotalValue)

                                </td>
                            </tr>

                            <tr>
                                <td width="5%"></td>
                                <td>Registratie</td>
                                <td Class="text-right" width="20%">@Html.EditorFor(Function(m) Model.Units(j).ReductionLandValue, New With {.class = "form-control input-sm text-right", .id = "reductionlandvalue"})</td>
                                <td Class="text-right">@String.Format("{0:C}", Model.Units(j).Base.LandValue)</td>
                            </tr>
                            @For i As Integer = 0 To Model.Units(j).Base.ConstructionValues.Count - 1
                                @*Dim childIdentifier As String = ""
                    @Using (Html.BeginChildCollectionItem("ConstructionValues", parentIdentifier, childIdentifier))*@
                                @<text>
                                    <tr>
                                        <td>
                                            @Html.HiddenFor(Function(m) Model.Units(j).Base.ConstructionValues(i).Description)
                                            @Html.HiddenFor(Function(m) Model.Units(j).Base.ConstructionValues(i).Id)
                                            @Html.HiddenFor(Function(m) Model.Units(j).Base.ConstructionValues(i).PaymentGroupId)
                                            @Html.HiddenFor(Function(m) Model.Units(j).Base.ConstructionValues(i).UnitId)
                                            @Html.HiddenFor(Function(m) Model.Units(j).Base.ConstructionValues(i).Value)
                                        </td>
                                        <td> @Model.Units(j).Base.ConstructionValues(i).Description</td>
                                        <td Class="text-right">
                                            @Html.EditorFor(Function(m) Model.Units(j).Base.ConstructionValues(i).Reduction, New With {.class = "form-control input-sm text-right", .id = "reductionlandvalue"})@Html.HiddenFor(Function(m) Model.Units(j).Base.ConstructionValues(i).Id)
                                        </td>
                                        <td Class="text-right">@String.Format("{0:C}", Model.Units(j).Base.ConstructionValues(i).Value)</td>
                                    </tr>
                                </text>
                                'End Using
                            Next

                        </text>
                        'End Using

                    Next


                    @If Model.SalesSettings.MixedVatRegistration = False Then
                        @<text>
                            <tr class="active text-weight-semibold">
                                <td colspan="2">
                                    @String.Format("{0:F0}", Model.SalesSettings.RegistrationPercentage) % Registratierechten
                                </td>
                                <td class="text-right"></td>
                                <td class="text-right">
                                    @String.Format("{0:C}", Model.Units.Sum(Function(m) m.Base.LandValue) / 100 * Model.SalesSettings.RegistrationPercentage)
                                </td>
                            </tr>
                        </text>
                        @For Each type In Model.Units.Select(Function(m) m.Base.Type.Id).Distinct()
                            @<text>
                                <tr>
                                    <td></td>
                                    <td>Registratie @Model.SalesSettings.RegistrationType.GetDisplayName().ToLower @Model.Units.Where(Function(l) l.Base.Type.Id = type).FirstOrDefault().Base.Type.Name.ToLower()  </td>
                                    <td class="text-right"></td>
                                    <td class="text-right">@String.Format("{0:C}", Model.Units.Where(Function(i) i.Base.Type.Id = type).Sum(Function(m) m.Base.LandValue) / 100 * Model.SalesSettings.RegistrationPercentage)</td>
                                </tr>
                            </text>
                        Next
                        If Model.SalesSettings.RegistrationType = RegistrationType.existingbuilding Then
                            @<text>
                                <tr>
                                    <td></td>
                                    <td><div class="Checkbox"><label>Enige eigen woning  @Html.CheckBoxFor(Function(m) m.OneAndOwnHome, New With {.id = "enige_eigen_woning"})</label></div></td>
                                    <td class="text-right enige_eigen_woning"></td>
                                    <td class="text-right"></td>
                                </tr>
                                @*<tr>
                        <td></td>
                        <td><div class="Checkbox"><label>Verhoogd abattement  @Html.CheckBoxFor(Function(m) m.RaisedAbatement, New With {.id = "raisedabatement"})</label></div></td>
                        <td class="text-right raisedabatement">€ -</td>
                        <td class="text-right">€ -</td>
                    </tr>*@
                            </text>
                        End If

                    End If
                    <tr class="active text-weight-semibold">
                        <td colspan="2">Notariskosten</td>
                        <td class="text-right"></td>
                        <td class="text-right ">@String.Format("{0:C}", CalculatePrice.CalculateNotaryFees(Model.Units, Model.SalesSettings.MixedVatRegistration) + Model.SalesSettings.FixedCertificateCost + Model.SalesSettings.BaseCertificateCost * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count + Model.SalesSettings.MortageRegistrationCost)</td>
                    </tr>
                    <tr>
                        <td></td>
                        <td>Notariskosten</td>
                        <td Class="text-right"></td>
                        <td Class="text-right">@String.Format("{0:C}", CalculatePrice.CalculateNotaryFees(Model.Units, Model.SalesSettings.MixedVatRegistration))</td>
                    </tr>
                    <tr>
                        <td>
                        </td>

                        <td> Vaste Aktekost</td>
                        <td Class="text-right"></td>
                        <td Class="text-right">@Html.DisplayFor(Function(m) m.SalesSettings.FixedCertificateCost)</td>
                    </tr>

                    <tr>
                        <td></td>
                        <td> Aandeel Basisakte</td>
                        @If Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count > 1 Then
                            @<text>
                                <td Class="text-right"></td>
                                <td Class="text-right">@String.Format("{0:C}", (Model.SalesSettings.BaseCertificateCost * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count))</td>
                            </text>
                        Else
                            @<text>
                                <td Class="text-right"></td>
                                <td Class="text-right">@Html.DisplayFor(Function(m) m.SalesSettings.BaseCertificateCost)</td>
                            </text>
                        End If

                    </tr>
                    <tr>
                        <td></td>
                        <td> Aandeel Verkavelingsakte</td>
                        @If Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count > 1 Then
                            @<text>
                                <td Class="text-right"></td>
                                <td Class="text-right">@String.Format("{0:C}", (Model.SalesSettings.ParcelCost * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count))</td>
                            </text>
                        Else
                            @<text>
                                <td Class="text-right"></td>
                                <td Class="text-right">@Html.DisplayFor(Function(m) m.SalesSettings.ParcelCost)</td>
                            </text>
                        End If

                    </tr>
                    <tr>
                        <td></td>
                        <td> Hypotheekkantoor</td>
                        <td Class="text-right"></td>
                        <td Class="text-right">@Html.DisplayFor(Function(m) m.SalesSettings.MortageRegistrationCost)</td>
                    </tr>
                    <tr class="active text-weight-semibold">
                        <td colspan="2">@Html.LabelFor(Function(m) m.SalesSettings.SurveyorCost)</td>
                        <td Class="text-right"></td>
                        <td Class="text-right">@String.Format("{0:C}", (Model.SalesSettings.SurveyorCost * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count))</td>
                    </tr>
                    <tr class="active text-weight-semibold">
                        <td colspan="2"> Raming Aansluitkosten</td>
                        @If Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count > 1 Then
                            @<text>
                                <td Class="text-right"></td>
                                <td Class="text-right">@String.Format("{0:C}", (Model.SalesSettings.ConnectionFees * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count))</td>
                            </text>
                        Else
                            @<text>
                                <td Class="text-right"></td>
                                <td Class="text-right">@Html.DisplayFor(Function(m) m.SalesSettings.ConnectionFees)</td>
                            </text>
                        End If

                    </tr>


                    <tr class="active text-weight-semibold">
                        <td colspan="2">
                            BTW
                        </td>
                        <td class="text-right">
                        </td>
                        <td class="text-right">
                            @String.Format("{0:C}", CalculatePrice.CalculateTotalVat(Model.Units, Model.SalesSettings))
                        </td>
                    </tr>

                    @For Each unit In Model.Units
                        @for each constructionvalue In unit.Base.ConstructionValues
                            @<text>
                                <tr>
                                    <td></td>
                                    <td>@CalculatePrice.GetVatPercentage(constructionvalue.PaymentGroupId).ToString("0")@Html.Raw("% ") BTW op @constructionvalue.Description - @unit.Base.Type.Name.ToLower() @unit.Base.Name.ToLower()  </td>
                                    @if Model.SalesSettings.MixedVatRegistration = True Then
                                        @<text>
                                            <td Class="text-right"></td>
                                            <td Class="text-right">@String.Format("{0:C}", (constructionvalue.Value + unit.Base.LandValue) / 100 * Model.SalesSettings.VatPercentage)</td>
                                        </text>
                                    Else
                                        @<text>
                                            <td Class="text-right"></td>
                                            <td Class="text-right">@String.Format("{0:C}", CalculatePrice.CalculateConstructionPriceVat(constructionvalue))</td>
                                        </text>
                                    End If
                                </tr>
                            </text>
                        Next
                    Next
                    <tr>
                        <td></td>
                        <td> BTW op notariskosten</td>
                        <td Class="text-right"></td>
                        <td Class="text-right">@String.Format("{0:C}", CalculatePrice.CalculateNotaryFees(Model.Units, Model.SalesSettings.MixedVatRegistration) / 100 * Model.SalesSettings.VatPercentage)</td>
                    </tr>
                    <tr>
                        <td></td>
                        <td> BTW op vaste aktekost</td>
                        <td Class="text-right"></td>
                        <td Class="text-right">@String.Format("{0:C}", Model.SalesSettings.FixedCertificateCost / 100 * Model.SalesSettings.VatPercentage)</td>
                    </tr>
                    <tr>
                        <td></td>
                        <td> BTW op aandeel basisakte</td>
                        <td Class="text-right"></td>
                        <td Class="text-right">@String.Format("{0:C}", Model.SalesSettings.BaseCertificateCost * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count / 100 * Model.SalesSettings.VatPercentage)</td>
                    </tr>
                    <tr>
                    <tr>
                        <td></td>
                        <td> BTW op aandeel verkavelingsakte</td>
                        <td Class="text-right"></td>
                        <td Class="text-right">@String.Format("{0:C}", Model.SalesSettings.ParcelCost * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count / 100 * Model.SalesSettings.VatPercentage)</td>
                    </tr>
                    <tr>
                        <td></td>
                        <td> BTW op aansluitkosten</td>
                        <td Class="text-right"></td>
                        <td Class="text-right">@String.Format("{0:C}", Model.SalesSettings.ConnectionFees * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count / 100 * Model.SalesSettings.VatPercentage)</td>
                    </tr>
                    <tr>
                        <td></td>
                        <td> BTW op landmetingskosten</td>
                        <td Class="text-right"></td>
                        <td Class="text-right">@String.Format("{0:C}", Model.SalesSettings.SurveyorCost * Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count / 100 * Model.SalesSettings.VatPercentage)</td>
                    </tr>
                    <tr class="active text-weight-semibold">
                        <td colspan="2">
                            Totale aankoopprijs
                        </td>
                        <td class="text-right">
                        </td>
                        <td class="text-right">
                            @String.Format("{0:C}", CalculatePrice.CalculateTotalPrice(Model.Units, Model.SalesSettings, Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count()))
                        </td>
                    </tr>

                </Table>


                                                                                                                                                                            </div>

                                                                                                                                                                                    <footer Class="panel-footer">
                                                                                                                                                                                        <div Class="row">
                                                                                                                                                                                            <div Class="col-md-12 text-right">
                                                                                                                                                                                                <Button Class="btn btn-primary btn-block">Afdrukken</Button>
                                                                                                                                                                                                <Button Class="btn btn-default btn-block modal-dismiss">Annuleren</button>
                                                                                                                                                                                            </div>
                                                                                                                                                                                        </div>
                                                                                                                                                                                    </footer>
                                                                                                                                                                                </section>
                                                                                                                                                                            </text>
                                                                                                                                                                            End Using
<script>
    $(document).ready(function () {
        $("input[id^='reductionlandvalue'],input[id*='reductionlandvalue']").each(function () {
            $(this).autoNumeric('init', { aSign: '€ ', pSign: 'p', aSep: '.', aDec: ',' });
        });
        $("input[id^='reductionconstructionvalue'],input[id*='reductionconstructionvalue']").each(function () {
            $(this).autoNumeric('init', { aSign: '€ ', pSign: 'p', aSep: '.', aDec: ',' });
        });

    });

    $('.reductionconstructionvalue').change(function () {
        calculateprice($(this).attr('data-id'));

    });
    $('.reductionlandvalue').change(function () {
        calculateprice($(this).attr('data-id'));

    });
    $('#enige_eigen_woning').change(function () {
        calculateprice(@Model.Units.FirstOrDefault().Base.Type.Id);
    });

    function calculateprice(id) {
        var totalvalue = 0;
        var constructionvalue = parseFloat($('#constructionvalue' + id).val());
        var landvalue = parseFloat($('#landvalue' + id).val());
        var reductionconstructionvalue = parseFloat($('#reductionconstructionvalue' + id).autoNumeric('get'));
        var reductionlandvalue = parseFloat($('#reductionlandvalue' + id).autoNumeric('get'));
        var totallandvalue = 0;
        var totalconstructionvalue = 0;
        $("input[id^='landvalue']").each(function(){
            totallandvalue = totallandvalue + parseFloat($(this).val());
        });
        $("input[id^='reductionlandvalue'],input[id*='reductionlandvalue']").each(function () {
            totallandvalue = totallandvalue + parseFloat($(this).autoNumeric('get'));
        });
        $("input[id^='constructionvalue']").each(function(){
            totalconstructionvalue = totalconstructionvalue + parseFloat($(this).val());
        });
        $("input[id^='reductionconstructionvalue'],input[id*='reductionconstructionvalue']").each(function () {
            totalconstructionvalue = totalconstructionvalue + parseFloat($(this).autoNumeric('get'));
        });
        var notaryfees = 0;
        var totalnotaryfees = 0;
        var vatconstruction = 0;
        var vatnotaryfees = 0;
        var totalvat = 0;
        var vatperc = parseFloat(@Model.SalesSettings.VatPercentage);
        var totalprice = 0;
        var units = @Model.Units.Where(Function(m) m.Base.Type.GroupId = 1).Count;

        $('.notaryfees').autoNumeric('init', { aSign: '€ ', pSign: 'p', aSep: '.', aDec: ',' });
        $('.totalnotaryfees').autoNumeric('init', { aSign: '€ ', pSign: 'p', aSep: '.', aDec: ',' });
        $('.vatconstruction').autoNumeric('init', { aSign: '€ ', pSign: 'p', aSep: '.', aDec: ',' });
        $('.vatnotaryfees').autoNumeric('init', { aSign: '€ ', pSign: 'p', aSep: '.', aDec: ',' });
        $('.totalvat').autoNumeric('init', { aSign: '€ ', pSign: 'p', aSep: '.', aDec: ',' });
        $('.totalprice').autoNumeric('init', { aSign: '€ ', pSign: 'p', aSep: '.', aDec: ',' });
        //indien onder registratie
        if('@Model.SalesSettings.MixedVatRegistration' == 'False'){
            var registrationperc = parseFloat(@Model.SalesSettings.RegistrationPercentage);
            var lowerregistrationperc = 6
            var registrationvalue = (landvalue + reductionlandvalue) / 100 * registrationperc;
            var totalregistrationvalue = totallandvalue / 100 * registrationperc;
            if ($('#enige_eigen_woning').prop('checked')) {

                totalregistrationvalue = totalregistrationvalue / registrationperc * lowerregistrationperc
                $('.enige_eigen_woning').autoNumeric('init', { aSign: '€ ', pSign: 'p', aSep: '.', aDec: ',' });
                $('.enige_eigen_woning').autoNumeric('set', - registrationvalue + totalregistrationvalue );
            }
            else {
                $('.enige_eigen_woning').autoNumeric('init', { aSign: '€ ', pSign: 'p', aSep: '.', aDec: ',' });
                $('.enige_eigen_woning').autoNumeric('set', 0);
            }

            $('.registrationvalue' + id).autoNumeric('init', { aSign: '€ ', pSign: 'p', aSep: '.', aDec: ',' });
            $('.registrationvalue' + id).autoNumeric('set', registrationvalue);
            $('.registrationtotalvalue').autoNumeric('init', { aSign: '€ ', pSign: 'p', aSep: '.', aDec: ',' });
            $('.registrationtotalvalue').autoNumeric('set', totalregistrationvalue);

            $.ajax({
                url: '/Projecten/CalculateNotaryFees',
data: { landvalue: totallandvalue, constructionvalue: totalconstructionvalue, mixedvatregistration: '@(Model.SalesSettings.MixedVatRegistration)' },
async: false,
                cache: false,
                traditional: true,
                type: 'GET',
success: function (result) {
                    notaryfees = parseFloat(result);
                    $('.notaryfees').autoNumeric('set', notaryfees);
                    totalnotaryfees = parseFloat(result) + parseFloat(@Model.SalesSettings.FixedCertificateCost) + parseFloat(@Model.SalesSettings.BaseCertificateCost) * units parseFloat(@Model.SalesSettings.ParcelCost) * units  + parseFloat(@Model.SalesSettings.MortageRegistrationCost);
                    $('.totalnotaryfees').autoNumeric('set', totalnotaryfees);
                },

            });
            vatconstruction = (constructionvalue + reductionconstructionvalue) / 100 * parseFloat(@Model.SalesSettings.VatPercentage);
            vatnotaryfees = parseFloat(notaryfees) / 100 * parseFloat(@Model.SalesSettings.VatPercentage);
            $('.vatconstruction' + id).autoNumeric('init', { aSign: '€ ', pSign: 'p', aSep: '.', aDec: ',' });
            $('.vatconstruction' + id).autoNumeric('set', vatconstruction);
            $('.vatnotaryfees').autoNumeric('set', vatnotaryfees);
            totalvat = (totalconstructionvalue / 100 * vatperc) + vatnotaryfees + (parseFloat(@Model.SalesSettings.FixedCertificateCost) / 100 * vatperc) + (parseFloat(@Model.SalesSettings.BaseCertificateCost) * units / 100 * vatperc) + (parseFloat(@Model.SalesSettings.ParcelCost) * units / 100 * vatperc) + (parseFloat(@Model.SalesSettings.ConnectionFees) * units / 100 * vatperc)
            $('.totalvat').autoNumeric('set', totalvat);
        };
        totalvalue = constructionvalue + landvalue + reductionconstructionvalue + reductionlandvalue;
        $('.totalvalue' + id).autoNumeric('init', { aSign: '€ ', pSign: 'p', aSep: '.', aDec: ',' });
        $('.totalvalue' + id).autoNumeric('set', totalvalue);
        @*alert(totalvalue);
        alert(totalvat);
        alert(totalnotaryfees);
        alert(totalregistrationvalue);
        alert(parseFloat(@Model.SalesSettings.ConnectionFees));*@
        totalprice = totallandvalue + totalconstructionvalue + totalvat + totalnotaryfees + totalregistrationvalue + parseFloat(@Model.SalesSettings.ConnectionFees) * units
        $('.totalprice').autoNumeric('set', totalprice);
    };

        jQuery(function ($) {
            $('.Currencymask').autoNumeric('init');  //autoNumeric with defaults

        });
</script>


