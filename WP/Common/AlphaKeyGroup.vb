
Imports System.Collections.Generic
Imports System.Globalization
Imports Windows.Globalization.Collation

Namespace Common
    Public Class AlphaKeyGroup(Of T)
        Inherits List(Of T)
        ''' <summary>
        ''' The delegate that is used to get the key information.
        ''' </summary>
        ''' <param name="item">An object of type T</param>
        ''' <returns>The key value to use for this object</returns>
        Public Delegate Function GetKeyDelegate(item As T) As String

        ''' <summary>
        ''' The Key of this group.
        ''' </summary>
        Public Property Key() As String
            Get
                Return m_Key
            End Get
            Private Set(value As String)
                m_Key = Value
            End Set
        End Property
        Private m_Key As String

        ''' <summary>
        ''' Public constructor.
        ''' </summary>
        ''' <param name="key">The key for this group.</param>
        Public Sub New(key__1 As String)
            Key = key__1
        End Sub

        ''' <summary>
        ''' Create a list of AlphaGroup<T> with keys set by a SortedLocaleGrouping.
        ''' </summary>
        ''' <param name="slg">The </param>
        ''' <returns>Theitems source for a LongListSelector</returns>
        Private Shared Function CreateGroups(slg As CharacterGroupings) As List(Of AlphaKeyGroup(Of T))
            Dim list As New List(Of AlphaKeyGroup(Of T))()

            For Each key As CharacterGrouping In slg
                If String.IsNullOrWhiteSpace(key.Label) = False Then
                    list.Add(New AlphaKeyGroup(Of T)(key.Label))
                End If
            Next

            Return list
        End Function

        ''' <summary>
        ''' Create a list of AlphaGroup<T> with keys set by a SortedLocaleGrouping.
        ''' </summary>
        ''' <param name="items">The items to place in the groups.</param>
        ''' <param name="ci">The CultureInfo to group and sort by.</param>
        ''' <param name="getKey">A delegate to get the key from an item.</param>
        ''' <param name="sort">Will sort the data if true.</param>
        ''' <returns>An items source for a LongListSelector</returns>
        Public Shared Function CreateGroups(items As IEnumerable(Of T), ci As CultureInfo, getKey As GetKeyDelegate, sort As Boolean) As List(Of AlphaKeyGroup(Of T))
            Dim slg As New CharacterGroupings()
            Dim list As List(Of AlphaKeyGroup(Of T)) = CreateGroups(slg)

            For Each item As T In items
                Dim index As String = ""
                Dim c As String = getKey(item)
                index = slg.Lookup(getKey(item))
                If String.IsNullOrEmpty(index) = False Then
                    list.Find(Function(a) a.Key = index).Add(item)
                End If
            Next

            If sort Then
                For Each group As AlphaKeyGroup(Of T) In list
                    group.Sort(Function(c0, c1)
                                                  Return ci.CompareInfo.Compare(getKey(c0), getKey(c1))

                                              End Function)
                Next
            End If

            Return list
        End Function

    End Class
End Namespace

'=======================================================
'Service provided by Telerik (www.telerik.com)
'Conversion powered by NRefactory.
'Twitter: @telerik
'Facebook: facebook.com/telerik
'=======================================================

