Imports System.ComponentModel

Public Enum ChatRole
    System
    Opposite
    ThisUser
End Enum

Public Enum ChatState
    Connected
    Encrypted
    Disconnected
    FileSent
    FileReceived
End Enum

Public Class ChatBox
    Public Event SendMessage(message As String)
    Public Event SendFile(fileBytes As Byte(), fileName As String, path As String)
    Public Event SendImage(imageBytes As Byte())

    Private Const SAVEPATH As String = "./Received_Files/"

    Private _socket As SocketBase

    Public Sub New()
        InitializeComponent()
    End Sub
    Public Sub New(socket As SocketBase)
        _socket = socket

        ' message from the opposite
        AddHandler _socket.ReceivedText, AddressOf NewMessage
        AddHandler _socket.ReceivedText, AddressOf FlashTaskbar
        AddHandler _socket.ReceivedFile, AddressOf HandleReceivedFile
        AddHandler _socket.ReceivedFile, AddressOf FlashTaskbar
        AddHandler _socket.Disconnected, AddressOf UpdateDisconnectedState
        AddHandler _socket.Disconnected, AddressOf FlashTaskbar
        ' My own message
        AddHandler _socket.ReceivedFeedBack, AddressOf HandleMySentMsg

        InitializeComponent()
        Me.Title = _socket.GetRemoteEndPoint()
    End Sub

    Private Sub FlashTaskbar()
        Me.Dispatcher.BeginInvoke(Windows.Threading.DispatcherPriority.Normal,
            Sub()
                FlashWindow.Flash(New Windows.Interop.WindowInteropHelper(Me).Handle)
            End Sub)
    End Sub

#Region "Response to socket state"
    Private Sub HandleMySentMsg(localContentPack As AesLocalPackage)
        Select Case localContentPack.AesContentPack.Kind
            Case AesContentKind.Text
                Me.MyMessage(CType(localContentPack.AesContentPack, AesTextPackage).Text)
            Case AesContentKind.File
                Me.NewState(ChatState.FileSent, CType(localContentPack, AesLocalFilePackage).FilePath)
                Me.DisplayImageIfValid(ChatRole.ThisUser, CType(localContentPack, AesLocalFilePackage).FilePath)
        End Select
    End Sub

    Private Sub UpdateDisconnectedState()
        NewState(ChatState.Disconnected)
    End Sub
#End Region

