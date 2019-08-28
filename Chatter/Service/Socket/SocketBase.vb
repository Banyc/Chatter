Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading


Public Enum SocketCS
    Client
    Server
End Enum


Public MustInherit Class SocketBase
#Region "Event Declaration"
    Public Event ReceivedText(text As String)
    Public Event Connected()
    Public Event OpppsiteStandby()
    Public Event SendedSessionKey()
    Public Event ReceivedSessionKey()
    Public Event Encrypted()
    Public Event ReceivedFeedBack(contentPackage As AesLocalPackage)
    Public Event Disconnected()
    Public Event ReceivedFile(fileBytes As Byte(), fileName As String)
    Public Event ReceivedImage(imageBytes As Byte())
#End Region

#Region "Variables Decl"
    Private _config As SocketSettingsFramework
    Public ReadOnly Property EndPointType As SocketCS

    Private _ip As IPAddress
    Private _port As Integer

    Public _IsOppositeStandby As Boolean

    Private _IsShutdown As Boolean = False
    Public Property IsShutdown As Boolean
        Get
            Return _IsShutdown
        End Get
        Private Set(value As Boolean)
            _IsShutdown = value
        End Set
    End Property

    Private _handler As Socket = Nothing

    ' ManualResetEvent instances signal completion.
    Private _connectDone As New ManualResetEvent(False)
    Private _encryptDone As New ManualResetEvent(False)
    Private _receivedStandbyMsg As New ManualResetEvent(False)
    Private _sendSessionKeyDone As New ManualResetEvent(False)
    Private _handshakeTimes As Integer

    ' Thread pointers
    Private _listenThread As Thread
    Private _checkConnectThread As Thread
    Private _encryptionStepThread As Thread

    ' checks the integrity and authenticity of the incoming message
    ' against replay attack
    Private _myMsgId As UInteger
    Private _othersMsgId As UInteger

    Private _textReceivedQueue As Queue(Of String)  ' temp storage storing readable messages
    Private _msgNotComfirmedList As Dictionary(Of Integer, AesLocalPackage)  ' stores sended messages  ' msgID : msgPackage
    'Private _msgOnScreen As List(Of String)
    Private _byteQueue As Queue(Of Byte())  ' stores incoming bytes, containing the encrypted session key and IV, which are still bytes

    Private Structure MessageType
        Public Const ENDOFSTREAM As String = "EOF"  ' identification of a plain text
        Public Const ID As String = "ID"
        Public Const Text As String = "TEXT"
        Public Const FeedBack As String = "FB"
        Public Const Standby As String = "STANDBY"  '
    End Structure

    Private _RSA As RsaApi
    Private _AES As AesApi
    Private WithEvents _messageFramer As MessageFraming
#End Region

#Region "Contructor"
    Protected Sub New(config As SocketSettingsFramework)
        Me.New(config.IP, config.Port, config.Role)
        _config = config
    End Sub
    Protected Sub New(ipStr As String, port As Integer, socketCS As SocketCS)
        Me.New(IPAddress.Parse(ipStr), port, socketCS)
    End Sub
    Protected Sub New(ip As IPAddress, port As Integer, socketCS As SocketCS)
        _ip = ip
        _port = port
        Me.EndPointType = socketCS
        _msgNotComfirmedList = New Dictionary(Of Integer, AesLocalPackage)
        _textReceivedQueue = New Queue(Of String)
        _byteQueue = New Queue(Of Byte())
        _RSA = New RsaApi()

        _handshakeTimes = 0

        Dim rnd As New Random()
        _myMsgId = CUInt(rnd.Next())
        _othersMsgId = Nothing

        _IsOppositeStandby = False

        _messageFramer = New MessageFraming()

        CheckConnectLoop()
        ListenLoop()
    End Sub
#End Region

#Region "MustOverride"
    Public MustOverride Sub BuildConnection()
#End Region

