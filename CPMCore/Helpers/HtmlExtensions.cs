using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace CPMCore.Helpers
{
    public static class HtmlExtensions
    {
        private const string idsToReuseKey = "__htmlPrefixScopeExtensions_IdsToReuse_";

        public static IDisposable BeginCollectionItem(this IHtmlHelper html, string collectionName)
        {
            var idsToReuse = GetIdsToReuse(html.ViewContext.HttpContext, collectionName);
            string itemIndex = idsToReuse.Count > 0 ? idsToReuse.Dequeue() : Guid.NewGuid().ToString();

            var writer = html.ViewContext.Writer;
            writer.WriteLine($"<input type=\"hidden\" name=\"{collectionName}.index\" autocomplete=\"off\" value=\"{html.Encode(itemIndex)}\" />");

            return BeginHtmlFieldPrefixScope(html, $"{collectionName}[{itemIndex}]");
        }

        public static IDisposable BeginCollectionItem(this IHtmlHelper html, string collectionName, ref string identifier)
        {
            var idsToReuse = GetIdsToReuse(html.ViewContext.HttpContext, collectionName, string.Empty);
            string itemIndex = idsToReuse.Count > 0 ? idsToReuse.Dequeue() : Guid.NewGuid().ToString();

            html.ViewContext.Writer.WriteLine($"<input type=\"hidden\" name=\"{collectionName}.index\" autocomplete=\"off\" value=\"{html.Encode(itemIndex)}\" />");
            identifier = $"{collectionName}[{itemIndex}]";

            return BeginHtmlFieldPrefixScope(html, identifier);
        }

        public static IDisposable BeginChildCollectionItem(this IHtmlHelper html, string collectionName, string parentIdentifier, ref string identifier)
        {
            var idsToReuse = GetIdsToReuse(html.ViewContext.HttpContext, collectionName, parentIdentifier);
            string itemIndex = idsToReuse.Count > 0 ? idsToReuse.Dequeue() : Guid.NewGuid().ToString();

            html.ViewContext.Writer.WriteLine($"<input type=\"hidden\" name=\"{parentIdentifier}.{collectionName}.index\" autocomplete=\"off\" value=\"{html.Encode(itemIndex)}\" />");
            identifier = $"{parentIdentifier}.{collectionName}[{itemIndex}]";

            return BeginHtmlFieldPrefixScope(html, identifier);
        }

        public static string GetHtmlId(this IHtmlHelper html, string identifier, string propertyName)
        {
            var idValue = identifier.Replace("[", "_").Replace("]", "__" + propertyName);
            return idValue;
        }

        public static string GetHtmlName(this IHtmlHelper html, string identifier, string propertyName)
        {
            return $"{identifier}.{propertyName}";
        }

        public static IDisposable BeginHtmlFieldPrefixScope(this IHtmlHelper html, string htmlFieldPrefix)
        {
            return new HtmlFieldPrefixScope(html.ViewData.TemplateInfo, htmlFieldPrefix);
        }

        private static Queue<string> GetIdsToReuse(HttpContext httpContext, string collectionName)
        {
            string key = idsToReuseKey + collectionName;
            var items = httpContext.Items;

            if (!items.ContainsKey(key))
            {
                items[key] = new Queue<string>();
                var previouslyUsedIds = httpContext.Request.Form[$"{collectionName}.index"].ToString();

                if (!string.IsNullOrEmpty(previouslyUsedIds))
                {
                    foreach (var id in previouslyUsedIds.Split(','))
                    {
                        ((Queue<string>)items[key]).Enqueue(id);
                    }
                }
            }

            return (Queue<string>)items[key];
        }

        private static Queue<string> GetIdsToReuse(HttpContext httpContext, string collectionName, string parentIdentifier)
        {
            string key = !string.IsNullOrEmpty(parentIdentifier) ? idsToReuseKey + parentIdentifier + "." + collectionName : idsToReuseKey + collectionName;
            var items = httpContext.Items;

            if (!items.ContainsKey(key))
            {
                items[key] = new Queue<string>();
                string previouslyUsedIds = !string.IsNullOrEmpty(parentIdentifier)
                    ? httpContext.Request.Form[$"{parentIdentifier}.{collectionName}.index"].ToString()
                    : httpContext.Request.Form[$"{collectionName}.index"].ToString();

                if (!string.IsNullOrEmpty(previouslyUsedIds))
                {
                    foreach (var id in previouslyUsedIds.Split(','))
                    {
                        ((Queue<string>)items[key]).Enqueue(id);
                    }
                }
            }

            return (Queue<string>)items[key];
        }

        private class HtmlFieldPrefixScope : IDisposable
        {
            private readonly TemplateInfo _templateInfo;
            private readonly string _previousHtmlFieldPrefix;

            public HtmlFieldPrefixScope(TemplateInfo templateInfo, string htmlFieldPrefix)
            {
                _templateInfo = templateInfo;
                _previousHtmlFieldPrefix = templateInfo.HtmlFieldPrefix;
                templateInfo.HtmlFieldPrefix = htmlFieldPrefix;
            }

            public void Dispose()
            {
                _templateInfo.HtmlFieldPrefix = _previousHtmlFieldPrefix;
            }
        }

        public static IHtmlContent DisableIf(this IHtmlContent htmlContent, Func<bool> condition)
        {
            if (condition())
            {
                using var writer = new StringWriter();
                htmlContent.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                var html = writer.ToString();
                var index = html.IndexOf("/>");
                if (index == -1) index = html.IndexOf(">");
                html = html.Insert(index, " disabled=\"disabled\"");
                return new HtmlString(html);
            }

            return htmlContent;
        }

        public static IHtmlContent HtmlActionLink(this IHtmlHelper htmlHelper, string linkText, string actionName, string controllerName, object routeValues, object htmlAttributes)
        {
            var repID = Guid.NewGuid().ToString();
            var lnk = htmlHelper.ActionLink(repID, actionName, controllerName, routeValues, htmlAttributes);
            using var writer = new StringWriter();
            lnk.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
            var result = writer.ToString().Replace(repID, linkText);
            return new HtmlString(result);
        }

        //public static string GenerateSlug(this string phrase)
        //{
        //    string str = RemoveAccent(phrase).ToLower();
        //    str = Regex.Replace(str, "[^a-z0-9\s-]", "");
        //    str = Regex.Replace(str, "\s+", " ").Trim();
        //    str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
        //    str = Regex.Replace(str, "\s", "-");
        //    return str;
        //}

        public static string RemoveAccent(string txt)
        {
            byte[] bytes = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(txt);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }

        //public static IHtmlContent BasicCheckBoxFor<T>(this IHtmlHelper<T> html, Expression<Func<T, bool>> expression, object htmlAttributes = null)
        //{
        //    var tag = new TagBuilder("input")
        //    {
        //        TagRenderMode = TagRenderMode.SelfClosing
        //    };

        //    tag.Attributes["type"] = "checkbox";
        //    tag.Attributes["id"] = html.IdFor(expression).ToString();
        //    tag.Attributes["name"] = html.NameFor(expression).ToString();
        //    tag.Attributes["value"] = "true";

        //    var metadata = ExpressionMetadataProvider.FromLambdaExpression(expression, html.ViewData, html.MetadataProvider).Metadata;
        //    if (metadata.Model is bool modelChecked && modelChecked)
        //    {
        //        tag.Attributes["checked"] = "checked";
        //    }

        //    tag.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));

        //    using var writer = new StringWriter();
        //    tag.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        //    return new HtmlString(writer.ToString());
        //}
    }
}
