'*************************************************************************************************
' 
' [SCFramework]
' ZipHelper
' by Samuele Carassai
'
' Helper class to zip data and file
' Version 5.0.0
' Created 19/10/2016
' Updated 19/10/2016
'
' TODO: Archive folders and files
'
'*************************************************************************************************


Public Class ZipHelper

#Region " STRING MANAGEMENT "

    ' Compress a string
    Public Shared Function Compress(Value As String) As String
        If String.IsNullOrEmpty(Value) Then
            Return String.Empty
        Else
            Dim Buffer As Byte() = System.Text.Encoding.UTF8.GetBytes(Value)
            Dim RawDataStream As System.IO.MemoryStream = New System.IO.MemoryStream()

            Dim GZipOut As System.IO.Compression.GZipStream = New System.IO.Compression.GZipStream(RawDataStream, IO.Compression.CompressionMode.Compress)
            GZipOut.Write(Buffer, 0, Buffer.Length)
            GZipOut.Flush()
            GZipOut.Close()

            Return Convert.ToBase64String(RawDataStream.ToArray())
        End If
    End Function

    ' Uncompress a string
    Public Shared Function Uncompress(Value As String) As String
        Dim Buffer As Byte() = Convert.FromBase64String(Value)
        Dim RawDataStream As System.IO.MemoryStream = New System.IO.MemoryStream(Buffer)

        Dim GZipIn As System.IO.Compression.GZipStream = New System.IO.Compression.GZipStream(RawDataStream, IO.Compression.CompressionMode.Decompress)
        Dim InBuffer(1024) As Byte
        Dim Result As String = String.Empty
        Dim BytesRead As Integer = 0

        Do
            BytesRead = GZipIn.Read(InBuffer, 0, InBuffer.Length)
            Result &= System.Text.Encoding.UTF8.GetString(InBuffer, 0, BytesRead)
        Loop While BytesRead > 0

        GZipIn.Flush()
        GZipIn.Close()
        RawDataStream.Close()

        Return Result
    End Function

#End Region

End Class
