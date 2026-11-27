using DALCore.Models;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>
/// De zin "wat wordt er van je verwacht" bij een mijlpaal, voor de mails en taken die een trigger maakt.
/// Volgorde: 1) de tekst die bij de trigger zelf staat (parameter <c>omschrijving</c>), 2) de standaardtekst voor de mijlpaalcode hieronder,
/// 3) een algemene zin. Zo vertelt elke mail en elke taak wat er concreet moet gebeuren en niet enkel dat er iets is.
/// De teksten zijn geschreven in de gebiedende wijs, tegen de persoon die ze leest.
/// </summary>
internal static class MijlpaalVerwachting
{
    private static readonly Dictionary<string, string> PerCode = new(StringComparer.OrdinalIgnoreCase)
    {
        // Aankoop, studie en financiering
        ["GRONDVERWERVING"] = "Rond de aankoop van de grond af en vink deze mijlpaal af zodra de akte van aankoop verleden is. Start daarna het stedenbouwkundig onderzoek en de verkavelingsstudie.",
        ["HAALBAARHEIDSSTUDIE"] = "Neem de go/no-go beslissing over dit project: bekijk de haalbaarheidsstudie en beslis of we doorgaan. Vink de mijlpaal af en noteer de beslissing bij de opmerking.",
        ["FINANCIERING"] = "Zorg dat de financiering rond is (bankkrediet en bankgaranties) en vink de mijlpaal af zodra de bank het akkoord heeft bevestigd.",
        ["VERKOOPDOSSIER"] = "Leg de verkoopprijzen vast en maak het verkoopdossier klaar. Vink de mijlpaal af zodra de prijzen goedgekeurd zijn, zodat de volgende fase kan starten.",
        ["BREYNE_AANVRAAG"] = "Vraag de waarborg van de Wet Breyne aan voor de eerste verkoop. Zonder die waarborg mag er niet verkocht worden. Vink de mijlpaal af zodra de aanvraag is ingediend.",

        // Ontwerp, vergunning en wegenis
        ["VOORONTWERP_INFRASTRUCTUUR"] = "Controleer het voorontwerp van de infrastructuur en keur het goed. Vink de mijlpaal af zodra het ontwerp gevalideerd is.",
        ["VERGUNNING_AANVRAAG"] = "Dien de vergunningsaanvraag in en vink de mijlpaal af zodra de ontvangstbevestiging binnen is.",
        ["VERGUNNING_VERLEEND"] = "De vergunning is verleend. Start de beroepstermijn van 35 dagen en bewaak dat er geen beroep wordt ingediend.",
        ["OPENBAAR_ONDERZOEK"] = "Het openbaar onderzoek is afgerond. Volg de bezwaartermijn op en bekijk of er bezwaren zijn ingediend.",
        ["ADVIEZEN_INSTANTIES"] = "Zorg dat alle adviezen van de betrokken instanties binnen zijn. Neem contact op met de instanties waarvan het advies nog ontbreekt en vink de mijlpaal af zodra alles compleet is.",
        ["GEMEENTERAADSBESLISSING"] = "De gemeenteraad heeft beslist over de overname van de wegenis. Regel nu de financiële waarborg (bankgarantie) en vink de mijlpaal af zodra die bevestigd is.",
        ["FINANCIELE_WAARBORG"] = "Zorg dat de financiële waarborg gestort of bevestigd is en vink de mijlpaal af zodra het bewijs in het dossier staat.",
        ["VERGUNNING_DEFINITIEF"] = "De vergunning is definitief. Start de volgende fase en laat het team weten dat het project verder kan.",
        ["AANBESTEDING_WEGENIS"] = "Rond de aanbesteding voor de wegenis af: vergelijk de offertes, kies de aannemer en vink de mijlpaal af zodra de opdracht is toegekend.",
        ["WEGENIS_NUTSVOORZIENINGEN"] = "Controleer of de nutsvoorzieningen (water, gas, elektriciteit, riolering) zijn aangelegd en vink de mijlpaal af zodra de maatschappijen dat bevestigd hebben.",
        ["WEGENIS_VOORLOPIGE_OPLEVERING"] = "Plan de voorlopige oplevering van de wegenis met de gemeente en de aannemer. Vink de mijlpaal af zodra het proces-verbaal getekend is.",
        ["VOORLOPIGE_OPLEVERING_WEGENIS"] = "Plan de voorlopige oplevering van de wegenis met de gemeente en de aannemer. Vink de mijlpaal af zodra het proces-verbaal getekend is.",
        ["WEGENIS_DEFINITIEVE_OPLEVERING"] = "Rond de definitieve oplevering en overdracht van de wegenis af en vraag daarna de vrijgave van de financiële waarborg aan.",
        ["DEFINITIEVE_OPLEVERING_WEGENIS"] = "Rond de definitieve oplevering en overdracht van de wegenis af en vraag daarna de vrijgave van de financiële waarborg aan.",
        ["LANDMETERPLAN"] = "Laat de landmeter het plan en de afpaling afronden en vraag daarna de kadastrale mutatie aan.",
        ["KADASTRERING"] = "Controleer of de kavels gekadastreerd zijn en vink de mijlpaal af zodra de kadastrale nummers gekend zijn. Daarna kan de verkoop starten.",
        ["BASISAKTE"] = "Rond de basisakte en het reglement van mede-eigendom af bij de notaris en vink de mijlpaal af zodra de akte verleden is. Daarna kan de verkoop starten.",

        // Verkoop
        ["COMMERCIALISATIE"] = "Geef de kavels vrij voor verkoop: controleer de prijslijst en de website en laat de verkopers weten dat ze mogen verkopen.",
        ["VERKOOPDREMPEL"] = "De verkoopdrempel is bereikt. Beoordeel de financiering en beslis of de bouwstart kan beginnen. Vraag zo nodig de bankfinanciering aan en bereid de kredietovereenkomst voor.",
        ["VERKOOP_COMPROMIS"] = "Zorg dat het compromis van deze woning ondertekend is en bewaard staat in het dossier.",
        ["OPSCHORTENDE_VOORWAARDEN"] = "Volg op of de opschortende voorwaarden (zoals de lening van de koper) vervuld zijn en vink de mijlpaal af zodra de koper dat bevestigd heeft. Pas daarna kan de akte gepland worden.",
        ["NOTARIELE_AKTE"] = "Volg de notariële akte van deze woning op. Zorg dat alles klaar is voor de ondertekening en vink de mijlpaal af zodra de akte verleden is.",
        ["AKTEFACTUREN_KLAAR"] = "Zet de aktefacturen (grond en kosten) klaar op de pagina Facturatie van het project, met de knop Aktefacturen klaarzetten, en vink de mijlpaal af zodra ze aangemaakt zijn.",
        ["AKTEFACTUREN_BETAALD"] = "Controleer of de aktefacturen van deze woning betaald zijn. Zijn ze betaald, vink de mijlpaal dan af. Zijn ze nog open, volg dan de betaling op bij de koper of de notaris.",
        ["BOUWVERGUNNING_KOPER_INGEDIEND"] = "Zorg dat de bouwvergunningsaanvraag van de koper is ingediend en vink de mijlpaal af zodra de ontvangstbevestiging binnen is.",
        ["BOUWVERGUNNING_KOPER"] = "Controleer of de bouwvergunning van de koper verleend is. Zo ja, vink de mijlpaal af zodat de bouw voor deze woning kan starten.",
        ["KEUZES_MEERWERK"] = "Zorg dat de afwerkingskeuzes en het meerwerk van de koper bevestigd zijn en vink de mijlpaal af zodra de koper getekend heeft voor akkoord.",
        ["VERZEKERING_ABR"] = "Zorg dat de verzekering (ABR) in orde is en vink de mijlpaal af zodra het bewijs in het dossier staat. Zonder verzekering mag de bouw niet starten.",

        // Werfvoorbereiding en bouw
        ["AANSTELLING_COORDINATOR_EPB"] = "Stel de veiligheidscoördinator en de EPB-verslaggever aan en vink de mijlpaal af zodra beide aangesteld zijn.",
        ["WERFMELDING"] = "Dien de werfmelding in en vink de mijlpaal af zodra ze is ingediend. Zonder werfmelding mag de werf niet starten.",
        ["UITVOERING_START"] = "De uitvoering mag starten. Controleer of de werf klaar is voor de aannemers en start de planning.",
        ["WERF_UITGEZET"] = "Laat de landmeter de werf uitzetten en vink de mijlpaal af zodra het uitzetten gecontroleerd is.",
        ["FUNDERING_KLAAR"] = "Controleer of de funderingen klaar zijn en vink de mijlpaal af. Daarna kan de volgende schijf gefactureerd worden als die aan deze stap hangt.",
        ["KEURING_RIOLERING"] = "Plan de keuring van de riolering en vink de mijlpaal af zodra het keuringsverslag goed is.",
        ["VLOERPLAAT_GLVL"] = "Controleer of de vloerplaat van het gelijkvloers klaar is en vink de mijlpaal af.",
        ["OPSTORT"] = "Controleer of de opstort van de verdieping klaar is en vink de mijlpaal af.",
        ["METSELWERK_RUWBOUW"] = "Controleer of de ruwbouw gesloten is (metselwerk afgewerkt) en vink de mijlpaal af. Daarna kan de volgende schijf gefactureerd worden als die aan deze stap hangt.",
        ["DAK_PLAT_DICHT"] = "Controleer of het platte dak dicht is en vink de mijlpaal af.",
        ["DAK_HELLEND_DICHT"] = "Controleer of het hellende dak dicht is en vink de mijlpaal af.",
        ["BUITENSCHRIJNWERK"] = "Controleer of het buitenschrijnwerk geplaatst is (winddicht) en vink de mijlpaal af.",
        ["TECHNIEKEN_RUW"] = "Controleer of de leidingen en technieken geplaatst zijn en vink de mijlpaal af.",
        ["KEURING_ELEKTRICITEIT"] = "Plan de keuring van de elektriciteit en vink de mijlpaal af zodra het keuringsverslag goed is.",
        ["PLEISTER"] = "Controleer of het pleisterwerk klaar is en vink de mijlpaal af.",
        ["CHAPE"] = "Controleer of de chape geplaatst is en vink de mijlpaal af.",
        ["KEUKEN"] = "Controleer of de keuken geplaatst is en vink de mijlpaal af.",

        // Oplevering en nazorg
        ["EPB_AANGIFTE"] = "Zorg dat de EPB-verslaggever de EPB-aangifte heeft ingediend en vink de mijlpaal af zodra de aangifte bevestigd is.",
        ["PID_AFGELEVERD"] = "Zorg dat het postinterventiedossier (PID) is afgeleverd en vink de mijlpaal af zodra het in het dossier staat.",
        ["OPLEVERING_VOORLOPIG"] = "Plan de voorlopige oplevering met de koper en de aannemers en vink de mijlpaal af zodra het proces-verbaal getekend is.",
        ["VOORLOPIGE_OPLEVERING"] = "Plan de voorlopige oplevering met de koper en de aannemers en vink de mijlpaal af zodra het proces-verbaal getekend is.",
        ["EINDAFREKENING"] = "Maak de eindafrekening op (meer- en minwerk en aansluitkosten) en bezorg ze aan de koper. Vink de mijlpaal af zodra ze verstuurd is.",
        ["SALDO_ONTVANGEN"] = "Controleer of het saldo van deze woning ontvangen is. De sleutels mogen pas overgedragen worden als het saldo betaald is.",
        ["SLEUTELOVERDRACHT"] = "Draag de sleutels over aan de koper en vink de mijlpaal af zodra de koper getekend heeft voor ontvangst.",
        ["DEFINITIEVE_OPLEVERING_EENHEID"] = "Plan de definitieve oplevering van deze woning en vink de mijlpaal af zodra het proces-verbaal getekend is.",
        ["DEFINITIEVE_OPLEVERING_GEBOUW"] = "Plan de definitieve oplevering van het gebouw en vink de mijlpaal af zodra het proces-verbaal getekend is. Ontbreken er nog herstellingen, zorg dan dat die afgewerkt zijn.",
        ["NACALCULATIE_WONING"] = "Sluit de nacalculatie van deze woning af: controleer alle kosten en vink de mijlpaal af zodra de nacalculatie klopt.",
        ["GARANTIE_VERZEKERING_WONING"] = "Controleer of de tienjarige garantie en de verzekering van deze woning in orde zijn en vink de mijlpaal af.",
        ["NAZORG_VERKAVELING"] = "Rond de verplichtingen van de verkavelaar af en vink de mijlpaal af zodra alles gedaan is.",
        ["WAARBORGPERIODE_WEGENIS"] = "Controleer of de waarborgperiode van de wegenis voorbij is en vink de mijlpaal af. Daarna kan de waarborg vrijgegeven worden.",

        // Overige codes uit de andere trajectsjablonen (woonproject, residentieel)
        ["GRONDOPTIE"] = "Volg de grondoptie op: beslis vóór de einddatum of we de optie omzetten in een aankoop, en vink de mijlpaal af zodra dat beslist is.",
        ["AANKOOP_COMPROMIS"] = "Zorg dat het compromis voor de aankoop van de grond ondertekend is en vink de mijlpaal af.",
        ["AANKOOP_AKTE"] = "Volg de akte van aankoop van de grond op bij de notaris en vink de mijlpaal af zodra ze verleden is.",
        ["AANKOOPAKTE_GROND"] = "Volg de akte van aankoop van de grond op bij de notaris en vink de mijlpaal af zodra ze verleden is.",
        ["SCHETSONTWERP"] = "Bekijk het schetsontwerp met de architect en vink de mijlpaal af zodra het goedgekeurd is.",
        ["VOORONTWERP_GOEDGEKEURD"] = "Zorg dat het voorontwerp goedgekeurd is en vink de mijlpaal af. Daarna kan de vergunningsaanvraag worden voorbereid.",
        ["ARCHITECT_AANGESTELD"] = "Stel de architect aan en vink de mijlpaal af zodra de opdracht getekend is.",
        ["VERGUNNING_INGEDIEND"] = "Zorg dat de vergunningsaanvraag is ingediend en vink de mijlpaal af zodra de ontvangstbevestiging binnen is.",
        ["VERGUNNING_VOLLEDIG"] = "Controleer of het vergunningsdossier volledig verklaard is en vink de mijlpaal af. Mist er nog iets, vul het dossier dan aan.",
        ["VERGUNNING_DOSSIER_KLAAR"] = "Controleer of het vergunningsdossier klaar is voor indiening en vink de mijlpaal af.",
        ["DOSSIER_VOLLEDIG"] = "Controleer of het dossier volledig is en vink de mijlpaal af. Mist er nog iets, vraag het dan op.",
        ["BREYNE_WAARBORG"] = "Zorg dat de waarborg van de Wet Breyne is afgegeven en vink de mijlpaal af zodra het bewijs in het dossier staat.",
        ["OPRICHTING_VME"] = "Rond de oprichting van de vereniging van mede-eigenaars af en vink de mijlpaal af zodra ze is opgericht.",
        ["VOORVERKOOP_DREMPEL"] = "De drempel voor de voorverkoop is bereikt. Beoordeel de financiering en beslis of het project verder kan naar de bouwvoorbereiding.",
        ["VERKOOP_GESTART"] = "De verkoop is gestart. Controleer of de prijslijst, de documenten en de website klaar zijn voor de kopers.",
        ["UNIT_COMPROMIS"] = "Zorg dat het compromis van deze eenheid ondertekend is en bewaard staat in het dossier.",
        ["UNIT_AKTE"] = "Volg de notariële akte van deze eenheid op en vink de mijlpaal af zodra ze verleden is.",
        ["UNIT_OPLEVERING_VL"] = "Plan de voorlopige oplevering van deze eenheid met de koper en vink de mijlpaal af zodra het proces-verbaal getekend is.",
        ["UNIT_OPLEVERING_DEF"] = "Plan de definitieve oplevering van deze eenheid en vink de mijlpaal af zodra het proces-verbaal getekend is.",
        ["VEILIGHEIDSCOORDINATIE"] = "Zorg dat de veiligheidscoördinatie geregeld is en vink de mijlpaal af zodra de coördinator is aangesteld.",
        ["WERF_START"] = "De werf mag starten. Controleer of de vergunningen, de verzekering en de werfmelding in orde zijn en start de planning.",
        ["WIND_WATERDICHT"] = "Controleer of het gebouw wind- en waterdicht is en vink de mijlpaal af. Daarna kan de afwerking starten.",
        ["BOUWVOORTGANG_RUWBOUW"] = "Controleer of de ruwbouw klaar is en vink de mijlpaal af. Daarna kan de volgende schijf gefactureerd worden als die aan deze stap hangt.",
        ["BOUWVOORTGANG_DAK_DICHT"] = "Controleer of het dak dicht is en vink de mijlpaal af.",
        ["BOUWVOORTGANG_AFWERKING"] = "Controleer de voortgang van de afwerking en vink de mijlpaal af zodra ze klaar is.",
        ["TECHNIEKEN_AF"] = "Controleer of de technieken (elektriciteit, sanitair en verwarming) afgewerkt zijn en vink de mijlpaal af.",
        ["AFWERKING_AF"] = "Controleer of de afwerking klaar is en vink de mijlpaal af. Daarna kan de oplevering voorbereid worden.",
        ["KEURING_ELEK"] = "Plan de keuring van de elektriciteit en vink de mijlpaal af zodra het keuringsverslag goed is.",
        ["KEURING_GAS"] = "Plan de keuring van de gasinstallatie en vink de mijlpaal af zodra het keuringsverslag goed is.",
        ["KEURING_WATER"] = "Plan de keuring van de waterinstallatie en vink de mijlpaal af zodra het keuringsverslag goed is.",
        ["KEURING_RIOOL"] = "Plan de keuring van de riolering en vink de mijlpaal af zodra het keuringsverslag goed is.",
        ["NUTSVOORZIENINGEN"] = "Controleer of de nutsvoorzieningen (water, gas, elektriciteit, riolering) aangesloten zijn en vink de mijlpaal af.",
        ["NUTS_AFREKENING"] = "Rond de afrekening van de nutsaansluitingen af en vink de mijlpaal af zodra ze verwerkt is.",
        ["SYNDICUS_VOORLOPIG"] = "Stel de voorlopige syndicus aan en vink de mijlpaal af zodra de opdracht getekend is.",
        ["OVERDRACHT"] = "Rond de overdracht af en vink de mijlpaal af zodra alles getekend is.",
        ["OVERDRACHT_GROENZONES"] = "Rond de overdracht van de groenzones af en vink de mijlpaal af zodra ze getekend is.",
        ["PID_AFGIFTE"] = "Zorg dat het postinterventiedossier (PID) is afgeleverd en vink de mijlpaal af zodra het in het dossier staat.",
        ["OPLEVERING_DEFINITIEF"] = "Plan de definitieve oplevering en vink de mijlpaal af zodra het proces-verbaal getekend is.",
        ["DEFINITIEVE_OPLEVERING"] = "Plan de definitieve oplevering en vink de mijlpaal af zodra het proces-verbaal getekend is.",
        ["WAARBORG_VRIJGAVE"] = "Vraag de vrijgave van de waarborg aan en vink de mijlpaal af zodra ze is uitgevoerd.",
        ["GARANTIEPERIODE"] = "Volg de garantieperiode op en vink de mijlpaal af zodra ze afloopt en er geen openstaande punten meer zijn.",
        ["UITVOERING_DEADLINE"] = "Controleer of de uitvoering nog binnen de uitvoeringstermijn valt. Loopt het uit, bekijk dan de planning en neem maatregelen of verwittig de klanten.",
        ["PROJECT_AFGESLOTEN"] = "Controleer of alles afgerond is (facturen, nacalculatie en documenten) en sluit het project af."
    };

