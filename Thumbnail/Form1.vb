Imports System.Drawing.Imaging
Imports System.IO
Imports Microsoft.WindowsAPICodePack.Shell

Public Class Form1
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim di As New DirectoryInfo("Y:\Copro\Uploads\DOCS\")
        ' Get a reference to each file in that directory.
        Dim fiArr As FileInfo() = di.GetFiles()
        ' Display the names of the files.
        Dim fri As FileInfo

        For Each fri In fiArr
            Dim ShellFile As ShellFile = ShellFile.FromFilePath(fri.FullName)
            'ShellFile.Thumbnail.FormatOption = ShellThumbnailFormatOption.ThumbnailOnly
            Dim image1 As Bitmap = ShellFile.Thumbnail.LargeBitmap

            'Dim image1 = System.Drawing.Image.FromFile(imagepath)
            Dim ratioX = CDbl(447) / image1.Width
            Dim ratioY = CDbl(447) / image1.Height
            Dim ratio = Math.Max(ratioX, ratioY)
            Dim newWidth = CInt(image1.Width * ratio)
            Dim newHeight = CInt(image1.Height * ratio)
            Dim newImage As New Bitmap(newWidth, newHeight)

            Using graphics1 = Graphics.FromImage(newImage)

                graphics1.DrawImage(image1, 0, 0, newWidth, newHeight)

            End Using
            Dim croppedImage = New Bitmap(447, 447)
            Dim CropRect As New Rectangle((newWidth - 447) / 2, (newHeight - 447) / 2, 447, 447)
            Using graphics2 = Graphics.FromImage(croppedImage)
                graphics2.DrawImage(newImage, New Rectangle(0, 0, 447, 447), CropRect, GraphicsUnit.Pixel)
                graphics2.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
                graphics2.CompositingQuality = Drawing2D.CompositingQuality.HighQuality
                graphics2.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
                newImage.Dispose()
            End Using
            'PictureBox1.Image = croppedImage
            Dim parameters As New EncoderParameters(1)
            parameters.Param(0) = New EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 65)
            croppedImage.Save("Y:\Copro\Uploads\PICTURES\PDF\" & Path.GetFileNameWithoutExtension(fri.FullName) & ".jpg", GetCodecInfo("image/jpeg"), parameters)
            ''CompressAndSaveImage(croppedImage, imagepath, 65)
            ''croppedImage.Save(imagepath)

            image1.Dispose()
            croppedImage.Dispose()
            ShellFile.Dispose()

        Next fri


    End Sub
    Private Shared Function GetCodecInfo(mimetype As String) As ImageCodecInfo
        For Each Encoder As ImageCodecInfo In ImageCodecInfo.GetImageEncoders()
            If Encoder.MimeType = mimetype Then
                Return Encoder
            End If
        Next Encoder
        Throw New ArgumentOutOfRangeException(String.Format("'{0}' not supported", mimetype))

            End Function
End Class
