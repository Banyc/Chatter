Public Interface IHandshake
    Sub Receive(message As Byte())
    Sub Start()
    Event Send(message As Byte())
    Event DoneHandshake(aes As AesApi)
End Interface
