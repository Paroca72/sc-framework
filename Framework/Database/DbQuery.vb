'*************************************************************************************************
' 
' [SCFramework]
' DbQuery  
' by Samuele Carassai
'
' Database query manager
' Versione 5.0.0
'
' Created 05/11/2015
' Updated 11/10/2016
'
' Integration: SqlClient, OleDB
'
'*************************************************************************************************

Imports System.Data.Common


' Classe Query
Public Class DbQuery

    ' Providers
    Public Enum ProvidersList As Short
        Undefined = -1
        SqlClient = 0
        OleDb = 1
    End Enum


    ' The database connection generic object
    Private mConnection As DbConnection = Nothing
    Private mTransaction As DbTransaction = Nothing
    Private mCommandTimeout As Integer = 30


    ' Constructor
    Sub New()
        Me.mConnection = Me.CreateDatabaseConnection()
    End Sub

    Sub New(ConnectionStringName As String)
        Me.mConnection = Me.CreateDatabaseConnection(ConnectionStringName)
    End Sub

#Region " CONNECTION "

    ' Create the connection to database by the passes connection string name
    Private Function CreateDatabaseConnection(ConnectionString As ConnectionStringSettings) As DbConnection
        Try
            ' Get the factory class
            Dim Factory As DbProviderFactory = DbProviderFactories.GetFactory(ConnectionString.ProviderName)

            ' Create the connection
            Dim Connection As DbConnection = Factory.CreateConnection()
            Connection.ConnectionString = ConnectionString.ConnectionString

            ' Return
            Return Connection

        Catch ex As Exception
            ' Else return nothing
            Return Nothing
        End Try
    End Function

    ' Get the connection string by name
    Private Function GetConnectionStringByName(Name As String) As ConnectionStringSettings
        ' Get the collection of connection strings. 
        Dim Settings As ConnectionStringSettingsCollection = ConfigurationManager.ConnectionStrings

        ' Check for settings
        If Not Settings Is Nothing Then
            ' Walk through the collection and return the first connection string matching the name
            For Each CS As ConnectionStringSettings In Settings
                If CS.Name = Name Then
                    Return CS
                End If
            Next
        End If

        ' Else
        Return Nothing
    End Function

    ' Create the connection to database by the passes connection string name
    Private Function CreateDatabaseConnection(Name As String) As DbConnection
        ' Get the connection string
        Dim ConnectionString As ConnectionStringSettings = Me.GetConnectionStringByName(Name)

        ' Check if have a connection string
        If ConnectionString IsNot Nothing Then
            Throw New Exception("The connection string to database is undefined!")
        End If

        ' Get the connection
        Return Me.CreateDatabaseConnection(ConnectionString)
    End Function

    ' Create the connection to database by the passes connection string name
    Private Function CreateDatabaseConnection() As DbConnection
        ' Check if have at least a connection string available
        If ConfigurationManager.ConnectionStrings.Count = 0 Then
            Throw New Exception("No have connection string available!")
        End If

        ' Get the connection string
        Dim ConnectionString As ConnectionStringSettings = Me.GetConnectionStringByName("Default")

        ' if nothing try to take the first available
        If ConnectionString Is Nothing Then
            ConnectionString = ConfigurationManager.ConnectionStrings(0)
        End If

        ' Create the connection
        Return Me.CreateDatabaseConnection(ConnectionString)
    End Function

#End Region

