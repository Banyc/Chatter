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
    Public Event Encrypted()
    Public Event ReceivedFeedBack(contentPackage As AesLocalPackage)
    Public Event Disconnected()
    Public Event ReceivedFile(fileBytes As Byte(), fileName As String)
    Public Event ReceivedImage(imageBytes As Byte())
#End Region

#Region "Variables Declaration"
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

    Protected _socket As Socket = Nothing

    ' Services
    Private WithEvents _keyExchange As Handshake
    Private _feedback As Feedback

    ' ManualResetEvent instances signal completion.
    Private _connectDone As New ManualResetEvent(False)
    Private _encryptDone As New ManualResetEvent(False)
    Private _receivedStandbyMsg As New ManualResetEvent(False)
    Private _sendSessionKeyDone As New ManualResetEvent(False)

    Private Structure MessageType
        Public Const ENDOFSTREAM As String = "EOF"  ' identification of a plain text
        Public Const ID As String = "ID"
        Public Const Text As String = "TEXT"
        Public Const FeedBack As String = "FB"
        Public Const Standby As String = "STANDBY"  ' signal that is ready for session key exchange
    End Structure

    Private _AES As AesApi
    Private WithEvents _messageFramer As MessageFraming
#End Region

#Region "Contructor"
    Protected Sub New(ipStr As String, port As Integer, socketCS As SocketCS)
        Me.New(IPAddress.Parse(ipStr), port, socketCS)
    End Sub
    Protected Sub New(ip As IPAddress, port As Integer, socketCS As SocketCS)
        _ip = ip
        _port = port
        Me.EndPointType = socketCS

        _feedback = New Feedback()

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
    Private Sub SendCipher(aesPack As AesContentPackage, localPack As AesLocalPackage)
        ' NOTICE: This method must be called first
        _feedback.SetMsgID_StoreMyMsg(localPack)

        ' Serialize Object into string
        Dim unencryptedJson As String = AesContentFraming.GetJsonString(aesPack)

        _SendCipherPackage(_socket, unencryptedJson)
    End Sub

    Public Sub SendStandbyMsg()
        Dim msgBytes As Byte() = MessageFraming.SendStandby()
        _SendBytes(msgBytes)
    End Sub

    ' GENERAL METHOD
    ' send AesTextPackage in JSON form
    ' involving an encryption of AES process
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
            _SendBytes(msgBytes)
        Else
            ' TODO: throw ERROR that the encrypted tunnel has not been built!!!
            MessageBox.Show("Want to encrypt a message while there is no key to encrypt it.")
        End If
    End Sub

    Private Sub SendPlaintextMessage(sock As Socket, plaintext As String)
        Dim msgBytes As Byte() = MessageFraming.SendPlaintext(plaintext)
        _SendBytes(msgBytes)
    End Sub

    ' send session key or encrypted message without attaching anything
    ' Basic function for all "send"s
    ' GENERAL METHOD
    Private Sub _SendBytes(msgAttached As Byte()) Handles _keyExchange.Send
        'If Not handler Is Nothing And handler.Connected Then
        If Not _socket Is Nothing Then
            If _socket.Connected Then
                Dim thread As New Task(
                    Sub()
                        _connectDone.WaitOne()
                        ' Send the data through the socket.
                        Dim bytesSent As Integer = _socket.Send(msgAttached)
                    End Sub)
                thread.Start()
            End If
        End If
    End Sub
#End Region

#Region "loop roots"
    ' listen if new message is received
    Private Sub ListenLoop()
        Dim listenThread = New Task(
        Sub()
            _connectDone.WaitOne()  ' the socket has done the connection
            While Not _IsShutdown
                Try
                    Receive(_socket)
                Catch ex As ObjectDisposedException
                    ' when socket is shutdown (disposed) while the loop is still running
                Catch ex As SeeminglyDosAttackException
                    Shutdown()
                Catch ex As SocketException
                    ' when socket is closed while the receiving loop is still running
#If Not DEBUG Then
                Catch ex As Exception
                    MessageBox.Show(ex.ToString(), Me.EndPointType.ToString())
                    Shutdown()
#End If
                End Try
            End While
        End Sub)
        listenThread.Start()
    End Sub

    ' check if the connection is off
    Private Sub CheckConnectLoop()
        Dim checkConnectThread = New Task(
        Sub()
            _connectDone.WaitOne()  ' the handler has done the connection
            While Not _IsShutdown
                Try
                    If Not IsConnect(_socket) Then
                        Thread.Sleep(1000)
                        If Not IsConnect(_socket) Then  ' double check
                            RaiseEvent Disconnected()
                            'Shutdown()
                            Exit While
                        End If
                    End If
                Catch ex As ObjectDisposedException
                    ' when socket is shutdown (disposed) while the loop is still running
                End Try
                Thread.Sleep(2000)
            End While
        End Sub)
        checkConnectThread.Start()
    End Sub
