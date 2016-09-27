﻿'*************************************************************************************************
' 
' [SCFramework]
' DbSqlBuilder  
' by Samuele Carassai
'
' Sql builder manager
' Version 5.0.0
' Created 17/09/2015
' Updated 02/11/2015
'
' Integrazione: OleDb, Sql
'
'*************************************************************************************************


' Classe Adattatore
Public Class DbSqlBuilder

    ' Constants
    Public Const QuotePrefix As String = "["
    Public Const QuoteSuffix As String = "]"

#Region " CLAUSES "

    ' Clause structure
    Public Class Clauses

        ' Enums
        Public Enum ComparerType As Integer
            Equal
            Minor
            MinorAndEqual
            Major
            MajorAndEqual
            Different
            [Like]
            LikeStart
            LikeEnd
        End Enum

        ' Holder
        Private mSql As String = String.Empty

        ' Contructor
        Public Sub New()
        End Sub

        Public Sub New(Column As String, Value As Object)
            Me.Add(Column, Value)
        End Sub

        Public Sub New(Clauses As IDictionary(Of String, Object))
            Me.Add(Clauses)
        End Sub

        ' Add a clause
        Public Sub Add(Column As String, Comparer As ComparerType, Value As Object, GroupAsAnd As Boolean)
            ' Fix the particular case
            If Comparer = ComparerType.Like Then
                Value = String.Format("%{0}%", Value.ToString())
            ElseIf Comparer = ComparerType.LikeStart Then
                Value = String.Format("%{0}", Value.ToString())
            ElseIf Comparer = ComparerType.LikeEnd Then
                Value = String.Format("{0}%", Value.ToString())
            End If

            ' Convert the comparer to its string rappresentation
            Dim ComparerToString As String = "="
            Select Case Comparer
                Case ComparerType.Different : ComparerToString = "<>"
                Case ComparerType.Major : ComparerToString = ">"
                Case ComparerType.MajorAndEqual : ComparerToString = ">="
                Case ComparerType.Minor : ComparerToString = "<"
                Case ComparerType.MinorAndEqual : ComparerToString = "<="
            End Select

            ' Fix the comparer in the case of null values
            If Value Is Nothing OrElse IsDBNull(Value) Then
                If Comparer = ComparerType.Equal Then ComparerToString = "IS"
                If Comparer = ComparerType.Different Then ComparerToString = "IS NOT"
            End If

            ' Build the group clausole
            If Not Utils.IsEmpty(Me.mSql) Then
                ' Check for AND or OR
                Me.mSql &= IIf(GroupAsAnd, " AND ", " OR ")
            End If

            ' Append the new clause
            Me.mSql &= String.Format("{0} {1} {2}", DbSqlBuilder.Quote(Column), ComparerToString, DbSqlBuilder.Variant(Value))
        End Sub

        ' Add one clause using the default parameters
        Public Sub Add(Column As String, Value As Object)
            Me.Add(Column, ComparerType.Equal, Value, True)
        End Sub

        ' Add a list of clauses
        Public Sub Add(Clauses As IDictionary(Of String, Object))
            ' Cycle all clause of list
            For Each Column As String In Clauses.Keys
                ' Add
                Me.Add(Column, ComparerType.Equal, Clauses(Column), True)
            Next
        End Sub

        ' Get the builded sql
        Public ReadOnly Property Sql As String
            Get
                Return Me.mSql
            End Get
        End Property

        ' True if is empty
        Public ReadOnly Property IsEmpty As Boolean
            Get
                Return Utils.IsEmpty(Me.mSql)
            End Get
        End Property

    End Class

#End Region

