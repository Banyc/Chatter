
Public Class MainWindowViewModel
    Public Property Config As EndpointConfiguration

    Public Event Refreshed()

    Public Sub New()
        Me.Reload()
    End Sub

    Public Sub Reload()
        Dim endpointConfigList As EndpointConfiguration = EndpointConfiguration.Load()
        Me.Config = endpointConfigList
        RaiseEvent Refreshed()
    End Sub

    Public Sub DeleteSettings(id As Integer)
        Dim config As EndpointConfiguration = EndpointConfiguration.Load()
        config.Delete(config.FindById(id))
        config.Save()
        Me.Reload()
    End Sub

    Public Sub SaveKeyPairs(path As String)
        Dim rsaProvider As RsaApi
        rsaProvider = New RsaApi()

        If path IsNot Nothing Then
            IO.File.WriteAllText(path & ".priKey", rsaProvider.GetPrivateKey())
            IO.File.WriteAllText(path & ".pubKey", rsaProvider.GetMyPublicKey())
        End If
    End Sub
End Class
