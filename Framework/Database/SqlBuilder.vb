'*************************************************************************************************
' 
' [SCFramework]
' SqlBuilder  
' by Samuele Carassai
'
' Sql builder manager
'
' Version 5.0.0
' Updated 15/10/2016
'
' Support databases: OleDb, Sql
' TODO: 
' - Implement the join Function
' - Implement the group Function
' - Implement the having Function
' - Implement the union Function
'
'*************************************************************************************************


' Define the name space
Namespace DB

    ' Class definition
    Public Class SqlBuilder

        ' Constants
        Public Const QUOTE_PREFIX As String = "["
        Public Const QUOTE_SUFFIX As String = "]"

        ' Holder
        Private mProvider As String = "Undefined"

        Private mForceQuote As Boolean = True
        Private mStringEmptyIsNULL As Boolean = True
        Private mDateMinIsNULL As Boolean = True
        Private mDataBaseCulture As Globalization.CultureInfo = Nothing

        Private mTableName As String = Nothing
        Private mSelectFields As String = Nothing
        Private mInsertValues As String = Nothing
        Private mUpdateValues As String = Nothing
        Private mWhereClauses As String = Nothing
        Private mOrderFields As String = Nothing

        Private mDistinct As Boolean = False


#Region " CONSTRUCTOR "

        Sub New(Provider As String)
            Me.mProvider = Provider
            Me.mDataBaseCulture = CultureInfo.InvariantCulture
        End Sub

#End Region

#Region " PROPERTIES "

        ' Force to quote fields
        Public Property ForceQuote As Boolean
            Get
                Return Me.mForceQuote
            End Get
            Set(value As Boolean)
                Me.mForceQuote = value
            End Set
        End Property


        ' Consider the empty string as a NULL db value
        Public Property StringEmptyIsNULL As Boolean
            Get
                Return Me.mStringEmptyIsNULL
            End Get
            Set(value As Boolean)
                Me.mStringEmptyIsNULL = value
            End Set
        End Property


        ' Consider the date min value as a NULL db value
        Public Property DateMinIsNULL As Boolean
            Get
                Return Me.mDateMinIsNULL
            End Get
            Set(value As Boolean)
                Me.mDateMinIsNULL = value
            End Set
        End Property


        ' Set the database culture
        Public Property DataBaseCulture As Globalization.CultureInfo
            Get
                Return Me.mDataBaseCulture
            End Get
            Set(value As Globalization.CultureInfo)
                Me.mDataBaseCulture = value
            End Set
        End Property

#End Region

