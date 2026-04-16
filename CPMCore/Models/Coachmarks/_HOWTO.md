# Coachmark systeem — gebruik & uitbreiding

## 1. Nieuwe coachmark toevoegen

Open `CoachmarkRegistry.cs` en voeg één object toe aan de `_definitions` lijst:

```csharp
new CoachmarkDefinition
{
    FeatureKey     = "Invoices.Create.NewMode",       // unieke sleutel
    PageKey        = "Invoices.Create",               // pagina-sleutel
    TargetSelector = "#modeSelect",                   // CSS-selector
    Title          = "Nieuwe factuurmodus",
    Message        = "Kies hier hoe je factuurlijnen wil opmaken.",
    Placement      = CoachmarkPlacement.Right,
    ReleaseDate    = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
    MaxShowDays    = 45
},
```

## 2. Coachmark activeren op een pagina

In de **controller**:

```csharp
ViewData["CoachmarkPageKey"] = "Invoices.Create";
```

Dat is alles. De `_Coachmarks.cshtml` partial (automatisch ingeladen via `_Layout.cshtml`)
pikt de pageKey op, bevraagt de service, en injecteert de config als `window.GlCoachmarkFlow`.

## 3. Van single naar multi-step sequence

Single (geen `SequenceKey`):

```csharp
new CoachmarkDefinition
{
    FeatureKey = "Invoices.Create.NewMode",
    PageKey    = "Invoices.Create",
    ...
}
```

Multi-step: dezelfde `SequenceKey` + oplopende `StepIndex`:

```csharp
new CoachmarkDefinition
{
    FeatureKey     = "Invoices.Create.Tour.Step1",
    PageKey        = "Invoices.Create",
    TargetSelector = "#IssuerCompanyId",
    Title          = "Stap 1/3 — Facturatiebedrijf",
    Message        = "Selecteer hier het bedrijf dat de factuur uitschrijft.",
    Placement      = CoachmarkPlacement.Bottom,
    SequenceKey    = "Invoices.Create.Tour",           // ← zelfde key
    StepIndex      = 0,
    ReleaseDate    = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
    MaxShowDays    = 45
},
new CoachmarkDefinition
{
    FeatureKey     = "Invoices.Create.Tour.Step2",
    PageKey        = "Invoices.Create",
    TargetSelector = "#partySelect",
    Title          = "Stap 2/3 — Klant of leverancier",
    Message        = "Zoek en selecteer de ontvangende partij.",
    Placement      = CoachmarkPlacement.Bottom,
    SequenceKey    = "Invoices.Create.Tour",           // ← zelfde key
    StepIndex      = 1,
    ReleaseDate    = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
    MaxShowDays    = 45
},
new CoachmarkDefinition
{
    FeatureKey     = "Invoices.Create.Tour.Step3",
    PageKey        = "Invoices.Create",
    TargetSelector = "#VatTypeId",
    Title          = "Stap 3/3 — BTW-tarief",
    Message        = "Stel het standaard BTW-tarief in voor alle lijnen.",
    Placement      = CoachmarkPlacement.Top,
    SequenceKey    = "Invoices.Create.Tour",           // ← zelfde key
    StepIndex      = 2,
    ReleaseDate    = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
    MaxShowDays    = 45
},
```

## 4. Coachmark laten uitdoven

Stel `MaxShowDays` in op het aantal dagen na `ReleaseDate`.
Na die periode retourneert de service `null` en toont de frontend niets.
Verwijder de definitie uit `CoachmarkRegistry.cs` zodra ze echt obsoleet is.

## 5. State resetten (dev/test)

```sql
DELETE FROM UserCoachmarkState WHERE FeatureKey LIKE 'Invoices.%';
```