#Region " DATABASE "

    ' Check for database connection.
    ' If no have an available connection throw a new exception.
    Private Sub CheckDbConnection()
        If Me.mConnection Is Nothing Then
            Throw New Exception("No database connection available!")
        End If
    End Sub

    ' Check if a table exists inside the database
    Public Function ExistsDbTable(Name As String) As Boolean
        ' Check for database connection
        Me.CheckDbConnection()

        ' Hold the last connection status
        Dim MustBeOpen As Boolean

        ' Try
        Try
            ' Check if the connection must be open
            MustBeOpen = (Me.mConnection.State = ConnectionState.Closed)
            If MustBeOpen Then
                Me.mConnection.Open()
            End If

            ' Select the analisys by provider
            Select Case Me.GetProvider()
                Case DbQuery.ProvidersList.OleDb
                    '-----------------------------------------
                    Dim Table As DataTable = Me.mConnection.GetSchema("Tables", New Object() {Nothing, Name, Nothing})
                    Return Table.Rows.Count <> 0

                Case DbQuery.ProvidersList.SqlClient
                    '-----------------------------------------
                    Dim Sql As String = String.Format("SELECT * FROM sys.tables WHERE name = '{0}' AND type = 'U'", Name)
                    Return Me.Exists(Sql)

            End Select

        Catch ex As Exception
            ' Throw the exception
            Throw ex

        Finally
            ' Try to close the connection to database and restore the last connection status
            If MustBeOpen Then
                Me.mConnection.Close()
            End If

        End Try

        ' Default
        Return False
    End Function

    ' Try Create the database table by an sql dump saved inside the resource
    Public Sub CreateDbTable(Name As String)
        ' Create the resource file name
        Dim ResourceName As String = String.Format("DUMP_{0}", Name)
        ' Check if have into resuorce a sql dump file to use for learn the table structure
        Dim Dump As String = My.Resources.ResourceManager.GetString(ResourceName)
        ' Check for empty value
        If Not String.IsNullOrEmpty(Dump) Then

        End If
    End Sub

#End Region

#Region " PROPERTIES "

    ' Get the provider type (READONLY)
    Public ReadOnly Property GetProvider() As ProvidersList
        Get
            ' Check if the connection exists
            If Me.mConnection IsNot Nothing Then
                ' Check for sql
                If TypeOf Me.mConnection Is SqlClient.SqlConnection Then
                    Return ProvidersList.SqlClient
                End If

                ' Check for OleDb
                If TypeOf Me.mConnection Is OleDb.OleDbConnection Then
                    Return ProvidersList.OleDb
                End If
            End If

            ' Not finded
            Return ProvidersList.Undefined
        End Get
    End Property

    ' Get the connection
    Public ReadOnly Property GetConnection As DbConnection
        Get
            Return Me.mConnection
        End Get
    End Property

    ' Get or set the command timeout
    Public Property CommandTimeout() As Integer
        Get
            Return Me.mCommandTimeout
        End Get
        Set(ByVal value As Integer)
            If value < 0 Then value = 0
            Me.mCommandTimeout = value
        End Set
    End Property

#End Region

#Region " TRANSACTION "

    ' Start a transaction
    Public Sub StartTransaction()
        ' Check for the connection available
        If Me.mConnection IsNot Nothing And Me.mTransaction Is Nothing Then
            ' Open the connection to the database
            Me.mConnection.Open()
            ' Hold the transaction reference
            Me.mTransaction = Me.mConnection.BeginTransaction(IsolationLevel.ReadCommitted)
        End If
    End Sub

    ' Finish a executing transaction
    Public Sub FinishTransaction(ByVal Commit As Boolean)
        ' Check for the transaction and the connection
        If Me.mTransaction IsNot Nothing AndAlso Me.mTransaction.Connection IsNot Nothing Then
            ' Choice the operation
            If Commit Then
                Me.mTransaction.Commit()
            Else
                Me.mTransaction.Rollback()
            End If
        End If

        ' Reset the transaction holder variable
        Me.mTransaction = Nothing

        ' Close the connection to the database if needed
        If Me.mConnection IsNot Nothing AndAlso Me.mConnection.State = ConnectionState.Open Then
            Me.mConnection.Close()
        End If
    End Sub

    ' Return true if a transaction is already started
    Public Function InTransaction() As Boolean
        Return Me.mTransaction IsNot Nothing
    End Function

    ' Shortcut to commint
    Public Sub CommitTransaction()
        Me.FinishTransaction(True)
    End Sub

    ' Shortcut to rollback
    Public Sub RollBackTransaction()
        Me.FinishTransaction(False)
    End Sub

#End Region

