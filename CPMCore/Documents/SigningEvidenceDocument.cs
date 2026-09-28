using System;
using System.Globalization;
using FacadeCore.Signing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents
{
    /// <summary>
    /// Het ondertekeningsblad (fase 2, ONDERTEKENEN_VOORSTEL.md §5.3): één QuestPDF-pagina die na het
    /// origineel wordt samengevoegd (<see cref="Services.Signing.SignedDocumentComposer"/>, PdfSharpCore)
    /// — een kopie van de stijl van het origineel (zelfde <see cref="GroupLnPdfDocument"/>-kop/-voet),
    /// met per ondertekenaar een compacte stempel (naam, tijdstip, verificatie-ID, SHA-256 van het
    /// origineel) i.p.v. een uitgebreide tabel — die forensische details (IP, methode, event-hashketting)
    /// staan al in het interne auditrapport (<see cref="SigningAuditReportDocument"/>), niet nodig om
    /// hier te herhalen (feedback Niels, 2026-09-28). Los van <c>ChangeOrderDocument</c>: dit blad moet
    /// voor elke toekomstige <c>ISigningDocumentSource</c> werken, niet enkel wijzigingsopdrachten — het
    /// lettertype komt dan ook niet van <c>ChangeOrderPdfBuilder</c> maar van <see cref="GroupLnFonts"/>.
    /// De SHA-256 hier is bewust die van het origineel, niet van het uiteindelijke PDF: die laatste kan
    /// een pagina die zelf deel uitmaakt van dat PDF niet vooraf tonen (kip-en-ei) — die staat wel op het
    /// interne auditrapport en, sinds fase 3, op de publieke verificatiepagina (<c>/verifieer/{id}</c>,
    /// waar de QR hieronder naartoe wijst).
    /// </summary>
    public sealed class SigningEvidenceDocument : GroupLnPdfDocument
    {
        private readonly FinalDocumentInput _input;
        private readonly byte[]? _qrPng;

        public SigningEvidenceDocument(FinalDocumentInput input, byte[]? logoBytes = null, string? fontFamily = null, byte[]? qrPng = null)
            : base(logoBytes, fontFamily, 1)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _qrPng = qrPng;
        }

        public override DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = $"Ondertekeningsblad {_input.Case.Title}",
            Author = "Group LN",
            Subject = "Bewijs van elektronische ondertekening",
        };

        protected override PageSize PageSize => PageSizes.A4.Portrait();
        protected override string DocumentTitle => "Ondertekeningsblad";
        protected override string WerfTitel => _input.Case.Title;

        protected override string HeaderMetaLine =>
            $"{_input.Case.DocumentNumber} · voltooid {(_input.Case.CompletedAt ?? DateTime.UtcNow).ToString("dd/MM/yyyy HH:mm", Culture)}";

        protected override string FooterNote => _input.Case.DocumentNumber ?? _input.Case.Title;

        protected override void Content(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().Element(x => SectionLabel(x, "Kopie met elektronische handtekening", first: true));
                col.Item().PaddingBottom(10).Text(
                        "Dit blad maakt integraal deel uit van het voorafgaande document en bewijst de elektronische ondertekening ervan.")
                    .FontSize(8).FontColor(Ink).LineHeight(1.5f);

                col.Item().Column(sc =>
                {
                    foreach (var s in _input.Signers)
                        sc.Item().PaddingBottom(10).Element(x => SignatureStamp(x, s));
                });

                if (_qrPng is { Length: > 0 })
                {
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.ConstantItem(64).Image(_qrPng).FitArea();
                        row.RelativeItem().PaddingLeft(10).AlignMiddle().Text(
                            "Scan deze QR-code of ga naar de verificatiepagina om na te gaan dat dit document echt is.")
                            .FontSize(7.6f).FontColor(Muted);
                    });
                }
                // Zonder QR (generatie mislukt): geen kale URL op het klantdocument — die kan pas terug
                // zodra de QR er wél is (feedback Niels, 2026-09-28: een dode/kale link hoort niet thuis
                // op wat de klant ziet). Intern (auditrapport) staat de link sowieso al als tekst.
            });
        }

        /// <summary>Compacte stempel (feedback Niels, 2026-09-28) — geen tabel meer, dit is het blad dat
        /// als een ondertekende kopie van het origineel oogt: getekende handtekening (indien aanwezig)
        /// boven een korte identiteits-/bewijsregel, in hetzelfde vak-gevoel als
        /// ChangeOrderDocument.SignatureBlock ("Handtekening voor akkoord"), nu ingevuld. Het origineel
        /// zelf blijft ongewijzigd (ONDERTEKENEN_VOORSTEL.md) — dit is een apart blad erna, geen wijziging
        /// van pagina 1.</summary>
        private void SignatureStamp(IContainer c, SignedPartyInfo s)
        {
            c.Border(0.75f).BorderColor(BorderCol).Padding(12).Column(sc =>
            {
                if (s.SignatureImagePng is { Length: > 0 } png)
                    sc.Item().PaddingBottom(8).MaxHeight(70).AlignLeft().Image(png).FitHeight();

                sc.Item().Text("Elektronisch ondertekend via CPM").FontSize(9).Bold().FontColor(GreenAcc);
                sc.Item().PaddingTop(4).Text(text =>
                {
                    text.Span(s.DisplayName).FontSize(9).SemiBold().FontColor(Heading);
                    if (!string.IsNullOrWhiteSpace(s.Capacity))
                        text.Span($" · {s.Capacity}").FontSize(8).FontColor(Muted);
                });
                sc.Item().PaddingTop(2).Text($"Ondertekend op {s.SignedAt.ToString("d MMMM yyyy", Culture)} om {s.SignedAt.ToString("HH:mm", Culture)}")
                    .FontSize(8).FontColor(Ink);
                sc.Item().PaddingTop(2).Text($"Verificatie-ID: {s.PartyVerificationId.ToString("N").Substring(0, 8)}…")
                    .FontSize(7.6f).FontColor(Muted);
                sc.Item().PaddingTop(2).Text($"SHA-256 van het originele document: {Truncate(_input.OriginalSha256)}")
                    .FontSize(7.6f).FontColor(Muted);
                if (s.SignatureImagePng is not { Length: > 0 })
                    sc.Item().PaddingTop(4).Text($"Geverifieerd via {SigningLabel(s.VerificationMethod)}.").FontSize(7.6f).Italic().FontColor(Muted);
            });
        }

        private static string SigningLabel(string? method) => method switch
        {
            "email-otp" => "e-mailcode",
            "sms-otp" => "sms-code",
            null or "" => "—",
            _ => method,
        };

        private static string Truncate(string sha256) => sha256.Length > 24 ? sha256.Substring(0, 24) + "…" : sha256;
    }
}
