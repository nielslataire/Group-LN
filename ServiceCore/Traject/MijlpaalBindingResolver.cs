using BOCore;
using DALCore.Models;
using FacadeCore;

namespace ServiceCore.Traject;

/// <summary>Zie <see cref="IMijlpaalBindingResolver"/>. Alle bronnen worden uitsluitend gelezen.</summary>
public class MijlpaalBindingResolver : IMijlpaalBindingResolver
{
    public BindingUitkomst? Resolve(Mijlpaal m, TrajectBronContext ctx)
    {
        if (m.BronBinding is not int b) return null;

        return (ComputedBinding)b switch
        {
            ComputedBinding.ProjectDoc => ResolveProjectDoc(m, ctx),
            ComputedBinding.ProjectDatum => ResolveProjectDatum(m, ctx),
            ComputedBinding.ProjectVlag => ResolveProjectVlag(m, ctx),
            ComputedBinding.PlanningTaak => ResolvePlanningTaak(m, ctx),
            ComputedBinding.PlanningSectie => ResolvePlanningSectie(m, ctx),
            ComputedBinding.InvoicingPaymentStage => ResolvePaymentStage(m, ctx),
            ComputedBinding.ClientAccountDatum => ResolveClientAccountDatum(m, ctx),
            ComputedBinding.ConstructionIssue => ResolveConstructionIssue(m, ctx),
            ComputedBinding.ProjectVoortgangFase => ResolveVoortgangFase(m, ctx),
            ComputedBinding.ConnectionSettlement => ResolveConnectionSettlement(ctx),
            ComputedBinding.Dossier => ResolveDossier(m, ctx),
            ComputedBinding.DossierSubstap => ResolveDossierSubstap(m, ctx),
            _ => null // Handmatig, onbekend
        };
    }

    private static BindingUitkomst? ResolveDossier(Mijlpaal m, TrajectBronContext ctx)
    {
        if (m.DossierId is not int id || !ctx.DossiersById.TryGetValue(id, out var dossier))
            return new BindingUitkomst(false, null);

        return dossier.Status == (int)DossierStatus.Afgehandeld
            ? new BindingUitkomst(true, dossier.AfgehandeldDatum)
            : new BindingUitkomst(false, null);
    }

    private static BindingUitkomst? ResolveDossierSubstap(Mijlpaal m, TrajectBronContext ctx)
    {
        if (m.DossierId is not int id || string.IsNullOrWhiteSpace(m.BronParam)
            || !ctx.SubstappenPerDossier.TryGetValue(id, out var stappen))
            return new BindingUitkomst(false, null);

        var stap = stappen.FirstOrDefault(s => string.Equals(s.Code, m.BronParam, StringComparison.OrdinalIgnoreCase));
        if (stap == null) return new BindingUitkomst(false, null);

        return stap.Status == (int)DossierSubstapStatus.Afgerond
            ? new BindingUitkomst(true, stap.Datum)
            : new BindingUitkomst(false, null);
    }

    /// <summary>Int-parameter voor bindingen die op een id/type werken: eerst <c>BronRefId</c>, anders <c>BronParam</c> als getal.</summary>
    private static int? RefId(Mijlpaal m)
    {
        if (m.BronRefId is int r) return r;
        if (!string.IsNullOrWhiteSpace(m.BronParam) && int.TryParse(m.BronParam.Trim(), out var x)) return x;
        return null;
    }

    private static BindingUitkomst? ResolveProjectDoc(Mijlpaal m, TrajectBronContext ctx)
    {
        if (RefId(m) is not int type) return null;
        var datum = ctx.Docs.Where(d => d.Type == type && d.Date != null)
            .Select(d => d.Date).DefaultIfEmpty(null).Max();
        return datum != null ? new BindingUitkomst(true, datum) : new BindingUitkomst(false, null);
    }

    private static BindingUitkomst? ResolveProjectDatum(Mijlpaal m, TrajectBronContext ctx)
    {
        var p = ctx.Project;
        DateOnly? d = (m.BronParam ?? "").Trim() switch
        {
            "StartDateConstruction" => p.StartDateConstruction,
            "DeliveryDate" => p.DeliveryDate,
            "DeliveryDateDef" => p.DeliveryDateDef,
            "WerfmeldingDate" => p.WerfmeldingDate,
            "WerfmeldingEndDate" => p.WerfmeldingEndDate,
            _ => null
        };
        return d != null ? new BindingUitkomst(true, d) : new BindingUitkomst(false, null);
    }

