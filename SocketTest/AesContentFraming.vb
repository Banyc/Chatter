Imports Newtonsoft.Json

Public Enum AesContentKind
    Text
    Image  ' TODO
    Feedback
End Enum

#Region "Serial Classes"
Public MustInherit Class AesContentPackage
    Public Property Kind As AesContentKind
End Class

Public Class AesFeedbackPackage
    Inherits AesContentPackage
    Public Property MessageID As Integer
    Public Sub New()
        Kind = AesContentKind.Feedback
    End Sub
End Class

Public Class AesTextPackage
    Inherits AesContentPackage
    Public Property MessageID As Integer
    Public Property Text As String
    Public Sub New()
        Kind = AesContentKind.Text
    End Sub
End Class
#End Region

Public Class AesContentFraming
    Private Sub New()
        ' Preventing the class from being instantiated
    End Sub
#Region "On Sending"
    Public Shared Function GetJsonString(AesContentPackage As AesContentPackage) As String
        ' Set identifier for json
        Dim jsonSettings = New JsonSerializerSettings()
        jsonSettings.TypeNameHandling = TypeNameHandling.Objects

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

    Public Shared Function GetAesContentPackage(json As String) As AesContentPackage
        ' Set identifier for json
        Dim jsonSettings = New JsonSerializerSettings()
        jsonSettings.TypeNameHandling = TypeNameHandling.Objects

        Return JsonConvert.DeserializeObject(Of AesContentPackage)(json, jsonSettings)
    End Function
#End Region
End Class
