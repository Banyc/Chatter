Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

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

' Containing procedures that convert between AesContentPackage and JSON
Public Class AesContentFraming
    Private Sub New()
        ' Preventing the class from being instantiated
    End Sub
#Region "On Sending"
    Public Shared Function GetJsonString(AesContentPackage As AesContentPackage) As String
        ' Set identifier for json
        Dim jsonSettings = New JsonSerializerSettings()
        jsonSettings.TypeNameHandling = TypeNameHandling.None

        Dim jsonStr As String = JsonConvert.SerializeObject(AesContentPackage, jsonSettings)
        Return jsonStr
        'Return Text.Encoding.UTF8.GetBytes(jsonStr)
    End Function
#End Region

#Region "On Receiving"
    Public Shared Function GetAesContentPackage(bytes As Byte()) As AesContentPackage
        Dim jsonStr As String = Text.Encoding.UTF8.GetString(bytes)
        Return GetAesContentPackage(jsonStr)
    End Function

    Public Shared Function GetAesContentPackage(jsonStr As String) As AesContentPackage
        ' Set identifier for json
        Dim jsonSettings = New JsonSerializerSettings()
        jsonSettings.TypeNameHandling = TypeNameHandling.None

        ' make an object of json
        Dim jo As JObject = JObject.Parse(jsonStr)

        ' distribute
        Dim aesContentPack As AesContentPackage
        Select Case jo("Kind").Value(Of Int64)
            Case AesContentKind.Feedback
                aesContentPack = JsonConvert.DeserializeObject(Of AesFeedbackPackage)(jsonStr, jsonSettings)
            Case AesContentKind.File
                aesContentPack = JsonConvert.DeserializeObject(Of AesFilePackage)(jsonStr, jsonSettings)
            Case AesContentKind.Image
                aesContentPack = JsonConvert.DeserializeObject(Of AesImagePackage)(jsonStr, jsonSettings)
            Case AesContentKind.Text
                aesContentPack = JsonConvert.DeserializeObject(Of AesTextPackage)(jsonStr, jsonSettings)
            Case Else
                Throw New Exception("Unknown message received")
        End Select

        Return aesContentPack
    End Function
#End Region
End Class
