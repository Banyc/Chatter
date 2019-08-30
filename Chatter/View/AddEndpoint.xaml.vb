Imports System.ComponentModel

Public Class AddEndpoint

    Private WithEvents _builder As New SocketBuilder()
    Private WithEvents _socketMng As SocketBase

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

    Private Sub ReadyToChat(socketMng As SocketBase) Handles _builder.BuildDone
        If _socketMng Is Nothing OrElse _socketMng.IsShutdown Then
            _socketMng = socketMng
            Me.Dispatcher.BeginInvoke(Windows.Threading.DispatcherPriority.Normal,
                                  Sub()
                                      Dim chatBox As New ChatBox(socketMng)
                                      chatBox.Show()
                                  End Sub)
        Else
            socketMng.Shutdown()
        End If
    End Sub

    Private Sub btnRoloadChatBox_Click(sender As Object, e As RoutedEventArgs)
        If _socketMng IsNot Nothing AndAlso Not _socketMng.IsShutdown Then
            Me.Dispatcher.BeginInvoke(Windows.Threading.DispatcherPriority.Normal,
                                  Sub()
                                      Dim chatBox As New ChatBox(_socketMng)
                                      chatBox.Show()
                                  End Sub)
        End If
    End Sub

    Private Sub AddEndpoint_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        _builder.Abort()
        If _socketMng IsNot Nothing Then
            _socketMng.Shutdown()
        End If
    End Sub

    Private Sub _socketMng_Connected() Handles _socketMng.Connected
        Me.Title = "Connected"
    End Sub
End Class
