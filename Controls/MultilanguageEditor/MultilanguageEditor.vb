Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Text
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.HtmlControls
Imports System.Web.UI.WebControls


Namespace WebControls

    <DefaultProperty("LanguageKey"),
        ToolboxData("<{0}:MultilanguageEditor runat=server></{0}:MultilanguageEditor>")>
    Public Class MultilanguageEditor
        Inherits WebControl

        ' Constants
        Private Const STATIC_CSS_RESOURCE_NAME As String = "SCFramework.MultilanguageEditor.css"
        Private Const STATIC_SCRIPT_RESOURCE_NAME As String = "SCFramework.MultilanguageEditor.js"

        Private Const STATIC_SCRIPT_RESOURCE_UID As String = "MultilanguageEditor-StaticScript"
        Private Const DYNAMIC_SCRIPT_RESOURCE_UID As String = "MultilanguageEditor-DynamicScript"

        Private Const TINYMCE_CDN_URL As String = "http://cdn.tinymce.com/4/tinymce.min.js"
        Private Const TINYMCE_RESOURCE_UID As String = "TinyMCE-StaticScript"


#Region " CONTRUCTOR "

        ' Define the class constructor
        Public Sub New()
            ' Define the container as a DIV
            MyBase.New(HtmlTextWriterTag.Div)
        End Sub

#End Region

#Region " EVENTS "

        ' When Init the web control
        Private Sub MultilanguageEditor_Init(sender As Object, e As EventArgs) Handles Me.Init
            ' Define the component ID is not exists
            If String.IsNullOrEmpty(Me.ID) Then
                Me.ID = "MLE_" & Crypt.CreateUniqueKey(10)
            End If
        End Sub

        ' When the control is loaded
        Private Sub MultilanguageHtmlEditor_Load(sender As Object, e As System.EventArgs) Handles Me.Load
            ' If not in design mode
            If Not Me.DesignMode Then
                ' Include the resources
                Me.IncludeCSSResources()
                Me.IncludeDynamicScriptsResources()
                Me.IncludeStaticScriptsResources()
            End If
        End Sub

        ' When the control contents is rendered
        Protected Overrides Sub RenderContents(ByVal writer As HtmlTextWriter)
            ' Check for design mode
            If Me.DesignMode Then
                ' Draw the default
                Me.DefaultDrawing(writer)
            Else
                ' Draw all component in control content
                Me.ControlDrawing(writer)
            End If
        End Sub

#End Region

#Region " DRAW METHODS "

        ' Default drawing 
        Private Sub DefaultDrawing(ByVal writer As HtmlTextWriter)
            ' Create the text box and set all properties
            Dim TextBox As TextBox = New TextBox()
            TextBox.Width = IIf(Me.Width.IsEmpty, Unit.Percentage(100), Me.Width)
            TextBox.Text = "Multilanguage HTML editor"

            ' Choice for multiline is the height is not empty
            If Not Me.Height.IsEmpty Then
                TextBox.TextMode = TextBoxMode.MultiLine
                TextBox.Height = IIf(Me.Height.IsEmpty, Unit.Pixel(400), Me.Height)
            End If

            ' Render the control
            TextBox.RenderControl(writer)
        End Sub

        ' Control drawing
        Private Sub ControlDrawing(ByVal writer As HtmlTextWriter)
            ' Get all the translated values
            Dim Values As Dictionary(Of String, String) = Bridge.Translations.GetTranslations(Me.TranslationKey)

            ' Serialize (JSON) the values
            Dim Serialized As String = String.Empty
            For Each Key As String In Values.Keys
                If Not String.IsNullOrEmpty(Serialized) Then Serialized &= ", "
                Serialized &= String.Format("{0}: {1}",
                                            Utils.String.EscapeJSON(Key.Replace("-", "_"), True),
                                            Utils.String.EscapeJSON(Values(Key), True))
            Next
            Serialized = "{ " & Serialized & " }"

            ' Create the html editor (TinyMCE)
            Dim TinyMCE As TextBox = New TextBox()
            TinyMCE.ID = String.Format("{0}_editor", Me.ID)
            TinyMCE.TextMode = TextBoxMode.MultiLine
            TinyMCE.CssClass = "SCFMultilanguageEditor"
            TinyMCE.Text = Values(Bridge.Languages.Default)
            TinyMCE.RenderControl(writer)

            ' Create the hidden field
            Dim Hidden As HiddenField = New HiddenField()
            Hidden.ID = String.Format("{0}_holder", Me.ID)
            Hidden.Value = Serialized
            Hidden.RenderControl(writer)
        End Sub

