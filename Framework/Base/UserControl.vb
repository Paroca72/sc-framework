'*************************************************************************************************
' 
' [SCFramework]
' UserControl  
' by Samuele Carassai
'
' Define a wrapper for the user control
' Version 5.0.0
' Created 14/10/2016
' Updated --/--/----
'
'*************************************************************************************************


Public Class UserControl
    Inherits Web.UI.UserControl

    Public Shadows Property Page As SCFramework.Page
        Get
            Return MyBase.Page
        End Get
        Set(value As SCFramework.Page)
            MyBase.Page = CType(value, Web.UI.Page)
        End Set
    End Property

End Class

