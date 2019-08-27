Public Class CryptoPanel
    Private WithEvents _socket As SocketBase
    Private seedStr As String

    Public Sub New()
        InitializeComponent()
        _socket = Nothing
        btnSendSessionKey.IsEnabled = False
    End Sub

    ' update current socket object
    Public Sub SetSocket(handler As SocketBase)
        _socket = handler
        seedStr = Nothing
    End Sub

    ' returns the coordinate hashing value of a string
    Private Function GetHashValue(seedStr As String) As Integer
        If seedStr Is Nothing Then
            stateBanner.Text = "YOU FORGOT TO SET SEED!"
            Return Nothing
#If Not DEBUG Then
        ElseIf seedStr.Count < 64 Then
            stateBanner.Text = "PLEASE TYPE MORE RANDOM KEYS!"
            Return Nothing
#End If
        Else
            Dim hashCode As Integer = seedStr.GetHashCode()
            Return hashCode
        End If
    End Function

#Region "key manager"
    ' request a path to save file
    Private Shared Function GetSavePath() As String
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

    Private Shared Function GetReadPath()
        Dim importFileDialog As New Microsoft.Win32.OpenFileDialog()
        If importFileDialog.ShowDialog() Then
            Dim fileName = importFileDialog.FileName
            Return fileName
        Else
            Return Nothing
        End If
    End Function

    ' update the private key
    Private Sub SetPriKey(keyContent As String)
        _socket.SetPrivateKey(keyContent)
    End Sub

    ' update the public key
    Private Sub SetPubKey(keyContent As String)
        _socket.SetPublicKey(keyContent)
        stateBanner.Text = "Public key loaded. Ready to send session key."
        btnSendSessionKey.IsEnabled = True
    End Sub

    Private Sub BtnSetPriKey_Click(sender As Object, e As RoutedEventArgs)
        If Not _socket Is Nothing Then
            Dim fileName As String
            fileName = GetReadPath()
            If Not fileName Is Nothing Then
                SetPriKey(IO.File.ReadAllText(fileName))
            End If
        End If
    End Sub

    Private Sub BtnSetPubKey_Click(sender As Object, e As RoutedEventArgs)
        If Not _socket Is Nothing Then
            Dim fileName As String
            fileName = GetReadPath()
            If Not fileName Is Nothing Then
                SetPubKey(IO.File.ReadAllText(fileName))
            End If
        End If
    End Sub

    ' send "Stand-by" signal OR send session key to launch key exchange 
    Private Sub BtnSendSessionKey_Click(sender As Object, e As RoutedEventArgs)
        If Not _socket Is Nothing Then
            Dim hashValue As Integer
            hashValue = GetHashValue(seedStr)
            If Not hashValue = Nothing Or Not _socket.IsOppositeStandby() Then  ' sending stand-by signal does not require the generation of a session key
                'btnSendSessionKey.IsEnabled = False  ' prevent misbehaving
                _socket.MakeEncryptTunnel(hashValue)
                stateBanner.Text = "Stand-by message sent." & vbNewLine & "Your key will not be used."
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
                    SetPriKey(keyContent)
                Else
                    SetPubKey(keyContent)
                End If
            End If
        Next
    End Sub

    Private Sub CryptoPanel_PreviewDragEnter(sender As Object, e As DragEventArgs) Handles Me.PreviewDragEnter
        FileDropZone.Visibility = Visibility.Visible
    End Sub

    Private Sub CryptoPanel_PreviewDragLeave(sender As Object, e As DragEventArgs) Handles Me.PreviewDragLeave
        FileDropZone.Visibility = Visibility.Hidden
    End Sub

    Private Sub CryptoPanel_PreviewDrop(sender As Object, e As DragEventArgs) Handles Me.PreviewDrop
        FileDropZone.Visibility = Visibility.Hidden
    End Sub
#End Region
End Class
