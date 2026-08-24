using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ServiceCore.Budget;

// ─────────────────────────────────────────────────────────────────────────────
// Kleine expressie-parser voor bewerkbare budgetformules.
// Syntax:  @parameter_naam, getallen (punt of komma als decimaal),
//          + - * / ( ), en × ÷ als alternatieven voor * /.
// Voorbeeld: @mat_dakwerken_daktimmerwerk * @opp_hellend_dak * 1.42 * 0.45
// ─────────────────────────────────────────────────────────────────────────────

public class FormuleParseException : Exception
{
    public FormuleParseException(string message) : base(message) { }
}

public abstract class FormuleNode
{
    public abstract decimal Evaluate(IReadOnlyDictionary<string, decimal> parameters);
    public abstract void CollectParameters(HashSet<string> namen);

    // Weergave van de (sub)expressie; paramWeergave bepaalt hoe een @parameter
    // getoond wordt (bv. zijn naam, of zijn geformatteerde waarde).
    public abstract string ToDisplay(Func<string, string> paramWeergave);
}

public class GetalNode : FormuleNode
{
    public decimal Waarde { get; }
    private readonly string _raw;
    public GetalNode(decimal waarde, string raw) { Waarde = waarde; _raw = raw; }
    public override decimal Evaluate(IReadOnlyDictionary<string, decimal> p) => Waarde;
    public override void CollectParameters(HashSet<string> namen) { }
    public override string ToDisplay(Func<string, string> paramWeergave) => _raw;
}

public class ParameterNode : FormuleNode
{
    public string Naam { get; }
    public ParameterNode(string naam) { Naam = naam; }
    public override decimal Evaluate(IReadOnlyDictionary<string, decimal> p) =>
        p.TryGetValue(Naam, out var v) ? v : 0m;
    public override void CollectParameters(HashSet<string> namen) => namen.Add(Naam);
    public override string ToDisplay(Func<string, string> paramWeergave) => paramWeergave(Naam);
}

public class NegatieNode : FormuleNode
{
    public FormuleNode Kind { get; }
    public NegatieNode(FormuleNode kind) { Kind = kind; }
    public override decimal Evaluate(IReadOnlyDictionary<string, decimal> p) => -Kind.Evaluate(p);
    public override void CollectParameters(HashSet<string> namen) => Kind.CollectParameters(namen);
    public override string ToDisplay(Func<string, string> paramWeergave) => "-" + Kind.ToDisplay(paramWeergave);
}

public class BinaryNode : FormuleNode
{
    public char Op { get; }
    public FormuleNode Links { get; }
    public FormuleNode Rechts { get; }

    public BinaryNode(char op, FormuleNode links, FormuleNode rechts)
    {
        Op = op; Links = links; Rechts = rechts;
    }

    public override decimal Evaluate(IReadOnlyDictionary<string, decimal> p)
    {
        var l = Links.Evaluate(p);
        var r = Rechts.Evaluate(p);
        return Op switch
        {
            '+' => l + r,
            '-' => l - r,
            '*' => l * r,
            '/' => r == 0m ? 0m : l / r,
            _   => 0m
        };
    }

    public override void CollectParameters(HashSet<string> namen)
    {
        Links.CollectParameters(namen);
        Rechts.CollectParameters(namen);
    }

    public override string ToDisplay(Func<string, string> paramWeergave)
    {
        var opTxt = Op switch { '*' => " × ", '/' => " ÷ ", '+' => " + ", '-' => " − ", _ => Op.ToString() };
        var l = Links.ToDisplay(paramWeergave);
        var r = Rechts.ToDisplay(paramWeergave);
        // Haakjes rond additieve subexpressies binnen een vermenigvuldiging
        if (Op is '*' or '/')
        {
            if (Links  is BinaryNode bl && bl.Op is '+' or '-') l = "(" + l + ")";
            if (Rechts is BinaryNode br && br.Op is '+' or '-') r = "(" + r + ")";
        }
        return l + opTxt + r;
    }
}

public class HaakjesNode : FormuleNode
{
    public FormuleNode Kind { get; }
    public HaakjesNode(FormuleNode kind) { Kind = kind; }
    public override decimal Evaluate(IReadOnlyDictionary<string, decimal> p) => Kind.Evaluate(p);
    public override void CollectParameters(HashSet<string> namen) => Kind.CollectParameters(namen);
    public override string ToDisplay(Func<string, string> paramWeergave) => "(" + Kind.ToDisplay(paramWeergave) + ")";
}

public static class FormuleParser
{
    public static FormuleNode Parse(string formule)
    {
        if (string.IsNullOrWhiteSpace(formule))
            throw new FormuleParseException("Formule is leeg.");
        var pos = 0;
        var node = ParseExpressie(formule, ref pos);
        SkipSpaties(formule, ref pos);
        if (pos < formule.Length)
            throw new FormuleParseException($"Onverwacht teken '{formule[pos]}' op positie {pos + 1}.");
        return node;
    }

