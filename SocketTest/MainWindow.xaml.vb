Imports System.ComponentModel

Class MainWindow
    Private WithEvents _client As SocketClient
    Private WithEvents _server As SocketListener
    Private WithEvents _socket As SocketBase
    Private _IsFlashing As Boolean

    Public Sub New()
        InitializeComponent()
        'plainTextPanel.Visibility = Visibility.Collapsed
        cryptoPanel.Visibility = Visibility.Collapsed
        chatBox.Visibility = Visibility.Collapsed
        _IsFlashing = False
    End Sub

#Region "Events of controls"
    'Private Sub btnSend_Click(sender As Object, e As RoutedEventArgs)
    '    If Not _socket Is Nothing Then
    '        _socket.SendText(txtSend.Text)
    '    End If
    'End Sub

    Private Sub btnServerActivate_Click(sender As Button, e As RoutedEventArgs)
        btnClientServerActivate_Click(SocketCS.Server, sender, e)
    End Sub

    Private Sub btnClientActivate_Click(sender As Button, e As RoutedEventArgs)
        btnClientServerActivate_Click(SocketCS.Client, sender, e)
    End Sub

    Private Sub btnClientServerActivate_Click(csType As SocketCS, sender As Button, e As RoutedEventArgs)
        DisableServerOptions()
        DisableClientOptions()
        DisableIpTxt()
        If _client Is Nothing Then
            If _server Is Nothing Then
                Select Case csType
                    Case SocketCS.Client
                        _client = New SocketClient(txtIpAddress.Text, Int(txtPort.Text))
                        _socket = _client  ' mark
                        Me.Title = "Client"
                    Case SocketCS.Server
                        _server = New SocketListener(txtIpAddress.Text, Int(txtPort.Text), txtExpectedIpAddress.Text)
                        _socket = _server  ' mark
                        Me.Title = "Server"
                End Select
                'plainTextPanel.Visibility = Visibility.Visible
                cryptoPanel.Visibility = Visibility.Visible
                CSPanel.Visibility = Visibility.Collapsed
                cryptoPanel.SetSocket(_socket)
                _socket.Start()
            End If
        End If
    End Sub

    Private Sub MainWindow_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        If _IsFlashing Then
            FlashWindow.Stop(Me)
            _IsFlashing = False
        End If
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

#Region "socket events"  ' Inside which should let dispatcher invoke procedures concerning GUI
    Private Sub _socket_ReceivedText() Handles _socket.ReceiveText
        Receive(_socket)
        FlashTaskbar()
    End Sub

    Private Sub _socket_ReceivedFeedBack(myText As String) Handles _socket.ReceivedFeedBack
        chatBox.MyMessage(myText)
    End Sub

    Private Sub _socket_Connected() Handles _socket.Connected
        Connected(_socket)
        FlashTaskbar()
    End Sub

    Private Sub _socket_Encrypted() Handles _socket.Encrypted
        Encrypted(_socket)
    End Sub

    Private Sub _socket_Disconnected() Handles _socket.Disconnected
        Disconnected(_socket)
        FlashTaskbar()
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
        loginPanel.Visibility = Visibility.Collapsed
        chatBox.Visibility = Visibility.Visible
    End Sub

    Private Sub FlashTaskbar()
        If Not _IsFlashing Then
            Me.Dispatcher.
                BeginInvoke(Windows.Threading.DispatcherPriority.Normal,
                            Sub()
                                If Not Me.IsActive Then
                                    FlashWindow.Start(Me)
                                    _IsFlashing = True
                                End If
                            End Sub)
        End If
    End Sub
#End Region

#Region "test"
    Private Sub btnTest_Click(sender As Object, e As RoutedEventArgs)
    End Sub
#End Region
End Class
