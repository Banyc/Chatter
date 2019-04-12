Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

Public Class SocketListener
    Inherits SocketBase

    Private _buildThread As Thread

    Private _listener As Socket  ' for shutdown

    Private _expectedIp As IPAddress

    ' Incoming data from the client.  
    Private data As String = Nothing

    Public Sub New(ipStr As String, port As Integer, expectedIpStr As String)
        MyBase.New(ipStr, port, SocketCS.Server)

        If expectedIpStr = Nothing Then  ' it is wrong to express like `If expectedIpStr is Nothing Then`
            _expectedIp = Nothing
        Else
            _expectedIp = IPAddress.Parse(expectedIpStr)
        End If
    End Sub

    Public Overrides Sub Start()
        _buildThread = New Thread(
            Sub()
                ' Connect to a remote device. 
                Dim server As Socket = Build(GetIp(), GetPort())

                ' keep listenning until it established a legal connection
                While Not IsConnectorLegal(server, _expectedIp)
                    server.Dispose()

                    server = Build(GetIp(), GetPort())
                End While

                SetHandler(server)
                ConnectDone()  ' enable the listenLoop

                _buildThread.Abort()
            End Sub)
        _buildThread.Start()
    End Sub

    Private Function IsConnectorLegal(server As Socket, expectedIp As IPAddress) As Boolean
        ' if `expectedIp` is nothing then server receive all attempted connections
        If expectedIp Is Nothing Then
            Return True
        End If

        Dim exactIp As IPAddress
        Dim ipEndPoint As IPEndPoint = server.RemoteEndPoint
        exactIp = ipEndPoint.Address

        If exactIp.ToString() = expectedIp.ToString() Then
            Return True
        Else
            Return False
        End If
    End Function

    Private Function Build(ipStr As String, port As Integer) As Socket
        Return Build(IPAddress.Parse(ipStr), port)
    End Function

    ' build and connect
    Private Function Build(ip As IPAddress, port As Integer) As Socket
        Dim handler As Socket = Nothing
        Try
            ' Establish the local endpoint for the socket.  
            ' Dns.GetHostName returns the name of the   
            ' host running the application.  
            'Dim ipHostInfo As IPHostEntry = Dns.GetHostEntry(Dns.GetHostName())
            'Dim ipAddress As IPAddress = ipHostInfo.AddressList(0)
            'Dim localEndPoint As New IPEndPoint(ipAddress, 11000)
            Dim ipAddress As IPAddress = ip
            Dim localEndPoint As New IPEndPoint(ipAddress, port)

            ' Create a TCP/IP socket.  
            _listener = New Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp)

            ' makes restarting a socket become possible
            ' https://blog.csdn.net/limlimlim/article/details/23424855
            _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, True)

            ' Bind the socket to the local endpoint and   
            ' listen for incoming connections.  

            _listener.Bind(localEndPoint)
            _listener.Listen(10)

            ' Start listening for connections.  
            Console.WriteLine("Waiting for a connection...")
            ' Program is suspended while waiting for an incoming connection.  
            handler = _listener.Accept()
        Catch ex As Exception
            MessageBox.Show(ex.ToString(), SocketCS.Server.ToString())
            Shutdown()
        End Try
        Return handler
    End Function

    Public Overrides Sub Shutdown()
        MyBase.Shutdown()
        If Not _listener Is Nothing Then
            _listener.Close()  ' to kill `_listener.Accept()`, freeing the thread
            _listener.Dispose()
        End If
        If _buildThread.IsAlive Then
            _buildThread.Abort()  ' if the thread is jammed, it cannot be aborted
        End If
    End Sub

End Class 'SocketListener  
