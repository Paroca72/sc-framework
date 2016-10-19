'*************************************************************************************************
' 
' [SCFramework]
' Crypt
' by Samuele Carassai
'
' Helper class to manage cryptography
' Version 5.0.0
' Created --/--/----
' Updated 19/10/2016
'
'*************************************************************************************************


Public Class Images
    ' Qualità JPG
    Private Const JpgQuality As Integer = 90

    ' Enum
    Public Enum QualityType
        [Default] = 0
        Speed = 1
        High = 2
    End Enum

#Region " PRIVATE "

    ' Set the graphics elaboration mode
    Private Shared Function SetGraphicsQuality(ByVal G As Drawing.Graphics, ByVal Quality As QualityType) As Drawing.Graphics
        ' Settings by case
        Select Case Quality
            Case QualityType.High
                G.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                G.PixelOffsetMode = Drawing.Drawing2D.PixelOffsetMode.HighQuality
                G.SmoothingMode = Drawing.Drawing2D.SmoothingMode.HighQuality
                G.CompositingQuality = Drawing.Drawing2D.CompositingQuality.HighQuality

            Case QualityType.Speed
                G.InterpolationMode = Drawing.Drawing2D.InterpolationMode.Bicubic
                G.PixelOffsetMode = Drawing.Drawing2D.PixelOffsetMode.HighSpeed
                G.SmoothingMode = Drawing.Drawing2D.SmoothingMode.HighSpeed
                G.CompositingQuality = Drawing.Drawing2D.CompositingQuality.HighSpeed

            Case QualityType.Default
                ' Do nothing

        End Select

        ' Return the graphics
        Return G
    End Function

    ' Get the graphics from an image and set the quality
    Private Shared Function GetGraphicsFromImage(Image As Drawing.Image, ByVal Quality As QualityType) As Drawing.Graphics
        Dim G As Drawing.Graphics = Drawing.Graphics.FromImage(Image)
        Return Images.SetGraphicsQuality(G, Quality)
    End Function

    ' Strech a rectangle
    Private Shared Function StrechRectangle(Source As Drawing.Rectangle, Destination As Drawing.Rectangle, MinScale As Boolean) As Drawing.Rectangle
        ' Find the scale
        Dim XScale As Single = Destination.Width / Source.Width
        Dim YScale As Single = Destination.Height / Source.Height

        ' Choice the scale and inflate the new rectangle
        Dim Scale As Single = IIf(MinScale, Math.Min(XScale, YScale), Math.Max(XScale, YScale))
        Dim NewRect As Drawing.Rectangle = New Drawing.Rectangle(0, 0, Source.Width * Scale, Source.Height * Scale)

        ' Center in destination and return
        NewRect.Offset((Destination.Width - Source.Width) / 2, (Destination.Height - Source.Height) / 2)
        Return NewRect
    End Function

#End Region

