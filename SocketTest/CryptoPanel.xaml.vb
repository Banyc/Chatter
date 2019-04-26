Public Class CryptoPanel
    Private WithEvents _socket As SocketBase
    Private seedStr As String

    Public Sub New()
        InitializeComponent()
        _socket = Nothing
        btnSendSessionKey.IsEnabled = False
    End Sub

    Public Sub SetSocket(handler As SocketBase)
        _socket = handler
        seedStr = Nothing
    End Sub

    Private Function GetHashValue(seedStr As String) As Integer
        If seedStr Is Nothing Then
            stateBanner.Text = "YOU FORGOT TO SET SEED!"
            Return Nothing
        ElseIf seedStr.Count < 64 Then
            stateBanner.Text = "PLEASE TYPE MORE RANDOM KEYS!"
            Return Nothing
        Else
            Dim hashCode As Integer = seedStr.GetHashCode()
            Return hashCode
        End If
    End Function

#Region "key manager"
    Private Function GetSavePath() As String
        Dim exportFileDialog As New Microsoft.Win32.SaveFileDialog()
        If exportFileDialog.ShowDialog() Then
            Dim fileName As String = exportFileDialog.FileName
            Return fileName
        Else
            Return Nothing
        End If
    End Function

    Private Sub BtnSavePubKey_Click(sender As Object, e As RoutedEventArgs)
        If Not _socket Is Nothing Then
            Dim fileName As String
            fileName = GetSavePath()
            If Not fileName Is Nothing Then
                IO.File.WriteAllText(fileName, _socket.GetPublicKey())
            End If
        End If
    End Sub

    Private Sub BtnSavePriKey_Click(sender As Object, e As RoutedEventArgs)
        If Not _socket Is Nothing Then
            Dim fileName As String
            fileName = GetSavePath()
            If Not fileName Is Nothing Then
                IO.File.WriteAllText(fileName, _socket.GetPrivateKey())
            End If
        End If
    End Sub

    Private Function GetReadPath()
        Dim importFileDialog As New Microsoft.Win32.OpenFileDialog()
        If importFileDialog.ShowDialog() Then
            Dim fileName = importFileDialog.FileName
            Return fileName
        Else
            Return Nothing
        End If
    End Function

    Private Sub BtnSetPriKey_Click(sender As Object, e As RoutedEventArgs)
        If Not _socket Is Nothing Then
            Dim fileName As String
            fileName = GetReadPath()
            If Not fileName Is Nothing Then
                _socket.SetPrivateKey(IO.File.ReadAllText(fileName))
            End If
        End If
    End Sub

    Private Sub BtnSetPubKey_Click(sender As Object, e As RoutedEventArgs)
        If Not _socket Is Nothing Then
            Dim fileName As String
            fileName = GetReadPath()
            If Not fileName Is Nothing Then
                _socket.SetPublicKey(IO.File.ReadAllText(fileName))
                stateBanner.Text = "Other's public key is loaded. Ready to send session key."
                btnSendSessionKey.IsEnabled = True
            End If
        End If
    End Sub

    Private Sub BtnSendSessionKey_Click(sender As Object, e As RoutedEventArgs)
        If Not _socket Is Nothing Then
            Dim hashValue As Integer
            hashValue = GetHashValue(seedStr)
            If Not hashValue = Nothing Then
                'btnSendSessionKey.IsEnabled = False  ' prevent misbehaving
                _socket.MakeEncryptTunnel(hashValue)
            End If
        End If
    End Sub

    Private Sub txtSeed_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles txtSeed.PreviewKeyDown
        seedStr &= e.Key.ToString()
        e.Handled = True
    End Sub
#End Region

#Region "socket events"
    Private Sub OpppsiteStandby() Handles _socket.OpppsiteStandby
        Me.Dispatcher.BeginInvoke(Windows.Threading.DispatcherPriority.Normal, Sub()
                                                                                   btnSendSessionKey.Content = "Send Session Key!"
                                                                               End Sub)
    End Sub
#End Region
End Class
