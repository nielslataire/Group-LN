Imports System.Net.Http
Imports System.Runtime.Serialization.Json
Imports System.Net.Http.Headers

Public NotInheritable Class DataSource
    Private Shared _DataSource As New DataSource()

    'Private _contacts As New ObservableCollection(Of Contact)()
    'Public ReadOnly Property Contacts As ObservableCollection(Of Contact)
    '    Get
    '        Return Me._contacts
    '    End Get
    'End Property

    'Public Shared Async Function GetContactsAsync() As Task(Of IEnumerable(Of Contact))
    '    Await _DataSource.GetContactsDataAsync()
    '    Return _DataSource.Contacts
    'End Function

    Public Async Function GetContactsDataAsync() As Task(Of IEnumerable(Of Contact))
        Dim o As New List(Of Contact)
        'If Me._contacts.Count <> 0 Then
        'Return
        'End If
        Dim url As String = "http://api.copro-bouwteam.be/"
        Using client As New HttpClient()
            client.BaseAddress = New Uri(url)
            client.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("application/json"))
            Using response As HttpResponseMessage = Await client.GetAsync("api/contact")
                If response.IsSuccessStatusCode Then
                    Dim content As String = Await response.Content.ReadAsStringAsync()
                    o = DirectCast(Newtonsoft.Json.JsonConvert.DeserializeObject(content, GetType(List(Of Contact))), List(Of Contact))

                    'For Each c As Contact In o
                    '    Contacts.Add(c)
                    'Next
                End If
            End Using
        End Using

        Return o

    End Function
    Public Shared Async Function GetContactAsync(ByVal id As Integer) As Task(Of ContactDetail)
        Dim c As ContactDetail = Await _DataSource.GetContactDataAsync(id)
        Return c
    End Function

    Private Async Function GetContactDataAsync(ByVal id As Integer) As Task(Of ContactDetail)
        Dim o As New ContactDetail(0, "", "")
        Dim url As String = "http://api.copro-bouwteam.be/"
        Using client As New HttpClient()
            client.BaseAddress = New Uri(url)
            client.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("application/json"))
            Using response As HttpResponseMessage = Await client.GetAsync("api/contact/" & id)
                If response.IsSuccessStatusCode Then
                    Dim content As String = Await response.Content.ReadAsStringAsync()
                    o = DirectCast(Newtonsoft.Json.JsonConvert.DeserializeObject(content, GetType(ContactDetail)), ContactDetail)

                End If
            End Using
        End Using
        Return o
    End Function
    Public Shared Async Function DeleteContactAsync(ByVal id As Integer) As Task(Of Boolean)
        Dim url As String = "http://api.copro-bouwteam.be/"
        Using client As New HttpClient()
            client.BaseAddress = New Uri(url)
            client.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("application/json"))
            Using response As HttpResponseMessage = Await client.DeleteAsync("api/contact/" & id)

                If response.IsSuccessStatusCode Then
                    Return True
                Else
                    Return False
                End If
            End Using
        End Using

    End Function

    Public Shared Async Function GetCompanyAsync(ByVal id As Integer) As Task(Of CompanyDetail)
        Dim c As CompanyDetail = Await _DataSource.GetCompanyDataAsync(id)
        Return c
    End Function

    Private Async Function GetCompanyDataAsync(ByVal id As Integer) As Task(Of CompanyDetail)
        Dim o As CompanyDetail
        Dim url As String = "http://api.copro-bouwteam.be/"
        Using client As New HttpClient()
            client.BaseAddress = New Uri(url)
            client.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("application/json"))
            Using response As HttpResponseMessage = Await client.GetAsync("api/company/" & id)
                If response.IsSuccessStatusCode Then
                    Dim content As String = Await response.Content.ReadAsStringAsync()
                    o = DirectCast(Newtonsoft.Json.JsonConvert.DeserializeObject(content, GetType(CompanyDetail)), CompanyDetail)

                End If
            End Using
        End Using
        Return o
    End Function
   
    Public Shared Async Function DeleteCompanyAsync(ByVal id As Integer) As Task(Of Boolean)
        Dim url As String = "http://api.copro-bouwteam.be/"
        Using client As New HttpClient()
            client.BaseAddress = New Uri(url)
            client.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("application/json"))
            Using response As HttpResponseMessage = Await client.DeleteAsync("api/company/" & id)

                If response.IsSuccessStatusCode Then
                    Return True
                Else
                    Return False
                End If
            End Using
        End Using

    End Function
End Class
