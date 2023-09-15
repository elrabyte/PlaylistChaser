using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;

namespace PlaylistChaser.Web.Util
{
    internal static class Controls
    {
        public static IHtmlContent BsLabelFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, object htmlAttributes = null)
        {
            htmlAttributes = htmlAttributes ?? new { @class = "col-sm-2 col-form-label" };
            return htmlHelper.LabelFor<TModel, TResult>(expression, htmlAttributes);
        }

        public static IHtmlContent BsDisplayFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, object additionalViewData = null)
        {
            additionalViewData = additionalViewData ?? new { @class = "form-control" };
            return htmlHelper.DisplayFor<TModel, TResult>(expression, additionalViewData);
        }
        public static IHtmlContent Button(string caption, string onClick, string? id = null, string cssClass = "btn-primary", string role = "button")
        {
            id = (id == null ? "" : $"id=\"{id}\"");
            var html = $@"<a {id} class=""btn {cssClass}"" href=""javascript:;"" onclick=""{onClick}"" role=""{role}"">{caption}</a>";

            return new HtmlString(html);
        }
    }
}
