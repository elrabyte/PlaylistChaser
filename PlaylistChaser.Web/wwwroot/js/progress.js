// progress.js

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/progressHub")
    .build();

connection.on("InitProgressToast", (title, maxProgress) => {
    staticProgressToast.show(title, maxProgress);
});

connection.on("UpdateProgressToast", (progress, message) => {
    staticProgressToast.updateProgress(progress, message);
});

connection.on("EndProgressToast", () => {
    staticProgressToast.hide();
});

connection.start().catch((err) => console.error(err));


var staticProgressToast = {
    maxProgressValue: null,
    show: function (title, maxProgressValue) {        
        let toast = $("#progressToast");
        toast.find(".toast-header #title").html(title)
        toast.find(".toast-body #progressBarContainer").attr("aria-valuemin", 0)
        toast.find(".toast-body #progressBarContainer").attr("aria-valuemax", maxProgressValue)
        this.maxProgressValue = maxProgressValue;
        this.updateProgress(0);
        toast.addClass("show");
    },
    updateProgress: function (progressValue, message) {
        let toast = $("#progressToast");
        toast.find(".toast-body #progressBarContainer").attr('aria-valuenow', progressValue);
        let percentage = ((progressValue / this.maxProgressValue) * 100).toFixed(2);
        toast.find(".toast-body #progressBar").css('width', percentage + '%');
        toast.find(".toast-body #progressBar").html(percentage + '%');
        toast.find(".toast-body #message").html(message);
    },
    hide: function () {
        $("#progressToast").removeClass("show");
    }
};