' <https://stackoverflow.com/questions/17128038/c-sharp-rsa-encryption-decryption-with-transmission>
' <https://stackoverflow.com/questions/15702718/public-key-encryption-with-rsacryptoserviceprovider>

Imports System.Security.Cryptography

Public Class RsaDecryptionException
    Inherits System.Exception
    Public Sub New()
        MyBase.New()
    End Sub
    Public Sub New(ByVal message As String)
        MyBase.New(message)
    End Sub
End Class

Public Class RsaApi

    'Private _csp As RSACryptoServiceProvider
    Private _pri As RSACryptoServiceProvider
    Private _pub As RSACryptoServiceProvider

    '' I have my own private key
    'Public ReadOnly Property HasPriKey As Boolean

    ' I have other's public key
    Public ReadOnly Property HasPubKey As Boolean

    Public Sub New()
        '_csp = Nothing
        _pri = New RSACryptoServiceProvider(2048)
        _pub = New RSACryptoServiceProvider(2048)

        'Me.HasPriKey = False
        Me.HasPubKey = False
    End Sub

#Region "key operations"
    Public Sub SetPrivateKey(RSA_PrivateKey As String)
        SetPrivateKey(Str2Param(RSA_PrivateKey))
    End Sub

    Public Sub SetOthersPublicKey(publicKey As String)
        Me._HasPubKey = True
        SetOthersPublicKey(Str2Param(publicKey))
    End Sub

    Public Function GetMyPublicKey() As String
        Return Param2Str(_pri.ExportParameters(False))
    End Function

    Public Function GetPrivateKey() As String
        Return Param2Str(_pri.ExportParameters(True))
    End Function

    Private Sub SetPrivateKey(privateKey As RSAParameters)
        _pri.ImportParameters(privateKey)
    End Sub
    Private Sub SetOthersPublicKey(publicKey As RSAParameters)
        _pub.ImportParameters(publicKey)
    End Sub
#End Region

#Region "Deals with message"
    Public Function EncryptMsg(data As Byte()) As Byte()
        Return _pub.Encrypt(data, False)
    End Function
    Public Function DecryptMsg(data As Byte()) As Byte()
        Try
            Return _pri.Decrypt(data, False)
        Catch ex As Exception
            Throw New RsaDecryptionException("Using wrong RSA key pair? Try to check the correctness of those key pair you set.")
        End Try
    End Function
#End Region

#Region "type convertion"
    ' converts string to RSAParameters
    Private Function Str2Param(str As String) As RSAParameters
        '//get a stream from the string
        Dim SR = New System.IO.StringReader(str)
        '//we need a deserializer
        Dim xs = New System.Xml.Serialization.XmlSerializer(GetType(RSAParameters))
        '//get the object back from the stream
        Dim rsaParam As RSAParameters = xs.Deserialize(SR)

        Return rsaParam
    End Function

    ' converts RSAParameters to string
    Private Function Param2Str(param As RSAParameters) As String
        '//we need some buffer
        Dim sw = New System.IO.StringWriter()
        '//we need a serializer
        Dim xs = New System.Xml.Serialization.XmlSerializer(GetType(RSAParameters))
        '//serialize the key into the stream
        xs.Serialize(sw, param)
        '//get the string from the stream
        Return sw.ToString()
    End Function
#End Region

End Class
