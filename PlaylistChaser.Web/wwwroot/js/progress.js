// progress.js

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/progressHub")
    .build();


connection.on("InitProgressToast", (title, toastId, cancellable) => {
    staticProgressToast.show(title, toastId, cancellable);
});

connection.on("UpdateProgressToast", (title, progress, maxProgress, message, toastId, cancellable) => {
    staticProgressToast.updateProgress(title, progress, maxProgress, message, toastId, cancellable);
});

connection.on("EndProgressToast", (toastId) => {
    staticProgressToast.hide(toastId);
});

connection.start().catch((err) => console.error(err));


var staticProgressToast = {
    show: function (title, toastId, cancellable) {
        let toastContainer = $("#toastContainer");
        if (toastContainer.find("#progressToast_" + toastId).length == 1)
            return;

        let toastHtml = this.getHtml(title, toastId, cancellable)
        toastContainer.append(toastHtml);
    },
    updateProgress: function (title, progress, maxProgress, message, toastId, cancellable) {
        this.show(title, toastId, cancellable);
        let toast = $("#progressToast_" + toastId);
        toast.find(".toast-body #progressBarContainer").attr('aria-valuenow', progress);
        let percentage = ((progress / maxProgress) * 100).toFixed(2);
        toast.find(".toast-body #progressBar").css('width', percentage + '%');
        toast.find(".toast-body #progressBar").html(percentage + '%');
        toast.find(".toast-body #message").html(message);
    },
    hide: function (toastId) {
        $("#progressToast_" + toastId).remove();
    },
    getHtml: function (title, toastId, cancellable) {
        let html = "" +
            "<div id=\"progressToast_" + toastId + "\" class=\"toast show\" role=\"alert\" aria-live=\"assertive\" aria-atomic=\"true\"> " +
            "   <div class=\"toast-header\">" +
            "       <strong id=\"title\" class=\"me-auto\">" + title + "</strong>" +
            "       <button type=\"button\" class=\"btn-close\" data-bs-dismiss=\"toast\" aria-label=\"Close\"></button>" +
            "   </div>" +
            "   <div class=\"toast-body\" style=\"display:flow-root; white-space:pre-line;\">" +
            "       <span id=\"message\"></span>";
        if (cancellable == true) {
            html += "" +
                "   <button id=\"cancelBtn\" type=\"button\" style=\"margin-left:5px;\" class=\"btn btn-secondary btn-sm float-end\" onclick=\"staticProgressToast.cancel('" + toastId + "');\">Cancel</button>";
        }
        html += "" +
            "       <div id=\"progressBarContainer\" class=\"progress\" role=\"progressbar\" aria-label=\"Example with label\" aria-valuenow=\"0\" aria-valuemin=\"0\" aria-valuemax=\"100\">" +
            "           <div id=\"progressBar\" class=\"progress-bar\" style=\"width: 0%\">0%</div>" +
            "       </div>" +
            "   </div> " +
            "</div>";
        return html;
    },
    enableCancelButton: function () {
        $("#cancelBtn").removeClass("disabled");
    },
    disableCancelButton: function () {
        $("#cancelBtn").addClass("disabled");
    },
    cancel: function (toastId) {
        this.disableCancelButton();
        let url = '/Base/CancelAction'
        $.post(url, { toastId: toastId }, function (data, status, jqXHR) {
            staticProgressToast.enableCancelButton();
        });
    }
};