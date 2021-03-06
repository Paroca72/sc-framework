﻿'*************************************************************************************************
' 
' [SCFramework]
' DbHelperExtended  
' by Samuele Carassai
'
' Extend the helper class to link to database (new from version 5.x)
'
' Version 5.0.0
' Updated 16/10/2016
'
'*************************************************************************************************

Imports System.Data.Common
Imports System.Data.SqlClient


' Define the name space
Namespace DB

    ' Class definition
    Public MustInherit Class HelperExtended
        Inherits Table

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
            ' Base
            MyBase.New()

            ' Analize the table 
            Me.OnAnalizeTable()
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
                Builder = SqlBuilder.Quote(Value)
            Next

            ' Return
            Return Builder
        End Function


        ' OleDb analisys 
        Private Sub OleDbAnalisys(Connection As DbConnection)
            ' Connection
            Dim CustomConnection As OleDb.OleDbConnection = CType(Connection, OleDb.OleDbConnection)

            ' Translations, Images and Orders
            Dim Table As DataTable = CustomConnection.GetOleDbSchemaTable(OleDb.OleDbSchemaGuid.Columns,
                                                                          New Object() {Nothing, Nothing, Me.ViewName(), Nothing})
            ' TODO: temporary fixed
            Dim PrimaryKeys() As String = Me.GetColumnsName(Column.Types.PrimaryKey)
            Dim AutoNumbers() As String = Me.GetColumnsName(Column.Types.Identity)

            For Each Row As DataRow In Table.Rows
                ' Column name
                Dim ColumnName As String = Row!COLUMN_NAME

                ' Check is not a primary or incremental key
                If Not PrimaryKeys.Contains(ColumnName) And Not AutoNumbers.Contains(ColumnName) Then
                    ' Translations
                    If Row!DATA_TYPE = OleDb.OleDbType.WChar AndAlso Row!CHARACTER_MAXIMUM_LENGTH = 32 Then
                        Me.mTranslateColumns.Add(ColumnName)
                    End If

                    ' Images
                    If Row!DATA_TYPE = OleDb.OleDbType.Integer AndAlso Me.Contains(Me.mPossibleImageColumnsName, ColumnName) Then
                        Me.mImageColumns.Add(ColumnName)
                    End If

                    ' Files
                    If Row!DATA_TYPE = OleDb.OleDbType.Integer AndAlso Me.Contains(Me.mPossibleFileColumnsName, ColumnName) Then
                        Me.mFileColumns.Add(ColumnName)
                    End If

                    ' Order
                    If (Row!DATA_TYPE = OleDb.OleDbType.Integer Or Row!DATA_TYPE = OleDb.OleDbType.SmallInt) AndAlso
                        Me.Contains(Me.mPossibleOrderColumnsName, ColumnName) Then
                        Me.mOrderColumns.Add(ColumnName)
                    End If
                End If
            Next
        End Sub


        ' Sql analisys
        Private Sub SqlAnalisys(Connection As DbConnection)
            ' Define the request for a specific table
            Dim Sql As String = New DB.SqlBuilder(Me.Query.GetProvider()) _
                .Table(Me.ViewName()) _
                .Where(Clauses.AlwaysFalse) _
                .SelectCommand

            ' Find the reader
            Dim Command As SqlCommand = New SqlCommand(Sql, Connection)
            Dim Reader As SqlDataReader = Command.ExecuteReader(CommandBehavior.KeyInfo)

            ' Find the infos table
            Dim Table As DataTable = Reader.GetSchemaTable()

            ' TODO: temporary fixed
            Dim PrimaryKeys() As String = Me.GetColumnsName(Column.Types.PrimaryKey)
            Dim AutoNumbers() As String = Me.GetColumnsName(Column.Types.Identity)

            For Each Row As DataRow In Table.Rows
                ' Column name
                Dim ColumnName As String = Row!COLUMN_NAME

                ' Check is not a primary or incremental key
                If Not PrimaryKeys.Contains(ColumnName) And Not AutoNumbers.Contains(ColumnName) Then
                    ' Translations
                    If Row!DataType = GetType(System.String).Name AndAlso Row!ColumnSize = 32 Then
                        Me.mTranslateColumns.Add(ColumnName)
                    End If

                    ' Images
                    If Row!DataType = GetType(System.Int32).Name AndAlso
                       Me.Contains(Me.mPossibleImageColumnsName, ColumnName) Then
                        Me.mImageColumns.Add(ColumnName)
                    End If

                    ' Files
                    If Row!DataType = GetType(System.Int32).Name AndAlso
                       Me.Contains(Me.mPossibleFileColumnsName, ColumnName) Then
                        Me.mFileColumns.Add(ColumnName)
                    End If

                    ' Order
                    If (Row!DataType = GetType(System.Int32).Name Or Row!DataType = GetType(System.Int16).Name) AndAlso
                       Me.Contains(Me.mPossibleOrderColumnsName, ColumnName) Then
                        Me.mOrderColumns.Add(ColumnName)
                    End If
                End If
            Next
        End Sub


        ' Analize the table and store all usefull data
        Private Sub OnAnalizeTable()
            ' Private holders
            Dim Query As Query = Me.Query
            Dim Connection As DbConnection = Query.GetConnection()

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
            Select Case Query.GetProvider()
                Case "System.Data.OleDb" : Me.OleDbAnalisys(Connection)
                Case "System.Data.SqlClient" : Me.SqlAnalisys(Connection)
            End Select

            ' Close
            If MustBeOpen Then
                Connection.Close()
            End If
        End Sub