#Region " DATA TO STRING "

        ' Format a generic object
        Public Function [Variant](ByVal Value As Object) As String
            ' Check for null value
            If Value Is Nothing OrElse IsDBNull(Value) Then
                Return "NULL"
            End If

            ' Return the value by the case
            Select Case LCase(Value.GetType.Name)
                Case "date", "datetime" : Return [Date](Value, True)
                Case "boolean" : Return [Boolean](Value)
                Case "string" : Return [String](Value)
                Case "byte[]" : Return Binary(Value)
                Case Else : Return Numeric(Value)
            End Select
        End Function


        ' Format a string
        Public Function [String](ByVal Value As Object) As String
            ' Check for return NULL
            If Value Is Nothing OrElse IsDBNull(Value) OrElse
            (Me.mStringEmptyIsNULL And CStr(Value).Trim = String.Empty) Then
                Return "NULL"
            End If

            ' Fix the quote
            Value = CStr(Value).Replace("'", "''")

            ' Select the return value by provider type
            Select Case Me.mProvider
                Case "System.Data.OleDb" : Return "'" & Value & "'"
                Case "System.Data.SqlClient" : Return "N'" & Value & "'"
            End Select

            ' Else
            Return String.Empty
        End Function


        ' Format a number
        Public Function Numeric(ByVal Value As Object) As String
            Try
                ' Check for null value
                If Value Is Nothing Or IsDBNull(Value) Then Throw New Exception()

                ' Try to parse to number
                If TypeOf Value Is String Then
                    Value = Double.Parse(Value, Global.System.Threading.Thread.CurrentThread.CurrentUICulture)
                End If

                ' Check the object type and return its string rappresentation
                If ((TypeOf Value Is Long) Or (TypeOf Value Is ULong)) Or
               ((TypeOf Value Is Integer) Or (TypeOf Value Is UInteger)) Or
               ((TypeOf Value Is Short) Or (TypeOf Value Is UShort)) Then
                    Return CLng(Value).ToString
                Else
                    Return CDbl(Value).ToString(Me.mDataBaseCulture)
                End If

            Catch ex As Exception
                ' Else
                Return "NULL"
            End Try
        End Function


        ' Format a date
        Public Function [Date](ByVal Value As Object,
                           Optional ByVal Complete As Boolean = False) As String
            Try
                ' If the passed value is a string try to parse to a date using the current culture
                If TypeOf Value Is String Then
                    Value = Date.Parse(Value, Global.System.Threading.Thread.CurrentThread.CurrentUICulture)
                End If

                ' Check for null value
                If (IsDBNull(Value) OrElse Value Is Nothing) OrElse
               (IsDate(Value) AndAlso CDate(Value) = Date.MinValue) Then
                    Throw New Exception
                End If

                ' Select the returned value by the provider type
                Select Case Me.mProvider
                    Case "System.Data.OleDb"
                        If Complete Then
                            ' Long date format
                            Return "#" & CDate(Value).ToString(Me.mDataBaseCulture) & "#"
                        Else
                            ' Short date format
                            Return "#" & CDate(Value).ToString("d", Me.mDataBaseCulture) & "#"
                        End If

                    Case "System.Data.SqlClient"
                        If Complete Then
                            ' Long date format
                            Dim Temp As String = "CONVERT(DateTime, '" & CDate(Value).ToString("yyyyMMdd HH:mm:ss") & "')"
                            Return Temp.Replace(".", ":")
                        Else
                            ' Short date format
                            Return "CONVERT(DateTime, '" & CDate(Value).ToString("yyyyMMdd") & "')"
                        End If

                    Case Else
                        Throw New Exception

                End Select

            Catch ex As Exception
                ' Else
                Return "NULL"
            End Try
        End Function


        ' Format a boolean
        Public Function [Boolean](ByVal Value As Object) As String
            ' Check for null value
            If Value Is Nothing Or IsDBNull(Value) Then Return "NULL"

            ' Select the returned value by the provider type
            Select Case Me.mProvider
                Case "System.Data.OleDb" : Return IIf(CBool(Value), "TRUE", "FALSE")
                Case "System.Data.SqlClient" : Return IIf(CBool(Value), "1", "0")
            End Select

            ' Else
            Return "NULL"
        End Function


        ' Format a binary
        Public Function Binary(ByVal Buffer As Object) As String
            ' Check for null value
            If IsDBNull(Buffer) Or Buffer Is Nothing Then
                Return "NULL"

            ElseIf TypeOf Buffer Is Array Then
                Return Me.Binary(CType(Buffer, Byte()))

            ElseIf TypeOf Buffer Is String Then
                Return Me.Binary(CType(Buffer, String))

            Else
                Return "NULL"
            End If
        End Function

        Public Function Binary(ByVal Buffer() As Byte) As String
            ' Check for null value
            If Buffer Is Nothing OrElse Buffer.Length = 0 Then
                Return "NULL"
            End If

            ' Holder
            Dim Builder As String = "0x"

            ' Cycle all bytes in buffer
            For Each [Byte] As Byte In Buffer
                ' Convert to hexadecimal
                Dim HEX As String = Convert.ToString([Byte], 16)
                ' Add to builder
                Builder &= IIf(HEX.Length = 1, "0" & HEX, HEX)
            Next

            ' Return the value
            Return Builder
        End Function

        Public Function Binary(ByVal Value As String) As String
            ' Check for null value
            If String.IsNullOrEmpty(Value.Trim()) Then Return "NULL"
            ' Return the encoded string
            Return Me.Binary(Encoding.Default.GetBytes(Value))
        End Function

#End Region

