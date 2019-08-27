Imports System.ComponentModel

Class MainWindow
    Private _IsFlashing As Boolean

    Public Sub New()
        InitializeComponent()
        _IsFlashing = False
    End Sub

#Region "Events of controls"
    Private Sub MainWindow_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        If _IsFlashing Then
            FlashWindow.Stop(Me)
            _IsFlashing = False
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

        Dim window As New AddEndpoint()
        window.Show()

    End Sub

    Private Sub btnBuildKeyPair_Click(sender As Object, e As RoutedEventArgs)
        Dim exportFileDialog As New Microsoft.Win32.SaveFileDialog()
        Dim filePath As String = Nothing
        If exportFileDialog.ShowDialog() Then
            filePath = exportFileDialog.FileName
        End If

        Dim rsaProvider As RsaApi
        rsaProvider = New RsaApi()

        If filePath IsNot Nothing Then
            IO.File.WriteAllText(filePath & ".priKey", rsaProvider.GetPrivateKey())
            IO.File.WriteAllText(filePath & ".pubKey", rsaProvider.GetMyPublicKey())
        End If
    End Sub

    Private Sub Button_Click(sender As Object, e As RoutedEventArgs)

    End Sub

#End Region
End Class
