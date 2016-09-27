'*************************************************************************************************
' 
' [SCFramework]
' BaseMasterPage
' di Samuele Carassai
'
' Definisce l'accesso alle funzioni standard
' Versione 1.0.0
'
'------------------------------------------------------------------------------------------------
' // DIPENDENZE //
'
'   Classi: 
'       SCFramework.BasePage
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
