using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Projecten;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    /// <summary>gl-v2 eenheidsformulier (design-handoff 16b "Eenheid bewerken" + 16c "Nieuwe eenheid").
    /// Eigen bestand (partial) i.p.v. de 12k regels van ProjectenController.cs verder te laten groeien:
    /// de legacy AddUnit/EditUnit-acties blijven daar ongewijzigd, ze schakelen enkel door naar
    /// <see cref="UnitFormV2Get"/> zodra de gl-v2-lay-out actief is.</summary>
    public partial class ProjectenController
    {
        private const string UnitFormV2View = "UnitFormV2";

        // ── GET ─────────────────────────────────────────────────────────────────────────────────
        /// <summary>Bouwt het formulier voor een nieuwe (unitid = 0, optioneel gekopieerd van
        /// <paramref name="copyFrom"/>) of bestaande eenheid. <c>null</c> = laat de legacy view het doen
        /// (een KOPPELING-pseudo-eenheid, <c>Units.IsLink</c>, heeft een eigen legacy formulier).</summary>
        private ActionResult? UnitFormV2Get(int projectid, int unitid, int copyFrom, string? returnUrl)
        {
            var vm = BuildUnitFormV2Vm(projectid, unitid, copyFrom);
            if (vm == null) return null;

            vm.ReturnUrl = ResolveUnitFormReturnUrl(vm.ProjectId, returnUrl);
            FillUnitFormV2Lists(vm);

            SetUnitFormV2Breadcrumb(vm);

            return View(UnitFormV2View, vm);
        }

        /// <summary>Kruimelpad volgens 16b: Projecten / project / Eenheden / Lot 1 (of "Nieuw").</summary>
        private void SetUnitFormV2Breadcrumb(UnitFormV2Vm vm)
        {
            var index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = index };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", vm.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = vm.ProjectId }
            };
            var projectUnits = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailUnits", "Projecten", "Eenheden")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = vm.ProjectId }
            };
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(
                vm.IsNew ? "AddUnit" : "EditUnit", "Projecten", vm.IsNew ? "Nieuw" : (vm.Unit.Name ?? "Eenheid"))
            {
                Parent = projectUnits,
                RouteValues = vm.IsNew ? new { projectid = vm.ProjectId } : new { projectid = vm.ProjectId, unitid = vm.Unit.Id }
            };

        }

        private UnitFormV2Vm? BuildUnitFormV2Vm(int projectid, int unitid, int copyFrom)
        {
            var vm = new UnitFormV2Vm { ProjectId = projectid };

            if (unitid > 0)
            {
                var resp = _unitService.GetUnitById(unitid);
                if (!resp.Success || resp.Value is null) return null;
                if (resp.Value.IsLink) return null;

                vm.Unit = resp.Value;
                vm.ProjectId = vm.Unit.ProjectId;
                vm.SelectedType = vm.Unit.Type?.Id ?? 0;
                vm.SelectedGroupType = vm.Unit.Type?.GroupId ?? 0;

                // Bewust GEEN EnsureDefaultFinishingOption meer (de legacy pagina zette bij het openen alle
                // constructieprijzen onder een "Standaard"-afwerking): zonder afwerkingen zijn het gewoon
                // constructieprijzen onder de grondwaarde, en een afwerking bestaat pas als iemand er een
                // toevoegt — anders wordt elke eenheid voor de publieke site een eenheid "met opties".
                var fo = _unitService.GetFinishingOptions(unitid);
                vm.FinishingOptions = fo.Success && fo.Values is not null ? fo.Values : new List<UnitFinishingOptionBO>();
                var rooms = _unitService.GetRooms(unitid);
                vm.Rooms = (rooms.Success && rooms.Values is not null ? rooms.Values : new List<RoomBO>()).OrderBy(r => r.Id).ToList();

                var cvs = _unitService.GetConstructionValues(unitid);
                var baseCvs = cvs.Success && cvs.Values is not null
                    ? cvs.Values.Where(cv => cv.FinishingOptionId is null).ToList()
                    : new List<UnitConstructionValueBO>();
                if (vm.FinishingOptions.Count == 0)
                {
                    vm.BaseConstructionValues = baseCvs;
                    if (vm.BaseConstructionValues.Count == 0) vm.BaseConstructionValues.Add(new UnitConstructionValueBO());
                }
                else
                {
                    vm.UnlistedBaseValue = baseCvs.Sum(cv => cv.Value ?? 0m);
                }
                return vm;
            }

            vm.Unit = new UnitBO { ProjectId = projectid };

            if (copyFrom > 0)
            {
                var src = _unitService.GetUnitById(copyFrom);
                if (src.Success && src.Value is not null && src.Value.ProjectId == projectid && !src.Value.IsLink)
                {
                    var s = src.Value;
                    // 16c: "neem type, bedragen, oppervlaktes en indeling over — enkel naam, adres en
                    // kadaster vul je opnieuw in." Verdieping/aandeel/EAN/watermeter zijn per eenheid uniek.
                    vm.Unit.Type = new UnitTypeBO { Id = s.Type.Id, GroupId = s.Type.GroupId, Name = s.Type.Name };
                    vm.SelectedType = s.Type.Id;
                    vm.SelectedGroupType = s.Type.GroupId;
                    vm.Unit.LandValue = s.LandValue;
                    vm.Unit.Surface = s.Surface;
                    vm.Unit.GroundSurface = s.GroundSurface;
                    vm.CopyFromUnitId = s.Id;
                    vm.CopyFromName = s.Name ?? "";

                    var fo = _unitService.GetFinishingOptions(s.Id);
                    var options = fo.Success && fo.Values is not null ? fo.Values : new List<UnitFinishingOptionBO>();
                    if (options.Count == 0)
                    {
                        // Bron zonder afwerkingen: de constructieprijzen komen als constructieprijzen mee.
                        var baseCvs = _unitService.GetConstructionValues(s.Id);
                        vm.BaseConstructionValues = baseCvs.Success && baseCvs.Values is not null
                            ? baseCvs.Values.Where(cv => cv.FinishingOptionId is null).ToList()
                            : new List<UnitConstructionValueBO>();
                        foreach (var cv in vm.BaseConstructionValues)
                        {
                            cv.Id = 0;
                            cv.UnitId = 0;
                            cv.FinishingOptionId = null;
                            cv.ValueSold = null;
                        }
                    }
                    foreach (var o in options)
                    {
                        o.Id = 0;
                        o.UnitId = 0;
                        foreach (var cv in o.ConstructionValues)
                        {
                            cv.Id = 0;
                            cv.UnitId = 0;
                            cv.FinishingOptionId = null;
                            cv.ValueSold = null;
                        }
                    }
                    vm.FinishingOptions = options;

                    var rooms = _unitService.GetRooms(s.Id);
                    vm.Rooms = (rooms.Success && rooms.Values is not null ? rooms.Values : new List<RoomBO>())
                        .OrderBy(r => r.Id)
                        .Select(r => new RoomBO { Type = r.Type, Number = r.Number, Surface = r.Surface, Remark = r.Remark })
                        .ToList();
                }
            }

            // Een nieuw formulier begint met één lege constructieprijs onder de grondwaarde, GEEN afwerking:
            // afwerkingen zijn optioneel (afgewerkt/casco/…), en een eenheid met minstens één afwerking
            // wordt voor de publieke site een eenheid "met opties".
            if (vm.FinishingOptions.Count == 0 && vm.BaseConstructionValues.Count == 0)
                vm.BaseConstructionValues.Add(new UnitConstructionValueBO());
            return vm;
        }

        /// <summary>Vult alles wat NIET uit het formulier terugkomt (keuzelijsten, projectgegevens,
        /// gekoppelde eenheden, volgende eenheid) — ook nodig om bij een ongeldige POST opnieuw te renderen.</summary>
        private void FillUnitFormV2Lists(UnitFormV2Vm vm)
        {
            vm.ProjectName = _projectService.GetProjectNameById(vm.ProjectId) ?? "";
            vm.ProjectLandShare = (int)_projectService.GetProjectLandshareById(vm.ProjectId);

            var groups = _unitService.GetUnitGroupTypes();
            vm.GroupTypes = groups.Success && groups.Values is not null ? groups.Values : new List<UnitGroupTypeBO>();
            var types = _unitService.GetUnitTypesByGroupId(vm.SelectedGroupType);
            vm.Types = types.Success && types.Values is not null ? types.Values : new List<UnitTypeBO>();

            var attachable = vm.IsNew
                ? _unitService.GetUnitsByProjectIdForSelectAttachedUnit(vm.ProjectId)
                : _unitService.GetUnitsByProjectIdForSelectAttachedUnit(vm.ProjectId, vm.Unit.Id);
            vm.AttachableUnits = attachable.Success && attachable.Values is not null ? attachable.Values : new List<IdNameBO>();

            var pg = _projectService.GetProjectPaymentGroupsForSelect(vm.ProjectId);
            vm.PaymentGroups = pg.Success && pg.Values is not null ? pg.Values : new List<IdNameBO>();

            var all = _unitService.GetUnitsByProjectId(vm.ProjectId);
            var allUnits = all.Success && all.Values is not null ? all.Values : new List<UnitBO>();
            vm.LandShareOthers = (int)allUnits.Where(u => u.Id != vm.Unit.Id).Sum(u => u.Landshare ?? 0m);
            vm.ProjectUnitCount = allUnits.Count(u => !u.IsLink);

            if (vm.IsNew)
            {
                var sources = _unitService.GetUnitsByProjectIdForSelect(vm.ProjectId, false);
                vm.CopySources = sources.Success && sources.Values is not null ? sources.Values : new List<IdNameBO>();
                return;
            }

            vm.ExecutionPlans = BuildUnitExecutionPlansVm(vm.Unit.Id).GetAwaiter().GetResult();

            // Status (alleen-lezen) — zelfde indeling als de lijst (BuildDetailUnitsV2Vm).
            var isSecondary = vm.IsSecondaryUnit;
            if (vm.Unit.ClientAccountId is not null) { vm.StatusLabel = "Verkocht"; vm.StatusTone = "is-positive"; }
            else if (vm.Unit.IsOption) { vm.StatusLabel = "In optie"; vm.StatusTone = "is-attention"; }
            else if (isSecondary) { vm.StatusLabel = "Los te koop"; vm.StatusTone = "is-info"; }
            else { vm.StatusLabel = "Beschikbaar"; vm.StatusTone = "is-neutral"; }

            // Volgende eenheid in alfanumerieke volgorde ("Opslaan en naar Lot 2").
            var ordered = allUnits.Where(u => !u.IsLink)
                .OrderBy(u => u.Name, new ServiceCore.Helpers.AlphanumComparator())
                .ToList();
            var idx = ordered.FindIndex(u => u.Id == vm.Unit.Id);
            if (idx >= 0 && idx + 1 < ordered.Count)
            {
                vm.NextUnitId = ordered[idx + 1].Id;
                vm.NextUnitName = ordered[idx + 1].Name;
            }

            // Gekoppelde eenheden = wat onder deze eenheid hangt (Units.AttachedUnitId, zie AttachUnitV2).
            var treeResp = _unitService.GetUnitsWithAttachedByProjectId(vm.ProjectId);
            var node = treeResp.Success && treeResp.Values is not null
                ? treeResp.Values.FirstOrDefault(n => n.Unit.Id == vm.Unit.Id)
                : null;
            if (node != null && node.AttachedUnits.Count > 0)
            {
                var childIds = node.AttachedUnits.Select(c => c.Id).Distinct().ToList();
                var moneyResp = _unitService.GetUnitsById(childIds);
                var money = new Dictionary<int, UnitBO>();
                if (moneyResp.Success && moneyResp.Values is not null)
                    foreach (var u in moneyResp.Values) money[u.Id] = u;
                foreach (var child in node.AttachedUnits.OrderBy(c => c.Name, new ServiceCore.Helpers.AlphanumComparator()))
                {
                    var m = money.TryGetValue(child.Id, out var found) ? found : child;
                    var levelLabel = UnitLevelLabelV2(child.Level, child.Type?.GroupId ?? 0);
                    var typeLine = string.Join(" · ", new[] { child.Type?.Name, levelLabel == "—" ? null : levelLabel }
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s!.ToLowerInvariant()));
                    vm.LinkedUnits.Add(new UnitFormV2LinkedUnit
                    {
                        UnitId = child.Id,
                        Name = child.Name ?? "",
                        TypeLine = typeLine,
                        Price = UnitPriceV2(m, false).Price
                    });
                }
            }
        }

        /// <summary>Terugkeer-url: expliciet meegegeven (bv. bij "Opslaan en naar Lot 2", zodat de keten
        /// niet elke keer de vorige bewerkpagina als "terug" onthoudt), anders de Referer van dezelfde host
        /// die niet zelf een AddUnit/EditUnit is, anders de eenhedenlijst.</summary>
        private string ResolveUnitFormReturnUrl(int projectId, string? returnUrl)
        {
            var fallback = Url.Action("DetailUnits", "Projecten", new { projectid = projectId }) ?? "/";
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return returnUrl;

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrWhiteSpace(referer)
                && Uri.TryCreate(referer, UriKind.Absolute, out var uri)
                && string.Equals(uri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase)
                && !uri.AbsolutePath.Contains("/EditUnit", StringComparison.OrdinalIgnoreCase)
                && !uri.AbsolutePath.Contains("/AddUnit", StringComparison.OrdinalIgnoreCase))
            {
                return uri.PathAndQuery;
            }
            return fallback;
        }

        // ── POST ────────────────────────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsUnits)]
        public async Task<IActionResult> SaveUnitV2(
            UnitFormV2Vm model,
            IFormFile? file,
            List<IFormFile>? executionPlanFiles,
            List<string>? executionPlanNames,
            List<int>? deleteExecutionPlanIds)
        {
            var isNew = model.Unit.Id == 0;
            var projectId = model.ProjectId;

            // ── Validatie ───────────────────────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(model.Unit.Name))
                ModelState.AddModelError("Unit.Name", "Geef de eenheid een naam.");
            if (model.SelectedType == 0)
                ModelState.AddModelError("SelectedType", "Kies een type en een subtype.");

            var validTypes = new[] { "application/pdf" };
            if (file != null && file.Length > 0 && !validTypes.Contains(file.ContentType))
                ModelState.AddModelError("file", "Het verkoopplan moet een pdf zijn.");
            if (executionPlanFiles != null && executionPlanFiles.Any(f => f != null && f.Length > 0 && !validTypes.Contains(f.ContentType)))
                ModelState.AddModelError("executionPlanFiles", "Alle uitvoeringsplannen moeten pdf-bestanden zijn.");

            foreach (var (room, i) in model.Rooms.Select((r, i) => (r, i)))
            {
                var blank = RoomIsBlank(room);
                if (!blank && (int)room.Type == 0)
                    ModelState.AddModelError($"Rooms[{i}].Type", "Kies een ruimtetype.");
            }

            // Dezelfde blokkade als de legacy pagina (zie DESIGN.md, "De twee koppelmechanismen op
            // Units"): een lid van een KOPPELING mag niet óók onder een hoofdeenheid hangen.
            UnitBO? existing = null;
            if (!isNew)
            {
                var existingResp = _unitService.GetUnitById(model.Unit.Id);
                if (!existingResp.Success || existingResp.Value is null || existingResp.Value.ProjectId != projectId)
                    return NotFound();
                if (existingResp.Value.IsLink)
                    return RedirectToAction("EditUnit", "Projecten", new { projectid = projectId, unitid = model.Unit.Id });
                existing = existingResp.Value;

                if (model.Unit.AttachedUnitsId is not null and not 0)
                {
                    if (model.Unit.AttachedUnitsId == existing.Id)
                        ModelState.AddModelError("Unit.AttachedUnitsId", "Een eenheid kan niet aan zichzelf hangen.");
                    else if (existing.LinkedUnitId is not null and not 0)
                        ModelState.AddModelError("Unit.AttachedUnitsId",
                            $"{existing.Name} is al lid van een koppeling en kan daarom niet ook onder een andere eenheid gehangen worden.");
                }
            }

            if (!ModelState.IsValid)
            {
                if (!isNew)
                {
                    // Niet gepost, wel nodig om de pagina opnieuw op te bouwen (status, verkoopplan-rij).
                    model.Unit.ClientAccountId = existing?.ClientAccountId;
                    model.Unit.Plan = existing?.Plan;
                }
                model.Unit.Type ??= new UnitTypeBO();
                model.Unit.Type.Id = model.SelectedType;
                if (model.Unit.Type.GroupId == 0) model.Unit.Type.GroupId = model.SelectedGroupType;
                FillUnitFormV2Lists(model);
                if (!isNew) model.Unit.IsOption = existing!.IsOption;
                SetUnitFormV2Breadcrumb(model);
                AddMessage("error", "De eenheid is nog niet opgeslagen — kijk de aangeduide velden na.", "Fout!");
                return View(UnitFormV2View, model);
            }

            // ── Eenheid zelf: enkel de velden die dit formulier bezit, over de bestaande eenheid heen —
            //    zodat verkoopgegevens (klant, verkochte bedragen, koppeling, betalingsgroep) nooit
            //    stil gewist worden door een veld dat hier niet staat. ─────────────────────────────
            var unit = existing ?? new UnitBO();
            unit.ProjectId = projectId;
            unit.Name = model.Unit.Name.Trim();
            unit.Level = model.Unit.Level;
            unit.Landshare = model.Unit.Landshare;
            unit.Street = model.Unit.Street;
            unit.HouseNumber = model.Unit.HouseNumber;
            unit.BusNumber = model.Unit.BusNumber;
            unit.PreKad = model.Unit.PreKad;
            unit.EanGas = model.Unit.EanGas;
            unit.EanElektriciteit = model.Unit.EanElektriciteit;
            unit.WatermeterNummer = model.Unit.WatermeterNummer;
            unit.LandValue = model.Unit.LandValue;
            unit.Surface = model.Unit.Surface;
            unit.GroundSurface = model.Unit.GroundSurface;
            unit.AttachedUnitsId = model.Unit.AttachedUnitsId is null or 0 ? null : model.Unit.AttachedUnitsId;
            unit.Type ??= new UnitTypeBO();
            unit.Type.Id = model.SelectedType;

            if (file != null && file.Length > 0)
            {
                var uploaded = await UploadAssetToStorageAsync(file, "plans");
                if (string.IsNullOrWhiteSpace(uploaded))
                {
                    ModelState.AddModelError("file", "Het verkoopplan uploaden naar de opslag is mislukt.");
                    FillUnitFormV2Lists(model);
                    SetUnitFormV2Breadcrumb(model);
                    AddMessage("error", "Het verkoopplan kon niet geüpload worden.", "Fout!");
                    return View(UnitFormV2View, model);
                }
                unit.Plan = uploaded;
            }
            else if (model.RemovePlan)
            {
                unit.Plan = null;
            }

            var saved = _unitService.InsertUpdateUnit(unit);
            if (!saved.Success)
            {
                AddMessage("error", "De eenheid is NIET opgeslagen, gelieve opnieuw te proberen of contact op te nemen met de administrator.", "Fout!");
                ModelState.AddModelError("", saved.Messages.FirstOrDefault()?.Message ?? "Opslaan mislukt.");
                FillUnitFormV2Lists(model);
                SetUnitFormV2Breadcrumb(model);
                return View(UnitFormV2View, model);
            }
            var unitId = saved.InsertedId;

            // Vanaf hier bestaat de eenheid: een latere fout stuurt naar de bewerkpagina van die
            // eenheid (en niet terug naar "nieuw", waar een tweede POST een dubbele eenheid zou maken).
            try
            {
                SyncUnitFinishingOptionsV2(unitId, isNew, model.FinishingOptions, model.BaseConstructionValues);
                SyncUnitRoomsV2(unitId, isNew, model.Rooms);
                await SyncUnitExecutionPlansV2(unitId, executionPlanFiles, executionPlanNames, deleteExecutionPlanIds);
            }
            catch (Exception ex)
            {
                AddMessage("error", "De eenheid is bewaard, maar niet alles kon bijgewerkt worden: " + ex.Message, "Fout!");
                return RedirectToAction("EditUnit", "Projecten", new { projectid = projectId, unitid = unitId });
            }

            AddMessage("success", isNew ? "De eenheid is aan het project toegevoegd." : "De eenheid is met succes bijgewerkt.", "Geslaagd!");

            if (string.Equals(model.SaveMode, "next", StringComparison.OrdinalIgnoreCase))
            {
                if (isNew)
                    return RedirectToAction("AddUnit", "Projecten", new { projectid = projectId, returnUrl = model.ReturnUrl });

                var nextId = NextUnitIdV2(projectId, unitId);
                if (nextId != null)
                    return RedirectToAction("EditUnit", "Projecten", new { projectid = projectId, unitid = nextId, returnUrl = model.ReturnUrl });
            }

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction("DetailUnits", "Projecten", new { projectid = projectId });
        }

        private int? NextUnitIdV2(int projectId, int currentId)
        {
            var all = _unitService.GetUnitsByProjectId(projectId);
            if (!all.Success || all.Values is null) return null;
            var ordered = all.Values.Where(u => !u.IsLink)
                .OrderBy(u => u.Name, new ServiceCore.Helpers.AlphanumComparator())
                .ToList();
            var idx = ordered.FindIndex(u => u.Id == currentId);
            return idx >= 0 && idx + 1 < ordered.Count ? ordered[idx + 1].Id : null;
        }

        private static bool RoomIsBlank(RoomBO r) =>
            (int)r.Type == 0 && r.Number == 0 && r.Surface == 0m && string.IsNullOrWhiteSpace(r.Remark);

        private static bool CvHasContent(UnitConstructionValueBO cv) =>
            cv.Value.HasValue || !string.IsNullOrWhiteSpace(cv.Description) || (cv.PaymentGroupId ?? 0) > 0;

        /// <summary>Synchroniseert de afwerkingsblokken + hun bouwwaarderegels met wat er gepost is.
        /// Een gepost id dat niet bij deze eenheid hoort (foutief/gekopieerd) wordt als nieuw behandeld.
        /// Bouwwaarderegels zonder afwerkingsoptie (legacy "basis") blijven onaangeroerd — het formulier
        /// toont ze niet als blok, dus het mag ze ook nooit verwijderen.</summary>
        private void SyncUnitFinishingOptionsV2(int unitId, bool isNew, List<UnitFinishingOptionBO> posted, List<UnitConstructionValueBO> postedBase)
        {
            var existingOptions = new List<UnitFinishingOptionBO>();
            var existingCvs = new List<UnitConstructionValueBO>();
            if (!isNew)
            {
                var eo = _unitService.GetFinishingOptions(unitId);
                if (eo.Success && eo.Values is not null) existingOptions = eo.Values;
                var ec = _unitService.GetConstructionValues(unitId);
                if (ec.Success && ec.Values is not null) existingCvs = ec.Values;
            }
            var existingOptionIds = existingOptions.Select(o => o.Id).ToHashSet();
            var existingCvIds = existingCvs.Select(c => c.Id).ToHashSet();

            var options = posted
                .Where(o => !string.IsNullOrWhiteSpace(o.Name) || o.ConstructionValues.Any(CvHasContent))
                .ToList();
            if (options.Count > 0)
            {
                var defaultOption = options.FirstOrDefault(o => o.IsDefault) ?? options[0];
                foreach (var o in options) o.IsDefault = ReferenceEquals(o, defaultOption);
            }

            var keptOptionIds = new HashSet<int>();
            var keptCvIds = new HashSet<int>();

            // Zonder afwerkingen: de constructieprijzen staan los onder de grondwaarde (FinishingOptionId
            // = null). Eerst bewaren, vóór de afwerkingen hieronder verwijderd worden: verhuisde regels
            // (laatste afwerking verwijderd → regels terug naar de basis) dragen hun bestaande id.
            // Mét afwerkingen wordt een gepost basislijstje genegeerd (het formulier verhuist die regels
            // zelf naar de eerste afwerking) en blijven bestaande basisregels onaangeroerd.
            if (options.Count == 0)
            {
                foreach (var cv in (postedBase ?? new List<UnitConstructionValueBO>()).Where(CvHasContent))
                {
                    if (cv.Id != 0 && !existingCvIds.Contains(cv.Id)) { cv.Id = 0; cv.ValueSold = null; }
                    cv.UnitId = unitId;
                    cv.FinishingOptionId = null;
                    if ((cv.PaymentGroupId ?? 0) == 0) cv.PaymentGroupId = null;
                    var rb = _unitService.InsertUpdateConstructionValue(cv);
                    if (!rb.Success) throw new ApplicationException(rb.Messages.FirstOrDefault()?.Message ?? "Constructieprijs opslaan mislukt.");
                    keptCvIds.Add(rb.InsertedId);
                }
                var orphanBase = existingCvs.Where(c => c.FinishingOptionId is null && !keptCvIds.Contains(c.Id)).Select(c => c.Id).ToList();
                if (orphanBase.Count > 0)
                {
                    var rdb = _unitService.DeleteConstructionValues(orphanBase);
                    if (!rdb.Success) throw new ApplicationException(rdb.Messages.FirstOrDefault()?.Message ?? "Constructieprijs verwijderen mislukt.");
                }
            }
            var order = 0;
            foreach (var option in options)
            {
                if (option.Id != 0 && !existingOptionIds.Contains(option.Id)) option.Id = 0;
                option.UnitId = unitId;
                option.SortOrder = order++;
                if (string.IsNullOrWhiteSpace(option.Name)) option.Name = "Afwerking " + order;
                option.Name = option.Name.Trim();

                var ro = _unitService.InsertUpdateFinishingOption(option);
                if (!ro.Success) throw new ApplicationException(ro.Messages.FirstOrDefault()?.Message ?? "Afwerking opslaan mislukt.");
                var optionId = ro.InsertedId;
                keptOptionIds.Add(optionId);

                foreach (var cv in option.ConstructionValues.Where(CvHasContent))
                {
                    if (cv.Id != 0 && !existingCvIds.Contains(cv.Id)) { cv.Id = 0; cv.ValueSold = null; }
                    cv.UnitId = unitId;
                    cv.FinishingOptionId = optionId;
                    if ((cv.PaymentGroupId ?? 0) == 0) cv.PaymentGroupId = null;
                    var rc = _unitService.InsertUpdateConstructionValue(cv);
                    if (!rc.Success) throw new ApplicationException(rc.Messages.FirstOrDefault()?.Message ?? "Bouwwaarde opslaan mislukt.");
                    keptCvIds.Add(rc.InsertedId);
                }
            }

            var orphanOptionIds = existingOptions.Where(o => !keptOptionIds.Contains(o.Id)).Select(o => o.Id).ToList();
            var orphanCvIds = existingCvs
                .Where(c => c.FinishingOptionId is not null && !keptCvIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToList();
            if (orphanCvIds.Count > 0)
            {
                var rd = _unitService.DeleteConstructionValues(orphanCvIds);
                if (!rd.Success) throw new ApplicationException(rd.Messages.FirstOrDefault()?.Message ?? "Bouwwaarde verwijderen mislukt.");
            }
            foreach (var id in orphanOptionIds) _unitService.DeleteFinishingOption(id);
        }

        private void SyncUnitRoomsV2(int unitId, bool isNew, List<RoomBO> posted)
        {
            var existing = new List<RoomBO>();
            if (!isNew)
            {
                var er = _unitService.GetRooms(unitId);
                if (er.Success && er.Values is not null) existing = er.Values;
            }
            var existingIds = existing.Select(r => r.Id).ToHashSet();
            var keptIds = new HashSet<int>();

            foreach (var room in posted.Where(r => !RoomIsBlank(r)))
            {
                if (room.Id != 0 && !existingIds.Contains(room.Id)) room.Id = 0;
                room.UnitId = unitId;
                var rr = _unitService.InsertUpdateRoom(room);
                if (!rr.Success) throw new ApplicationException(rr.Messages.FirstOrDefault()?.Message ?? "Ruimte opslaan mislukt.");
                if (room.Id != 0) keptIds.Add(room.Id);
            }

            var removed = existing.Where(r => !keptIds.Contains(r.Id)).Select(r => r.Id).ToList();
            if (removed.Count > 0) _unitService.DeleteRooms(removed);
        }

        private async Task SyncUnitExecutionPlansV2(int unitId, List<IFormFile>? files, List<string>? names, List<int>? deleteIds)
        {
            if (deleteIds != null && deleteIds.Count > 0)
            {
                var toDelete = await _db.UnitExecutionPlan
                    .Where(x => x.UnitId == unitId && deleteIds.Contains(x.Id))
                    .ToListAsync();
                if (toDelete.Count > 0) _db.UnitExecutionPlan.RemoveRange(toDelete);
            }

            if (files != null && files.Count > 0)
            {
                var enteredNames = names ?? new List<string>();
                var index = 0;
                foreach (var planFile in files.Where(f => f != null && f.Length > 0))
                {
                    var uploaded = await UploadAssetToStorageAsync(planFile, "plans");
                    if (string.IsNullOrWhiteSpace(uploaded))
                        throw new ApplicationException("Uitvoeringsplan upload naar storage API mislukt.");

                    var entered = index < enteredNames.Count ? enteredNames[index] : null;
                    _db.UnitExecutionPlan.Add(new UnitExecutionPlan
                    {
                        UnitId = unitId,
                        Name = string.IsNullOrWhiteSpace(entered) ? Path.GetFileNameWithoutExtension(planFile.FileName) : entered.Trim(),
                        FileId = uploaded,
                        CreatedDate = DateTime.UtcNow,
                        CreatedByUserId = User.FindFirst(CpmClaims.UserId)?.Value
                    });
                    index++;
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
