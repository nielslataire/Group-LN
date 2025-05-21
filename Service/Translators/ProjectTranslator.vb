Imports DAL
Imports BO
Imports System.Text.RegularExpressions
Public Class ProjectTranslator

    Friend Shared Function TranslateEntityToBO(_entity As Project, bo As ProjectBO) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        'Algemene gegevens
        bo.CommercialTextNL = _entity.CommercialTextNL
        bo.CommercialTitleNL = _entity.CommercialTitleNL
        bo.DeliveryDate = _entity.DeliveryDate
        bo.DeliveryDateDef = _entity.DeliveryDateDef
        If Not _entity.ExecutionDays Is Nothing Then bo.ExecutionDays = _entity.ExecutionDays
        bo.HouseNumber = _entity.Number
        bo.Id = _entity.ProjectID
        bo.Name = _entity.ProjectName
        bo.ProjectType = _entity.ProjectType
        bo.Slug = _entity.Slug
        bo.StartDateConstruction = _entity.StartDateConstruction
        bo.Street = _entity.Street
        bo.AspNetUserID = _entity.AspNetUserID
        bo.ProjectFolder = _entity.ProjectFolder
        If Not _entity.TotalLandShare Is Nothing Then bo.TotalLandShare = _entity.TotalLandShare
        If Not _entity.FacebookAlbumID Is Nothing Then
            bo.FacebookAlbumId = _entity.FacebookAlbumID
        End If
        bo.FacebookPlaceId = _entity.FacebookPlaceId
        bo.DocDefDelivery = _entity.DocDefDelivery
        bo.DocDelivery = _entity.DocDelivery
        bo.DocElectricalInspection = _entity.DocElectricalInspection
        bo.DocFireInspection = _entity.DocFireInspection
        bo.DocPID = _entity.DocPID
        bo.DocSewerInspection = _entity.DocSewerInspection
        bo.DocWaterInspection = _entity.DocWaterInspection
        'bo.UploadToFacebook = _entity.UploadToFacebook

        'Gemeente en postcode van het project
        If (_entity.PostalCode IsNot Nothing) Then
            bo.Postalcode.Postcode = _entity.PostalCode.Postcode
            bo.Postalcode.Gemeente = _entity.PostalCode.Gemeente
            bo.Postalcode.PostcodeId = _entity.PostalCode.PostcodeID
            If _entity.PostalCode.Country IsNot Nothing Then
                bo.Postalcode.Country.Name = _entity.PostalCode.Country.LandNaam
                bo.Postalcode.Country.CountryID = _entity.PostalCode.Country.ID
                bo.Postalcode.Country.ISOCode = _entity.PostalCode.Country.LandISOCode
            End If
            If _entity.PostalCode.Provincie IsNot Nothing Then
                bo.Postalcode.Provincie.Name = _entity.PostalCode.Provincie.ProvincieName
                bo.Postalcode.Provincie.ProvincieId = _entity.PostalCode.Provincie.ProvincieID
            End If
        End If
        'Projectstatus (uitvoering, oplevering, ...)
        If (_entity.ProjectStatus IsNot Nothing) Then
            bo.Status.Id = _entity.ProjectStatus.StatusID
            bo.Status.Name = _entity.ProjectStatus.StatusName
        End If
        'Projectontwikkelaar
        If (_entity.Developer IsNot Nothing) Then
            bo.Developer = _entity.Developer.GetIdName()
        End If
        'Bouwheer
        If (_entity.Builder IsNot Nothing) Then
            bo.Builder = _entity.Builder.GetIdName()
        End If
        'Architect
        If (_entity.Architect IsNot Nothing) Then
            bo.Architect = _entity.Architect.GetIdName()
        End If
        'Ingenieur stabiliteit
        If (_entity.Engineer IsNot Nothing) Then
            bo.Engineer = _entity.Engineer.GetIdName()
        End If
        'Veiligheidscoordinator
        If (_entity.SecurityCoordinator IsNot Nothing) Then
            bo.SecurityCoordinator = _entity.SecurityCoordinator.GetIdName()
        End If
        'EPB verslaggever
        If (_entity.EpbReporter IsNot Nothing) Then
            bo.EpbReporter = _entity.EpbReporter.GetIdName()
        End If
        'Weerstation ivm verletdagen
        If (_entity.WheaterStations IsNot Nothing) Then
            bo.WheaterStation.Id = _entity.WheaterStations.ID
            bo.WheaterStation.Name = _entity.WheaterStations.Name
            bo.WheaterStation.Visible = _entity.WheaterStations.Visible
        End If
        'Standaard weer te geven foto
        If (_entity.DefaultPicture IsNot Nothing) Then
            bo.DefaultPicture.Id = _entity.DefaultPicture.Id
            bo.DefaultPicture.Name = _entity.DefaultPicture.Name
            bo.DefaultPicture.Caption = _entity.DefaultPicture.Caption
        End If
        'Alle project fotos
        For Each x In _entity.ProjectPictures
            Dim picture As New ProjectPictureBO
            picture.Id = x.Id
            picture.Caption = x.Caption
            picture.Name = x.Name
            picture.Type = x.Type
            picture.DateTimeUploaded = x.Datetimeuploaded
            picture.FacebookIdCopro = x.FacebookIdCopro
            bo.Pictures.Add(picture)

        Next
        Return ErrorCode.Success
    End Function
    Friend Shared Function TranslateBOToEntity(_entity As Project, bo As ProjectBO, uow As UnitOfWork) As ErrorCode
        If _entity Is Nothing Then Return ErrorCode.EntityNull
        If bo Is Nothing Then Return ErrorCode.BoNull
        _entity.CommercialTextNL = bo.CommercialTextNL
        _entity.CommercialTitleNL = bo.CommercialTitleNL
        _entity.DeliveryDate = bo.DeliveryDate
        _entity.DeliveryDateDef = bo.DeliveryDateDef
        _entity.ExecutionDays = bo.ExecutionDays
        _entity.Number = bo.HouseNumber
        _entity.ProjectName = bo.Name
        _entity.Slug = bo.Slug
        _entity.StartDateConstruction = bo.StartDateConstruction
        _entity.Street = bo.Street
        _entity.FacebookAlbumID = bo.FacebookAlbumId
        _entity.AspNetUserID = bo.AspNetUserID
        _entity.TotalLandShare = bo.TotalLandShare
        _entity.FacebookPlaceId = bo.FacebookPlaceId
        _entity.ProjectFolder = bo.ProjectFolder
        _entity.DocDefDelivery = bo.DocDefDelivery
        _entity.DocDelivery = bo.DocDelivery
        _entity.DocElectricalInspection = bo.DocElectricalInspection
        _entity.DocFireInspection = bo.DocFireInspection
        _entity.DocPID = bo.DocPID
        _entity.DocSewerInspection = bo.DocSewerInspection
        _entity.DocWaterInspection = bo.DocWaterInspection
        _entity.ProjectType = bo.ProjectType


        If (bo.Postalcode IsNot Nothing And bo.Postalcode.PostcodeId <> 0) Then
            _entity.PostalCodeID = bo.Postalcode.PostcodeId
        End If
        If (bo.Developer IsNot Nothing And bo.Developer.ID <> 0) Then
            _entity.DeveloperID = bo.Developer.ID
        End If
        If (bo.Builder IsNot Nothing And bo.Builder.ID <> 0) Then
            _entity.BuilderID = bo.Builder.ID
        End If
        If (bo.Architect IsNot Nothing And bo.Architect.ID <> 0) Then
            _entity.ArchitectID = bo.Architect.ID
        End If
        If (bo.Engineer IsNot Nothing And bo.Engineer.ID <> 0) Then
            _entity.EngineerID = bo.Engineer.ID
        End If
        If (bo.SecurityCoordinator IsNot Nothing And bo.SecurityCoordinator.ID <> 0) Then
            _entity.SecurityCoordinatorID = bo.SecurityCoordinator.ID
        End If
        If (bo.EpbReporter IsNot Nothing And bo.EpbReporter.ID <> 0) Then
            _entity.EpbReporterID = bo.EpbReporter.ID
        End If
        If (bo.Status IsNot Nothing And bo.Status.Id <> 0) Then
            _entity.StatusID = bo.Status.Id
        End If
        If (bo.WheaterStation IsNot Nothing And bo.WheaterStation.Id <> 0) Then
            _entity.WheaterStationID = bo.WheaterStation.Id
        End If
        If (bo.DefaultPicture IsNot Nothing And bo.DefaultPicture.Id <> 0) Then
            _entity.DefaultPicture.Id = bo.DefaultPicture.Id
        End If
        Dim err = HandlePictures(_entity, bo.Pictures)
        If (err <> ErrorCode.Success) Then Return err
        Return ErrorCode.Success
    End Function

    Private Shared Function HandlePictures(_entity As Project, pictures As List(Of ProjectPictureBO)) As ErrorCode
        If (pictures.Count = 0) Then Return ErrorCode.Success
        For Each x In pictures
            If (x.Id = 0) Then
                'insert
                Dim picture As New ProjectPictures
                picture.Name = x.Name
                picture.Caption = x.Caption
                picture.Type = x.Type
                picture.Datetimeuploaded = DateTime.Now()
                picture.FacebookIdCopro = x.FacebookIdCopro
                _entity.ProjectPictures.Add(picture)
            Else
                'update
                Dim picture = _entity.ProjectPictures.FirstOrDefault(Function(f) f.Id = x.Id)
                If (picture IsNot Nothing) Then
                    picture.Name = x.Name
                    picture.Caption = x.Caption
                    picture.Type = x.Type
                    picture.Datetimeuploaded = x.DateTimeUploaded
                    picture.FacebookIdCopro = x.FacebookIdCopro

                End If
            End If
        Next
        'delete
        Dim delList As New List(Of ProjectPictures)
        For Each x In _entity.ProjectPictures
            If (Not pictures.Any(Function(f) f.Id = x.Id)) Then
                delList.Add(x)
            End If
        Next
        For Each x In delList
            _entity.ProjectPictures.Remove(x)
        Next
        Return ErrorCode.Success
    End Function
End Class
