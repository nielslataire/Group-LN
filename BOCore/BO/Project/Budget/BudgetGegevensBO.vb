Imports System.ComponentModel.DataAnnotations

Public Class BudgetGegevensBO
    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property

    Private _budgetVersieId As Integer
    Public Property BudgetVersieId() As Integer
        Get
            Return _budgetVersieId
        End Get
        Set(ByVal value As Integer)
            _budgetVersieId = value
        End Set
    End Property

    Private _naam As String
    <Display(Name:="Naam")>
    Public Property Naam() As String
        Get
            Return _naam
        End Get
        Set(ByVal value As String)
            _naam = value
        End Set
    End Property

    Private _adres As String
    <Display(Name:="Adres")>
    Public Property Adres() As String
        Get
            Return _adres
        End Get
        Set(ByVal value As String)
            _adres = value
        End Set
    End Property

    Private _bouwheerCompanyId As Integer?
    Public Property BouwheerCompanyId() As Integer?
        Get
            Return _bouwheerCompanyId
        End Get
        Set(ByVal value As Integer?)
            _bouwheerCompanyId = value
        End Set
    End Property

    Private _aantalLiften As Integer
    <Display(Name:="Aantal liften")>
    Public Property AantalLiften() As Integer
        Get
            Return _aantalLiften
        End Get
        Set(ByVal value As Integer)
            _aantalLiften = value
        End Set
    End Property

    Private _aantalBinnentrappen As Integer
    <Display(Name:="Aantal binnentrappen")>
    Public Property AantalBinnentrappen() As Integer
        Get
            Return _aantalBinnentrappen
        End Get
        Set(ByVal value As Integer)
            _aantalBinnentrappen = value
        End Set
    End Property

    Private _aantalBovengrondseVerdiepingen As Integer
    <Display(Name:="Bovengrondse verdiepingen")>
    Public Property AantalBovengrondseVerdiepingen() As Integer
        Get
            Return _aantalBovengrondseVerdiepingen
        End Get
        Set(ByVal value As Integer)
            _aantalBovengrondseVerdiepingen = value
        End Set
    End Property

    Private _aantalVerdiepingenOndergronds As Integer
    <Display(Name:="Ondergrondse verdiepingen")>
    Public Property AantalVerdiepingenOndergronds() As Integer
        Get
            Return _aantalVerdiepingenOndergronds
        End Get
        Set(ByVal value As Integer)
            _aantalVerdiepingenOndergronds = value
        End Set
    End Property

    Private _typePoorten As String
    <Display(Name:="Type poorten")>
    Public Property TypePoorten() As String
        Get
            Return _typePoorten
        End Get
        Set(ByVal value As String)
            _typePoorten = value
        End Set
    End Property

    Private _typeDak As String
    <Display(Name:="Type dak")>
    Public Property TypeDak() As String
        Get
            Return _typeDak
        End Get
        Set(ByVal value As String)
            _typeDak = value
        End Set
    End Property

    Private _gevelLeienSidings As Integer
    <Display(Name:="Gevel leien/sidings")>
    Public Property GevelLeienSidings() As Integer
        Get
            Return _gevelLeienSidings
        End Get
        Set(ByVal value As Integer)
            _gevelLeienSidings = value
        End Set
    End Property

    Private _oppFunderingen As Decimal?
    <Display(Name:="Oppervlakte funderingen (m²)")>
    Public Property OppFunderingen() As Decimal?
        Get
            Return _oppFunderingen
        End Get
        Set(ByVal value As Decimal?)
            _oppFunderingen = value
        End Set
    End Property

    Private _m3Grondwerk As Decimal?
    <Display(Name:="Grondwerk (m³)")>
    Public Property M3Grondwerk() As Decimal?
        Get
            Return _m3Grondwerk
        End Get
        Set(ByVal value As Decimal?)
            _m3Grondwerk = value
        End Set
    End Property

    Private _lmBerlinerwanden As Decimal?
    <Display(Name:="Berlinerwanden (lm)")>
    Public Property LmBerlinerwanden() As Decimal?
        Get
            Return _lmBerlinerwanden
        End Get
        Set(ByVal value As Decimal?)
            _lmBerlinerwanden = value
        End Set
    End Property

    Private _lmSecanpalen As Decimal?
    <Display(Name:="Secanpalen (lm)")>
    Public Property LmSecanpalen() As Decimal?
        Get
            Return _lmSecanpalen
        End Get
        Set(ByVal value As Decimal?)
            _lmSecanpalen = value
        End Set
    End Property

    Private _nacalcBasisprijs As Decimal?
    <Display(Name:="Nacalc basisprijs (€/m²)")>
    Public Property NacalcBasisprijs() As Decimal?
        Get
            Return _nacalcBasisprijs
        End Get
        Set(ByVal value As Decimal?)
            _nacalcBasisprijs = value
        End Set
    End Property

    Private _nacalcBasisJaar As Integer?
    <Display(Name:="Nacalc basisjaar")>
    Public Property NacalcBasisJaar() As Integer?
        Get
            Return _nacalcBasisJaar
        End Get
        Set(ByVal value As Integer?)
            _nacalcBasisJaar = value
        End Set
    End Property

    Private _sIndexStart As Decimal?
    <Display(Name:="S-index bij opmaak nacalc (lonen)")>
    Public Property SIndexStart() As Decimal?
        Get
            Return _sIndexStart
        End Get
        Set(ByVal value As Decimal?)
            _sIndexStart = value
        End Set
    End Property

    Private _sIndexHuidig As Decimal?
    <Display(Name:="S-index huidig (lonen)")>
    Public Property SIndexHuidig() As Decimal?
        Get
            Return _sIndexHuidig
        End Get
        Set(ByVal value As Decimal?)
            _sIndexHuidig = value
        End Set
    End Property

    Private _iIndexStart As Decimal?
    <Display(Name:="I-index bij opmaak nacalc (materialen)")>
    Public Property IIndexStart() As Decimal?
        Get
            Return _iIndexStart
        End Get
        Set(ByVal value As Decimal?)
            _iIndexStart = value
        End Set
    End Property

    Private _iIndexHuidig As Decimal?
    <Display(Name:="I-index huidig (materialen)")>
    Public Property IIndexHuidig() As Decimal?
        Get
            Return _iIndexHuidig
        End Get
        Set(ByVal value As Decimal?)
            _iIndexHuidig = value
        End Set
    End Property

    Private _gevelMetselwerkPrijsPerM2 As Decimal?
    <Display(Name:="Gevelmetselwerk (€/m²)")>
    Public Property GevelMetselwerkPrijsPerM2() As Decimal?
        Get
            Return _gevelMetselwerkPrijsPerM2
        End Get
        Set(ByVal value As Decimal?)
            _gevelMetselwerkPrijsPerM2 = value
        End Set
    End Property

    Private _gipswerkenPrijsPerM2 As Decimal?
    <Display(Name:="Gipswerken (€/m²)")>
    Public Property GipswerkenPrijsPerM2() As Decimal?
        Get
            Return _gipswerkenPrijsPerM2
        End Get
        Set(ByVal value As Decimal?)
            _gipswerkenPrijsPerM2 = value
        End Set
    End Property

    Private _updatedAt As DateTime?
    Public Property UpdatedAt() As DateTime?
        Get
            Return _updatedAt
        End Get
        Set(ByVal value As DateTime?)
            _updatedAt = value
        End Set
    End Property

    Public ReadOnly Property GewogenIndexFactor() As Decimal
        Get
            Dim sF As Decimal = If(_sIndexStart.HasValue AndAlso _sIndexStart.Value > 0 AndAlso _sIndexHuidig.HasValue,
                                   _sIndexHuidig.Value / _sIndexStart.Value, 1D)
            Dim inF As Decimal = If(_iIndexStart.HasValue AndAlso _iIndexStart.Value > 0 AndAlso _iIndexHuidig.HasValue,
                                   _iIndexHuidig.Value / _iIndexStart.Value, 1D)
            Return inF * 0.4D + sF * 0.4D + 0.2D
        End Get
    End Property
End Class
