'*************************************************************************************************
' 
' [SCFramework]
' Files management
' by Samuele Carassai
'
' Files management
' Version 5.0.0
' Created 02/11/2015
' Updated 13/10/2016
'
'*************************************************************************************************


Public Class Files
    Inherits SCFramework.DataSourceHelper


#Region " MUST OVERRIDES "

    ' Define the linked databse table name
    Public Overrides Function GetTableName() As String
        Return "SYS_FILES"
    End Function

#End Region

#Region " PRIVATE "

    ' Get the column value
    Private Function GetColumnValue(Source As DataRow, Column As String) As Object
        ' Check if exists
        If Source.Table.Columns.Contains(Column) Then
            ' Check the row state
            Select Case Source.RowState
                Case DataRowState.Deleted : Return Source(Column, DataRowVersion.Original)
                Case Else : Return Source(Column)
            End Select
        End If
        ' Return nothing
        Return Nothing
    End Function

    ' Delete the file or files from the phisical drive
    Private Sub DeletePhisically(Path As String)
        ' Check for empty values
        If Not SCFramework.Utils.String.IsEmptyOrWhite(Path) Then
            ' Get the file phisical path
            Path = Web.Hosting.HostingEnvironment.MapPath(Path)
            ' Check if file exists
            If IO.File.Exists(Path) Then
                ' Delete 
                IO.File.Delete(Path)
            End If
        End If
    End Sub

    Private Sub DeletePhisically(Row As DataRow)
        ' Get all the languages from the manager class
        Dim Languages As SCFramework.Languages = New SCFramework.Languages()

        ' Check for null values
        If Row IsNot Nothing Then
            ' Cycle all languages columns
            For Each Language As String In Languages.AllCodes
                ' Delete it
                Me.DeletePhisically(Me.GetColumnValue(Row, Language))
            Next
        End If
    End Sub

    Private Sub DeletePhisically(Source As DataTable)
        ' Check for empty values
        If Source IsNot Nothing Then
            ' Cycle all rows
            For Each Row As DataRow In Source.Rows
                Me.DeletePhisically(Row)
            Next
        End If
    End Sub

    Private Sub DeletePhisically(Files() As String)
        ' Cycle all files
        For Each File As String In Files
            ' Delete
            Me.DeletePhisically(File)
        Next
    End Sub

    ' Check the path field
    Private Sub CheckPath(Path As String)
        ' Check for virtual path
        If Not Path.StartsWith("~/") Then
            Throw New Exception("The file path must be in virtual format.")
        End If

        ' Check if file exists
        Dim PhisicalPath As String = Web.Hosting.HostingEnvironment.MapPath(Path)
        If Not IO.File.Exists(Path) Then
            Throw New Exception("File not exists.")
        End If
    End Sub

    ' Check for have at list one languages field
    Private Sub CheckPathFields(Values As IDictionary(Of String, Object))
        ' Get all the languages from the manager class
        Dim Languages As SCFramework.Languages = New SCFramework.Languages()
        Dim AtLeastOneValid As Boolean = False

        ' Cycle all paths
        For Each Language As String In Languages.AllCodes
            ' Check if contained inside the keys list
            If Values.ContainsKey(Language) AndAlso
                SCFramework.Utils.String.IsEmptyOrWhite(Values(Language)) Then
                ' Check the path and set found one valid
                Me.CheckPath(Values(Language))
                AtLeastOneValid = True
            End If
        Next

        ' Check if have at least one valid path
        If Not AtLeastOneValid Then
            Throw New Exception("The file path is mandatory.")
        End If
    End Sub

#End Region

#Region " PUBLIC "

    ' Insert command
    Public Overrides Function Insert(Values As IDictionary(Of String, Object)) As Long
        ' Check all path fields
        Me.CheckPathFields(Values)

        ' Call the base 
        Return MyBase.Insert(Values)
    End Function

    Public Overloads Function Insert(Path As String,
                                     Optional Name As String = Nothing,
                                     Optional Language As String = Nothing) As Long
        ' Fix the file name
        If Name Is Nothing Then Name = IO.Path.GetFileName(Name)

        ' Create the values
        Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
        Values.Add()
    End Function

    ' Delete command
    Public Overrides Function Delete(Clauses As DbClauses) As Long
        ' Check if memory managed
        If Not Me.IsMemoryManaged Then
            ' Delete the files phisically
            Me.DeletePhisically(Me.GetSource(Clauses))
        End If

        ' Call the base method
        MyBase.Delete(Clauses)
    End Function

    ' Accept the datasource changes
    Public Overrides Sub AcceptChanges()
        ' Only if work in memory
        If Not Me.IsMemoryManaged Then Exit Sub

        ' Cycle all the datasource for delete files phisivally
        Dim Deleted As DataTable = Me.GetSource().GetChanges(DataRowState.Deleted)
        Me.DeletePhisically(Deleted)

        ' Call the base method
        MyBase.AcceptChanges()
    End Sub

#End Region

#Region " STRUCTURE "

    ' Add column language to the table
    Public Sub AddLanguageColumn(LanguageCode As String, Optional Query As SCFramework.DbQuery = Nothing)
        ' Check if the column already exists
        If Not SCFramework.Utils.String.IsEmptyOrWhite(LanguageCode) And
            Not Me.WritableColumns.Contains(LanguageCode) Then
            ' Check for the query manager object
            If Query Is Nothing Then Query = Me.Query

            ' Alter the table and add to the writable columns because we changed the table structure
            Query.Exec(String.Format("ALTER TABLE [{0}] ADD [{1}] NVARCHAR(512)", Me.GetTableName(), LanguageCode))
            Me.WritableColumns.Add(LanguageCode)
        End If
    End Sub

    ' Add column language to the table
    Public Sub DropLanguageColumn(LanguageCode As String, Optional Query As SCFramework.DbQuery = Nothing)
        ' Check if the column exists
        If Not SCFramework.Utils.String.IsEmptyOrWhite(LanguageCode) And
            Me.WritableColumns.Contains(LanguageCode) Then
            ' Check for the query manager object
            If Query Is Nothing Then Query = Me.Query

            ' Before delete the column from the table we must delete phisically all the files
            ' stored in this column.
            Me.DeletePhisically(SCFramework.Utils.DataTable.ToArray(Me.GetSource(Nothing), LanguageCode))

            ' Alter the table and remove to the writable columns because we changed the table structure
            Query.Exec(String.Format("ALTER TABLE [{0}] DROP COLUMN [{1}]", Me.GetTableName(), LanguageCode))
            Me.WritableColumns.Remove(LanguageCode)
        End If
    End Sub

#End Region

End Class

