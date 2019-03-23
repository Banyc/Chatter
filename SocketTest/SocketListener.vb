Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

Public Class SocketListener
    Inherits SocketBase

    Private _buildThread As Thread

    ' Incoming data from the client.  
    Private data As String = Nothing

    Public Sub New(ipStr As String, port As Integer)
        MyBase.New(SocketCS.Server)

        _buildThread = New Thread(
            Sub()
                ' Connect to a remote device. 
                Dim server As Socket = Build(ipStr, port)
                SetHandler(server)
                ConnectDone()  ' enable the listenLoop
                _buildThread.Abort()
            End Sub)
        _buildThread.Start()
    End Sub

    Private Function Build(ipStr As String, port As Integer)
        ' Establish the local endpoint for the socket.  
        ' Dns.GetHostName returns the name of the   
        ' host running the application.  
        'Dim ipHostInfo As IPHostEntry = Dns.GetHostEntry(Dns.GetHostName())
        'Dim ipAddress As IPAddress = ipHostInfo.AddressList(0)
        'Dim localEndPoint As New IPEndPoint(ipAddress, 11000)
        Dim ipAddress As IPAddress = IPAddress.Parse(ipStr)
        Dim localEndPoint As New IPEndPoint(ipAddress, port)

        ' Create a TCP/IP socket.  
        Dim listener As New Socket(ipAddress.AddressFamily,
        SocketType.Stream, ProtocolType.Tcp)

        ' Bind the socket to the local endpoint and   
        ' listen for incoming connections.  

        listener.Bind(localEndPoint)
        listener.Listen(10)

        ' Start listening for connections.  
        Console.WriteLine("Waiting for a connection...")
        ' Program is suspended while waiting for an incoming connection.  
        Dim handler As Socket = listener.Accept()


        Return handler
    End Function

    Public Overrides Sub Shutdown()
        MyBase.Shutdown()
        If _buildThread.IsAlive Then
            _buildThread.Abort()
        End If
    End Sub

End Class 'SynchronousSocketListener  
