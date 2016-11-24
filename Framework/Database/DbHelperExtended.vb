'*************************************************************************************************
' 
' [SCFramework]
' DbHelperExtended  
' by Samuele Carassai
'
' Extend the helper class to link to database (new from version 5.x)
' Version 5.0.0
' Created 17/09/2015
' Updated 16/10/2016
'
'*************************************************************************************************

Imports System.Data.Common
Imports System.Data.SqlClient


Public MustInherit Class DbHelperExtended
    Inherits DbHelper

    ' Constants
    Private mPossibleImageColumnsName() As String = {"ID_IMAGE", "IMAGE", "ID_BACKGROUND", "BACKGROUND"}
    Private mPossibleFileColumnsName() As String = {"ID_FILE", "FILE", "ID_ATTACHMENT", "ATTACHMENT"}
    Private mPossibleOrderColumnsName() As String = {"ORDER", "ORDER_INDEX"}

    ' Define the holders
    Private mTranslateColumns As List(Of String) = Nothing
    Private mImageColumns As List(Of String) = Nothing
    Private mFileColumns As List(Of String) = Nothing
    Private mOrderColumns As List(Of String) = Nothing


#Region " CONSTRUCTOR "

    Public Sub New()
        ' Base class
        MyBase.New()
    End Sub

#End Region

#Region " PRIVATES "

    ' Check if string is contained in a list of strings
    Private Function Contains(List() As String, ToFind As String) As Boolean
        ' Check for empty values
        If List Is Nothing Then Return False
        ' Return if contained
        Return List.Contains(ToFind)
    End Function

    ' Join the list
    Private Function Join(Values As List(Of String)) As String
        ' Reset the holder
        Dim Builder As String = String.Empty

        ' Cycle all values
        For Each Value As String In Values
            ' Check if empty
            If Not String.IsNullOrEmpty(Builder) Then Builder &= ", "
            ' Add the new value
            Builder = DbSqlBuilder.Quote(Value)
        Next

        ' Return
        Return Builder
    End Function

    ' OleDb analisys 
    Private Sub OleDbAnalisys(Connection As DbConnection)
        ' Connection
        Dim CustomConnection As OleDb.OleDbConnection = CType(Connection, OleDb.OleDbConnection)

        ' Translations, Images and Orders
        Dim Table As DataTable = CustomConnection.GetOleDbSchemaTable(OleDb.OleDbSchemaGuid.Columns, _
                                                                      New Object() {Nothing, Nothing, Me.GetViewName(), Nothing})
        For Each Row As DataRow In Table.Rows
            ' Translations
            If Row!DATA_TYPE = OleDb.OleDbType.WChar AndAlso _
               Row!CHARACTER_MAXIMUM_LENGTH = 32 Then
                Me.mTranslateColumns.Add(Row!COLUMN_NAME)
            End If

            ' Images
            If Row!DATA_TYPE = OleDb.OleDbType.Integer AndAlso _
               Me.Contains(Me.mPossibleImageColumnsName, Row!COLUMN_NAME) Then
                Me.mImageColumns.Add(Row!COLUMN_NAME)
            End If

            ' Files
            If Row!DATA_TYPE = OleDb.OleDbType.Integer AndAlso _
               Me.Contains(Me.mPossibleFileColumnsName, Row!COLUMN_NAME) Then
                Me.mFileColumns.Add(Row!COLUMN_NAME)
            End If

            ' Order
            If (Row!DATA_TYPE = OleDb.OleDbType.Integer Or Row!DATA_TYPE = OleDb.OleDbType.SmallInt) AndAlso _
               Me.Contains(Me.mPossibleOrderColumnsName, Row!COLUMN_NAME) Then
                Me.mOrderColumns.Add(Row!COLUMN_NAME)
            End If
        Next
    End Sub

    ' Sql analisys
    Private Sub SqlAnalisys(Connection As DbConnection)
        ' Define the request for a specific table
        Dim Sql As String = New DbSqlBuilder() _
            .Table(Me.GetViewName()) _
            .Where(DbClauses.AlwaysFalse) _
            .SelectCommand

        ' Find the reader
        Dim Command As SqlCommand = New SqlCommand(SQL, Connection)
        Dim Reader As SqlDataReader = Command.ExecuteReader(CommandBehavior.KeyInfo)

        ' Find the infos table
        Dim Table As DataTable = Reader.GetSchemaTable()
        For Each Row As DataRow In Table.Rows
            ' Translations
            If Row!DataTypeName = GetType(System.String).Name AndAlso _
               Row!ColumnSize = 32 Then
                Me.mTranslateColumns.Add(Row!ColumnName)
            End If

            ' Images
            If Row!DataTypeName = GetType(System.Int32).Name AndAlso _
               Me.Contains(Me.mPossibleImageColumnsName, Row!ColumnName) Then
                Me.mImageColumns.Add(Row!ColumnName)
            End If

            ' Files
            If Row!DataTypeName = GetType(System.Int32).Name AndAlso _
               Me.Contains(Me.mPossibleFileColumnsName, Row!ColumnName) Then
                Me.mFileColumns.Add(Row!ColumnName)
            End If

            ' Order
            If (Row!DataTypeName = GetType(System.Int32).Name Or Row!DataTypeName = GetType(System.Int16).Name) AndAlso _
               Me.Contains(Me.mPossibleOrderColumnsName, Row!ColumnName) Then
                Me.mOrderColumns.Add(Row!ColumnName)
            End If
        Next
    End Sub

