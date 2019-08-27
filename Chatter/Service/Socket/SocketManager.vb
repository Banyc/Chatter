' Manage cooperations between socket and other classes
Public Class SocketManager
    Private WithEvents _socket As SocketBase
    Private WithEvents _chatBox As ChatBox
    'Private WithEvents _cryptoPanel As CryptoPanel
    Private WithEvents _mainWindow As MainWindow

    'Public Sub New(socket As SocketBase, mainWindow As Window, chatBox As ChatBox, cryptoPanel As CryptoPanel)
    Public Sub New(socket As SocketBase, mainWindow As Window, chatBox As ChatBox)
        _socket = socket
        _chatBox = chatBox
        _mainWindow = mainWindow
        '_cryptoPanel = cryptoPanel
    End Sub

#Region "socket events"  ' Inside which should let dispatcher invoke procedures concerning GUI  ' _socket --> _mainWindow, _chatBox
    Private Sub _socket_ReceivedText() Handles _socket.ReceiveText
        Receive(_socket)
        _mainWindow.FlashTaskbar_Invoke()
    End Sub

    Private Sub _socket_ReceivedFeedBack(localContentPack As AesLocalPackage) Handles _socket.ReceivedFeedBack
        Select Case localContentPack.AesContentPack.Kind
            Case AesContentKind.Text
                _chatBox.MyMessage(CType(localContentPack.AesContentPack, AesTextPackage).Text)
            Case AesContentKind.File
                _chatBox.NewState(ChatState.FileSent, CType(localContentPack, AesLocalFilePackage).FilePath)
                _chatBox.DisplayImageIfValid(ChatRole.ThisUser, CType(localContentPack, AesLocalFilePackage).FilePath)
        End Select
    End Sub

    Private Sub _socket_Connected() Handles _socket.Connected
        Connected(_socket)
        _mainWindow.FlashTaskbar_Invoke()
    End Sub

    Private Sub _socket_Encrypted() Handles _socket.Encrypted
        Encrypted(_socket)
    End Sub

    Private Sub _socket_Disconnected() Handles _socket.Disconnected
        Disconnected(_socket)
        _mainWindow.FlashTaskbar_Invoke()
    End Sub

    Private Sub _socket_ReceivedFile(fileBytes As Byte(), fileName As String) Handles _socket.ReceivedFile
        _chatBox.HandleReceivedFile(fileBytes, fileName)
    End Sub

#Region "functions"
    Private Sub Receive(endPoint As SocketBase)
        _chatBox.NewMessage(endPoint.GetEarlyMsg())
    End Sub

    Private Sub Connected(endPoint As SocketBase)
        _chatBox.NewState(ChatState.Connected, String.Format("RemoteEndPoint:" & vbCrLf & "{0}", endPoint.GetRemoteEndPoint()))
        _mainWindow.UpdateUI_Invoke(endPoint, ChatState.Connected)
    End Sub

    Private Sub Encrypted(endPoint As SocketBase)
        _chatBox.NewState(ChatState.Encrypted)
        _mainWindow.PrepareChatBox_Invoke()
    End Sub

    Private Sub Disconnected(endPoint As SocketBase)
        _chatBox.NewState(ChatState.Disconnected)
        _mainWindow.UpdateUI_Invoke(endPoint, ChatState.Disconnected)
    End Sub
#End Region
#End Region

#Region "SocketBase - CharBox"  ' _charBox --> _socket

#Region "char box events"
    Private Sub chatBox_SendMessage(message As String) Handles _chatBox.SendMessage
        Send(message)
    End Sub

    Private Sub chatBox_SendFile(fileBytes As Byte(), fileName As String, path As String) Handles _chatBox.SendFile
        Send(fileBytes, fileName, path)
    End Sub
#End Region
    Private Sub Send(msgStr As String)
        If Not _socket Is Nothing Then
            _socket.SendCipherText(msgStr)
        End If
    End Sub

    Private Sub Send(fileBytes As Byte(), fileName As String, path As String)
        If Not _socket Is Nothing Then
            _socket.SendFile(fileBytes, fileName, path)
        End If
    End Sub
#End Region

End Class
