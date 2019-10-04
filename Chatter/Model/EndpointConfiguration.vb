Imports System.Collections.ObjectModel
Imports Newtonsoft.Json

'Public Class EndpointSavingConfig
'    Public Property ID As Integer
'    Public Property Name As String
'    Public Property IP As String
'    Public Property Port As Integer
'    Public Property ExpectedIP As String
'    Public Property Role As SocketCS
'    Public Property PublicKeyPath As String
'    Public Property PrivateKeyPath As String

'    Public Function ToSettings() As SocketSettingsFramework
'        Dim frame As SocketSettingsFramework = New SocketSettingsFramework
'        frame.ExpectedIP = Me.ExpectedIP
'        frame.IP = Me.IP
'        frame.Port = Me.Port
'        frame.Role = Me.Role
'        frame.PublicKeyPath = Me.PublicKeyPath
'        frame.PrivateKeyPath = Me.PrivateKeyPath
'        Return frame
'    End Function
'End Class

Public Class EndpointConfiguration
    Private Const _savingPath As String = "./Endpoint.config"
    Public Property List As List(Of SocketSettingsFramework)

    Private Sub New()
        Me.List = New List(Of SocketSettingsFramework)
    End Sub

    Public Shared Function Load() As EndpointConfiguration
        If Not IO.File.Exists(_savingPath) Then
            IO.File.CreateText(_savingPath).Close()
            Return New EndpointConfiguration()
        Else
            Dim jsonStr As String = IO.File.ReadAllText(_savingPath)
            Dim endpointConfig As EndpointConfiguration = New EndpointConfiguration()
            Dim loadedList As List(Of SocketSettingsFramework) = JsonConvert.DeserializeObject(Of List(Of SocketSettingsFramework))(jsonStr)
            If loadedList IsNot Nothing Then
                endpointConfig.List = loadedList
            End If
            Return endpointConfig
        End If
    End Function

    Public Sub Save()
        Dim jsonStr As String = JsonConvert.SerializeObject(Me.List)
        IO.File.WriteAllText(_savingPath, jsonStr)
    End Sub

    Public Sub Add(socketSettingsFramework As SocketSettingsFramework)
        socketSettingsFramework.Seed = ""

        If socketSettingsFramework.ID >= 0 Then  ' replace
            Me.List(Me.List.IndexOf(FindById(socketSettingsFramework.ID))) = socketSettingsFramework
        Else  ' create
            If Me.List.Count = 0 Then
                socketSettingsFramework.ID = 0
            Else
                socketSettingsFramework.ID = Me.List.Last().ID + 1
            End If

            Me.List.Add(socketSettingsFramework)
        End If
    End Sub

    Public Sub Delete(socketSettingsFramework As SocketSettingsFramework)
        Me.List.Remove(socketSettingsFramework)
    End Sub

    Public Function FindById(id As Integer) As SocketSettingsFramework
        Dim endpoint As SocketSettingsFramework
        endpoint = Me.List.ToList().Find(Function(x As SocketSettingsFramework) x.ID = id)
        Return endpoint
    End Function
End Class