#End Region

#Region " OVERRIDES "

    ' Analize the table and store all usefull data
    Public Overrides Sub OnAnalizeTable()
        ' Call the super
        MyBase.OnAnalizeTable()

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
        Me.mTranslateColumns = New List(Of String)
        Me.mImageColumns = New List(Of String)
        Me.mFileColumns = New List(Of String)
        Me.mOrderColumns = New List(Of String)

        ' Select the analisys by provider
        Select Case Provider
            Case DbQuery.ProvidersList.OleDb : Me.OleDbAnalisys(Connection)
            Case DbQuery.ProvidersList.SqlClient : Me.SqlAnalisys(Connection)
        End Select

        ' Close
        If MustBeOpen Then
            Connection.Close()
        End If
    End Sub

    ' Delete
    Public Overrides Function Delete(Clauses As SCFramework.DbClauses) As Long
        ' Get the current query object
        Dim Query As SCFramework.DbQuery = Me.Query
        ' Determine if must manage the transaction
        Dim TransactionOwner As Boolean = Not Query.InTransaction

        Try
            ' Check if not within a transaction
            If TransactionOwner Then Query.StartTransaction()

            ' Check if have references to delete
            If Me.FileColumns.Count > 0 Or Me.ImageColumns.Count > 0 Or Me.TranslateColumns.Count > 0 Then
                ' TODO: delete translation and images references
            End If

            ' Call the base method to delete records on the current table
            Delete = MyBase.Delete(Clauses)

            ' Commit the transaction is needed
            If TransactionOwner Then Query.CommitTransaction()

        Catch ex As Exception
            ' Rollback the transaction is needed and propagate the exception
            If TransactionOwner Then Query.RollBackTransaction()
            Throw ex

        End Try
    End Function

#End Region

#Region " PROPERTIES "

    ' The order key columns list
    Public Property OrderColumns As List(Of String)
        Get
            Return Me.mOrderColumns
        End Get
        Set(value As List(Of String))
            Me.mOrderColumns = value
        End Set
    End Property

    ' The file key columns list
    Public Property FileColumns As List(Of String)
        Get
            Return Me.mFileColumns
        End Get
        Set(value As List(Of String))
            Me.mFileColumns = value
        End Set
    End Property

    ' The image key columns list
    Public Property ImageColumns As List(Of String)
        Get
            Return Me.mImageColumns
        End Get
        Set(value As List(Of String))
            Me.mImageColumns = value
        End Set
    End Property

    ' The translate key columns list
    Public Property TranslateColumns As List(Of String)
        Get
            Return Me.mTranslateColumns
        End Get
        Set(value As List(Of String))
            Me.mTranslateColumns = value
        End Set
    End Property

#End Region

#Region " PUBLIC "

    ' Get the linked database view name.
    ' If not overrided the view table name is the same of the linked table name.
    Public Overridable Function GetViewName() As String
        Return Me.GetTableName()
    End Function

#End Region

End Class
