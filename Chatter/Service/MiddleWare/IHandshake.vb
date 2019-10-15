Public Interface IHandshake
    Sub Receive(message As Byte())
    Sub ReceiveRsaException()
    Sub Start()
    Event Send(message As Byte())
    Event DoneHandshake(aes As AesApi)
    Event Failed(ex As Exception)
End Interface
