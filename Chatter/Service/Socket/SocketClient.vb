Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

Public Class SocketClient
    Inherits SocketBase

    Private _connectThread As Thread

    Public Sub New(ipStr As String, port As Integer)
        MyBase.New(ipStr, port, SocketCS.Client)
    End Sub

    Public Overrides Sub BuildConnection()
        _connectThread = New Thread(
            Sub()
                ' Connect to a remote device. 
                Dim client As Socket = Connect(GetIp(), GetPort())
                SetSocket(client)
                ConnectDone()  ' enable the listenLoop
                _connectThread.Abort()
            End Sub)
        _connectThread.Start()
    End Sub

    Private Function Connect(ipStr As String, port As Integer) As Socket
        Return Connect(IPAddress.Parse(ipStr), port)
    End Function

    ' Connect to a remote device.  
    Private Function Connect(ip As IPAddress, port As Integer) As Socket
        Dim sender As Socket
        Dim remoteEP As IPEndPoint = Nothing
        Try
            ' Establish the remote endpoint for the socket.  
            ' This example uses port 11000 on the local computer.  
            'Dim ipHostInfo As IPHostEntry = Dns.GetHostEntry(Dns.GetHostName())
            'Dim ipAddress As IPAddress = ipHostInfo.AddressList(0)
            'Dim remoteEP As New IPEndPoint(ipAddress, 11000)
            Dim ipAddress As IPAddress = ip
            remoteEP = New IPEndPoint(ipAddress, port)

            ' Create a TCP/IP socket.  
            sender = New Socket(ipAddress.AddressFamily,
        SocketType.Stream, ProtocolType.Tcp)
        Catch ex As Exception
            MessageBox.Show(ex.ToString(), EndPointType.ToString())
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