#Region " PRIVATE "

    ' Get the value pass by generic web control
    Private Shared Function GetValue(ByVal [Control] As WebControl) As String
        If Not ([Control] Is Nothing) Then
            If TypeOf [Control] Is ListControl Then
                Return CType([Control], ListControl).SelectedValue
            Else
                Select Case [Control].GetType.Name
                    Case "TextBox" : Return CType([Control], TextBox).Text
                    Case "Label" : Return CType([Control], Label).Text
                    Case "CheckBox" : Return CType([Control], CheckBox).Checked.ToString()
                    Case "RadioButton" : Return CType([Control], RadioButton).Checked.ToString()
                    Case "RadioButtonList" : Return CType([Control], RadioButtonList).SelectedValue
                End Select
            End If
        End If
        Return Nothing
    End Function

#End Region

#Region " DATA TO STRING "

    ' Format a generic object
    Public Shared Function [Variant](ByVal Value As Object, Optional ByVal Provider As DbQuery.ProvidersList = DbQuery.ProvidersList.Undefined) As String
        ' Check for null value
        If Value Is Nothing OrElse IsDBNull(Value) Then
            Return "NULL"
        End If

        ' Return the value by the case
        Select Case LCase(Value.GetType.Name)
            Case "date", "datetime" : Return [Date](Value, , Provider)
            Case "boolean" : Return [Boolean](Value, Provider)
            Case "string" : Return [String](Value, , Provider)
            Case "byte[]" : Return Binary(Value)
            Case Else : Return Numeric(Value)
        End Select
    End Function


    ' Format a string
    Public Shared Function [String](ByVal Value As Object, _
                                    Optional ByVal EmptyIsNULL As Boolean = True, _
                                    Optional ByVal Provider As DbQuery.ProvidersList = DbQuery.ProvidersList.Undefined) As String
        ' Check for return NULL
        If Value Is Nothing OrElse IsDBNull(Value) Then Return "NULL"
        If EmptyIsNULL AndAlso ("" & Value).Trim() = String.Empty Then Return "NULL"

        ' Fix the provider
        If Provider = DbQuery.ProvidersList.Undefined Then
            Provider = Bridge.Query.GetProvider()
        End If

        ' Fix the quote
        Value = CStr(Value).Replace("'", "''")

        ' Select the return value by provider type
        Select Case Provider
            Case DbQuery.ProvidersList.OleDb : Return "'" & Value & "'"
            Case DbQuery.ProvidersList.SqlClient : Return "N'" & Value & "'"
        End Select

        ' Else
        Return String.Empty
    End Function

    Public Shared Function [String](ByVal [Control] As WebControl, _
                                    Optional ByVal EmptyIsNULL As Boolean = True, _
                                    Optional ByVal Provider As DbQuery.ProvidersList = DbQuery.ProvidersList.Undefined) As String
        Return [String](GetValue([Control]), EmptyIsNULL, Provider)
    End Function


    ' Format a number
    Public Shared Function Numeric(ByVal Value As Object) As String
        Try
            ' Check for null value
            If Value Is Nothing Or IsDBNull(Value) Then Throw New Exception()

            ' Try to parse to number
            If TypeOf Value Is String Then
                Value = Double.Parse(Value, Global.System.Threading.Thread.CurrentThread.CurrentUICulture)
            End If

            ' Check the object type and return its string rappresentation
            If ((TypeOf Value Is Long) Or (TypeOf Value Is ULong)) Or _
               ((TypeOf Value Is Integer) Or (TypeOf Value Is UInteger)) Or _
               ((TypeOf Value Is Short) Or (TypeOf Value Is UShort)) Then
                Return CLng(Value).ToString
            Else
                Return CDbl(Value).ToString(CultureInfo.InvariantCulture)
            End If

        Catch ex As Exception
            ' Else
            Return "NULL"
        End Try
    End Function

    Public Shared Function Numeric(ByVal [Control] As WebControl) As String
        Return Numeric(GetValue([Control]))
    End Function


    ' Format a date
    Public Shared Function [Date](ByVal Value As Object, _
                                  Optional ByVal Complete As Boolean = False, _
                                  Optional ByVal Provider As DbQuery.ProvidersList = DbQuery.ProvidersList.Undefined) As String
        Try
            ' Get the current culture code
            Dim Culture As CultureInfo = Global.System.Threading.Thread.CurrentThread.CurrentUICulture

            ' If the passed value is a string try to parse to a date using the current culture
            If TypeOf Value Is String Then
                Value = Date.Parse(Value, Culture)
            End If

            ' Check for null value
            If (IsDBNull(Value) OrElse Value Is Nothing) OrElse _
               (IsDate(Value) AndAlso CDate(Value) = Date.MinValue) Then
                Throw New Exception
            End If

            ' Fix the provider
            If Provider = DbQuery.ProvidersList.Undefined Then
                Provider = Bridge.Query.GetProvider()
            End If

            ' Select the returned value by the provider type
            Select Case Provider
                Case DbQuery.ProvidersList.OleDb
                    If Complete Then
                        ' Long date format
                        Return "#" & CDate(Value).ToString(CultureInfo.InvariantCulture) & "#"
                    Else
                        ' Short date format
                        Return "#" & CDate(Value).ToString("d", CultureInfo.InvariantCulture) & "#"
                    End If

                Case DbQuery.ProvidersList.SqlClient
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

    Public Shared Function [Date](ByVal [Control] As WebControl, _
                                  Optional ByVal Complete As Boolean = False, _
                                  Optional ByVal Provider As DbQuery.ProvidersList = DbQuery.ProvidersList.Undefined) As String
        Return [Date](GetValue([Control]), Complete, Provider)
    End Function


    ' Format a boolean
    Public Shared Function [Boolean](ByVal Value As Object, _
                                     Optional ByVal Provider As DbQuery.ProvidersList = DbQuery.ProvidersList.Undefined) As String
        ' Check for null value
        If Value Is Nothing Or IsDBNull(Value) Then Return "NULL"

        ' Fix the provider
        If Provider = DbQuery.ProvidersList.Undefined Then
            Provider = Bridge.Query.GetProvider()
        End If

        ' Select the returned value by the provider type
        Select Case Provider
            Case DbQuery.ProvidersList.OleDb : Return IIf(CBool(Value), "TRUE", "FALSE")
            Case DbQuery.ProvidersList.SqlClient : Return IIf(CBool(Value), "1", "0")
            Case Else : Return "NULL"
        End Select
    End Function

    Public Shared Function [Boolean](ByVal [Control] As WebControl, _
                                     Optional ByVal Provider As DbQuery.ProvidersList = DbQuery.ProvidersList.Undefined) As String
        Return [Boolean](GetValue([Control]), Provider)
    End Function


    ' Format a binary
    Public Shared Function Binary(ByVal Buffer As Object) As String
        ' Check for null value
        If IsDBNull(Buffer) Or Buffer Is Nothing Then
            Return "NULL"

        ElseIf TypeOf Buffer Is Array Then
            Return Binary(CType(Buffer, Byte()))

        ElseIf TypeOf Buffer Is String Then
            Return Binary(CType(Buffer, String))

        Else
            Return "NULL"
        End If
    End Function

    Public Shared Function Binary(ByVal Buffer() As Byte) As String
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

    Public Shared Function Binary(ByVal Value As String) As String
        ' Check for null value
        If String.IsNullOrEmpty(Value.Trim()) Then
            Return "NULL"
        End If

        ' Return the encoded string
        Return Binary(Encoding.Default.GetBytes(Value))
    End Function

