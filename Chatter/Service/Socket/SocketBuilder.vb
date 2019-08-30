Public Class SocketBuilder
    Private WithEvents _socketMng As SocketBase
    Private _config As SocketSettingsFramework
    Public Event BuildDone(socket As SocketBase)

    Public Sub AsyncBuild(config As SocketSettingsFramework)
        ' Initiation
        If _socketMng IsNot Nothing AndAlso _socketMng.IsShutdown Then
            _socketMng = Nothing
        End If
        If _socketMng Is Nothing Then
            _config = config
            Select Case config.Role
                Case SocketCS.Client
                    _socketMng = New SocketClient(config.IP, config.Port)
                Case SocketCS.Server
                    _socketMng = New SocketListener(config.IP, config.Port, config.ExpectedIP)
                Case Else
                    _socketMng = Nothing
            End Select
            If _socketMng IsNot Nothing Then
                Dim rsa As RsaApi

                Try
                    rsa = InitCryptoFacility()
                Catch ex As IO.IOException
                    _socketMng.Shutdown()
                    MessageBox.Show(ex.Message.ToString(), "Error")
                    Return
                End Try

                _socketMng.InitKeyExchange(_config.Seed.GetHashCode(), rsa)

                ' Start building socket
                _socketMng.BuildConnection()
            End If
        End If
    End Sub

    Public Sub Abort()
        If _socketMng IsNot Nothing Then
            _socketMng.Shutdown()
        End If
    End Sub

    Private Function InitCryptoFacility() As RsaApi
        Dim rsa As New RsaApi()
        rsa.SetPrivateKey(IO.File.ReadAllText(_config.PrivateKeyPath))
        rsa.SetOthersPublicKey(IO.File.ReadAllText(_config.PublicKeyPath))
        Return rsa
    End Function

    Private Sub ConnectedDone() Handles _socketMng.Connected
        Select Case _socketMng.EndPointType
            Case SocketCS.Client
                ' server actively launch signal to start session key exchange process
            Case SocketCS.Server
                _socketMng.SendStandbyMsg()
        End Select
    End Sub

    Private Sub GotStandbyMsg() Handles _socketMng.OpppsiteStandby
        _socketMng.LaunchKeyExchange()
    End Sub

    Private Sub Done() Handles _socketMng.Encrypted
        RaiseEvent BuildDone(_socketMng)
    End Sub
End Class