    public static HashSet<string> GetParameterNamen(FormuleNode node)
    {
        var namen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        node.CollectParameters(namen);
        return namen;
    }

    // Splitst de formule in top-level termen (gescheiden door + of -),
    // met teken per term. Basis voor de detailregels in de popover.
    public static List<(FormuleNode Node, int Teken)> GetTermen(FormuleNode root)
    {
        var termen = new List<(FormuleNode, int)>();
        Verzamel(root, 1, termen);
        return termen;

        static void Verzamel(FormuleNode node, int teken, List<(FormuleNode, int)> termen)
        {
            if (node is BinaryNode b && b.Op is '+' or '-')
            {
                Verzamel(b.Links, teken, termen);
                Verzamel(b.Rechts, b.Op == '-' ? -teken : teken, termen);
            }
            else if (node is NegatieNode n)
            {
                Verzamel(n.Kind, -teken, termen);
            }
            else
            {
                termen.Add((node, teken));
            }
        }
    }

    // ── Recursive descent ────────────────────────────────────────────────────

    private static FormuleNode ParseExpressie(string s, ref int pos)
    {
        var links = ParseTerm(s, ref pos);
        while (true)
        {
            SkipSpaties(s, ref pos);
            if (pos < s.Length && (s[pos] == '+' || s[pos] == '-' || s[pos] == '−'))
            {
                var op = s[pos] == '+' ? '+' : '-';
                pos++;
                var rechts = ParseTerm(s, ref pos);
                links = new BinaryNode(op, links, rechts);
            }
            else return links;
        }
    }

    private static FormuleNode ParseTerm(string s, ref int pos)
    {
        var links = ParseFactor(s, ref pos);
        while (true)
        {
            SkipSpaties(s, ref pos);
            if (pos < s.Length && (s[pos] == '*' || s[pos] == '×' || s[pos] == '/' || s[pos] == '÷'))
            {
                var op = (s[pos] == '*' || s[pos] == '×') ? '*' : '/';
                pos++;
                var rechts = ParseFactor(s, ref pos);
                links = new BinaryNode(op, links, rechts);
            }
            else return links;
        }
    }

    private static FormuleNode ParseFactor(string s, ref int pos)
    {
        SkipSpaties(s, ref pos);
        if (pos < s.Length && (s[pos] == '-' || s[pos] == '−'))
        {
            pos++;
            return new NegatieNode(ParseFactor(s, ref pos));
        }
        return ParsePrimair(s, ref pos);
    }

    private static FormuleNode ParsePrimair(string s, ref int pos)
    {
        SkipSpaties(s, ref pos);
        if (pos >= s.Length)
            throw new FormuleParseException("Formule eindigt onverwacht.");

        var c = s[pos];

        if (c == '(')
        {
            pos++;
            var binnen = ParseExpressie(s, ref pos);
            SkipSpaties(s, ref pos);
            if (pos >= s.Length || s[pos] != ')')
                throw new FormuleParseException("Sluitend haakje ')' ontbreekt.");
            pos++;
            return new HaakjesNode(binnen);
        }

        if (c == '@')
        {
            pos++;
            var start = pos;
            while (pos < s.Length && (char.IsLetterOrDigit(s[pos]) || s[pos] == '_')) pos++;
            if (pos == start)
                throw new FormuleParseException($"Parameternaam ontbreekt na '@' op positie {start}.");
            return new ParameterNode(s.Substring(start, pos - start).ToLowerInvariant());
        }

        if (char.IsDigit(c))
        {
            var start = pos;
            var sb = new StringBuilder();
            var separatorGezien = false;
            while (pos < s.Length && (char.IsDigit(s[pos]) || s[pos] == '.' || s[pos] == ','))
            {
                if (s[pos] == '.' || s[pos] == ',')
                {
                    if (separatorGezien)
                        throw new FormuleParseException($"Ongeldig getal op positie {start + 1} (gebruik één decimaalteken, zonder duizendtallen).");
                    separatorGezien = true;
                    sb.Append('.');
                }
                else sb.Append(s[pos]);
                pos++;
            }
            var raw = s.Substring(start, pos - start);
            if (!decimal.TryParse(sb.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var waarde))
                throw new FormuleParseException($"Ongeldig getal '{raw}' op positie {start + 1}.");
            return new GetalNode(waarde, raw);
        }

        throw new FormuleParseException($"Onverwacht teken '{c}' op positie {pos + 1}.");
    }

    private static void SkipSpaties(string s, ref int pos)
    {
        while (pos < s.Length && char.IsWhiteSpace(s[pos])) pos++;
    }
}
