Imports System.ComponentModel

Class MainWindow
    Private _socket As SocketBase
    Private _socketMng As SocketManager
    Private _IsFlashing As Boolean

    Public Sub New()
        InitializeComponent()
        cryptoPanel.Visibility = Visibility.Collapsed
        chatBox.Visibility = Visibility.Collapsed
        _IsFlashing = False
    End Sub

#Region "Events of controls"
    Private Sub btnServerActivate_Click(sender As Button, e As RoutedEventArgs)
        btnClientServerActivate_Click(SocketCS.Server, sender, e)
    End Sub

    Private Sub btnClientActivate_Click(sender As Button, e As RoutedEventArgs)
        btnClientServerActivate_Click(SocketCS.Client, sender, e)
    End Sub

    Private Sub btnClientServerActivate_Click(csType As SocketCS, sender As Button, e As RoutedEventArgs)
        DisableServerOptions()
        DisableClientOptions()
        DisableIpTxt()
        If _socket Is Nothing Then
            Select Case csType
                Case SocketCS.Client
                    _socket = New SocketClient(txtIpAddress.Text, Int(txtPort.Text))
                    Me.Title = "Client"
                Case SocketCS.Server
                    _socket = New SocketListener(txtIpAddress.Text, Int(txtPort.Text), txtExpectedIpAddress.Text)
                    Me.Title = "Server"
            End Select
            cryptoPanel.Visibility = Visibility.Visible
            CSPanel.Visibility = Visibility.Collapsed
            cryptoPanel.SetSocket(_socket)
            _socket.Start()
        End If
        '_socketMng = New SocketManager(_socket, Me, chatBox, cryptoPanel)
        _socketMng = New SocketManager(_socket, Me, chatBox)
    End Sub

    Private Sub MainWindow_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        If _IsFlashing Then
            FlashWindow.Stop(Me)
            _IsFlashing = False
        End If
    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If Not _socket Is Nothing Then
            _socket.Shutdown()
        End If
    End Sub
#End Region

#Region "functions"
    Public Sub UpdateUI_Invoke(endPoint As SocketBase, state As ChatState)
        Me.Dispatcher.BeginInvoke(Windows.Threading.DispatcherPriority.Normal,
                                  Sub()
                                      Me.Title = endPoint.EndPointType.ToString() & ", " & state.ToString()
                                  End Sub)
    End Sub

    Private Sub DisableIpTxt()
        txtIpAddress.IsEnabled = False
        txtPort.IsEnabled = False
        txtExpectedIpAddress.IsEnabled = False
    End Sub

    Private Sub DisableServerOptions()
        btnServerActivate.IsEnabled = False
    End Sub

    Private Sub DisableClientOptions()
        btnClientActivate.IsEnabled = False
    End Sub

    Public Sub PrepareChatBox_Invoke()
        Me.Dispatcher.BeginInvoke(Windows.Threading.DispatcherPriority.Normal,
                                  Sub()
                                      loginPanel.Visibility = Visibility.Collapsed
                                      chatBox.Visibility = Visibility.Visible
                                  End Sub)
    End Sub

    Public Sub FlashTaskbar_Invoke()
        If Not _IsFlashing Then
            Me.Dispatcher.
                BeginInvoke(Windows.Threading.DispatcherPriority.Normal,
                            Sub()
                                If Not Me.IsActive Then
                                    FlashWindow.Start(Me)
                                    _IsFlashing = True
                                End If
                            End Sub)
        End If
    End Sub
#End Region

