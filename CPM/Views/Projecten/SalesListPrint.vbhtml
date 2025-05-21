@imports bo
@ModelType ProjectSalesExportModel
@Code
    Layout = Nothing
End Code

<!DOCTYPE html>

<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Verkooplijst @Model.ProjectName</title>

    <!-- Web Fonts  -->
    <link href="//fonts.googleapis.com/css?family=Open+Sans:300,400,600,700,800" rel="stylesheet" type="text/css">

    <!-- Vendor CSS -->
    <link rel="stylesheet" href="~/vendor/bootstrap/css/bootstrap.css" />

    <!-- Invoice Print Style -->
    <link rel="stylesheet" href="~/Content/Admin/invoice-print.css" />
</head>
<body style="font-size:10px;line-height:1;">
    <div class="invoice">
        <div class="col-sm-12 mt-md">
            <section id="pnlList">
                <div>
                    <div>
                        <table>
                            <tr>
                                <td><img src="~/img/logoimg.jpg" width="50" height="50"></td>
                                <td style="font-size:16px;font-weight:700;">@Model.ProjectName - Eenheden te koop</td>
                            </tr>
                        </table>
                    </div>
                </div>
                <hr />
                <div>
                    <div>
                        <table class="table table-bordered mb-none" style="border-color:black;">
                            <thead>
                                <tr>

                                    <th width="20%">Naam</th>
                                    <th width="7%">Verdieping</th>
                                    <th width="7%" Class="text-right">Verkoopprijs</th>
                                    <th width="7%" Class="text-right">Grondwaarde</th>
                                    <th width="7%" Class="text-right">Bouwwaarde</th>
                                    <th width="4%" Class="text-right">Tienduiz.</th>
                                    <th Class="text-right">m² Eenheid</th>
                                    @For Each type In Model.SurfaceTypes
                                        If type = RoomType.Slaapkamer Then
                                            @<text>
                                                <th># @type</th>
                                            </text>
                                        Else
                                            @<text>
                                                <th>m&sup2; @type</th>
                                            </text>
                                        End If

                                    Next
                                    <th width="15%">Predkad</th>
                                </tr>
                            </thead>
                            <tbody>
                                @For Each Group In Model.UnitsGrouped

                                    @<text>
                                        <tr class="primary">
                                            <td colspan="@(Model.SurfaceTypes.Count + 8)" class="text-weight-bold">@Group.Units(0).Unit.Type.Name </td>
                                        </tr>



                                    </text>

                                    @code Dim Levels = Group.Units.GroupBy(Function(m) m.Unit.Level)

                                    End Code
                                        For Each Level In Levels
                                            For Each item In Level

                                                @<text>
                                                    <tr class="text-sm m-sm">
                                                        <td>@If item.Unit.Type.Id <> 11 Then @item.Unit.Type.Name End If @item.Unit.Name</td>
                                                        <td>@If item.Unit.Level = 0 Then @<text>Gelijkvloers</text> Else @<text>Verdieping @item.Unit.Level</text>    End If</td>
                                                        <td Class="text-right">@Html.DisplayFor(Function(m) item.Unit.TotalValue)</td>
                                                        <td class="text-right">@Html.DisplayFor(Function(m) item.Unit.LandValue)</td>
                                                        <td class="text-right">@Html.DisplayFor(Function(m) item.Unit.ConstructionValue)</td>
                                                        <td class="text-right">@Html.DisplayFor(Function(m) item.Unit.Landshare)</td>
                                                        <td class="text-right">@if Not item.Unit.Surface = 0 Then @<text> @Html.DisplayFor(Function(m) item.Unit.Surface) m²</text> End If</td>
                                                        @For Each type In Model.SurfaceTypes
                                                            If type = RoomType.Slaapkamer Then
                                                                @<text>
                                                                    <td>
                                                                        @for each room In item.Unit.Rooms.Where(Function(m) m.Type = type)
                                                                            @room.Number
                                                                        Next
                                                                    </td>

                                                                </text>
                                                            Else
                                                                @<text>
                                                                    <td>
                                                                        @for each room In item.Unit.Rooms.Where(Function(m) m.Type = type)
                                                                            @<text>
                                                                                @room.Surface m²
                                                                            </text>
                                                                        Next
                                                                    </td>

                                                                </text>
                                                            End If

                                                        Next
                                                        @If item.Unit.LinkedUnits.Count > 0 Then
                                                            @<text>
                                                                <td>
                                                                    @for each lu In item.Unit.LinkedUnits
                                                                        @lu.PreKad
                                                                        @If Not lu Is item.Unit.LinkedUnits.Last Then
                                                                            @Html.Raw(" - ")
                                                                        End If
                                                                    Next

                                                                </td>
                                                            </text>
                                                        Else
                                                            @<text>
                                                                <td>@Html.DisplayFor(Function(m) item.Unit.PreKad)</td>
                                                            </text>
                                                        End If
                                                    </tr>
                                                </text>
                                                For Each attachedunit In item.AttachedUnits
                                                    @<text>
                                                        <tr>
                                                            <td style="padding-left:30px;">@If attachedunit.Type.Id <> 11 Then @attachedunit.Type.Name End If @attachedunit.Name</td>
                                                            <td>@If attachedunit.Level = 0 Then @<text>Gelijkvloers</text> Else @<text>Verdieping @attachedunit.Level</text>    End If</td>
                                                            <td class="text-right">@Html.DisplayFor(Function(m) attachedunit.TotalValue)</td>
                                                            <td class="text-right">@Html.DisplayFor(Function(m) attachedunit.LandValue)</td>
                                                            <td class="text-right">@Html.DisplayFor(Function(m) attachedunit.ConstructionValue)</td>
                                                            <td class="text-right">@Html.DisplayFor(Function(m) attachedunit.Landshare)</td>
                                                            <td class="text-right">@If Not attachedunit.Surface = 0 Then @<text> @Html.DisplayFor(Function(m) attachedunit.Surface) m²</text> End If</td>
                                                            @For Each type In Model.SurfaceTypes
                                                                If type = RoomType.Slaapkamer Then
                                                                    @<text>
                                                                        <td>
                                                                            @for each room In attachedunit.Rooms.Where(Function(m) m.Type = type)
                                                                                @room.Number    Next
                                                                        </td>

                                                                    </text>
                                                                Else
                                                                    @<text>
                                                                        <td>
                                                                            @for each room In attachedunit.Rooms.Where(Function(m) m.Type = type)
                                                                                @<text>
                                                                                    @room.Surface m²
                                                                                </text>
                                                                            Next
                                                                        </td>

                                                                    </text>
                                                                End If

                                                            Next
                                                            @If attachedunit.LinkedUnits.Count > 0 Then
                                                                @<text>
                                                                    <td>
                                                                        @for each lu In attachedunit.LinkedUnits
                                                                            @lu.PreKad
                                                                            @If Not lu Is attachedunit.LinkedUnits.Last Then
                                                                                @Html.Raw(" - ")
                                                                            End If
                                                                        Next

                                                                    </td>
                                                                </text>
                                                            Else
                                                                @<text>
                                                                    <td>@Html.DisplayFor(Function(m) attachedunit.PreKad)</td>
                                                                </text>
                                                            End If
                                                        </tr>
                                                    </text>
                                                    Next





                                                Next


                                            Next
                                        Next

                            </tbody>
                        </table>
                    </div>
                </div>
            </section>

        </div>
    </div>
    <script>
            window.print();
    </script>
</body>
</html>