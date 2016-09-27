'*************************************************************************************************
' 
' [SCFramework]
' HTMLBuilder
' di Samuele Carassai
'
' Classi per la gestione HTML
' Versione 2.2.1
'
'*************************************************************************************************


' Classe per la creazione di Tag HTML
Public Class HTML

    ' Text Formatting
    Public Shared Function ReduceText(ByVal Text As Object, Optional ByVal Len As Integer = 40) As String
        Dim Str As String = ""

        If IsNothing(Text) Or IsDBNull(Text) Then Return ""
        If Text.Length > Len Then
            Dim Title As String = Mid(Replace(Text, "'", ""), 1, 2048)
            If CStr(Text).Length > 2048 Then
                Title &= " ..."
            End If

            Str &= "<acronym title='" & HttpUtility.HtmlEncode(Title) & "'>"
            Str &= Mid(Text, 1, Len - 4) & " ..."
            Str &= "</acronym>"
        Else
            Str = Text
        End If
        Return Str
    End Function

    Public Shared Function SimpleReduceText(ByVal Text As Object, Optional ByVal Len As Integer = 40) As String
        Dim Str As String = ""

        If IsNothing(Text) Or IsDBNull(Text) Then Return ""
        If Text.Length > Len Then
            Str &= Mid(Text, 1, Len - 4) & " ..."
        Else
            Str = Text
        End If
        Return Str
    End Function

    Private Shared Function ConvertUrlsToLinks(ByVal Value As String) As String
        Dim Regex As String = "((www\.|(http|https|ftp|news|file)+\:\/\/)[&#95;.a-z0-9-]+\.[a-z0-9\/&#95;:@=.+?,##%&~-]*[^.|\'|\# |!|\(|?|,| |>|<|;|\)])"
        Dim R As Regex = New Regex(Regex, RegexOptions.IgnoreCase)
        Return R.Replace(Value, "<a href=""$1"" target=""&#95;blank"">$1</a>").Replace("href=""www", "href=""http://www")
    End Function

    Private Shared Function ConvertEMailToLinks(ByVal Value As String) As String
        Dim emailregex As Regex = New Regex("([a-zA-Z_0-9.-]+\@[a-zA-Z_0-9.-]+\.\w+)", (RegexOptions.IgnoreCase Or RegexOptions.Compiled))
        Dim strContent As String = emailregex.Replace(Value, "<a href=mailto:$1>$1</a>")

        Return strContent
    End Function

    Public Shared Function SimpleFormat(ByVal Text As Object, _
                                        Optional ByVal ConvertUrlsToLinks As Boolean = False, _
                                        Optional ByVal ConvertEMailToLinks As Boolean = False) As String
        If Not IsDBNull(Text) AndAlso Not String.IsNullOrEmpty(Text) Then
            Text = Replace("" & Text, vbCrLf, "<br />")
            Text = Replace("" & Text, vbCr, "<br />")
            Text = Replace("" & Text, vbLf, "<br />")
            Text = Replace("" & Text, vbTab, "&nbsp;&nbsp;&nbsp;&nbsp;")

            If ConvertUrlsToLinks Then Text = HTML.ConvertUrlsToLinks(Text)
            If ConvertEMailToLinks Then Text = HTML.ConvertEMailToLinks(Text)

            Return Text
        Else
            Return String.Empty
        End If
    End Function

    Public Shared Function HTMLToPlain(Text As Object) As String
        If Not IsDBNull(Text) AndAlso Not String.IsNullOrEmpty(Text) Then
            Text = Replace("" & Text, "<br />", vbCrLf)
            Text = HTML.StripTags(Text)
            Text = HTML.SimpleFormat(Text)

            Return Text
        Else
            Return String.Empty
        End If
    End Function


    ' Removes tags from passed HTML
    Public Shared Function StripTags(ByVal HTML As String, Optional KeepMultiline As Boolean = False) As String
        HTML = HTML.Replace(Chr(13), String.Empty)
        HTML = HTML.Replace(Chr(10), String.Empty)

        Dim Doc As HtmlAgilityPack.HtmlDocument = New HtmlAgilityPack.HtmlDocument()
        Doc.LoadHtml(HTML)

        Dim Nodes As HtmlAgilityPack.HtmlNodeCollection = Doc.DocumentNode.SelectNodes("//text()")
        Dim Text As String = String.Empty

        If Nodes IsNot Nothing Then
            For Each Node As HtmlAgilityPack.HtmlNode In Nodes
                If Trim(Node.InnerText) <> String.Empty Then
                    If KeepMultiline And Not String.IsNullOrEmpty(Text) Then
                        Text &= vbCrLf
                    End If
                    Text &= Trim(Node.InnerText)
                End If
            Next
        End If

        Return Text
    End Function

    Public Shared Function ReduceAndClean(ByVal Text As Object, Optional ByVal Len As Integer = 40) As String
        If IsNothing(Text) Or IsDBNull(Text) Then Return ""
        Dim Cleaned As String = StripTags(Text)
        If Bridge.Server IsNot Nothing Then
            Cleaned = Bridge.Server.HtmlDecode(Cleaned)
        End If
        Return ReduceText(Cleaned, Len)
    End Function

    Public Shared Function SimpleReduceAndClean(ByVal Text As Object, Optional ByVal Len As Integer = 40) As String
        If IsNothing(Text) Or IsDBNull(Text) Then Return ""
        Dim Cleaned As String = StripTags(Text)
        If Bridge.Server IsNot Nothing Then
            Cleaned = Bridge.Server.HtmlDecode(Cleaned)
        End If
        Return SimpleReduceText(Cleaned, Len)
    End Function


    ' Get tags structure 
    Public Shared Function GetTagsStructure(HTML As String, Optional CreateBookmarks As Boolean = True) As String
        ' Lines
        Dim Content As String = SCFramework.HTML.StripTags(HTML, True)
        Dim Lines() As String = Content.Split(vbCrLf)

        ' Cycle
        For Index As Integer = 0 To Lines.Length - 1
            ' Choice replacement
            Dim Replacement As String = String.Empty
            If CreateBookmarks Then
                Replacement = "{" & Index & "}"
            End If

            ' Replace
            Dim Value As String = Lines(Index)
            Value = Utils.TrimAndClearCrLf(Value)

            HTML = HTML.Replace(Value, Replacement)
        Next

        ' Return
        Return HTML
    End Function


    ' Get links by URL
    Private Shared Function IsValidLink(Link As String) As Boolean
        'Ignore all anchor links
        If Link.StartsWith("#") Then
            Return False
        End If

        'Ignore all javascript calls
        If Link.ToLower.StartsWith("javascript:") Then
            Return False
        End If

        'Ignore all email links
        If Link.ToLower.StartsWith("mailto:") Then
            Return False
        End If

        Return True
    End Function

    Private Shared Function ExtractLinksFromHTMLDocument(Document As HtmlAgilityPack.HtmlDocument) As ArrayList
        Dim Nodes As HtmlAgilityPack.HtmlNodeCollection = Document.DocumentNode.SelectNodes("//a[@href]")
        Dim List As ArrayList = New ArrayList()

        If Nodes IsNot Nothing Then
            For Each Node As HtmlAgilityPack.HtmlNode In Nodes
                Dim Att As HtmlAgilityPack.HtmlAttribute = Node.Attributes("href")
                Dim Link As String = Att.Value

                If IsValidLink(Link) Then
                    List.Add(Link)
                End If
            Next
        End If

        Return List
    End Function

    Public Shared Function GetLinksByURL(URL As String) As ArrayList
        Try
            Dim Web As HtmlAgilityPack.HtmlWeb = New HtmlAgilityPack.HtmlWeb()
            Dim Document As HtmlAgilityPack.HtmlDocument = Web.Load(URL)
            Return HTML.ExtractLinksFromHTMLDocument(Document)

        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Shared Function GetLinksByHTML(HTML As String) As ArrayList
        Try
            Dim Document As HtmlAgilityPack.HtmlDocument = New HtmlAgilityPack.HtmlDocument()
            Document.LoadHtml(HTML)
            Return SCFramework.HTML.ExtractLinksFromHTMLDocument(Document)

        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Shared Function GetTitleByHTML(HTML As String) As String
        Try
            Dim Document As HtmlAgilityPack.HtmlDocument = New HtmlAgilityPack.HtmlDocument()
            Document.LoadHtml(HTML)

            Dim Node As HtmlAgilityPack.HtmlNode = Document.DocumentNode.SelectSingleNode("//head/title")
            Return Node.InnerText

        Catch ex As Exception
            Return Nothing
        End Try
    End Function


    ' Javascript
    Public Shared Function FixJavaScriptString(ByVal Value As String) As String
        Value = Replace(Value, "\", "\\")
        Value = Replace(Value, """", "\""")
        Value = Replace(Value, "'", "\'")
        Value = Replace(Value, vbCrLf, "\n")
        Value = Replace(Value, vbCr, "\n")
        Value = Replace(Value, vbTab, "\t")
        Return Value
    End Function

    Public Shared Function CreateJavaScript(ByVal Script As String) As String
        Return "<script type='text/javascript'>" & Script & "</script>"
    End Function


    ' Dialog
    Public Shared Sub ShowJavaMessage(ByVal message As String, ByVal page As Page)
        message = FixJavaScriptString(message)

        Dim script As String = ""
        script &= "<script type='text/javascript'>" & vbCrLf
        script &= vbTab & "function showMessage() {" & vbCrLf
        script &= vbTab & vbTab & "alert('" & message & "');" & vbCrLf
        script &= vbTab & "}" & vbCrLf
        script &= vbCrLf
        script &= vbTab & "if (window.attachEvent)" & vbCrLf
        script &= vbTab & vbTab & "window.attachEvent('onload', showMessage);" & vbCrLf
        script &= vbTab & "else" & vbCrLf
        script &= vbTab & vbTab & "document.addEventListener('DOMContentLoaded', showMessage);" & vbCrLf
        script &= "</script>" & vbCrLf

        page.ClientScript.RegisterClientScriptBlock(page.GetType, "ShowError", script)
    End Sub

    Public Shared Function CreateJavaAlertScript(ByVal message As String) As String
        message = FixJavaScriptString(message)
        Return "<script>alert('" & message & "');</script>"
    End Function


    ' Control
    Public Shared Function ControlToHtml([Control] As Control) As String
        Dim SB As System.Text.StringBuilder = New System.Text.StringBuilder()
        Dim SW As System.IO.StringWriter = New System.IO.StringWriter(SB)
        Dim HW As System.Web.UI.HtmlTextWriter = New System.Web.UI.HtmlTextWriter(SW)
        [Control].RenderControl(HW)
        Return SB.ToString()
    End Function


End Class
