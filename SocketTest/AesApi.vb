'https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes?view=netframework-4.7.2
'https://stackoverflow.com/questions/202011/encrypt-and-decrypt-a-string-in-c

Imports System.Security.Cryptography
Imports System.IO

Public Class AesApi
    Private _csp As AesCryptoServiceProvider
    Private _rnd As Random

    Public Sub New(seed As Integer)
        _rnd = New Random(seed)
        _csp = New AesCryptoServiceProvider()
        _csp.KeySize = 256
        _csp.BlockSize = 128
        _csp.GenerateIV()
        _csp.Key = GetNewSessionKey()
        _csp.Mode = CipherMode.CBC
        _csp.Padding = PaddingMode.Zeros
    End Sub

    Public Sub SetSessionKey(sKey As Byte())
        _csp.Key = sKey
    End Sub

    Public Sub SetIV(iV As Byte())
        _csp.IV = iV
    End Sub

    Private Function GetNewSessionKey() As Byte()
        Dim bKey(_csp.KeySize / 8 - 1) As Byte
        _rnd.NextBytes(bKey)
        Return bKey
    End Function

    Public Function GetSessionKey() As Byte()
        Return _csp.Key
    End Function

    Public Function GetIV() As Byte()
        Return _csp.IV
    End Function

    Public Function GetKeyBitSize() As Integer
        Return _csp.KeySize
    End Function

    Public Function EncryptMsg(plainText As Object) As Byte()
        Dim encryptor As ICryptoTransform = _csp.CreateEncryptor(_csp.Key, _csp.IV)
        Dim encrypted() As Byte

        ' Create the streams used for encryption.
        Using msEncrypt As New MemoryStream()
            Using csEncrypt As New CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)
                Using swEncrypt As New StreamWriter(csEncrypt)
                    'Write all data to the stream.
                    swEncrypt.Write(plainText)
                End Using
                encrypted = msEncrypt.ToArray()
            End Using
        End Using
        Return encrypted
    End Function

    Public Function DecryptMsg(cipherText As Byte()) As String
        Dim plaintext As String

        ' Create a decryptor to perform the stream transform.
        Dim decryptor As ICryptoTransform = _csp.CreateDecryptor(_csp.Key, _csp.IV)

        ' Create the streams used for decryption.
        Using msDecrypt As New MemoryStream(cipherText)

            Using csDecrypt As New CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)

                Using srDecrypt As New StreamReader(csDecrypt)


                    ' Read the decrypted bytes from the decrypting stream
                    ' and place them in a string.
                    plaintext = srDecrypt.ReadToEnd()
                End Using
            End Using
        End Using

        Return plaintext
    End Function

End Class
