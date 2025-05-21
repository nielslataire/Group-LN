Imports CPM.Common
Imports System.Globalization
Imports System.Text.RegularExpressions
Imports Windows.UI
Imports Windows.UI.Xaml.Documents

' The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

''' <summary>
''' A basic page that provides characteristics common to most applications.
''' </summary>
Public NotInheritable Class ContactsPage
    Inherits Page

    Private WithEvents _navigationHelper As New NavigationHelper(Me)
    Private ReadOnly _defaultViewModel As New ObservableDictionary()

    Dim DataContacts As New List(Of Contact)
    'Dim currentAccentColor As SolidColorBrush = DirectCast(Application.Current.Resources("PhoneAccentBrush"), Brush)
    Dim currentAccentColor As SolidColorBrush = New SolidColorBrush(TryCast(App.Current.Resources("PhoneAccentBrush"), SolidColorBrush).Color)

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
        ' TODO: Load the saved state of the page here.
        If (DataContacts.Count = 0) Then
            Await ShowAllContacts()
        End If

    End Sub
    Public Async Function ShowAllContacts() As task
        showorhideProgress(True)
        Dim ds As New DataSource()
        DirectCast(Resources("AddressGroups"), CollectionViewSource).Source = Nothing
        DataContacts = Await ds.GetContactsDataAsync()
        Dim itemSource As List(Of AlphaKeyGroup(Of Contact)) = AlphaKeyGroup(Of Contact).CreateGroups(DataContacts, CultureInfo.CurrentUICulture, Function(s) s.Weergavenaam1, True)
        DirectCast(Resources("AddressGroups"), CollectionViewSource).Source = itemSource

        showorhideProgress(False)
    End Function
    Public Sub showorhideProgress(ByVal b As Boolean)
        If b = True Then
            prProgress.Visibility = Windows.UI.Xaml.Visibility.Visible
            prProgress.IsActive = b

        ElseIf b = False Then
            prProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed
            prProgress.IsActive = b
        End If
    End Sub

    ''' <summary>
    ''' Preserves state associated with this page in case the application is suspended or the
    ''' page is discarded from the navigation cache.  Values must conform to the serialization
    ''' requirements of <see cref="SuspensionManager.SessionState"/>.
    ''' </summary>
    ''' <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
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



    Private Async Sub btnRefresh_Click(sender As Object, e As RoutedEventArgs) Handles btnRefresh.Click
        'Me.DefaultViewModel.Clear()
        'DirectCast(Resources("AddressGroups"), CollectionViewSource).Source = Nothing
        Await ShowAllContacts()
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

    Private Sub btnSearch_Click(sender As Object, e As RoutedEventArgs) Handles btnSearch.Click
        stpHeader.Visibility = Windows.UI.Xaml.Visibility.Collapsed
        SearchGrid.Visibility = Windows.UI.Xaml.Visibility.Visible
        txtsearch.Focus(Windows.UI.Xaml.FocusState.Programmatic)

    End Sub

    'functies om de backbutton te behandelen
    Protected Overrides Sub OnNavigatingFrom(e As NavigatingCancelEventArgs)
        'remove the handler before you leave!
        RemoveHandler HardwareButtons.BackPressed, AddressOf HardwareButtons_BackPressed
    End Sub

    Public Sub New()
        Me.InitializeComponent()
        AddHandler Windows.Phone.UI.Input.HardwareButtons.BackPressed, AddressOf HardwareButtons_BackPressed
    End Sub

    'indien er op de backknop gedrukt wordt en het zoekveld is nog zichtbaar dan wordt het zoeken geannulleerd
    Private Sub HardwareButtons_BackPressed(sender As Object, e As Windows.Phone.UI.Input.BackPressedEventArgs)
        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)
        If rootFrame IsNot Nothing AndAlso rootFrame.CanGoBack AndAlso SearchGrid.Visibility = Windows.UI.Xaml.Visibility.Visible Then
            DirectCast(Resources("AddressGroups"), CollectionViewSource).Source = Nothing
            Dim itemSource As List(Of AlphaKeyGroup(Of Contact)) = AlphaKeyGroup(Of Contact).CreateGroups(DataContacts, CultureInfo.CurrentUICulture, Function(s) s.Weergavenaam1, True)
            DirectCast(Resources("AddressGroups"), CollectionViewSource).Source = itemSource
            e.Handled = True
            txtsearch.Text = ""
            stpHeader.Visibility = Windows.UI.Xaml.Visibility.Visible
            SearchGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed
            lsvSearch.Visibility = Xaml.Visibility.Collapsed
            lsvContacten.Visibility = Windows.UI.Xaml.Visibility.Visible
        End If

    End Sub

    'Indien tekst in zoekveld getikt wordt
    Private Sub txtsearch_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtsearch.TextChanged

        Dim textentered As String = txtsearch.Text.ToString().ToLower()
        If textentered = "" Then
            lsvContacten.Visibility = Windows.UI.Xaml.Visibility.Visible
            lsvSearch.Visibility = Windows.UI.Xaml.Visibility.Collapsed
        Else
            lsvContacten.Visibility = Windows.UI.Xaml.Visibility.Collapsed
            lsvSearch.Visibility = Windows.UI.Xaml.Visibility.Visible
        End If
        Dim filterd As List(Of Contact) = DataContacts.Where(Function(w) StartsWithExtension(w.Weergavenaam1.ToLower, textentered) Or _
                                                                  StartsWithExtension(w.Weergavenaam2.ToLower, textentered)).ToList()
        Dim itemSource As List(Of AlphaKeyGroup(Of Contact)) = AlphaKeyGroup(Of Contact).CreateGroups(filterd, CultureInfo.CurrentUICulture, Function(s) s.Weergavenaam1, True)

        DirectCast(Resources("AddressGroups"), CollectionViewSource).Source = itemSource

    End Sub

    'Functie om op alle woorden in bedrijfsnaam te zoeken

    Public Shared Function StartsWithExtension(value As String, toFind As String) As Boolean
        Return Regex.IsMatch(value, "(^|\s)" + Regex.Escape(toFind))
    End Function

    Private Sub ResultsText_Loaded(sender As Object, e As RoutedEventArgs)

    End Sub

    Public Shared Sub BoldText(ByRef tb As TextBlock, partToColor As String, color As Color)
        Dim Text As String = tb.Text
        tb.Inlines.Clear()
        For Each word As String In Text.Split(" ")
            If word.Length > 0 And word.Contains(partToColor) Then
                Dim r As New Run()
                r.Text = word.Substring(0, word.IndexOf(partToColor))
                tb.Inlines.Add(r)

                r = New Run()
                r.Text = partToColor
                r.Foreground = New SolidColorBrush(color)
                tb.Inlines.Add(r)

                r = New Run()
                r.Text = word.Substring(word.IndexOf(partToColor) + partToColor.Length, word.Length - (word.IndexOf(partToColor) + partToColor.Length))
                r.Text = r.Text & " "
                tb.Inlines.Add(r)
            Else
                Dim r As New Run()
                r.Text = word & " "
                tb.Inlines.Add(r)
            End If
        Next

    End Sub

    Private Sub TextBlock_Loaded(sender As Object, e As RoutedEventArgs)
        Dim textBlock = TryCast(sender, TextBlock)

        If txtsearch.Text.Length > 0 AndAlso textBlock.Text.Length > 0 AndAlso textBlock.Text.Contains(txtsearch.Text) Then
            BoldText(textBlock, txtsearch.Text, currentAccentColor.Color)
        End If
    End Sub


End Class