#Region " QUERY EXECUTION "

    ' Internal sql command execution
    Private Function ExecuteQuery(Sql As String, ReturnValue As Boolean) As Object
        Dim MustBeOpen As Boolean = False
        Try
            ' Open connection is closed and save the state
            MustBeOpen = (Me.mConnection.State = ConnectionState.Closed)
            If MustBeOpen Then
                Me.mConnection.Open()
            End If

            ' Create a generic command and set the Sql command to execute
            Dim Command As DbCommand = Me.mConnection.CreateCommand()
            Command.CommandText = Sql

            ' Fix the command time out
            Command.CommandTimeout = Me.mCommandTimeout

            ' If have a defined transaction apply it to command
            If Me.mTransaction IsNot Nothing Then
                Command.Transaction = Me.mTransaction
            End If

            ' Execute the command
            If ReturnValue Then
                Return Command.ExecuteScalar()
            Else
                Return Command.ExecuteNonQuery()
            End If

        Catch ex As Exception
            Throw ex

        Finally
            ' Close connection
            If MustBeOpen Then
                Me.mConnection.Close()
            End If
        End Try
    End Function

    ' Execute a sql command and return the identity if needed
    Public Function Exec(ByVal Sql As String, Optional ByVal ReturnIdentity As Boolean = False) As Long
        ' Check for database connection
        Me.CheckDbConnection()

        ' Identity request
        If ReturnIdentity Then
            ' Choice the action by the provider
            Select Case Me.GetProvider()
                Case ProvidersList.OleDb
                    ' Execute the sql command
                    Me.ExecuteQuery(Sql, False)
                    ' Execute the identity request
                    Return Me.ExecuteQuery("SELECT @@IDENTITY", True)

                Case ProvidersList.SqlClient
                    ' Add the identity request to sql command
                    Sql &= "; SELECT Scope_Identity();"
                    ' Execute the command
                    Return Me.ExecuteQuery(Sql, True)

            End Select
        Else
            ' Normal execution
            Return Me.ExecuteQuery(Sql, False)
        End If
    End Function

    ' Execute a list of commands
    Public Sub Exec(ByVal ParamArray SqlCommands() As String)
        ' Choice the action by the provider
        Select Case Me.GetProvider()
            Case ProvidersList.OleDb
                ' Cycle all sql command and execute it
                For Each Sql As String In SqlCommands
                    Me.ExecuteQuery(Sql, False)
                Next

            Case ProvidersList.SqlClient
                ' Create a command list
                Dim Compound As String = String.Empty
                For Each Sql As String In SqlCommands
                    Compound &= Sql & ";"
                Next

                ' Execute
                Me.ExecuteQuery(Compound, False)
        End Select
    End Sub

    ' Execute a sql command and get the result value
    Public Function Value(ByVal Sql As String) As Object
        Return Me.ExecuteQuery(Sql, True)
    End Function

    ' Check if a query exists
    Public Function Exists(Sql As String) As Boolean
        Return Me.Row(Sql) IsNot Nothing
    End Function

    ' Generic insert command
    Public Function Insert(TableName As String, Values As Hashtable) As Long
        ' Execute the query command
        Return Me.Exec(DbSqlBuilder.BuildInsertCommand(TableName, Values), True)
    End Function

    ' Generic update command
    Public Function Update(TableName As String, Values As Hashtable, Clause As SCFramework.DbClauses) As Long
        ' Execute the query command
        Return Me.Exec(DbSqlBuilder.BuildUpdateCommand(TableName, Values, Clause), True)
    End Function

    ' Generic delete command
    Public Function Delete(TableName As String, Clause As SCFramework.DbClauses) As Long
        ' Execute the query command
        Return Me.Exec(DbSqlBuilder.BuildDeleteCommand(TableName, Clause), True)
    End Function

#End Region

