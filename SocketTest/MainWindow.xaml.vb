Imports System.ComponentModel

Class MainWindow
    Private _client As SocketClient
    Private _server As SocketListener

    Public Sub New()

        ' 此调用是设计器所必需的。
        InitializeComponent()

        ' 在 InitializeComponent() 调用之后添加任何初始化。
    End Sub

    Private Sub btnClientSend(sender As Object, e As RoutedEventArgs)
        If Not _client Is Nothing Then
            _client.Send(txtClient.Text)
        End If
    End Sub

    Private Sub btnServerSend(sender As Object, e As RoutedEventArgs)
        If Not _server Is Nothing Then
            _server.Send(txtServer.Text)
        End If
    End Sub

    Private Sub DisableTxt()
        txtIpAddress.IsEnabled = False
        txtPort.IsEnabled = False
    End Sub

    Private Sub btnServerActivate(sender As Object, e As RoutedEventArgs)
        DisableTxt()
        If _server Is Nothing Then
            If _client Is Nothing Then
                _server = New SocketListener(txtIpAddress.Text, Int(txtPort.Text))
                Me.Title = "Server"
            End If
        End If
    End Sub

    Private Sub btnClientActivate(sender As Object, e As RoutedEventArgs)
        DisableTxt()
        If _client Is Nothing Then
            If _server Is Nothing Then
                _client = New SocketClient(txtIpAddress.Text, Int(txtPort.Text))
                Me.Title = "Client"
            End If
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
End Class
