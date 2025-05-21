using Microsoft.AspNetCore.Mvc.Razor;

namespace CPMCore.Helpers
{
    public class CustomViewLocationExpander : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context)
        {
            // niets nodig hier
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            var customLocations = new[]
            {
            "/Views/{1}/Modals/{0}.cshtml",
            "/Views/{1}/Partials/{0}.cshtml",
            "/Views/{1}/Shared/{0}.cshtml",// {1} is de controllernaam
        };

            return customLocations.Concat(viewLocations);
        }
    }
}