#Region " DATASET AND DATATABLE "

    ' Execute a sql command and put the result inside a datatable
    Public Function Table(ByVal Sql As String, Optional ByVal TableName As String = Nothing) As DataTable
        ' Check for database connection
        Me.CheckDbConnection()

        ' Define the datatable and the adapter
        Dim DataTable As DataTable = New DataTable()
        Dim DataAdapter As DbDataAdapter = DbProviderFactories.GetFactory(Me.mConnection).CreateDataAdapter()

        ' Fix the table name
        If Not String.IsNullOrEmpty(TableName) Then
            DataTable.TableName = TableName
        End If

        ' If exists
        If DataAdapter IsNot Nothing Then
            ' Create the command
            Dim Command As DbCommand = Me.mConnection.CreateCommand()
            Command.CommandText = Sql
            Command.CommandTimeout = Me.mCommandTimeout
            Command.Transaction = Me.mTransaction

            ' Assign the command to the adapter and fill the table
            DataAdapter.SelectCommand = Command
            DataAdapter.Fill(DataTable)
        End If

        ' Return
        Return DataTable
    End Function

    ' Create a sql command and put the result inside a datatable
    Public Function Table(TableName As String, Fields As ICollection, Clauses As SCFramework.DbClauses) As DataTable
        ' Execute the query command
        Return Me.Table(DbSqlBuilder.BuildSelectCommand(TableName, Fields, Clauses), TableName)
    End Function

    ' Create a sql command and put the result inside a datatable
    Public Function Table(TableName As String) As DataTable
        ' Execute the query command
        Return Me.Table(DbSqlBuilder.BuildSelectCommand(TableName, Nothing, Nothing), TableName)
    End Function

    ' Execute a sql command and get the first row details
    Public Function Row(ByVal Sql As String) As DataRow
        ' Get the table
        Dim DataTable As DataTable = Table(Sql)
        ' If have rows return the first row of the list
        If DataTable IsNot Nothing AndAlso DataTable.Rows.Count > 0 Then
            Return DataTable.Rows(0)
        Else
            Return Nothing
        End If
    End Function

    ' Execute a sql command and put the result inside a hashtable
    Public Function Dictionary(ByVal Sql As String, ByVal KeyField As String, ByVal ValueField As String) As Dictionary(Of Object, Object)
        Dim Source As DataTable = Me.Table(Sql)
        Return SCFramework.Utils.DataTable.ToDictionary(Source, KeyField, ValueField)
    End Function

    ' Execute a sql command and put the result inside a arraylist
    Public Function Array(ByVal Sql As String, Optional ByVal Field As String = Nothing) As Object()
        ' Get the source
        Dim Source As DataTable = Me.Table(Sql)

        ' If field if nothing fill the array list with the first column available
        If Field Is Nothing AndAlso Source.Columns.Count > 0 Then
            Field = Source.Columns(0).ColumnName
        End If

        ' Return the array list
        Return SCFramework.Utils.DataTable.ToArray(Source, Field)
    End Function

    ' Update the database
    Public Sub UpdateDatabase(ByVal Source As DataTable, ByVal TableName As String, Optional ByVal ContinueOnError As Boolean = False)
        ' Fix the table name quotes
        If Not TableName.StartsWith("[") And Not TableName.EndsWith("]") Then
            TableName = String.Format("[{0}]", TableName)
        End If

        ' Create a generic selection command
        Dim SelectSql As String = String.Format("SELECT * FROM {0} WHERE 1 <> 1", TableName)

        ' Create the data adapter
        Dim DataAdapter As DbDataAdapter = DbProviderFactories.GetFactory(Me.mConnection).CreateDataAdapter()
        ' Create the command builder
        Dim CommandBuilder As DbCommandBuilder = DbProviderFactories.GetFactory(Me.mConnection).CreateCommandBuilder()

        ' Check for exists
        If DataAdapter IsNot Nothing And CommandBuilder IsNot Nothing Then
            ' Set the quotes character
            CommandBuilder.QuotePrefix = DbSqlBuilder.QuotePrefix
            CommandBuilder.QuoteSuffix = DbSqlBuilder.QuoteSuffix

            ' Create the command
            Dim Command As DbCommand = Me.mConnection.CreateCommand()
            Command.CommandText = SelectSql
            Command.CommandTimeout = Me.mCommandTimeout
            Command.Transaction = Me.mTransaction

            ' Assign the command and update the database
            DataAdapter.ContinueUpdateOnError = ContinueOnError
            DataAdapter.SelectCommand = Command
            DataAdapter.Update(Source)
        End If
    End Sub

    Public Sub UpdateDatabase(ByVal Source As DataTable)
        ' Check for the name of table
        If String.IsNullOrEmpty(Source.TableName) Then
            Throw New Exception("Table must be have the name assigned!")
        Else
            ' Update
            Me.UpdateDatabase(Source, Source.TableName)
        End If
    End Sub

#End Region

End Class

