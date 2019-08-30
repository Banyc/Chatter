Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

Public Class SocketListener
    Inherits SocketBase

    Private _expectedIp As IPAddress

    ' Incoming data from the client.  
    Private data As String = Nothing

    Public Sub New(ipStr As String, port As Integer, Optional expectedIpStr As String = Nothing)
        MyBase.New(ipStr, port, SocketCS.Server)

        If expectedIpStr = Nothing Then  ' it is wrong to express like `If expectedIpStr is Nothing Then`
            _expectedIp = Nothing
        Else
            _expectedIp = IPAddress.Parse(expectedIpStr)
        End If
    End Sub

    Public Overrides Sub BuildConnection()
        Dim buildThread As Task = New Task(
            Sub()
                Try
                    ' Connect to a remote device. 
                    MyBase._socket = Build(GetIp(), GetPort())

                Catch ex As SocketException
                    MessageBox.Show(ex.Message.ToString(), SocketCS.Server.ToString())
                    MyBase.Shutdown()
                End Try

                ' keep listenning until it established a legal connection
                While Not IsConnectorLegal(MyBase._socket, _expectedIp)
                    MyBase._socket.Dispose()

                    MyBase._socket = Build(GetIp(), GetPort())
                End While

                MyBase.ConnectDone()  ' enable the listenLoop
            End Sub)
        buildThread.Start()
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
        Dim server As Socket = Nothing
        ' Establish the local endpoint for the socket.  
        ' Dns.GetHostName returns the name of the   
        ' host running the application.  
        'Dim ipHostInfo As IPHostEntry = Dns.GetHostEntry(Dns.GetHostName())
        'Dim ipAddress As IPAddress = ipHostInfo.AddressList(0)
        'Dim localEndPoint As New IPEndPoint(ipAddress, 11000)
        Dim ipAddress As IPAddress = ip
        Dim localEndPoint As New IPEndPoint(ipAddress, port)

        ' Create a TCP/IP socket.  
        server = New Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp)

        ' makes restarting a socket become possible
        ' https://blog.csdn.net/limlimlim/article/details/23424855
        server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, True)

        ' Bind the socket to the local endpoint and   
        ' listen for incoming connections.  

        server.Bind(localEndPoint)
        server.Listen(10)

        ' Start listening for connections.  
        Console.WriteLine("Waiting for a connection...")

        ' Program is suspended while waiting for an incoming connection.  
        Return server.Accept()
    End Function

End Class 'SocketListener  
