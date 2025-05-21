Imports BO
Imports DAL

Public Class UnitTranslator
    Public Shared Function TranslateEntityToBO(_entity As Units, bo As UnitBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        bo.Id = _entity.Id
        bo.Name = _entity.Name
        bo.ProjectId = _entity.ProjectId
        bo.ProjectName = _entity.Project.ProjectName
        bo.Street = _entity.Street
        bo.HouseNumber = _entity.Housenumber
        bo.BusNumber = _entity.Busnumber
        bo.PreKad = _entity.PreKad
        bo.IsLink = _entity.IsLink
        bo.Plan = _entity.Plan
        If Not _entity.Surface Is Nothing Then bo.Surface = _entity.Surface
        If Not _entity.GroundSurface Is Nothing Then bo.GroundSurface = _entity.GroundSurface
        If Not _entity.Level Is Nothing Then bo.Level = _entity.Level
        If Not _entity.ClientAccount Is Nothing Then bo.ClientAccountId = _entity.ClientAccountID
        If Not _entity.ConstructionValueSold Is Nothing Then bo.ConstructionValueSold = _entity.ConstructionValueSold
        If Not _entity.LandValueSold Is Nothing Then bo.LandValueSold = _entity.LandValueSold
        If Not _entity.ConstructionValue Is Nothing Then bo.ConstructionValue = _entity.ConstructionValue
        If Not _entity.LandValue Is Nothing Then bo.LandValue = _entity.LandValue
        If Not _entity.AttachedUnit_attachedunit Is Nothing Then bo.AttachedUnitsId = _entity.AttachedUnit_attachedunit.Id
        If Not _entity.InvoicingPaymentGroup Is Nothing Then bo.PaymentGroupId = _entity.InvoicingPaymentGroup.Id
        If Not _entity.Landshare Is Nothing Then bo.Landshare = _entity.Landshare
        If (_entity.UnitTypes IsNot Nothing) Then
            Dim UnitType As New UnitTypeBO
            Dim err = UnitTypeTranslator.TranslateEntityToBO(_entity.UnitTypes, UnitType)
            If (err <> ErrorCode.Success) Then Return err
            bo.Type = UnitType

        End If

        For Each x In _entity.LinkedUnit_Unit
            Dim bou As New UnitBO
            Dim err = TranslateEntityToBO(x, bou)
            bo.LinkedUnits.Add(bou)
        Next
        bo.LinkedUnits = bo.LinkedUnits.OrderBy(Function(m) m.Name).ToList
        For Each x In _entity.UnitConstructionValue
            Dim bou As New UnitConstructionValueBO
            Dim err = UnitConstructionValueTranslator.TranslateEntityToBO(x, bou)
            bo.ConstructionValues.Add(bou)
        Next
        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As Units, bo As UnitBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.Name = bo.Name
        _entity.ProjectId = bo.ProjectId
        _entity.Landshare = bo.Landshare
        _entity.Street = bo.Street
        _entity.Housenumber = bo.HouseNumber
        _entity.Busnumber = bo.BusNumber
        _entity.PreKad = bo.PreKad
        _entity.ConstructionValueSold = bo.ConstructionValueSold
        _entity.LandValueSold = bo.LandValueSold
        _entity.ConstructionValue = bo.ConstructionValue
        _entity.LandValue = bo.LandValue
        _entity.IsLink = bo.IsLink
        _entity.Surface = bo.Surface
        _entity.GroundSurface = bo.GroundSurface
        _entity.Level = bo.Level
        _entity.Plan = bo.Plan
        '_entity.AttachedUnit_attachedunit.Id = bo.AttachedUnitsId
        If Not bo.ClientAccountId = 0 Then _entity.ClientAccountID = bo.ClientAccountId

        If (bo.Type IsNot Nothing) Then
            If bo.Type.Id <> 0 Then
                _entity.TypeId = bo.Type.Id
            End If
        End If

        'handle attached unit
        If (bo.AttachedUnitsId = 0 Or bo.AttachedUnitsId Is Nothing) Then
            'should never happen
            _entity.AttachedUnitId = Nothing
        Else
            'add the activity to the company
            If (_entity.AttachedUnit_attachedunit Is Nothing) Then
                Dim unit = uow.GetUnitsDAO().GetById(bo.AttachedUnitsId)
                _entity.AttachedUnit_attachedunit = unit
            ElseIf (Not _entity.AttachedUnit_attachedunit.Id = bo.AttachedUnitsId) Then
                Dim unit = uow.GetUnitsDAO().GetById(bo.AttachedUnitsId)
                _entity.AttachedUnit_attachedunit = unit
            End If
        End If
        'Handle Payment Group
        If (bo.PaymentGroupId = 0 Or bo.PaymentGroupId Is Nothing) Then
            'should never happen
            _entity.PaymentGroupId = Nothing
        Else
            'add paymentgroup to unit
            If (_entity.InvoicingPaymentGroup Is Nothing) Then
                Dim paymentgroup = uow.GetProjectPaymentGroupsDAO().GetById(bo.PaymentGroupId)
                _entity.InvoicingPaymentGroup = paymentgroup
            ElseIf (Not _entity.InvoicingPaymentGroup.Id = bo.PaymentGroupId) Then
                Dim paymentgroup = uow.GetProjectPaymentGroupsDAO().GetById(bo.PaymentGroupId)
                _entity.InvoicingPaymentGroup = paymentgroup
            End If
        End If
        Dim Err = HandleLinkedUnits(_entity, bo.LinkedUnits, uow)
        If (Err <> ErrorCode.Success) Then Return Err
        _entity.LinkedUnit_Unit = _entity.LinkedUnit_Unit.OrderBy(Function(m) m.Name).ToList
        If _entity.LinkedUnit_Unit.Count <> 0 Then
            _entity.Name = ""
            _entity.Landshare = _entity.LinkedUnit_Unit.Sum(Function(m) m.Landshare)
            _entity.TypeId = _entity.LinkedUnit_Unit(0).TypeId
            _entity.LevelId = _entity.LinkedUnit_Unit(0).LevelId
            For Each unit In _entity.LinkedUnit_Unit
                _entity.Name = _entity.Name & unit.Name
                If Not unit Is _entity.LinkedUnit_Unit.Last Then
                    _entity.Name = _entity.Name & " - "
                End If
            Next

        End If
        'Dim Err2 = HandleConstructionValues(_entity, bo.ConstructionValues, uow)
        'If (Err2 <> ErrorCode.Success) Then Return Err
        Return ErrorCode.Success
    End Function
    Private Shared Function HandleAttachedUnit(_entity As Units, unitid As Integer, uow As UnitOfWork) As ErrorCode
        'If (unitids Is Nothing) Then Return ErrorCode.Success
        'If (unitids.Count = 0) Then Return ErrorCode.Success

        If (unitid = 0) Then
            'should never happen
            _entity.AttachedUnit_attachedunit = Nothing
        Else
            'add the activity to the company
            If (_entity.AttachedUnit_attachedunit Is Nothing) Then
                Dim unit = uow.GetUnitsDAO().GetById(unitid)
                _entity.AttachedUnit_attachedunit = unit
            ElseIf (Not _entity.AttachedUnit_attachedunit.Id = unitid) Then
                Dim unit = uow.GetUnitsDAO().GetById(unitid)
                _entity.AttachedUnit_attachedunit = unit
            End If
        End If

        'delete
        'Dim delList As New List(Of Units)
        'For Each x In _entity.AttachedUnits_Attachedunit
        '    If (Not unitids.Any(Function(f) f = x.Id)) Then
        '        delList.Add(x)
        '    End If
        'Next
        'For Each x In delList
        '    _entity.AttachedUnits_Attachedunit.Remove(x)
        'Next
        Return ErrorCode.Success
    End Function
    Private Shared Function HandleLinkedUnits(_entity As Units, linkedunits As List(Of UnitBO), uow As UnitOfWork) As ErrorCode
        If (linkedunits Is Nothing) Then Return ErrorCode.Success
        'If (unitids.Count = 0) Then Return ErrorCode.Success
        For Each x In linkedunits
            If (x.Id = 0) Then
                'should never happen
            Else
                'add the activity to the company
                If (Not _entity.LinkedUnit_Unit.Any(Function(m) m.Id = x.Id)) Then
                    Dim unit = uow.GetUnitsDAO().GetById(x.Id)
                    _entity.LinkedUnit_Unit.Add(unit)
                End If
            End If
        Next
        'delete
        Dim delList As New List(Of Units)
        For Each x In _entity.LinkedUnit_Unit
            If (Not linkedunits.Any(Function(f) f.Id = x.Id)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            _entity.LinkedUnit_Unit.Remove(x)
        Next
        Return ErrorCode.Success
    End Function
    Private Shared Function HandleConstructionValues(_entity As Units, constructionvalues As List(Of UnitConstructionValueBO), uow As UnitOfWork) As ErrorCode
        If (constructionvalues Is Nothing) Then Return ErrorCode.Success
        'If (unitids.Count = 0) Then Return ErrorCode.Success
        For Each x In constructionvalues
            If (x.Id = 0) Then
                'should never happen
            Else
                'add the constructionvalue to the unit
                If (Not _entity.UnitConstructionValue.Any(Function(m) m.Id = x.Id)) Then
                    Dim constructionvalue = uow.GetUnitConstructionValuesDAO().GetById(x.Id)
                    _entity.UnitConstructionValue.Add(constructionvalue)
                End If
            End If
        Next
        'delete
        Dim delList As New List(Of UnitConstructionValue)
        For Each x In _entity.UnitConstructionValue
            If (Not constructionvalues.Any(Function(f) f.Id = x.Id)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            _entity.UnitConstructionValue.Remove(x)
        Next
        Return ErrorCode.Success
    End Function
End Class
