'*************************************************************************************************
' 
' [SCFramework]
' ManageImages
' di Samuele Carassai
'
' Classe di gestione e conversione delle immagini
' Versione 2.6.0
'
'*************************************************************************************************
'
' // DIPENDENZE //
'
'   Classi: 
'       SCFramework.Query
'
'
'*************************************************************************************************


Imports System.Runtime.Serialization.Formatters.Binary

Public Class ManageImages
    ' Qualità JPG
    Private Const JpgQuality As Integer = 90

    ' Enum
    Public Enum ResizeQuality
        [Default] = 0
        HighSpeed = 1
        HighQuality = 2
    End Enum


    ' Get mime
    Public Shared Function GetMimeType(ByVal [Image] As Global.System.Drawing.Image) As String
        For Each codec As ImageCodecInfo In ImageCodecInfo.GetImageDecoders()
            If codec.FormatID = [Image].RawFormat.Guid Then
                Return codec.MimeType
            End If
        Next

        Return "image/unknown"
    End Function

    Public Shared Function GetMimeType(ByVal [Bitmap] As Global.System.Drawing.Bitmap) As String
        If [Bitmap] IsNot Nothing Then
            Select Case [Bitmap].RawFormat.Guid
                Case Global.System.Drawing.Imaging.ImageFormat.Bmp.Guid : Return "image/bmp"
                Case Global.System.Drawing.Imaging.ImageFormat.Emf.Guid : Return "image/emf"
                Case Global.System.Drawing.Imaging.ImageFormat.Exif.Guid : Return "image/exif"
                Case Global.System.Drawing.Imaging.ImageFormat.Gif.Guid : Return "image/gif"
                Case Global.System.Drawing.Imaging.ImageFormat.Icon.Guid : Return "image/icon"
                Case Global.System.Drawing.Imaging.ImageFormat.Jpeg.Guid : Return "image/jpeg"
                Case Global.System.Drawing.Imaging.ImageFormat.MemoryBmp.Guid : Return "image/membmp"
                Case Global.System.Drawing.Imaging.ImageFormat.Png.Guid : Return "image/png"
                Case Global.System.Drawing.Imaging.ImageFormat.Tiff.Guid : Return "image/tiff"
                Case Global.System.Drawing.Imaging.ImageFormat.Wmf.Guid : Return "image/wmf"
            End Select
        End If

        Return "image/unknown"
    End Function


    ' Trova l'encoder
    Private Shared Function GetEncoderInfo(ByVal MimeType As String) As ImageCodecInfo
        Dim encoders() As ImageCodecInfo = ImageCodecInfo.GetImageEncoders()
        For Index As Integer = 0 To encoders.Length - 1
            If encoders(Index).MimeType = MimeType Then Return encoders(Index)
        Next
        Return encoders(0)
    End Function


    ' Image Format 
    Private Shared Function GetFormat(Mime As String) As ImageFormat
        Select Case Mime
            Case "image/bmp" : Return ImageFormat.Bmp
            Case "image/emf" : Return ImageFormat.Emf
            Case "image/exif" : Return ImageFormat.Exif
            Case "image/gif" : Return ImageFormat.Gif
            Case "image/icon" : Return ImageFormat.Icon
            Case "image/jpeg" : Return ImageFormat.Jpeg
            Case "image/membmp" : Return ImageFormat.MemoryBmp
            Case "image/png" : Return ImageFormat.Png
            Case "image/tiff" : Return ImageFormat.Tiff
            Case "image/wmf" : Return ImageFormat.Wmf
        End Select

        Return Nothing
    End Function


    ' Save
    Public Shared Sub SaveBitmap2Disk(ByVal BMP As Bitmap, ByVal FileName As String, Mime As String)
        If IO.File.Exists(FileName) Then
            IO.File.Delete(FileName)
        End If

        Mime = IIf(Mime Is Nothing, ManageImages.GetMimeType(BMP), Mime)
        BMP.Save(FileName, GetFormat(Mime))
    End Sub


    ' Conversioni 
    Public Shared Function ConvertBitmap2Array(ByVal BMP As Bitmap, Mime As String) As Byte()
        Dim Stream As IO.MemoryStream = New IO.MemoryStream
        Try
            Mime = IIf(Mime Is Nothing, ManageImages.GetMimeType(BMP), Mime)
            BMP.Save(Stream, GetFormat(Mime))
            Return Stream.ToArray.Clone
        Finally
            Stream.Flush()
            Stream.Close()
        End Try
    End Function

    Public Shared Function ConvertUploadImageToArray(ByVal Upload As HtmlInputFile, _
                                                     Optional ByVal Width As Integer = 0, _
                                                     Optional ByVal Height As Integer = 0) As Byte()
        Return ConvertUploadImageToArray(Upload.PostedFile, Width, Height)
    End Function

    Public Shared Function ConvertUploadImageToArray(ByVal Upload As HttpPostedFile, _
                                                     Optional ByVal Width As Integer = 0, _
                                                     Optional ByVal Height As Integer = 0) As Byte()
        Dim BMP As Bitmap = New Bitmap(Upload.InputStream)
        Dim Mime As String = Upload.ContentType

        Width = IIf(Width = 0, BMP.Width, Width)
        Height = IIf(Height = 0, BMP.Height, Height)

        Stretch(BMP, Width, Height)
        Return ConvertBitmap2Array(BMP, Mime)
    End Function


    ' Grafica
    Public Shared Function CreateTextImage(ByVal Text As String, ByVal FontName As String, ByVal BrushColor As Brush, ByVal Width As Integer) As Bitmap
        Dim Bmp As Bitmap = New Bitmap(Width, Width)
        Dim g As Graphics = Graphics.FromImage(Bmp)
        Dim CurrentSize As Integer = 1
        Dim BaseFont As Font

        While True
            BaseFont = New Font(New FontFamily(FontName), CurrentSize)
            Dim CurrentWidth As Integer = g.MeasureString(Text, BaseFont).Width

            If CurrentWidth > Width Then
                CurrentSize -= 1
                Exit While
            Else
                CurrentSize += 1
            End If
        End While

        BaseFont = New Font(New FontFamily(FontName), CurrentSize)
        Dim Height As Integer = g.MeasureString(Text, BaseFont).Height

        g.Dispose()
        g = Nothing

        Bmp.Dispose()
        Bmp = Nothing

        Bmp = New Bitmap(Width, Width)
        g = Graphics.FromImage(Bmp)
        g.DrawString(Text, BaseFont, BrushColor, 0, 0)

        Return Bmp
    End Function

    Public Shared Function Rotate(ByVal Bitmap As Drawing.Bitmap, ByVal Angle As Single) As Drawing.Bitmap
        Dim Width As Integer = Bitmap.Width
        Dim Height As Integer = Bitmap.Height

        Dim TempImgage As New Drawing.Bitmap(Width, Height, Bitmap.PixelFormat)
        Dim G As Drawing.Graphics = Drawing.Graphics.FromImage(TempImgage)
        G.DrawImageUnscaled(Bitmap, 1, 1)
        G.Dispose()

        Dim Path As New Drawing.Drawing2D.GraphicsPath()
        Path.AddRectangle(New Drawing.RectangleF(0.0F, 0.0F, Width, Height))

        Dim Matrix As New Drawing.Drawing2D.Matrix()
        Matrix.Rotate(-Angle)

        Dim Rectangle As Drawing.RectangleF = Path.GetBounds(Matrix)
        Dim NewImage As New Drawing.Bitmap(Convert.ToInt32(Rectangle.Width), Convert.ToInt32(Rectangle.Height), Bitmap.PixelFormat)
        G = Drawing.Graphics.FromImage(NewImage)
        G.TranslateTransform(-Rectangle.X, -Rectangle.Y)
        G.RotateTransform(-Angle)
        G.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBilinear
        G.DrawImageUnscaled(TempImgage, 0, 0)
        G.Dispose()

        TempImgage.Dispose()
        Return NewImage
    End Function

    Private Shared Sub SetGraphicsQuality(ByVal g As Graphics, ByVal Mode As ResizeQuality)
        Select Case Mode
            Case ResizeQuality.HighQuality
                g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
                g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality
                g.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
                g.CompositingQuality = Drawing2D.CompositingQuality.HighQuality

            Case ResizeQuality.HighSpeed
                g.InterpolationMode = Drawing2D.InterpolationMode.Bicubic
                g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighSpeed
                g.SmoothingMode = Drawing2D.SmoothingMode.HighSpeed
                g.CompositingQuality = Drawing2D.CompositingQuality.HighSpeed

            Case ResizeQuality.Default

        End Select
    End Sub

    Public Shared Sub Stretch(ByRef Src As Bitmap, ByVal Width As Integer, ByVal Height As Integer, Optional ByVal Mode As ResizeQuality = ResizeQuality.Default)
        If Not IsNothing(Src) Then
            Dim DstWidth As Int16 = Src.Width
            Dim DstHeight As Int16 = Src.Height

            If Width <= 0 Then Width = Src.Width
            If Height <= 0 Then Height = Src.Height

            If Src.Width <> Width Or Src.Height <> Height Then
                If Not (Src.Width < Width And Src.Height < Height) Then
                    Dim IncrX As Single = Src.Width / Width
                    DstWidth = Src.Width / IncrX
                    DstHeight = Src.Height / IncrX

                    If DstHeight > Height Then
                        Dim IncrY As Single = DstHeight / Height
                        DstWidth = DstWidth / IncrY
                        DstHeight = DstHeight / IncrY
                    End If
                End If

                Dim Dst As Bitmap = New Bitmap(DstWidth, DstHeight, Src.PixelFormat)
                Dim g As Graphics = Graphics.FromImage(Dst)
                Dim RectIni As Rectangle = New Rectangle(0, 0, Src.Width, Src.Height)
                Dim RectFin As Rectangle = New Rectangle(0, 0, DstWidth, DstHeight)

                ManageImages.SetGraphicsQuality(g, Mode)
                g.DrawImage(Src, RectFin, RectIni, GraphicsUnit.Pixel)
                g.Dispose()

                Src.Dispose()
                Src = Dst
            End If
        End If
    End Sub

    Public Shared Sub StretchAndCrop(ByRef Src As Bitmap, ByVal Width As Integer, ByVal Height As Integer, _
                                      Mode As ResizeQuality, CenterHor As Boolean, CenterVer As Boolean, Enlarge As Boolean)
        If Not IsNothing(Src) Then
            Dim sourceWidth As Integer = Src.Width
            Dim sourceHeight As Integer = Src.Height

            Dim nPercent As Double = 0
            Dim nPercentW As Double = 0
            Dim nPercentH As Double = 0

            nPercentW = (Width / sourceWidth)
            nPercentH = (Height / sourceHeight)

            If nPercentH < nPercentW Then
                nPercent = nPercentW
            Else
                nPercent = nPercentH
            End If

            If Not Enlarge Then
                If nPercent > 1 Or nPercent < 0 Then
                    nPercent = 1
                End If
            End If

            Dim destWidth As Integer = (sourceWidth * nPercent)
            Dim destHeight As Integer = (sourceHeight * nPercent)

            Dim Bmp As Bitmap = New Bitmap(Width, Height, Src.PixelFormat)
            Bmp.SetResolution(Src.HorizontalResolution, Src.VerticalResolution)

            Dim g As Graphics = Graphics.FromImage(Bmp)
            ManageImages.SetGraphicsQuality(g, Mode)

            Dim DestRect As Rectangle = New Rectangle(0, 0, destWidth, destHeight)
            Dim SrcRect As Rectangle = New Rectangle(0, 0, sourceWidth, sourceHeight)

            If CenterHor Then
                Dim DiffW As Integer = (Width - destWidth) \ 2
                DestRect.Offset(DiffW, 0)
            End If

            If CenterVer Then
                Dim DiffH As Integer = (Height - destHeight) \ 2
                DestRect.Offset(0, DiffH)
            End If

            g.DrawImage(Src, DestRect, SrcRect, GraphicsUnit.Pixel)
            g.Dispose()

            Src.Dispose()
            Src = Bmp
        End If
    End Sub


    ' Check
    Public Enum CheckMode As Integer
        Min = 0
        Max = 1
        Exact = 2
    End Enum

    Public Shared Function CheckForDimension([Image] As Global.System.Drawing.Image, Width As Integer, Height As Integer, Mode As ManageImages.CheckMode) As Boolean
        ' Check Width
        If Width < 0 Then Width = 0
        If Width > 0 Then
            Select Case Mode
                Case CheckMode.Exact : If Image.Width <> Width Then Return False
                Case CheckMode.Max : If Image.Width <= Width Then Return False
                Case CheckMode.Min : If Image.Width >= Width Then Return False
            End Select
        End If

        ' Check height
        If Height < 0 Then Height = 0
        If Height > 0 Then
            Select Case Mode
                Case CheckMode.Exact : If Image.Height <> Height Then Return False
                Case CheckMode.Max : If Image.Height <= Height Then Return False
                Case CheckMode.Min : If Image.Height >= Height Then Return False
            End Select
        End If

        Return True
    End Function

    Public Shared Function CheckForDimension(ImagePath As String, Width As Integer, Height As Integer, Mode As ManageImages.CheckMode) As Boolean
        Dim [Image] As Global.System.Drawing.Image = Global.System.Drawing.Image.FromFile(ImagePath)
        Return ManageImages.CheckForDimension([Image], Width, Height, Mode)
    End Function


    ' Extra column
    ' TODO
    'Public Shared Sub AddExtraInfosColumn(Source As DataTable, Type As ManageFiles.ExtraInfoType, Optional NewColumn As String = Nothing, Optional ImageColumn As String = Nothing)
    '    If String.IsNullOrEmpty(ImageColumn) Then ImageColumn = "ID_IMAGE"
    '    ManageFiles.AddExtraInfosColumn(Source, Type, NewColumn, ImageColumn)
    'End Sub

End Class

