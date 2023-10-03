// helper.js


var popupHelper = {
    showError: function (message, title = 'Error') {
        $("#errorPopupBody").text(message)
        if (title)
            $("#errorPopupTitleText").text(title);

        new bootstrap.Modal(document.getElementById('errorPopup')).show();
    }
};