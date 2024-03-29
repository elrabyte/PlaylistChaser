﻿using Microsoft.AspNetCore.Html;

namespace PlaylistChaser.Web.Util
{
    internal static class Partials
    {
        #region ModalPartal
        public static IHtmlContent ModalBareBonePartial(string name, string partialUrl)
        {
            var modalName = $"{name}Modal";
            var containerName = $"{name}Container";

            var html = $@"
                <div id=""{modalName}"" class=""modal fade"" id=""addCombinedPlaylistModal"" tabindex=""-1"" aria-labelledby=""{modalName}Labe"" aria-hidden=""true"">
	                <div class=""modal-dialog"" role=""document"">
		                <div id=""{containerName}"" class=""modal-content rounded-4 shadow"">
                        </div>
                    </div>
                </div>";

            html += getModalPartialScript(name, modalName, containerName, partialUrl);

            return new HtmlString(html);
        }
        public static IHtmlContent ModalPartial(string name, string title, string partialUrl)
        {
            var modalName = $"{name}Modal";
            var containerName = $"{name}Container";

            var html = $@"
                <div id=""{modalName}"" class=""modal fade"" tabindex=""-1"" aria-labelledby=""{modalName}Label"" aria-hidden=""true"">
                    <div class=""modal-dialog"">
                        <div class=""modal-content"">
                            <div class=""modal-header"">
                                <h1 class=""modal-title fs-5"" id=""{modalName}Label"">{title}</h1>
                                <button type=""button"" class=""btn-close"" data-bs-dismiss=""modal"" aria-label=""Close""></button>
                            </div>
                            <div id=""{containerName}"" class=""modal-body"">
                            </div>
                            <div class=""modal-footer"">
                                <button type=""button"" class=""btn btn-secondary"" data-bs-dismiss=""modal"">Close</button>
                            </div>
                        </div>
                    </div>
                </div>";

            html += getModalPartialScript(name, modalName, containerName, partialUrl);

            return new HtmlString(html);
        }
        private static string getModalPartialScript(string name, string modalName, string containerName, string partialUrl)
        {
            return $@"
                <script type=""text/javascript"">
                    var {name} = {{
                        __namespace: true,
                        loaded: false,
                        params: null,
                        submitBtn: null,
                        init: function() {{
                            {name}.loadBody();
                        }},                        
                        loadBody: function () {{
                            let url = ""{partialUrl}"";
                            if(typeof {name}.params !== 'undefined')
                                url = url + {name}.params
                            $.get(url, null, function (data, status, jqXHR) {{
                                if (data.success == false || status != ""success"") {{ 
                                    return console.error(""error while loading modal {modalName}!""); 
                                }}

                                $(""#{containerName}"").html(data);
                                {name}.loaded = true;
                                //move submit button to footer
                                {name}.submitBtn = $(""#{containerName}"").find("".btn.btn-primary[role=\""submit\""]"");
                                if({name}.submitBtn.length === 1) {{
                                    {name}.submitBtn.appendTo(""#{modalName} .modal-footer"");
                                    {name}.submitBtn = $(""#{modalName} .modal-footer"").find("".btn.btn-primary[role=\""submit\""]"");

                                    //if no onclick
                                    if(typeof {name}.submitBtn[0].onclick != 'undefined') {{
                                        {name}.submitBtn.click({name}.submit);
                                    }}
                                }}
                            }});
                        }},
                        show: function(params) {{
                            {name}.params = params;
                            if({name}.loaded == false)
                                {name}.init();
                            $(""#{modalName}"").modal('show');
                        }},
                        hide: function() {{
                            $(""#{modalName}"").modal('hide');
                        }},
                        on: function(event, handler) {{
                            return $(""#{modalName}"").on(event, handler);
                        }},
                        off: function(event, handler) {{
                            return $(""#{modalName}"").off(event, handler);
                        }},
                        submit: function(){{
                            let form = $(""#{containerName} form"");
                            let url = form[0].action;
                            //get form vals
                            //fire event
                            $.ajax({{
                                type: ""POST"",
                                url: url,
                                data: form.serialize(),
                                success: function (data) {{
                                    if (!data.success)
                                        return popupHelper.showError(data.message);
                                    {name}.hide();
                                    $(""#{modalName}"").trigger('saved');
                                }},
                                dataType: ""json""
                            }});
                        }},
                        undoFooter: function(){{
                            {name}.submitBtn.remove();
                            {name}.submitBtn = null;
                        }},
                        onHide: function(){{
                            {name}.undoFooter();
                            $(""#{containerName}"").html("""");
                            {name}.loaded = false;
                        }}
                    }}
                    $(""#{modalName}"").on(""hide.bs.modal"", function(){{
                        {name}.onHide();
                    }})
                </script>
            ";
        }
        #endregion

        #region ReloadablePartial
        public static IHtmlContent ReloadablePartial(string name, string partialUrl, bool loadInitial = false)
        {
            var containerName = $"{name}Container";
            var html = $@"<div id=""{containerName}""></div>
                <script type=""text/javascript"">
                    var {name} = {{
                        __namespace: true,
                        params: null,
                        init: function() {{
                            {name}.loadBody();
                        }},
                        load: function(params) {{
                            {name}.params = params;
                            {name}.init();
                        }},
                        unload: function() {{
                            $(""#{containerName}"").html("""");
                        }},
                        loadBody: function () {{
                            $(""#{containerName}"").html(""Loading..."");
                            let url = ""{partialUrl}"";
                            if(typeof {name}.params !== 'undefined')
                                url = url + {name}.params
                            $.get(url, null, function (data, status, jqXHR) {{
                                if (data.success == false || status != ""success"") {{ 
                                    return console.error(""error while loading partial!""); 
                                }}

                                $(""#{containerName}"").html(data);
                                
                                let submitBtn = $(""#{containerName}"").find(""#submitBtn"");
                                if(submitBtn.length === 1) {{
                                    submitBtn.appendTo("".modal-footer"");
                                    //if no onclick
                                    if(typeof submitBtn[0].onclick != 'undefined') {{
                                        {name}.setOnclickToSubmitBtn(submitBtn);
                                    }}
                                }}
                            }});
                        }},
                        setOnclickToSubmitBtn: function(submitBtn) {{
                            submitBtn.click({name}.submit);
                        }},
                        submit: function(){{
                            let form = $(""#{containerName} form"");
                            let url = form[0].action;
                            //get form vals
                            //fire event
                            $.ajax({{
                                type: ""POST"",
                                url: url,
                                data: form.serialize(),
                                success: function(data) {{
                                        if (!data.success)
                                            return popupHelper.showError(data.message);

                                    $(""#{containerName}"").trigger(""saved"");
                                    }},
                                dataType: ""json""
                            }});
                        }}                        
                    }};";
            if (loadInitial)
            {
                html += $@"
                    $(function() {{
                        {name}.load();
                    }})";
            }

            html += "</script>";

            return new HtmlString(html);
        }
        #endregion
    }
}
