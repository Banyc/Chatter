Public Class SocketBuilder
    Private WithEvents _socket As SocketBase
    Private _config As SocketSettingsFramework
    Public Event BuildDone(socket As SocketBase)

    Public Sub AsyncBuild(config As SocketSettingsFramework)
        ' Initiation
        If _socket IsNot Nothing AndAlso _socket.IsShutdown Then
            _socket = Nothing
        End If
        If _socket Is Nothing Then
            _config = config
            Select Case config.Role
                Case SocketCS.Client
                    _socket = New SocketClient(config.IP, config.Port)
                Case SocketCS.Server
                    _socket = New SocketListener(config.IP, config.Port, config.ExpectedIP)
                Case Else
                    _socket = Nothing
            End Select
            If _socket IsNot Nothing Then
                Dim rsa As RsaApi = InitCryptoFacility()
                _socket.InitKeyExchange(_config.Seed, rsa)

                ' Start building socket
                _socket.BuildConnection()
            End If
        End If
    End Sub

    Public Sub Abort()
        If _socket IsNot Nothing Then
            _socket.Shutdown()
        End If
    End Sub

    Private Function InitCryptoFacility() As RsaApi
        Dim rsa As New RsaApi()
        rsa.SetPrivateKey(IO.File.ReadAllText(_config.PrivateKeyPath))
        rsa.SetOthersPublicKey(IO.File.ReadAllText(_config.PublicKeyPath))
        Return rsa
    End Function

    Private Sub ConnectedDone() Handles _socket.Connected
        Select Case _socket.EndPointType
            Case SocketCS.Client
            Case SocketCS.Server
                _socket.SendStandbyMsg()
        End Select
    End Sub

    Private Sub GotStandbyMsg() Handles _socket.OpppsiteStandby
        _socket.LaunchKeyExchange()
    End Sub

    Private Sub Done() Handles _socket.Encrypted
        RaiseEvent BuildDone(_socket)
    End Sub

End Class
