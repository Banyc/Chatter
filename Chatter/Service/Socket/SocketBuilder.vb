Public Class SocketBuilder
    Private WithEvents _socketMng As SocketBase
    Private _settings As SocketSettingsFramework
    Public Event BuildDone(socket As SocketBase)

    Private _IsSucceeded As Boolean = False
    Private _catchedException As Exception = Nothing

    Private _allDone As System.Threading.ManualResetEvent = New System.Threading.ManualResetEvent(False)

    ' build a socket together with alternative actions
    Public Async Function GloballyBuild(setting As SocketSettingsFramework) As Task(Of SocketBase)
        Dim ShouldExitLoop As Boolean = False

        While (Not ShouldExitLoop)
            _IsSucceeded = False
            _catchedException = Nothing
            _allDone.Reset()

            Await Me.AsyncBuild(setting)
            'Dim task As Task = New Task(Sub()
            _allDone.WaitOne()
            _allDone.Reset()
            '                            End Sub)
            'task.Start()
            'Await task
            If Not _IsSucceeded Then
                Select Case setting.Role
                    Case SocketCS.Client
                        ShouldExitLoop = True  ' exit
                    Case SocketCS.Server
                        ' do nothing to wait a restart
                End Select

                If _catchedException IsNot Nothing Then
                    Select Case _catchedException.GetType()  ' all exceptions occurred in socket should be considered here
                        Case GetType(RsaDecryptionException)  ' there at least one key pair is not matched
                            Dim tmpAction As Action(Of Object) = Sub(msg As Object)
                                                                     MessageBox.Show(msg, "Error")
                                                                 End Sub
                            Dim tmpTask As New Task(tmpAction, _catchedException.Message)
                            tmpTask.Start()
                        Case Else
                            MessageBox.Show(_catchedException.Message, "Unknown Error")
                    End Select
                Else
                    ShouldExitLoop = True
                End If
            Else
                ShouldExitLoop = True
            End If
        End While
        Return _socketMng

        'Return Nothing
    End Function

    ' Trigger a construction of a specific socket. 
    Public Function AsyncBuild(settings As SocketSettingsFramework) As Task
        Dim buildThread As Task = New Task(
            Sub()
                ' Initiation
                'If _socketMng IsNot Nothing AndAlso _socketMng.IsShutdown Then
                _socketMng = Nothing
                    'End If
                    If _socketMng Is Nothing Then
                    _settings = settings
                    Select Case settings.Role
                        Case SocketCS.Client
                            _socketMng = New SocketClient(settings.IP, settings.Port)
                        Case SocketCS.Server
                            _socketMng = New SocketListener(settings.IP, settings.Port, settings.ExpectedIP)
                        Case Else
                            _socketMng = Nothing
                    End Select
                    If _socketMng IsNot Nothing Then
                        Dim rsa As RsaApi

                        Try
                            rsa = InitCryptoFacility()
                        Catch ex As IO.IOException
                            _socketMng.Shutdown()
                            MessageBox.Show(ex.Message, "Error")
                            Return
                        End Try

                        _socketMng.InitKeyExchange(_settings.Seed.GetHashCode(), rsa)

                        ' Start building socket
                        _socketMng.BuildConnection()
                    End If
                End If
                Return
            End Sub)
        buildThread.Start()
        Return buildThread
    End Function

    Public Sub Abort()
        If _socketMng IsNot Nothing Then
            _socketMng.Shutdown()
        End If
    End Sub

    Private Function InitCryptoFacility() As RsaApi
        Dim rsa As New RsaApi()
        rsa.SetPrivateKey(IO.File.ReadAllText(_settings.PrivateKeyPath))
        rsa.SetOthersPublicKey(IO.File.ReadAllText(_settings.PublicKeyPath))
        Return rsa
    End Function

    Private Sub ConnectedDone() Handles _socketMng.Connected
        Select Case _socketMng.EndPointType
            Case SocketCS.Client
            Case SocketCS.Server
                ' server actively launch signal to start session key exchange process
                _socketMng.SendStandbyMsg()
        End Select
    End Sub

    Private Sub GotStandbyMsg() Handles _socketMng.OpppsiteStandby
        _socketMng.LaunchKeyExchange()
    End Sub

    Private Sub Done() Handles _socketMng.Encrypted
        RaiseEvent BuildDone(_socketMng)
        _socketMng = Nothing
        _allDone.Set()
    End Sub

    Private Sub CatchedError(ex As Exception) Handles _socketMng.CatchedError
        _catchedException = ex
        _IsSucceeded = False
        _allDone.Set()
    End Sub

    Private Sub Disconnected() Handles _socketMng.Disconnected
        _IsSucceeded = False
        If Not _socketMng.IsEncrypted Then
            MessageBox.Show("Disconnected")
        End If
        _allDone.Set()
    End Sub
End Class
