@modeltype BO.UnitWithReductionBO 
@Using (Html.BeginCollectionItem("units"))

    @Html.HiddenFor(Function(m) m.Base.AttachedUnitsId)
    @Html.HiddenFor(Function(m) m.Base.BusNumber)
    @Html.HiddenFor(Function(m) m.Base.ClientAccountId)
    @Html.HiddenFor(Function(m) m.Base.ConstructionValue)
    @Html.HiddenFor(Function(m) m.Base.ConstructionValueSold)
    @Html.HiddenFor(Function(m) m.Base.HouseNumber)
    @Html.HiddenFor(Function(m) m.Base.Id)
    @Html.HiddenFor(Function(m) m.Base.IsLink)
    @Html.HiddenFor(Function(m) m.Base.Landshare)
    @Html.HiddenFor(Function(m) m.Base.LandValue)
    @Html.HiddenFor(Function(m) m.Base.LandValueSold)
    @Html.HiddenFor(Function(m) m.Base.Level)
    @Html.HiddenFor(Function(m) m.Base.LinkedUnitId)
    @Html.HiddenFor(Function(m) m.Base.Name)
    @Html.HiddenFor(Function(m) m.Base.PreKad)
    @Html.HiddenFor(Function(m) m.Base.ProjectId)
    @Html.HiddenFor(Function(m) m.Base.ProjectName)
    @Html.HiddenFor(Function(m) m.Base.Street)
    @Html.HiddenFor(Function(m) m.Base.Surface)
    @Html.HiddenFor(Function(m) m.Base.TotalValue)
    @Html.HiddenFor(Function(m) m.Base.Type.Id)
    @Html.HiddenFor(Function(m) m.ReductionLandValue)
    @Html.HiddenFor(Function(m) m.Base.Type.Id)
    @Html.HiddenFor(Function(m) m.Base.Type.GroupId)
    @Html.HiddenFor(Function(m) m.Base.Type.Name)
    @Html.HiddenFor(Function(m) m.Base.Type.Shortcode)

End Using


 