#Region " UTILITIES "

        ' Return the string rappresentation of the system date/time
        Public Function GetSystemDate() As String
            ' Select by provider
            Select Case Me.mProvider
                Case "System.Data.OleDb" : Return "Now()"
                Case "System.Data.SqlClient" : Return "GETDATE()"
            End Select

            ' Else
            Return Nothing
        End Function


        ' Return a DB value rappresentation
        Public Shared Function GetDbValue(ByVal Value As Object) As Object
            ' Check for null values
            If Value Is Nothing Or IsDBNull(Value) Then
                Return DBNull.Value
            End If

            ' Select by object type
            Select Case LCase(Value.GetType.Name)
                Case "string" : If Trim(CStr(Value)) = "" Then Return DBNull.Value
                Case "date" : If CDate(Value) = Date.MinValue Then Return DBNull.Value
            End Select

            Return Value
        End Function


        ' Quote a field is necessary
        Public Shared Function Quote(Field As String) As String
            If Not Field.Contains(".") And Not Field.Contains("(") And Not Field.Contains("=") Then
                ' Check for ASC and DESC
                ' Check if start with quote
                If Not Field.StartsWith(SqlBuilder.QUOTE_PREFIX) Then
                    Field = SqlBuilder.QUOTE_PREFIX & Field
                End If

                ' Check if end with quote
                If Not Field.EndsWith(SqlBuilder.QUOTE_SUFFIX) Then
                    Field = Field & SqlBuilder.QUOTE_SUFFIX
                End If
            End If

            ' Return
            Return Field
        End Function

#End Region

#Region " PRIVATE "

        ' Quote a field
        Private Function InternalQuote(Field As String) As String
            ' Check for forced
            If Me.mForceQuote And Field IsNot Nothing Then
                ' Check for ASC and DESC
                If Field.EndsWith(" ASC", StringComparison.OrdinalIgnoreCase) Then
                    Return SqlBuilder.Quote(Field.Replace(" ASC", String.Empty)) & " ASC"

                ElseIf Field.EndsWith(" DESC", StringComparison.OrdinalIgnoreCase) Then
                    Return SqlBuilder.Quote(Field.Replace(" DESC", String.Empty)) & " DESC"

                Else
                    Return SqlBuilder.Quote(Field)
                End If
            End If

            ' Else
            Return Field
        End Function


        ' Create a join 
        Private Function ListJoiner(Fields() As String) As String
            Return String.Join(", ", (From Field As String In Fields Where Field.Trim <> String.Empty Select Me.InternalQuote(Field)).ToArray)
        End Function

#End Region

#Region " SETTER "

        ' Set the table name
        Public Function Table(Name As String) As SqlBuilder
            ' Hold and return
            Me.mTableName = Name
            Return Me
        End Function


        ' Set the distict
        Public Function Distinct(Value As Boolean) As SqlBuilder
            ' Hold and return
            Me.mDistinct = Value
            Return Me
        End Function

        Public Function Distinct() As SqlBuilder
            Return Me.Distinct(True)
        End Function


        ' Set the select fields
        Public Function [Select](Sql As String) As SqlBuilder
            ' Hold and return
            Me.mSelectFields = Sql
            Return Me
        End Function

        Public Function [Select](ParamArray Fields() As String) As SqlBuilder
            ' Check for empty value
            If Fields IsNot Nothing AndAlso Fields.Count > 0 Then
                Me.mSelectFields = Me.ListJoiner(Fields)
            End If

            ' Return
            Return Me
        End Function

        Public Function [Select](Fields As List(Of String)) As SqlBuilder
            ' Check and call the overload
            If Fields IsNot Nothing Then
                Return Me.Select(Fields.ToArray)
            End If

            ' Return
            Return Me
        End Function


        ' Set the insert clauses
        Public Function Insert(Sql As String) As SqlBuilder
            ' Hold and return
            Me.mInsertValues = Sql
            Return Me
        End Function

        Public Function Insert(Values As Dictionary(Of String, Object)) As SqlBuilder
            ' Check for empty values
            If Values Is Nothing Then
                Return Me.Insert(String.Empty)
            End If

            ' Create the list
            Dim StrList As String = String _
            .Join(", ", (From Value In Values Where Value.Key.Trim <> String.Empty Select Me.InternalQuote(Value.Key)).ToArray)
            Dim StrValues As String = String _
            .Join(", ", (From Value In Values Where Value.Key.Trim <> String.Empty Select Me.Variant(Value.Value)).ToArray)
            ' Call the overload
            Return Me.Insert(String.Format("({0}) VALUES ({1})", StrList, StrValues))
        End Function


        ' Set the update clauses
        Public Function Update(Sql As String) As SqlBuilder
            ' Hold and return
            Me.mUpdateValues = Sql
            Return Me
        End Function

        Public Function Update(Values As Dictionary(Of String, Object)) As SqlBuilder
            ' Check for empty values
            If Values Is Nothing Then
                Return Me.Update(String.Empty)
            End If

            ' To Array
            Dim Filtered() As String = (From Value In Values
                                        Where Value.Key.Trim <> String.Empty
                                        Select Me.InternalQuote(Value.Key) & " = " & Me.Variant(Value.Value)).ToArray
            ' Build the string
            Return Me.Update(String.Join(", ", Filtered))
        End Function


        ' Set the where clauses
        Public Function Where(Sql As String) As SqlBuilder
            ' Hold and return
            Me.mWhereClauses = Sql
            Return Me
        End Function

        Public Function Where(Clauses As Clauses) As SqlBuilder
            ' Check for empty values
            If Clauses Is Nothing Then
                Return Me.Where(String.Empty)
            End If

            ' Call the overload
            Return Me.Where(Clauses.ForSql)
        End Function


        ' Set the order fields
        Public Function Order(Sql As String) As SqlBuilder
            ' Hold and return
            Me.mOrderFields = Sql
            Return Me
        End Function

        Public Function Order(ParamArray Fields() As String) As SqlBuilder
            ' Check for empty value
            If Fields IsNot Nothing AndAlso Fields.Count > 0 Then
                Me.mOrderFields = Me.ListJoiner(Fields)
            End If

            ' Return
            Return Me
        End Function

        Public Function Order(Fields As List(Of String)) As SqlBuilder
            ' Check for empty values
            If Fields Is Nothing Then
                Return Me.Order(String.Empty)
            End If

            ' Call the overload
            Return Me.Order(Fields.ToArray)
        End Function

