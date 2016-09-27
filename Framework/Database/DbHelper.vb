'*************************************************************************************************
' 
' [SCFramework]
' DatabaseBaseTableHelper  
' by Samuele Carassai
'
' Helper class to link to database (new from version 5.x)
' Version 5.0.0
' Created 10/08/2015
' Updated 29/10/2015
'
'*************************************************************************************************

Imports System.Data.Common
Imports System.Data.SqlClient


Public MustInherit Class DbHelper

    ' Define the holders
    Private mPrimaryKeysColumns As List(Of String) = Nothing
    Private mAutoNumberColumns As List(Of String) = Nothing
    Private mWritableColumns As List(Of String) = Nothing

    ' Subordinates
    Private mSubordinates As List(Of DbHelper) = Nothing


#Region " CONSTRUCTOR "

    Public Sub New()
        ' Check for database
        Me.CheckDataBase()
        ' Analize
        Me.OnAnalizeTable()
    End Sub

#End Region

#Region " PRIVATES "

    ' Check for table existance inside the database
    Private Sub CheckDataBase()
        ' Check if the table exists

    End Sub

    ' OleDb analisys 
    Private Sub OleDbAnalisys(Connection As DbConnection)
        ' Connection
        Dim CustomConnection As OleDb.OleDbConnection = CType(Connection, OleDb.OleDbConnection)

        ' Primary keys
        Dim Table As DataTable = CustomConnection.GetOleDbSchemaTable(OleDb.OleDbSchemaGuid.Primary_Keys, _
                                                                      New Object() {Nothing, Nothing, Me.GetTableName()})
        For Each Row As DataRow In Table.Rows
            mPrimaryKeysColumns.Add(Row!COLUMN_NAME)
        Next

        ' Autonumber and Writable
        Table = CustomConnection.GetOleDbSchemaTable(OleDb.OleDbSchemaGuid.Columns, _
                                                     New Object() {Nothing, Nothing, Me.GetTableName(), Nothing})
        For Each Row As DataRow In Table.Rows
            ' Auto Number
            If Row!DATA_TYPE = OleDb.OleDbType.Integer AndAlso _
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

    ' Extract the key pairs from a datarow
    Private Function ExtractLocalKeysPairs(Row As DataRow) As Hashtable
        ' Holder
        Dim Values As Hashtable = New Hashtable()

        ' Cycle all primary keys list
        For Each Key As String In Me.mPrimaryKeysColumns
            ' If exists add the key and the value at the list
            If Row.Table.Columns.Contains(Key) Then
                Values.Add(Key, Row(Key))
            End If
        Next

        ' Return the list
        Return Values
    End Function

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
    Protected Function ToClauses(Value As Long) As DbSqlBuilder.Clauses
        ' Define the where filter
        Dim Clauses As DbSqlBuilder.Clauses = New DbSqlBuilder.Clauses()

        ' Check if have at least one primary key
        If Me.mPrimaryKeysColumns.Count > 0 Then
            Clauses.Add(Me.mPrimaryKeysColumns(0), Value)
        End If

        ' Return
        Return Clauses
    End Function

    ' Get the data table filtered by where clausole
    Protected Overridable Function GetSource(Clauses As DbSqlBuilder.Clauses) As DataTable
        ' Source
        Dim Source As DataTable = Bridge.Query.Table(Me.GetTableName(), Nothing, Clauses)

        ' Primary key
        If mPrimaryKeysColumns.Count > 0 Then
            SCFramework.Utils.SetPrimaryKeyColumns(Source, Me.mPrimaryKeysColumns.ToArray)
        End If

        ' Auto number
        If mAutoNumberColumns.Count > 0 Then
            SCFramework.Utils.SetAutoIncrementColumns(Source, Me.mAutoNumberColumns.ToArray)
        End If

        Return Source
    End Function

    ' Delete command
    Protected Overridable Function Delete(Clauses As DbSqlBuilder.Clauses, Optional Safety As Boolean = True) As Long
        ' Check for safety
        If (Safety) And (Clauses Is Nothing OrElse Clauses.IsEmpty) Then
            Throw New Exception("This command will delete all row in the table!")
        End If

        ' Extract all the rows to delete
        Dim RowsToDelete As DataTable = Me.GetSource(Clauses)

        ' Cycle subortdinates
        For Each Subordinate As DbHelper In Me.mSubordinates
            ' Cycle rows and for each row to delete extract the pairs key
            For Each Row As DataRow In RowsToDelete.Rows
                ' Exctract the current primary keys
                Dim LocalKeys As Hashtable = Me.ExtractLocalKeysPairs(Row)
                ' Apply delete with the new clauses
                Subordinate.Delete(New DbSqlBuilder.Clauses(LocalKeys))
            Next
        Next

        ' Execute the command to delete
        Return SCFramework.Bridge.Query.Delete(Me.GetTableName(), Clauses)
    End Function

    ' Insert command
    Protected Overridable Function Insert(Values As IDictionary(Of String, Object)) As Long
        Return Bridge.Query.Insert(Me.GetTableName(), Me.FilterForWritableColumns(Values))
    End Function

    ' Update command
    Protected Overridable Function Update(Values As IDictionary(Of String, Object), _
                                          Clauses As DbSqlBuilder.Clauses, _
                                          Optional Safety As Boolean = True) As Long
        ' Holders
        Dim UpdateFields As Hashtable = New Hashtable()

        ' Create the sql update code
        For Each Field As String In Values.Keys
            ' Check if the column is to update
            If Me.WritableColumns.Contains(Field) And Not Me.mPrimaryKeysColumns.Contains(Field) Then
                ' Add to the set value list to update
                UpdateFields.Add(Field, Values(Field))
            End If
        Next

        ' Check for safety
        If Safety And Clauses.IsEmpty Then
            Throw New Exception("This command will update all row in the table!")
        End If

        ' Create the command and execute it
        Return SCFramework.Bridge.Query.Update(Me.GetTableName(), UpdateFields, Clauses)
    End Function

