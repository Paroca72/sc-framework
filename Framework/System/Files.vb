'*************************************************************************************************
' 
' [SCFramework]
' Files management
' by Samuele Carassai
'
' Files management
' Version 5.0.0
' Created 02/11/2015
' Updated 11/10/2016
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

    ' Delete the file from the phisical drive
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

    Private Sub DeletePhisically(Source As DataTable)
        ' Check for empty values
        If Source IsNot Nothing Then
            ' Cycle all rows
            For Each Row As DataRow In Source.Rows
                ' Delete
                Me.DeletePhisically(Row)
            Next
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

        ' Delete from the datalist
        MyBase.Delete(Clauses)
        Me.UpdateDataBase()

        ' Get the list of files to delete phisically
        Me.DeletePhisically(Me.Filter(Clauses))
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
        ' Create the clause
        Dim Clauses As SCFramework.DbClauses = MyBase.ToClauses(Value)

        ' To delete
        Dim ToDelete As DataTable = Me.Filter(Clauses)
        Me.DeletePhisically(ToDelete)

        ' Call the base method and update the database
        Return MyBase.Delete(Clauses)
    End Function

    ' Insert a file into database
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

        ' Create the values list to insert
        Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
        Values.Add("PATH", Path)
        Values.Add("NAME", IIf(SCFramework.Utils.String.IsEmptyOrWhite(Name), Name, IO.Path.GetFileName(Path)))
        Values.Add("INSERT_DATE", IIf(IsTemporary, Date.Now, Date.MinValue))

        ' Insert the new record using the base method
        Return MyBase.Insert(Values)
    End Function

    Public Shadows Function Insert(Path As String, IsTemporary As Boolean) As Long
        Return Me.Insert(Path, String.Empty, IsTemporary)
    End Function

    ' Change the file status
    Public Shadows Function Update(File As Long, IsTemporary As Boolean) As Long
        ' Create the values list to insert
        Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
        Values.Add("INSERT_DATE", IIf(IsTemporary, Date.Now, Date.MinValue))

        ' Insert the new record using the base method
        Return MyBase.Update(Values, New SCFramework.DbClauses("ID_FILE", File))
    End Function

#End Region

End Class