#Region "on transmission / send"
    Public Sub SendSessionKey(encryptedSessionKey As Byte())
        Dim msgBytes As Byte() = MessageFraming.SendEncryptedSessionKey(encryptedSessionKey)
        _SendBytes(_handler, msgBytes)
    End Sub

    Public Sub SendFile(fileBytes As Byte(), fileName As String, filePath As String)
        Dim contentPack = New AesFilePackage()
        contentPack.FileBytes = fileBytes
        contentPack.Name = fileName

        Dim localPack = New AesLocalFilePackage(contentPack, filePath)

        SendCipher(contentPack, localPack)
    End Sub

    ' Public entrance to send text
    Public Sub SendCipherText(plainText As String)
        Dim contentPack = New AesTextPackage()
        contentPack.Text = plainText

        Dim localPack = New AesLocalPackage(contentPack)

        SendCipher(contentPack, localPack)
    End Sub

    ' GENERAL METHOD
    ' NOTE: DOES NOT NEED TO UPDATE `.MessageID` of `aesPack`
    Public Sub SendCipher(aesPack As AesContentPackage, localPack As AesLocalPackage)
        ' NOTICE: This method must be called first
        _StoreTempPackForFeedback(localPack)

        ' Serialize Object into string
        Dim unencryptedJson As String = AesContentFraming.GetJsonString(aesPack)

        _SendCipherPackage(_handler, unencryptedJson)
    End Sub

    ' NOTICE: This method must be called before `SendCipher`
    Private Sub _StoreTempPackForFeedback(localPack As AesLocalPackage)
        ' updates msg ID
        _myMsgId = UIntIncrement(_myMsgId)

        ' update msg ID
        localPack.AesContentPack.MessageID = _myMsgId

        ' save the outing message for later examine if the opposite indeed received it
        _msgNotComfirmedList.Add(localPack.AesContentPack.MessageID, localPack)
    End Sub

    ' in plain text
    Public Sub SendStandbyMsg()
        Dim msgBytes As Byte() = MessageFraming.SendStandby()
        _SendBytes(_handler, msgBytes)
    End Sub

    ' GENERAL METHOD
    ' send AesTextPackage in JSON form
    Private Sub _SendCipherPackage(sock As Socket, unencryptedJson As String)

        If _encryptDone.WaitOne(0) Then  ' send cipher text
            Dim contentBytes As Byte()

            ' updates new IV
            Dim bIv As Byte() = _AES.GetNewIV()
            _AES.SetIV(bIv)

            ' Encrypt the data
            contentBytes = _AES.EncryptMsg(unencryptedJson)

            ' prepend IV to cipher text
            Dim packet As Byte()
            packet = bIv.Concat(contentBytes).ToArray()

            Dim msgBytes As Byte() = MessageFraming.SendCipher(packet)

            ' Send the MessagePackage in bytes form
            _SendBytes(sock, msgBytes)
        Else
            ' TODO: throw ERROR that the encrypted tunnel has not been built!!!
        End If
    End Sub

    Private Sub SendPlaintextMessage(sock As Socket, plaintext As String)
        Dim msgBytes As Byte() = MessageFraming.SendPlaintext(plaintext)
        _SendBytes(sock, msgBytes)
    End Sub

    ' send session key or encrypted message without attaching anything
    ' Basic function for all "send"s
    ' GENERAL METHOD
    Private Sub _SendBytes(sock As Socket, msgAttached As Byte())
        'If Not handler Is Nothing And handler.Connected Then
        If Not sock Is Nothing Then
            If sock.Connected Then
                Dim thread As New Thread(
                    Sub()
                        _connectDone.WaitOne()
                        ' Send the data through the socket.
                        Dim bytesSent As Integer = sock.Send(msgAttached)
                        thread.Abort()
                    End Sub)
                thread.Start()
            End If
        End If
    End Sub
#End Region

#Region "loop roots"
    ' listen if new message is received
    Private Sub ListenLoop()
        _listenThread = New Thread(
        Sub()
            While True
                _connectDone.WaitOne()  ' the handler has done the connection
                ''If IsConnect(_handler) Then
                Receive(_handler)
                'Else
                'RaiseEvent Disconnected()
                'Shutdown()
                'Exit While
                'End If
            End While
        End Sub)
        _listenThread.Start()
    End Sub

    ' check if the connection is off
    Private Sub CheckConnectLoop()
        _checkConnectThread = New Thread(
        Sub()
            While True
                _connectDone.WaitOne()  ' the handler has done the connection
                If Not IsConnect(_handler) Then
                    Thread.Sleep(1000)
                    If Not IsConnect(_handler) Then  ' double check
                        RaiseEvent Disconnected()
                        'Shutdown()
                        Exit While
                    End If
                End If
                Thread.Sleep(2000)
            End While
            _checkConnectThread.Abort()
        End Sub)
        _checkConnectThread.Start()
    End Sub
#End Region

