Imports CPM.Common
Imports CPM.Data
Imports System.Net.Http
Imports Newtonsoft.Json
Imports System.Net.Http.Headers
Imports Windows.ApplicationModel


' The Hub Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641

Public NotInheritable Class CompanyDetailPage
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
        Dim item As CompanyDetail = Await DataSource.GetCompanyAsync(CStr(e.NavigationParameter))
        DefaultViewModel("Detail") = item
        DefaultViewModel("Contacts") = item.Contacts
        'Dim stpTelefoon1 As StackPanel = FindChildControl(Of StackPanel)(HubPagina.Sections(0), "stpTelefoon1")
        'Dim stpTelefoon2 As StackPanel = FindChildControl(Of StackPanel)(HubPagina.Sections(0), "stpTelefoon2")
        'Dim stpGSM As StackPanel = FindChildControl(Of StackPanel)(HubPagina.Sections(0), "stpGSM")
        'Dim stpSMS As StackPanel = FindChildControl(Of StackPanel)(HubPagina.Sections(0), "stpSMS")
        'Dim stpMail As StackPanel = FindChildControl(Of StackPanel)(HubPagina.Sections(0), "stpMail")
       

        If Not item.Telefoon1 = "" Then
            stpTelefoon1.Visibility = Windows.UI.Xaml.Visibility.Visible
        End If
        If Not item.Telefoon2 = "" Then
            stpTelefoon2.Visibility = Windows.UI.Xaml.Visibility.Visible
        End If
        If Not item.GSM = "" Then
            stpGSM.Visibility = Windows.UI.Xaml.Visibility.Visible
            stpSMS.Visibility = Windows.UI.Xaml.Visibility.Visible
        End If
        If Not item.Email = "" Then
            stpMail.Visibility = Windows.UI.Xaml.Visibility.Visible
        End If

    End Sub

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




    Private Sub stpTelefoon1_Tapped(sender As Object, e As TappedRoutedEventArgs)

        Windows.ApplicationModel.Calls.PhoneCallManager.ShowPhoneCallUI(txtTelefoon1.Text, txtNaam.Text)
    End Sub

    Private Sub stpTelefoon2_Tapped(sender As Object, e As TappedRoutedEventArgs)

        Windows.ApplicationModel.Calls.PhoneCallManager.ShowPhoneCallUI(txtTelefoon2.Text, txtNaam.Text)
    End Sub

    Private Sub stpGSM_Tapped(sender As Object, e As TappedRoutedEventArgs)

        Windows.ApplicationModel.Calls.PhoneCallManager.ShowPhoneCallUI(txtGSM.Text, txtNaam.Text)
    End Sub

    Private Async Sub stpSMS_Tapped(sender As Object, e As TappedRoutedEventArgs)

        Dim msg As New Chat.ChatMessage
        msg.Body = ""
        msg.Recipients.Add(txtGSM.Text)
        Await Chat.ChatMessageManager.ShowComposeSmsMessageAsync(msg)
    End Sub

    Private Async Sub stpMail_Tapped(sender As Object, e As TappedRoutedEventArgs)

        Dim mail As New Email.EmailMessage
        mail.Subject = ""
        mail.Body = ""
        mail.To.Add(New Email.EmailRecipient(txtMail.Text, txtNaam.Text))
        Await Email.EmailManager.ShowComposeNewEmailAsync(mail)
    End Sub

    Private Sub ListView_ItemClick(sender As Object, e As ItemClickEventArgs)
        Dim itemId As String = DirectCast(e.ClickedItem, Contact).ContactId
        Dim companyId As String = DirectCast(e.ClickedItem, Contact).CompanyId
        If itemId = 0 Then
            If Not companyId = 0 Then
                If Not Frame.Navigate(GetType(CompanyDetailPage), companyId) Then
                    Dim resources As ResourceLoader = ResourceLoader.GetForCurrentView("Resources")
                    Throw New Exception(resources.GetString("NavigationFailedExceptionMessage"))
                End If
            End If

        Else
            If Not Frame.Navigate(GetType(ContactDetailPage), itemId) Then
                Dim resources As ResourceLoader = ResourceLoader.GetForCurrentView("Resources")
                Throw New Exception(resources.GetString("NavigationFailedExceptionMessage"))
            End If
        End If
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As RoutedEventArgs) Handles btnDelete.Click

    End Sub

    Private Sub btnEdit_Click(sender As Object, e As RoutedEventArgs) Handles btnEdit.Click

    End Sub

    Private Async Sub DeleteConfirmation_Click(sender As Object, e As RoutedEventArgs)
        Dim bool As Boolean = Await DataSource.DeleteCompanyAsync(txtCompanyId.Text.ToString)
        If bool = True Then
            NavigationHelper.GoBack()
        End If
    End Sub

    Private Sub DeleteAnnulation_Click(sender As Object, e As RoutedEventArgs)
        btnDelete.Flyout.Hide()
    End Sub
End Class