#End Region

#Region " RESOURCES "

        ' Check if is a call-back or a post-back
        Private Function IsCallback() As Boolean
            Dim SM As ScriptManager = ScriptManager.GetCurrent(Me.Page)
            Return SM IsNot Nothing AndAlso SM.IsInAsyncPostBack
        End Function

        ' Load the CSS resource
        Private Sub IncludeCSSResources()
            ' Get the resource URL
            Dim HREF As String = Page.ClientScript.GetWebResourceUrl(Me.GetType(), MultilanguageEditor.STATIC_CSS_RESOURCE_NAME)

            ' Create the link to add to the page
            Dim Link As HtmlLink = New HtmlLink()
            Link.EnableViewState = False
            Link.Attributes.Add("href", HREF)
            Link.Attributes.Add("type", "text/css")
            Link.Attributes.Add("rel", "stylesheet")

            ' If a call-back the link will be inserted directly inside this control as the control might create
            ' by an partial post back. Else the link will added within the page header.
            If Me.IsCallback Then
                ' Add to the control
                Me.Controls.Add(Link)
            Else
                ' Add to the page header
                Dim Head As HtmlHead = Page.Header
                Head.Controls.Add(Link)
            End If
        End Sub

        ' Load the dynamic scripts resource
        Private Sub IncludeDynamicScriptsResources()
            ' Create the buttons by languages codes
            Dim Clauses As DB.Clauses = New DB.Clauses("VISIBLE", DB.Clauses.Comparer.Equal, True)
            Dim Source As DataTable = Bridge.Languages.GetSource(Clauses)
            Dim Script As String = String.Empty

            ' Cycle all languages
            For Each Row As DataRow In Source.Rows
                If Not String.IsNullOrEmpty(Script) Then Script &= ", "
                Script &= String.Format("{{ text: '{0}', code: '{1}' }}", Row!TITLE, CStr(Row!CODE).Replace("-", "_"))
            Next
            Script = "scf_mle_languages = [" & Script & "];"

            ' If the page have associated a script manager (Ajax) add the script resource directly by the script manager.
            ' Else register the script by the page client script is not already registered.
            If ScriptManager.GetCurrent(Me.Page) IsNot Nothing Then
                ' Add by script manager
                ScriptManager.RegisterClientScriptBlock(
                    Me.Page, Me.GetType(), MultilanguageEditor.DYNAMIC_SCRIPT_RESOURCE_UID, Script, True)

            Else
                ' Check if already registered
                If Not Page.ClientScript.IsClientScriptBlockRegistered(MultilanguageEditor.DYNAMIC_SCRIPT_RESOURCE_UID) Then
                    ' Get the script resource and register it on the page
                    Page.ClientScript.RegisterClientScriptBlock(
                        Me.GetType(), MultilanguageEditor.TINYMCE_RESOURCE_UID, Script, True)
                End If
            End If
        End Sub

        ' Load the static scripts resource
        Private Sub IncludeStaticScriptsResources()
            ' If the page have associated a script manager (Ajax) add the script resource directly by the script manager.
            ' Else register the script by the page client script is not already registered.
            If ScriptManager.GetCurrent(Me.Page) IsNot Nothing Then
                ' Add by script manager
                ScriptManager.RegisterClientScriptInclude(
                Me.Page, Me.GetType(), MultilanguageEditor.TINYMCE_RESOURCE_UID, MultilanguageEditor.STATIC_SCRIPT_RESOURCE_NAME)
                ScriptManager.RegisterClientScriptResource(
                Me.Page, Me.GetType(), MultilanguageEditor.STATIC_SCRIPT_RESOURCE_NAME)

            Else
                ' Check if already registered
                If Not Page.ClientScript.IsClientScriptIncludeRegistered(MultilanguageEditor.TINYMCE_RESOURCE_UID) Then
                    ' Get the script resource URL and register it on the page
                    Dim URL As String = Me.ResolveUrl(MultilanguageEditor.TINYMCE_CDN_URL)
                    Page.ClientScript.RegisterClientScriptInclude(MultilanguageEditor.TINYMCE_RESOURCE_UID, URL)
                End If

                ' Check if already registered
                If Not Page.ClientScript.IsClientScriptIncludeRegistered(MultilanguageEditor.STATIC_SCRIPT_RESOURCE_UID) Then
                    ' Get the script resource URL and register it on the page
                    Dim URL As String = Page.ClientScript.GetWebResourceUrl(Me.GetType(), MultilanguageEditor.STATIC_SCRIPT_RESOURCE_NAME)
                    Page.ClientScript.RegisterClientScriptInclude(MultilanguageEditor.STATIC_SCRIPT_RESOURCE_UID, URL)
                End If
            End If
        End Sub

