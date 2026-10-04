using BOCore;
using CPMCore.Documents;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Models.Invoicing;
using CPMCore.Models.Klanten;
using CPMCore.Models.Leveranciers;
using CPMCore.Models.Projecten;
using FacadeCore;
using DALCore;
using DALCore.Models;
using FluentFTP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;
using ServiceCore;
using ServiceCore.Budget;
using ServiceCore.Invoicing;
using BOCore.Budget;
using CPMCore.Models.Budget;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using ClosedXML.Excel;
using SmartBreadcrumbs.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace CPMCore.Controllers
{
    /// <summary>Foto's, nieuws en media-secties van een project (upload, FTP, secties-API). Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/Media/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
        // ========== PROJECT DETAIL FOTO'S ==========

        [HttpGet]
        //[Breadcrumb("Foto's")]
        [Breadcrumb("Media", FromAction = "Detail")]
        public ActionResult DetailPhotos(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var imgBase = (Configuration["URL:ImageWebURL"] ?? "").TrimEnd('/');
            var vidBase = (Configuration["URL:VideoWebURL"] ?? imgBase).TrimEnd('/');
            ViewBag.ImageWebURL = imgBase + "/";
            ViewBag.VideoWebURL = vidBase + "/";

            var model   = new DetailPhotosModel();
            var service = _projectService;
            var response = service.GetPicturesByProjectId(projectid);

            if (response.Success)
                model.Photos = response.Values.OrderBy(m => m.SortOrder).ThenByDescending(m => m.DateTimeUploaded).ToList();

            // Secties laden
            model.Sections = _db.Set<DALCore.Models.ProjectMediaSection>()
                .Where(s => s.ProjectId == projectid)
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
                .Select(s => new ProjectMediaSectionVM
                {
                    Id          = s.Id,
                    Name        = s.Name,
                    Description = s.Description,
                    SortOrder   = s.SortOrder,
                    IsPublic    = s.IsPublic,
                    MediaCount  = s.ProjectPictures.Count,
                    PhotoCount  = s.ProjectPictures.Count(m => m.MediaType == 0),
                    VideoCount  = s.ProjectPictures.Count(m => m.MediaType == 1)
                })
                .ToList();

            model.ProjectId   = projectid;
            model.ProjectName = service.GetProjectNameById(projectid);

            model.Units = _db.Set<DALCore.Models.Units>()
                .Where(u => u.ProjectId == projectid)
                .OrderBy(u => u.Name)
                .Select(u => new IdNameBO { ID = u.Id, Display = u.Name })
                .ToList();

            // gl-v2: zelfde vlag/reden als DetailClientsModel/ClientModel.IsCoordinationProject —
            // _ProjectInnerMenuV2 heeft dit nodig om Nieuws/Contacten correct te verbergen.
            var projectResponse = service.GetProjectByID(projectid);
            model.IsCoordinationProject = projectResponse.Success && projectResponse.Value?.IsOnlyCoordinationProject == true;

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = Index };
            var projectDetail  = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
                { Parent = projectenIndex, RouteValues = new { projectid } };
            // gl-v2: stopt bewust bij de projectnaam i.p.v. een "Media"-blad toe te voegen — dat zou
            // dezelfde tekst herhalen als de topbar-titel ("Media"), exact wat de breadcrumb-regel-2-
            // sweep elders al aanpakte (zie Klanten/DetailV2 e.a.). Op gsm/tablet toont de topbar enkel
            // de laatste kruimel als subtitel, die wordt zo de projectnaam i.p.v. "Media" nog eens.
            ViewData["BreadcrumbNode"] = projectDetail;

            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectPhotos  = _ps.HasWrite(PermissionCodes.ProjectsPhotos);
            ViewBag.CanDeleteProjectPhotos = _ps.HasDelete(PermissionCodes.ProjectsPhotos);

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Media");
            // gl-v2 layout-pilot: zelfde data/query hierboven, enkel de view wisselt (design-handoff/
            // punt 15 "Media in een project").
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "DetailPhotosV2" : "DetailPhotos", model);
        }
        [HttpGet]
        public ActionResult ModalAddPhoto(int id)
        {
            var viewModel = new ProjectPictureBO();
            viewModel.ProjectId = id;
            viewModel.Type = PictureType.Werffoto;
            return PartialView("_ModalAddPhoto", viewModel);
        }

        [HttpGet]
        public ActionResult ModalDeletePhoto(int id)
        {
            var viewModel = new ProjectPictureBO();

            if (id != 0)
            {
                var dservice = _projectService;
                viewModel = dservice.GetPictureById(id).Value;
            }

            return PartialView("_ModalDeletePhoto", viewModel);
        }

        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsPhotos)]
        public ActionResult DeletePhoto(int id, int projectid, PictureType type)
        {
            if (id != 0 && projectid != 0)
            {
                if (type == PictureType.Hoofdfoto)
                {
                    var service = _projectService;
                    var ids = new List<int> { id };

                    var response1 = service.SetDefaultProjectPicture(projectid, 0);
                    if (response1.Success)
                    {
                        DeletePictureFile(id);
                        var response2 = service.DeletePicture(ids);

                        if (response2.Success)
                        {
                            AddMessage("success", "De foto is verwijderd", "Geslaagd!");
                            return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                        }
                        else
                        {
                            AddMessage("error", "De foto is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                            return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                        }
                    }
                    else
                    {
                        AddMessage("error", "De foto is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                    }
                }
                else
                {
                    DeletePictureFile(id);
                    var service = _projectService;
                    var ids = new List<int> { id };
                    var response = service.DeletePicture(ids);

                    if (response.Success)
                    {
                        AddMessage("success", "De foto is verwijderd", "Geslaagd!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                    }
                    else
                    {
                        AddMessage("error", "De foto is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                    }
                }
            }

            return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
        }

        public ActionResult UpdatePhotoType(int id, PictureType type)
        {
            var service = _projectService;
            var picture = service.GetPictureById(id).Value;

            if (picture != null)
            {
                if (type != PictureType.Hoofdfoto)
                {
                    picture.Type = type;
                    var response = service.InsertUpdatePicture(picture);

                    if (response.Success)
                    {
                        AddMessage("success", "Het type van de foto is gewijzigd", "Geslaagd!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid = picture.ProjectId });
                    }
                    else
                    {
                        AddMessage("error", "Het type van de foto is NIET gewijzigd", "Fout!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid = picture.ProjectId });
                    }
                }
                else
                {
                    picture.Type = type;

                    var response1 = service.SetDefaultProjectPicture(picture.ProjectId, picture.Id);
                    if (response1.Success)
                    {
                        var response = service.InsertUpdatePicture(picture);
                        if (response.Success)
                        {
                            AddMessage("success", "Het type van de foto is gewijzigd", "Geslaagd!");
                            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = picture.ProjectId });
                        }
                        else
                        {
                            AddMessage("error", "Het type van de foto is NIET gewijzigd", "Fout!");
                            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = picture.ProjectId });
                        }
                    }
                }
            }

            // fallback: als picture null is, vermijden we NullReference
            var fallbackProjectId = picture != null ? picture.ProjectId : 0;
            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = fallbackProjectId });
        }


        // ========== PROJECT DETAIL NIEUWS ==========

        [HttpGet]
        [Breadcrumb("Nieuws", FromAction = "Detail")]
        //[Breadcrumb("Nieuws")]
        public IActionResult DetailNews(int projectId)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";


            var model = new DetailNewsModel
            {
                ProjectId = projectId,

                ProjectName = _projectService.GetProjectNameById(projectId),
                News = _projectService.GetNewsByProjectId(projectId).Success
                              ? _projectService.GetNewsByProjectId(projectId).Values
                              : new List<ProjectNewsBO>()
            };
            ViewData["NewsBaseUrl"] = $"{Configuration["URL:ImageWebUrl"]?.TrimEnd('/')}/issues/";



            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectId }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailNews", "Projecten", "Nieuws")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectId }
            };
            ViewData["BreadcrumbNode"] = lastnode;
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectNews = _ps.HasWrite(PermissionCodes.ProjectsNews);
            ViewBag.CanDeleteProjectNews = _ps.HasDelete(PermissionCodes.ProjectsNews);

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Nieuws");
            return View(model);
        }

        [HttpGet]
        public IActionResult ModalAddNews(int id)
        {
            var vm = new ProjectNewsBO
            {
                NewsDate = DateOnly.FromDateTime(DateTime.Now),
                ProjectId = id 
            };

            return PartialView("_ModalAddNews", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddNews(ProjectNewsBO newsItem, IFormFile file)
        {
            if (!ModelState.IsValid)
            {
                AddMessage("error", "Formulier ongeldig.", "Fout!");
                return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
            }

            if (file is not null && file.Length > 0 && IsValidImage(file))
            {

                var filename = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}.jpg";
                var tempRoot = Path.Combine(Path.GetTempPath(), "cpmcore-news");
                Directory.CreateDirectory(tempRoot);

                var mainPath = Path.Combine(tempRoot, $"news_{filename}");
                var originalPath = Path.Combine(tempRoot, $"news_original_{filename}");
                var smallPath = Path.Combine(tempRoot, $"news_800_{filename}");

                try
                {
                    using (var stream = System.IO.File.Create(mainPath))
                        file.CopyTo(stream);

                    System.IO.File.Copy(mainPath, originalPath, overwrite: true);
                    System.IO.File.Copy(mainPath, smallPath, overwrite: true);

                    ScaleAndCropImage(mainPath, 1280, 500);
                    ScaleImage(smallPath, 800, 800);

                    var uploadMain = UploadAssetFileToStorageAsync(mainPath, "pictures/News", filename, "image/jpeg").GetAwaiter().GetResult();
                    var uploadOriginal = UploadAssetFileToStorageAsync(originalPath, "pictures/News/Original", filename, "image/jpeg").GetAwaiter().GetResult();
                    var uploadSmall = UploadAssetFileToStorageAsync(smallPath, "pictures/News/800", filename, "image/jpeg").GetAwaiter().GetResult();

                    if (string.IsNullOrWhiteSpace(uploadMain) || string.IsNullOrWhiteSpace(uploadOriginal) || string.IsNullOrWhiteSpace(uploadSmall))
                    {
                        AddMessage("error", "Afbeelding upload naar storage API mislukt.", "Fout!");
                        return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
                    }
                }
                finally
                {
                    TryDeleteTempFile(mainPath);
                    TryDeleteTempFile(originalPath);
                    TryDeleteTempFile(smallPath);
                }

                var picture = new ProjectPictureBO
                {
                    Name = filename,
                    Caption = newsItem.TitleNL,
                    ProjectId = newsItem.ProjectId,
                    Type = PictureType.Nieuws,
                    DateTimeUploaded = DateTime.Now
                };
                newsItem.Picture = picture;
            }

            // auteur (gebruik wat voorhanden is)
            newsItem.Author = (string?)ViewData["fullname"] ?? User?.Identity?.Name ?? "onbekend";

            var response = _projectService.InsertUpdateNews(newsItem);
            if (response.Success)
            {
                AddMessage("success", "Het nieuwsbericht is toegevoegd.", "Geslaagd!");
            }
            else
            {
                AddMessage("error", "Het nieuwsbericht is NIET toegevoegd. Probeer opnieuw of contacteer de administrator.", "Fout!");
            }

            return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
        }

        [HttpGet]
        public IActionResult ModalDeleteNews(int id)
        {
            var vm = new ProjectNewsBO();
            if (id != 0)
            {
    
                var resp = _projectService.GetNewsById(id);
                if (resp.Success && resp.Value is not null)
                    vm = resp.Value;
            }
            return PartialView("_ModalDeleteNews", vm);
        }

        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsNews)]
        public ActionResult DeleteNews(int id, int projectId, int pictureId)
        {
            if (id == 0 || projectId == 0)
                return RedirectToAction("DetailNews", new { projectId });

            var resp = _projectService.DeleteNews(new List<int> { id });
            if (!resp.Success)
            {
                AddMessage("error", "Het nieuwsitem is niet verwijderd.", "Fout!");
                return RedirectToAction("DetailNews", new { projectId });
            }

            // eventueel gekoppelde foto verwijderen
            if (pictureId > 0)
            {
                try { DeletePictureFile(pictureId); } catch { /* log indien gewenst */ }

                var respPic = _projectService.DeletePicture(new List<int> { pictureId });
                if (!respPic.Success)
                {
                    AddMessage("error", "Nieuws verwijderd maar foto kon niet verwijderd worden.", "Opgelet");
                    return RedirectToAction("DetailNews", new { projectId });
                }
            }

            AddMessage("success", "Het nieuwsitem is verwijderd.", "Geslaagd!");
            return RedirectToAction("DetailNews", new { projectId });
        }

        [HttpGet]
        public IActionResult ModalEditNews(int id)
        {
            var vm = new ProjectNewsBO();
            if (id != 0)
            {
    
                var resp = _projectService.GetNewsById(id);
                if (resp.Success && resp.Value is not null)
                    vm = resp.Value;
            }
            return PartialView("_ModalEditNews", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditNews(ProjectNewsBO newsItem, IFormFile? file, bool RemovePicture = false)
        {
            if (!ModelState.IsValid)
            {
                AddMessage("error", "Formulier ongeldig.", "Fout!");
                return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
            }



            // Bewaar referentie naar de (mogelijke) bestaande foto om na succes op te ruimen
            var oldPicId = newsItem.Picture?.Id ?? 0;
            var oldPicName = newsItem.Picture?.Name;

            // Validatie op bestandstype indien er een upload is
            if (file is not null && file.Length > 0 && !IsValidImage(file))
            {
                AddMessage("error", "Verkeerd bestandstype. Kies een JPG/PNG/GIF.", "Fout!");
                return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
            }

            // Nieuwe upload?
            ProjectPictureBO? newPicture = null;
            if (file is not null && file.Length > 0)
            {
                var filename = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}.jpg";
                var tempRoot = Path.Combine(Path.GetTempPath(), "cpmcore-news");
                Directory.CreateDirectory(tempRoot);

                var mainPath = Path.Combine(tempRoot, $"news_{filename}");
                var originalPath = Path.Combine(tempRoot, $"news_original_{filename}");

                try
                {
                    using (var stream = System.IO.File.Create(mainPath))
                        file.CopyTo(stream);
                    System.IO.File.Copy(mainPath, originalPath, overwrite: true);

                    ScaleAndCropImage(mainPath, 1280, 500);

                    var uploadMain = UploadAssetFileToStorageAsync(mainPath, "pictures/News", filename, "image/jpeg").GetAwaiter().GetResult();
                    var uploadOriginal = UploadAssetFileToStorageAsync(originalPath, "pictures/News/Original", filename, "image/jpeg").GetAwaiter().GetResult();
                    if (string.IsNullOrWhiteSpace(uploadMain) || string.IsNullOrWhiteSpace(uploadOriginal))
                    {
                        AddMessage("error", "Afbeelding upload naar storage API mislukt.", "Fout!");
                        return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
                    }
                }
                finally
                {
                    TryDeleteTempFile(mainPath);
                    TryDeleteTempFile(originalPath);
                }

                newPicture = new ProjectPictureBO
                {
                    Name = filename,
                    Caption = newsItem.TitleNL,
                    ProjectId = newsItem.ProjectId,
                    Type = PictureType.Nieuws,
                    DateTimeUploaded = DateTime.Now
                };

                newsItem.Picture = newPicture;
            }
            else if (RemovePicture)
            {
                // Alleen verwijderen (geen nieuwe upload)
                newsItem.Picture = null;               // verwijder koppeling
                                                       // de eigenlijke oude foto (record + bestand) ruimen we op ná een geslaagde update
            }
            // Else: niets doen → bestaande foto blijft gekoppeld

            var response = _projectService.InsertUpdateNews(newsItem);

            if (response.Success)
            {
                // Als we een nieuwe foto hebben gezet of expliciet verwijderen, oude foto opruimen
                if ((newPicture is not null || RemovePicture) && oldPicId > 0)
                {
                    try { DeletePictureFile(oldPicId); } catch { /* loggen indien gewenst */ }
                    _projectService.DeletePicture(new List<int> { oldPicId });
                }

                AddMessage("success", "Het nieuwsbericht is bijgewerkt.", "Geslaagd!");
            }
            else
            {
                AddMessage("error", "Het nieuwsbericht is NIET bijgewerkt.", "Fout!");
            }

            return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(ProjectPictureBO UploadedBO, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });

            var isVideo = _validVideoTypes.Contains(file.ContentType);
            var isImage = _validImageTypes.Contains(file.ContentType);

            if (!isImage && !isVideo)
            {
                AddMessage("error", "Ongeldig bestandstype. Kies een afbeelding (jpg, png, webp) of video (mp4, webm).", "Fout!");
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
            }

            string storedFilename;

            if (isVideo)
            {
                // Video: upload direct zonder verwerking
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext)) ext = ".mp4";
                var videoFilename = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
                var tempRoot = Path.Combine(Path.GetTempPath(), "cpmcore-videos");
                Directory.CreateDirectory(tempRoot);
                var tempPath = Path.Combine(tempRoot, videoFilename);

                try
                {
                    using (var stream = System.IO.File.Create(tempPath))
                        await file.CopyToAsync(stream);

                    var uploaded = await UploadAssetFileToStorageAsync(tempPath, "videos", videoFilename, file.ContentType);
                    if (string.IsNullOrWhiteSpace(uploaded))
                    {
                        AddMessage("error", "Video upload naar storage API mislukt.", "Fout!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
                    }
                    storedFilename = uploaded;
                }
                finally
                {
                    TryDeleteTempFile(tempPath);
                }

                var videoPicture = new ProjectPictureBO
                {
                    Name            = storedFilename,
                    Caption         = UploadedBO.Caption,
                    ProjectId       = UploadedBO.ProjectId,
                    Type            = UploadedBO.Type,
                    SectionId       = UploadedBO.SectionId,
                    IsPublic        = UploadedBO.IsPublic,
                    MediaType       = 1, // Video
                    FileSizeBytes   = file.Length,
                    DateTimeUploaded = DateTime.Now
                };
                _projectService.InsertUpdatePicture(videoPicture);
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
            }

            // Image: schalen, bijsnijden en opslaan als WebP
            var ts         = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var tempRootImg = Path.Combine(Path.GetTempPath(), "cpmcore-pictures");
            Directory.CreateDirectory(tempRootImg);

            var rawPath   = Path.Combine(tempRootImg, $"raw_{ts}{Path.GetExtension(file.FileName)}");
            var main447   = Path.Combine(tempRootImg, $"447_{ts}.webp");
            var main800   = Path.Combine(tempRootImg, $"800_{ts}.webp");
            var mainFull  = Path.Combine(tempRootImg, $"main_{ts}.webp");
            var webpName  = $"{ts}.webp";

            try
            {
                using (var stream = System.IO.File.Create(rawPath))
                    await file.CopyToAsync(stream);

                // Lees afmetingen voor de DB
                int imgWidth = 0, imgHeight = 0;
                using (var img = SixLabors.ImageSharp.Image.Load(rawPath))
                {
                    imgWidth  = img.Width;
                    imgHeight = img.Height;
                }

                System.IO.File.Copy(rawPath, main447, overwrite: true);
                System.IO.File.Copy(rawPath, main800, overwrite: true);
                System.IO.File.Copy(rawPath, mainFull, overwrite: true);

                ScaleAndCropImage(main447, 447, 447);
                ScaleAndCropImage(main800, 800, 800);
                ScaleImage(mainFull, 1280, 960);

                // Na ScaleAndCropImage worden paden omgezet naar .webp indien nodig
                main447  = Path.ChangeExtension(main447,  ".webp");
                main800  = Path.ChangeExtension(main800,  ".webp");
                mainFull = Path.ChangeExtension(mainFull, ".webp");

                var uploadMain = await UploadAssetFileToStorageAsync(mainFull, "pictures",     webpName, "image/webp");
                var upload447  = await UploadAssetFileToStorageAsync(main447,  "pictures/447", webpName, "image/webp");
                var upload800  = await UploadAssetFileToStorageAsync(main800,  "pictures/800", webpName, "image/webp");

                if (string.IsNullOrWhiteSpace(uploadMain) || string.IsNullOrWhiteSpace(upload447) || string.IsNullOrWhiteSpace(upload800))
                {
                    AddMessage("error", "Afbeelding upload naar storage API mislukt.", "Fout!");
                    return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
                }

                storedFilename = webpName;

                var picture = new ProjectPictureBO
                {
                    Name            = storedFilename,
                    Caption         = UploadedBO.Caption,
                    ProjectId       = UploadedBO.ProjectId,
                    Type            = UploadedBO.Type,
                    SectionId       = UploadedBO.SectionId,
                    IsPublic        = UploadedBO.IsPublic,
                    MediaType       = 0, // Photo
                    FileSizeBytes   = file.Length,
                    WidthPx         = imgWidth,
                    HeightPx        = imgHeight,
                    DateTimeUploaded = DateTime.Now
                };

                var service  = _projectService;
                var response = service.InsertUpdatePicture(picture);

                if (picture.Type == PictureType.Hoofdfoto && response?.Messages != null)
                {
                    foreach (var msg in response.Messages)
                    {
                        if (msg.Type == MessageType.Value && int.TryParse(msg.Message, out var pictureId))
                            _ = service.SetDefaultProjectPicture(UploadedBO.ProjectId, pictureId);
                    }
                }
            }
            finally
            {
                TryDeleteTempFile(rawPath);
                TryDeleteTempFile(main447);
                TryDeleteTempFile(main800);
                TryDeleteTempFile(mainFull);
            }

            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
        }

        // ── Media Sections API ────────────────────────────────────────────────

        [HttpGet]
        public IActionResult GetMediaSections(int projectId)
        {
            var ctx = _db;
            var sections = ctx.Set<DALCore.Models.ProjectMediaSection>()
                .Where(s => s.ProjectId == projectId)
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
                .Select(s => new {
                    s.Id, s.Name, s.Description, s.SortOrder, s.IsPublic,
                    MediaCount = s.ProjectPictures.Count,
                    PhotoCount = s.ProjectPictures.Count(m => m.MediaType == 0),
                    VideoCount = s.ProjectPictures.Count(m => m.MediaType == 1)
                })
                .ToList();
            return Json(sections);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateMediaSection([FromBody] ProjectMediaSectionRequest req)
        {
            if (req == null || req.ProjectId <= 0 || string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { error = "Ongeldige invoer." });

            var ctx = _db;
            var maxOrder = ctx.Set<DALCore.Models.ProjectMediaSection>()
                .Where(s => s.ProjectId == req.ProjectId)
                .Select(s => (int?)s.SortOrder).Max() ?? -1;

            var section = new DALCore.Models.ProjectMediaSection
            {
                ProjectId   = req.ProjectId,
                Name        = req.Name.Trim(),
                Description = req.Description?.Trim(),
                SortOrder   = maxOrder + 1,
                IsPublic    = req.IsPublic
            };
            ctx.Set<DALCore.Models.ProjectMediaSection>().Add(section);
            ctx.SaveChanges();
            return Json(new { section.Id, section.Name, section.Description, section.SortOrder, section.IsPublic, MediaCount = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMediaSection([FromBody] ProjectMediaSectionRequest req)
        {
            if (req == null || req.Id <= 0) return BadRequest();
            var ctx = _db;
            var section = ctx.Set<DALCore.Models.ProjectMediaSection>().Find(req.Id);
            if (section == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(req.Name))        section.Name        = req.Name.Trim();
            if (req.Description != null)                       section.Description = req.Description.Trim();
            section.IsPublic = req.IsPublic;
            ctx.SaveChanges();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMediaSection(int id, int projectId)
        {
            var ctx = _db;
            var section = ctx.Set<DALCore.Models.ProjectMediaSection>().Find(id);
            if (section == null || section.ProjectId != projectId) return NotFound();

            // Media in deze sectie -> sectie leegmaken (SectionId = null)
            var media = ctx.Set<DALCore.Models.ProjectPictures>().Where(p => p.SectionId == id).ToList();
            foreach (var m in media) m.SectionId = null;

            ctx.Set<DALCore.Models.ProjectMediaSection>().Remove(section);
            ctx.SaveChanges();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MoveMediaToSection([FromBody] MoveMediaRequest req)
        {
            if (req == null) return BadRequest();
            var ctx = _db;
            var media = ctx.Set<DALCore.Models.ProjectPictures>()
                .Where(p => req.MediaIds.Contains(p.Id))
                .ToList();
            foreach (var m in media) m.SectionId = req.SectionId;
            ctx.SaveChanges();
            return Ok(new { moved = media.Count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMediaVisibility([FromBody] VisibilityRequest req)
        {
            if (req == null) return BadRequest();
            var ctx = _db;
            var media = ctx.Set<DALCore.Models.ProjectPictures>()
                .Where(p => req.MediaIds.Contains(p.Id))
                .ToList();
            foreach (var m in media) m.IsPublic = req.IsPublic;
            ctx.SaveChanges();
            return Ok(new { updated = media.Count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDeleteMedia([FromBody] BulkDeleteRequest req)
        {
            if (req == null || req.MediaIds == null || req.MediaIds.Count == 0) return BadRequest();
            var ctx   = _db;
            var items = ctx.Set<DALCore.Models.ProjectPictures>()
                .Where(p => req.MediaIds.Contains(p.Id))
                .ToList();

            var baseUrl     = (Configuration["StorageApi:BaseUrl"] ?? "").TrimEnd('/');
            var writeApiKey = Configuration["StorageApi:WriteApiKey"] ?? "";

            foreach (var item in items)
            {
                // Verwijder fysieke bestanden via Storage API (best-effort)
                if (!string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(item.Name))
                {
                    var folder = item.MediaType == 1 ? "videos" : "pictures";
                    _ = DeleteStorageFileAsync(baseUrl, writeApiKey, folder, item.Name);
                    if (item.MediaType == 0)
                    {
                        _ = DeleteStorageFileAsync(baseUrl, writeApiKey, "pictures/447", item.Name);
                        _ = DeleteStorageFileAsync(baseUrl, writeApiKey, "pictures/800", item.Name);
                    }
                }
                ctx.Set<DALCore.Models.ProjectPictures>().Remove(item);
            }
            await ctx.SaveChangesAsync();
            return Ok(new { deleted = items.Count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMediaSortOrder([FromBody] SortOrderRequest req)
        {
            if (req == null) return BadRequest();
            var ctx = _db;
            foreach (var item in req.Items)
            {
                var m = ctx.Set<DALCore.Models.ProjectPictures>().Find(item.Id);
                if (m != null) { m.SortOrder = item.Order; m.SectionId = item.SectionId; }
            }
            ctx.SaveChanges();
            return Ok();
        }

        private async Task DeleteStorageFileAsync(string baseUrl, string apiKey, string folder, string fileName)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                await client.DeleteAsync($"{baseUrl}/api/assets/{folder}/{fileName}");
            }
            catch { /* best-effort */ }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetHoofdMedia([FromBody] SetHoofdMediaRequest req)
        {
            if (req.MediaId <= 0 || req.ProjectId <= 0) return BadRequest();

            var pic = _db.Set<DALCore.Models.ProjectPictures>()
                .FirstOrDefault(p => p.Id == req.MediaId && p.ProjectId == req.ProjectId);
            if (pic == null) return NotFound();

            // Reset alle huidige hoofdfoto's van dit project naar nevenfoto
            _db.Set<DALCore.Models.ProjectPictures>()
                .Where(p => p.ProjectId == req.ProjectId && p.Type == (int)BOCore.PictureType.Hoofdfoto)
                .ToList()
                .ForEach(p => p.Type = (int)BOCore.PictureType.Nevenfoto);

            pic.Type = (int)BOCore.PictureType.Hoofdfoto;

            var project = _db.Project.FirstOrDefault(p => p.ProjectId == req.ProjectId);
            if (project != null) project.DefaultPictureId = req.MediaId;

            _db.SaveChanges();
            return Ok(new { success = true });
        }

        public record ProjectMediaSectionRequest(int Id, int ProjectId, string Name, string? Description, bool IsPublic);
        public record MoveMediaRequest(List<int> MediaIds, int? SectionId);
        public record VisibilityRequest(List<int> MediaIds, bool IsPublic);
        public record BulkDeleteRequest(List<int> MediaIds);
        public record SetHoofdMediaRequest(int MediaId, int ProjectId);
        public record UpdateCaptionRequest(int MediaId, string? Caption);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMediaCaption([FromBody] UpdateCaptionRequest req)
        {
            if (req.MediaId <= 0) return BadRequest();
            var pic = _db.Set<DALCore.Models.ProjectPictures>().FirstOrDefault(p => p.Id == req.MediaId);
            if (pic == null) return NotFound();
            pic.Caption = req.Caption?.Trim();
            _db.SaveChanges();
            return Ok(new { success = true });
        }

        // gl-v2 (design-handoff punt 15b "Detailpaneel"): één Opslaan-actie voor het hele
        // detailpaneel — titel/sectie/eenheid/zichtbaarheid + de foto/video-specifieke velden
        // (alt-tekst resp. ondertitel + automatisch afspelen), i.p.v. losse toggle-per-toggle
        // AJAX-calls zoals de rest van deze controller. "Hoofdbeeld" loopt via dezelfde
        // SetHoofdMedia-logica als de bestaande sterknop (enkel aanzetten heeft effect — een
        // hoofdbeeld kan niet losstaand uitgezet worden zonder een ander aan te wijzen).
        public record UpdateMediaDetailsRequest(
            int MediaId, int ProjectId, string? Title, string? AltText, string? Subtitle,
            int? SectionId, int? UnitId, bool IsPublic, bool AutoPlayMuted, bool IsHoofdbeeld,
            double? PosterTimestampSeconds);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMediaDetails([FromBody] UpdateMediaDetailsRequest req)
        {
            if (req == null || req.MediaId <= 0) return BadRequest();
            var pic = _db.Set<DALCore.Models.ProjectPictures>().FirstOrDefault(p => p.Id == req.MediaId);
            if (pic == null) return NotFound();

            pic.Caption     = req.Title?.Trim();
            pic.SectionId   = req.SectionId;
            pic.UnitId      = req.UnitId;
            pic.IsPublic    = req.IsPublic;
            if (pic.MediaType == 0)
            {
                pic.AltText = req.AltText?.Trim();
            }
            else
            {
                pic.Subtitle               = req.Subtitle?.Trim();
                pic.AutoPlayMuted          = req.AutoPlayMuted;
                if (req.PosterTimestampSeconds.HasValue)
                {
                    pic.PosterTimestampSeconds = req.PosterTimestampSeconds;
                }
            }

            if (req.IsHoofdbeeld && pic.Type != (int)BOCore.PictureType.Hoofdfoto)
            {
                _db.Set<DALCore.Models.ProjectPictures>()
                    .Where(p => p.ProjectId == req.ProjectId && p.Type == (int)BOCore.PictureType.Hoofdfoto)
                    .ToList()
                    .ForEach(p => p.Type = (int)BOCore.PictureType.Nevenfoto);
                pic.Type = (int)BOCore.PictureType.Hoofdfoto;

                var project = _db.Project.FirstOrDefault(p => p.ProjectId == req.ProjectId);
                if (project != null) project.DefaultPictureId = req.MediaId;
            }

            _db.SaveChanges();
            return Ok(new { success = true });
        }

        public record SortOrderItem(int Id, int Order, int? SectionId);
        public record SortOrderRequest(List<SortOrderItem> Items);

        private string? GetSignedAssetUrl(int docId, string folder)
        {
            var svc = _projectService;
            var resp = svc.GetProjectDoc(docId);
            if (!resp.Success || resp.Value == null) return null;

            var fileName = Path.GetFileName(resp.Value.Filename ?? string.Empty);
            if (string.IsNullOrWhiteSpace(fileName)) return null;

            var baseUrl = Configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
            var readKey = Configuration["StorageApi:ReadApiKey"];
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(readKey))
                return null;

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", readKey);

            var signUrl = $"{baseUrl}/api/assets/{folder}/{Uri.EscapeDataString(fileName)}/sign";
            var response = httpClient.PostAsync(signUrl, content: null).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) return null;

            var payload = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var jsonDoc = JsonDocument.Parse(payload);
            if (!jsonDoc.RootElement.TryGetProperty("url", out var urlElement)) return null;

            var relativeOrAbsolute = urlElement.GetString();
            if (string.IsNullOrWhiteSpace(relativeOrAbsolute)) return null;

            return relativeOrAbsolute.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? relativeOrAbsolute
                : $"{baseUrl}{relativeOrAbsolute}";
        }

        private string? GetSignedAssetUrlByFileName(string fileName, string folder)
        {
            // Gedelegeerd naar de gedeelde IAssetStorageClient (zie UploadAssetToStorageAsync hierboven).
            var storage = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.IAssetStorageClient>();
            return storage.GetSignedUrl(folder, fileName);
        }

        private static string BuildDocThumbFileName(string sourceFileName)
        {
            var baseName = Path.GetFileNameWithoutExtension(sourceFileName);
            return $"thumb_{baseName}.jpg";
        }

        private async Task GenerateDocThumbnailViaStorageAsync(string sourceFileName)
        {
            var safeFileName = Path.GetFileName(sourceFileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(safeFileName))
                return;

            var baseUrl = Configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
            var writeKey = Configuration["StorageApi:WriteApiKey"];
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(writeKey))
                return;

            using var httpClient = CreateStorageHttpClient(writeKey, TimeSpan.FromMinutes(2));

            var endpointCandidates = BuildStorageEndpointCandidates(
                baseUrl,
                $"/api/assets/docs/{Uri.EscapeDataString(safeFileName)}/thumbnail",
                $"/assets/docs/{Uri.EscapeDataString(safeFileName)}/thumbnail",
                $"/docs/{Uri.EscapeDataString(safeFileName)}/thumbnail");

            foreach (var endpoint in endpointCandidates)
            {
                var response = await httpClient.PostAsync(endpoint, content: null);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (IsLikelyAuthRedirectOrLoginPage(response, responseBody))
                {
                    _logger.LogError("Single doc thumbnail generation hit auth/login page. Endpoint: {Endpoint}. Status: {StatusCode}.", endpoint, (int)response.StatusCode);
                    return;
                }

                if (response.IsSuccessStatusCode && IsValidSingleThumbnailResponse(responseBody))
                {
                    return;
                }

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Single doc thumbnail generation got a non-storage success response. Endpoint: {Endpoint}. Body: {Body}", endpoint, responseBody);
                }

                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    return;
                }
            }
        }

        private async Task<string?> UploadAssetFileToStorageAsync(string localPath, string folder, string originalFileName, string contentType)
        {
            await using var stream = System.IO.File.OpenRead(localPath);
            return await UploadAssetToStorageAsync(stream, originalFileName, contentType, folder);
        }

        private async Task<string?> UploadAssetToStorageAsync(IFormFile file, string folder)
        {
            await using var fileStream = file.OpenReadStream();
            return await UploadAssetToStorageAsync(fileStream, file.FileName, file.ContentType, folder);
        }

        private async Task<string?> UploadAssetToStorageAsync(Stream fileStream, string originalFileName, string? contentType, string folder)
        {
            // Gedelegeerd naar de gedeelde IAssetStorageClient (ONDERTEKENEN_VOORSTEL.md §4.7): zelfde
            // endpoint en gedrag als de vroegere inline HttpClient-code, maar nog maar één implementatie.
            var storage = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.IAssetStorageClient>();
            return await storage.UploadAsync(fileStream, originalFileName, contentType, folder);
        }

        private static HttpClient CreateStorageHttpClient(string apiKey, TimeSpan timeout)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };

            var httpClient = new HttpClient(handler)
            {
                Timeout = timeout
            };
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            return httpClient;
        }

        private static bool IsLikelyAuthRedirectOrLoginPage(HttpResponseMessage response, string responseBody)
        {
            if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308)
            {
                return true;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (!contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return true;
            }

            return responseBody.Contains("Sign in to your account", StringComparison.OrdinalIgnoreCase)
                || responseBody.Contains("login", StringComparison.OrdinalIgnoreCase)
                || responseBody.Contains("<html", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidBulkThumbnailResponse(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return false;
            }

            try
            {
                using var jsonDoc = JsonDocument.Parse(responseBody);
                return jsonDoc.RootElement.TryGetProperty("docsProcessed", out _)
                    && jsonDoc.RootElement.TryGetProperty("thumbnailsGenerated", out _)
                    && jsonDoc.RootElement.TryGetProperty("thumbnails", out _);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidSingleThumbnailResponse(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return false;
            }

            try
            {
                using var jsonDoc = JsonDocument.Parse(responseBody);
                return jsonDoc.RootElement.TryGetProperty("thumbFileName", out _);
            }
            catch
            {
                return false;
            }
        }

        private static List<string> BuildStorageEndpointCandidates(string baseUrl, params string[] suffixes)
        {
            var candidates = new List<string>();
            var normalizedBaseUrl = (baseUrl ?? string.Empty).TrimEnd('/');

            foreach (var suffix in suffixes)
            {
                var normalizedSuffix = suffix.StartsWith("/", StringComparison.Ordinal) ? suffix : $"/{suffix}";
                candidates.Add($"{normalizedBaseUrl}{normalizedSuffix}");
            }

            if (Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out var baseUri))
            {
                var origin = baseUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
                var basePath = baseUri.AbsolutePath.Trim('/');

                foreach (var suffix in suffixes)
                {
                    var normalizedSuffix = suffix.StartsWith("/", StringComparison.Ordinal) ? suffix : $"/{suffix}";
                    candidates.Add($"{origin}{normalizedSuffix}");

                    if (!string.IsNullOrWhiteSpace(basePath))
                    {
                        candidates.Add($"{origin}/{basePath}{normalizedSuffix}");
                    }
                }
            }

            return candidates
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void TryDeleteTempFile(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            catch { }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImageFtp(ProjectPictureBO UploadedBO, IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });

            var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif"
    };

            if (!validTypes.Contains(photo.ContentType))
            {
                ModelState.AddModelError("ImageUpload", "Verkeerd type gekozen, kies een gif, jpeg of png");
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
            }

            // 1) Lokaal temp-pad
            var tempRoot = (Configuration["URL:LocalTempPath"] ?? Path.Combine(Path.GetTempPath(), "project-pictures")).Trim();
            Directory.CreateDirectory(tempRoot);

            // 2) FTP root-pad
            var baseFtp = (Configuration["URL:PicturesFtpPath"] ?? string.Empty).TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseFtp))
            {
                ModelState.AddModelError("", "FTP-pad ontbreekt (URL:PicturesFtpPath).");
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
            }

            await using var ftp = await ConnectAsync();
            if (ftp == null)
            {
                ModelState.AddModelError("", "FTP-verbinding mislukt.");
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
            }

            var filename = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".jpg";

            // 3) Lokaal opslaan
            var localOriginal = Path.Combine(tempRoot, filename);
            var local447 = Path.Combine(tempRoot, "447_" + filename);
            var local800 = Path.Combine(tempRoot, "800_" + filename);

            try
            {
                using (var fs = new FileStream(localOriginal, FileMode.Create))
                {
                    await photo.CopyToAsync(fs);
                }

                System.IO.File.Copy(localOriginal, local447, true);
                System.IO.File.Copy(localOriginal, local800, true);

                // 4) Bewerken (met ImageSharp helpers)
                ScaleAndCropImage(local447, 447, 447);
                ScaleAndCropImage(local800, 800, 800);
                ScaleImage(localOriginal, 1280, 960);

                // 5) Zorg dat remote directories bestaan
                var remoteRoot = $"{baseFtp}";
                var remote447 = $"{baseFtp}/447";
                var remote800 = $"{baseFtp}/800";

                await EnsureDirectoryAsync(ftp, remoteRoot);
                await EnsureDirectoryAsync(ftp, remote447);
                await EnsureDirectoryAsync(ftp, remote800);

                // 6) Upload bestanden
                var ok1 = await UploadAsync(ftp, localOriginal, $"{remoteRoot}/{filename}");
                var ok2 = await UploadAsync(ftp, local447, $"{remote447}/{filename}");
                var ok3 = await UploadAsync(ftp, local800, $"{remote800}/{filename}");

                if (!(ok1 && ok2 && ok3))
                {
                    await DeleteFileAsync(ftp, $"{remoteRoot}/{filename}");
                    await DeleteFileAsync(ftp, $"{remote447}/{filename}");
                    await DeleteFileAsync(ftp, $"{remote800}/{filename}");

                    ModelState.AddModelError("ImageUpload", "Upload naar server is mislukt.");
                    return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
                }

                // 7) DB opslaan
                var picture = new ProjectPictureBO
                {
                    Name = filename,
                    Caption = UploadedBO.Caption,
                    ProjectId = UploadedBO.ProjectId,
                    Type = UploadedBO.Type,
                    DateTimeUploaded = DateTime.Now
                };

                var service = _projectService;
                var response = service.InsertUpdatePicture(picture);

                if (picture.Type == PictureType.Hoofdfoto && response?.Messages != null)
                {
                    foreach (var msg in response.Messages)
                    {
                        if (msg.Type == MessageType.Value && int.TryParse(msg.Message, out var pictureId))
                        {
                            _ = service.SetDefaultProjectPicture(UploadedBO.ProjectId, pictureId);
                        }
                    }
                }
            }
            finally
            {
                TryDelete(localOriginal);
                TryDelete(local447);
                TryDelete(local800);
            }

            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });

            // --- helpers ---
            void TryDelete(string p)
            {
                try { if (System.IO.File.Exists(p)) System.IO.File.Delete(p); } catch { /* ignore/log */ }
            }
        }

        public void ScaleAndCropImage(string imagePath, int maxWidth, int maxHeight, int quality = 80)
        {
            using var image = SixLabors.ImageSharp.Image.Load(imagePath);
            double ratioX = (double)maxWidth / image.Width;
            double ratioY = (double)maxHeight / image.Height;
            double ratio  = Math.Max(ratioX, ratioY);

            // Ceiling i.p.v. een afkappende cast: bij een afkappende (int) cast kan het herschaalde
            // beeld door drijvendekomma-afronding 1px SMALLER uitkomen dan maxWidth/maxHeight (bv.
            // 799 i.p.v. 800), waardoor het crop-rechthoek hieronder buiten de nieuwe beeldgrenzen valt
            // en ImageSharp een ArgumentException gooit ("Crop rectangle should be smaller than the
            // source bounds"). Ceiling garandeert newWidth/newHeight altijd >= maxWidth/maxHeight.
            int newWidth  = (int)Math.Ceiling(image.Width  * ratio);
            int newHeight = (int)Math.Ceiling(image.Height * ratio);
            image.Mutate(x => x.Resize(newWidth, newHeight));

            var cropX = (newWidth  - maxWidth)  / 2;
            var cropY = (newHeight - maxHeight) / 2;
            image.Mutate(x => x.Crop(new Rectangle(cropX, cropY, maxWidth, maxHeight)));

            var webpPath = Path.ChangeExtension(imagePath, ".webp");
            image.Save(webpPath, new WebpEncoder { Quality = quality });
            if (!string.Equals(imagePath, webpPath, StringComparison.OrdinalIgnoreCase))
                TryDeleteTempFile(imagePath);
        }

        public void ScaleImage(string imagePath, int maxWidth, int maxHeight, int quality = 80)
        {
            using var image = SixLabors.ImageSharp.Image.Load(imagePath);
            double ratioX = (double)maxWidth  / image.Width;
            double ratioY = (double)maxHeight / image.Height;
            double ratio  = Math.Min(ratioX, ratioY); // Min = fit inside, geen bijsnijden

            int newWidth  = (int)(image.Width  * ratio);
            int newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(newWidth, newHeight));

            var webpPath = Path.ChangeExtension(imagePath, ".webp");
            image.Save(webpPath, new WebpEncoder { Quality = quality });
            if (!string.Equals(imagePath, webpPath, StringComparison.OrdinalIgnoreCase))
                TryDeleteTempFile(imagePath);
        }
        private static bool IsValidImage(IFormFile file) => _validImageTypes.Contains(file.ContentType);


        private static void EnsureDir(string dir)
        {
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        // ==== FTP CONNECT ====
        public async Task<AsyncFtpClient?> ConnectToFtpAsync()
        {
            var host = Configuration["FTP:Host"];
            var user = Configuration["FTP:User"];
            var pass = Configuration["FTP:Password"];
            var port = int.TryParse(Configuration["FTP:Port"], out var p) ? p : 21;
            var useFtps = bool.TryParse(Configuration["FTP:UseFtps"], out var ftps) && ftps;
            var validateCert = bool.TryParse(Configuration["FTP:ValidateCert"], out var vc) && vc;

            var client = new AsyncFtpClient(host, new NetworkCredential(user, pass), port);

            // Config
            client.Config.DataConnectionType = FtpDataConnectionType.PASV; // passive
            if (useFtps)
            {
                client.Config.EncryptionMode = FtpEncryptionMode.Explicit;  // AUTH TLS
                client.Config.DataConnectionEncryption = true;

                client.ValidateCertificate += (control, e) =>
                {
                    e.Accept = !validateCert || e.PolicyErrors == SslPolicyErrors.None;
                };
            }
            else
            {
                client.Config.EncryptionMode = FtpEncryptionMode.None;
                client.Config.DataConnectionEncryption = false;
            }

            try
            {
                await client.Connect();  // AsyncFtpClient: async connect heet 'Connect()'
                return client;
            }
            catch (Exception ex)
            {
                // jouw logging hier
                Console.WriteLine("FTP connect failed: " + ex.Message);
                await client.DisposeAsync();
                return null;
            }
        }

        // ==== LOCAL DIR CHECK ====
        public void CheckDir(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        // ==== REMOTE DIR CHECK/CREATE ====
        public async Task CheckDirFtpAsync(string remoteDir, AsyncFtpClient ftp)
        {
            // Maakt recursief aan; geen chdir nodig bij FluentFTP
            await ftp.CreateDirectory(remoteDir, true);
        }

        // ==== LOKALE FOTO'S VERWIJDEREN (ongewijzigd, puur filesystem) ====
        public void DeletePictureFile(int id)
        {
            // Bestanden worden beheerd door de Storage API; lokale cleanup is niet meer van toepassing.
        }

        // ==== REMOTE FILE DELETE ====
        public async Task<bool> DeleteFtpFileAsync(string remoteDir, string filename, AsyncFtpClient ftp)
        {
            try
            {
                var remotePath = $"{remoteDir.TrimEnd('/')}/{filename}";
                if (await ftp.FileExists(remotePath))
                {
                    await ftp.DeleteFile(remotePath);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FTP delete failed: {ex.Message}");
                return false;
            }
        }


        //FTP HELPERS
        private AsyncFtpClient CreateClient()
        {
            var host = Configuration["FTP:Host"];
            var user = Configuration["FTP:User"];
            var pass = Configuration["FTP:Password"];
            var port = int.TryParse(Configuration["FTP:Port"], out var p) ? p : 21;
            var useFtps = bool.TryParse(Configuration["FTP:UseFtps"], out var ftps) && ftps;
            var validateCert = bool.TryParse(Configuration["FTP:ValidateCert"], out var vc) && vc;

            var client = new AsyncFtpClient(host, new NetworkCredential(user, pass), port);

            if (useFtps)
            {
                client.Config.EncryptionMode = FtpEncryptionMode.Explicit;   // FTPS (AUTH TLS)
                client.Config.DataConnectionEncryption = true;
                client.ValidateCertificate += (control, e) =>
                {
                    e.Accept = !validateCert || e.PolicyErrors == SslPolicyErrors.None;
                };
            }
            else
            {
                client.Config.EncryptionMode = FtpEncryptionMode.None;
                client.Config.DataConnectionEncryption = false;
            }

            client.Config.DataConnectionType = FtpDataConnectionType.PASV;  // passive

            return client;
        }

        public async Task<AsyncFtpClient> ConnectAsync()
        {
            var client = CreateClient();
            await client.Connect();   // AsyncFtpClient: async method heet Connect()
            return client;
        }

        public async Task EnsureDirectoryAsync(AsyncFtpClient client, string remoteDir)
            => await client.CreateDirectory(remoteDir, true);   // async

        public async Task<bool> UploadAsync(AsyncFtpClient client, string localPath, string remoteFullPath)
        {
            var status = await client.UploadFile(localPath, remoteFullPath,
                FtpRemoteExists.Overwrite, true);
            return status == FtpStatus.Success;
        }

        public async Task<bool> DeleteFileAsync(AsyncFtpClient client, string remoteFullPath)
        {
            try
            {
                if (await client.FileExists(remoteFullPath))
                    await client.DeleteFile(remoteFullPath);
                return true;
            }
            catch { return false; }
        }

        //SALES HELPERS
        private static decimal Percent(decimal amount, decimal pct)
=> Math.Round(amount * (pct / 100m), 2, MidpointRounding.AwayFromZero);

        private static decimal SafePos(decimal? v) => v.HasValue && v.Value > 0 ? v.Value : 0m;

        private static decimal SafeLand(UnitBO u) => u.LandValue ?? 0m;

        private static decimal SafeConstruction(UnitBO u) =>
            (u.ConstructionValues?.Sum(x => x.Value ?? 0m)) ?? 0m;

        private static decimal CalculateNotaryFeesFromTotals(decimal totalNetLand, decimal totalNetBuild, bool mixedVatRegistration)
        {
            decimal baseValue = mixedVatRegistration
                ? (totalNetLand + totalNetBuild)
                : (totalNetLand + (totalNetBuild / 2m));

            if (baseValue <= 0m) return 0m;

            var parts = new (decimal amount, decimal pct)[]
            {
        (  7500m, 4.56m),
        ( 10000m, 2.85m),
        ( 12500m, 2.28m),
        ( 15495m, 1.71m),
        ( 18600m, 1.14m),
        (186000m, 0.57m),
            };
            const decimal pctRest = 0.057m;

            decimal remaining = baseValue;
            decimal fee = 0m;

            foreach (var (amount, pct) in parts)
            {
                if (remaining <= 0m) break;
                var take = Math.Min(remaining, amount);
                fee += take * (pct / 100m);
                remaining -= take;
            }

            if (remaining > 0m) fee += remaining * (pctRest / 100m);

            return Math.Round(fee, 2, MidpointRounding.AwayFromZero);
        }
        private static bool ShouldDefaultInclude(UnitBO u)
        {
            // Zorg dat UnitBO.Type.GroupId gevuld is in je translator
            var gid = u?.Type?.GroupId ?? 0;
            return gid == 1 || gid == 4; // woning/appartement of commerciële ruimte
        }

    }
}