#Region "Procedures"
    ' the text should be put on the screen when the opposite received it and send back the feedback
    Public Sub MyMessage(msgStr As String)
        AddTxtMessage(ChatRole.ThisUser, msgStr)
    End Sub

    ' from the opposite
    Public Sub NewMessage(msgStr As String)
        AddTxtMessage(ChatRole.Opposite, msgStr)
    End Sub

    Public Sub NewState(state As ChatState, Optional ByVal additionalMsg As String = Nothing)
        If additionalMsg Is Nothing Then
            additionalMsg = state.ToString()
        Else
            additionalMsg = state.ToString() & ": " & additionalMsg
        End If

        AddTxtMessage(ChatRole.System, additionalMsg)
    End Sub

    Private Sub NewThumbnail(img As Image)
        AddThumbnail(ChatRole.Opposite, img)
    End Sub

    Private Sub MyThumbnail(img As Image)
        AddThumbnail(ChatRole.ThisUser, img)
    End Sub

    Private Sub AddThumbnail(sender As ChatRole, img As Image)
        AddThumbnail(sender, img, DateTime.Now)
    End Sub

    ' TODO: merge to `AddTxtMessage`
    Private Sub AddThumbnail(sender As ChatRole, img As Image, time As DateTime)
        ' <https://stackoverflow.com/questions/33466546/the-calling-thread-cannot-access-this-object-because-a-different-thread-owns-it>
        Me.Dispatcher.
            Invoke(Windows.Threading.DispatcherPriority.Normal,
                   Sub()
                       Dim stateLine = New Run(String.Format("{0}", time.ToString()))
                       stateLine.Foreground = Brushes.LightGray

                       Dim stateParag = New Paragraph()  ' deals with the state message
                       Dim textParag = New Paragraph()  ' deals with the new text message

                       'parag.Inlines.Add(emptyLine)
                       stateParag.Inlines.Add(stateLine)
                       stateParag.Margin = New Thickness(5, 10, 5, 5)

                       Dim maxHeight = scroll.ActualHeight * 3 / 4
                       Dim maxWidth = txtMessage.ActualWidth * 3 / 4

                       If img.Source.Width < maxWidth And img.Source.Height < maxHeight Then
                           img.Stretch = Stretch.None
                       Else
                           img.Stretch = Stretch.Uniform
                       End If

                       img.MaxHeight = maxHeight
                       img.MaxWidth = maxWidth

                       textParag.Inlines.Add(img)
                       textParag.Margin = New Thickness(5, 5, 5, 10)

                       Select Case sender
                           Case ChatRole.System
                               stateParag.TextAlignment = TextAlignment.Center
                               textParag.TextAlignment = TextAlignment.Center
                                                                            'img.HorizontalAlignment = HorizontalAlignment.Center

                           Case ChatRole.Opposite
                               stateParag.TextAlignment = TextAlignment.Left
                               textParag.TextAlignment = TextAlignment.Left
                                                                            'img.HorizontalAlignment = HorizontalAlignment.Left

                           Case ChatRole.ThisUser
                               stateParag.TextAlignment = TextAlignment.Right
                               textParag.TextAlignment = TextAlignment.Right
                               'img.HorizontalAlignment = HorizontalAlignment.Right

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
                   End Sub)
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

        Dim stateParag = New Paragraph()  ' deals with the state message
        Dim textParag = New Paragraph()  ' deals with the new text message

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

    ' The image in the file path will be locked
    Public Sub DisplayImageIfValid(role As ChatRole, imagePath As String)
        'If String.Equals(IO.Path.GetExtension(imagePath), ".jpg", StringComparison.CurrentCultureIgnoreCase) Then

        Dim bitmap As BitmapImage

        Try
            bitmap = New BitmapImage(New Uri(imagePath))
        Catch ex As NotSupportedException
            Exit Sub
        End Try

        ' <https://social.msdn.microsoft.com/Forums/vstudio/en-US/bca317e1-299b-4961-ba7b-8afdf977e2e8/thisdispatcherinvoke-gives-me-the-calling-thread-cannot-access-this-object-because-a-different?forum=wpf>
        bitmap.Freeze()

        Dim imgControl As Image = Nothing
        Me.Dispatcher.Invoke(Windows.Threading.DispatcherPriority.Normal, Sub()
                                                                              imgControl = New Image()
                                                                              imgControl.Source = bitmap
                                                                          End Sub)

        AddThumbnail(role, imgControl)
        'End If
    End Sub

    Public Sub ClearAllText()
        Me.Dispatcher.
            BeginInvoke(Windows.Threading.DispatcherPriority.Normal,
                        Sub()
                            txtMessage.Document.Blocks.Clear()
                        End Sub)
    End Sub

    Public Sub HandleReceivedFile(fileBytes As Byte(), fileName As String)
        Dim filePath As String
        filePath = SaveFile(fileBytes, fileName)

        ' display new state on screen
        NewState(ChatState.FileReceived, filePath)

        ' display image if it is a valid image
        DisplayImageIfValid(ChatRole.Opposite, filePath)
    End Sub

    Private Function SaveFile(fileBytes As Byte(), fileName As String)
        Dim fileBaseNameNoExt As String = IO.Path.GetFileNameWithoutExtension(fileName)
        Dim ext As String = IO.Path.GetExtension(fileName)  ' start with '.' if extension exists

        Dim filePath As String = IO.Path.Combine(SAVEPATH, fileName)

        ' create directory if does not exist
        Dim fileInfo As IO.FileInfo
        fileInfo = New IO.FileInfo(filePath)
        fileInfo.Directory.Create()

        ' rename if file exists
        Dim renameID As UInt16 = 0
        While fileInfo.Exists
            renameID += 1
            If ext IsNot Nothing Then
                filePath = IO.Path.Combine(SAVEPATH, String.Format("{0}.{1}{2}", fileBaseNameNoExt, renameID.ToString(), ext))
            Else
                filePath = IO.Path.Combine(SAVEPATH, String.Format("{0}.{1}", fileBaseNameNoExt, renameID.ToString()))
            End If
            fileInfo = New IO.FileInfo(filePath)
        End While

        ' write in
        IO.File.WriteAllBytes(filePath, fileBytes)

        ' return the path
        Return fileInfo.FullName
    End Function

    Public Function ReadFile(path As String) As Byte()
        Return IO.File.ReadAllBytes(path)
    End Function
#End Region

#Region "Events of Controls"
    Private Sub btnSend_Click(sender As Object, e As RoutedEventArgs)
        If _socket IsNot Nothing Then
            _socket.SendCipherText(txtInput.Text)
        Else
            RaiseEvent SendMessage(txtInput.Text)
        End If
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

    Private Sub FileDropZone_Drop(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            Dim files As String() = e.Data.GetData(DataFormats.FileDrop)
            For Each file In files
                If _socket IsNot Nothing Then
                    _socket.SendFile(ReadFile(file), IO.Path.GetFileName(file), file)
                Else
                    RaiseEvent SendFile(ReadFile(file), IO.Path.GetFileName(file), file)
                End If
            Next
        End If
    End Sub

    Private Sub ChatBox_PreviewDragEnter(sender As Object, e As DragEventArgs) Handles Me.PreviewDragEnter
        FileDropZone.Visibility = Visibility.Visible
    End Sub

    Private Sub ChatBox_PreviewDragLeave(sender As Object, e As DragEventArgs) Handles Me.PreviewDragLeave
        FileDropZone.Visibility = Visibility.Hidden
    End Sub

    Private Sub ChatBox_PreviewDrop(sender As Object, e As DragEventArgs) Handles Me.PreviewDrop
        FileDropZone.Visibility = Visibility.Hidden
    End Sub

    Private Sub ChatBox_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If _socket IsNot Nothing Then
            _socket.Shutdown()
        End If
    End Sub

#End Region
End Class