#End Region

#Region " PUBLIC "

        ' Create the select command
        Public ReadOnly Property SelectCommand() As String
            Get
                ' Check for table name
                If Me.mTableName Is Nothing Then
                    Throw New Exception("The table name cannot be empty!")
                End If

                ' Create
                Dim Sql As String = String.Format("SELECT {0}{1} FROM {2}",
                                          IIf(Me.mDistinct, "DISTICT ", String.Empty),
                                          IIf(String.IsNullOrEmpty(Me.mSelectFields), "*", Me.mSelectFields),
                                          Me.InternalQuote(Me.mTableName))

                ' Add the clauses
                If Not Utils.String.IsEmptyOrWhite(Me.mWhereClauses) Then Sql &= String.Format(" WHERE {0}", Me.mWhereClauses)
                If Not Utils.String.IsEmptyOrWhite(Me.mOrderFields) Then Sql &= String.Format(" ORDER BY {0}", Me.mOrderFields)

                ' Return the sql command
                Return Sql
            End Get
        End Property


        ' Create the insert command
        Public ReadOnly Property InsertCommand() As String
            Get
                ' Check for table name
                If Me.mTableName Is Nothing Then
                    Throw New Exception("The table name cannot be empty!")
                End If

                ' Return the sql command
                Return String.Format("INSERT INTO {0} {1}", Me.InternalQuote(Me.mTableName), Me.mInsertValues)
            End Get
        End Property


        ' Create the update command
        Public ReadOnly Property UpdateCommand() As String
            Get
                ' Check for table name
                If Me.mTableName Is Nothing Then
                    Throw New Exception("The table name cannot be empty!")
                End If

                ' Create the sql
                Dim Sql As String = String.Format("UPDATE {0}", SqlBuilder.Quote(Me.mTableName))

                ' Add the clauses
                If Not Utils.String.IsEmptyOrWhite(Me.mUpdateValues) Then Sql &= String.Format(" SET {0}", Me.mUpdateValues)
                If Not Utils.String.IsEmptyOrWhite(Me.mWhereClauses) Then Sql &= String.Format(" WHERE {0}", Me.mWhereClauses)

                ' Return the sql command
                Return Sql
            End Get
        End Property


        ' Create the delete command
        Public ReadOnly Property DeleteCommand() As String
            Get
                ' Check for table name
                If Me.mTableName Is Nothing Then
                    Throw New Exception("The table name cannot be empty!")
                End If

                ' Create
                Dim Sql As String = String.Format("DELETE FROM {1}", Me.InternalQuote(Me.mTableName))

                ' Add the clauses
                If Not Utils.String.IsEmptyOrWhite(Me.mWhereClauses) Then Sql &= String.Format(" WHERE {0}", Me.mWhereClauses)

                ' Return the sql command
                Return Sql
            End Get
        End Property

#End Region

    End Class

End Namespace