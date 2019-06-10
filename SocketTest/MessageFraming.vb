' Refs
' <https://eli.thegreenplace.net/2011/08/02/length-prefix-framing-for-protocol-buffers>
' <https://www.codeproject.com/Articles/37496/TCP-IP-Protocol-Design-Message-Framing>

Imports Newtonsoft.Json

Public Class SeeminglyDosAttackException : Inherits Exception
    Public Sub New()
        MyBase.New()
    End Sub
    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class

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

#Region "Serial Classes"
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
#End Region

Public Class MessageFraming
    Event ReceivedMessage(kind As MessageKind, content As Byte())
    Event ReceivedCipher(cipher As Byte())
    Event ReceivedPlaintext(text As String)
    Event ReceivedPlaintextSignal(signal As MessagePlaintextSignal)
    Event ReceivedEncryptedSessionKey(encryptedSessionKey As Byte())

    Public Sub New()
        _inMsgBytesQueue = New Queue(Of Byte())
        _EnableDosWarning = True
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
        Dim newStream As Byte()
        newStream = lenPrefix.Concat(buffer).ToArray()

        Return newStream
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

#Region "Receiving"
    Private _oldStream As Byte()
    Private _inMsgBytesQueue As Queue(Of Byte())
    Private _EnableDosWarning As Boolean

    ' Involving Events raising
    Public Sub DecodeMsgFrame(newStream As Byte())  ' TODO: review the code structure
        ' concat the old buffer and the new one
        If _oldStream Is Nothing Then
            _oldStream = newStream
        Else
            If newStream IsNot Nothing Then
                _oldStream = _oldStream.Concat(newStream).ToArray()
            End If
        End If

        ' read the 4-bit prefix int32
        If _oldStream Is Nothing Or _oldStream.Length < 4 Then
            Exit Sub
        Else
            ' read the length of the remaining message
            Dim length As Int32 = BitConverter.ToInt32(_oldStream, 0)

            ' handle the seemingly DOS attack
            DosPrevent(length)

            ' split as many messages to the queue as possible
            While _oldStream IsNot Nothing And _oldStream.Length >= length + 4
                '
                _EnableDosWarning = True

                ' discard the 4-bit prefix at the head of the stream
                ' to get a single message from the old stream
                Dim streamMsgBody As Byte() = _oldStream.Skip(4).Take(length).ToArray()

                _inMsgBytesQueue.Enqueue(streamMsgBody)
                _oldStream = _oldStream.Skip(4 + length).Take(_oldStream.Length - length - 4).ToArray()

                ' if the existing stream here is not complete
                If _oldStream Is Nothing Or _oldStream.Length < 4 Then
                    Exit While
                End If

                ' update the `length` info from the remaining stream
                length = BitConverter.ToInt32(_oldStream, 0)

                ' handle the seemingly DOS attack
                DosPrevent(length)
            End While
        End If

        ' further handle each message in the queue
        DecodeMessageBytes()
    End Sub

    Private Sub DosPrevent(sizeOfMessage As Integer)
        If _EnableDosWarning And sizeOfMessage > 1024 * 1024 * 10 Then
            Dim exceptionMsg As String = String.Format("The length of the incoming message is {0} MB.", (CType(sizeOfMessage, Single) / 1024 / 1024).ToString("F"))
            If MessageBox.Show(exceptionMsg & " Do you want to continue receiving the message (file)? If not, the connection will be terminated to prevent a possible DOS attack.", "Warning", MessageBoxButton.YesNo) = MessageBoxResult.No Then
                Throw New SeeminglyDosAttackException(exceptionMsg)
            Else
                _EnableDosWarning = False
            End If
        End If
    End Sub
#End Region

#Region "Static Sending"
    ' NOTE: TRIM THE INPUT BYTES BEFORE CALLING
    ' Send file or text
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
        ' Decode all bytes received in the queue
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
