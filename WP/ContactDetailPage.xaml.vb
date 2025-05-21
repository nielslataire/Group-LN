Imports CPM.Common
Imports CPM.Data
Imports System.Net.Http
Imports Newtonsoft.Json
Imports System.Net.Http.Headers
Imports Windows.ApplicationModel

' The Hub Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641

Public NotInheritable Class ContactDetailPage
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
        Dim item As ContactDetail = Await DataSource.GetContactAsync(CStr(e.NavigationParameter))
        DefaultViewModel("Detail") = item
        If Not item.Telefoon = "" Then
            stpTelefoon.Visibility = Windows.UI.Xaml.Visibility.Visible
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

    Private Sub stpTelefoon_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles stpTelefoon.Tapped
        Windows.ApplicationModel.Calls.PhoneCallManager.ShowPhoneCallUI(txtTelefoon.Text, txtNaam.Text)
    End Sub

    Private Sub stpGSM_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles stpGSM.Tapped
        Windows.ApplicationModel.Calls.PhoneCallManager.ShowPhoneCallUI(txtGSM.Text, txtNaam.Text)
    End Sub

    Private Async Sub stpSMS_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles stpSMS.Tapped
        Dim msg As New Chat.ChatMessage
        msg.Body = ""
        msg.Recipients.Add(txtGSM.Text)
        Await Chat.ChatMessageManager.ShowComposeSmsMessageAsync(msg)       
    End Sub

    Private Async Sub stpMail_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles stpMail.Tapped
        Dim mail As New Email.EmailMessage
        mail.Subject = ""
        mail.Body = ""
        mail.To.Add(New Email.EmailRecipient(txtMail.Text, txtNaam.Text))
        Await Email.EmailManager.ShowComposeNewEmailAsync(mail)
    End Sub

    Private Async Sub DeleteConfirmation_Click(sender As Object, e As RoutedEventArgs)
        Dim bool As Boolean = Await DataSource.DeleteContactAsync(txtContactId.Text.ToString)
        If bool = True Then
            NavigationHelper.GoBack()
        End If

    End Sub

    Private Sub DeleteAnnulation_Click(sender As Object, e As RoutedEventArgs)
        btnDelete.Flyout.Hide()
    End Sub
End Class
