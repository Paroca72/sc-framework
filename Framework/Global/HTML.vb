'*************************************************************************************************
' 
' [SCFramework]
' HTML
' by Samuele Carassai
'
' HTML parser
' Version 5.0.0
' Created --/--/----
' Updated 19/10/2016
'
'*************************************************************************************************


Public Class HTML

    ' Static settings
    Private Shared ABBREVIATION_TITLE_LEN As Integer = 2048
    Private Shared ABBREVIATION_SUFFIX As String = " ..."


    ' Reduce to text within the passed Length
    Public Shared Function ReduceText(ByVal Text As String,
                                      Optional ByVal MaxLength As Integer = 40,
                                      Optional CreateAbbrTag As Boolean = True) As String
        ' Check for empty values and reduce the text only if the text length is over the limit
        If SCFramework.Utils.String.IsEmptyOrWhite(Text) OrElse Text.Length <= MaxLength Then
            Return Text

        Else
            ' Fix the text length
            MaxLength = MaxLength - HTML.ABBREVIATION_SUFFIX.Length

            ' Create the abbreviation tag
            If CreateAbbrTag Then
                ' Title
                Dim MaxTitleLen As Integer = HTML.ABBREVIATION_TITLE_LEN - HTML.ABBREVIATION_SUFFIX.Length
                Dim Title As String = IIf(Text.Length > MaxTitleLen, Text.Substring(1, MaxTitleLen), Text)

                ' Return
                Return String.Format("<abbr title=""{0}"">{1} ...</abbr>",
                                 HttpUtility.HtmlEncode(Title),
                                 Text.Substring(1, MaxLength))
            Else
                ' Simply reduce
                Return String.Format("{0} ...", Text.Substring(1, MaxLength))
            End If
        End If
    End Function

End Class
