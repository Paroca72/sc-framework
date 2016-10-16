'*************************************************************************************************
' 
' [SCFramework]
' DbHelper  
' by Samuele Carassai
'
' Helper class to link to database (new from version 5.x)
' Version 5.0.0
' Created 10/08/2015
' Updated 16/10/2016
'
'*************************************************************************************************

Imports System.Data.Common
Imports System.Data.SqlClient


Public MustInherit Class DbHelper

    ' Define the holders
    Private mPrimaryKeysColumns As List(Of String) = Nothing
    Private mAutoNumberColumns As List(Of String) = Nothing
    Private mWritableColumns As List(Of String) = Nothing

    Private mQuery As SCFramework.DbQuery = Nothing
    Private mSubordinates As List(Of DbHelper) = Nothing
    Private mSafety As Boolean = True


#Region " CONSTRUCTOR "

    Public Sub New()
        ' Analize
        Me.OnAnalizeTable()
    End Sub

#End Region

#Region " PRIVATES "

    ' OleDb analisys 
    Private Sub OleDbAnalisys(Connection As DbConnection)
        ' Connection
        Dim CustomConnection As OleDb.OleDbConnection = CType(Connection, OleDb.OleDbConnection)

        ' Primary keys
        Dim Table As DataTable = CustomConnection.GetOleDbSchemaTable(OleDb.OleDbSchemaGuid.Primary_Keys,
                                                                      New Object() {Nothing, Nothing, Me.GetTableName()})
        For Each Row As DataRow In Table.Rows
            ' TODO: understand if automunber
            mPrimaryKeysColumns.Add(Row!COLUMN_NAME)
        Next

        ' Autonumber and Writable
        Table = CustomConnection.GetOleDbSchemaTable(OleDb.OleDbSchemaGuid.Columns,
                                                     New Object() {Nothing, Nothing, Me.GetTableName(), Nothing})
        For Each Row As DataRow In Table.Rows
            ' Auto Number
            If Row!DATA_TYPE = OleDb.OleDbType.Integer AndAlso
               Row!COLUMN_FLAGS = 90 Then
                Me.mAutoNumberColumns.Add(Row!COLUMN_NAME)
            End If

            ' Writable
            Me.mWritableColumns.Add(Row!COLUMN_NAME)
        Next
    End Sub

    ' Sql analisys
    Private Sub SqlAnalisys(Connection As DbConnection)
        ' Define the request for a specific table
        Dim Sql As String = DbSqlBuilder.BuildSelectCommand(Me.GetTableName(), Nothing, Nothing)

        ' Find the reader
        Dim Command As SqlCommand = New SqlCommand(Sql, Connection)
        Dim Reader As SqlDataReader = Command.ExecuteReader(CommandBehavior.KeyInfo)

        ' Find the infos table
        Dim Table As DataTable = Reader.GetSchemaTable()
        For Each Row As DataRow In Table.Rows
            ' Primary key
            If CBool(Row!IsKey) And CBool(Row!IsUnique) Then
                Me.mPrimaryKeysColumns.Add(Row!ColumnName)
            End If

            ' Autoincrement
            If CBool(Row!IsIdentity) And CBool(Row!IsAutoIncrement) Then
                Me.mAutoNumberColumns.Add(Row!ColumnName)
            End If

            ' Writable
            Me.mWritableColumns.Add(Row!ColumnName)
        Next
    End Sub

    ' Extract only the writable columns
    Private Function FilterForWritableColumns(Source As Hashtable) As Hashtable
        ' Holder
        Dim HT As Hashtable = New Hashtable()

        ' Cycle all keys
        For Each Key As String In Source.Keys
            ' Check if writable
            If Me.mWritableColumns.Contains(Key) Then
                HT.Add(Key, Source(Key))
            End If
        Next

        ' Return
        Return HT
    End Function

#End Region

#Region " PROTECTED "

    ' Convert a single value in a pair value using the primary key as pair key
    Protected Function ToClauses(Value As Long) As SCFramework.DbClauses
        ' Define the where filter
        Dim Clauses As SCFramework.DbClauses = New SCFramework.DbClauses()

        ' Check if have at least one primary key
        If Me.mPrimaryKeysColumns.Count > 0 Then
            Clauses.Add(Me.mPrimaryKeysColumns(0), Value)
        End If

        ' Return
        Return Clauses
    End Function