#Region "on reception"
    Private Sub RaiseStandbyEventThread()
        ' updates UI
        Dim eventThread As New Thread(
        Sub()
            RaiseEvent OpppsiteStandby()
            eventThread.Abort()
        End Sub)
        eventThread.Start()
    End Sub

    ' Should be run in thread
    Private Sub Receive(handler As Socket)
        If Not handler Is Nothing Then
            ' Data buffer for incoming data.
            Dim bytes() As Byte = New [Byte](262144 - 1) {}  ' 256K

            ' An incoming connection needs to be processed.
            Try
                Dim bytesRec As Integer = handler.Receive(bytes)

                ' shorten the bytes array
                bytes = bytes.Take(bytesRec).ToArray()

                ' decode the incoming stream
                _messageFramer.DecodeMsgFrame(bytes)

                '#If Not DEBUG Then
            Catch ex As SocketException
                '#End If
            Catch ex As SeeminglyDosAttackException
                Shutdown()

            Catch ex As ThreadAbortException
                'MessageBox.Show(ex.ToString(), me.endpointtype.ToString())  ' TODO: comment out this
                Shutdown()
                'Exit While
#If Not DEBUG Then
            Catch ex As Exception
                MessageBox.Show(ex.ToString(), me.endpointtype.ToString())
                Shutdown()
#End If
            End Try
        End If
    End Sub

    Private Sub ReceivedCipher(bytes As Byte()) Handles _messageFramer.ReceivedCipher
        If _encryptDone.WaitOne(0) Then ' if the key exchange process is done; the message is now encrypted by AES
            ' :: decrypt message ::

            ' extract IV and cipher text
            Dim iV As Byte()
            iV = bytes.Take(_AES.GetIvSize() / 8).ToArray()
            Dim cipher As Byte()
            cipher = bytes.Skip(_AES.GetIvSize() / 8).Take(bytes.Length - _AES.GetIvSize() / 8).ToArray()

            ' updates IV
            _AES.SetIV(iV)

            ' decrypt message
            Dim decryptedContent As String
            decryptedContent = _AES.DecryptMsg(cipher)

            ' :: End Decryption ::


            ' Parse Content body
            Dim contentPack As AesContentPackage
            contentPack = AesContentFraming.GetAesContentPackage(decryptedContent)

            ' checks the integrity and authenticity of the incoming message
            If contentPack.Kind <> AesContentKind.Feedback Then
                If Not DoesMessageIntegrated(contentPack) Then
                    Exit Sub
                End If
            End If

            ' Handle each kind of content previously encrypted by AES
            Select Case contentPack.Kind
                Case AesContentKind.Text
                    ' determine package type
                    Dim textPack As AesTextPackage = contentPack

                    ' send feedback to the sender
                    SendFeedback(textPack.MessageID)

                    ' text that is waiting for being read
                    _textReceivedQueue.Enqueue(textPack.Text)

                    ' further handle the file
                    RaiseEvent ReceivedText(textPack.Text)

                Case AesContentKind.Feedback
                    ' determine package type
                    Dim feedbackPack As AesFeedbackPackage = contentPack

                    ' handle this incoming feedback
                    ReceiveFeedback(feedbackPack.MessageID)

                Case AesContentKind.File
                    ' determine package type
                    Dim fileBytesPack As AesFilePackage = contentPack

                    ' send feedback to the sender
                    SendFeedback(fileBytesPack.MessageID)

                    ' further handle the file
                    RaiseEvent ReceivedFile(fileBytesPack.FileBytes, fileBytesPack.Name)

                Case AesContentKind.Image
                    ' determine package type
                    Dim imagePack As AesImagePackage = contentPack

                    ' send feedback to the sender
                    SendFeedback(imagePack.MessageID)

                    ' further handle the file
                    RaiseEvent ReceivedImage(imagePack.ImageBytes)
            End Select

        Else ' if it is a encryted key to be exchanged; the message is now encrypted by RSA
            ' TODO: ERROR
            MessageBox.Show("Message received is encrypted while there is no key to decrypt it.")
        End If
    End Sub
    Private Sub ReceivedPlaintext(text As String) Handles _messageFramer.ReceivedPlaintext
        ' explicitly pop out a window for it is tranmitted without encrypted
        Dim messageBoxThread As New Thread(Sub()
                                               'MessageBox.Show(_textReceivedQueue.Dequeue(), "[RECEIVED] PLAIN TEXT NOT SAFE")
                                               MessageBox.Show(text, "[RECEIVED] PLAIN TEXT NOT SAFE")
                                               messageBoxThread.Abort()
                                           End Sub)
        messageBoxThread.Start()
    End Sub
    Private Sub ReceivedPlaintextSignal(signal As MessagePlaintextSignal) Handles _messageFramer.ReceivedPlaintextSignal
        Select Case signal
            Case MessagePlaintextSignal.Standby
                _receivedStandbyMsg.Set()
                RaiseStandbyEventThread()
                _IsOppositeStandby = True
        End Select
    End Sub

    Private Sub ReceivedEncryptedSessionKey(encryptedSessionKey As Byte()) Handles _messageFramer.ReceivedEncryptedSessionKey
        If Not _encryptDone.WaitOne(0) Then ' if it is a encryted key to be exchanged; the message is now encrypted by RSA

            _byteQueue.Enqueue(encryptedSessionKey)  ' stores encrypted session key and IV

            ' When the queue received encrypted session key and IV
            If _byteQueue.Count >= 2 Then
                ReceiveTunnalRequest(_byteQueue.Dequeue(), _byteQueue.Dequeue())
            End If
        Else
            ' TODO: ERROR
        End If
    End Sub
