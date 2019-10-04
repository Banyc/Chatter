Imports System.ComponentModel

Class MainWindow
    Private _IsFlashing As Boolean
    Private WithEvents _viewModel As MainWindowViewModel

    Public Sub New()
        _viewModel = New MainWindowViewModel()
        Me.DataContext = _viewModel
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

    Private Sub btnBuildKeyPair_Click(sender As Object, e As RoutedEventArgs)
        Dim exportFileDialog As New Microsoft.Win32.SaveFileDialog()
        Dim filePath As String = Nothing
        If exportFileDialog.ShowDialog() Then
            filePath = exportFileDialog.FileName
        End If

        _viewModel.SaveKeyPairs(filePath)
    End Sub

    Private Sub lvConfig_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
        Call btnSelectConnect_Click(sender, e)
    End Sub

    Private Sub btnAddConnect_Click(sender As Object, e As RoutedEventArgs)
        Dim window As New EndpointSettings(_viewModel)
        window.Show()
    End Sub

    Private Sub btnSelectConnect_Click(sender As Object, e As RoutedEventArgs)
        Dim window As New EndpointSettings(_viewModel, lvConfig.SelectedItem)
        window.Show()
    End Sub

    Private Sub btnDeleteConnect_Click(sender As Object, e As RoutedEventArgs)
        If lvConfig.SelectedItem IsNot Nothing Then
            _viewModel.DeleteSettings(lvConfig.SelectedItem.ID)
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

    Private Sub _viewModel_Refreshed() Handles _viewModel.Refreshed
        lvConfig.ItemsSource = _viewModel.Config.List
        lvConfig.Items.Refresh()
    End Sub
#End Region

#Region "test"

#End Region
End Class
