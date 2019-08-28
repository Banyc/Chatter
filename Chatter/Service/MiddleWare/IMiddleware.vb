Public Interface IMiddleware
    Sub Receive(message As Byte())
    Event Send(message As Byte())
End Interface