#End Region

#Region "shared details"
    ' https://stackoverflow.com/questions/722240/instantly-detect-client-disconnection-from-server-socket
    ' detects if the connection is terminated
    Private Shared Function IsConnect(handler As Socket) As Boolean
        Try
            Return Not (handler.Available = 0 And handler.Poll(1, SelectMode.SelectRead))
        Catch ex As Exception
            Return False
        End Try
    End Function

    Protected Sub ConnectDone()
        _connectDone.Set()
        Dim thread As New Thread(
        Sub()
            RaiseEvent Connected()
            thread.Abort()
        End Sub)
        thread.Start()
    End Sub

    Public Overridable Sub Shutdown()
        Shutdown(_handler)
    End Sub

    Private Sub Shutdown(handler As Socket)
        If Not IsShutdown Then
            IsShutdown = True

            If Not handler Is Nothing Then
                ' Release the socket.
                handler.Shutdown(SocketShutdown.Both)  ' it cannot be shutdown while the thread is running
                handler.Close()
                handler.Dispose()
            End If

            If Not _listenThread Is Nothing Then
                If _listenThread.IsAlive Then
                    Try
                        _listenThread.Abort()  ' the side effect is to cause error
                    Catch ex As ThreadAbortException
#If DEBUG Then
                        MessageBox.Show(ex.ToString())
#End If
                    Catch ex As Exception
                        MessageBox.Show(ex.ToString())

                    End Try
                End If
            End If

            If Not _checkConnectThread Is Nothing Then
                If _checkConnectThread.IsAlive Then
                    _checkConnectThread.Abort()  ' known side effect: dead thread cannot update state of a broken connection anymore.
                    RaiseEvent Disconnected()  ' manually update its state
                End If
            End If

#If DEBUG Then
            MessageBox.Show("Shutdowned", Me.EndPointType.ToString())
#End If
        End If
    End Sub

    Private Shared Function IncrementBytes(ByVal bVal As Byte(), ByVal increment As Integer) As Byte()
        'Dim carry As Integer = 0
        Dim i As Integer
        For i = 0 To bVal.Length - 1
            'bVal(i) = (bVal(i) + carry + increment) Mod Byte.MaxValue
            bVal(i) = (bVal(i) + increment) Mod Byte.MaxValue
            'If bVal(i) + increment + carry <= Byte.MaxValue Then
            If bVal(i) + increment <= Byte.MaxValue Then
                Exit For
                'Else
                '    carry = 1
            End If
            increment = increment / Byte.MaxValue
        Next i
        Return bVal
    End Function

    Private Shared Function DecrementBytes(ByVal bVal As Byte(), ByVal decrement As Integer) As Byte()
        'Dim borrow As Integer = 0
        Dim i As Integer
        For i = 0 To bVal.Length - 1
            'Dim tmp As Integer = (bVal(i) - decrement - borrow)
            Dim tmp As Integer = (bVal(i) - decrement)
            While tmp < Byte.MinValue
                tmp = tmp + Byte.MaxValue
            End While
            bVal(i) = tmp
            'If bVal(i) - decrement - borrow >= Byte.MinValue Then
            If bVal(i) - decrement >= Byte.MinValue Then
                Exit For
                'Else
                '    borrow = 1
            End If
            decrement = decrement / Byte.MaxValue
        Next i
        Return bVal
    End Function

    Private Shared Function UIntIncrement(uInt As UInteger)
        If uInt = UInteger.MaxValue Then
            Return UInteger.MinValue
        Else
            Return uInt + 1
        End If
    End Function

#End Region

