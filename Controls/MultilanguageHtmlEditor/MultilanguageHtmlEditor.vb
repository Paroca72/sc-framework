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
        ToolboxData("<{0}:MultilanguageHtmlEditor runat=server></{0}:MultilanguageHtmlEditor>")>
    Public Class MultilanguageHtmlEditor
        Inherits WebControl

        ' Constants
        Private Const STATIC_CSS_RESOURCE_NAME As String = "SCFramework.MultilanguageHtmlEditor.css"
        Private Const STATIC_SCRIPT_RESOURCE_NAME As String = "SCFramework.MultilanguageHtmlEditor.js"

        Private Const STATIC_SCRIPT_RESOURCE_UID As String = "MultilanguageHtmlEditor-StaticScript"
        Private Const DYNAMIC_SCRIPT_RESOURCE_UID As String = "MultilanguageHtmlEditor-DynamicScript"

        Private Const TINYMCE_CDN_URL As String = "http://cdn.tinymce.com/4/tinymce.min.js"
        Private Const TINYMCE_RESOURCE_UID As String = "TinyMCE-StaticScript"


#Region " CONTRUCTOR "

        Public Sub New()
            ' Define the container as a DIV
            MyBase.New(HtmlTextWriterTag.Div)
        End Sub

#End Region

#Region " EVENTS "

        ' When the control is loaded
        Private Sub MultilanguageHtmlEditor_Load(sender As Object, e As System.EventArgs) Handles Me.Load
            ' If not in design mode
            If Not Me.DesignMode Then
                ' Include the resources
                Me.IncludeCSSResources()
                Me.IncludeScriptsResources()
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
            ' Define the component ID is not exists
            If String.IsNullOrEmpty(Me.ID) Then
                Me.ID = "MLHE_" & Crypt.CreateUniqueKey(10)
            End If

            ' Create the html editor (TinyMCE)
            Dim TinyMCE As TextBox = New TextBox()
            TinyMCE.TextMode = TextBoxMode.MultiLine
            TinyMCE.CssClass = "MultilanguageHtmlEditor"

            ' Create the values holder
            Dim Holder As HiddenField = New HiddenField()
            Holder.ID = Me.ID & "_Holder"

            ' Render the controls
            TinyMCE.RenderControl(writer)
            Holder.RenderControl(writer)
        End Sub

#End Region

#Region " PRIVATES METHODS "

        ' Check if is a call-back or a post-back
        Private Function IsCallback() As Boolean
            Dim SM As ScriptManager = ScriptManager.GetCurrent(Me.Page)
            Return SM IsNot Nothing AndAlso SM.IsInAsyncPostBack
        End Function

        ' Load the CSS resource
        Private Sub IncludeCSSResources()
            ' Get the resource URL
            Dim HREF As String = Page.ClientScript.GetWebResourceUrl(Me.GetType(), MultilanguageHtmlEditor.STATIC_CSS_RESOURCE_NAME)

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

        ' Load the scripts resource
        Private Sub IncludeScriptsResources()
            ' Create the buttons by languages codes
            Dim Clauses As DbClauses = New DbClauses("VISIBLE", DbClauses.ComparerType.Equal, True)
            Dim Buttons As String = String.Empty

            For Each Row As DataRow In Bridge.Languages.GetSource(Clauses).Rows
                If String.IsNullOrEmpty(Buttons) Then Buttons &= ", "
                Buttons = "{text: " & Row!TITLE & "}"
            Next

            ' Create the dynamic script
            Dim DynamicScript As String = "for (edId in tinyMCE.editors)" &
                                            "tinyMCE.editors[edId].addButton('Languages', {" &
                                                "type: 'buttongroup'," &
                                                "items: [" &
                                                    Buttons &
                                                "]" &
                                            "}"

            ' If the page have associated a script manager (Ajax) add the script resource directly by the script manager.
            ' Else register the script by the page client script is not already registered.
            If ScriptManager.GetCurrent(Me.Page) IsNot Nothing Then
                ' Add by script manager
                ScriptManager.RegisterClientScriptInclude(
                Me.Page, Me.GetType(), MultilanguageHtmlEditor.TINYMCE_RESOURCE_UID, MultilanguageHtmlEditor.STATIC_SCRIPT_RESOURCE_NAME)
                ScriptManager.RegisterClientScriptResource(
                Me.Page, Me.GetType(), MultilanguageHtmlEditor.STATIC_SCRIPT_RESOURCE_NAME)

            Else
                ' Check if already registered
                If Not Page.ClientScript.IsClientScriptIncludeRegistered(MultilanguageHtmlEditor.TINYMCE_RESOURCE_UID) Then
                    ' Get the script resource URL and register it on the page
                    Dim URL As String = Me.ResolveUrl(MultilanguageHtmlEditor.TINYMCE_CDN_URL)
                    Page.ClientScript.RegisterClientScriptInclude(MultilanguageHtmlEditor.TINYMCE_RESOURCE_UID, URL)
                End If

                ' Check if already registered
                If Not Page.ClientScript.IsClientScriptIncludeRegistered(MultilanguageHtmlEditor.STATIC_SCRIPT_RESOURCE_UID) Then
                    ' Get the script resource URL and register it on the page
                    Dim URL As String = Page.ClientScript.GetWebResourceUrl(Me.GetType(), MultilanguageHtmlEditor.STATIC_SCRIPT_RESOURCE_NAME)
                    Page.ClientScript.RegisterClientScriptInclude(MultilanguageHtmlEditor.STATIC_SCRIPT_RESOURCE_UID, URL)
                End If
            End If
        End Sub

#End Region

#Region " PUBLIC PROPERTIES "

        <Bindable(True),
     Category("Behavior"),
     DefaultValue(""),
     Description("Set the key of translation")>
        Public Property LanguageKey As String
            Set(value As String)
                ViewState("LanguageKey") = value
            End Set
            Get
                If ViewState("LanguageKey") Is Nothing Then
                    ViewState("LanguageKey") = String.Empty
                End If
                Return CStr(ViewState("LanguageKey"))
            End Get
        End Property

#End Region

    End Class

End Namespace