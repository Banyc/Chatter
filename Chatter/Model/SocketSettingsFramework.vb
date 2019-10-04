Imports Newtonsoft.Json

Public Class SocketSettingsFramework
    Public Property ID As Integer = -1  ' -1 indicates that it is a new settings
    Public Property Name As String
    Public Property IP As String
    Public Property Port As Integer
    Public Property ExpectedIP As String
    Public Property Role As SocketCS

    <JsonIgnore>
    Public Property Seed As String
    Public Property PublicKeyPath As String
    Public Property PrivateKeyPath As String

    Public Overrides Function ToString() As String
        If Me.Name <> "" Then
            Return Me.Name
        Else
            Return Me.IP
        End If
    End Function
End Class
