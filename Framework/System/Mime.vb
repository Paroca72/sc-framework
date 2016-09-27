'*************************************************************************************************
' 
' [SCFramework]
' ManageFiles
' di Samuele Carassai
'
' Class for manage the mime type (new from the version 5.x)
' Versione 5.0.0
'
'*************************************************************************************************

Public Class Mime

    '----------------------------------------------------------------------------------
    ' STATIC PRIVATE METHODS

    ' Mime type structure holder
    Private Shared TypeList As List(Of KeyValuePair(Of String, String))

    ' Create the mime type structure
    Private Shared Function CreateTheMimeListStructure() As List(Of KeyValuePair(Of String, String))
        Try
            ' Define the structure holder
            Dim [Structure] As List(Of KeyValuePair(Of String, String)) = New List(Of KeyValuePair(Of String, String))
            ' Get the mime file references
            Dim Text As String = My.Resources.mime
            ' Split in all rows
            Dim Rows() As String = Text.Split(vbCrLf)

            ' Cycle all rows
            For Each Row As String In Rows
                ' Get the key value pair
                Dim Key As String = Row.Split(vbTab)(0)
                Dim Value As String = Row.Split(vbTab)(1)

                ' Check the pair
                If Not String.IsNullOrEmpty(Key) And Not String.IsNullOrEmpty(Value) Then
                    ' Save the pair
                    [Structure].Add(New KeyValuePair(Of String, String)(Key.Trim, Value.Trim))
                End If
            Next

            ' Return
            Return [Structure]

        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ' Get the mime type structure
    Private Shared Function GetTheMimeTypeStructure() As List(Of KeyValuePair(Of String, String))
        ' Check if the structure is already create
        If Mime.TypeList Is Nothing Then
            ' Create the structure
            Mime.TypeList = Mime.CreateTheMimeListStructure()
        End If
        ' Return the structure
        Return Mime.TypeList
    End Function


    '----------------------------------------------------------------------------------
    ' STATIC PUBLIC METHODS

    ' Get the mime type by file extension
    Public Shared Function GetMimeByFileExtension(Extension As String) As String
        ' Fix the extension
        If Not Extension.StartsWith(".") Then
            Extension = "." & Extension
        End If

        ' Cycle the mime structure
        For Each Pair As KeyValuePair(Of String, String) In Mime.TypeList
            ' Check the pair
            If Pair.Key.Equals(Extension) Then
                ' If equal return the value
                Return Pair.Value
            End If
        Next
        ' If not found
        Return Nothing
    End Function

    ' Get the mime type by file extension
    Public Shared Function GetFileExtensionByMime(MimeType As String, Optional RemoveDot As Boolean = False) As String
        ' Cycle the mime structure
        For Each Pair As KeyValuePair(Of String, String) In Mime.TypeList
            ' Check the pair
            If Pair.Value.Equals(MimeType) Then
                ' Checking for remove dot
                If RemoveDot Then
                    ' RemoveDot 
                    Return Pair.Key.Remove(0, 1)
                Else
                    Return Pair.Key
                End If
            End If
        Next
        ' If not found
        Return Nothing
    End Function

    ' Get the mime representing bitmap
    Public Shared Function GetRepresentingBitmap(MimeType As String) As Bitmap
        Try
            ' Build the resource name
            Dim ResourceName As String = String.Format("filetype_{0}", Mime.GetFileExtensionByMime(MimeType))
            ' Get the image by manager
            Return My.Resources.ResourceManager.GetObject(ResourceName)

        Catch ex As Exception
            ' If error
            Return Nothing
        End Try
    End Function

End Class