#End Region

#Region " OVERRIDES "

        ' Get the list of all values inside the columns
        Private Function GetValues(Columns As List(Of String), Clauses As Clauses) As List(Of String)
            ' Values
            Dim Values As List(Of String) = New List(Of String)

            ' Cycle all columns
            For Each Column As String In Columns
                ' Create the SQL
                Dim SQL As SqlBuilder = New DB.SqlBuilder(Me.Query.GetProvider()) _
                    .Table(Me.Name()) _
                    .Select(Column) _
                    .Where(Clauses)

                ' Get the values of the current column
                Dim Table As DataTable = Me.Query.Table(SQL.SelectCommand)
                Values.AddRange(Table.AsEnumerable().Select(Of String)(Function(Row) Str(Row(Column))).ToArray())
            Next

            ' Return the values list
            Return Values
        End Function


        ' Delete all values using a multilanguages class manager
        Private Sub DeleteMultilanguagesColumns(Query As Query, Values As List(Of String), Manager As Multilanguages)
            ' link the query to the manager and delete
            Manager.Query = Query
            Values.ForEach(Sub(Value) Manager.Delete(Value))

            ' Save the changes on DB
            Manager.AcceptChanges()
        End Sub


        ' Delete
        Public Overrides Function Delete(Clauses As Clauses) As Long
            ' Get the current query object and determine if must manage the transaction
            Dim Query As Query = Me.Query
            Dim TransactionOwner As Boolean = Not Query.InTransaction

            Try
                ' Check if not within a transaction
                If TransactionOwner Then Query.StartTransaction()

                ' Delete the related multilanguages columns
                If Me.TranslateColumns.Count > 0 Then Me.DeleteMultilanguagesColumns(Query, Me.GetValues(Me.TranslateColumns, Clauses), Bridge.Translations())
                If Me.FileColumns.Count > 0 Then Me.DeleteMultilanguagesColumns(Query, Me.GetValues(Me.FileColumns, Clauses), Bridge.Files())
                If Me.ImageColumns.Count > 0 Then Me.DeleteMultilanguagesColumns(Query, Me.GetValues(Me.ImageColumns, Clauses), Bridge.Files())

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
        Public Overridable Function ViewName() As String
            Return Me.Name()
        End Function

#End Region

    End Class

End Namespace
