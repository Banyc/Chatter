Public Class EndpointSettingsViewModel
    Public Property Settings As SocketSettingsFramework
    Private _invokee As MainWindowViewModel

    Public Sub New(invokee As MainWindowViewModel, Optional settings As SocketSettingsFramework = Nothing)
        _invokee = invokee
        If settings Is Nothing Then
            Me.Settings = New SocketSettingsFramework
            Me.Settings.IP = "127.0.0.1"
            Me.Settings.Port = "11000"
            Me.Settings.PrivateKeyPath = "./Path/To/priKey"
            Me.Settings.PublicKeyPath = "./Path/To/pubKey"
            Me.Settings.Seed = "Please type in random characters"
            Me.Settings.Name = "Undefined"
        Else
            Me.Settings = settings
        End If
    End Sub

    Public Sub SaveSettings()
        Dim config As EndpointConfiguration = EndpointConfiguration.Load()
        config.Add(Me.Settings)
        config.Save()
        _invokee.Reload()
    End Sub
End Class
