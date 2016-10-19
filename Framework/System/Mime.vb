'*************************************************************************************************
' 
' [SCFramework]
' Mime
' di Samuele Carassai
'
' Class for manage the mime type (new from the version 5.x)
' Version 5.0.0
' Created --/--/----
' Updated 19/10/2016
'
'*************************************************************************************************


Public Class Mime

#Region " PRIVATE "

    ' Mime type structure holder
    Private Shared mTypeList As List(Of KeyValuePair(Of String, String))

    ' Create the mime type structure
    Private Shared Function CreateMimeListStructure() As List(Of KeyValuePair(Of String, String))
        Try
            Return (From Row As String In My.Resources.mime.Split(vbCrLf)
                    Where Row.Contains(vbTab)
                    Let Key As String = Row.Split(vbTab)(0).Trim, Value As String = Row.Split(vbTab)(1).Trim
                    Select New KeyValuePair(Of String, String)(Key, Value)).ToList

        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ' Get the mime type structure
    Private Shared Function GetMimeTypeStructure() As List(Of KeyValuePair(Of String, String))
        ' Check if the structure is already create
        If Mime.mTypeList Is Nothing Then
            ' Create the structure
            Mime.mTypeList = Mime.CreateMimeListStructure()
        End If
        ' Return the structure
        Return Mime.mTypeList
    End Function

#End Region

#Region " PUBLIC "

    ' Get the mime type by file extension
    Public Shared Function GetMimeType(Extension As String) As String
        Try
            ' Fix the extension and check for empty values
            If Not Extension.StartsWith(".") And
            Mime.GetMimeTypeStructure() IsNot Nothing Then Extension = "." & Extension

            ' Searching for the pair value
            Return (From Pair As KeyValuePair(Of String, String) In Mime.GetMimeTypeStructure()
                    Where Pair.Key.Equals(Extension) Select Pair.Value).FirstOrDefault

        Catch ex As Exception
            ' In error case return nothing
            Return Nothing
        End Try
    End Function

    ' Get the mime type from a image object
    Public Shared Function GetMimeType(ByVal [Image] As Drawing.Image) As String
        ' Get the mime and return it
        Dim Mime As String = (From Codec In Drawing.Imaging.ImageCodecInfo.GetImageDecoders()
                              Where Codec.FormatID = [Image].RawFormat.Guid
                              Select Codec.MimeType).FirstOrDefault
        Return IIf(Mime Is Nothing, "image/unknown", Mime)
    End Function

    ' Get the mime type by file extension
    Public Shared Function GetFileExtension(MimeType As String) As String
        Try
            ' Searching for the pair value
            Return (From Pair As KeyValuePair(Of String, String) In Mime.GetMimeTypeStructure()
                    Where Pair.Value.Equals(MimeType) Select Pair.Value).FirstOrDefault

        Catch ex As Exception
            ' In error case return nothing
            Return Nothing
        End Try
    End Function

    ' Get the mime representing bitmap
    Public Shared Function GetRepresentingBitmap(MimeType As String) As Drawing.Bitmap
        Try
            ' Build the resource name and get the image by the resources manager
            Dim ResourceName As String = String.Format("filetype_{0}", Mime.GetFileExtension(MimeType))
            Return My.Resources.ResourceManager.GetObject(ResourceName)

        Catch ex As Exception
            ' If error
            Return Nothing
        End Try
    End Function

    ' Get knowned image Format from the mime type
    Public Shared Function GetFormat(MimeType As String) As Drawing.Imaging.ImageFormat
        ' Select by case
        Select Case MimeType
            Case "image/bmp" : Return Drawing.Imaging.ImageFormat.Bmp
            Case "image/emf" : Return Drawing.Imaging.ImageFormat.Emf
            Case "image/exif" : Return Drawing.Imaging.ImageFormat.Exif
            Case "image/gif" : Return Drawing.Imaging.ImageFormat.Gif
            Case "image/icon" : Return Drawing.Imaging.ImageFormat.Icon
            Case "image/jpeg" : Return Drawing.Imaging.ImageFormat.Jpeg
            Case "image/membmp" : Return Drawing.Imaging.ImageFormat.MemoryBmp
            Case "image/png" : Return Drawing.Imaging.ImageFormat.Png
            Case "image/tiff" : Return Drawing.Imaging.ImageFormat.Tiff
            Case "image/wmf" : Return Drawing.Imaging.ImageFormat.Wmf
        End Select
        ' Else return nothing
        Return Nothing
    End Function

#End Region

End Class
