#Region "Serial Classes - Not-sending packages"
' the content package here contains private data
' only used to temperately store the package in local
Public Class AesLocalPackage
    Public ReadOnly Property AesContentPack As AesContentPackage
    Public Sub New(aesPack As AesContentPackage)
        _AesContentPack = aesPack
    End Sub
End Class
Public Class AesLocalFilePackage : Inherits AesLocalPackage
    Public ReadOnly Property FilePath As String
    Public Sub New(aesPack As AesContentPackage, theFilePath As String)
        MyBase.New(aesPack)
        _FilePath = theFilePath
    End Sub
End Class
#End Region
