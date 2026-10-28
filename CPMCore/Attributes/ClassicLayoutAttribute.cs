using System;

namespace CPMCore.Attributes
{
    /// <summary>
    /// Houdt een controller of actie in de klassieke shell (_Layout), ook als de gl-v2-layout aanstaat (cookie of de standaardinstelling
    /// <c>Layout:GlV2Default</c>). Voor schermen die nog geen gl-v2-versie hebben of een eigen doelgroep hebben (Documentencentrum, Werfportaal):
    /// hun legacy-view zou anders onopgemaakt in de gl-v2-shell staan. Haal het attribuut weg zodra het scherm een V2-versie heeft.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ClassicLayoutAttribute : Attribute { }
}
