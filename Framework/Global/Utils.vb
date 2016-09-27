'*************************************************************************************************
' 
' [SCFramework]
' Generic
' di Samuele Carassai
'
' Classi di routines generiche
' Versione 3.3.6
'
'*************************************************************************************************


Public Class Utils

    ' Youtube
    Public Shared Function GetYoutubeThumbnailLink(ByVal URL As String) As String
        If URL.Contains("youtube") Or URL.Contains("youtu.be") Then
            If URL.Contains("?") Then URL = URL.Remove(URL.LastIndexOf("?"))
            If URL.Contains("/") Then URL = URL.Substring(URL.LastIndexOf("/") + 1)
            URL = "http://i1.ytimg.com/vi/" & URL & "/hqdefault.jpg"
        End If

        Return URL
    End Function

    Public Shared Function GetYoutubeEmbedLink(ByVal URL As String) As String
        If URL.Contains("youtube") Or URL.Contains("youtu.be") Then
            If URL.Contains("?") Then URL = URL.Remove(URL.LastIndexOf("?"))
            If URL.Contains("/") Then URL = URL.Substring(URL.LastIndexOf("/") + 1)
            URL = "http://www.youtube.com/embed/" & URL & "?rel=0&amp;wmode=transparent"
        End If

        Return URL
    End Function


    ' URL
    Public Shared Function ConvertRelativeToAbsoluteURL(ByVal AppDomain As String, ByVal Relative As String) As String
        If Relative.StartsWith("http://") Or Relative.StartsWith("https://") Then
            Return Relative
        End If

        If Relative.StartsWith("..") Then
            Relative = Replace(Relative, "..", "~", 1, 1)
        End If

        If Not Relative.StartsWith("~/") Then
            Relative = "~/" & Relative
        ElseIf Relative.StartsWith("/") Then
            Relative = "~" & Relative
        End If

        Dim Params As String = String.Empty
        Dim Pos As Integer = Relative.IndexOf("?")
        If Pos > 0 Then
            Params = Relative.Substring(Pos)
            Relative = Relative.Substring(0, Pos - 1)
        End If

        Dim Absolute As String = AppDomain & VirtualPathUtility.ToAbsolute(Relative) & Params
        Return Absolute
    End Function

    Public Shared Function ConvertRelativeToAbsoluteURL(ByVal RelativePath As String) As String
        If Bridge.Request IsNot Nothing Then
            Return Utils.ConvertRelativeToAbsoluteURL(Utils.GetAppURLDomain(), RelativePath)
        Else
            Return Nothing
        End If
    End Function

    Public Shared Function GetAppURLDomain() As String
        If Bridge.Request IsNot Nothing Then
            Dim Port As String = Bridge.Request.Url.Port.ToString()
            If Port <> "80" Then
                Port = ":" & Port
            Else
                Port = String.Empty
            End If

            Return String.Format("{0}://{1}{2}", Bridge.Request.Url.Scheme, _
                                                 Bridge.Request.Url.Host, _
                                                 Port)
        Else
            Return Nothing
        End If
    End Function

    Public Shared Function FixURLValue(ByVal Value As String) As String
        If Not String.IsNullOrEmpty(Value) Then
            If Not Value.StartsWith("http://") Then
                Return "http://" & Value
            End If
        End If

        Return Value
    End Function

    Public Shared Function IsValidURL(ByVal URL As String) As Boolean
        Dim RE As Regex = New Regex("((([A-Za-z]{3,9}:(?:\/\/)?)(?:[-;:&=\+\$,\w]+@)?[A-Za-z0-9.-]+|(?:www.|[-;:&=\+\$,\w]+@)[A-Za-z0-9.-]+)((?:\/[\+~%\/.\w-_]*)?\??(?:[-\+=&;%@.\w_]*)#?(?:[\w]*))?)")
        Return RE.IsMatch(URL)
    End Function


    ' Currency
    Public Shared Function ToEuro(ByVal Value As Double) As String
        Dim Culture As Globalization.CultureInfo = Globalization.CultureInfo.CreateSpecificCulture("it-IT")
        Return Value.ToString("c", Culture)
    End Function


    ' List control
    Public Shared Sub SelectOption(ByVal [Control] As ListControl, ByVal Value As String)
        [Control].SelectedIndex = FindIndexByValue([Control], Value)
    End Sub

    Public Shared Sub SelectOptionByText(ByVal [Control] As ListControl, ByVal Text As String)
        [Control].SelectedIndex = FindIndexByText([Control], Text)
    End Sub

    Public Shared Function FindIndexByValue(ByVal [Control] As ListControl, ByVal Value As String) As Integer
        Dim Item As ListItem = [Control].Items.FindByValue(Value)
        Return [Control].Items.IndexOf(Item)
    End Function

    Public Shared Function FindIndexByText(ByVal [Control] As ListControl, ByVal Text As String) As Integer
        Dim Item As ListItem = [Control].Items.FindByText(Text)
        Return [Control].Items.IndexOf(Item)
    End Function

    Public Shared Function RetriveTextByValue(ByVal [Control] As ListControl, ByVal Value As String) As String
        Dim Item As ListItem = [Control].Items.FindByValue(Value)
        Return Item.Text
    End Function

    Public Shared Sub PushEmptyValuedOption(ByVal [Control] As ListControl, ByVal Text As String, Optional ByVal ToEnd As Boolean = False)
        Dim Item As ListItem = New ListItem(Text, "")
        If ToEnd Then
            [Control].Items.Add(Item)
        Else
            [Control].Items.Insert(0, Item)
        End If
    End Sub

    Public Shared Sub FillListControl(ByVal [Control] As ListControl, ByVal Source As Hashtable, _
                                      Optional ByVal SelectedValue As String = Nothing, _
                                      Optional ByVal AddEmptyField As Boolean = False, _
                                      Optional ByVal Sort As Boolean = True)
        [Control].Items.Clear()
        For Each Key As String In Source.Keys
            Dim Item As ListItem = New ListItem(Source(Key), Key)
            [Control].Items.Add(Item)
        Next

        If Sort Then SortListItem([Control])
        If Not SelectedValue Is Nothing Then SelectOption([Control], SelectedValue)
        If AddEmptyField Then PushEmptyValuedOption([Control], "")
    End Sub

    Private Class SortItemComparer
        Implements IComparer

        Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements Global.System.Collections.IComparer.Compare
            Dim item_1 As ListItem = CType(x, ListItem)
            Dim item_2 As ListItem = CType(y, ListItem)

            If item_1 Is Nothing And item_2 Is Nothing Then
                Return 0
            ElseIf item_1 Is Nothing Then
                Return -1
            ElseIf item_2 Is Nothing Then
                Return 1
            Else
                Return item_1.Text.CompareTo(item_2.Text)
            End If
        End Function

    End Class

    Public Shared Function SortListItem(ByVal [Control] As ListControl, Optional ByVal ApplyToThis As Boolean = True) As ListItem()
        Dim Collection As ListItemCollection = [Control].Items
        Dim Items(Collection.Count - 1) As ListItem

        Collection.CopyTo(Items, 0)
        Global.System.Array.Sort(Items, New SortItemComparer)

        If ApplyToThis Then
            [Control].Items.Clear()
            [Control].Items.AddRange(Items)
        End If
        Return Items
    End Function

    Public Shared Function SortListItem(ByVal List As Hashtable) As ListItem()
        Dim AL As ArrayList = New ArrayList
        For Each Key As String In List.Keys
            Dim Item As ListItem = New ListItem(List(Key), Key)
            AL.Add(Item)
        Next

        AL.Sort(New SortItemComparer)
        Return AL.ToArray(GetType(ListItem))
    End Function

    Public Shared Function GetTextFromSelectedItem(ByVal [Control] As ListControl) As String
        If [Control].SelectedIndex = -1 Then
            Return Nothing
        Else
            Dim Index As Integer = [Control].SelectedIndex
            Dim LI As ListItem = [Control].Items(Index)
            Return LI.Text
        End If
    End Function

    Public Shared Sub FillComboWithIncremental(ByVal [Control] As DropDownList, ByVal From As Integer, ByVal [To] As Integer)
        [Control].Items.Clear()

        For Index As Integer = From To [To]
            [Control].Items.Add(Index.ToString())
        Next
    End Sub

    Public Shared Function AtLeastOne([Control] As ListControl) As Boolean
        For Each Item As ListItem In [Control].Items
            If Item.Selected Then
                Return True
            End If
        Next
        Return False
    End Function

    Public Shared Function AllSelected([Control] As ListControl) As Boolean
        For Each Item As ListItem In [Control].Items
            If Not Item.Selected Then
                Return False
            End If
        Next
        Return True
    End Function

    Public Shared Function GetCheckedList([Control] As ListControl) As ArrayList
        Dim List As ArrayList = New ArrayList()
        For Each Item As ListItem In [Control].Items
            If Item.Selected Then
                List.Add(Item.Value)
            End If
        Next
        Return List
    End Function

    Public Shared Function GetUncheckedList([Control] As ListControl) As ArrayList
        Dim List As ArrayList = New ArrayList()
        For Each Item As ListItem In [Control].Items
            If Not Item.Selected Then
                List.Add(Item.Value)
            End If
        Next
        Return List
    End Function


    ' Check
    Public Shared Function CheckForEmptyValue(ByVal [Control] As Object, Optional ByVal Mark As Boolean = True) As Boolean
        Dim Empty As Boolean = True

        If Not IsNothing([Control]) AndAlso (TypeOf ([Control]) Is WebControl Or TypeOf ([Control]) Is HtmlControl) Then
            Select Case UCase([Control].GetType.Name)
                Case "TEXTBOX" : Empty = CType([Control], TextBox).Text.Trim = ""
                Case "DROPDOWNLIST" : Empty = CType([Control], DropDownList).SelectedValue.Trim = ""
                Case "HTMLTEXTAREA" : Empty = CType([Control], HtmlTextArea).InnerHtml = ""
            End Select

            If TypeOf ([Control]) Is WebControl Then
                If Empty And Mark Then : CType([Control], WebControl).BackColor = Color.Yellow
                Else : CType([Control], WebControl).BackColor = Color.Empty
                End If
            End If
            If TypeOf ([Control]) Is HtmlControl Then
                If Empty And Mark Then : CType([Control], HtmlControl).Style.Add("backgroundColor", ColorTranslator.ToHtml(Color.Yellow))
                Else : CType([Control], HtmlControl).Style.Remove("backgroundColor")
                End If
            End If
        End If

        Return Empty
    End Function

    Public Shared Function CheckForNumericValue(ByVal [Control] As Object, Optional ByVal MustBePositive As Boolean = False, _
                                                                           Optional ByVal Mark As Boolean = True) As Boolean
        Dim Check As Boolean = False

        If Not IsNothing([Control]) AndAlso TypeOf ([Control]) Is WebControl Then
            Select Case UCase([Control].GetType.Name)
                Case "TEXTBOX" : Check = Not IsNumeric(CType([Control], TextBox).Text)
                Case "DROPDOWNLIST" : Check = Not IsNumeric(CType([Control], DropDownList).SelectedValue)
            End Select

            If Not Check And MustBePositive Then
                Select Case UCase([Control].GetType.Name)
                    Case "TEXTBOX" : Check = CDbl(CType([Control], TextBox).Text) <= 0
                    Case "DROPDOWNLIST" : Check = CDbl(CType([Control], DropDownList).SelectedValue) <= 0
                End Select
            End If

            If Check And Mark Then
                CType([Control], WebControl).BackColor = Color.Yellow
            Else
                CType([Control], WebControl).BackColor = Color.Empty
            End If
        End If

        Return Check
    End Function

    Public Shared Function CheckForMultiMailValue(ByVal [Control] As Object, Optional ByVal Mark As Boolean = True) As Boolean
        Dim Value As String = Nothing

        If Not IsNothing([Control]) AndAlso (TypeOf ([Control]) Is WebControl) Then
            Select Case UCase([Control].GetType.Name)
                Case "TEXTBOX" : Value = CType([Control], TextBox).Text.Trim
                Case "DROPDOWNLIST" : Value = CType([Control], DropDownList).SelectedValue.Trim = ""
            End Select

            Dim Right As Boolean = IsValidMail(Value, True)

            If Not Right And Mark Then
                CType([Control], WebControl).BackColor = Color.Yellow
            Else
                CType([Control], WebControl).BackColor = Color.Empty
            End If
            Return Not Right
        End If

        Return False
    End Function

    Public Shared Function CheckForMailValue(ByVal [Control] As Object, Optional ByVal Mark As Boolean = True) As Boolean
        Dim Value As String = Nothing

        If Not IsNothing([Control]) AndAlso (TypeOf ([Control]) Is WebControl) Then
            Select Case UCase([Control].GetType.Name)
                Case "TEXTBOX" : Value = CType([Control], TextBox).Text.Trim
                Case "DROPDOWNLIST" : Value = CType([Control], DropDownList).SelectedValue.Trim = ""
            End Select

            Dim Right As Boolean = IsValidMail(Value)

            If Not Right And Mark Then
                CType([Control], WebControl).BackColor = Color.Yellow
            Else
                CType([Control], WebControl).BackColor = Color.Empty
            End If
            Return Not Right
        End If

        Return False
    End Function


    ' Time
    Public Shared Function DateToDays(ByVal [Date] As Date) As Integer
        Return TimeSpan.FromTicks([Date].Ticks).Days
    End Function

    Public Shared Function DateToDays(ByVal Ticks As Long) As Integer
        Return TimeSpan.FromTicks(Ticks).Days
    End Function

    Public Shared Function DateToHours(ByVal [Date] As Date) As Integer
        Return TimeSpan.FromTicks([Date].Ticks).Hours
    End Function

    Public Shared Function DateToHours(ByVal Ticks As Long) As Integer
        Return TimeSpan.FromTicks(Ticks).Hours
    End Function

    Public Shared Function DateToMinutes(ByVal [Date] As Date) As Integer
        Return TimeSpan.FromTicks([Date].Ticks).Minutes
    End Function

    Public Shared Function DateToMinutes(ByVal Ticks As Long) As Integer
        Return TimeSpan.FromTicks(Ticks).Minutes
    End Function

    Public Shared Function IsValidDate(ByVal Day As String, ByVal Month As String, ByVal Year As String) As Boolean
        Try
            Dim [Date] As Date = New Date(CInt(Year), CInt(Month), CInt(Day))
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function


    ' Mail
    Public Shared Function IsValidMail(ByVal Mail As String, ByVal IsMulti As Boolean) As Boolean
        If IsMulti And Trim("" & Mail) <> String.Empty Then
            Dim Tokens() As String = Mail.Split(New [Char]() {","c, " "c})
            Dim Right As Boolean = True

            For Each Token As String In Tokens
                If Token <> String.Empty Then
                    If Not IsValidMail(Token) Then
                        Return False
                    End If
                End If
            Next
            Return True
        Else
            Return IsValidMail(Mail)
        End If
    End Function

    Public Shared Function IsValidMail(ByVal Mail As String) As Boolean
        Dim RE As Regex = New Regex("[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?")
        Return RE.IsMatch(Mail)
    End Function


    ' Boolean
    Public Shared Function GetBoolean(ByVal Value As Object) As Boolean
        Try
            If IsNothing(Value) Or IsDBNull(Value) Then
                Return False
            Else
                Return CBool(Value)
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Shared Function TranslateBoolean(ByVal Value As Object, ByVal [True] As String, ByVal [False] As String) As String
        If GetBoolean(Value) Then
            Return [True]
        Else
            Return [False]
        End If
    End Function


    ' String
    Public Shared Function FixTwoCharacters(ByVal Value As Integer) As String
        If Value < 10 Then
            Return "0" & Value.ToString
        Else
            Return Value.ToString
        End If
    End Function

    Public Shared Function TrimAndClearCrLf(Value As String) As String
        Value = Trim(Value)
        Value = Value.Replace(vbCr, String.Empty)
        Value = Value.Replace(vbLf, String.Empty)
        Value = Value.Replace(vbCrLf, String.Empty)

        Return Value
    End Function

    Public Shared Function ConvertToCommaSeparatedString(List() As String, Separator As String) As String
        If List.Length > 0 Then
            Return String.Join(Separator, List)
        Else
            Return String.Empty
        End If
    End Function

    Public Shared Function ConvertToCommaSeparatedString(List As ArrayList, Separator As String) As String
        If List.Count > 0 Then
            Dim Temp() As String = Array.ConvertAll(List.ToArray(), Function(s) CStr(s))
            Return Utils.ConvertToCommaSeparatedString(Temp, Separator)
        Else
            Return String.Empty
        End If
    End Function

    Public Shared Function ConvertToArrayList(Value As String, Separator As String, [Type] As Type) As ArrayList
        If Not String.IsNullOrEmpty(Value) Then
            Dim Splitted() As String = Value.Split(Separator)

            If Splitted.Length > 0 Then
                Dim Converted() As Object = System.Array.ConvertAll(Splitted, Function(s) System.Convert.ChangeType(s, [Type]))
                Return New ArrayList(Converted)
            End If
        End If
        Return New ArrayList()
    End Function

    Public Shared Function IsEmpty(Value As String) As Boolean
        Return String.IsNullOrEmpty(Value) Or String.IsNullOrWhiteSpace(Value)
    End Function

    ' GUID
    Public Shared Function GuidToString(ByVal [Guid] As Guid) As String
        Return [Guid].ToString("N")
    End Function

    Public Shared Function GuidToString() As String
        Return GuidToString(Guid.NewGuid)
    End Function

    Public Shared Function IsGuid(Value As String) As Boolean
        Try
            Dim G As Guid = New Guid(Value)
            Return True

        Catch ex As Exception
            Return False
        End Try
    End Function


    ' Stream
    Public Shared Function GetStreamAsByteArray(ByVal Stream As System.IO.Stream) As Byte()
        Dim StreamLength As Integer = Convert.ToInt32(Stream.Length)
        Dim FileData As Byte() = New Byte(StreamLength) {}

        Stream.Read(FileData, 0, StreamLength)
        Stream.Close()

        Return FileData
    End Function


    ' DataView / DataTable
    Public Shared Function ToArrayList(ByVal Source As DataView, ByVal Value As String) As ArrayList
        Dim AL As ArrayList = New ArrayList
        For Each Row As DataRowView In Source
            Try
                AL.Add(Row(Value))
            Catch ex As Exception
            End Try
        Next
        Return AL
    End Function

    Public Shared Function ToArrayList(ByVal Source As DataTable, ByVal Value As String) As ArrayList
        Dim AL As ArrayList = New ArrayList
        For Each Row As DataRow In Source.Rows
            AL.Add(Row(Value))
        Next
        Return AL
    End Function

    Public Shared Function ToHashTable(ByVal Source As DataRow) As Hashtable
        Dim Table As DataTable = Source.Table
        Dim HT As Hashtable = New Hashtable

        For Each Column As DataColumn In Table.Columns
            HT.Add(Column.ColumnName, Source(Column.ColumnName))
        Next

        Return HT
    End Function

    Public Shared Function ToHashTable(ByVal Source As DataView, ByVal Key As String, ByVal Value As String) As Hashtable
        Dim HT As Hashtable = New Hashtable
        For Each Row As DataRowView In Source
            Try
                HT.Add(Row(Key), Row(Value))
            Catch ex As Exception
            End Try
        Next
        Return HT
    End Function

    Public Shared Function ToHashTable(ByVal Source As DataTable, ByVal Key As String, ByVal Value As String) As Hashtable
        Dim HT As Hashtable = New Hashtable
        For Each Row As DataRow In Source.Rows
            HT.Add(Row(Key), Row(Value))
        Next
        Return HT
    End Function

    Public Shared Function FindNextID(ByVal Source As DataView, ByVal ColumnName As String) As Integer
        Return Utils.FindNextID(Source.Table, ColumnName)
    End Function

    Public Shared Function FindDuplicatedValues(ByVal Source As DataTable, ByVal Excluded As DataRow, ByVal Values() As String) As Boolean
        Dim Filters As String = ""
        For Each Filter As String In Values
            If Filters <> "" Then Filters &= " AND "
            Filters &= Filter
        Next
        Filters = Filters.Replace("=N'", "='")

        Dim Rows() As DataRow = Source.Select(Filters)
        For Each Row As DataRow In Rows
            If Not Row Is Nothing Then
                If Not Row Is Excluded Then Return True
            Else
                Return True
            End If
        Next
        Return False
    End Function

    Public Shared Function FindDuplicatedValues(ByVal Source As DataTable, ByVal Excluded As DataRow, _
                                                ByVal ColumnName As String, ByVal Value As Object) As Boolean
        Dim Filter(0) As String
        Filter(0) = "[" & ColumnName & "]=" & SCFramework.DbSqlBuilder.Variant(Value)

        Return FindDuplicatedValues(Source, Excluded, Filter)
    End Function

    Public Shared Function FindDuplicatedValues(ByVal Source As DataTable, _
                                                ByVal ColumnName As String, ByVal Value As Object) As Boolean
        Dim Row As DataRow = Nothing
        Dim Filter(0) As String
        Filter(0) = "[" & ColumnName & "]=" & SCFramework.DbSqlBuilder.Variant(Value)

        Return FindDuplicatedValues(Source, Row, Filter)
    End Function

    Public Shared Function FindNextID(ByVal Source As DataTable, ByVal ColumnName As String) As Integer
        Dim Value As Object = Source.Compute("MAX(" & ColumnName & ")", "")
        If IsNumeric(Value) Then
            Return CInt(Value) + 1
        Else
            Return 1
        End If
    End Function

    Public Shared Function FindIndexByValue(Source As DataView, Field As String, Value As Object) As Integer
        For Index As Integer = 0 To Source.Count - 1
            Dim Row As DataRowView = Source(Index)
            Dim Confr As Object = Row(Field)

            If Object.Equals(Confr, Value) Then
                Return Index
            End If
        Next

        Return -1
    End Function

    Public Shared Sub SetAutoIncrementColumns(ByVal Source As DataTable, ByVal ParamArray FieldsName() As String)
        For Each Name As String In FieldsName
            Try
                Dim Column As DataColumn = Source.Columns(Name)
                Column.AutoIncrement = True
                Column.AutoIncrementStep = 1
                Column.AutoIncrementSeed = Utils.FindNextID(Source, Name)

            Catch ex As Exception
            End Try
        Next
    End Sub

    Public Shared Sub SetPrimaryKeyColumns(ByVal Source As DataTable, ByVal ParamArray Fields() As String)
        Dim Columns(Fields.Length - 1) As DataColumn
        For Index As Integer = 0 To Fields.Length - 1
            Dim Field As String = Fields(Index)
            Columns(Index) = Source.Columns(Field)
        Next

        Source.PrimaryKey = Columns
    End Sub

    Public Enum SwapRowDirection
        Up = 0
        Down = 1
    End Enum

    Public Shared Sub SwapDataSourceRowOrder(ByVal Source As DataView, ByVal Index As Integer, ByVal OrderField As String, _
                                             Optional ByVal Direction As SwapRowDirection = SwapRowDirection.Up)
        If (Direction = SwapRowDirection.Up And Index > 0) Or _
           (Direction = SwapRowDirection.Down And Index < Source.Count - 1) Then
            Dim Dest As Integer = IIf(Direction = SwapRowDirection.Up, Index - 1, Index + 1)
            Dim SourceRow As DataRow = Source(Index).Row
            Dim DestRow As DataRow = Source(Dest).Row

            If IsDBNull(SourceRow(OrderField)) Then SourceRow(OrderField) = -Source.Count
            If IsDBNull(DestRow(OrderField)) Then DestRow(OrderField) = -Source.Count

            Dim Temp As Integer = SourceRow(OrderField)
            SourceRow(OrderField) = DestRow(OrderField)
            DestRow(OrderField) = Temp
        End If
    End Sub

    Public Shared Sub SwapDataSourceRowOrder(ByVal Source As DataTable, ByVal Index As Integer, ByVal OrderField As String, _
                                             Optional ByVal Direction As SwapRowDirection = SwapRowDirection.Up)
        If (Direction = SwapRowDirection.Up And Index > 0) Or _
           (Direction = SwapRowDirection.Down And Index < Source.Rows.Count - 1) Then
            Dim Dest As Integer = IIf(Direction = SwapRowDirection.Up, Index - 1, Index + 1)
            Dim SourceRow As DataRow = Source.Rows(Index)
            Dim DestRow As DataRow = Source.Rows(Dest)

            If IsDBNull(SourceRow(OrderField)) Then SourceRow(OrderField) = -Source.Rows.Count
            If IsDBNull(DestRow(OrderField)) Then DestRow(OrderField) = -Source.Rows.Count

            Dim Temp As Integer = SourceRow(OrderField)
            SourceRow(OrderField) = DestRow(OrderField)
            DestRow(OrderField) = Temp
        End If
    End Sub

    Private Shared Function FindRowIndex(ByVal Source As DataTable, ByVal ToFind As DataRow) As Integer
        For Index As Integer = 0 To Source.Rows.Count - 1
            Dim Row As DataRow = Source.Rows(Index)
            If Row Is ToFind Then
                Return Index
            End If
        Next
        Return -1
    End Function

    Private Shared Function FindRowIndex(ByVal Source As DataView, ByVal ToFind As DataRowView) As Integer
        For Index As Integer = 0 To Source.Count - 1
            Dim Row As DataRowView = Source(Index)
            If Row Is ToFind Then
                Return Index
            End If
        Next
        Return -1
    End Function

    Public Shared Sub OrderDataTableField(ByVal Source As DataTable, ByVal FieldName As String)
        OrderDataTableField(Source, FieldName, FieldName)
    End Sub

    Public Shared Sub OrderDataTableField(ByVal Source As DataTable, ByVal FieldName As String, ByVal FieldToSaveOrder As String)
        Dim Order(Source.Rows.Count - 1) As Integer
        Dim DV As DataView = New DataView(Source)
        DV.Sort = FieldName

        For Index As Integer = 0 To DV.Count - 1
            Order(Index) = FindRowIndex(Source, DV(Index).Row)
        Next

        For Index As Integer = 0 To Order.Length - 1
            Dim Row As DataRow = Source.Rows(Order(Index))
            If Row.RowState <> DataRowState.Deleted Then
                Row(FieldToSaveOrder) = Index
            End If
        Next
    End Sub

    Public Shared Sub OrderDataTableField(ByVal Source As DataView, ByVal FieldName As String)
        OrderDataTableField(Source, FieldName, FieldName)
    End Sub

    Public Shared Sub OrderDataTableField(ByVal Source As DataView, ByVal FieldName As String, ByVal FieldToSaveOrder As String)
        Dim Order(Source.Count - 1) As Integer
        Dim OldSort As String = Source.Sort
        Source.Sort = FieldName

        For Index As Integer = 0 To Source.Count - 1
            Order(Index) = FindRowIndex(Source, Source(Index))
        Next

        For Index As Integer = 0 To Order.Length - 1
            Dim Row As DataRow = Source(Order(Index)).Row
            If Row.RowState <> DataRowState.Deleted Then
                Row(FieldToSaveOrder) = Index
            End If
        Next

        Source.Sort = OldSort
    End Sub

    Public Shared Sub ClearValueForIdentity(ByVal Source As DataTable, ByVal Identity As String)
        For Each Row As DataRow In Source.Rows
            Select Case Row.RowState
                Case DataRowState.Added
                    Row(Identity) = DBNull.Value
            End Select
        Next
    End Sub

    Public Shared Function SelectDistinct(ByVal Source As DataTable, ByVal ParamArray FieldNames() As String) As DataTable
        Dim lastValues() As Object
        Dim newTable As DataTable

        If FieldNames Is Nothing OrElse FieldNames.Length = 0 Then
            Throw New ArgumentNullException("FieldNames")
        End If

        If Source Is Nothing Then
            Return Nothing
        End If

        lastValues = New Object(FieldNames.Length - 1) {}
        newTable = New DataTable

        For Each field As String In FieldNames
            newTable.Columns.Add(field, Source.Columns(field).DataType)
        Next

        For Each Row As DataRow In Source.Select("", String.Join(", ", FieldNames))
            If Not FieldValuesAreEqual(lastValues, Row, FieldNames) Then
                newTable.Rows.Add(CreateRowClone(Row, newTable.NewRow(), FieldNames))

                SetLastValues(lastValues, Row, FieldNames)
            End If
        Next

        Return newTable
    End Function

    Public Shared Function SelectDistinct(ByVal Source As DataView, ByVal ParamArray FieldNames() As String) As DataView
        Dim Table As DataTable = SelectDistinct(Source.Table, FieldNames)
        Dim DV As DataView = Table.DefaultView
        DV.RowFilter = Source.RowFilter
        DV.Sort = Source.Sort

        Return DV
    End Function

    Private Shared Function FieldValuesAreEqual(ByVal LastValues() As Object, ByVal CurrentRow As DataRow, ByVal FieldNames() As String) As Boolean
        Dim areEqual As Boolean = True

        For i As Integer = 0 To FieldNames.Length - 1
            If LastValues(i) Is Nothing OrElse Not LastValues(i).Equals(CurrentRow(FieldNames(i))) Then
                areEqual = False
                Exit For
            End If
        Next

        Return areEqual
    End Function

    Private Shared Function CreateRowClone(ByVal SourceRow As DataRow, ByVal NewRow As DataRow, ByVal FieldNames() As String) As DataRow
        For Each field As String In FieldNames
            NewRow(field) = SourceRow(field)
        Next

        Return NewRow
    End Function

    Private Shared Sub SetLastValues(ByVal LastValues() As Object, ByVal SourceRow As DataRow, ByVal FieldNames() As String)
        For i As Integer = 0 To FieldNames.Length - 1
            LastValues(i) = SourceRow(FieldNames(i))
        Next
    End Sub

    Public Shared Function ExtractValues(Source As DataTable, Fields() As String) As ArrayList
        ' Holder
        Dim Values As ArrayList = New ArrayList()

        ' Cycle all rows
        For Each Row As DataRow In Source.Rows
            ' Cycle all fields
            For Each Field As String In Fields
                ' Check if field belong to table
                If Source.Columns.Contains(Field) Then
                    ' Add the value to the list
                    Values.Add(Row(Field))
                End If
            Next
        Next

        ' Return
        Return Values
    End Function

    Public Shared Function ExtractStringValues(Source As DataTable, Fields() As String) As String()
        Dim Values As ArrayList = Utils.ExtractValues(Source, Fields)
        Return Values.ToArray(GetType(System.String))
    End Function

    Public Shared Function ExtractIntValues(Source As DataTable, Fields() As String) As Long()
        Dim Values As ArrayList = Utils.ExtractValues(Source, Fields)
        Return Values.ToArray(GetType(System.Int32))
    End Function


    ' IP
    Public Shared Function GetLocalIP() As String
        Dim IPList As System.Net.IPHostEntry = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName)

        For Each Address As IPAddress In IPList.AddressList
            'Only return IPv4 routable IPs
            If (Address.AddressFamily = Sockets.AddressFamily.InterNetwork) AndAlso (Not IsPrivateIP(Address.ToString)) Then
                Return Address.ToString
            End If
        Next
        Return ""
    End Function

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


    ' Random
    Public Shared Sub ResetRandomSeed()
        Dim Key As String = "$SCFramework$RandomGenerator"
        If Bridge.Application IsNot Nothing Then
            Bridge.Application(Key) = Nothing
        End If
    End Sub

    Public Shared Function Random() As Random
        If Bridge.Application Is Nothing Then
            Return New Random(Now.Millisecond)
        Else
            Dim Key As String = "$SCFramework$RandomGenerator"
            If Bridge.Application(Key) Is Nothing Then
                Bridge.Application(Key) = New Random(Now.Millisecond)
            End If

            Return CType(Bridge.Application(Key), Random)
        End If
    End Function

    Public Shared Function ProbablyToExecute(Percentage As Integer) As Boolean
        If Percentage < 1 Then Return False
        If Percentage > 99 Then Return True

        Dim Value As Integer = Utils.Random.Next(1, 101)
        Return (Value <= Percentage)
    End Function

End Class