#End Region

#Region "on reception"
    Private Sub RaiseStandbyEventThread()
        ' updates UI
        Dim eventThread As New Task(
        Sub()
            RaiseEvent OpppsiteStandby()
        End Sub)
        eventThread.Start()
    End Sub

    ' Should be run in thread
    Private Sub Receive(handler As Socket)
        If handler IsNot Nothing Then
            ' Data buffer for incoming data.
            Dim bytes() As Byte = New [Byte](262144 - 1) {}  ' 256K

            ' An incoming connection needs to be processed.
            Dim bytesRec As Integer = handler.Receive(bytes)

            ' shorten the bytes array
            bytes = bytes.Take(bytesRec).ToArray()

            ' decode the incoming stream
            _messageFramer.DecodeMsgFrame(bytes)
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
                If Not _feedback.ReceiveNewMsg_CheckIntegrity(contentPack) Then
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

                    ' further handle the text
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
        Dim messageBoxThread As New Task(Sub()
                                             MessageBox.Show(text, "[RECEIVED] PLAIN TEXT NOT SAFE")
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
        _keyExchange.Receive(encryptedSessionKey)
    End Sub
#End Region

#Region "Key Exchange"
    ' Notice: Call this after key pair (`rsa`) is set
    Public Sub InitKeyExchange(seed As Integer, rsa As RsaApi)
        _keyExchange = New Handshake(seed, rsa)
    End Sub

    Public Sub LaunchKeyExchange()
        _keyExchange.Start()
    End Sub

    Private Sub DoneKeyExchange(aes As AesApi) Handles _keyExchange.DoneHandshake
        _AES = aes
        _encryptDone.Set()
        _keyExchange = Nothing
        RaiseEvent Encrypted()
    End Sub
#End Region

#Region "Feedback"
    Private Sub SendFeedback(msgID As Integer)

        Dim contentPack = New AesFeedbackPackage()
        contentPack.MessageID = msgID

        Dim json As String = AesContentFraming.GetJsonString(contentPack)

        _SendCipherPackage(_socket, json)
    End Sub

    Private Sub ReceiveFeedback(msgID As Integer)
        RaiseEvent ReceivedFeedBack(_feedback.ReceiveFeedback_PopMyMsg(msgID))
    End Sub
#End Region

#Region "other methods"
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
        Dim thread As New Task(
        Sub()
            RaiseEvent Connected()
        End Sub)
        thread.Start()
    End Sub

    Public Overridable Sub Shutdown()
        If Not IsShutdown Then
            IsShutdown = True

            Try
                If Not _socket Is Nothing Then
                    ' Release the socket.
                    _socket.Shutdown(SocketShutdown.Both)  ' it cannot be shutdown while the thread is running
                    _socket.Close()
                    _socket.Dispose()
                End If
            Catch ex As SocketException

            End Try

#If DEBUG Then
            MessageBox.Show("Shutdowned", Me.EndPointType.ToString())
#End If
        End If
    End Sub
#End Region

#Region "Sets&Gets"
    Protected Function GetIp() As IPAddress
        Return _ip
    End Function

    Protected Function GetPort() As Integer
        Return _port
    End Function

    Protected Function GetSocket() As Socket
        Return _socket
    End Function

    Protected Sub SetSocket(handler As Socket)
        _socket = handler
    End Sub

    Public Function GetRemoteEndPoint() As String
        Return _socket.RemoteEndPoint.ToString()
    End Function

    Public Function IsOppositeStandby() As Boolean
        Return _IsOppositeStandby
    End Function
#End Region
End Class
