Imports System.ComponentModel

Public Class EndpointSettings

    Private WithEvents _builder As New SocketBuilder()
    Private WithEvents _socketMng As SocketBase

    Private WithEvents _viewModel As EndpointSettingsViewModel

    Public Sub New(invokee As MainWindowViewModel, Optional settings As SocketSettingsFramework = Nothing)
        _viewModel = New EndpointSettingsViewModel(invokee, settings)
        Me.DataContext = _viewModel

        InitializeComponent()

        cbRole.SelectedIndex = _viewModel.Settings.Role
    End Sub

    Private Sub btnBuildSocket_Click(sender As Object, e As RoutedEventArgs)
        If _builder IsNot Nothing Then
            _builder.Abort()
        End If

        Dim settings As New SocketSettingsFramework()
        settings.IP = IP.Text
        settings.Port = Port.Text
        settings.ExpectedIP = ExpectedIP.Text
        settings.Role = cbRole.SelectedIndex
        settings.Seed = Seed.Text
        settings.PrivateKeyPath = PathToPriKey.Text
        settings.PublicKeyPath = PathToPubKey.Text

        _builder.AsyncBuild(settings)
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
            _socketMng.Shutdown()
            _socketMng = socketMng
        End If
    End Sub

    Private Sub btnReloadChatBox_Click(sender As Object, e As RoutedEventArgs)
        If _socketMng IsNot Nothing AndAlso Not _socketMng.IsShutdown Then
            Me.Dispatcher.BeginInvoke(Windows.Threading.DispatcherPriority.Normal,
                                  Sub()
                                      Dim chatBox As New ChatBox(_socketMng)
                                      chatBox.Show()
                                  End Sub)
        End If
    End Sub

    Private Sub btnSaveSettings_Click(sender As Object, e As RoutedEventArgs)
        _viewModel.Settings.Role = cbRole.SelectedIndex
        _viewModel.SaveSettings()
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
