Public Enum ChatRole
    System
    Opposite
    User
End Enum

Public Enum ConnectState
    Connected
    Encrypted
    Disconnected
End Enum

Public Class ChatBox
    Public Event SendMessage(message As String)

    Public Sub New()
        InitializeComponent()

        btnSend.Visibility = True  ' hide the unfit button
    End Sub

    ' the text should be put on the screen when the opposite received it and send back the feedback
    Public Sub MyReceivedMsg(msgStr As String)
        AddTxtMessage(ChatRole.User, msgStr)
    End Sub

    ' from the opposite
    Public Sub NewMessage(msgStr As String)
        AddTxtMessage(ChatRole.Opposite, msgStr)
    End Sub

    Public Sub NewState(state As ConnectState, Optional ByVal additionalMsg As String = Nothing)
        If additionalMsg Is Nothing Then
            additionalMsg = state.ToString()
        Else
            additionalMsg = state.ToString() & ": " & additionalMsg
        End If

        AddTxtMessage(ChatRole.System, additionalMsg)
    End Sub

    Private Sub AddTxtMessage(sender As ChatRole, msgStr As String)
        AddTxtMessage(sender, msgStr, DateTime.Now)
    End Sub

    Private Sub AddTxtMessage(sender As ChatRole, msgStr As String, time As DateTime)
        Me.Dispatcher.
            BeginInvoke(Windows.Threading.DispatcherPriority.Normal,
                        Sub()
                            txtMessage.Text &= String.Format(vbCrLf & "{0} {1}" & vbCrLf & "{2}" & vbCrLf, sender.ToString(), time.ToString(), msgStr)
                            scroll.ScrollToBottom()
                        End Sub)
    End Sub

    Private Sub btnSend_Click(sender As Object, e As RoutedEventArgs) Handles btnSend.Click
        RaiseEvent SendMessage(txtInput.Text)
        'AddTxtMessage(ChatRole.User, txtInput.Text)  ' the text should be put on the screen when the opposite received it and send back the feedback
        txtInput.Clear()
        e.Handled = True
    End Sub

    ' auto sends message when press enter
    Private Sub txtInput_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles txtInput.PreviewKeyDown
        If e.Key = Key.Enter Then
            If txtInput.Text.Any Then
                Call btnSend_Click(sender, e)
                e.Handled = True
            End If
        End If
    End Sub
End Class