    private const string Algemeen = "Open het traject van het project, controleer deze mijlpaal en vink ze af zodra ze klaar is. Past ze niet bij dit project, zet ze dan op Niet van toepassing.";

    /// <summary>Wat er van de lezer verwacht wordt: de tekst van de trigger, anders de standaardtekst voor de code, anders een algemene zin.</summary>
    public static string Wat(Mijlpaal mijlpaal, string? triggerTekst)
    {
        if (!string.IsNullOrWhiteSpace(triggerTekst)) return triggerTekst.Trim();
        if (!string.IsNullOrWhiteSpace(mijlpaal.Code) && PerCode.TryGetValue(mijlpaal.Code.Trim(), out var tekst)) return tekst;
        return Algemeen;
    }

    /// <summary>Waarom je dit nu krijgt, in één zin, volgens de gebeurtenis van de trigger en de datum van de mijlpaal.</summary>
    public static string Waarom(Mijlpaal mijlpaal, MijlpaalTrigger trigger, DateOnly vandaag)
    {
        var datum = mijlpaal.Doeldatum ?? mijlpaal.DoeldatumBerekend;
        var datumTekst = datum?.ToString("dd/MM/yyyy");
        switch ((BOCore.TriggerEvent)trigger.TriggerEvent)
        {
            case BOCore.TriggerEvent.BijBereiken:
                return "Deze mijlpaal is bereikt.";
            case BOCore.TriggerEvent.BijOverschrijding:
                return datumTekst == null ? "Deze mijlpaal is niet op tijd afgerond." : $"Deze mijlpaal had klaar moeten zijn op {datumTekst} en is nog niet afgerond.";
            case BOCore.TriggerEvent.XDagenVoorDoeldatum:
                if (datum == null) return "Deze mijlpaal nadert en is nog niet afgerond.";
                var dagen = datum.Value.DayNumber - vandaag.DayNumber;
                var wanneer = dagen switch { <= -1 => $"was op {datumTekst}", 0 => "is vandaag", 1 => "is morgen", _ => $"is over {dagen} dagen ({datumTekst})" };
                return $"De streefdatum van deze mijlpaal {wanneer} en ze is nog niet afgerond.";
            case BOCore.TriggerEvent.BijFaseAfronding:
                return "De fase waartoe deze mijlpaal behoort, is afgerond.";
            default:
                return "De status van deze mijlpaal is gewijzigd.";
        }
    }
}
