Public Enum AesContentKind
    Text
    Image  ' Abandoned. Merged into `File`
    File
    Feedback
End Enum


#Region "Serial Classes - send-out packages"
Public MustInherit Class AesContentPackage
    Public ReadOnly Property Kind As AesContentKind
    Public Property MessageID As Integer

    Public Sub New(kind As AesContentKind)
        _Kind = kind
    End Sub
End Class

Public Class AesFeedbackPackage : Inherits AesContentPackage
    Public Sub New()
        MyBase.New(AesContentKind.Feedback)
    End Sub
End Class

Public Class AesTextPackage
    Inherits AesContentPackage
    Public Property Text As String
    Public Sub New()
        MyBase.New(AesContentKind.Text)
    End Sub
End Class

Public Class AesFilePackage : Inherits AesContentPackage
    Public Property FileBytes As Byte()
    Public Property Name As String
    Public Sub New()
        MyBase.New(AesContentKind.File)
    End Sub
End Class

Public Class AesImagePackage : Inherits AesContentPackage
    Public Property ImageBytes As Byte()
    Public Sub New()
        MyBase.New(AesContentKind.Image)
    End Sub
End Class
#End Region
