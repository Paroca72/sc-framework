'*************************************************************************************************
' 
' [SCFramework]
' LogTracer  
' by Samuele Carassai
'
' Trace the user access/action
' Version 5.0.0
' Created 14/10/2016
' Updated 20/10/2016
'
'*************************************************************************************************


Public Class Tracer
    Inherits SCFramework.DataSourceHelper

#Region " MUST OVERRIDES "

    ' Define the linked databse table name
    Public Overrides Function Name() As String
        Return "SYS_LOGTRACER"
    End Function

#End Region

#Region " PUBLIC "

    ' The actions type
    Public Enum Actions As Integer
        Unknown = 0
        Login = 1
    End Enum

    ' Trace a login action
    Public Sub RecLoginAttempt([Alias] As String, Password As String, Success As Boolean)
        ' Define the field values
        Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
        Values.Add("DATE", Date.Now)
        Values.Add("TYPE", Tracer.Actions.Login)
        Values.Add("SUCCESS", Success)
        Values.Add("ALIAS", [Alias])
        Values.Add("PASSWORD", Password)

        If Bridge.Request IsNot Nothing Then
            Values.Add("REFERRER", Bridge.Request.UserHostAddress)
        End If

        ' Call the base method
        MyBase.Insert(Values)
    End Sub

    ' Delete all trace in the history.
    ' Note that if you not disable the auto-safe before call this method it will throw an exception.
    Public Overloads Function Delete() As Long
        Return MyBase.Delete(DB.Clauses.AlwaysTrue)
    End Function

    ' Delete the trace log filtered by the type.
    Public Overloads Function Delete(ActionFilter As Tracer.Actions) As Long
        Return Me.Delete(New DB.Clauses("TYPE", DB.Clauses.Comparer.Equal, ActionFilter))
    End Function

#End Region

End Class
