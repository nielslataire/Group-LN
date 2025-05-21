Imports System.Web.Mvc
Imports BO

Namespace Controllers
    Public Class SharedController
        Inherits Controller

        ' GET: Shared
        Function Index() As ActionResult
            Return View()
        End Function
        <HttpPost>
        Public Function GetPostcodesByCountry(term As String, CountryId As Integer) As JsonResult
            Dim pservice = ServiceFactory.GetPostalcodeService()
            Dim presponse = pservice.GetPostalcodeByCountryAndSearchstring(CountryId, term)
            Dim iList As New List(Of PostalCodeBO)
            If (presponse.Success) Then iList = presponse.Values
            Dim PostalcodeList As New List(Of Select2DTO)()
            Dim singlePostalcode As Select2DTO
            For Each selectedPostalcode As PostalCodeBO In iList
                singlePostalcode = New Select2DTO()
                singlePostalcode.id = selectedPostalcode.PostcodeId
                singlePostalcode.text = selectedPostalcode.Postcode & " - " & selectedPostalcode.Gemeente
                PostalcodeList.Add(singlePostalcode)
            Next

            Return Json(PostalcodeList, JsonRequestBehavior.AllowGet)
        End Function
        <HttpPost>
        Function GetCountryIsoCode(countryid As Integer) As String
            Dim pservice = ServiceFactory.GetCountryService
            Dim presponse = pservice.GetCountryById(countryid)
            Dim iPostcode As New CountryBO
            If (presponse.Success) Then iPostcode = presponse.Values.FirstOrDefault
            Return iPostcode.ISOCode
        End Function
        <HttpPost>
        Public Function GetAvailableUnitsByProjectId(id As Integer) As JsonResult
            Dim pservice = ServiceFactory.GetUnitService()
            Dim presponse = pservice.GetAvailableUnitsByProjectId(id)
            Dim iList As New List(Of IdNameBO)
            If (presponse.Success) Then iList = presponse.Values
            Return Json(iList, JsonRequestBehavior.AllowGet)
        End Function

        Public Class Select2DTO
            ' as select2 is formed like id and text so we used DTO
            Public Property id() As Integer
                Get
                    Return m_id
                End Get
                Set(value As Integer)
                    m_id = value
                End Set
            End Property
            Private m_id As Integer
            Public Property text() As String
                Get
                    Return m_text
                End Get
                Set(value As String)
                    m_text = value
                End Set
            End Property
            Private m_text As String
        End Class

    End Class
End Namespace