#Region "test"
    Private Sub btnTest_Click(sender As Object, e As RoutedEventArgs)
        '' test AES
        'Dim aes = New AesApi(834098156)
        'Dim bKey = aes.GetSessionKey()
        'Dim key = Text.Encoding.ASCII.GetString(bKey)
        'MessageBox.Show(key)

        '' test RSA
        'Dim pri As New RsaApi()
        'Dim pub As New RsaApi()
        'Dim pubKey As String
        'pubKey = pri.GetMyPublicKey()
        'pub.SetOthersPublicKey(pubKey)
        'Dim plainText As String = "trip round"
        'Dim cipherText As Byte() = pub.EncryptMsg(Text.Encoding.ASCII.GetBytes(plainText))
        ''Dim tripRound As String = Text.Encoding.ASCII.GetString(pri.DecryptMsg(cipherText))
        ''MessageBox.Show(tripRound)
        'Dim wrongTrip As String = Text.Encoding.ASCII.GetString(pub.DecryptMsg(cipherText))
        'MessageBox.Show(wrongTrip)

        '' test AES
        'Dim sKey As Byte()
        'Dim IV As Byte()
        'Dim endSend As New AesApi(123)
        'Dim endReceive As New AesApi(654)
        'Dim plainText As String = "trip round"
        'Dim cipherText As Byte() = endSend.EncryptMsg(plainText)
        'Dim tripRound As String
        ''tripRound = endReceive.DecryptMsg(cipherText)
        ''MessageBox.Show(tripRound)
        'sKey = endSend.GetSessionKey()
        'IV = endSend.GetIV()
        'endReceive.SetSessionKey(sKey)
        'endReceive.SetIV(IV)
        ''tripRound = endReceive.DecryptMsg(cipherText)
        'tripRound = endReceive.DecryptMsg(cipherText)
        'MessageBox.Show(tripRound)


        '' ///////////////    test key exchange        ////////////////////
        'Dim sKey As Byte()
        'Dim IV As Byte()
        'Dim encryptedSKey As Byte()
        'Dim encryptedIV As Byte()


        'Dim pubKey As String
        'Dim priKey As String


        'Dim plainText As String = "aaaaa"
        'Dim cipherText As Byte()

        '' 1 sends to 2

        'Dim RSA1 As New RsaApi()  ' public key receiver
        'Dim RSA2 As New RsaApi()  ' private key holder

        'Dim AES1 As New AesApi(1234)
        'Dim AES2 As New AesApi(0)

        '' public key exchange
        'pubKey = RSA2.GetMyPublicKey()
        'RSA1.SetOthersPublicKey(pubKey)

        'sKey = AES1.GetSessionKey()
        'IV = AES1.GetIV()

        '' encrypt session key
        'encryptedSKey = RSA1.EncryptMsg(sKey)
        'encryptedIV = RSA1.EncryptMsg(IV)

        '' 1 send

        '' 2 received

        '' decrypt session key
        'sKey = RSA2.DecryptMsg(encryptedSKey)
        'IV = RSA2.DecryptMsg(encryptedIV)
        'AES2.SetSessionKey(sKey)
        'AES2.SetIV(IV)

        '' encrypt message by session key
        'cipherText = AES1.EncryptMsg(plainText)

        '' 1 send

        '' 2 received

        '' decrypt message by session key
        'plainText = AES2.DecryptMsg(cipherText)

        'MessageBox.Show(plainText)


        '' Test increment of bytes

        'Dim bVal As Byte() = {0, 0, 0, 0}
        'SocketBase.IncrementBytes(bVal, 300)  ' 0,0,1,45
        'SocketBase.IncrementBytes(bVal, 300)  ' 0,0,2,90
        'SocketBase.IncrementBytes(bVal, 300)  ' 0,0,3,135
        'SocketBase.DecrementBytes(bVal, 300)
        'SocketBase.DecrementBytes(bVal, 300)
        'SocketBase.DecrementBytes(bVal, 300)

        'SocketBase.DecrementBytes(bVal, 300)
        'SocketBase.DecrementBytes(bVal, 300)
        'SocketBase.DecrementBytes(bVal, 300)
        'SocketBase.IncrementBytes(bVal, 300)  ' 
        'SocketBase.IncrementBytes(bVal, 300)  ' 
        'SocketBase.IncrementBytes(bVal, 300)

        Me.Title = "Socket Demo"
        PrepareChatBox_Invoke()
        chatBox.NewState(ChatState.Connected, "127.0.0.1")
        chatBox.NewMessage("Hello")
        chatBox.NewMessage("This is Mike")
        chatBox.MyMessage("Nice to meet you!")
        chatBox.NewMessage("Me too")
        chatBox.MyMessage("We have a really important project")
        chatBox.MyMessage("Do you want to join us?")
        chatBox.NewMessage("Sure! Why not?")

        Dim msgBoxThread As Threading.Thread = New System.Threading.Thread(
            Sub()
                MessageBox.Show("Return to main page")
                chatBox.ClearAllText()
                Me.Dispatcher.BeginInvoke(Windows.Threading.DispatcherPriority.Normal, Sub()
                                                                                           loginPanel.Visibility = Visibility.Visible
                                                                                           chatBox.Visibility = Visibility.Collapsed
                                                                                       End Sub)
                msgBoxThread.Abort()
            End Sub)
        msgBoxThread.Start()
    End Sub
#End Region
End Class
