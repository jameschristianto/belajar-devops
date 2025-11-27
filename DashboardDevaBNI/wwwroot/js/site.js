// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const purifyConfig = {
    ALLOWED_TAGS: [
        'form', 'input', 'select', 'option', 'textarea', 'button',
        'label', 'div', 'span', 'a', 'ul', 'li', 'p', 'b', 'i', 'u',
        'strong', 'em', 'br', 'hr'
    ],
    ALLOWED_ATTR: [
        'class', 'style', 'required', 'multiple', 'placeholder', 'data-*',
        'type', 'name', 'value', 'id', 'for', 'checked', 'autocomplete'
    ]
};

function LoadPartialViewData(urlController, callback, SendData) {
    //console.log(SendData);
    $.ajax({
        type: 'GET',
        url: urlController,
        data: SendData,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            callback(data);
        }
    });
}

function RenderView(data) {
    //var sanitizedData = DOMPurify.sanitize(data, purifyConfig);
    //document.getElementById("DivFormBody").innerHTML = sanitizedData;
    document.getElementById("DivFormBody").innerHTML = data;
}