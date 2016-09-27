'*************************************************************************************************
' 
' [SCFramework]
' Crypt
' by Samuele Carassai
'
' Helper class to manage cryptography
' Version 5.0.0
' Created --/--/----
' Updated 02/11/2015
'
'*************************************************************************************************


Imports System.Security.Cryptography

' Class Crypt
Public Class Crypt

    ' MD5 Utilities
    Public Class MD5

        ' Compite the hash code
        Public Shared Function ComputeHash(ByVal Input As String) As String
            Dim Md5Hasher As New MD5CryptoServiceProvider()
            Dim Data As Byte() = Md5Hasher.ComputeHash(Encoding.ASCII.GetBytes(Input))

            Dim sBuilder As New StringBuilder()
            For Index As Integer = 0 To Data.Length - 1
                sBuilder.Append(Data(Index).ToString("x2"))
            Next

            Return sBuilder.ToString()
        End Function

        ' Decrypt a MD5 string
        Public Shared Function Decrypt(ByVal ToOriginal As String, ByVal Key As String) As String
            Try
                Dim MD5Hash As MD5CryptoServiceProvider = New MD5CryptoServiceProvider()

                Dim sPassKeyArray As Byte() = MD5Hash.ComputeHash(UTF8Encoding.UTF8.GetBytes(Key))
                Dim sOriginalArray As Byte() = Convert.FromBase64String(ToOriginal)

                MD5Hash.Clear()

                Dim tripleDesCsp As TripleDESCryptoServiceProvider = New TripleDESCryptoServiceProvider()
                tripleDesCsp.Key = sPassKeyArray
                tripleDesCsp.Mode = CipherMode.ECB
                tripleDesCsp.Padding = PaddingMode.PKCS7

                Dim cTransform As ICryptoTransform = tripleDesCsp.CreateDecryptor()
                Dim resultArray As Byte() = cTransform.TransformFinalBlock(sOriginalArray, 0, sOriginalArray.Length)

                tripleDesCsp.Clear()
                Return UTF8Encoding.UTF8.GetString(resultArray)

            Catch ex As Exception
                Throw New Exception(ex.Message)
                Return Nothing
            End Try
        End Function

        ' Encrypt a MD5
        Public Shared Function Encrypt(ByVal Original As String, ByVal Key As String) As String
            Dim MD5Hash As MD5CryptoServiceProvider = New MD5CryptoServiceProvider()

            Dim sPassKeyArray As Byte() = MD5Hash.ComputeHash(UTF8Encoding.UTF8.GetBytes(Key))
            Dim sOriginalArray As Byte() = UTF8Encoding.UTF8.GetBytes(Original)

            MD5Hash.Clear()

            Dim tripleDesCsp As TripleDESCryptoServiceProvider = New TripleDESCryptoServiceProvider()
            tripleDesCsp.Key = sPassKeyArray
            tripleDesCsp.Mode = CipherMode.ECB
            tripleDesCsp.Padding = PaddingMode.PKCS7

            Dim cTransform As ICryptoTransform = tripleDesCsp.CreateEncryptor()
            Dim resultArray As Byte() = cTransform.TransformFinalBlock(sOriginalArray, 0, sOriginalArray.Length)

            tripleDesCsp.Clear()
            Return Convert.ToBase64String(resultArray, 0, resultArray.Length)
        End Function

    End Class

    ' Convert a string in Base64
    Public Shared Function ToBase64(ByVal Text As String) As String
        Try
            Dim Bytes() As Byte = Global.System.Text.Encoding.ASCII.GetBytes(Text)
            If Bytes.Length = 0 Then
                Return String.Empty
            Else
                Return Convert.ToBase64String(Bytes)
            End If
        Catch ex As Exception
            Return String.Empty
        End Try
    End Function

    ' Convert an image to Base64
    Public Shared Function ToBase64(ByVal Image As System.Drawing.Bitmap, Format As Imaging.ImageFormat) As String
        Dim Memory As New System.IO.MemoryStream()

        Image.Save(Memory, Format)
        Dim Base64 As String = System.Convert.ToBase64String(Memory.ToArray)

        Memory.Close()
        Memory = Nothing

        Return Base64
    End Function

    ' Covert a Base64 code in a string
    Public Shared Shadows Function ToString(Base64 As String) As String
        Try
            Dim Bytes() As Byte = Convert.FromBase64String(Base64)
            Return Global.System.Text.Encoding.ASCII.GetString(Bytes)

        Catch ex As Exception
            Return String.Empty
        End Try
    End Function

    ' Convert a Base64 code to a bitmap
    Public Shared Function ToBitmap(Base64 As String) As System.Drawing.Bitmap
        Try
            Dim Bytes() As Byte = Convert.FromBase64String(Base64)
            Dim Memory As System.IO.MemoryStream = New System.IO.MemoryStream(Bytes)
            Return New Bitmap(Memory)

        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ' Create a random unique key of the passed length
    Public Shared Function GetUniqueKey(KeyLength As Integer) As String
        Dim a As String = "ABCDEFGHJKLMNOPQRSTUVWXYZ234567890"
        Dim chars() As Char = New Char((a.Length) - 1) {}
        chars = a.ToCharArray
        Dim data() As Byte = New Byte((KeyLength) - 1) {}
        Dim crypto As RNGCryptoServiceProvider = New RNGCryptoServiceProvider
        crypto.GetNonZeroBytes(data)
        Dim result As StringBuilder = New StringBuilder(KeyLength)
        For Each b As Byte In data
            result.Append(chars(b Mod (chars.Length - 1)))
        Next
        Return result.ToString()
    End Function

End Class
