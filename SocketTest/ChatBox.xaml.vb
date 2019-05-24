Public Enum ChatRole
    System
    Opposite
    ThisUser
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
    End Sub

#Region "Procedures"
    ' the text should be put on the screen when the opposite received it and send back the feedback
    Public Sub MyMessage(msgStr As String)
        AddTxtMessage(ChatRole.ThisUser, msgStr)
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

    ' Add text to the chat box
    Private Sub AddTxtMessage(sender As ChatRole, msgStr As String, time As DateTime)
        'Dim emptyLine = New Run(vbNewLine)
        Dim stateLine = New Run(String.Format("{0}", time.ToString()))
        stateLine.Foreground = Brushes.LightGray
        Dim textLine = New Run(String.Format("{0}", msgStr))
        textLine.Foreground = Brushes.DarkSlateGray

        Dim stateParag = New Paragraph()
        Dim textParag = New Paragraph()

        'parag.Inlines.Add(emptyLine)
        stateParag.Inlines.Add(stateLine)
        stateParag.Margin = New Thickness(5, 10, 5, 5)

        'parag.Inlines.Add(emptyLine)
        textParag.Inlines.Add(textLine)
        textParag.Margin = New Thickness(5, 5, 5, 10)

        Select Case sender
            Case ChatRole.System
                stateParag.TextAlignment = TextAlignment.Center
                textParag.TextAlignment = TextAlignment.Center
                textLine.Foreground = Brushes.Gray
            Case ChatRole.Opposite
                stateParag.TextAlignment = TextAlignment.Left
                textParag.TextAlignment = TextAlignment.Left
            Case ChatRole.ThisUser
                stateParag.TextAlignment = TextAlignment.Right
                textParag.TextAlignment = TextAlignment.Right
        End Select

        '<https://www.wiredprairie.us/journal/2007/05/creating_wpf_flowdocuments_on.html>
        Dim stateStream As System.IO.MemoryStream = New System.IO.MemoryStream()
        System.Windows.Markup.XamlWriter.Save(stateParag, stateStream)
        stateStream.Position = 0

        Dim textStream As System.IO.MemoryStream = New System.IO.MemoryStream()
        System.Windows.Markup.XamlWriter.Save(textParag, textStream)
        textStream.Position = 0

        Me.Dispatcher.
            BeginInvoke(Windows.Threading.DispatcherPriority.Normal,
                        Sub()
                            'txtMessage.Text &= String.Format(vbCrLf & "{0} {1}" & vbCrLf & "{2}" & vbCrLf, sender.ToString(), time.ToString(), msgStr)
                            txtMessage.Document.Blocks.Add(System.Windows.Markup.XamlReader.Load(stateStream))
                            txtMessage.Document.Blocks.Add(System.Windows.Markup.XamlReader.Load(textStream))
                            scroll.ScrollToBottom()
                        End Sub)
    End Sub

    Public Sub ClearAllText()
        Me.Dispatcher.
            BeginInvoke(Windows.Threading.DispatcherPriority.Normal,
                        Sub()
                            txtMessage.Document.Blocks.Clear()
                        End Sub)
    End Sub
#End Region

    Private Sub btnSend_Click(sender As Object, e As RoutedEventArgs)
        RaiseEvent SendMessage(txtInput.Text)
        'AddTxtMessage(ChatRole.ThisUser, txtInput.Text)  ' the text should be put on the screen when the opposite received it and send back the feedback
        txtInput.Clear()
        e.Handled = True
    End Sub

    Private Sub btnNewLine_Click(sender As Object, e As RoutedEventArgs)
        Dim currentCaretIndex As Integer = txtInput.CaretIndex
        txtInput.Text = txtInput.Text.Substring(0, currentCaretIndex) & vbNewLine & txtInput.Text.Substring(currentCaretIndex)
        txtInput.Focus()
        txtInput.CaretIndex = currentCaretIndex + 1
        e.Handled = True
    End Sub

    ' auto sends message when press enter
    Private Sub txtInput_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles txtInput.PreviewKeyDown
        If e.Key = Key.Enter Then
            If txtInput.Text.Any Then
                Call btnSend_Click(sender, e)
            End If
            e.Handled = True
        End If
    End Sub
End Class
