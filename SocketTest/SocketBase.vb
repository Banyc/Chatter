Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading


Public Class SocketBase
    Protected Enum SocketCS
        Client
        Server
    End Enum

    Private _socketCS As SocketCS

    Private _handler As Socket = Nothing

    ' ManualResetEvent instances signal completion.  
    Private _connectDone As New ManualResetEvent(False)

    Private _listenThread As Thread

    ' Incoming data from the client.  
    Private _data As String = Nothing

    Private Const ENDOFSTREAM As String = "<EOF>"

    Protected Sub New(socketCS As SocketCS)
        _socketCS = socketCS
        ListenLoop()
    End Sub

    Public Sub Send(msgStr As String)
        Send(_handler, msgStr)
    End Sub

    Private Sub Send(handler As Socket, msgStr As String)

        ' Encode the data string into a byte array.
        Dim msg As Byte() = Encoding.ASCII.GetBytes(msgStr & ENDOFSTREAM)
        Send(handler, msg)

    End Sub

    Private Sub Send(handler As Socket, msg As Byte())
        'If Not handler Is Nothing And handler.Connected Then
        If Not handler Is Nothing Then
            If handler.Connected Then
                Dim thread As New Thread(
                    Sub()
                        _connectDone.WaitOne()
                        ' Send the data through the socket.  
                        Dim bytesSent As Integer = handler.Send(msg)
                        thread.Abort()
                    End Sub)
                thread.Start()
            End If
        End If
    End Sub

    Private Sub ListenLoop()
        _listenThread = New Thread(
            Sub()
                While True
                    _connectDone.WaitOne()  ' the handler has done the connection
                    Receive(_handler)
                End While
            End Sub)
        _listenThread.Start()
    End Sub

    Private Sub Receive(handler As Socket)

        If Not handler Is Nothing Then
            ' Data buffer for incoming data.  
            Dim bytes() As Byte = New [Byte](1024) {}
            '_data = Nothing

            ' An incoming connection needs to be processed.  
            While True
                Try
                    Dim bytesRec As Integer = handler.Receive(bytes)
                    _data += Encoding.ASCII.GetString(bytes, 0, bytesRec)
                    If _data.EndsWith(ENDOFSTREAM) Then
                        Dim cookedData = _data.Substring(0, _data.Length - ENDOFSTREAM.Length)
                        MessageBox.Show(cookedData)
                        _data = Nothing
                        Exit While
                    End If
                Catch ex As Exception
                    MessageBox.Show(ex.ToString(), _socketCS.ToString())
                    Shutdown()
                    Exit While
                End Try
            End While
        End If

    End Sub

    Public Overridable Sub Shutdown()
        Shutdown(_handler)
    End Sub

    Private Sub Shutdown(handler As Socket)
        If Not _listenThread Is Nothing Then
            If _listenThread.IsAlive Then
                _listenThread.Abort()
            End If
        End If

        If Not handler Is Nothing Then
            ' Release the socket.  
            handler.Shutdown(SocketShutdown.Both)
            handler.Close()
        End If

        MessageBox.Show("Shutdowned", _socketCS.ToString())
    End Sub

    Protected Function GetHandler() As Socket
        Return _handler
    End Function

    Protected Sub SetHandler(handler As Socket)
        _handler = handler
    End Sub

    Protected Sub ConnectDone()
        _connectDone.Set()
        Dim thread As New Thread(
            Sub()
                MessageBox.Show(String.Format("RemoteEndPoint:" & vbCrLf & "{0}", _handler.RemoteEndPoint.ToString()), _socketCS.ToString() & ", " & "Connect Done")
                thread.Abort()
            End Sub)
        thread.Start()
    End Sub
End Class
