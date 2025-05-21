using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;
using DALCore;


namespace ServiceCore.Translators
{
    public class ProjectTranslator
    {
        internal static ErrorCode TranslateEntityToBO(Project _entity, ProjectBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            // Algemene gegevens
            bo.CommercialTextNL = _entity.CommercialTextNl;
            bo.CommercialTitleNL = _entity.CommercialTitleNl;
            bo.DeliveryDate = _entity.DeliveryDate;
            bo.DeliveryDateDef = _entity.DeliveryDateDef;
            if (_entity.ExecutionDays is not null)
                bo.ExecutionDays = _entity.ExecutionDays;
            bo.HouseNumber = _entity.Number;
            bo.Id = _entity.ProjectId;
            bo.Name = _entity.ProjectName;
            bo.ProjectType = (ProjectType)_entity.ProjectType;
            bo.Slug = _entity.Slug;
            bo.StartDateConstruction = _entity.StartDateConstruction;
            bo.Street = _entity.Street;
            bo.AspNetUserID = _entity.AspNetUserId;
            bo.ProjectFolder = _entity.ProjectFolder;
            if (_entity.TotalLandShare is not null)
                bo.TotalLandShare = _entity.TotalLandShare;
            if (_entity.FacebookAlbumId is not null)
                bo.FacebookAlbumId = _entity.FacebookAlbumId;
            bo.FacebookPlaceId = _entity.FacebookPlaceId;
            bo.DocDefDelivery = _entity.DocDefDelivery;
            bo.DocDelivery = _entity.DocDelivery;
            bo.DocElectricalInspection = _entity.DocElectricalInspection;
            bo.DocFireInspection = _entity.DocFireInspection;
            bo.DocPID = _entity.DocPid;
            bo.DocSewerInspection = _entity.DocSewerInspection;
            bo.DocWaterInspection = _entity.DocWaterInspection;
            // bo.UploadToFacebook = _entity.UploadToFacebook

            // Gemeente en postcode van het project
            if ((_entity.PostalCode != null))
            {
                bo.Postalcode.Postcode = _entity.PostalCode.Postcode;
                bo.Postalcode.Gemeente = _entity.PostalCode.Gemeente;
                bo.Postalcode.PostcodeId = _entity.PostalCode.PostcodeId;
                if (_entity.PostalCode.Country != null)
                {
                    bo.Postalcode.Country.Name = _entity.PostalCode.Country.LandNaam;
                    bo.Postalcode.Country.CountryId = _entity.PostalCode.Country.Id;
                    bo.Postalcode.Country.ISOCode = _entity.PostalCode.Country.LandIsocode;
                }
                if (_entity.PostalCode.Provincie != null)
                {
                    bo.Postalcode.Provincie.Name = _entity.PostalCode.Provincie.ProvincieName;
                    bo.Postalcode.Provincie.ProvincieId = _entity.PostalCode.Provincie.ProvincieId;
                }
            }
            // Projectstatus (uitvoering, oplevering, ...)
            if ((_entity.Status != null))
            {
                bo.Status.Id = _entity.Status.StatusId;
                bo.Status.Name = _entity.Status.StatusName;
            }
            // Projectontwikkelaar
            if ((_entity.Developer != null))
                bo.Developer = _entity.Developer.GetIdName();
            // Bouwheer
            if ((_entity.Builder != null))
                bo.Builder = _entity.Builder.GetIdName();
            // Architect
            if ((_entity.Architect != null))
                bo.Architect = _entity.Architect.GetIdName();
            // Ingenieur stabiliteit
            if ((_entity.Engineer != null))
                bo.Engineer = _entity.Engineer.GetIdName();
            // Veiligheidscoordinator
            if ((_entity.SecurityCoordinator != null))
                bo.SecurityCoordinator = _entity.SecurityCoordinator.GetIdName();
            // EPB verslaggever
            if ((_entity.EpbReporter != null))
                bo.EpbReporter = _entity.EpbReporter.GetIdName();
            // Weerstation ivm verletdagen
            if ((_entity.WheaterStation != null))
            {
                bo.WheaterStation.Id = _entity.WheaterStation.Id;
                bo.WheaterStation.Name = _entity.WheaterStation.Name;
                bo.WheaterStation.Visible = _entity.WheaterStation.Visible;
            }
            // Standaard weer te geven foto
            if ((_entity.DefaultPicture != null))
            {
                bo.DefaultPicture.Id = _entity.DefaultPicture.Id;
                bo.DefaultPicture.Name = _entity.DefaultPicture.Name;
                bo.DefaultPicture.Caption = _entity.DefaultPicture.Caption;
            }
            // Alle project fotos
            foreach (var x in _entity.ProjectPictures)
            {
                ProjectPictureBO picture = new ProjectPictureBO();
                picture.Id = x.Id;
                picture.Caption = x.Caption;
                picture.Name = x.Name;
                picture.Type = (PictureType)x.Type;
                picture.DateTimeUploaded = x.Datetimeuploaded;
                picture.FacebookIdCopro = x.FacebookIdCopro;
                bo.Pictures.Add(picture);
            }
            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(Project _entity, ProjectBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.CommercialTextNl = bo.CommercialTextNL;
            _entity.CommercialTitleNl = bo.CommercialTitleNL;
            _entity.DeliveryDate = bo.DeliveryDate;
            _entity.DeliveryDateDef = bo.DeliveryDateDef;
            _entity.ExecutionDays = bo.ExecutionDays;
            _entity.Number = bo.HouseNumber;
            _entity.ProjectName = bo.Name;
            _entity.Slug = bo.Slug;
            _entity.StartDateConstruction = bo.StartDateConstruction;
            _entity.Street = bo.Street;
            _entity.FacebookAlbumId = bo.FacebookAlbumId;
            _entity.AspNetUserId = bo.AspNetUserID;
            _entity.TotalLandShare = bo.TotalLandShare;
            _entity.FacebookPlaceId = bo.FacebookPlaceId;
            _entity.ProjectFolder = bo.ProjectFolder;
            _entity.DocDefDelivery = bo.DocDefDelivery;
            _entity.DocDelivery = bo.DocDelivery;
            _entity.DocElectricalInspection = bo.DocElectricalInspection;
            _entity.DocFireInspection = bo.DocFireInspection;
            _entity.DocPid = bo.DocPID;
            _entity.DocSewerInspection = bo.DocSewerInspection;
            _entity.DocWaterInspection = bo.DocWaterInspection;
            _entity.ProjectType = (int)bo.ProjectType;


            if ((bo.Postalcode != null && bo.Postalcode.PostcodeId != 0))
                _entity.PostalCodeId = bo.Postalcode.PostcodeId;
            if ((bo.Developer != null && bo.Developer.ID != 0))
                _entity.DeveloperId = bo.Developer.ID;
            if ((bo.Builder != null && bo.Builder.ID != 0))
                _entity.BuilderId = bo.Builder.ID;
            if ((bo.Architect != null && bo.Architect.ID != 0))
                _entity.ArchitectId = bo.Architect.ID;
            if ((bo.Engineer != null && bo.Engineer.ID != 0))
                _entity.EngineerId = bo.Engineer.ID;
            if ((bo.SecurityCoordinator != null && bo.SecurityCoordinator.ID != 0))
                _entity.SecurityCoordinatorId = bo.SecurityCoordinator.ID;
            if ((bo.EpbReporter != null && bo.EpbReporter.ID != 0))
                _entity.EpbReporterId = bo.EpbReporter.ID;
            if ((bo.Status != null && bo.Status.Id != 0))
                _entity.StatusId = bo.Status.Id;
            if ((bo.WheaterStation != null && bo.WheaterStation.Id != 0))
                _entity.WheaterStationId = bo.WheaterStation.Id;
            if ((bo.DefaultPicture != null && bo.DefaultPicture.Id != 0))
                _entity.DefaultPicture.Id = bo.DefaultPicture.Id;
            var err = HandlePictures(_entity, bo.Pictures);
            if ((err != ErrorCode.Success))
                return err;
            return ErrorCode.Success;
        }

        private static ErrorCode HandlePictures(Project _entity, List<ProjectPictureBO> pictures)
        {
            if ((pictures.Count == 0))
                return ErrorCode.Success;
            foreach (var x in pictures)
            {
                if ((x.Id == 0))
                {
                    // insert
                    ProjectPictures picture = new ProjectPictures();
                    picture.Name = x.Name;
                    picture.Caption = x.Caption;
                    picture.Type = (int)x.Type;
                    picture.Datetimeuploaded = DateTime.Now;
                    picture.FacebookIdCopro = x.FacebookIdCopro;
                    _entity.ProjectPictures.Add(picture);
                }
                else
                {
                    // update
                    var picture = _entity.ProjectPictures.FirstOrDefault(f => f.Id == x.Id);
                    if ((picture != null))
                    {
                        picture.Name = x.Name;
                        picture.Caption = x.Caption;
                        picture.Type = (int)x.Type;
                        picture.Datetimeuploaded = x.DateTimeUploaded;
                        picture.FacebookIdCopro = x.FacebookIdCopro;
                    }
                }
            }
            // delete
            List<ProjectPictures> delList = new List<ProjectPictures>();
            foreach (var x in _entity.ProjectPictures)
            {
                if ((!pictures.Any(f => f.Id == x.Id)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.ProjectPictures.Remove(x);
            return ErrorCode.Success;
        }
    }
}
