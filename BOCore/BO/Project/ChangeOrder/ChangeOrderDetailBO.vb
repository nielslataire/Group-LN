Imports System.ComponentModel.DataAnnotations
Imports System.Globalization
Imports Common.Extensions ' <-- nodig voor GetDisplayName()

Public Class ChangeOrderDetailBO
    Public Sub New()
    End Sub

    ' ---------- Ruwe velden ----------
    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property

    Private _changeorderid As Integer
    Public Property ChangeOrderID() As Integer
        Get
            Return _changeorderid
        End Get
        Set(ByVal value As Integer)
            _changeorderid = value
        End Set
    End Property

    Private _description As String
    <Required(ErrorMessage:="Gelieve een detailomschrijving in te geven")>
    <Display(Name:="Omschrijving")>
    Public Property Description() As String
        Get
            Return _description
        End Get
        Set(ByVal value As String)
            _description = value
        End Set
    End Property

    Private _measurementtype As MeasurementType
    <Display(Name:="Meetmethode")>
    Public Property MeasurementType() As MeasurementType
        Get
            Return _measurementtype
        End Get
        Set(ByVal value As MeasurementType)
            _measurementtype = value
        End Set
    End Property

    Private _measurementUnit As MeasurementUnit
    <Display(Name:="Eenheid")>
    Public Property MeasurementUnit() As MeasurementUnit
        Get
            Return _measurementUnit
        End Get
        Set(ByVal value As MeasurementUnit)
            _measurementUnit = value
        End Set
    End Property

    Private _number As Integer
    <Required(ErrorMessage:="Gelieve een aantal in te geven")>
    <Display(Name:="Aantal")>
    Public Property Number() As Integer
        Get
            Return _number
        End Get
        Set(ByVal value As Integer)
            _number = value
        End Set
    End Property

    Private _price As Decimal
    <Required(ErrorMessage:="Gelieve een prijs in te geven")>
    <DisplayFormat(ApplyFormatInEditMode:=True, DataFormatString:="{0:C}")>
    <Display(Name:="Eenheidsprijs")>
    <UIHint("Currency")>
    Public Property Price() As Decimal
        Get
            Return _price
        End Get
        Set(ByVal value As Decimal)
            _price = value
        End Set
    End Property

    ' Let op: eigenschap heet "Commision" (zonder tweede s) — behouden voor compatibiliteit
    Private _commision As Decimal
    <Required(ErrorMessage:="Gelieve een commissie in te geven")>
    <Display(Name:="Perc. commissie")>
    <UIHint("Percentage")>
    Public Property Commision() As Decimal   ' opgeslagen als percentage, bv. 10 voor 10%
        Get
            Return _commision
        End Get
        Set(ByVal value As Decimal)
            _commision = value
        End Set
    End Property

    Private _invoicable As Boolean?
    <Display(Name:="Te Factureren")>
    Public Property Invoicable() As Boolean?
        Get
            Return _invoicable
        End Get
        Set(ByVal value As Boolean?)
            _invoicable = value
        End Set
    End Property

    Private _invoiced As Boolean?
    <Display(Name:="Gefactureerd")>
    Public Property Invoiced() As Boolean?
        Get
            Return _invoiced
        End Get
        Set(ByVal value As Boolean?)
            _invoiced = value
        End Set
    End Property

    ' ---------- Weergave/Helpers ----------
    Private Shared ReadOnly NlBe As CultureInfo = CultureInfo.GetCultureInfo("nl-BE")

    ' Enum display (vb. "F.H." / "V.H.")
    Public ReadOnly Property MeasurementTypeDisplay As String
        Get
            Return MeasurementType.GetDisplayName()
        End Get
    End Property

    Public ReadOnly Property MeasurementUnitDisplay As String
        Get
            Return MeasurementUnit.GetDisplayName()
        End Get
    End Property

    Public ReadOnly Property NumberFormatted As String
        Get
            Return Number.ToString("N0", NlBe)
        End Get
    End Property

    Public ReadOnly Property PriceFormatted As String
        Get
            Return Price.ToString("C", NlBe) ' EUR o.b.v. cultuur nl-BE
        End Get
    End Property

    ' Commision is in procenten (bv. 10) → toon "10 %"
    Public ReadOnly Property CommisionFormatted As String
        Get
            Return (Commision / CDec(100)).ToString("P0", NlBe)
        End Get
    End Property

    ' Fractie 0..1 indien je ermee wil rekenen
    Public ReadOnly Property CommisionFraction As Decimal
        Get
            Return Commision / CDec(100)
        End Get
    End Property

    ' Totaal incl. commissie
    Public ReadOnly Property Totaal() As Decimal
        Get
            Return (Number * Price) * (1D + (Commision / CDec(100)))
        End Get
    End Property

    Public ReadOnly Property TotaalFormatted As String
        Get
            Return Totaal.ToString("C", NlBe)
        End Get
    End Property
End Class
