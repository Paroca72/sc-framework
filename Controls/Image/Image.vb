'*************************************************************************************************
' 
' [SCFramework]
' Image Cached
' di Samuele Carassai
'
' Definisce un componente Image
' Versione 1.5.0
'
'*************************************************************************************************


Namespace WebControls

    <ToolboxData("<{0}:Image runat=""server"" />")> _
    Partial Public Class Image : Inherits Global.System.Web.UI.WebControls.Literal

#Region " PUBLIC ENUM "

        Public Enum ResizeModes As Integer
            Resize = 0
            Crop = 1
        End Enum

        Public Enum CropAligns As Integer
            None = 0
            Both = 1
            Horizontal = 2
            Vertical = 3
        End Enum

#End Region

#Region " PUBLIC PROPERTIES "

        <Category("Behavior"), _
         DefaultValue(""), _
         Description("Set image url")> _
        Public Property ImageUrl As String
            Set(value As String)
                ViewState("ImageUrl") = value
            End Set
            Get
                If ViewState("ImageUrl") Is Nothing Then
                    ViewState("ImageUrl") = String.Empty
                End If
                Return CStr(ViewState("ImageUrl"))
            End Get
        End Property

        <Category("Behavior"), _
         Description("Define the image width")> _
        Public Property Width As Unit
            Set(value As Unit)
                ViewState("Width") = value
            End Set
            Get
                If ViewState("Width") Is Nothing Then
                    ViewState("Width") = Unit.Empty
                End If
                Return ViewState("Width")
            End Get
        End Property

        <Category("Behavior"), _
         Description("Define the image height")> _
        Public Property Height As Unit
            Set(value As Unit)
                ViewState("Height") = value
            End Set
            Get
                If ViewState("Height") Is Nothing Then
                    ViewState("Height") = Unit.Empty
                End If
                Return ViewState("Height")
            End Get
        End Property

        <Category("Behavior"), _
         DefaultValue(800), _
         Description("Define the cache image width")> _
        Public Property CacheWidth As Integer
            Set(value As Integer)
                ViewState("CacheWidth") = value
            End Set
            Get
                If ViewState("CacheWidth") Is Nothing Then
                    ViewState("CacheWidth") = 800
                End If
                Return ViewState("CacheWidth")
            End Get
        End Property

        <Category("Behavior"), _
         DefaultValue(600), _
         Description("Define the cache image height")> _
        Public Property CacheHeight As Integer
            Set(value As Integer)
                ViewState("CacheHeight") = value
            End Set
            Get
                If ViewState("CacheHeight") Is Nothing Then
                    ViewState("CacheHeight") = 600
                End If
                Return ViewState("CacheHeight")
            End Get
        End Property

        <Category("Behavior"), _
         DefaultValue(""), _
         Description("Define the class style.")> _
        Public Property CssClass As String
            Set(value As String)
                ViewState("CssClass") = value
            End Set
            Get
                If ViewState("CssClass") Is Nothing Then
                    ViewState("CssClass") = String.Empty
                End If
                Return CStr(ViewState("CssClass"))
            End Get
        End Property

        <Category("Behavior"), _
         DefaultValue(""), _
         Description("Define the image tooltip.")> _
        Public Property ToolTip As String
            Set(value As String)
                ViewState("ToolTip") = value
            End Set
            Get
                If ViewState("ToolTip") Is Nothing Then
                    ViewState("ToolTip") = String.Empty
                End If
                Return CStr(ViewState("ToolTip"))
            End Get
        End Property

        <Category("Behavior"), _
         DefaultValue(ResizeModes.Resize), _
         Description("Set the mode to resize image. Crop or simple resize.")> _
        Public Property ResizeMode As ResizeModes
            Set(value As ResizeModes)
                ViewState("ResizeMode") = value
            End Set
            Get
                If ViewState("ResizeMode") Is Nothing Then
                    ViewState("ResizeMode") = ResizeModes.Resize
                End If
                Return CStr(ViewState("ResizeMode"))
            End Get
        End Property

        <Category("Behavior"), _
         DefaultValue(CropAligns.Both), _
         Description("Set the image align when image is cropped.")> _
        Public Property CropAlign As CropAligns
            Set(value As CropAligns)
                ViewState("CropAligns") = value
            End Set
            Get
                If ViewState("CropAligns") Is Nothing Then
                    ViewState("CropAligns") = CropAligns.Both
                End If
                Return CStr(ViewState("CropAligns"))
            End Get
        End Property

#End Region

#Region " PRIVATE PROPERTIES "

#End Region

#Region " EVENTS "

        Private Sub Image_Load(sender As Object, e As System.EventArgs) Handles Me.Load
        End Sub

        Protected Overrides Sub Render(writer As System.Web.UI.HtmlTextWriter)
            If Not Me.DesignMode Then
                ' Build
                Dim Style As String = String.Empty
                If Not Me.Width.IsEmpty Or Not Me.Height.IsEmpty Then
                    If Not Me.Width.IsEmpty Then Style = String.Format("width: {0};", Me.Width.ToString())
                    If Not Me.Height.IsEmpty Then Style = String.Format("height: {0};", Me.Height.ToString())

                    Style = String.Format(" style=""{0}""", Style)
                End If

                Dim [Class] As String = String.Empty
                If String.IsNullOrEmpty(Me.CssClass) Then
                    [Class] = String.Format(" class=""{0}""", Me.CssClass)
                End If

                Dim Tooltip As String = String.Empty
                If String.IsNullOrEmpty(Me.ToolTip) Then
                    Tooltip = String.Format(" tooltip=""{0}""", Me.ToolTip)
                End If

                Dim NewName As String = String.Empty
                Select Case Me.ResizeMode
                    Case ResizeModes.Resize
                        NewName = SCFramework.Cache.AddImage(Me.ImageUrl, Me.CacheWidth, Me.CacheHeight)

                    Case ResizeModes.Crop
                        Dim CenterHor As Boolean = (Me.CropAlign = CropAligns.Both Or Me.CropAlign = CropAligns.Horizontal)
                        Dim CenterVer As Boolean = (Me.CropAlign = CropAligns.Both Or Me.CropAlign = CropAligns.Vertical)

                        NewName = SCFramework.Cache.AddCroppedImage(Me.ImageUrl, Me.CacheWidth, Me.CacheHeight, CenterHor, CenterVer)
                End Select

                If Not String.IsNullOrEmpty(NewName) Then
                    Dim FilePath As String = Me.ResolveUrl(NewName)
                    Dim HTML As String = String.Format("<img src='{0}'{1}{2}{3} alt="""" />", FilePath, Style, [Class], Tooltip)

                    writer.Write(HTML)
                End If
            Else
                MyBase.Render(writer)
            End If
        End Sub

#End Region

    End Class

End Namespace
