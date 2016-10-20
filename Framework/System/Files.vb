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
    Inherits DataSourceHelper

    ' The expire period (hours)
    Private Const DELETE_TEMPORARY_FILES_AFTER = 4

    ' Define a static thread and his timer
    Private Shared CleanThread As Thread = Nothing
    Private CleanDelay As TimeSpan = New TimeSpan(0, 15, 0)


#Region " CONSTRUCTOR "

    Public Sub New()
        ' Start the thread only if not exists
        If Files.CleanThread Is Nothing Then
            ' Create the thread and start it with low priority
            Files.CleanThread = New Thread(AddressOf Me.ThreadProc)
            Files.CleanThread.Priority = ThreadPriority.BelowNormal
            Files.CleanThread.Start()
        End If
    End Sub

#End Region

#Region " STATIC "

    ' Static instance holder
    Private Shared mInstance As Files = Nothing

    ' Instance property
    Public Shared ReadOnly Property Instance As Files
        Get
            ' Check if null
            If Files.mInstance Is Nothing Then
                Files.mInstance = New Files()
            End If

            ' Return the static instance
            Return Files.mInstance
        End Get
    End Property

#End Region

#Region " MUST OVERRIDES "

    ' Define the linked databse table name
    Public Overrides Function GetTableName() As String
        Return "SYS_FILES"
    End Function

#End Region

#Region " PRIVATE "

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
        ' Check for null values
        If Not IsDBNull(Row!PATH) Then
            ' Delete it
            Me.DeletePhisically(Row!PATH)
        End If
    End Sub

    Private Sub DeletePhisically(Rows() As DataRow)
        ' Cycle all rows
        For Each Row As DataRow In Rows
            ' Delete
            Me.DeletePhisically(Row)
        Next
    End Sub

    ' Chekc the fields
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

#End Region

#Region " CLEANER "

    ' Clean temporary files
    Private Sub CleanTemporaryFiles()
        ' Create the clause
        Dim Clauses As SCFramework.DbClauses = New SCFramework.DbClauses()
        Clauses.Add("INSERT_DATE",
                    SCFramework.DbClauses.ComparerType.MinorOrEqual,
                    Date.Now.AddHours(-SCFramework.Files.DELETE_TEMPORARY_FILES_AFTER),
                    True)

        ' Delete from the datasource
        Dim DeletedRows() As DataRow = MyBase.Delete(Clauses)

        ' If have deleted something update the datasource and delete the file fisically
        If DeletedRows.Count > 0 Then
            Me.DeletePhisically(DeletedRows)
        End If
    End Sub

    ' Create the thread procedure
    Private Sub ThreadProc()
        ' Check the status
        While Files.CleanThread.ThreadState <> Threading.ThreadState.Stopped
            Try
                ' Clean the temporary files
                Me.CleanTemporaryFiles()

            Catch ex As ThreadAbortException
                Thread.ResetAbort()

            Catch ex As Exception
                ' Write the log
                SCFramework.Configuration.Instance.SystemLogs.Write(ex.Message)

            Finally
                ' Sleep
                Thread.Sleep(Me.CleanDelay)

            End Try
        End While
    End Sub

#End Region

