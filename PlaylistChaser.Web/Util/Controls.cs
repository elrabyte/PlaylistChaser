using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
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
        public static IHtmlContent BsLabel(this IHtmlHelper htmlHelper, string displayValue, string? displayFor = null)
        {
            displayFor = displayFor == null ? "" : $"for=\"{displayFor}\"";
            var html = $@"<label class=""col-sm-2 col-form-label"" {displayFor}>{displayValue}</label>";
            return new HtmlString(html);
        }

        public static IHtmlContent BsDisplayFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, object additionalViewData = null)
        {
            additionalViewData = additionalViewData ?? new { @class = "form-control" };
            var displayValue = htmlHelper.DisplayFor<TModel, TResult>(expression, additionalViewData).GetString();

            var html = $"<input type=\"text\" readonly class=\"form-control-plaintext\" value=\"" + displayValue + "\">";
            return new HtmlString(html);
        }

        public static IHtmlContent BsDisplay(this IHtmlHelper htmlHelper, string displayValue)
        {
            var html = $"<input type=\"text\" readonly class=\"form-control form-control-plaintext\" value=\"" + displayValue + "\">";
            return new HtmlString(html);
        }

        public static IHtmlContent BsTextBoxFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, object htmlAttributes = null)
        {
            htmlAttributes = htmlAttributes ?? new { @class = "form-control" };
            return htmlHelper.TextBoxFor<TModel, TResult>(expression, htmlAttributes);
        }

        public static IHtmlContent Button(string caption, string? onClickFunction = null, string? id = null, string cssClass = "btn-primary", string? iconName = null, string role = "button", string type = "button")
        {
            id = id == null ? "" : $"id=\"{id}\"";
            var icon = iconName == null ? "" : $"<i class=\"bi bi-{iconName}\"></i> ";
            var onClick = onClickFunction == null ? "" : $"href='javascript:;' onclick='{onClickFunction}'";
            var html = $@"<a {id} 
                             class=""btn {cssClass}"" 
                             {onClick}
                             role=""{role}"">
                          {icon}{caption}</a>";

            return new HtmlString(html);
        }

        public static IHtmlContent SubmitButton(string caption = "Save")
            => Button(caption, id: "submitBtn", iconName: "save", role: "submit", type: "submit");

        public static IHtmlContent EnumSelect<T>(string caption, string id = null, string @class = null)
        {
            var enumValues = Enum.GetValues(typeof(T)).Cast<T>();
            var html = $@"
            <div class=""form-floating"">
                <select id=""{id}"" class=""form-select {@class}"" >";

            foreach (var val in enumValues)
                html += $@"<option value=""{(int)(object)val}"">{val.ToString()}</option>";

            html += $@"
                </select>
                <label for=""sourceSelector"">{caption}</label>
            </div>";

            return new HtmlString(html);
        }
    }
}
