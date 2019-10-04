Public Interface IFeedBack
    Sub StoreMyMsg(localPack As AesLocalPackage)
    Function PopMyMsg(msgID As Integer) As AesLocalPackage
    Function CheckNewMsgIntegrity(aesPack As AesContentPackage) As Boolean
End Interface
