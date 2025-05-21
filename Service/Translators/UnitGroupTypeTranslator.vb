Imports BO
Imports DAL

Public Class UnitGroupTypeTranslator
    Public Shared Function TranslateEntityToBO(_entity As UnitGroupTypes, bo As UnitGroupTypeBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.ID
        bo.Name = _entity.Name
        If (_entity.UnitTypes IsNot Nothing) Then
            For Each Type In _entity.UnitTypes
                Dim UnitType As New UnitTypeBO
                Dim err = UnitTypeTranslator.TranslateEntityToBO(Type, UnitType)
                If (err <> ErrorCode.Success) Then Return err
                bo.UnitTypes.Add(UnitType)
            Next

        End If
        Return ErrorCode.Success
    End Function

    Friend Shared Function TranslateBOToEntity(_entity As UnitGroupTypes, bo As UnitGroupTypeBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Name = bo.Name
        Dim err = HandleTypes(_entity, bo.UnitTypes)
        If (err <> ErrorCode.Success) Then Return err
        Return ErrorCode.Success
    End Function
    Private Shared Function HandleTypes(_entity As UnitGroupTypes, types As List(Of UnitTypeBO)) As ErrorCode
        If (types.Count = 0) Then Return ErrorCode.Success
        For Each x In types
            If (x.Id = 0) Then
                'insert
                Dim type As New UnitTypes
                Dim err = UnitTypeTranslator.TranslateBOToEntity(type, x)
                If (err <> ErrorCode.Success) Then Return err
                _entity.UnitTypes.Add(type)
            Else
                'update
                Dim type = _entity.UnitTypes.FirstOrDefault(Function(f) f.ID = x.Id)
                If (type IsNot Nothing) Then
                    Dim err = UnitTypeTranslator.TranslateBOToEntity(type, x)
                    If (err <> ErrorCode.Success) Then Return err
                End If
            End If
        Next
        'delete
        Dim delList As New List(Of UnitTypes)
        For Each x In _entity.UnitTypes
            If (Not types.Any(Function(f) f.Id = x.ID)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            _entity.UnitTypes.Remove(x)
        Next
        Return ErrorCode.Success
    End Function
End Class