#End Region

#Region " PROPERTIES "

    ' Get the query to use
    Public Property Query As SCFramework.DbQuery
        Set(value As SCFramework.DbQuery)
            Me.mQuery = value
        End Set
        Get
            If Me.mQuery Is Nothing Then
                ' Return the global one if the base is not empty
                Return Me.mQuery

            Else
                ' Else create a new one
                Return SCFramework.Bridge.Query
            End If
        End Get
    End Property

    ' The primary key columns list
    Public Property PrimaryKeys As List(Of String)
        Get
            Return Me.mPrimaryKeysColumns
        End Get
        Set(value As List(Of String))
            Me.mPrimaryKeysColumns = value
        End Set
    End Property

    ' The autonumber columns list
    Public Property AutoNumbers As List(Of String)
        Get
            Return Me.mAutoNumberColumns
        End Get
        Set(value As List(Of String))
            Me.mAutoNumberColumns = value
        End Set
    End Property

    ' The writable column list
    Public ReadOnly Property WritableColumns As List(Of String)
        Get
            Return Me.mWritableColumns
        End Get
    End Property

    ' Set the dafety checker
    Public Property Safety As Boolean
        Set(Value As Boolean)
            Me.mSafety = Value
        End Set
        Get
            Return Me.mSafety
        End Get
    End Property

#End Region

#Region " PUBLIC "

    ' Get the linked database table name
    Public MustOverride Function GetTableName() As String

    ' Analize the table and store all usefull data
    Public Overridable Sub OnAnalizeTable()
        ' Private query holder
        Dim Query As SCFramework.DbQuery = Me.Query

        ' Holder
        Dim Connection As DbConnection = Query.GetConnection()
        Dim Provider As DbQuery.ProvidersList = Query.GetProvider()

        ' Open
        Dim MustBeOpen As Boolean = (Query.GetConnection().State = ConnectionState.Closed)
        If MustBeOpen Then
            Connection.Open()
        End If

        ' Define the holder
        Me.mPrimaryKeysColumns = New List(Of String)
        Me.mAutoNumberColumns = New List(Of String)
        Me.mWritableColumns = New List(Of String)

        ' Select the analisys by provider
        Select Case Provider
            Case DbQuery.ProvidersList.OleDb : Me.OleDbAnalisys(Connection)
            Case DbQuery.ProvidersList.SqlClient : Me.SqlAnalisys(Connection)
        End Select

        ' Close the connection to database
        If MustBeOpen Then
            Connection.Close()
        End If
    End Sub

    ' Delete command
    Public Overridable Function Delete(Clauses As SCFramework.DbClauses) As Long
        ' Check for safety
        If (Me.mSafety) And (Clauses Is Nothing OrElse Clauses.IsEmpty) Then
            Throw New Exception("This command will delete all row in the table!")
        End If

        ' Create the query object is needed and execute the delete command
        Return Me.Query.Delete(Me.GetTableName(), Clauses)
    End Function

    ' Insert command
    Public Overridable Function Insert(Values As IDictionary(Of String, Object)) As Long
        Return Me.Query.Insert(Me.GetTableName(), Me.FilterForWritableColumns(Values))
    End Function

    ' Update command
    Public Overridable Function Update(Values As IDictionary(Of String, Object), Clauses As SCFramework.DbClauses) As Long
        ' Check for safety
        If (Me.Safety) And (Clauses Is Nothing OrElse Clauses.IsEmpty) Then
            Throw New Exception("This command will update all row in the table!")
        End If

        ' Holders
        Dim UpdateFields As Dictionary(Of String, Object) = New Dictionary(Of String, Object)

        ' Create the sql update code
        For Each Field As String In Values.Keys
            ' Check if the column is to update
            If Me.WritableColumns.Contains(Field) And Not Me.mPrimaryKeysColumns.Contains(Field) Then
                ' Add to the set value list to update
                UpdateFields.Add(Field, Values(Field))
            End If
        Next

        ' Create the command and execute it
        Return Me.Query.Update(Me.GetTableName(), UpdateFields, Clauses)
    End Function

#End Region

End Class
