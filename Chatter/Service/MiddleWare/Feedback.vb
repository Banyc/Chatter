Public Class Feedback
    Private _msgNotComfirmedList As Dictionary(Of Integer, AesLocalPackage)  ' stores sended messages  ' msgID : msgPackage

    ' checks the integrity and authenticity of the incoming message
    ' against replay attack
    Private _myMsgId As UInteger
    Private _othersMsgId As UInteger

    Public Sub New()
        _msgNotComfirmedList = New Dictionary(Of Integer, AesLocalPackage)

        Dim rnd As New Random()
        _myMsgId = CUInt(rnd.Next())
        _othersMsgId = Nothing
        rnd = Nothing
    End Sub

    ' NOTICE: This method must be called before `SendCipher`
    Public Sub SetMsgID_StoreMyMsg(localPack As AesLocalPackage)
        ' updates msg ID
        _myMsgId = UIntIncrement(_myMsgId)

        ' update msg ID
        localPack.AesContentPack.MessageID = _myMsgId

        ' save the outing message for later examine if the opposite indeed received it
        _msgNotComfirmedList.Add(localPack.AesContentPack.MessageID, localPack)
    End Sub

    Public Function ReceiveFeedback_PopMyMsg(msgID As Integer)
        Dim myMsg As AesLocalPackage = _msgNotComfirmedList(msgID)
        _msgNotComfirmedList.Remove(msgID)
        Return myMsg
    End Function

    Public Function ReceiveNewMsg_CheckIntegrity(aesPack As AesContentPackage)
        Return DoesMessageIntegrated(aesPack)
    End Function

    ' checks the integrity and authenticity of the incoming message
    Private Function DoesMessageIntegrated(aesPack As AesContentPackage) As Boolean
        If _othersMsgId = Nothing Then  ' update other's message id
            _othersMsgId = aesPack.MessageID
        Else
            Dim incrementedId As UInteger
            incrementedId = UIntIncrement(_othersMsgId)
            If incrementedId <> aesPack.MessageID Then  ' SYNs (previous msgID + 1 and the incoming msgID) do NOT match
                MessageBox.Show("previous msgID + 1 and the incoming msgID do NOT match!", "WARNING")
                Return False
            End If
            _othersMsgId = incrementedId
        End If
        Return True
    End Function

    Private Shared Function UIntIncrement(uInt As UInteger)
        If uInt = UInteger.MaxValue Then
            Return UInteger.MinValue
        Else
            Return uInt + 1
        End If
    End Function
End Class