#Region " PUBLIC "

    ' Convert an image to an array 
    Public Shared Function ToArray(ByVal Source As Drawing.Image, Format As Drawing.Imaging.ImageFormat) As Byte()
        ' Create the memory stream
        Dim Stream As IO.MemoryStream = New IO.MemoryStream()

        Try
            ' To the stream and to array
            Source.Save(Stream, Format)
            Return Stream.ToArray().Clone()

        Finally
            ' Clean the memory
            Stream.Flush()
            Stream.Close()
        End Try
    End Function

    Public Shared Function ToArray(ByVal Source As Drawing.Image, Mime As String) As Byte()
        Return Images.ToArray(Source, SCFramework.Mime.GetFormat(Mime))
    End Function

    Public Shared Function ToArray(ByVal Source As Drawing.Image) As Byte()
        Return Images.ToArray(Source, SCFramework.Mime.GetMimeType(Source))
    End Function

    ' Rotate an image in degrees
    Public Shared Function Rotate(ByVal Source As Drawing.Image, ByVal Angle As Single,
                                  Optional ByVal Quality As QualityType = QualityType.Default) As Drawing.Bitmap
        ' Check for empty values
        If Source Is Nothing Then Return Nothing

        ' Create the matrix
        Dim Matrix As New Drawing.Drawing2D.Matrix()
        Matrix.Rotate(-Angle)

        ' Create a path
        Dim Path As New Drawing.Drawing2D.GraphicsPath()
        Path.AddRectangle(New Drawing.RectangleF(0.0F, 0.0F, Source.Width, Source.Height))

        ' Get the new image boundaires and create a new image
        Dim Rectangle As Drawing.RectangleF = Path.GetBounds(Matrix)
        Dim NewImage As New Drawing.Bitmap(Rectangle.Width, Rectangle.Height, Source.PixelFormat)

        ' Define the graphics object and apply the rotation
        Dim G As Drawing.Graphics = Images.GetGraphicsFromImage(NewImage, Quality)
        G.TranslateTransform(-Rectangle.X, -Rectangle.Y)
        G.RotateTransform(-Angle)
        G.DrawImageUnscaled(Source, 0, 0)
        G.Dispose()

        ' Return the new image
        Return NewImage
    End Function

    ' Crop the source image at the passed dimension
    Public Shared Function Crop(Source As Drawing.Image, ByVal Width As Integer, ByVal Height As Integer,
                                Optional ByVal Quality As Images.QualityType = Images.QualityType.Default) As Drawing.Image
        ' Check for empty values
        If Source Is Nothing Then Return Nothing

        ' Create the bounds
        Dim SourceBounds As Drawing.Rectangle = New Drawing.Rectangle(0, 0, Source.Width, Source.Height)
        Dim DestBounds As Drawing.Rectangle = New Drawing.Rectangle(0, 0, Width, Height)
        Dim Bounds As Drawing.Rectangle = Images.StrechRectangle(SourceBounds, DestBounds, False)

        ' Create the destination image and the graphics
        Dim Dest As Drawing.Image = New Drawing.Bitmap(Width, Height, Source.PixelFormat)
        Dim G As Drawing.Graphics = Images.GetGraphicsFromImage(Dest, Quality)

        ' Apply the image
        G.DrawImage(Source, Bounds, SourceBounds, Drawing.GraphicsUnit.Pixel)
        G.Dispose()

        ' Return the new image
        Return Dest
    End Function

    ' Strech the source image at the passed dimension
    Public Shared Function Stretch(Source As Drawing.Image, ByVal Width As Integer, ByVal Height As Integer,
                                   Optional ByVal Quality As Images.QualityType = Images.QualityType.Default) As Drawing.Image
        ' Check for empty values
        If Source Is Nothing Then Return Nothing

        ' Create the bounds
        Dim SourceBounds As Drawing.Rectangle = New Drawing.Rectangle(0, 0, Source.Width, Source.Height)
        Dim DestBounds As Drawing.Rectangle = New Drawing.Rectangle(0, 0, Width, Height)
        Dim Bounds As Drawing.Rectangle = Images.StrechRectangle(SourceBounds, DestBounds, True)

        ' Create the destination image and the graphics
        Dim Dest As Drawing.Image = New Drawing.Bitmap(Width, Height, Source.PixelFormat)
        Dim G As Drawing.Graphics = Images.GetGraphicsFromImage(Dest, Quality)

        ' Apply the image
        G.DrawImage(Source, Bounds, SourceBounds, Drawing.GraphicsUnit.Pixel)
        G.Dispose()

        ' Return the new image
        Return Dest
    End Function

    ' Fit the source image at the passed dimension
    Public Shared Function Fit(Source As Drawing.Image, ByVal Width As Integer, ByVal Height As Integer,
                               Optional ByVal Quality As Images.QualityType = Images.QualityType.Default) As Drawing.Image
        ' Check for empty values
        If Source Is Nothing Then Return Nothing

        ' Create the bounds
        Dim SourceBounds As Drawing.Rectangle = New Drawing.Rectangle(0, 0, Source.Width, Source.Height)
        Dim DestBounds As Drawing.Rectangle = New Drawing.Rectangle(0, 0, Width, Height)

        ' Create the destination image and the graphics
        Dim Dest As Drawing.Image = New Drawing.Bitmap(Width, Height, Source.PixelFormat)
        Dim G As Drawing.Graphics = Images.GetGraphicsFromImage(Dest, Quality)

        ' Apply the image
        G.DrawImage(Source, DestBounds, SourceBounds, Drawing.GraphicsUnit.Pixel)
        G.Dispose()

        ' Return the new image
        Return Dest
    End Function

#End Region

End Class

