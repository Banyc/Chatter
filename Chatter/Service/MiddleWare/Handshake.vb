' The handshake between endpoints is for Key exchange (exchange AES session key)


' __Handshake Showcase__
' - first stage - to compose a complete session key
' A ==(A's partial session key)==> B
' B ==(B's partial session key)==> A
' - second stage - check if the opposite is an authentic user
' A ==(complete session key + 1)==> B
' B ==(complete session key + 2)==> A
' A ==(complete session key + 3)==> B


Public Class Handshake : Implements IHandshake
    Public Event DoneHandshake(aes As AesApi) Implements IHandshake.DoneHandshake
    Public Event Send(message As Byte()) Implements IHandshake.Send

    Private _handshakeTimes As Integer
    Private _AES As AesApi
    Private _RSA As RsaApi
    Private _DidSendSessionKey As Boolean
    Private _IsPartialKeysMerged As Boolean
    Private _DidILaunchKeyExchange As Boolean

    Public Sub New(seed As Integer, rsa As RsaApi)
        _handshakeTimes = 0
        _AES = New AesApi(seed)
        _RSA = rsa
        _DidSendSessionKey = False
        _IsPartialKeysMerged = False
        _DidILaunchKeyExchange = False
    End Sub

    Public Sub Start() Implements IHandshake.Start
        _DidILaunchKeyExchange = True
        ' encrypt session key
        Dim encryptedKey As Byte() = _RSA.EncryptMsg(_AES.GetSessionKey())
        ' send encrypted session key
        SendSessionKey(encryptedKey)
    End Sub

    Private Function ReceiveNewPartialSessionKey_GetCompleteSessionKey(decryptedKey As Byte()) As Byte()
        Dim bKey = _AES.GetSessionKey()

        ' merge the two byte arrays
        Dim i As Integer
        For i = 0 To bKey.Length - 1
            bKey(i) = bKey(i) Xor decryptedKey(i)
        Next

        ' hash bKey
        Dim sha256 As New Security.Cryptography.SHA256Managed()
        bKey = sha256.ComputeHash(bKey)

        _IsPartialKeysMerged = True
        Return bKey
    End Function

    ' received encrypted session key
    ' to decrypt the session key and check it and even save it if necessary
    Public Sub Receive(encryptedKey As Byte()) Implements IHandshake.Receive
        If Not _IsPartialKeysMerged Then
            Dim decryptedKey As Byte() = _RSA.DecryptMsg(encryptedKey)
            Dim myPartialSessionKey As Byte() = _AES.GetSessionKey()

            ' set the complete session key
            _AES.SetSessionKey(ReceiveNewPartialSessionKey_GetCompleteSessionKey(decryptedKey))

            If Not _DidILaunchKeyExchange Then
                ' encrypt session key
                Dim myPartialSessionKey_Encrypted As Byte() = _RSA.EncryptMsg(myPartialSessionKey)
                ' send encrypted session key
                SendSessionKey(myPartialSessionKey_Encrypted)
            Else
                SendTunnalRequest()
            End If

        Else ' check if the opposite is an authentic user  ' the second stage begins
            _handshakeTimes += 1

            Dim decryptedKey As Byte() = DecrementBytes(_RSA.DecryptMsg(encryptedKey), _handshakeTimes)

            If _DidSendSessionKey Then  ' which means you have already have the session key
                If decryptedKey.SequenceEqual(_AES.GetSessionKey()) Then
                    ' do nothing
                Else
                    _handshakeTimes -= 1
                    MessageBox.Show("Session key not matched!")
                    Throw New Exception("Session key not matched!")
                    Exit Sub
                End If
            Else
                _AES.SetSessionKey(decryptedKey)
                MessageBox.Show("Your seed for seesion key is not in effect", "Warning")
                ' send later
            End If

            If _handshakeTimes >= 3 Then
                RaiseEvent DoneHandshake(_AES)
            Else
                SendTunnalRequest()
            End If
        End If
    End Sub

    ' check if the opposite is an authentic user
    ' belongs to the second handshake stage
    Private Sub SendTunnalRequest()
        If _RSA.HasPubKey Then
            _handshakeTimes += 1

            ' encrypt session key
            Dim encryptedKey As Byte() = _RSA.EncryptMsg(IncrementBytes(_AES.GetSessionKey(), _handshakeTimes))
            ' send encrypted session key
            SendSessionKey(encryptedKey)

        Else
            Throw New Exception("Public key is missing when key exchange")
        End If

        If _handshakeTimes >= 3 Then
            RaiseEvent DoneHandshake(_AES)
            ' else wait for the opposite sending here a hand shake
        End If
    End Sub

    Private Sub SendSessionKey(encryptedSessionKey As Byte())
        _DidSendSessionKey = True
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
