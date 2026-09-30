Imports System.Linq

''' <summary>
''' Gedeelde landcode-tabel voor elk GlV2-telefoon-/gsm-veld (design-handoff optie 8f·2). Vroeger
''' toonden GlV2Telefoon.cshtml/GlV2Gsm.cshtml enkel een vast, niet-bewerkbaar "+32"-voorvoegsel dat
''' nergens werd opgeslagen — de gebruiker moet dit nu kunnen wijzigen (bv. een niet-Belgische
''' eigenaar/leverancier) én het gekozen voorvoegsel moet mee in de database staan. Om dit zonder
''' een migratie (extra kolom per telefoon-/gsm-veld, verspreid over meerdere tabellen) te kunnen,
''' wordt het voorvoegsel ALS ONDERDEEL van de bestaande NVARCHAR-kolom bewaard: "+32 495123456"
''' i.p.v. enkel "495123456". Split()/Combine() hieronder zijn de ene, gedeelde plek die dat formaat
''' kent — de Razor-editortemplates gebruiken ze om het veld in prefix+nummer te ontleden, elke
''' [GlV2Phone]-validatie (zie GlV2PhoneAttribute.vb) gebruikt IsValid() voor "voor zover mogelijk"
''' geldigheid. gl-v2-error-summary.js houdt een eigen, kleinere kopie van dezelfde tabel bij voor de
''' client-side spiegel (zie DESIGN.md "Telefoon/Gsm-voorvoegsel + validatie" voor waarom die twee
''' tabellen niet automatisch gesynchroniseerd zijn en hoe je ze samen aanpast).
''' </summary>
Public NotInheritable Class GlV2PhonePrefixes

    Public NotInheritable Class Prefix
        Public ReadOnly Property Code As String
        Public ReadOnly Property Label As String
        Public ReadOnly Property MinDigits As Integer
        Public ReadOnly Property MaxDigits As Integer
        Public Sub New(code As String, label As String, minDigits As Integer, maxDigits As Integer)
            Me.Code = code
            Me.Label = label
            Me.MinDigits = minDigits
            Me.MaxDigits = maxDigits
        End Sub
    End Class

    Public Const DefaultCode As String = "+32"

    ' Digit-bereiken zijn een redelijke, niet-uitputtende benadering ("voor zover mogelijk", geen
    ' volwaardige libphonenumber-validatie) — ruim genoeg om een duidelijke tikfout (te kort/te lang)
    ' te vangen zonder een geldig nummer per ongeluk af te keuren.
    Public Shared ReadOnly All As Prefix() = {
        New Prefix("+32", "België", 8, 9),
        New Prefix("+31", "Nederland", 9, 9),
        New Prefix("+33", "Frankrijk", 9, 9),
        New Prefix("+49", "Duitsland", 6, 11),
        New Prefix("+352", "Luxemburg", 6, 9),
        New Prefix("+44", "Verenigd Koninkrijk", 9, 10),
        New Prefix("+41", "Zwitserland", 9, 9),
        New Prefix("+34", "Spanje", 9, 9),
        New Prefix("+39", "Italië", 6, 11),
        New Prefix("+1", "VS/Canada", 10, 10)
    }

    ' Langste code eerst geprobeerd (bv. "+352" vóór "+35"/"+3") zodat een 3-cijferige landcode nooit
    ' als een kortere gelezen wordt.
    Private Shared ReadOnly OrderedByLength As Prefix() = All.OrderByDescending(Function(p) p.Code.Length).ToArray()

    Public Shared Function Find(code As String) As Prefix
        Return All.FirstOrDefault(Function(p) p.Code = code)
    End Function

    ''' <summary>
    ''' Ontleedt een opgeslagen waarde in (voorvoegsel, nummer). Een waarde zonder "+" (elk bestaand
    ''' record van vóór deze feature) krijgt het standaardvoorvoegsel +32 — exact het gedrag van
    ''' vandaag, dus bestaande data blijft ongewijzigd weergeven zonder dat er iets gemigreerd moet
    ''' worden.
    ''' </summary>
    Public Shared Function Split(stored As String) As (Prefix As String, Number As String)
        If String.IsNullOrWhiteSpace(stored) Then Return (DefaultCode, "")
        Dim s = stored.Trim()
        If Not s.StartsWith("+") Then Return (DefaultCode, s)
        For Each p In OrderedByLength
            If s.StartsWith(p.Code & " ") Then Return (p.Code, s.Substring(p.Code.Length).Trim())
            If s.StartsWith(p.Code) AndAlso (s.Length = p.Code.Length OrElse Not Char.IsDigit(s(p.Code.Length))) Then
                Return (p.Code, s.Substring(p.Code.Length).Trim())
            End If
        Next
        ' Onbekend "+"-voorvoegsel (een land buiten onze lijst): val terug op splitsen bij de eerste
        ' spatie zodat we het toch niet stilzwijgend weggooien.
        Dim spaceIndex = s.IndexOf(" "c)
        If spaceIndex > 0 Then Return (s.Substring(0, spaceIndex), s.Substring(spaceIndex + 1).Trim())
        Return (DefaultCode, s)
    End Function

    ''' <summary>Combineert voorvoegsel+nummer terug tot de op te slaan tekst. Een leeg nummer geeft
    ''' een lege string (geen kaal voorvoegsel zonder nummer bewaren).</summary>
    Public Shared Function Combine(prefix As String, number As String) As String
        Dim trimmedNumber = If(number, "").Trim()
        If trimmedNumber.Length = 0 Then Return ""
        Dim usedPrefix = If(String.IsNullOrWhiteSpace(prefix), DefaultCode, prefix.Trim())
        Return usedPrefix & " " & trimmedNumber
    End Function

    ''' <summary>"Voor zover mogelijk" geldigheid: enkel cijfers tellen, vergeleken met het
    ''' digit-bereik van het voorvoegsel (een onbekend voorvoegsel krijgt een ruime algemene marge).
    ''' Leeg is geldig hier — verplicht-zijn is een aparte regel (Required/data-gl-v2-required).</summary>
    Public Shared Function IsValid(stored As String) As Boolean
        If String.IsNullOrWhiteSpace(stored) Then Return True
        Dim parsed = Split(stored)
        Dim digits = New String(parsed.Number.Where(AddressOf Char.IsDigit).ToArray())
        If digits.Length = 0 Then Return False
        Dim rule = Find(parsed.Prefix)
        If rule Is Nothing Then Return digits.Length >= 6 AndAlso digits.Length <= 15
        Return digits.Length >= rule.MinDigits AndAlso digits.Length <= rule.MaxDigits
    End Function

End Class
