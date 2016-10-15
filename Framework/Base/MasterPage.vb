'*************************************************************************************************
' 
' [SCFramework]
' MasterPage  
' by Samuele Carassai
'
' Define a wrapper for the matser page
' Version 5.0.0
' Created 14/10/2016
' Updated --/--/----
'
'*************************************************************************************************


Public Class MasterPage
    Inherits System.Web.UI.MasterPage

    Public Shadows Property Page As SCFramework.Page
        Get
            Return MyBase.Page
        End Get
        Set(value As SCFramework.Page)
            MyBase.Page = CType(value, System.Web.UI.Page)
        End Set
    End Property

End Class