#End Region

#Region " UTILITIES "

    ' Return the string rappresentation of the system date/time
    Public Shared Function GetSystemDate(Optional ByVal Provider As DbQuery.ProvidersList = DbQuery.ProvidersList.Undefined) As String
        ' Fix the provider
        If Provider = DbQuery.ProvidersList.Undefined Then
            Provider = Bridge.Query.GetProvider()
        End If

        ' Select by provider
        Select Case Provider
            Case DbQuery.ProvidersList.OleDb : Return "Now()"
            Case DbQuery.ProvidersList.SqlClient : Return "GETDATE()"
            Case Else : Return String.Empty
        End Select
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
        If Not Field.Contains(".") And Not Field.Contains("(") Then
            ' Check if start with quote
            If Not Field.StartsWith(DbSqlBuilder.QuotePrefix) Then
                Field = DbSqlBuilder.QuotePrefix & Field
            End If

            ' Check if end with quote
            If Not Field.EndsWith(DbSqlBuilder.QuoteSuffix) Then
                Field = Field & DbSqlBuilder.QuoteSuffix
            End If
        End If

        ' Return
        Return Field
    End Function

#End Region

#Region " BUILDER "

    ' Build a generic select sql command
    Public Shared Function BuildSelectCommand(TableName As String, Fields As IList(Of String), Clause As Clauses) As String
        ' Build the value list
        Dim FieldList As String = String.Empty
        For Each Field As String In Fields
            ' Build the field/value list
            If Not String.IsNullOrEmpty(FieldList) Then FieldList &= ", "
            FieldList &= DbSqlBuilder.Quote(Field)
        Next

        ' Build the sql command
        Dim Sql As String = String.Format("SELECT {0} FROM {1} ", _
                                          IIf(String.IsNullOrEmpty(FieldList), "*", FieldList), _
                                          DbSqlBuilder.Quote(TableName))

        ' Add the where clausole only if have
        If Not Clause.IsEmpty Then
            Sql &= String.Format("WHERE {0} ", Clause.Sql)
        End If

        ' Return the sql command
        Return Sql
    End Function

    ' Build a generic insert sql command
    Public Shared Function BuildInsertCommand(TableName As String, Values As IDictionary(Of String, Object)) As String
        ' Strings builder
        Dim FieldList As String = String.Empty
        Dim ValueList As String = String.Empty

        For Each Key As String In Values.Keys
            ' Build the field list
            If Not String.IsNullOrEmpty(FieldList) Then FieldList &= ", "
            FieldList &= DbSqlBuilder.Quote(Key)

            ' Build thew value list
            If Not String.IsNullOrEmpty(ValueList) Then ValueList &= ", "
            ValueList &= DbSqlBuilder.Variant(Values(Key))
        Next

        ' Define the query
        Return String.Format("INSERT INTO {0} ({1}) VALUES ({2})", _
                             DbSqlBuilder.Quote(TableName), _
                             FieldList, _
                             ValueList)
    End Function

    ' Build a generic update sql command
    Public Shared Function BuildUpdateCommand(TableName As String, Values As IDictionary(Of String, Object), Clause As Clauses) As String
        ' Build the value list
        Dim ValuesList As String = String.Empty
        For Each Key As String In Values.Keys
            ' Build the field/value list
            If Not String.IsNullOrEmpty(ValuesList) Then ValuesList &= ", "
            ValuesList &= String.Format("{0} = {1}", _
                                        DbSqlBuilder.Quote(Key), _
                                        DbSqlBuilder.Variant(Values(Key)))
        Next

        ' Build the sql command
        Dim Sql As String = String.Format("UPDATE {0} ", DbSqlBuilder.Quote(TableName))

        If Not String.IsNullOrEmpty(ValuesList) Then
            Sql &= String.Format("SET {0} ", ValuesList)
        End If

        ' Add the where clausole only if have
        If Not Clause.IsEmpty Then
            Sql &= String.Format("WHERE {0} ", Clause.Sql)
        End If

        ' Return the sql command
        Return Sql
    End Function

    ' Generic delete command
    Public Shared Function BuildDeleteCommand(TableName As String, Clause As Clauses) As String
        ' Build the sql command
        Dim Sql As String = String.Format("DELETE FROM {0} ", DbSqlBuilder.Quote(TableName))

        ' Add the where clausole only if have
        If Not Clause.IsEmpty Then
            Sql &= String.Format("WHERE {0} ", Clause.Sql)
        End If

        ' Return the sql command
        Return Sql
    End Function

#End Region

End Class

