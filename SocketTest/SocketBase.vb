Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading


Public Enum SocketCS
    Client
    Server
End Enum


Public MustInherit Class SocketBase
    Public Event ReceiveMsg()
    Public Event Connected()
    Public Event OpppsiteStandby()
    Public Event SendedSessionKey()
    Public Event ReceivedSessionKey()
    Public Event Encrypted()
    Public Event ReceivedFeedBack(myText As String)
    Public Event Disconnected()

    Public ReadOnly Property EndPointType As SocketCS

    Private _ip As IPAddress
    Private _port As Integer

    Public IsOppositeStandby As Boolean

    Private _IsShutdown As Boolean = False
    Public Property IsShutdown As Boolean
        Get
            Return _IsShutdown
        End Get
        Private Set(value As Boolean)
            _IsShutdown = value
        End Set
    End Property

    Private _socketCS As SocketCS

    Private _handler As Socket = Nothing

    ' ManualResetEvent instances signal completion.
    Private _connectDone As New ManualResetEvent(False)
    Private _encryptDone As New ManualResetEvent(False)
    Private _sendSessionKeyDone As New ManualResetEvent(False)
    Private _receiveSessionKeyDone As New ManualResetEvent(False)
    Private _handshakeTimes As Integer

    ' Thread pointers
    Private _listenThread As Thread
    Private _checkConnectThread As Thread
    Private _encryptionStepThread As Thread

    ' Incoming data from the client.
    Private _dataStr As String = Nothing

    ' checks the integrity and authenticity of the incoming message
    ' against replay attack
    Private _myMsgId As UInteger
    Private _othersMsgId As UInteger

    Private _msgReceivedQueue As Queue(Of String)  ' temp storage storing readable messages
    Private _msgNotComfirmedList As Dictionary(Of Integer, String)  ' stores sended messages  ' msgID : msgText
    'Private _msgOnScreen As List(Of String)
    Private _byteQueue As Queue(Of Byte())  ' stores incoming bytes, containing the encrypted seesion key and IV, which are still bytes

    Private Structure MessageType
        Public Const ENDOFSTREAM As String = "EOF"  ' identification of a plain text
        Public Const ID As String = "ID"
        Public Const Text As String = "TEXT"
        Public Const FeedBack As String = "FB"
        Public Const Standby As String = "STANDBY"  '
    End Structure

    Private _RSA As RsaApi
    Private _AES As AesApi

    Protected Sub New(ipStr As String, port As Integer, socketCS As SocketCS)
        Me.New(IPAddress.Parse(ipStr), port, socketCS)
    End Sub
    Protected Sub New(ip As IPAddress, port As Integer, socketCS As SocketCS)
        _ip = ip
        _port = port
        EndPointType = socketCS
        _msgNotComfirmedList = New Dictionary(Of Integer, String)
        _msgReceivedQueue = New Queue(Of String)
        _byteQueue = New Queue(Of Byte())
        _socketCS = socketCS
        _RSA = New RsaApi()

        _handshakeTimes = 0

        Dim rnd As New Random()
        _myMsgId = CUInt(rnd.Next())
        _othersMsgId = Nothing

        IsOppositeStandby = False

        CheckConnectLoop()
        ListenLoop()
    End Sub

    Public MustOverride Sub Start()

