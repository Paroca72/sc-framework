'*************************************************************************************************
' 
' [SCFramework]
' Crypt
' by Samuele Carassai
'
' Helper class to manage cryptography.
' This class propvide a very basis method to encrypt string and image to and from Base64.
' Also provide some encription/decription methos relative at MD5 algorithm.

' Version 5.0.0
' Updated 19/10/2016
'
'*************************************************************************************************


' Class Crypt
Public Class Crypt

    ' MD5 Utilities
    Public Class MD5

        ' Compite the hash code
        Public Shared Function ComputeHash(ByVal Input As String) As String
            Dim Md5Hasher As New Cryptography.MD5CryptoServiceProvider()
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
                Dim MD5Hash As Cryptography.MD5CryptoServiceProvider = New Cryptography.MD5CryptoServiceProvider()

                Dim sPassKeyArray As Byte() = MD5Hash.ComputeHash(UTF8Encoding.UTF8.GetBytes(Key))
                Dim sOriginalArray As Byte() = Convert.FromBase64String(ToOriginal)

                MD5Hash.Clear()

                Dim tripleDesCsp As Cryptography.TripleDESCryptoServiceProvider = New Cryptography.TripleDESCryptoServiceProvider()
                tripleDesCsp.Key = sPassKeyArray
                tripleDesCsp.Mode = Cryptography.CipherMode.ECB
                tripleDesCsp.Padding = Cryptography.PaddingMode.PKCS7

                Dim cTransform As Cryptography.ICryptoTransform = tripleDesCsp.CreateDecryptor()
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
            Dim MD5Hash As Cryptography.MD5CryptoServiceProvider = New Cryptography.MD5CryptoServiceProvider()

            Dim sPassKeyArray As Byte() = MD5Hash.ComputeHash(UTF8Encoding.UTF8.GetBytes(Key))
            Dim sOriginalArray As Byte() = UTF8Encoding.UTF8.GetBytes(Original)

            MD5Hash.Clear()

            Dim tripleDesCsp As Cryptography.TripleDESCryptoServiceProvider = New Cryptography.TripleDESCryptoServiceProvider()
            tripleDesCsp.Key = sPassKeyArray
            tripleDesCsp.Mode = Cryptography.CipherMode.ECB
            tripleDesCsp.Padding = Cryptography.PaddingMode.PKCS7

            Dim cTransform As Cryptography.ICryptoTransform = tripleDesCsp.CreateEncryptor()
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
    Public Shared Function ToBase64(ByVal Image As Drawing.Bitmap, Format As Drawing.Imaging.ImageFormat) As String
        Dim Memory As New IO.MemoryStream()

        Image.Save(Memory, Format)
        Dim Base64 As String = Convert.ToBase64String(Memory.ToArray)

        Memory.Close()
        Memory = Nothing

        Return Base64
    End Function


    ' Covert a Base64 code in a string
    Public Shared Shadows Function ToString(Base64 As String) As String
        Try
            Dim Bytes() As Byte = Convert.FromBase64String(Base64)
            Return Encoding.ASCII.GetString(Bytes)

        Catch ex As Exception
            Return String.Empty
        End Try
    End Function


    ' Convert a Base64 code to a bitmap
    Public Shared Function ToBitmap(Base64 As String) As Drawing.Bitmap
        Try
            Dim Bytes() As Byte = Convert.FromBase64String(Base64)
            Dim Memory As IO.MemoryStream = New IO.MemoryStream(Bytes)
            Return New Drawing.Bitmap(Memory)

        Catch ex As Exception
            Return Nothing
        End Try
    End Function


    ' Create a random unique key of the passed length
    Public Shared Function CreateUniqueKey(KeyLength As Integer) As String
        Dim a As String = "ABCDEFGHJKLMNOPQRSTUVWXYZ234567890"
        Dim chars() As Char = New Char((a.Length) - 1) {}
        chars = a.ToCharArray
        Dim data() As Byte = New Byte((KeyLength) - 1) {}
        Dim crypto As Cryptography.RNGCryptoServiceProvider = New Cryptography.RNGCryptoServiceProvider()
        crypto.GetNonZeroBytes(data)
        Dim result As StringBuilder = New StringBuilder(KeyLength)
        For Each b As Byte In data
            result.Append(chars(b Mod (chars.Length - 1)))
        Next
        Return result.ToString()
    End Function

End Class