    private static BindingUitkomst? ResolveProjectVlag(Mijlpaal m, TrajectBronContext ctx)
    {
        var p = ctx.Project;
        bool? v = (m.BronParam ?? "").Trim() switch
        {
            "DocPid" => p.DocPid,
            "DocElectricalInspection" => p.DocElectricalInspection,
            "DocWaterInspection" => p.DocWaterInspection,
            "DocSewerInspection" => p.DocSewerInspection,
            "DocFireInspection" => p.DocFireInspection,
            "DocDelivery" => p.DocDelivery,
            "DocDefDelivery" => p.DocDefDelivery,
            _ => null
        };
        if (v == null) return null;
        return v == true ? new BindingUitkomst(true, null) : new BindingUitkomst(false, null);
    }

    private static BindingUitkomst? ResolvePlanningTaak(Mijlpaal m, TrajectBronContext ctx)
    {
        if (RefId(m) is not int id) return null;
        var t = ctx.PlanningTaken.FirstOrDefault(x => x.Id == id);
        if (t == null) return new BindingUitkomst(false, null);
        return t.IsAfgerond ? new BindingUitkomst(true, t.AfgerondOp) : new BindingUitkomst(false, null);
    }

    private static BindingUitkomst? ResolvePlanningSectie(Mijlpaal m, TrajectBronContext ctx)
    {
        if (RefId(m) is not int id) return null;
        var taken = ctx.PlanningTaken.Where(x => x.PlanningSectieId == id).ToList();
        if (taken.Count == 0) return new BindingUitkomst(false, null);
        if (taken.All(x => x.IsAfgerond))
        {
            var datum = taken.Where(x => x.AfgerondOp != null).Select(x => x.AfgerondOp).DefaultIfEmpty(null).Max();
            return new BindingUitkomst(true, datum);
        }
        return new BindingUitkomst(false, null);
    }

    private static BindingUitkomst? ResolvePaymentStage(Mijlpaal m, TrajectBronContext ctx)
    {
        if (RefId(m) is not int id) return null;
        var stage = ctx.PaymentStages.FirstOrDefault(x => x.Id == id);
        if (stage == null) return new BindingUitkomst(false, null);
        return stage.Invoicable ? new BindingUitkomst(true, null) : new BindingUitkomst(false, null);
    }

    private static BindingUitkomst? ResolveClientAccountDatum(Mijlpaal m, TrajectBronContext ctx)
    {
        var veld = (m.BronParam ?? "").Trim();
        DateOnly? Read(ClientAccount ca) => veld switch
        {
            "DateSalesAgreement" => ca.DateSalesAgreement,
            "DateDeedOfSale" => ca.DateDeedOfSale,
            "DeedOfSaleExpDate" => ca.DeedOfSaleExpDate,
            "DeliveryDate" => ca.DeliveryDate,
            "DeliveryDateDef" => ca.DeliveryDateDef,
            _ => null
        };

        if (m.UnitId is int uid)
        {
            if (!ctx.ClientAccountPerUnit.TryGetValue(uid, out var ca)) return new BindingUitkomst(false, null);
            var d = Read(ca);
            return d != null ? new BindingUitkomst(true, d) : new BindingUitkomst(false, null);
        }

        // Projectniveau: bereikt zodra elke gekoppelde klant de datum heeft; werkelijke datum = de laatste
        if (ctx.ClientAccounts.Count == 0) return new BindingUitkomst(false, null);
        var datums = ctx.ClientAccounts.Select(Read).ToList();
        if (datums.All(d => d != null))
            return new BindingUitkomst(true, datums.Max());
        return new BindingUitkomst(false, null);
    }

    private static BindingUitkomst? ResolveConstructionIssue(Mijlpaal m, TrajectBronContext ctx)
    {
        int open = RefId(m) is int phase
            ? (ctx.OpenIssuesPerPhase.TryGetValue(phase, out var c) ? c : 0)
            : ctx.OpenIssuesTotaal;
        return open == 0 ? new BindingUitkomst(true, null) : new BindingUitkomst(false, null);
    }

    private static BindingUitkomst? ResolveVoortgangFase(Mijlpaal m, TrajectBronContext ctx)
    {
        if (ctx.Voortgang == null || RefId(m) is not int doel) return new BindingUitkomst(false, null);
        return ctx.Voortgang.Fase >= doel
            ? new BindingUitkomst(true, DateOnly.FromDateTime(ctx.Voortgang.BerekendOp))
            : new BindingUitkomst(false, null);
    }

    private static BindingUitkomst? ResolveConnectionSettlement(TrajectBronContext ctx)
        => ctx.HeeftConnectionSettlement
            ? new BindingUitkomst(true, ctx.LaatsteConnectionSettlement)
            : new BindingUitkomst(false, null);
}
