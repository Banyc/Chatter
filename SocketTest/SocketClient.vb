Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

Public Class SocketClient
    Inherits SocketBase

    Private _connectThread As Thread

    Public Sub New(ipStr As String, port As Integer)
        MyBase.New(SocketCS.Client)

        _connectThread = New Thread(
            Sub()
                ' Connect to a remote device. 
                Dim client As Socket = Connect(ipStr, port)
                SetHandler(client)
                ConnectDone()  ' enable the listenLoop
                _connectThread.Abort()
            End Sub)
        _connectThread.Start()
    End Sub

    ' Connect to a remote device.  
    Private Function Connect(ipStr As String, port As Integer) As Socket
        Dim sender As Socket
        Dim remoteEP As IPEndPoint
        Try
            ' Establish the remote endpoint for the socket.  
            ' This example uses port 11000 on the local computer.  
            'Dim ipHostInfo As IPHostEntry = Dns.GetHostEntry(Dns.GetHostName())
            'Dim ipAddress As IPAddress = ipHostInfo.AddressList(0)
            'Dim remoteEP As New IPEndPoint(ipAddress, 11000)
            Dim ipAddress As IPAddress = IPAddress.Parse(ipStr)
            remoteEP = New IPEndPoint(ipAddress, port)

            ' Create a TCP/IP socket.  
            sender = New Socket(ipAddress.AddressFamily,
        SocketType.Stream, ProtocolType.Tcp)
        Catch ex As Exception
            MessageBox.Show(ex.ToString(), SocketCS.Client.ToString())
            Shutdown()
        End Try

        While Not sender.Connected
            Try
                ' Connect the socket to the remote endpoint.  
                sender.Connect(remoteEP)
            Catch ex As Exception

            End Try
        End While

        Console.WriteLine("Socket connected to {0}",
                          sender.RemoteEndPoint.ToString())

        Return sender
    End Function

    Public Overrides Sub Shutdown()
        MyBase.Shutdown()
        If _connectThread.IsAlive Then
            _connectThread.Abort()
        End If
    End Sub

End Class 'SocketClient