-- =============================================
-- Migratie: 032_TrajectStandaardBindings
-- Datum: 2026-09-10
-- Omschrijving: Zet standaard bron-bindingen op de mijlpalen van de
--   standaardsjablonen (IsStandaard = 1), zodat de automatische herberekening
--   (increment 2) meteen werkt. Enkel rijen waar BronBinding nog NULL is worden
--   aangepast — handmatige aanpassingen blijven behouden. Idempotent.
--
--   ComputedBinding: 1=ProjectDoc 2=ProjectDatum 3=ProjectVlag 4=PlanningTaak
--       5=PlanningSectie 6=InvoicingPaymentStage 7=ClientAccountDatum
--       8=ConnectionSettlement 9=ConstructionIssue 10=ProjectVoortgangFase 11=Dossier
--   ProjectDocType: 6=Electrisch 7=Water 8=Gas 13=Riool 15=Brandweer
-- =============================================

IF COL_LENGTH('dbo.TrajectSjabloonMijlpaal', 'BronBinding') IS NOT NULL
BEGIN
    ;WITH b AS (
        SELECT * FROM (VALUES
            (N'WERFMELDING',          2, N'WerfmeldingDate'),
            (N'WERF_START',           2, N'StartDateConstruction'),
            (N'OPLEVERING_VOORLOPIG', 2, N'DeliveryDate'),
            (N'OPLEVERING_DEFINITIEF',2, N'DeliveryDateDef'),
            (N'KEURING_ELEK',         3, N'DocElectricalInspection'),
            (N'KEURING_WATER',        3, N'DocWaterInspection'),
            (N'KEURING_RIOOL',        3, N'DocSewerInspection'),
            (N'KEURING_GAS',          1, N'8'),
            (N'PID_AFGIFTE',          3, N'DocPid'),
            (N'UNIT_COMPROMIS',       7, N'DateSalesAgreement'),
            (N'UNIT_AKTE',            7, N'DateDeedOfSale'),
            (N'UNIT_OPLEVERING_VL',   7, N'DeliveryDate'),
            (N'UNIT_OPLEVERING_DEF',  7, N'DeliveryDateDef')
        ) AS x([Code], [Binding], [Param])
    )
    UPDATE m
        SET m.[BronBinding] = b.[Binding],
            m.[BronParam]   = b.[Param]
    FROM [dbo].[TrajectSjabloonMijlpaal] m
    JOIN [dbo].[TrajectSjabloonFase] f ON f.[Id] = m.[TrajectSjabloonFaseId]
    JOIN [dbo].[TrajectSjabloon] s     ON s.[Id] = f.[TrajectSjabloonId]
    JOIN b ON b.[Code] = m.[Code]
    WHERE s.[IsStandaard] = 1
      AND m.[BronBinding] IS NULL;

    PRINT 'Standaard bron-bindingen gezet (waar nog niet ingevuld).';
END
ELSE
    PRINT 'Kolom TrajectSjabloonMijlpaal.BronBinding ontbreekt — draai eerst 030/031.';

PRINT 'Migratie 032_TrajectStandaardBindings voltooid.';
