'*************************************************************************************************
' 
' [SCFramework]
' LogFile
' by Samuele Carassai
'
' Helper class to manage a log file
' Version 5.0.0
' Created 10/08/2015
' Updated 02/11/2015
'
'*************************************************************************************************


Public Class LogFile

    ' Define the internat variables
    Private mFilePath As String = Nothing
    Private mContent As List(Of String) = Nothing

    Private mRaiseException As Boolean = True
    Private mMaxLines As Integer = 512


    ' Constructor
    Sub New(ByVal FilePath As String)
        ' Store values
        Me.mFilePath = FilePath
        ' Init the content collection
        Me.mContent = New List(Of String)

        ' Read the content
        Me.Read()
    End Sub


#Region " PRIVATE "

    ' Check the file for the lines limit.
    ' If over the limit reduce the lines number.
    Private Sub CheckLinesLimit()
        ' Check the limit
        If Me.mContent IsNot Nothing AndAlso Me.mContent.Count > Me.mMaxLines Then
            ' Delete the last lines that exceed the max
            Me.mContent.RemoveRange(Me.MaxLines, Me.mContent.Count - Me.MaxLines)
        End If
    End Sub

    ' Read the log content
    Private Sub Read()
        Try
            ' Reset the content
            Me.mContent.Clear()
            ' Read the file content
            Me.mContent.AddRange(IO.File.ReadAllLines(Me.mFilePath))

            ' Check the limits
            Me.CheckLinesLimit()

        Catch ex As Exception
            ' Check if must raise an exception
            If Me.mRaiseException Then
                Throw ex
            End If
        End Try
    End Sub

#End Region

#Region " PUBLIC "

    ' Append a new formatted message to the file
    Public Sub Write(Message As String)
        Try
            ' Format the message and append to the first position of content collection
            Dim Formatted As String = String.Format("{0} -> {1}", Date.Now.ToString("D"), Message)
            Me.mContent.Insert(0, Formatted)

            ' Check the limits
            Me.CheckLinesLimit()

            ' Write on file
            IO.File.WriteAllLines(Me.mFilePath, Me.mContent)

        Catch ex As Exception
            ' Check if must raise an exception
            If Me.mRaiseException Then
                Throw ex
            End If
        End Try
    End Sub

    ' Clear the log file content
    Public Sub Clear()
        Try
            ' Write on file an empty content
            IO.File.WriteAllText(Me.mFilePath, String.Empty)

        Catch ex As Exception
            ' Check if must raise an exception
            If Me.mRaiseException Then
                Throw ex
            End If
        End Try
    End Sub

#End Region

#Region " PROPERTIES "

    ' The text content
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

    ' Determine if in error case raise an exception
    Public Property RaiseException() As Boolean
        Get
            Return Me.mRaiseException
        End Get
        Set(ByVal Value As Boolean)
            Me.mRaiseException = Value
        End Set
    End Property

    ' The max lines number limit
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

#End Region

End Class
