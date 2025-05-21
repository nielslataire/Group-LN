Imports CPM.Common
Imports CPM.Data
Imports System.Net.Http
Imports Newtonsoft.Json
Imports System.Net.Http.Headers

' The Hub Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641

Public NotInheritable Class ItemPage
    Inherits Page

    Private WithEvents _navigationHelper As New NavigationHelper(Me)
    Private ReadOnly _defaultViewModel As New ObservableDictionary

    Public Sub New()
        InitializeComponent()
    End Sub

    ''' <summary>
    ''' Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
    ''' </summary>
    Public ReadOnly Property NavigationHelper As NavigationHelper
        Get
            Return _navigationHelper
        End Get
    End Property

    ''' <summary>
    ''' Gets the view model for this <see cref="Page"/>.
    ''' This can be changed to a strongly typed view model.
    ''' </summary>
    Public ReadOnly Property DefaultViewModel As ObservableDictionary
        Get
            Return _defaultViewModel
        End Get
    End Property

    ''' <summary>
    ''' Populates the page with content passed during navigation. Any saved state is also
    ''' provided when recreating a page from a prior session.
    ''' </summary>
    ''' <param name="sender">
    ''' The source of the event; typically <see cref="NavigationHelper"/>.
    ''' </param>
    ''' <param name="e">Event data that provides both the navigation parameter passed to
    ''' <see cref="Frame.Navigate"/> when this page was initially requested and
    ''' a dictionary of state preserved by this page during an earlier.
    ''' session. The state will be null the first time a page is visited.</param>
    Private Async Sub NavigationHelper_LoadState(sender As Object, e As LoadStateEventArgs) Handles _navigationHelper.LoadState
        ' TODO: Create an appropriate data model for your problem domain to replace the sample data.
        Dim item As SampleDataItem = Await SampleDataSource.GetItemAsync(CStr(e.NavigationParameter))
        DefaultViewModel("Item") = item
        Dim contacts As List(Of Contact) = Await GetContacts()
        'Await ReadDataFromWeb()

    End Sub
    
    Public Async Function GetContacts() As Task(Of List(Of Contact))
        Dim o As New List(Of Contact)
        Dim url As String = "http://91.183.42.214:8080/"
        Using client As New HttpClient()
            client.BaseAddress = New Uri(url)
            client.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("application/json"))
            Using response As HttpResponseMessage = Await client.GetAsync("api/contact")
                If response.IsSuccessStatusCode Then
                    Dim content As String = Await response.Content.ReadAsStringAsync()
                    o = DirectCast(Newtonsoft.Json.JsonConvert.DeserializeObject(content, GetType(List(Of Contact))), List(Of Contact))
                End If
            End Using
        End Using
        Return o
    End Function
    Private Async Function ReadDataFromWeb() As Task
        Dim handler As New HttpClientHandler() With {.UseDefaultCredentials = True}
        Dim client As New HttpClient(handler)
        client.BaseAddress = New Uri("http://localhost:52691/")
        client.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("application/json"))
        Dim response As HttpResponseMessage = client.GetAsync("api/contact").Result
    End Function


    ''' <summary>
    ''' Preserves state associated with this page in case the application is suspended or the
    ''' page is discarded from the navigation cache.  Values must conform to the serialization
    ''' requirements of <see cref="SuspensionManager.SessionState"/>.
    ''' </summary>
    ''' <param name="sender">The source of the event; typically <see cref="NavigationHelper"/>.</param>
    ''' <param name="e">Event data that provides an empty dictionary to be populated with
    ''' serializable state.</param>
    Private Sub NavigationHelper_SaveState(sender As Object, e As SaveStateEventArgs) Handles _navigationHelper.SaveState
        ' TODO: Save the unique state of the page here.
    End Sub

#Region "NavigationHelper registration"

    ''' <summary>
    ''' The methods provided in this section are simply used to allow
    ''' NavigationHelper to respond to the page's navigation methods.
    ''' <para>
    ''' Page specific logic should be placed in event handlers for the
    ''' <see cref="NavigationHelper.LoadState"/>
    ''' and <see cref="NavigationHelper.SaveState"/>.
    ''' The navigation parameter is available in the LoadState method
    ''' in addition to page state preserved during an earlier session.
    ''' </para>
    ''' </summary>
    ''' <param name="e">Event data that describes how this page was reached.</param>
    Protected Overrides Sub OnNavigatedTo(e As NavigationEventArgs)
        _navigationHelper.OnNavigatedTo(e)
    End Sub

    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)
        _navigationHelper.OnNavigatedFrom(e)
    End Sub

#End Region

End Class
