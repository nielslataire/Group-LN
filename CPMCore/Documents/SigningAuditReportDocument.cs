using System;
using System.Linq;
using BOCore;
using FacadeCore.Signing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents
{
    /// <summary>
    /// Het auditrapport (fase 2, ONDERTEKENEN_VOORSTEL.md §5.3): alle events van de hash-ketting van
    /// één dossier, het resultaat van de kettingverificatie en de akkoordtekst die de ondertekenaars
    /// zagen. Staat los van het ondertekende document — eigen PDF, geen samenvoeging nodig.
    /// </summary>
    public sealed class SigningAuditReportDocument : GroupLnPdfDocument
    {
        private readonly AuditReportInput _input;

        public SigningAuditReportDocument(AuditReportInput input, byte[]? logoBytes = null, string? fontFamily = null)
            : base(logoBytes, fontFamily, 1)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
        }

        public override DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = $"Auditrapport {_input.Case.Title}",
            Author = "Group LN",
            Subject = "Audit trail van elektronische ondertekening",
        };

        protected override PageSize PageSize => PageSizes.A4.Portrait();
        protected override string DocumentTitle => "Auditrapport";
        protected override string WerfTitel => _input.Case.Title;

        protected override string HeaderMetaLine =>
            $"{_input.Case.DocumentNumber} · {_input.Events.Count} events · ketting {(_input.Chain.Valid ? "intact" : "NIET intact")}";

        protected override string FooterNote => _input.Case.DocumentNumber ?? _input.Case.Title;

        protected override void Content(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().Element(x => SectionLabel(x, "Documenten", first: true));
                col.Item().PaddingBottom(10).Column(b =>
                {
                    b.Item().Text($"Origineel: SHA-256 {_input.OriginalSha256}").FontSize(7.6f).FontColor(Ink);
                    if (!string.IsNullOrWhiteSpace(_input.FinalSha256))
                        b.Item().Text($"Ondertekend: SHA-256 {_input.FinalSha256}").FontSize(7.6f).FontColor(Ink);
                    b.Item().PaddingTop(4).Text(_input.Chain.Valid
                            ? $"Hash-ketting van {_input.Chain.EventCount} events is intact."
                            : $"Hash-ketting NIET intact — {_input.Chain.Message} (eerste afwijking: event {_input.Chain.FirstBrokenEventId}).")
                        .FontSize(7.8f).SemiBold().FontColor(_input.Chain.Valid ? GreenAcc : "#a83232");
                });

                col.Item().Element(x => SectionLabel(x, "Akkoordtekst getoond aan de ondertekenaars"));
                col.Item().PaddingBottom(10).Text(string.IsNullOrWhiteSpace(_input.ConsentText) ? "—" : _input.ConsentText)
                    .FontSize(7.6f).FontColor(Ink).LineHeight(1.5f);

                col.Item().Element(x => SectionLabel(x, "Verloop (hash-ketting)"));
                col.Item().PaddingTop(4).Element(EventsTable);

                col.Item().PaddingTop(14).Text("Verifieer dit dossier op:").FontSize(7.6f).FontColor(Muted);
                col.Item().PaddingTop(2).Text(_input.VerificationUrl).FontSize(8.4f).SemiBold().FontColor(GreenAcc);
            });
        }

        private void EventsTable(IContainer c)
        {
            c.Border(0.75f).BorderColor(BorderCol).Table(t =>
            {
                t.ColumnsDefinition(d =>
                {
                    d.RelativeColumn(1.6f);
                    d.RelativeColumn(2.2f);
                    d.RelativeColumn(1.4f);
                    d.RelativeColumn(1.6f);
                    d.RelativeColumn(2.2f);
                });

                t.Header(h =>
                {
                    void Head(string text)
                    {
                        h.Cell().Background(Green900).PaddingVertical(6).PaddingHorizontal(6)
                            .Text(text.ToUpperInvariant()).FontSize(6.2f).Bold().FontColor("#ffffff").LetterSpacing(0.08f);
                    }
                    Head("Tijdstip");
                    Head("Event");
                    Head("Actor");
                    Head("IP");
                    Head("Eventhash");
                });

                if (_input.Events.Count == 0)
                {
                    t.Cell().ColumnSpan(5).PaddingVertical(8).PaddingHorizontal(6)
                        .Text("Geen events.").Italic().FontColor(Muted);
                    return;
                }

                var zebra = false;
                foreach (var e in _input.Events.OrderBy(ev => ev.Id))
                {
                    zebra = !zebra;
                    var bg = zebra ? "#ffffff" : Zebra;
                    IContainer Cell() => t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(RowLine).PaddingVertical(4).PaddingHorizontal(6);

                    Cell().Text(e.OccurredAtUtc.ToString("dd/MM/yy HH:mm:ss", Culture)).FontSize(7).FontColor(Ink);
                    Cell().Text(e.EventType).FontSize(7).FontColor(Ink);
                    Cell().Text(ActorLabel(e.ActorType, e.ActorLabel)).FontSize(7).FontColor(Ink);
                    Cell().Text(e.IpMasked ?? "—").FontSize(7).FontColor(Muted);
                    Cell().Text(e.EventHash.Length > 16 ? e.EventHash.Substring(0, 16) + "…" : e.EventHash).FontSize(6.6f).FontColor(Muted);
                }
            });
        }

        private static string ActorLabel(int actorType, string? label)
        {
            if (!string.IsNullOrWhiteSpace(label)) return label;
            return (SigningActorType)actorType switch
            {
                SigningActorType.System => "Systeem",
                SigningActorType.Internal => "Intern",
                SigningActorType.Party => "Ondertekenaar",
                SigningActorType.Provider => "Provider",
                _ => "—",
            };
        }
    }
}
