' This is the definition of a setting panel
' It is used to launch a new connection or save settings for future use
Imports System.ComponentModel

Public Class EndpointSettings

    Private WithEvents _builder As New SocketBuilder()
    Private WithEvents _socketMng As SocketBase

    Private WithEvents _viewModel As EndpointSettingsViewModel

    Public Sub New(invokee As MainWindowViewModel, Optional settings As SocketSettingsFramework = Nothing)
        _viewModel = New EndpointSettingsViewModel(invokee, settings)
        Me.DataContext = _viewModel

        InitializeComponent()

        UpdateView()
    End Sub

    Private Sub btnBuildSocket_Click(sender As Object, e As RoutedEventArgs)
        If _builder IsNot Nothing Then
            _builder.Abort()
        End If

        UpdateViewModel()

        Dim task As Task(Of SocketBase) = _builder.GloballyBuild(_viewModel.Settings)
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
        UpdateViewModel()
        _viewModel.SaveSettings()
    End Sub

    Private Sub btnSaveAs_Click(sender As Object, e As RoutedEventArgs)
        UpdateViewModel()
        _viewModel.SaveAs()
    End Sub

#Region "File Drop on panel"
    ' this user drop a file on `Me`
    Private Sub FileDropZone_Drop(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            Dim files As String() = e.Data.GetData(DataFormats.FileDrop)
            SetKeysFromFile(files)
        End If
    End Sub

    ' extract keys from the files and update them to the container
    Private Sub SetKeysFromFile(files As String())
        For Each file In files
            Dim keyContent As String = IO.File.ReadAllText(file)
            If keyContent.Contains("RSAParameters") Then
                If keyContent.Contains("<P>") Then
                    PathToPriKey.Text = file
                Else
                    PathToPubKey.Text = file
                End If
            End If
        Next
    End Sub

    Private Sub Me_PreviewDragEnter(sender As Object, e As DragEventArgs) Handles Me.PreviewDragEnter
        FileDropZone.Visibility = Visibility.Visible
    End Sub

    Private Sub Me_PreviewDragLeave(sender As Object, e As DragEventArgs) Handles Me.PreviewDragLeave
        FileDropZone.Visibility = Visibility.Hidden
    End Sub

    Private Sub Me_PreviewDrop(sender As Object, e As DragEventArgs) Handles Me.PreviewDrop
        FileDropZone.Visibility = Visibility.Hidden
    End Sub
#End Region

    Private Sub AddEndpoint_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        '_builder.Abort()
        'If _socketMng IsNot Nothing Then
        '    _socketMng.Shutdown()
        'End If
    End Sub

    Private Sub _socketMng_Connected() Handles _socketMng.Connected
        Me.Title = "Connected"
    End Sub

    Private Sub UpdateViewModel()
        _viewModel.Settings.IP = IP.Text
        _viewModel.Settings.Port = Port.Text
        _viewModel.Settings.PrivateKeyPath = PathToPriKey.Text
        _viewModel.Settings.PublicKeyPath = PathToPubKey.Text
        _viewModel.Settings.Seed = Seed.Text
        _viewModel.Settings.Name = Name.Text
        _viewModel.Settings.Role = cbRole.SelectedIndex
    End Sub
    Private Sub UpdateView()
        cbRole.SelectedIndex = _viewModel.Settings.Role
    End Sub
End Class
