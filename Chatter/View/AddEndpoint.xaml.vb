Imports System.ComponentModel

Public Class AddEndpoint

    Private WithEvents _builder As New SocketBuilder()
    Private WithEvents _socket As SocketBase

    Public Sub New()

        ' 此调用是设计器所必需的。
        InitializeComponent()

        ' 在 InitializeComponent() 调用之后添加任何初始化。

    End Sub
    Private Sub btnBuildSocket_Click(sender As Object, e As RoutedEventArgs)
        Dim config As New SocketSettingsFramework()
        config.IP = IP.Text
        config.Port = Port.Text
        config.ExpectedIP = ExpectedIP.Text
        config.Role = Role.SelectedIndex
        config.Seed = Seed.Text
        config.PrivateKeyPath = PathToPriKey.Text
        config.PublicKeyPath = PathToPubKey.Text

        _builder.AsyncBuild(config)
    End Sub

    Private Sub ReadyToChat(socket As SocketBase) Handles _builder.BuildDone
        If _socket Is Nothing OrElse _socket.IsShutdown Then
            _socket = socket
            Me.Dispatcher.BeginInvoke(Windows.Threading.DispatcherPriority.Normal,
                                  Sub()
                                      Dim chatBox As New ChatBox(socket)
                                      chatBox.Show()
                                  End Sub)
        Else
            socket.Shutdown()
        End If
    End Sub

    Private Sub btnRoloadChatBox_Click(sender As Object, e As RoutedEventArgs)
        If _socket IsNot Nothing Then
            Me.Dispatcher.BeginInvoke(Windows.Threading.DispatcherPriority.Normal,
                                  Sub()
                                      Dim chatBox As New ChatBox(_socket)
                                      chatBox.Show()
                                  End Sub)
        End If
    End Sub

    Private Sub AddEndpoint_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        _builder.Abort()
        If _socket IsNot Nothing Then
            _socket.Shutdown()
        End If
    End Sub

    Private Sub _socket_Connected() Handles _socket.Connected
        Me.Title = "Connected"
    End Sub
End Class
