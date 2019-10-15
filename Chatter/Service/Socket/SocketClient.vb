Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

Public Class SocketClient
    Inherits SocketBase

    Public Sub New(ipStr As String, port As Integer)
        MyBase.New(ipStr, port, SocketCS.Client)
    End Sub

    Public Overrides Sub BuildConnection()
        Try
            ' Connect to a remote device. 
            MyBase._socket = Connect(GetIp(), GetPort())

        Catch ex As SocketException
            MessageBox.Show(ex.Message.ToString(), EndPointType.ToString())
            MyBase.Shutdown()
        End Try

        ConnectDone()  ' enable the listenLoop
    End Sub

    Private Function Connect(ipStr As String, port As Integer) As Socket
        Return Connect(IPAddress.Parse(ipStr), port)
    End Function

    ' Connect to a remote device.  
    Private Function Connect(ip As IPAddress, port As Integer) As Socket
        Dim client As Socket
        Dim remoteEP As IPEndPoint

        ' Establish the remote endpoint for the socket.  
        ' This example uses port 11000 on the local computer.  
        'Dim ipHostInfo As IPHostEntry = Dns.GetHostEntry(Dns.GetHostName())
        'Dim ipAddress As IPAddress = ipHostInfo.AddressList(0)
        'Dim remoteEP As New IPEndPoint(ipAddress, 11000)
        Dim ipAddress As IPAddress = ip
        remoteEP = New IPEndPoint(ipAddress, port)

        ' Create a TCP/IP socket.  
        client = New Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp)

        While Not client.Connected
            Try
                ' Connect the socket to the remote endpoint.  
                client.Connect(remoteEP)
            Catch ex As SocketException

            End Try
        End While

        Console.WriteLine("[Client] Socket connected to {0}",
                          client.RemoteEndPoint.ToString())
        Return client
    End Function

End Class 'SocketClient