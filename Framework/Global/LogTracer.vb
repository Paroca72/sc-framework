'*************************************************************************************************
' 
' [SCFramework]
' LogTracer  
' by Samuele Carassai
'
' Trace the user access/action
' Version 5.0.0
' Created 14/10/2016
' Updated 15/10/2016
'
'*************************************************************************************************


Public Class LogTracer
    Inherits SCFramework.DbHelper

#Region " MUST OVERRIDES "

    ' Define the linked databse table name
    Public Overrides Function GetTableName() As String
        Return "SYS_LOGTRACER"
    End Function

#End Region

#Region " PUBLIC "

    ' The actions type
    Public Enum Actions As Integer
        Unknown = 0
        Login = 1
    End Enum

    ' Get the source
    Public Function GetSource(Optional Clauses As DbClauses = Nothing) As DataTable
        ' Source
        Dim Source As DataTable = Bridge.Query.Table(Me.GetTableName(), Nothing, Clauses)
        Source.CaseSensitive = False
        Source.Locale = CultureInfo.InvariantCulture

        ' Set the table column properties
        If Me.PrimaryKeys.Count > 0 Then SCFramework.Utils.DataTable.SetPrimaryKeys(Source, Me.PrimaryKeys.ToArray)
        If Me.AutoNumbers.Count > 0 Then SCFramework.Utils.DataTable.SetAutoIncrements(Source, Me.AutoNumbers.ToArray)

        ' Return the filtered table
        Return Source
    End Function


    ' Trace a login action
    Public Sub RecLoginAttempt([Alias] As String, Password As String, Success As Boolean)
        ' Define the field values
        Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
        Values.Add("DATE", Date.Now)
        Values.Add("TYPE", LogTracer.Actions.Login)
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
    Public Shadows Sub Delete()
        MyBase.Delete(Nothing)
    End Sub

    ' Delete the trace log filtered by the type.
    Public Shadows Sub Delete(Filter As LogTracer.Actions)
        MyBase.Delete(New SCFramework.DbClauses("TYPE", Filter))
    End Sub

#End Region

End Class
