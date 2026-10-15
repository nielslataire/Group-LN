using ServiceCore.Budget;
using Xunit;

public class BudgetVergelijkTests
{
    [Theory]
    [InlineData("Definitief", 3, true)]
    [InlineData("Afgerond", 9, true)]
    [InlineData("Concept", 9, true)]     // concept dat stap 9 bereikte
    [InlineData("Concept", 6, false)]
    [InlineData("Concept", null, false)]
    public void Compleet_is_afgerond_definitief_of_stap_9(string status, int? stap, bool verwacht)
        => Assert.Equal(verwacht, BudgetVergelijkService.IsCompleet(status, stap));

    [Fact]
    public void Kopiestappen_bevatten_de_negen_stappen_van_het_nieuw_budget_venster()
        => Assert.Equal(new[] { "gegevens", "oppervlaktes", "sanitair", "gevels", "dak", "activiteiten", "parameters", "verkoop", "opmerkingen" }, ServiceCore.BudgetWizardService.KopieStappen);
}
