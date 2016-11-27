
// Holders
scf_mle_languagesbar = 'scf_mle_languagesbar';

// Init all the html editor
tinymce.init({
    selector: 'textarea.SCFMultilanguageEditor',
    plugins: [
        scf_mle_languagesbar,
        "advlist autolink link image lists charmap print preview hr anchor pagebreak",
        "searchreplace wordcount visualblocks visualchars code fullscreen media nonbreaking",
        "table contextmenu template textcolor paste textcolor colorpicker textpattern"
    ],
    toolbar: scf_mle_languagesbar + " | " +
             "undo redo | cut copy paste | searchreplace | bold italic underline strikethrough | alignleft aligncenter alignright alignjustify | " +
             "outdent indent | bullist numlist | table | link unlink anchor image media code | forecolor backcolor | hr removeformat | " +
             "subscript superscript | charmap | print fullscreen preview | visualchars visualblocks nonbreaking template pagebreak restoredraft",
    menubar: false,
    toolbar_items_size: 'small'
});

// Create the languages plugin to add to the editor
tinymce.PluginManager.add(scf_mle_languagesbar, function (editor, url) {
    // Get the hidden field
    var getHiddenField = function () {
        // Get the original textarea and extract the name
        var textArea = editor.getElement();
        var name = textArea.name;

        // Create the hidden field name and get it
        name = name.substr(0, name.length - "_editor".length) + "_holder";
        return document.getElementById(name);
    }

    // Toggle the button status 
    var toggle = function (e) {
        if (e) e.active(!e.active());
    };

    // Serialize the languages values inside the hidden field
    var deserialize = function () {
        try {
            // Try to parse
            return JSON.parse(editor.scf_hiddenField.value);

        } catch (e) {
            // Return a new object
            return {}
        }
    };

    // Serialize the languages values inside the hidden field
    var serialize = function (values) {
        try {
            // Try to serialize
            editor.scf_hiddenField.value = JSON.stringify(values);

        } catch (e) {
            // Assign empty
            editor.scf_hiddenField.value = "";
        }
    };

    // Save the current editor content inside the original textarea
    var save = function (e) {
        // Check for empty values
        if (e) {
            // Get the values object and set the current lnaguage value
            var values = deserialize();
            values[e.settings.code] = editor.getContent();

            // Save
            serialize(values);
        }
    };

    // Load the value inside the current editor from the original textarea
    var load = function (e) {
        // Check for empty values
        if (e) {
            // Get the values object and load the value from current languge
            var value = deserialize()[e.settings.code];
            editor.setContent(typeof (value) == "undefined" ? "" : value);
        }
    };

    // Init the holders
    editor.scf_lastSelection = null;
    editor.scf_hiddenField = getHiddenField();

    // Craete the group button object
    var buttons = [];
    for (var index in scf_mle_languages) {
        // Get the language object
        var language = scf_mle_languages[index];

        // Create the single button and the add to the group
        buttons.push({
            index: index,
            text: language.text,
            code: language.code,
            onclick: function () {
                // Save the status
                save(editor.scf_lastSelection);
                toggle(editor.scf_lastSelection);
                // Load the status
                load(this);
                toggle(this);
                // Save the selection
                editor.scf_lastSelection = this;
            },
            onPostRender: function () {
                // If the first
                if (this.settings.index == 0) {
                    // Set the initial status
                    toggle(this);
                    editor.scf_lastSelection = this;
                }
            }
        });
    };

    // Add the group buttons to the editor
    editor.addButton(scf_mle_languagesbar, {
        type: 'buttongroup',
        items: buttons
    });

    // On change save the current status and serialize in the hidden field
    editor.on("change", function (e) {
        save(editor.scf_lastSelection);
    });
    editor.on("keyup", function () {
        save(editor.scf_lastSelection);
    });

});
