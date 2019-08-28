' The handshake between endpoints is for Key exchange (exchange AES session key)

Public Class Handshake : Implements IMiddleware
    Public Event DoneHandshake(aes As AesApi)
    Public Event Send(message As Byte()) Implements IMiddleware.Send

    Private _handshakeTimes As Integer
    Private _AES As AesApi
    Private _RSA As RsaApi
    Private _DoesSendSessionKey As Boolean

    Public Sub New(seed As Integer, rsa As RsaApi)
        _handshakeTimes = 0
        _AES = New AesApi(seed)
        _RSA = rsa
        _DoesSendSessionKey = False
    End Sub

    Public Sub Start()
        SendTunnalRequest()
    End Sub

    ' received encrypted session key
    ' to decrypt the session key and check it and even save it if necessary
    Public Sub Receive(encryptedKey As Byte()) Implements IMiddleware.Receive
        _handshakeTimes += 1

        Dim decryptedKey As Byte() = DecrementBytes(_RSA.DecryptMsg(encryptedKey), _handshakeTimes)

        If _DoesSendSessionKey Then
            If decryptedKey.SequenceEqual(_AES.GetSessionKey()) Then
                ' do nothing
            Else
                _handshakeTimes -= 1
                MessageBox.Show("Session key not matched!")
                Throw New Exception("Session key not matched!")
                Exit Sub
            End If
        Else
            _AES = New AesApi(0)
            _AES.SetSessionKey(decryptedKey)

            ' send later
        End If

        If _handshakeTimes >= 3 Then
            RaiseEvent DoneHandshake(_AES)
        Else
            SendTunnalRequest()
        End If
    End Sub

    Private Sub SendTunnalRequest()
        If _RSA.HasPubKey Then
            _handshakeTimes += 1

            _DoesSendSessionKey = True  ' the first hand shake is still considered

            ' encrypt session key
            Dim encryptedKey As Byte() = _RSA.EncryptMsg(IncrementBytes(_AES.GetSessionKey(), _handshakeTimes))
            ' send encrypted session key
            SendSessionKey(encryptedKey)

        End If

        If _handshakeTimes >= 3 Then
            RaiseEvent DoneHandshake(_AES)
            ' else wait for the opposite sending here a hand shake
        End If
    End Sub

    Private Sub SendSessionKey(encryptedSessionKey As Byte())
        Dim msgBytes As Byte() = MessageFraming.SendEncryptedSessionKey(encryptedSessionKey)
        RaiseEvent Send(msgBytes)
    End Sub

    Private Shared Function IncrementBytes(ByVal bVal As Byte(), ByVal increment As Integer) As Byte()
        'Dim carry As Integer = 0
        Dim i As Integer
        For i = 0 To bVal.Length - 1
            'bVal(i) = (bVal(i) + carry + increment) Mod Byte.MaxValue
            bVal(i) = (bVal(i) + increment) Mod Byte.MaxValue
            'If bVal(i) + increment + carry <= Byte.MaxValue Then
            If bVal(i) + increment <= Byte.MaxValue Then
                Exit For
                'Else
                '    carry = 1
            End If
            increment /= Byte.MaxValue
        Next i
        Return bVal
    End Function

    Private Shared Function DecrementBytes(ByVal bVal As Byte(), ByVal decrement As Integer) As Byte()
        'Dim borrow As Integer = 0
        Dim i As Integer
        For i = 0 To bVal.Length - 1
            'Dim tmp As Integer = (bVal(i) - decrement - borrow)
            Dim tmp As Integer = (bVal(i) - decrement)
            While tmp < Byte.MinValue
                tmp = tmp + Byte.MaxValue
            End While
            bVal(i) = tmp
            'If bVal(i) - decrement - borrow >= Byte.MinValue Then
            If bVal(i) - decrement >= Byte.MinValue Then
                Exit For
                'Else
                '    borrow = 1
            End If
            decrement /= Byte.MaxValue
        Next i
        Return bVal
    End Function

End Class
