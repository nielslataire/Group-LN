using System;
using System.Collections.Generic;
using System.Linq;
using CPMCore.Models.Projecten;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents
{
    /// <summary>
    /// Klantenlijst voor een werf: een PROJECTFICHE en per klant een kaart met naam, adres,
    /// gekoppelde eenheden, contactpersonen (naam/e-mail/telefoon/gsm) en mede-eigenaars
    /// (naam/adres/telefoon/e-mail). Kop/voet/kleuren komen van <see cref="GroupLnPdfDocument"/>.
    /// </summary>
    public class ClientListDocument : GroupLnPdfDocument
    {
        private readonly ClientListModel _m;

        public ClientListDocument(ClientListModel model, byte[] logoBytes = null, string fontFamily = null, int version = 1)
            : base(logoBytes, fontFamily, version)
        {
            _m = model ?? throw new ArgumentNullException(nameof(model));
        }

        public override DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = $"Klantenlijst {_m.ProjectName} - {DateTime.Now:dd/MM/yyyy}"
        };

        protected override string DocumentTitle => "Klantenlijst";

        protected override string WerfTitel => _m.Project?.ProjectName ?? _m.ProjectName;

        private string Opgemaakt => (_m.Project?.LaatstBijgewerkt == default
            ? DateTime.Now
            : _m.Project.LaatstBijgewerkt).ToString("dd/MM/yyyy", Culture);

        protected override string HeaderMetaLine =>
            $"Opgemaakt {Opgemaakt} · versie {Version} · overzicht klanten, contactpersonen & mede-eigenaars";

        // ── Inhoud ────────────────────────────────────────────────────────────────
        protected override void Content(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().Element(x => SectionLabel(x, "Projectfiche", first: true));
                col.Item().PaddingBottom(16).Element(Fiche);
                col.Item().Element(x => SectionLabel(x, "Klanten, contactpersonen & mede-eigenaars"));
                col.Item().Element(ClientsList);
            });
        }

        // ── Projectfiche ──────────────────────────────────────────────────────────
        private void Fiche(IContainer c)
        {
            var p = _m.Project ?? new ClientListProjectInfo { ProjectName = _m.ProjectName };

            var partijenSub = p.LaatstBijgewerkt == default ? null
                : $"Laatst bijgewerkt: {p.LaatstBijgewerkt.ToString("dd/MM/yyyy", Culture)}"
                  + (string.IsNullOrWhiteSpace(p.LaatstBijgewerktDoor) ? "" : $" door {p.LaatstBijgewerktDoor}");

            var boxes = new (string Label, string Value, string Small)[]
            {
                ("Werf / adres", string.IsNullOrWhiteSpace(p.AddressLine) ? p.ProjectName : p.AddressLine, p.CityLine),
                ("Bouwheer", p.OpdrachtgeverName, p.OpdrachtgeverAddress),
                ("Aantal klanten", $"{p.AantalKlanten} klanten", partijenSub),
                ("Aantal eenheden", $"{p.AantalEenhedenTotaal} eenheden", null),
                ("Waarvan verkocht", $"{p.AantalEenhedenVerkocht} eenheden", null),
            };

            FicheGrid(c, boxes, columns: 3);
        }

        // ── Klantenlijst ──────────────────────────────────────────────────────────
        private void ClientsList(IContainer c)
        {
            if (_m.Rows.Count == 0)
            {
                c.Text("Er zijn nog geen klanten gekoppeld aan dit project.")
                    .Italic().FontColor(Muted);
                return;
            }

            c.Column(col =>
            {
                col.Spacing(10);
                foreach (var row in _m.Rows)
                    col.Item().Element(x => ClientCard(x, row));
            });
        }

        private void ClientCard(IContainer c, ClientListRow r)
        {
            c.Border(0.75f).BorderColor(BorderCol).Column(card =>
            {
                card.Item().Background(GreenSoft).BorderBottom(1).BorderColor(GrpLine)
                    .PaddingVertical(6).PaddingHorizontal(10).Row(row =>
                {
                    row.RelativeItem(2).Column(a =>
                    {
                        a.Item().Text(r.ClientName).FontSize(8.5f).Bold().FontColor(Green900);
                        if (!string.IsNullOrWhiteSpace(r.Address))
                            a.Item().PaddingTop(1).Text(r.Address).FontSize(7).Italic().FontColor(Muted);
                    });
                    row.RelativeItem(1).AlignRight().AlignMiddle().Text(r.Units.Count == 0 ? "—" : string.Join(", ", r.Units))
                        .FontSize(7.4f).SemiBold().FontColor(Green700);
                });

                if (r.CoOwners.Count == 0 && r.Contacts.Count == 0) return;

                card.Item().PaddingVertical(8).PaddingHorizontal(10).Column(body =>
                {
                    body.Spacing(6);
                    // Mede-eigenaars staan boven de contactpersonen.
                    if (r.CoOwners.Count > 0)
                        body.Item().Element(x => PersonSection(x, "Mede-eigenaars", r.CoOwners, showAddress: true));
                    if (r.Contacts.Count > 0)
                        body.Item().Element(x => PersonSection(x, "Contactpersonen", r.Contacts, showAddress: false));
                });
            });
        }

        private void PersonSection(IContainer c, string title, List<ClientListPerson> people, bool showAddress)
        {
            c.Column(col =>
            {
                col.Item().PaddingBottom(4).Text(title.ToUpperInvariant())
                    .FontSize(6.4f).Bold().FontColor(Muted).LetterSpacing(0.15f);

                col.Spacing(3);
                foreach (var p in people)
                    col.Item().Element(x => PersonLine(x, p, showAddress));
            });
        }

        // Eén persoon op één lijn: naam (met aanspreking) — adres (enkel mede-eigenaars) · telefoon · gsm · e-mail.
        private void PersonLine(IContainer c, ClientListPerson p, bool showAddress)
        {
            var extras = new List<string>();
            if (showAddress && !string.IsNullOrWhiteSpace(p.Address)) extras.Add(p.Address);
            if (!string.IsNullOrWhiteSpace(p.Phone)) extras.Add($"T {FormatBelgianPhone(p.Phone)}");
            if (!string.IsNullOrWhiteSpace(p.Cellphone)) extras.Add($"GSM {FormatBelgianPhone(p.Cellphone)}");
            if (!string.IsNullOrWhiteSpace(p.Email)) extras.Add(p.Email);

            c.Text(t =>
            {
                t.Span(p.Name).FontSize(7.6f).SemiBold().FontColor(Heading);
                if (extras.Count > 0)
                {
                    t.Span("  —  ").FontSize(7.2f).FontColor(Sand);
                    t.Span(string.Join("   ·   ", extras)).FontSize(7.2f).FontColor(Ink);
                }
            });
        }
    }
}
