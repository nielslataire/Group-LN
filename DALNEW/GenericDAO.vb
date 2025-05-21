Imports System.Linq
Imports System.Linq.Expressions

Public Class GenericDAO(Of t As Class)
    Implements IDisposable

    Private _dbSet As System.Data.Entity.DbSet(Of t)
    Private _context As testdbEntities
    Public Property Context() As testdbEntities
        Get
            Return _context
        End Get
        Set(ByVal value As testdbEntities)
            If (value IsNot Nothing) Then
                _context = value
                _dbSet = _context.Set(Of t)()
            End If
        End Set
    End Property

    Public Sub Dispose() Implements IDisposable.Dispose
        If (_context IsNot Nothing) Then
            _context.Dispose()
            _context = Nothing
        End If
    End Sub

    Public Function GetNormal() As IQueryable(Of t)
        Return _dbSet
    End Function

    Public Function GetNoTracking() As IQueryable(Of t)
        Return _dbSet.AsNoTracking()
    End Function
    Public Function GetNormal(filter As Expression(Of Func(Of t, Boolean))) As IQueryable(Of t)
        Dim query As IQueryable(Of t) = _dbSet
        If filter IsNot Nothing Then
            query = query.Where(filter)
        End If
        Return query
    End Function

    Public Function GetById(id As Integer) As t
        Return _dbSet.Find(id)
    End Function
    Public Function DeleteObject(t)
        Return _dbSet.Remove(t)
    End Function
    Public Function DeleteObject(id As Integer)
        Dim entity = _dbSet.Find(id)

        If (entity IsNot Nothing) Then

            Return _dbSet.Remove(entity)
        End If
        Return Nothing
    End Function


    Public Function GetNew() As t
        Dim retval = _dbSet.Create()
        _dbSet.Add(retval)
        Return retval
    End Function

End Class
