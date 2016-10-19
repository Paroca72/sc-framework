'*************************************************************************************************
' 
' [SCFramework]
' Utils  
' by Samuele Carassai
'
' Utilities class
' Version 5.0.0
' Created 09/10/2016
' Updated 19/10/2016
'
'*************************************************************************************************


Namespace Utils

    '------------------------------------------------------------------------------------------
    ' YouTube 

    Public Class YouTube

        ' Get the thumbnail of a youtube video from the related url
        Public Shared Function GetThumbnailLink(ByVal URL As String) As String
            If URL.Contains("youtube") Or URL.Contains("youtu.be") Then
                If URL.Contains("?") Then URL = URL.Remove(URL.LastIndexOf("?"))
                If URL.Contains("/") Then URL = URL.Substring(URL.LastIndexOf("/") + 1)
                URL = "http://i1.ytimg.com/vi/" & URL & "/hqdefault.jpg"
            End If

            Return URL
        End Function

        ' Get the youtube video embended link
        Public Shared Function GetEmbedLink(ByVal URL As String) As String
            If URL.Contains("youtube") Or URL.Contains("youtu.be") Then
                If URL.Contains("?") Then URL = URL.Remove(URL.LastIndexOf("?"))
                If URL.Contains("/") Then URL = URL.Substring(URL.LastIndexOf("/") + 1)
                URL = "http://www.youtube.com/embed/" & URL & "?rel=0&amp;wmode=transparent"
            End If

            Return URL
        End Function

    End Class


    '------------------------------------------------------------------------------------------
    ' URL

    Public Class URL

        ' Check if a passed URL (in string format) is valid
        Public Shared Function IsValid(ByVal URL As String) As Boolean
            Dim RE As Regex = New Regex("((([A-Za-z]{3,9}:(?:\/\/)?)(?:[-;:&=\+\$,\w]+@)?[A-Za-z0-9.-]+|(?:www.|[-;:&=\+\$,\w]+@)[A-Za-z0-9.-]+)((?:\/[\+~%\/.\w-_]*)?\??(?:[-\+=&;%@.\w_]*)#?(?:[\w]*))?)")
            Return RE.IsMatch(URL)
        End Function

    End Class


    '------------------------------------------------------------------------------------------
    ' List control

    Public Class ListControl

        ' Check if at least one items of the list control is checked
        Public Shared Function AtLeastOne([Control] As System.Web.UI.WebControls.ListControl) As Boolean
            For Each Item As ListItem In [Control].Items
                If Item.Selected Then
                    Return True
                End If
            Next
            Return False
        End Function

        ' Check if all the items are checked
        Public Shared Function IsAllSelected([Control] As System.Web.UI.WebControls.ListControl) As Boolean
            For Each Item As ListItem In [Control].Items
                If Not Item.Selected Then
                    Return False
                End If
            Next
            Return True
        End Function

    End Class


    '------------------------------------------------------------------------------------------
    ' EMail

    Public Class EMail

        ' Check if all the email passed is in a valid format
        Public Shared Function IsValid(ParamArray EMails() As String) As Boolean
            ' If find at least one email not valid exists
            For Each Email As String In EMails
                If Not Not SCFramework.Utils.EMail.IsValid(Email) Then
                    Return False
                End If
            Next
            ' Else all email are valid
            Return True
        End Function

        ' Check if an email is in a valid format
        Public Shared Function IsValid(ByVal Mail As String) As Boolean
            Dim RE As Regex = New Regex("[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?")
            Return RE.IsMatch(Mail)
        End Function

    End Class


    '------------------------------------------------------------------------------------------
    ' String

    Public Class [String]

        ' Check if a string is empty or only compose by white space
        Public Shared Function IsEmptyOrWhite(Value As String) As Boolean
            Return String.IsNullOrEmpty(Value) Or String.IsNullOrWhiteSpace(Value)
        End Function

    End Class


    '------------------------------------------------------------------------------------------
    ' GUID

    Public Class [GUID]

        ' Convert a GUID in a string
        Public Shared Function GuidToString(ByVal [Guid] As System.Guid) As String
            Return [Guid].ToString("N")
        End Function

        ' Create a new GUID and convert it a new string
        Public Shared Function GuidToString() As String
            Return SCFramework.Utils.GUID.GuidToString(System.Guid.NewGuid)
        End Function

        ' Check if a string is a GUID
        Public Shared Function IsGuid(Value As String) As Boolean
            Try
                Dim G As System.Guid = New System.Guid(Value)
                Return True

            Catch ex As Exception
                Return False
            End Try
        End Function

    End Class


    '------------------------------------------------------------------------------------------
    ' DataView

    Public Class DataTable

        ' Convert a DataView column in an array of object
        Public Shared Function ToArray(ByVal Source As System.Data.DataTable, ByVal ColumnName As String, Optional Where As Func(Of Object, Boolean) = Nothing) As Object()
            ' Check if column exists
            If Not Source.Columns.Contains(ColumnName) Then Return Nothing

            ' Convert
            Return (From Row As DataRow In Source.AsEnumerable() Select Row(ColumnName)) _
                .Where(Where) _
                .ToArray()
        End Function

        ' Convert a DataView column in an array of object
        Public Shared Function ToDictionary(ByVal Source As System.Data.DataTable, ByVal KeyField As String, ValueField As String,
                                            Optional Where As Func(Of Object, Boolean) = Nothing) As Dictionary(Of Object, Object)
            ' Check if column exists
            If Not Source.Columns.Contains(KeyField) Or Not Source.Columns.Contains(ValueField) Then Return Nothing

            ' Convert
            Return Source _
                .AsEnumerable() _
                .Where(Where) _
                .ToDictionary(Function(Key) Key(KeyField), Function(Value) Value(ValueField))
        End Function

        ' Find the next ID value
        Public Shared Function NextID(ByVal Source As System.Data.DataTable, ByVal ColumnName As String) As Long
            Return (From Row In Source.AsEnumerable() Select Row(ColumnName)).Max() + 1
        End Function

        ' Define the auto-incremental fields in the data table
        Public Shared Sub SetAutoIncrements(ByVal Source As System.Data.DataTable, ByVal ParamArray Fields() As String)
            For Each Name As String In Fields
                Try
                    Dim Column As DataColumn = Source.Columns(Name)
                    Column.AutoIncrement = True
                    Column.AutoIncrementStep = 1
                    Column.AutoIncrementSeed = SCFramework.Utils.DataTable.NextID(Source, Name)

                Catch ex As Exception
                End Try
            Next
        End Sub

        ' Define the primary keys fields in the data table
        Public Shared Sub SetPrimaryKeys(ByVal Source As System.Data.DataTable, ByVal ParamArray Fields() As String)
            Dim Columns(Fields.Length - 1) As System.Data.DataColumn
            For Index As Integer = 0 To Fields.Length - 1
                Dim Field As String = Fields(Index)
                Columns(Index) = Source.Columns(Field)
            Next

            Source.PrimaryKey = Columns
        End Sub

    End Class


    '------------------------------------------------------------------------------------------
    ' IP

    Public Class IP

        ' Check for private IP network
        Private Shared Function IsPrivateIP(ByVal CheckIP As String) As Boolean
            Dim Quad1, Quad2 As Integer

            Quad1 = CInt(CheckIP.Substring(0, CheckIP.IndexOf(".")))
            Quad2 = CInt(CheckIP.Substring(CheckIP.IndexOf(".") + 1).Substring(0, CheckIP.IndexOf(".")))
            Select Case Quad1
                Case 10
                    Return True
                Case 172
                    If Quad2 >= 16 And Quad2 <= 31 Then Return True
                Case 192
                    If Quad2 = 168 Then Return True
            End Select
            Return False
        End Function

        ' Get the local IP
        Public Shared Function GetLocal() As String
            Dim IPList As System.Net.IPHostEntry = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName)

            For Each Address As IPAddress In IPList.AddressList
                'Only return IPv4 routable IPs
                If (Address.AddressFamily = Sockets.AddressFamily.InterNetwork) AndAlso (Not IsPrivateIP(Address.ToString)) Then
                    Return Address.ToString
                End If
            Next
            Return ""
        End Function

    End Class

End Namespace
