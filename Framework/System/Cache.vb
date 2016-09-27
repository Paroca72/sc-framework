'*************************************************************************************************
' 
' [SCFramework]
' Cache
' di Samuele Carassai
'
' Classe di gestione della cache dei files
' Versione 1.1.1
'
'*************************************************************************************************


' Classe gestione della cache

Public Class Cache

#Region " PROPERTIES "

    Private Shared ReadOnly Property FilesList() As ArrayList
        Get
            Dim Key As String = "$SCFramework$Cache$FilesList"
            If Bridge.Session(Key) Is Nothing Then
                Bridge.Session(Key) = Cache.GetFilesList()
            End If

            Return Bridge.Session(Key)
        End Get
    End Property

#End Region

#Region " PRIVATE "

    Private Const Folder As String = "cache"

    Private Shared Function GetFilesList() As ArrayList
        Dim Path As String = IO.Path.Combine(Configuration.Instance.PublicPath, Folder)

        ' Check folder
        If Not IO.Directory.Exists(Path) Then
            IO.Directory.CreateDirectory(Path)
        End If

        ' Get list
        Dim Files() As String = IO.Directory.GetFiles(Path, "*.*")
        Dim List As ArrayList = New ArrayList()

        For Each File As String In Files
            Dim Name As String = IO.Path.GetFileName(File)
            List.Add(Name)
        Next

        Return List
    End Function

    Private Shared Function CreateCacheFileName(FilePath As String, MaxWidth As Integer, MaxHeight As Integer) As String
        Dim Directory As String = IO.Path.GetDirectoryName(FilePath)
        Dim Name As String = IO.Path.GetFileNameWithoutExtension(FilePath)
        Dim Extension As String = IO.Path.GetExtension(FilePath)

        If Directory.StartsWith("..\") Then Directory = Directory.Substring(3)
        If Directory.StartsWith("~\") Then Directory = Directory.Substring(2)

        Dim NewName As String = String.Format("{0}-{1}x{2}{3}", Name, MaxWidth, MaxHeight, Extension)
        Dim NewPath As String = IO.Path.Combine(Directory, NewName)

        Return NewPath.Replace("\", "-")
    End Function

    Private Shared Function GetPhiscalPath(Path As String) As String
        If Path.StartsWith("/") Then Path = Path.Remove(1)

        If Not Path.StartsWith("~/") Then Path = String.Format("~/{0}", Path)
        Return System.Web.Hosting.HostingEnvironment.MapPath(Path)
    End Function

    Private Shared Function CreateCachedImage(Crop As Boolean, FilePath As String, Width As Integer, Height As Integer, CenterHor As Boolean, CenterVer As Boolean) As String
        ' Name
        Dim RealPath As String = Cache.GetPhiscalPath(FilePath)
        Dim NewName As String = Cache.CreateCacheFileName(FilePath, Width, Height)

        Dim PhisicalCachePath As String = IO.Path.Combine(Configuration.Instance.PublicPath, Folder)
        Dim RelativeCachePath As String = IO.Path.Combine(Configuration.Instance.PublicFolder, Folder).Replace("\", "/")

        If Not Cache.FilesList.Contains(NewName) AndAlso IO.File.Exists(RealPath) Then
            ' Elaborate
            Dim BMP As Bitmap = New Bitmap(RealPath)
            Dim Mime As String = SCFramework.ManageImages.GetMimeType(BMP)
            Dim DestPath As String = IO.Path.Combine(PhisicalCachePath, NewName)

            Try
                If Crop Then
                    SCFramework.ManageImages.StretchAndCrop(BMP, Width, Height, ManageImages.ResizeQuality.HighQuality, CenterHor, CenterVer, False)
                Else
                    SCFramework.ManageImages.Stretch(BMP, Width, Height, ManageImages.ResizeQuality.HighQuality)
                End If
            Catch ex As Exception
            End Try
            SCFramework.ManageImages.SaveBitmap2Disk(BMP, DestPath, Mime)

            ' Hold
            Cache.FilesList.Add(NewName)
        End If

        Return "~/" & RelativeCachePath & "/" & NewName
    End Function

#End Region

#Region " PUBLIC "

    Public Shared Function AddImage(FilePath As String, Width As Integer, Height As Integer) As String
        Return Cache.CreateCachedImage(False, FilePath, Width, Height, False, False)
    End Function

    Public Shared Function AddCroppedImage(FilePath As String, Width As Integer, Height As Integer, CenterHor As Boolean, CenterVer As Boolean) As String
        Return Cache.CreateCachedImage(True, FilePath, Width, Height, CenterHor, CenterVer)
    End Function

    Public Shared Sub Clean(Optional Days As Integer = 30)
        ' Files
        Dim Path As String = IO.Path.Combine(Configuration.Instance.PublicPath, Folder)
        If IO.Directory.Exists(Path) Then
            Dim Files() As String = IO.Directory.GetFiles(Path)
            For Each FileName As String In Files
                Try
                    Dim Info As IO.FileInfo = New IO.FileInfo(FileName)
                    If Info.CreationTime < Now.AddDays(-Days) Or Days = 0 Then
                        IO.File.Delete(FileName)
                    End If

                Catch ex As Exception
                End Try
            Next
        End If
    End Sub

    Public Shared Sub Clear()
        Cache.Clean(0)
    End Sub

#End Region

End Class
