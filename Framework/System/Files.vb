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
            Path = System.Web.Hosting.HostingEnvironment.MapPath(Path)
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

    ' Internal procedure for delete 
    Private Sub DeleteAndUpdate(Clauses As SCFramework.DbClauses)
        ' Delete from the datasource
        Dim DeletedRows() As DataRow = MyBase.Delete(Clauses)

        ' If have deleted something update the datasource and delete the file fisically
        If DeletedRows.Count > 0 Then
            Me.DeletePhisically(DeletedRows)
            Me.UpdateDataBase()
        End If
    End Sub


#End Region

#Region " CLEANER "

    ' The expire period (hours)
    Private Const DELETE_TEMPORARY_FILES_AFTER = 4

    ' Define a static thread and his timer
    Private Shared CleanThread As Thread = Nothing
    Private CleanDelay As TimeSpan = New TimeSpan(0, 15, 0)

    ' Clean temporary files
    Private Sub CleanTemporaryFiles()
        ' Create the clause
        Dim Clauses As SCFramework.DbClauses = New SCFramework.DbClauses()
        Clauses.Add("INSERT_DATE",
                    SCFramework.DbClauses.ComparerType.MinorAndEqual,
                    Date.Now.AddHours(-SCFramework.Files.DELETE_TEMPORARY_FILES_AFTER),
                    True)

        ' Delete from everywhere
        Me.DeleteAndUpdate(Clauses)
    End Sub

    ' Create the thread procedure
    Private Sub ThreadProc()
        ' Check the status
        While Files.CleanThread.ThreadState <> Global.System.Threading.ThreadState.Stopped
            Try
                ' Clean the temporary files
                Me.CleanTemporaryFiles()

            Catch ex As ThreadAbortException
                Thread.ResetAbort()

            Catch ex As Exception
                ' TODO: Write the error inside a log

            Finally
                ' Sleep
                Thread.Sleep(Me.CleanDelay)

            End Try
        End While
    End Sub

#End Region

#Region " PUBLIC "

    ' Delete by a generic one numeric primary key
    Public Shadows Function Delete(Value As Long) As Long
        ' Delete from everywhere
        Me.DeleteAndUpdate(MyBase.ToClauses(Value))
    End Function

    ' Insert a file into database and return the new ID.
    ' This procedure access directly to the database so for massive inserts is not performing.
    Public Shadows Function Insert(Path As String, Name As String, IsTemporary As Boolean) As Long
        ' Check for virtual path
        If Not Path.StartsWith("~/") Then
            Throw New Exception("The file path must be in virtual format.")
        End If

        ' Check if file exists
        Dim PhisicalPath As String = System.Web.Hosting.HostingEnvironment.MapPath(Path)
        If Not IO.File.Exists(Path) Then
            Throw New Exception("File not exists.")
        End If

        ' Create the values list to insert.
        ' NOTE: The primary key must stay for last as the value Is returned by the databse inserting as identity.
        ' In this way we don't need to reload the datasource as the data source will be update using the base method. 
        Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
        Values.Add("PATH", Path)
        Values.Add("NAME", IIf(SCFramework.Utils.String.IsEmptyOrWhite(Name), Name, IO.Path.GetFileName(Path)))
        Values.Add("INSERT_DATE", IIf(IsTemporary, Date.Now, Date.MinValue))
        Values.Add("ID_FILE", Me.DbInsert(Values))

        ' Insert the new record using the base method
        MyBase.Insert(Values)

        ' Return the new file ID
        Return Values("ID_FILE")
    End Function

    Public Shadows Function Insert(Path As String, IsTemporary As Boolean) As Long
        Return Me.Insert(Path, String.Empty, IsTemporary)
    End Function

    ' Massive insert
    Public Shadows Sub Insert(Files() As KeyValuePair(Of String, String), IsTemporary As Boolean)
        ' Cycle all the files for insert it inside the data source
        For Each Pair As KeyValuePair(Of String, String) In Files
            ' Get the infos
            Dim Path As String = Pair.Key
            Dim Name As String = Pair.Value

            ' Create the values list to insert
            Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
            Values.Add("PATH", Path)
            Values.Add("NAME", IIf(SCFramework.Utils.String.IsEmptyOrWhite(Name), Name, IO.Path.GetFileName(Path)))
            Values.Add("INSERT_DATE", IIf(IsTemporary, Date.Now, Date.MinValue))

            ' Insert calling the base method
            MyBase.Insert(Values)
        Next

        ' Update the database
        Me.UpdateDataBase()
    End Sub

    ' Change the file status.
    ' Return true if had at least one updated succeed.
    Public Shadows Function Update(File As Long, IsTemporary As Boolean) As Boolean
        ' Create the values list to insert
        Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
        Values.Add("INSERT_DATE", IIf(IsTemporary, Date.Now, Date.MinValue))

        ' Insert the new record using the base method
        Dim Clauses As SCFramework.DbClauses = New SCFramework.DbClauses("ID_FILE", File)
        Update = MyBase.Update(Values, Clauses).Count > 0

        ' Update the database
        Me.UpdateDataBase()
    End Function

#End Region

End Class