#Region "on transmission / send"
    '
    Public Sub SendText(plainText As String)
        _myMsgId = UIntIncrement(_myMsgId)  ' updates msg ID

        SendText(plainText, _myMsgId)
    End Sub

    Private Sub SendText(plainText As String, id As Integer)
        Dim compasser As String
        compasser = MessageTypeStart(MessageType.ID) & Str(id) & MessageTypeEnd(MessageType.ID) &
        MessageTypeStart(MessageType.Text) & plainText & MessageTypeEnd(MessageType.Text)

        _msgNotComfirmedList.Add(id, plainText)
        Send(_handler, compasser)
    End Sub

    ' in plain text
    Private Sub SendStandbyMsg()
        Send(_handler, MessageTypeBody(MessageType.Standby))
    End Sub

    ' send plain or encrypted message
    Private Sub Send(handler As Socket, msgStr As String)

        If _encryptDone.WaitOne(0) Then  ' send cipher text
            Dim msgBytes As Byte()

            ' updates new IV
            Dim bIv As Byte() = _AES.GetNewIV()
            _AES.SetIV(bIv)

            ' Encrypt the data
            msgBytes = _AES.EncryptMsg(msgStr & MessageTypeBody(MessageType.ENDOFSTREAM))

            ' prepend IV to cipher text
            Dim packet As Byte()
            packet = bIv.Concat(msgBytes).ToArray()

            ' Encrypted data cannot attach anything
            SendBytes(handler, packet)

        Else  ' send plain text without encrypting it
            ' Encode the data string into a byte array.
            'Dim msg As Byte() = Encoding.ASCII.GetBytes(msgStr & ENDOFSTREAM)
            Dim msg As Byte() = Encoding.UTF8.GetBytes(msgStr & MessageTypeBody(MessageType.ENDOFSTREAM))
            _Send(handler, msg)
        End If

    End Sub

    ' send session key or encrypted message without attaching anything
    Private Sub SendBytes(handler As Socket, msgByte As Byte())
        _Send(handler, msgByte)
    End Sub

    ' Basic function for all "send"s
    Private Sub _Send(handler As Socket, msgAttached As Byte())
        'If Not handler Is Nothing And handler.Connected Then
        If Not handler Is Nothing Then
            If handler.Connected Then
                Dim thread As New Thread(
                        Sub()
                            _connectDone.WaitOne()
                            ' Send the data through the socket.
                            Dim bytesSent As Integer = handler.Send(msgAttached)
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
    Private Sub RaiseReceivedEventThread()
        ' raise event when the decrypted message is reached
        Dim eventThread As New Thread(
            Sub()
                RaiseEvent ReceiveMsg()
                eventThread.Abort()
            End Sub)
        eventThread.Start()
    End Sub

    Private Sub RaiseStandbyEventThread()
        ' updates UI
        Dim eventThread As New Thread(
            Sub()
                RaiseEvent OpppsiteStandby()
                eventThread.Abort()
            End Sub)
        eventThread.Start()
    End Sub

    Private Sub AsyncParsePlainMsg(plainMsg As String)
        Dim thread As New Thread(Sub()
                                     Dim text As String
                                     Dim msgID As UInteger

                                     If plainMsg.IndexOf(MessageTypeStart(MessageType.Text)) >= 0 Then  ' it is a text message
                                         text = GetCertainContent(plainMsg, MessageType.Text)
                                         msgID = Int(GetCertainContent(plainMsg, MessageType.ID))

                                         ' checks the integrity and authenticity of the incoming message
                                         If _othersMsgId = Nothing Then  ' update other's message id
                                             _othersMsgId = msgID
                                         Else
                                             Dim incrementedId As UInteger
                                             incrementedId = UIntIncrement(_othersMsgId)
                                             If incrementedId <> msgID Then  ' SYNs (previous msgID + 1 and the incoming msgID) do NOT match
                                                 MessageBox.Show("previous msgID + 1 and the incoming msgID do NOT match!", "WARNING")
                                                 thread.Abort()
                                                 Exit Sub
                                             End If
                                             _othersMsgId = incrementedId
                                         End If

                                         SendFeedback(msgID)  ' send feedback to the sender

                                         _msgReceivedQueue.Enqueue(text)
                                         RaiseReceivedEventThread()  ' message waiting for being read
                                     ElseIf plainMsg.IndexOf(MessageTypeBody(MessageType.FeedBack)) >= 0 Then ' it is a feedback
                                         msgID = Int(GetCertainContent(plainMsg, MessageType.ID))
                                         ReceiveFeedback(msgID)
                                     Else
                                         ' do nothing
                                     End If


                                     thread.Abort()
                                 End Sub)
        thread.Start()
    End Sub

    Private Sub DistributeReceivedMessage(bytesRec As Integer, bytes As Byte())
        _dataStr += Encoding.UTF8.GetString(bytes, 0, bytesRec)  ' try to get plain text

        Dim cookedData As String  ' plain text without "<EOF>"

        ' Enqueue all the stuff received below
        If _encryptDone.WaitOne(0) Then ' if the encryption is done
            ' :: decrypt message ::

            ' extract IV and cipher text
            Dim iV As Byte()
            iV = bytes.Take(_AES.GetIvSize() / 8).ToArray()
            Dim cipher As Byte()
            cipher = bytes.Skip(_AES.GetIvSize() / 8).Take(bytesRec - _AES.GetIvSize() / 8).ToArray()

            ' updates IV
            _AES.SetIV(iV)

            ' decrypt message
            _dataStr = _AES.DecryptMsg(cipher)

            ' :: End Decryption ::

            ' clears out "<EOF>" attachment
            Dim lengthMsg As Integer
            lengthMsg = _dataStr.LastIndexOf(MessageTypeBody(MessageType.ENDOFSTREAM)) + 1
            cookedData = _dataStr.Substring(0, lengthMsg - 1)

            ' further parses the decrypted data
            AsyncParsePlainMsg(cookedData)

            'MessageBox.Show(cookedData, _socketCS.ToString() & " received msg")
            _dataStr = Nothing

        ElseIf _dataStr.EndsWith(MessageTypeBody(MessageType.ENDOFSTREAM)) Then ' if data was not encrypted
            cookedData = _dataStr.Substring(0, _dataStr.Length - MessageTypeBody(MessageType.ENDOFSTREAM).Length)  ' not allow plain text anymore, which will be considered as encrypted message  ' TODO: disdinguish plain text and encrypted message since plain text attaches "<EOF>"
            '_msgReceivedQueue.Enqueue(cookedData)

            'MessageBox.Show(cookedData, _socketCS.ToString() & " received msg")
            _dataStr = Nothing


            If cookedData.EndsWith(MessageTypeBody(MessageType.Standby)) Then  ' If it is a standby message
                RaiseStandbyEventThread()
                IsOppositeStandby = True
            Else ' if it is a pure plain text
                ' explicitly pop out a window for it is tranmitted without encrypted
                Dim messageBoxThread As New Thread(Sub()
                                                       'MessageBox.Show(_msgReceivedQueue.Dequeue(), "[RECEIVED] PLAIN TEXT NOT SAFE")
                                                       MessageBox.Show(cookedData, "[RECEIVED] PLAIN TEXT NOT SAFE")
                                                       messageBoxThread.Abort()
                                                   End Sub)
                messageBoxThread.Start()
            End If

        Else ' if it is a encryted key to be exchanged
            ' the size of the byte() must be the multiple of 16?
            Dim encryptedSessionKey(bytesRec - 1) As Byte

            Array.Copy(bytes, encryptedSessionKey, encryptedSessionKey.Length)
            _byteQueue.Enqueue(encryptedSessionKey)  ' stores encrypted session key and IV


            ' When the queue received encrypted session key and IV
            If _byteQueue.Count >= 2 Then
                ReceiveTunnalRequest(_byteQueue.Dequeue(), _byteQueue.Dequeue())
            End If
        End If
    End Sub

    ' Should be run in thread
    Private Sub Receive(handler As Socket)

        If Not handler Is Nothing Then
            ' Data buffer for incoming data.
            Dim bytes() As Byte = New [Byte](1024 - 1) {}
            '_dataStr = Nothing

            ' An incoming connection needs to be processed.
            'While True
            Try
                Dim bytesRec As Integer = handler.Receive(bytes)

                DistributeReceivedMessage(bytesRec, bytes)

                'Exit While
                'End If
            Catch ex As Exception
                'MessageBox.Show(ex.ToString(), _socketCS.ToString())  ' TODO: comment out this
                Shutdown()
                'Exit While
            End Try
            'End While
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
                'MessageBox.Show(String.Format("RemoteEndPoint:" & vbCrLf & "{0}", _handler.RemoteEndPoint.ToString()), _socketCS.ToString() & ", " & "Connect Done")
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

            MessageBox.Show("Shutdowned", _socketCS.ToString())
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

    Private Shared Function MessageTypeStart(state As String) As String
        Return "<" & state & ">"
    End Function

    Private Shared Function MessageTypeEnd(state As String) As String
        Return "</" & state & ">"
    End Function

    Private Shared Function MessageTypeBody(state As String) As String
        Return "<" & state & "/>"
    End Function

    ' Only matches the first occurence
    Private Shared Function GetCertainContent(receivedMessage As String, state As String) As String
        Dim startIndex As Integer
        Dim endIndex As Integer
        startIndex = receivedMessage.IndexOf(MessageTypeStart(state)) + MessageTypeStart(state).Length
        endIndex = receivedMessage.LastIndexOf(MessageTypeEnd(state))
        Return receivedMessage.Substring(startIndex, endIndex - startIndex)
    End Function


#End Region

#Region "three-way-handshake"
    ' Actively build a tunnel
    Public Sub MakeEncryptTunnel(seed As Integer)
        If IsOppositeStandby Then
            ' start the first handshake
            _AES = New AesApi(seed)
            SendTunnalRequest()
        Else
            'Send(_handler, "<STANDBY>")
            SendStandbyMsg()
        End If
    End Sub

    ' received encrypted session key
    ' to decrypt the session key and check it and even save it if necessary
    Private Sub ReceiveTunnalRequest(encryptedKey As Byte(), encryptedIV As Byte())
        _handshakeTimes += 1
        _receiveSessionKeyDone.Set()

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

    Private Sub SendTunnalRequest()
        If _RSA.HasPubKey Then
            _handshakeTimes += 1

            _sendSessionKeyDone.Set()  ' the first hand shake is still considered

            ' encrypt session key
            Dim encryptedKey As Byte() = _RSA.EncryptMsg(IncrementBytes(_AES.GetSessionKey(), _handshakeTimes))
            ' send encrypted session key
            SendBytes(_handler, encryptedKey)

            Dim encryptedIV As Byte() = _RSA.EncryptMsg(IncrementBytes(_AES.GetIV(), _handshakeTimes))
            SendBytes(_handler, encryptedIV)

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

    Private Sub SendFeedback(msgID As Integer)
        Dim compasser As String
        compasser = MessageTypeStart(MessageType.ID) & msgID & MessageTypeEnd(MessageType.ID) &
            MessageTypeBody(MessageType.FeedBack)

        Send(_handler, compasser)
    End Sub
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
        If _msgReceivedQueue.Count > 0 Then
            Return _msgReceivedQueue.Dequeue()
        Else
            Return Nothing
        End If
    End Function
#End Region
End Class
