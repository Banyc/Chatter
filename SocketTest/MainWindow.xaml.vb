Imports System.ComponentModel

Class MainWindow
    Private WithEvents _client As SocketClient
    Private WithEvents _server As SocketListener
    Private WithEvents _socket As SocketBase

    Public Sub New()
        InitializeComponent()
        plainTextPanel.Visibility = True
        cryptoPanel.Visibility = True
        chatBox.Visibility = True
    End Sub

#Region "Events of controls"
    Private Sub btnSend_Click(sender As Object, e As RoutedEventArgs)
        If Not _socket Is Nothing Then
            _socket.SendText(txtSend.Text)
        End If
    End Sub

    Private Sub btnServerActivate_Click(sender As Button, e As RoutedEventArgs)
        DisableClientOptions()
        sender.IsEnabled = False
        DisableIpTxt()
        If _server Is Nothing Then
            If _client Is Nothing Then
                _server = New SocketListener(txtIpAddress.Text, Int(txtPort.Text), txtExpectedIpAddress.Text)
                _socket = _server  ' mark
                plainTextPanel.Visibility = False
                cryptoPanel.Visibility = False
                cryptoPanel.SetSocket(_socket)
                _server.Start()
                Me.Title = "Server"
            End If
        End If
        'PrepareChatBox()
    End Sub

    Private Sub btnClientActivate_Click(sender As Button, e As RoutedEventArgs)
        DisableServerOptions()
        sender.IsEnabled = False
        DisableIpTxt()
        If _client Is Nothing Then
            If _server Is Nothing Then
                _client = New SocketClient(txtIpAddress.Text, Int(txtPort.Text))
                _socket = _client  ' mark
                plainTextPanel.Visibility = False
                cryptoPanel.Visibility = False
                cryptoPanel.SetSocket(_socket)
                _client.Start()
                Me.Title = "Client"
            End If
        End If
        'PrepareChatBox()
    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If Not _server Is Nothing Then
            _server.Shutdown()
        End If
        If Not _client Is Nothing Then
            _client.Shutdown()
        End If
    End Sub
#End Region

#Region "socket events"
    Private Sub _socket_ReceiveMsg() Handles _socket.ReceiveMsg
        'txtServer.Text = _socket.GetEarlyMsg()
        Receive(_socket)
    End Sub

    Private Sub _socket_ReceivedFeedBack(myText As String) Handles _socket.ReceivedFeedBack
        chatBox.MyReceivedMsg(myText)
    End Sub

    Private Sub _socket_Connected() Handles _socket.Connected
        Connected(_socket)
    End Sub

    Private Sub _socket_Encrypted() Handles _socket.Encrypted
        Encrypted(_socket)
    End Sub

    Private Sub _socket_Disconnected() Handles _socket.Disconnected
        Disconnected(_socket)
    End Sub
#End Region

#Region "char box events"
    Private Sub chatBox_SendMessage(message As String) Handles chatBox.SendMessage
        Send(message)
    End Sub
#End Region

#Region "functions"
    Private Sub Receive(endPoint As SocketBase)
        'MessageBox.Show(endPoint.GetEarlyMsg(), endPoint.EndPointType.ToString() & " received msg")
        chatBox.NewMessage(endPoint.GetEarlyMsg())
    End Sub

    Private Sub Connected(endPoint As SocketBase)
        'MessageBox.Show(String.Format("RemoteEndPoint:" & vbCrLf & "{0}", endPoint.GetRemoteEndPoint()), endPoint.EndPointType.ToString() & ", Connect Done")
        chatBox.NewState(ConnectState.Connected, String.Format("RemoteEndPoint:" & vbCrLf & "{0}", endPoint.GetRemoteEndPoint()))
        Me.Dispatcher.BeginInvoke(Windows.Threading.DispatcherPriority.Normal, Sub() UpdateUI(endPoint, ConnectState.Connected))
    End Sub

    Private Sub Encrypted(endPoint As SocketBase)
        chatBox.NewState(ConnectState.Encrypted)
        Me.Dispatcher.BeginInvoke(Windows.Threading.DispatcherPriority.Normal, Sub() PrepareChatBox())
    End Sub

    Private Sub Disconnected(endPoint As SocketBase)
        chatBox.NewState(ConnectState.Disconnected)
        Me.Dispatcher.BeginInvoke(Windows.Threading.DispatcherPriority.Normal, Sub() UpdateUI(endPoint, ConnectState.Disconnected))
    End Sub

    Private Sub UpdateUI(endPoint As SocketBase, state As ConnectState)
        Me.Title = endPoint.EndPointType.ToString() & ", " & state.ToString()
    End Sub

    Private Sub Send(msgStr As String)
        If Not _socket Is Nothing Then
            _socket.SendText(msgStr)
        End If
    End Sub

    Private Sub DisableIpTxt()
        txtIpAddress.IsEnabled = False
        txtPort.IsEnabled = False
        txtExpectedIpAddress.IsEnabled = False
    End Sub

    Private Sub DisableServerOptions()
        btnServerActivate.IsEnabled = False
    End Sub

    Private Sub DisableClientOptions()
        btnClientActivate.IsEnabled = False
    End Sub

    Private Sub PrepareChatBox()
        loginPanel.Visibility = True
        chatBox.Visibility = False
    End Sub
#End Region

#Region "test"
    Private Sub btnTest_Click(sender As Object, e As RoutedEventArgs)
    End Sub
#End Region
End Class
