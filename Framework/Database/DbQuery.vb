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
' Updated 18/10/2016
'
' Integration: SqlClient, OleDB
'
'*************************************************************************************************

Imports System.Data.Common


' Classe Query
Public Class DbQuery

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
    Private Function CreateDatabaseConnection(ConnectionSettings As ConnectionStringSettings) As DbConnection
        Try
            ' Fix the connection string if the url is relative
            Dim ConnectionString As String = ConnectionSettings.ConnectionString
            If ConnectionString.Contains("~/") Then
                Dim MapPath As String = Web.Hosting.HostingEnvironment.MapPath("~")
                ConnectionString = ConnectionString.Replace("~/", MapPath).Replace("/", "\")
            End If

            ' Get the factory class
            Dim Factory As DbProviderFactory = DbProviderFactories.GetFactory(ConnectionSettings.ProviderName)

            ' Create the connection
            Dim Connection As DbConnection = Factory.CreateConnection()
            Connection.ConnectionString = ConnectionString

            ' Return
            Return Connection

        Catch ex As Exception
            ' Else throw an exception
            Throw New Exception("Impossible to create the connection to the DB!")
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
                Case "System.Data.OleDb"
                    '-----------------------------------------
                    Dim Table As DataTable = Me.mConnection.GetSchema("Tables", New Object() {Nothing, Name, Nothing})
                    Return Table.Rows.Count <> 0

                Case "System.Data.SqlClient"
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
    Public ReadOnly Property GetProvider() As String
        Get
            ' Check if the connection exists
            If Me.mConnection IsNot Nothing Then
                ' Check for sql
                If TypeOf Me.mConnection Is SqlClient.SqlConnection Then
                    Return "System.Data.SqlClient"
                End If

                ' Check for OleDb
                If TypeOf Me.mConnection Is OleDb.OleDbConnection Then
                    Return "System.Data.OleDb"
                End If
            End If

            ' Not finded
            Return "Undefined"
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

    ' If the have an active transaction
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
                Case "System.Data.OleDb"
                    ' Execute the sql command
                    Me.ExecuteQuery(Sql, False)
                    ' Execute the identity request
                    Return Me.ExecuteQuery("SELECT @@IDENTITY", True)

                Case "System.Data.SqlClient"
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
            Case "System.Data.OleDb"
                ' Cycle all sql command and execute it
                For Each Sql As String In SqlCommands
                    Me.ExecuteQuery(Sql, False)
                Next

            Case "System.Data.SqlClient"
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
    Public Function Insert(TableName As String, Values As Dictionary(Of String, Object)) As Long
        ' Execute the query command
        Dim Command As String = New DbSqlBuilder() _
            .Table(TableName) _
            .Insert(Values) _
            .InsertCommand
        Return Me.Exec(Command, True)
    End Function

    ' Get the last identity
    Public Function Indentity() As Long
        ' Check for database connection
        Me.CheckDbConnection()

        ' Choice the action by the provider
        Select Case Me.GetProvider()
            Case "System.Data.OleDb" : Return Me.ExecuteQuery("SELECT @@IDENTITY", True)
            Case "System.Data.SqlClient" : Return Me.ExecuteQuery("SELECT Scope_Identity();", True)
        End Select
    End Function

    ' Generic update command
    Public Function Update(TableName As String, Values As Dictionary(Of String, Object), Clause As SCFramework.DbClauses) As Long
        ' Execute the query command
        Dim Command As String = New DbSqlBuilder() _
            .Table(TableName) _
            .Update(Values) _
            .Where(Clause) _
            .UpdateCommand
        Return Me.Exec(Command, True)
    End Function

    ' Generic delete command
    Public Function Delete(TableName As String, Clause As SCFramework.DbClauses) As Long
        ' Execute the query command
        Dim Command As String = New DbSqlBuilder() _
            .Table(TableName) _
            .Where(Clause) _
            .DeleteCommand
        Return Me.Exec(Command, True)
    End Function

#End Region

#Region " DATASET AND DATATABLE "

    ' Execute a sql command and put the result inside a datatable
    Public Function Table(ByVal Sql As String, Optional ByVal TableName As String = Nothing) As DataTable
        ' Check for database connection
        Me.CheckDbConnection()

        ' Define the adapter
        Dim ProviderFactory As DbProviderFactory = DbProviderFactories.GetFactory(Me.GetProvider)
        Dim DataAdapter As DbDataAdapter = ProviderFactory.CreateDataAdapter()

        ' Define the datatale and fix the table name
        Dim DataTable As DataTable = New DataTable()
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
    Public Function Table(TableName As String) As DataTable
        ' Execute the query command
        Dim Command As String = New DbSqlBuilder() _
            .Table(TableName) _
            .SelectCommand
        Return Me.Table(Command, TableName)
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

    ' Create a generic adapter
    Public Function CreateAdapter(ByVal TableName As String) As DbDataAdapter
        Try
            ' Get the facotry
            Dim ProviderFactory As DbProviderFactory = DbProviderFactories.GetFactory(Me.GetProvider)

            ' Create the command
            Dim Command As DbCommand = ProviderFactory.CreateCommand()
            Command.Connection = Me.mConnection
            Command.CommandText = New DbSqlBuilder().Table(TableName).Where(DbClauses.AlwaysFalse).SelectCommand
            Command.CommandTimeout = Me.mCommandTimeout
            Command.Transaction = Me.mTransaction

            ' Create the adapter
            Dim DataAdapter As DbDataAdapter = ProviderFactory.CreateDataAdapter()
            DataAdapter.SelectCommand = Command

            ' Associate the command builder to the adapter
            Dim CommandBuilder As DbCommandBuilder = ProviderFactory.CreateCommandBuilder()
            CommandBuilder.DataAdapter = DataAdapter
            CommandBuilder.QuotePrefix = DbSqlBuilder.QUOTE_PREFIX
            CommandBuilder.QuoteSuffix = DbSqlBuilder.QUOTE_SUFFIX

            ' Return adapter
            Return DataAdapter

        Catch ex As Exception
            ' If have some errors
            Return Nothing
        End Try
    End Function

    ' Update the database
    Public Sub UpdateDatabase(ByVal Source As DataTable, ByVal TableName As String,
                              Optional ByVal ContinueOnError As Boolean = False)
        ' Create the adapter and check for empty value
        Dim Adapter As DbDataAdapter = Me.CreateAdapter(TableName)
        If Adapter IsNot Nothing Then
            ' Set the adapter and update the table
            Adapter.ContinueUpdateOnError = ContinueOnError
            Adapter.Update(Source)
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

