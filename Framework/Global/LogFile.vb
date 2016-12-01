'*************************************************************************************************
' 
' [SCFramework]
' LogFile
' by Samuele Carassai
'
' Helper class to manage a log file.
' Essentially write and read from a log file but the writing session will be made async.
' Write every time span gap for not load over the system but this can be create some lost of
' information in case the server stop or crash before the completed cycle.
'
' Version 5.0.0
' Updated 02/11/2015
'
'*************************************************************************************************


Public Class LogFile

    ' Constants
    Private Const DEFAULT_MAXLINES As Integer = 512

    ' Define the internat variables
    Private mFilePath As String = Nothing
    Private mFileLocker As Object = Nothing

    Private mContent As List(Of String) = Nothing
    Private mHasChanges As Boolean = False
    Private mMaxLines As Integer = LogFile.DEFAULT_MAXLINES

    Private WriterThread As Thread = Nothing
    Private WriterDelay As TimeSpan = New TimeSpan(0, 0, 1)


#Region " CONSTRUCTOR "

    ' Constructor
    Sub New(ByVal FilePath As String)
        ' Init 
        Me.mFilePath = FilePath
        Me.mFileLocker = New Object()
        Me.mContent = New List(Of String)

        ' Read the content
        Me.Read()
        Me.CheckLinesLimit()

        ' Create the thread and start it with low priority
        Me.WriterThread = New Thread(AddressOf Me.ThreadProc)
        Me.WriterThread.Priority = ThreadPriority.BelowNormal
        Me.WriterThread.Start()
    End Sub

#End Region

#Region " PRIVATE "

    ' Check the file for the lines limit.
    ' If over the limit reduce the lines number.
    Private Sub CheckLinesLimit()
        ' Check the limit
        If Me.mContent IsNot Nothing AndAlso Me.mContent.Count > Me.mMaxLines Then
            ' Delete the last lines that exceed the max
            Me.mContent.RemoveRange(Me.MaxLines, Me.mContent.Count - Me.MaxLines)
            Me.mHasChanges = True
        End If
    End Sub


    ' Try to read all the log content.
    Private Sub Read()
        Try
            ' Reset and read the content
            Me.mContent.Clear()
            Me.mContent.AddRange(IO.File.ReadAllLines(Me.mFilePath))

        Catch ex As Exception
            ' Do nothing
        End Try
    End Sub


    ' The thread procedure for the asynch writing of the log history.
    ' Writing will be only if have some change to write. The file will be locked to avoid 
    ' contemporary writing.
    Private Sub ThreadProc()
        ' Check the status
        While Me.WriterThread.ThreadState <> Threading.ThreadState.Stopped
            Try
                ' Write the pending changes
                If Me.mHasChanges Then
                    ' Lock the file
                    SyncLock Me.mFileLocker
                        ' Write on file and reset the trigger
                        IO.File.WriteAllLines(Me.mFilePath, Me.mContent)
                        Me.mHasChanges = False
                    End SyncLock
                End If

            Catch ex As ThreadAbortException
                Thread.ResetAbort()

            Catch ex As Exception
                ' Do nothing

            Finally
                ' Sleep
                Thread.Sleep(Me.WriterDelay)

            End Try
        End While
    End Sub

#End Region

#Region " PUBLIC "

    ' Append a new formatted message to the file.
    ' The formatting will be by the prefix defined from the user or by default the time of writing.
    Public Sub Write(Prefix As String, Message As String)
        ' Adjust the prefix
        If Prefix IsNot Nothing Then
            Prefix = Date.Now.ToString("D", CultureInfo.InvariantCulture)
        Else
            Prefix = String.Format("[{0}][{1}]", Date.Now.ToString("D", CultureInfo.InvariantCulture), Prefix)
        End If

        ' Format the message and append to the first position of content collection
        Dim Formatted As String = String.Format("{0} -> {1}", Prefix, Message)
        Me.mContent.Insert(0, Formatted)

        ' Check the limits
        Me.CheckLinesLimit()
        Me.mHasChanges = True
    End Sub

    Public Sub Write(Message As String)
        Me.Write(Nothing, Message)
    End Sub


    ' Clear all the log history from the file.
    Public Sub Clear()
        Me.mContent.Clear()
        Me.mHasChanges = True
    End Sub

#End Region

#Region " PROPERTIES "

    ' Get the log text.
    Public ReadOnly Property Text() As String
        Get
            ' Check for empty values
            If Me.mContent Is Nothing OrElse Me.mContent.Count = 0 Then
                ' Return empty
                Return String.Empty
            Else
                ' Return the contatenation of the lines with line feed
                Return String.Join(vbCrLf, Me.mContent)
            End If
        End Get
    End Property


    ' The max lines number limit.
    ' After this limit the older line will be deleted.
    Public Property MaxLines() As Integer
        Get
            Return Me.mMaxLines
        End Get
        Set(ByVal Value As Integer)
            If Value < 1 Then
                Value = 1
            End If
            Me.mMaxLines = Value
        End Set
    End Property


    ' Get the file path.
    Public ReadOnly Property FilePath As String
        Get
            Return Me.mFilePath
        End Get
    End Property

#End Region

End Class