#Region " PUBLIC "

    ' Insert a file into database and return the new ID.
    ' This procedure access directly to the database so for massive inserts is not performing.
    Public Shadows Function DbInsert(Path As String, Name As String, IsTemporary As Boolean) As Long
        ' Check the path
        Me.CheckPath(Path)

        ' Create the values list to insert.
        Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
        Values.Add("PATH", Path)
        Values.Add("NAME", IIf(SCFramework.Utils.String.IsEmptyOrWhite(Name), Name, IO.Path.GetFileName(Path)))
        Values.Add("INSERT_DATE", IIf(IsTemporary, Date.Now, Date.MinValue))

        ' Insert the new record using the base method
        Return MyBase.DbInsert(Values)
    End Function

    Public Shadows Function DbInsert(Path As String, IsTemporary As Boolean) As Long
        Return Me.DbInsert(Path, String.Empty, IsTemporary)
    End Function

    ' Massive insert
    Public Shadows Sub Insert(Files() As KeyValuePair(Of String, String), IsTemporary As Boolean)
        ' Check all files path
        For Each File As KeyValuePair(Of String, String) In Files
            Me.CheckPath(File.Key)
        Next

        ' Cycle all the files for insert it inside the data source
        For Each File As KeyValuePair(Of String, String) In Files
            ' Get the infos
            Dim Path As String = File.Key
            Dim Name As String = File.Value

            ' Create the values list to insert
            Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
            Values.Add("PATH", Path)
            Values.Add("NAME", IIf(SCFramework.Utils.String.IsEmptyOrWhite(Name), Name, IO.Path.GetFileName(Path)))
            Values.Add("INSERT_DATE", IIf(IsTemporary, Date.Now, Date.MinValue))

            ' Insert calling the base method
            MyBase.Insert(Values)
        Next
    End Sub

    ' Single insert
    Public Shadows Sub Insert(Path As String, Name As String, IsTemporary As Boolean)
        ' Create the pairs and call the insert
        Dim Pairs() As KeyValuePair(Of String, String) = {New KeyValuePair(Of String, String)(Path, Name)}
        Me.Insert(Pairs, IsTemporary)
    End Sub

    ' Delete by a generic one numeric primary key
    Public Shadows Function Delete(File As Long) As DataRow
        ' Delete from datasource and return the deleted row if exists
        Dim Rows() As DataRow = MyBase.Delete(Me.ToClauses(File))
        Return Rows.FirstOrDefault()
    End Function

    ' Change the file status.
    ' Return true if had at least one updated succeed.
    Public Shadows Function Update(File As Long, IsTemporary As Boolean) As DataRow
        ' Create the values list to insert
        Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
        Values.Add("INSERT_DATE", IIf(IsTemporary, Date.Now, Date.MinValue))

        ' Insert the new record using the base method and return the updated if exists
        Dim Rows() As DataRow = MyBase.Update(Values, Me.ToClauses(File))
        Return Rows.FirstOrDefault()
    End Function

    ' Accept the datasource changes
    Public Overrides Function AcceptChanges(Optional Query As SCFramework.DbQuery = Nothing) As Boolean
        ' Cycle all the datasource for delete files phisivally
        Dim Deleted As DataTable = Me.Source.GetChanges(DataRowState.Deleted)
        For Each Row As DataRow In Deleted.Rows
            Me.DeletePhisically(Row)
        Next

        ' Call the base method
        Return MyBase.AcceptChanges(Query)
    End Function

    ' Add column language to the table
    Public Sub AddLanguageColumn(LanguageCode As String, Optional Query As SCFramework.DbQuery = Nothing)
        ' Check if the column already exists
        If Not SCFramework.Utils.String.IsEmptyOrWhite(LanguageCode) And
            Not Me.WritableColumns.Contains(LanguageCode) Then
            ' Check for the query manager object
            If Query Is Nothing Then Query = Me.Query

            ' Alter the table and add to the writable columns
            Query.Exec(String.Format("ALTER TABLE [{0}] ADD [{1}] NTEXT", Me.GetTableName(), LanguageCode))
            Me.WritableColumns.Add(LanguageCode)
            ' TODO: reload the datasource
        End If
    End Sub

    ' Add column language to the table
    Public Sub DropLanguageColumn(LanguageCode As String, Optional Query As SCFramework.DbQuery = Nothing)
        ' Check if the column exists
        If Not SCFramework.Utils.String.IsEmptyOrWhite(LanguageCode) And
            Me.WritableColumns.Contains(LanguageCode) Then
            ' Check for the query manager object
            If Query Is Nothing Then Query = Me.Query

            ' Alter the table and add to the writable columns
            Query.Exec(String.Format("ALTER TABLE [{0}] DROP COLUMN [{1}]", Me.GetTableName(), LanguageCode))
            Me.WritableColumns.Remove(LanguageCode)
            ' TODO: reload the datasource
        End If
    End Sub

#End Region

End Class