#End Region

#Region " PUBLIC PROPERTIES "

        <Bindable(True), Category("Behavior"), DefaultValue(""),
         Description("Set the key of translation")>
        Public Property TranslationKey As String
            Set(value As String)
                ViewState("TranslationKey") = value
            End Set
            Get
                If ViewState("TranslationKey") Is Nothing Then
                    ViewState("TranslationKey") = String.Empty
                End If
                Return CStr(ViewState("TranslationKey"))
            End Get
        End Property

#End Region

#Region " PUBLIC METHODS "

        ' Get all the languages values
        Public Function GetValues() As Dictionary(Of String, String)
            ' Get the hidden fields
            Dim Serialized As String = Me.Page.Request.Unvalidated.Form(String.Format("{0}_holder", Me.ID))
            Dim JSON As Script.Serialization.JavaScriptSerializer = New Script.Serialization.JavaScriptSerializer()

            ' Deserialize and init holders
            Dim Source As Object = JSON.Deserialize(Of Object)(Serialized)
            GetValues = New Dictionary(Of String, String)

            ' Cicle all languages
            For Each LanguageCode As String In SCFramework.Bridge.Languages.AllCodes(True)
                Try
                    ' Try to get the element value and add to values
                    Dim Code As String = LanguageCode.Replace("-", "_")
                    Dim Value As String = Nothing

                    ' Fix the value if the source containe the serching code
                    If Source.containsKey(Code) Then
                        Value = Source(Code)
                    End If

                    ' Add the language value to the list
                    GetValues.Add(LanguageCode, Value)

                Catch ex As Exception
                End Try
            Next
        End Function

        ' Get the language value
        Public Function GetValue(Language As String) As String
            ' Get all values
            Dim Values As Dictionary(Of String, String) = Me.GetValues()

            ' Search for the value if found return the filtered one
            ' else will return nothing.
            If Values.ContainsKey(Language) Then
                Return Values(Language)
            Else
                Return Nothing
            End If
        End Function

        ' Save the values inside the database translation table
        Public Sub Save()
            ' Check for the translation key
            If Utils.String.IsEmptyOrWhite(Me.TranslationKey) Then
                Throw New Exception("To be saved the translation must be a defined valid key.")
            End If

            ' Get all values and update on the database
            For Each Pair As KeyValuePair(Of String, String) In Me.GetValues()
                ' Try to update the translation.
                ' If not exists (update false) a new translation will be inserted.
                If Not Bridge.Translations.Update(Me.TranslationKey, Pair.Value, Pair.Key) Then
                    Bridge.Translations.Insert(Me.TranslationKey, Pair.Value, Pair.Key)
                End If
            Next
        End Sub

#End Region

    End Class

End Namespace