#Region "three-way-handshake"
    ' Actively build a tunnel
    Public Sub MakeEncryptTunnel(seed As Integer)
        If _IsOppositeStandby Then
            ' start the first handshake
            _AES = New AesApi(seed)
            SendTunnalRequest()
        Else
            SendStandbyMsg()
        End If
    End Sub

    ' received encrypted session key
    ' to decrypt the session key and check it and even save it if necessary
    Private Sub ReceiveTunnalRequest(encryptedKey As Byte(), encryptedIV As Byte())
        _handshakeTimes += 1

        Dim decryptedKey As Byte() = DecrementBytes(_RSA.DecryptMsg(encryptedKey), _handshakeTimes)
        Dim decryptedIV As Byte() = DecrementBytes(_RSA.DecryptMsg(encryptedIV), _handshakeTimes)


        If _sendSessionKeyDone.WaitOne(0) Then
            If decryptedKey.SequenceEqual(_AES.GetSessionKey()) And decryptedIV.SequenceEqual(_AES.GetIV()) Then
                ' do nothing
            Else
                _handshakeTimes -= 1
                MessageBox.Show("Session key not matched!")
                Shutdown()
                Exit Sub
            End If
        Else
            _AES = New AesApi(0)
            _AES.SetSessionKey(decryptedKey)
            _AES.SetIV(decryptedIV)

            ' send later
        End If

        If _handshakeTimes >= 3 Then
            HandShakeDone()
        Else
            SendTunnalRequest()
        End If
    End Sub

    Public Sub SendTunnalRequest()
        If _RSA.HasPubKey Then
            _handshakeTimes += 1

            _sendSessionKeyDone.Set()  ' the first hand shake is still considered

            ' encrypt session key
            Dim encryptedKey As Byte() = _RSA.EncryptMsg(IncrementBytes(_AES.GetSessionKey(), _handshakeTimes))
            ' send encrypted session key
            SendSessionKey(encryptedKey)

            Dim encryptedIV As Byte() = _RSA.EncryptMsg(IncrementBytes(_AES.GetIV(), _handshakeTimes))
            SendSessionKey(encryptedIV)

        End If

        If _handshakeTimes >= 3 Then
            HandShakeDone()
            ' else wait for the opposite sending here a hand shake
        End If
    End Sub

    Private Sub HandShakeDone()
        _encryptDone.Set()
        RaiseEvent Encrypted()
    End Sub
#End Region

#Region "Feedback"
    Private Sub ReceiveFeedback(msgID As Integer)
        RaiseEvent ReceivedFeedBack(_msgNotComfirmedList(msgID))
        _msgNotComfirmedList.Remove(msgID)
    End Sub

    ' TODO
    Private Sub SendFeedback(msgID As Integer)

        Dim contentPack = New AesFeedbackPackage()
        contentPack.MessageID = msgID

        Dim json As String = AesContentFraming.GetJsonString(contentPack)

        _SendCipherPackage(_handler, json)
    End Sub

    ' checks the integrity and authenticity of the incoming message
    Private Function DoesMessageIntegrated(aesPack As AesContentPackage) As Boolean
        If _othersMsgId = Nothing Then  ' update other's message id
            _othersMsgId = aesPack.MessageID
        Else
            Dim incrementedId As UInteger
            incrementedId = UIntIncrement(_othersMsgId)
            If incrementedId <> aesPack.MessageID Then  ' SYNs (previous msgID + 1 and the incoming msgID) do NOT match
                MessageBox.Show("previous msgID + 1 and the incoming msgID do NOT match!", "WARNING")
                Return False
            End If
            _othersMsgId = incrementedId
        End If
        Return True
    End Function
#End Region

#Region "Sets&Gets"
    Public Function GetPrivateKey() As String
        Return _RSA.GetPrivateKey()
    End Function

    Public Function GetPublicKey() As String
        Return _RSA.GetMyPublicKey()
    End Function

    Public Sub SetPrivateKey(privateKey As String)
        _RSA.SetPrivateKey(privateKey)
    End Sub

    Public Sub SetPublicKey(publicKey As String)
        _RSA.SetOthersPublicKey(publicKey)
    End Sub

    Protected Function GetIp() As IPAddress
        Return _ip
    End Function

    Protected Function GetPort() As Integer
        Return _port
    End Function

    Protected Function GetHandler() As Socket
        Return _handler
    End Function

    Protected Sub SetHandler(handler As Socket)
        _handler = handler
    End Sub

    Public Function GetRemoteEndPoint() As String
        Return _handler.RemoteEndPoint.ToString()
    End Function

    Public Function GetEarlyMsg()
        If _textReceivedQueue.Count > 0 Then
            Return _textReceivedQueue.Dequeue()
        Else
            Return Nothing
        End If
    End Function

    Public Function IsOppositeStandby() As Boolean
        Return _IsOppositeStandby
    End Function
#End Region
End Class
