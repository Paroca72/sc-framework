'*************************************************************************************************
' 
' [SCFramework]
' ZipHelper
' di Samuele Carassai
'
' Routines per zippare files o directories
' Versione 2.5.3
'
'*************************************************************************************************
'
' // DIPENDENZE //
'
'   Classi: 
'       SCFramework.Bridge
'
'
'*************************************************************************************************


Imports System.IO
Imports ICSharpCode.SharpZipLib.Zip

Public Class ZipHelper

#Region " CREATE "

    Public Shared Function Compress(Value As String) As String
        If String.IsNullOrEmpty(Value) Then
            Return String.Empty
        Else
            Dim Buffer As Byte() = System.Text.Encoding.UTF8.GetBytes(Value)
            Dim RawDataStream As MemoryStream = New MemoryStream()

            Dim GZipOut As System.IO.Compression.GZipStream = New System.IO.Compression.GZipStream(RawDataStream, IO.Compression.CompressionMode.Compress)
            GZipOut.Write(Buffer, 0, Buffer.Length)
            GZipOut.Flush()
            GZipOut.Close()

            Return Convert.ToBase64String(RawDataStream.ToArray())
        End If
    End Function

    Public Shared Function CreateZipFile(ByVal zipFileStoragePath As String, ByVal relativeBy As String, ByVal zipFileName As String, ByVal fileToZip As FileInfo) As Boolean
        Return CreateZipFile(zipFileStoragePath, _
                             relativeBy, _
                             zipFileName, _
                             fileToZip)
    End Function

    Public Shared Function CreateZipFile(ByVal zipFileStoragePath As String, ByVal relativeBy As String, ByVal zipFileName As String, ByVal directoryToZip As DirectoryInfo) As Boolean
        Dim Array() As FileSystemInfo = {directoryToZip}
        Return CreateZipFile(zipFileStoragePath, _
                             relativeBy, _
                             zipFileName, _
                             Array)
    End Function

    Public Shared Function CreateZipFile(ByVal zipFileStoragePath As String, ByVal relativeBy As String, ByVal zipFileName As String, ByVal fileSystemInfoToZip As FileSystemInfo) As Boolean
        Dim Array() As FileSystemInfo = {fileSystemInfoToZip}
        Return CreateZipFile(zipFileStoragePath, _
                             relativeBy, _
                             zipFileName, _
                             Array)
    End Function

    Public Shared Function CreateZipFile(ByVal zipFileStoragePath As String, ByVal relativeBy As String, ByVal zipFileName As String, ByVal fileSystemInfosToZip() As FileSystemInfo) As Boolean
        ' a bool variable that says whether or not the file was created
        Dim isCreated As Boolean = False

        Try
            ' create our zip file
            Dim z As ZipFile = ZipFile.Create(zipFileStoragePath & "\" & zipFileName)
            ' initialize the file so that it can accept updates
            z.BeginUpdate()
            ' get all the files and directory to zip
            GetFilesToZip(fileSystemInfosToZip, z, relativeBy)
            ' commit the update once we are done
            z.CommitUpdate()
            ' close the file
            z.Close()
            ' success!
            isCreated = True

        Catch ex As Exception
            ' failed
            isCreated = False
            ' lets throw our error
            Throw ex

        End Try

        ' return the creation status
        Return isCreated
    End Function

    Private Shared Function RelativizePath(ByVal path As String, ByVal toRemove As String, ByVal directory As Boolean) As String
        If directory AndAlso Not path.EndsWith("\") Then
            path &= "\"
        End If

        If toRemove IsNot Nothing AndAlso path.StartsWith(toRemove) Then
            Return Replace(path, toRemove, String.Empty, , 1, CompareMethod.Binary)
        Else
            Return path
        End If
    End Function

    Private Shared Sub GetFilesToZip(ByVal fileSystemInfosToZip() As FileSystemInfo, ByVal z As ZipFile, ByVal relativeBy As String)
        ' check whether the objects are null
        If Not fileSystemInfosToZip Is Nothing And Not z Is Nothing Then
            ' iterate thru all the filesystem info objects
            For Each fi As FileSystemInfo In fileSystemInfosToZip
                ' check if it is a directory
                If TypeOf fi Is DirectoryInfo Then
                    Dim di As DirectoryInfo = fi
                    ' Relativize
                    Dim diName As String = RelativizePath(di.FullName, relativeBy, True)
                    ' add the directory
                    z.AddDirectory(diName)
                    ' drill thru the directory to get all
                    ' the files and folders inside it.
                    GetFilesToZip(di.GetFileSystemInfos(), z, relativeBy)
                Else
                    ' Relativize
                    Dim fiName As String = RelativizePath(fi.FullName, relativeBy, False)
                    ' add it
                    z.Add(fi.FullName, fiName)
                End If
            Next
        End If
    End Sub

#End Region

#Region " RESTORE "

    Public Shared Function Uncompress(Value As String) As String
        Dim Buffer As Byte() = Convert.FromBase64String(Value)
        Dim RawDataStream As MemoryStream = New MemoryStream(Buffer)

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

    Public Shared Sub CheckForDirectory(ByVal dirName As String)
        If Not Directory.Exists(dirName) Then
            Directory.CreateDirectory(dirName)
        End If
    End Sub

    Public Shared Function ExtractFilesFromZip(ByVal zipFilename As String, ByVal destFolder As String, ByVal Overwrite As Boolean, ByVal deleteDir As Boolean) As Boolean
        Try
            If Directory.Exists(destFolder) And deleteDir Then
                Directory.Delete(destFolder, True)
            End If

            If (Not destFolder.EndsWith("\")) Then
                destFolder = destFolder & "\"
            End If

            Dim stream As FileStream = New FileStream(zipFilename, FileMode.Open)
            Dim zipStream As ZipInputStream = New ZipInputStream(stream)

            Try
                Dim zipEntry As ZipEntry
                Dim buff As Byte() = New Byte(65535) {}

                zipEntry = zipStream.GetNextEntry()
                Do While Not (zipEntry) Is Nothing
                    If zipEntry.IsDirectory Then
                        CheckForDirectory(destFolder & zipEntry.Name.TrimStart("/"c))
                        Do While zipStream.Read(buff, 0, buff.Length) > 0
                        Loop
                    Else
                        If (Not File.Exists(destFolder & zipEntry.Name.TrimStart("/"c))) OrElse Overwrite Then
                            CheckForDirectory(Path.GetDirectoryName(destFolder & zipEntry.Name))
                            SaveFile(zipStream, destFolder & zipEntry.Name, zipEntry.Size)
                        End If
                    End If

                    zipEntry = zipStream.GetNextEntry()
                Loop
            Finally
                zipStream.Close()
            End Try

            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Shared Sub SaveFile(ByVal stream As Stream, ByVal fullName As String, ByVal uncompressedSize As Long)
        If (Not stream.CanRead) OrElse stream.Length <= 0 Then
            Return
        End If
        Dim fs As FileStream = Nothing
        Try
            Dim buff As Byte() = New Byte(65535) {}
            fs = New FileStream(fullName, FileMode.Create)
            Dim res As Integer
            If uncompressedSize > 0 Then

                res = stream.Read(buff, 0, buff.Length)

                Do While (res) > 0
                    fs.Write(buff, 0, res)
                    res = stream.Read(buff, 0, buff.Length)
                Loop
            End If
        Finally
            If Not fs Is Nothing Then
                fs.Close()
            End If
        End Try
    End Sub

#End Region

End Class
