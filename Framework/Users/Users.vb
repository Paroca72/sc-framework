'*************************************************************************************************
' 
' [SCFramework]
' di Samuele Carassai
'
' Users manager (new from the version 5.x)
' Versione 5.0.0
' Created --/--/----
'
'*************************************************************************************************


' TODO: implements
Public Class Users
    Inherits DbHelper

#Region " CONSTRUCTOR "

    Public Overrides Function GetTableName() As String
        Return "SYS_USERS"
    End Function

#End Region


End Class
