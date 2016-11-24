
// Init all the html editor
tinymce.init({
    selector: 'textarea.MultilanguageHtmlEditor',
    plugins: [
        "advlist autolink autosave link image lists charmap print preview hr anchor pagebreak",
        "searchreplace wordcount visualblocks visualchars code fullscreen media nonbreaking",
        "table contextmenu template textcolor paste fullpage textcolor colorpicker textpattern"
    ],
    toolbar: 'mybutton',
    toolbar1: "undo redo | cut copy paste | searchreplace | bold italic underline strikethrough | alignleft aligncenter alignright alignjustify | " +
              "outdent indent | bullist numlist | table | link unlink anchor image media code | forecolor backcolor | hr removeformat | " +
              "subscript superscript | charmap | print fullscreen preview | visualchars visualblocks nonbreaking template pagebreak restoredraft",
    menubar: false,
    toolbar_items_size: 'small',
    setup: function (editor) {

    }
});