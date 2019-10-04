Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq


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
