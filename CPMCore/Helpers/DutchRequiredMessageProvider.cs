using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace CPMCore.Helpers;

/// <summary>
/// Nederlandse standaardtekst voor élke <see cref="RequiredAttribute"/> zonder eigen ErrorMessage —
/// zowel de expliciete <c>[Required]</c> op een model als de impliciete die MVC toevoegt voor een
/// niet-nullable referentietype (die gaf in het foutoverzicht kale teksten als "The ProjectName field
/// is required."). Ook élke <see cref="EmailAddressAttribute"/> zonder eigen ErrorMessage (zowel de
/// C#-kant — <c>[EmailAddress]</c>, bv. SupplierFormViewModel — als de VB-kant — <c>&lt;EmailAddress&gt;</c>,
/// bv. ClientAccountBO/ClientContactBO): één gedeelde, gebruiksvriendelijke boodschap i.p.v. een mix
/// van "Ongeldige e-mail" hier en "The Email field is not a valid e-mail address." daar. Hoort bij
/// het gedeelde Foutoverzicht (design-handoff punt 24, DESIGN.md): de server-gerenderde samenvatting
/// toont ModelState letterlijk, dus de teksten moeten aan de bron al gebruiksvriendelijk zijn. De
/// ModelBindingMessageProvider-teksten (Program.cs) dekken de rest. Geregistreerd via
/// <c>options.ModelMetadataDetailsProviders.Add(...)</c> — ná de DataAnnotations-provider van het
/// framework, zodat de impliciet toegevoegde RequiredAttribute hier al bestaat.
/// </summary>
public sealed class DutchRequiredMessageProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata)
        {
            if (attribute is RequiredAttribute required && string.IsNullOrEmpty(required.ErrorMessage) && required.ErrorMessageResourceName == null)
            {
                // {0} = weergavenaam van het veld ([Display(Name = …)] of de propertynaam).
                required.ErrorMessage = "{0} is verplicht.";
            }
            else if (attribute is EmailAddressAttribute email && string.IsNullOrEmpty(email.ErrorMessage) && email.ErrorMessageResourceName == null)
            {
                email.ErrorMessage = "{0} is geen geldig e-mailadres.";
            }
        }
    }
}
