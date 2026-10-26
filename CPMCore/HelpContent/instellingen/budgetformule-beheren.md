---
title: Een budgetformule beheren
topic: instellingen
treeOrder: 11
device: Desktop
minutes: 6
updated: 2026-10-25
newsTag: Nieuw
screens: instellingen-budgetformules
synonyms: formule, budgetformule, parameter, testen, uitwerking, activiteit, wizard, ALS, EN, OF
description: een formule schrijven, testen met een echt budget en opslaan
related: formule-koppelingen-beheren, kostprijs-materialen-beheren
intro: |
  Een budgetformule bepaalt hoe CPM de kost van een activiteit berekent. Zonder formule valt de activiteit terug op de standaardberekening.
  Je schrijft de formule met parameters en rekent ze meteen door met een bestaand budget.
before: |
  Je hebt schrijfrecht op **Calculatie**. Je opent de pagina via **Instellingen** en dan **Budgetformules**.
faq:
  - q: Hoe voeg ik een parameter in?
    a: Klik rechts op de parameter, of typ @ in het formuleveld en kies uit de suggesties.
  - q: Waarom zie ik geen uitwerking?
    a: Kies bovenaan bij **Testen met** een budgetversie, of klik op **Testen aan**.
  - q: Wat betekent onbekende parameter?
    a: De naam bestaat niet. De parameter telt als 0. Controleer de spelling.
  - q: Wat gebeurt er als ik een formule verwijder?
    a: De activiteit gebruikt weer de standaardberekening.
  - q: Wat als de activiteit al een formule heeft?
    a: Kies je ze bij een nieuwe formule, dan open je de bestaande formule. Opslaan vervangt die.
---
## Een formule kiezen

Links staan de formules per lot. Zoek op activiteit of formule, of filter op lot en status. Een groene stip betekent actief.

## Een formule schrijven

Typ de formule in het veld **Formule, totaal project**. Je mag rekenen met + − × ÷ en haakjes.
Voorwaarden schrijf je als `ALS(x > 4; a; b)`. Combineren doe je met `EN(x; y)` en `OF(x; y)`.
Het **Label in de budgetwizard** is de tekst die de gebruiker in de wizard ziet.

## Parameters invoegen

Rechts staan de parameters per categorie. Klik op een parameter om ze op de cursor in te voegen, of typ @ in het veld.
Met een testbudget zie je naast elke parameter zijn waarde.

## Testen met een budget

Kies bovenaan bij **Testen met** een budgetversie. De uitwerking toont elke term met de waarden die CPM invult, en onderaan het resultaat in totaal en per eenheid.
Een fout in de formule staat meteen onder het veld.

## Opslaan

Klik op **Opslaan**. Met **Herstel** ga je terug naar de opgeslagen formule. Een stip naast **Niet opgeslagen wijzigingen** herinnert je eraan dat je nog moet opslaan.
Wissel je van formule met openstaande wijzigingen, dan vraagt CPM of je ze wilt weggooien.

## Een nieuwe formule maken

Klik op **Nieuwe formule**, kies de activiteit en schrijf de formule.

## Een formule uitschakelen of verwijderen

Zet **Actief** uit om de formule te bewaren zonder ze te gebruiken. Met **Verwijderen** wis je ze. Je kunt dit niet ongedaan maken.

## Let op

Een gewijzigde formule geldt voor nieuwe berekeningen. Test ze altijd met een echt budget voor je opslaat.
