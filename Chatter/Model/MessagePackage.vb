' Serializer

' key-value types:
' ```
' enum Kind
'     cipher
'     plaintext
'     image
' content
' ```

Public Enum MessageKind
    PlaintextSignal  ' .Content encoded by UTF8
    Cipher  ' .Content encoded by AesApi from object `As UTF8.GetBytes(AesContentPackage)`
    Plaintext  ' .Content encoded by UTF8
    EncryptedSessionKey
End Enum

Public Enum MessagePlaintextSignal
    Standby
End Enum

#Region "Serial Classes"
Friend MustInherit Class MessagePackage
    'Friend MustInherit Class MessagePackage(Of T)
    Public ReadOnly Property Kind As MessageKind
    'Public MustOverride Property Content As T

    Public Sub New(kind As MessageKind)
        _Kind = kind
    End Sub
End Class

Friend Class CipherMessagePackage
    'Inherits MessagePackage(Of Byte())
    Inherits MessagePackage
    'Public Overrides Property Content As Byte()  ' NOTE: `As UTF8.GetBytes(AesContentPackage)`
    Public Property Content As Byte()  ' NOTE: `As UTF8.GetBytes(AesContentPackage)`
    Public Sub New()
        MyBase.New(MessageKind.Cipher)
    End Sub
End Class
Friend Class PlaintextMessagePackage
    'Inherits MessagePackage(Of String)
    Inherits MessagePackage
    'Public Overrides Property Content As String
    Public Property Content As String
    Public Sub New()
        MyBase.New(MessageKind.Plaintext)
    End Sub
End Class
Friend Class PlaintextSignalMessagePackage
    'Inherits MessagePackage(Of MessagePlaintextSignal)
    Inherits MessagePackage
    'Public Overrides Property Content As MessagePlaintextSignal
    Public Property Content As MessagePlaintextSignal
    Public Sub New()
        MyBase.New(MessageKind.PlaintextSignal)
    End Sub
End Class

Friend Class EncryptedSessionKeyMessagePackage
    'Inherits MessagePackage(Of Byte())
    Inherits MessagePackage
    'Public Overrides Property Content As Byte()
    Public Property Content As Byte()
    Sub New()
        MyBase.New(MessageKind.EncryptedSessionKey)
    End Sub
End Class
#End Region
