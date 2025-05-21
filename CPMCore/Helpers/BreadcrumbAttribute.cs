using System;

namespace CPMCore.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class BreadcrumbAttribute : Attribute
    {
        public string Title { get; }

        public BreadcrumbAttribute(string title)
        {
            Title = title;
        }
    }
}
