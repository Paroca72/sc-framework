'*************************************************************************************************
' 
' [SCFramework]
' DatabaseTableHelper  
' by Samuele Carassai
'
' Extend the helper class to link to database (new from version 5.x)
' Version 5.0.0
' Created 17/09/2015
' Updated 29/10/2015
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
    Private mImagesColumns As List(Of String) = Nothing
    Private mFilesColumns As List(Of String) = Nothing
    Private mOrdersColumns As List(Of String) = Nothing


#Region " PRIVATES "

    ' Check if string is contained in a list of strings
    Private Function Contains(List() As String, ToFind As String) As Boolean
        Return CType(List, IList(Of String)).Contains(ToFind)
    End Function

    ' Filter the list and return only the item contained inside the writable fields
    Private Function GetWritableFilteredColumns(Source As List(Of String)) As String()
        ' The holder
        Dim List As List(Of String) = New List(Of String)

        ' Cycle all elements in list
        For Each Column As String In Source
            ' Check if conatined inside the writable columns list
            If Me.WritableColumns.Contains(Column) Then
                ' Add to filtered list
                List.Add(Column)
            End If
        Next

        ' Return the filtered list
        Return List.ToArray
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
                Me.mImagesColumns.Add(Row!COLUMN_NAME)
            End If

            ' Files
            If Row!DATA_TYPE = OleDb.OleDbType.Integer AndAlso _
               Me.Contains(Me.mPossibleFileColumnsName, Row!COLUMN_NAME) Then
                Me.mFilesColumns.Add(Row!COLUMN_NAME)
            End If

            ' Order
            If (Row!DATA_TYPE = OleDb.OleDbType.Integer Or Row!DATA_TYPE = OleDb.OleDbType.SmallInt) AndAlso _
               Me.Contains(Me.mPossibleOrderColumnsName, Row!COLUMN_NAME) Then
                Me.mOrdersColumns.Add(Row!COLUMN_NAME)
            End If
        Next
    End Sub

    ' Sql analisys
    Private Sub SqlAnalisys(Connection As DbConnection)
        ' Define the request for a specific table
        Dim Sql As String = DbSqlBuilder.BuildSelectCommand(Me.GetViewName(), Nothing, Nothing)

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
                Me.mImagesColumns.Add(Row!ColumnName)
            End If

            ' Files
            If Row!DataTypeName = GetType(System.Int32).Name AndAlso _
               Me.Contains(Me.mPossibleFileColumnsName, Row!ColumnName) Then
                Me.mFilesColumns.Add(Row!ColumnName)
            End If

            ' Order
            If (Row!DataTypeName = GetType(System.Int32).Name Or Row!DataTypeName = GetType(System.Int16).Name) AndAlso _
               Me.Contains(Me.mPossibleOrderColumnsName, Row!ColumnName) Then
                Me.mOrdersColumns.Add(Row!ColumnName)
            End If
        Next
    End Sub

#End Region

#Region " OVERRIDES "

    ' Analize the table and store all usefull data
    Public Overrides Sub OnAnalizeTable()
        ' Call the super
        MyBase.OnAnalizeTable()

        ' Holder
        Dim Connection As DbConnection = Bridge.Query.GetConnection()
        Dim Provider As DbQuery.ProvidersList = Bridge.Query.GetProvider()

        ' Open
        Dim MustBeOpen As Boolean = (Bridge.Query.GetConnection().State = ConnectionState.Closed)
        If MustBeOpen Then
            Connection.Open()
        End If

        ' Define the holder
        Me.mTranslateColumns = New List(Of String)
        Me.mImagesColumns = New List(Of String)
        Me.mFilesColumns = New List(Of String)
        Me.mOrdersColumns = New List(Of String)

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

    ' Get the source and add some extra information
    Protected Overrides Function GetSource(Clauses As DbSqlBuilder.Clauses) As DataTable
        ' Get the source from the base class
        Dim Source As DataTable = MyBase.GetSource(Clauses)

        ' Translations
        For Each Column As String In Me.mTranslateColumns
            SCFramework.Translations.TranslateColumn(Source, Column)
        Next

        ' Language less
        If Me.mTranslateColumns.Count > 0 Then
            SCFramework.Translations.AddLanguageLessColumn(Source, "LANGUAGE_LESS", Me.mTranslateColumns.ToArray)
        End If

        ' Images
        For Each Column As String In Me.mImagesColumns
            ' TODO
            'SCFramework.ManageImages.AddExtraInfosColumn(Source, SCFramework.ManageFiles.ExtraInfoType.RelativePath, Column & "_URL", Column)
        Next

        ' Files
        For Each Column As String In Me.mFilesColumns
            ' TODO
            'SCFramework.ManageImages.AddExtraInfosColumn(Source, SCFramework.ManageFiles.ExtraInfoType.RelativePath, Column & "_URL", Column)
        Next

        ' Return
        Return Source
    End Function

    ' Delete
    Protected Overrides Function Delete(Clauses As DbSqlBuilder.Clauses, Optional Safety As Boolean = True) As Long
        ' Get the source datatable
        Dim Source As DataTable = Me.GetSource(Clauses)

        ' Get the filtered list
        Dim Translations() As String = Me.GetWritableFilteredColumns(Me.mTranslateColumns)
        Dim Images() As String = Me.GetWritableFilteredColumns(Me.mImagesColumns)
        Dim Files() As String = Me.GetWritableFilteredColumns(Me.mFilesColumns)

        ' Extract the values
        Dim TranslationValues() As String = SCFramework.Utils.ExtractStringValues(Source, Translations)
        Dim ImageValues() As Long = SCFramework.Utils.ExtractIntValues(Source, Images)
        Dim FileValues() As Long = SCFramework.Utils.ExtractIntValues(Source, Files)

        ' Delete translations
        SCFramework.Translations.Delete(TranslationValues)
        ' TODO
        'SCFramework.ManageFiles.Delete(ImageValues)
        'SCFramework.ManageFiles.Delete(FileValues)

        ' Super
        Return MyBase.Delete(Clauses, Safety)
    End Function

#End Region

#Region " PUBLIC "

    ' Get the linked database table name.
    ' If not overrided the view table name is the same of the linked table name.
    Public Overridable Function GetViewName() As String
        Return Me.GetTableName()
    End Function

    ' Return an ordered dataview
    Public Function GetView(Clauses As DbSqlBuilder.Clauses) As DataView
        ' Get the source
        Dim Source As DataTable = Me.GetSource(Clauses)
        ' Get the view
        Dim View As DataView = Source.DefaultView

        ' Improove the sorting
        If mOrdersColumns.Count > 0 Then
            View.Sort = Me.Join(Me.mOrdersColumns)
        End If

        ' Return the view
        Return View
    End Function

    Public Function GetView(Value As Long) As DataView
        Return Me.GetView(Me.ToClauses(Value))
    End Function

    Public Function GetView() As DataView
        Return Me.GetView(New DbSqlBuilder.Clauses())
    End Function

#End Region

End Class