#End Region

#Region " SUBORDINATES "

    Public Function FindSubordinate(TableName As String) As DbHelper
        ' Cycle all subortdinates
        For Each Subordinate As DbHelper In Me.mSubordinates
            ' Compare the table name
            If String.Compare(Subordinate.GetTableName(), TableName, True) Then
                Return Subordinate
            End If
        Next
        ' Not find
        Return Nothing
    End Function

    Public Sub AddSubordinate(Subordinate As DbHelper)
        Me.mSubordinates.Add(Subordinate)
    End Sub

#End Region

#Region " PROPERTIES "

    Public ReadOnly Property PrimaryKeys As List(Of String)
        Get
            Return Me.mPrimaryKeysColumns
        End Get
    End Property

    Public ReadOnly Property AutoNumbers As List(Of String)
        Get
            Return Me.mAutoNumberColumns
        End Get
    End Property

    Public ReadOnly Property WritableColumns As List(Of String)
        Get
            Return Me.mWritableColumns
        End Get
    End Property

#End Region

#Region " PUBLIC "

    ' Get the linked database table name
    Public MustOverride Function GetTableName() As String

    ' Analize the table and store all usefull data
    Public Overridable Sub OnAnalizeTable()
        ' Holder
        Dim Connection As DbConnection = Bridge.Query.GetConnection()
        Dim Provider As DbQuery.ProvidersList = Bridge.Query.GetProvider()

        ' Open
        Dim MustBeOpen As Boolean = (Bridge.Query.GetConnection().State = ConnectionState.Closed)
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

    ' Get all row from this table
    Public Overridable Function GetSource() As DataTable
        Return Me.GetSource(New DbSqlBuilder.Clauses())
    End Function

#End Region

End Class
