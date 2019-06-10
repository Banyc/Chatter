' Refs
' <https://eli.thegreenplace.net/2011/08/02/length-prefix-framing-for-protocol-buffers>
' <https://www.codeproject.com/Articles/37496/TCP-IP-Protocol-Design-Message-Framing>

Imports Newtonsoft.Json

' Serializer

' key-value types:
' ```
' enum Kind
'     cipher
'     plaintext
'     image
' content
' ```

Public Enum MessageKind
    PlaintextSignal  ' .Content encoded by UTF8
    Cipher  ' .Content encoded by AesApi from object `As UTF8.GetBytes(AesContentPackage)`
    Plaintext  ' .Content encoded by UTF8
    EncryptedSessionKey
End Enum

Public Enum MessagePlaintextSignal
    Standby
End Enum

Friend MustInherit Class MessagePackage
    Public Property Kind As MessageKind
    'Public MustOverride Property Content
End Class

Friend Class CipherMessagePackage
    Inherits MessagePackage
    Public Property Content As Byte()  ' NOTE: `As UTF8.GetBytes(AesContentPackage)`
    Public Sub New()
        Kind = MessageKind.Cipher
    End Sub
End Class
Friend Class PlaintextMessagePackage
    Inherits MessagePackage
    Public Property Content As String
    Public Sub New()
        Kind = MessageKind.Plaintext
    End Sub
End Class
Friend Class PlaintextSignalMessagePackage
    Inherits MessagePackage
    Public Property Content As MessagePlaintextSignal
    Public Sub New()
        Kind = MessageKind.PlaintextSignal
    End Sub
End Class

Friend Class EncryptedSessionKeyMessagePackage
    Inherits MessagePackage
    Public Property Content As Byte()
    Sub New()
        Kind = MessageKind.EncryptedSessionKey
    End Sub
End Class


Public Class MessageFraming
    Event ReceivedMessage(kind As MessageKind, content As Byte())
    Event ReceivedCipher(cipher As Byte())
    Event ReceivedPlaintext(text As String)
    Event ReceivedPlaintextSignal(signal As MessagePlaintextSignal)
    Event ReceivedEncryptedSessionKey(encryptedSessionKey As Byte())

    Public Sub New()
        _inMsgBytesQueue = New Queue(Of Byte())
    End Sub

#Region "Static Sending"
    ' Add a length prefix ahead of the message
    Private Shared Function GetMsgFrame(serializedStr As String) As Byte()
        Dim length As Int32
        Dim buffer As Byte()

        ' converts the message to a byte array
        buffer = System.Text.Encoding.UTF8.GetBytes(serializedStr)

        ' sends the length of the byte array followed by the byte array itself
        length = buffer.Length

        ' receive 4 bytes and assuming they represent a 32-bit integer in big-endian order, decode them to get the length
        Dim lenPrefix As Byte()
        lenPrefix = BitConverter.GetBytes(length)

        ' prepend the 4-bit int32 to the buffer
        Dim newBuffer As Byte()
        newBuffer = lenPrefix.Concat(buffer).ToArray()

        Return newBuffer
    End Function
    Private Shared Function GetMsgFrame(msgPack As MessagePackage) As Byte()
        ' Set identifier for json - ref - <https://christianarg.wordpress.com/2012/11/06/serializing-and-deserializing-inherited-types-with-json-anything-you-want/>
        Dim jsonSettings = New JsonSerializerSettings()
        jsonSettings.TypeNameHandling = TypeNameHandling.Objects

        ' Serialize into JSON
        Dim jsonMsg As String = JsonConvert.SerializeObject(msgPack, jsonSettings)
        ' Send the message with frame
        Return GetMsgFrame(jsonMsg)
    End Function
#End Region

#Region "Receivng"
    Private _oldBuffer As Byte()
    Private _inMsgBytesQueue As Queue(Of Byte())
    Public Sub DecodeMsgFrame(newBuffer As Byte())  ' TODO: review the code structure
        If _oldBuffer Is Nothing Then
            _oldBuffer = newBuffer
        Else
            _oldBuffer = _oldBuffer.Concat(newBuffer).ToArray()
        End If
        If _oldBuffer.Length < 4 Then
            Exit Sub
        Else
            Dim length As Int32 = BitConverter.ToInt32(_oldBuffer, 0)

            While Not _oldBuffer Is Nothing And _oldBuffer.Length >= length + 4
                Dim realBuffer As Byte() = _oldBuffer.Skip(4).Take(length).ToArray()
                _inMsgBytesQueue.Enqueue(realBuffer)
                _oldBuffer = _oldBuffer.Skip(4 + length).Take(_oldBuffer.Length - length - 4).ToArray()

                If _oldBuffer Is Nothing Or _oldBuffer.Length < 4 Then
                    Exit While
                Else
                    length = BitConverter.ToInt32(_oldBuffer, 0)
                End If
            End While
        End If

        DecodeMessageBytes()
    End Sub
#End Region

#Region "Static Sending"
    ' NOTE: TRIM THE INPUT BYTES BEFORE CALLING
    Public Shared Function SendCipher(cipher As Byte()) As Byte()
        Dim msgPack = New CipherMessagePackage()
        msgPack.Content = cipher

        Return GetMsgFrame(msgPack)
    End Function

    Public Shared Function SendPlaintext(text As String) As Byte()
        Dim msgPack = New PlaintextMessagePackage()
        msgPack.Content = text

        Return GetMsgFrame(msgPack)
    End Function

    Public Shared Function SendStandby() As Byte()
        Dim msgPack = New PlaintextSignalMessagePackage()
        msgPack.Content = MessagePlaintextSignal.Standby

        Return GetMsgFrame(msgPack)
    End Function

    ' NOTE: TRIM THE INPUT BYTES BEFORE CALLING
    Public Shared Function SendEncryptedSessionKey(encryptedSessionKey As Byte()) As Byte()
        Dim msgPack = New EncryptedSessionKeyMessagePackage()
        msgPack.Content = encryptedSessionKey

        Return GetMsgFrame(msgPack)
    End Function
#End Region

#Region "Receive"
    Private Sub DecodeMessageBytes()
        While (_inMsgBytesQueue.Any())
            Dim jsonStr As String
            jsonStr = System.Text.Encoding.UTF8.GetString(_inMsgBytesQueue.Dequeue())

            ' Set identifier for json
            Dim jsonSettings = New JsonSerializerSettings()
            jsonSettings.TypeNameHandling = TypeNameHandling.Objects

            Dim msgPack As MessagePackage = Newtonsoft.Json.JsonConvert.DeserializeObject(Of MessagePackage)(jsonStr, jsonSettings)
            Select Case msgPack.Kind
                Case MessageKind.Cipher
                    Dim cipherPack As CipherMessagePackage = msgPack
                    RaiseEvent ReceivedCipher(cipherPack.Content)
                Case MessageKind.Plaintext
                    Dim plaintextPack As PlaintextMessagePackage = msgPack
                    RaiseEvent ReceivedPlaintext(plaintextPack.Content)
                Case MessageKind.PlaintextSignal
                    Dim plaintextSignalPack As PlaintextSignalMessagePackage = msgPack
                    RaiseEvent ReceivedPlaintextSignal(plaintextSignalPack.Content)
                Case MessageKind.EncryptedSessionKey
                    Dim encryptedSessionKeyPack As EncryptedSessionKeyMessagePackage = msgPack
                    RaiseEvent ReceivedEncryptedSessionKey(encryptedSessionKeyPack.Content)
            End Select
        End While
    End Sub


#End Region
End Class
