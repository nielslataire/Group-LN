Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class InvoiceTranslator
    Friend Shared Function TranslateEntityToBO(_entity As Invoices, bo As InvoiceBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull

        bo.Filename = _entity.Filename
        bo.Invoicedate = _entity.Date
        bo.Id = _entity.Id
        bo.ClientId = _entity.ClientId
        bo.ClientType = _entity.ClientType
        bo.PublicId = _entity.PublicId
        If Not _entity.ExpirationDate Is Nothing Then bo.ExpirationDate = _entity.ExpirationDate
        bo.VatNumber = _entity.VatNumber
        bo.ClientName = _entity.ClientName
        bo.Adress = _entity.Adress
        'Gemeente en postcode van het project
        If (_entity.PostalCode IsNot Nothing) Then
            bo.PostalCode.Postcode = _entity.PostalCode.Postcode
            bo.PostalCode.Gemeente = _entity.PostalCode.Gemeente
            bo.PostalCode.PostcodeId = _entity.PostalCode.PostcodeID
            If _entity.PostalCode.Country IsNot Nothing Then
                bo.PostalCode.Country.Name = _entity.PostalCode.Country.LandNaam
                bo.PostalCode.Country.CountryID = _entity.PostalCode.Country.ID
                bo.PostalCode.Country.ISOCode = _entity.PostalCode.Country.LandISOCode
            End If
            If _entity.PostalCode.Provincie IsNot Nothing Then
                bo.PostalCode.Provincie.Name = _entity.PostalCode.Provincie.ProvincieName
                bo.PostalCode.Provincie.ProvincieId = _entity.PostalCode.Provincie.ProvincieID
            End If
        End If
        bo.BankAccount = _entity.BankAccount
        bo.ExtraInfo = _entity.ExtraInfo
        bo.Text = _entity.Text
        Dim stringlist As String() = _entity.DetailText.Split(New String() {"\n"}, StringSplitOptions.None)
        For Each line In stringlist
            If Not line = stringlist.Last() Then
                bo.DetailText &= line & "<br />"
            Else
                bo.DetailText &= line
            End If

        Next
        For Each x In _entity.InvoicesDetails
            Dim bou As New InvoiceRowBO
            Dim err = InvoiceDetailTranslator.TranslateEntityToBO(x, bou)
            bo.Rows.Add(bou)
        Next
        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As Invoices, bo As InvoiceBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull

        _entity.Filename = bo.Filename
        _entity.Date = bo.Invoicedate
        _entity.ClientId = bo.ClientId
        _entity.ClientType = bo.ClientType
        _entity.PublicId = bo.PublicId
        _entity.ExpirationDate = bo.ExpirationDate
        _entity.VatNumber = bo.VatNumber
        _entity.ClientName = bo.ClientName
        _entity.Adress = bo.Adress
        If Not bo.PostalCode Is Nothing Then _entity.PostalCodeID = bo.PostalCode.PostcodeId
        _entity.BankAccount = bo.BankAccount
        _entity.ExtraInfo = bo.ExtraInfo
        _entity.Text = bo.Text
        _entity.DetailText = bo.DetailText

        Dim err = HandleRows(_entity, bo.Rows)
        If (err <> ErrorCode.Success) Then Return err
        Return ErrorCode.Success
    End Function

    Private Shared Function HandleRows(_entity As Invoices, rows As List(Of InvoiceRowBO)) As ErrorCode
        If (rows.Count = 0) Then Return ErrorCode.Success
        For Each x In rows
            If (x.Id = 0) Then
                'insert
                Dim row As New InvoicesDetails
                Dim err = InvoiceDetailTranslator.TranslateBOToEntity(row, x)
                _entity.InvoicesDetails.Add(row)
            Else
                'update
                Dim row = _entity.InvoicesDetails.FirstOrDefault(Function(f) f.Id = x.Id)
                If (row IsNot Nothing) Then
                    Dim err = InvoiceDetailTranslator.TranslateBOToEntity(row, x)
                End If
            End If
        Next
        'delete
        Dim delList As New List(Of InvoicesDetails)
        For Each x In _entity.InvoicesDetails
            If (Not rows.Any(Function(f) f.Id = x.Id)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            _entity.InvoicesDetails.Remove(x)
        Next
        Return ErrorCode.Success
    End Function
End Class
