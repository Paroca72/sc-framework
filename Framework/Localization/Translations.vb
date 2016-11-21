'*************************************************************************************************
' 
' [SCFramework]
' Translations
' di Samuele Carassai
'
' Classe di gestione lingue
' Version 5.0.0
' Created --/--/----
' Updated 29/10/2015
'
'*************************************************************************************************


Public Class Translations
    Inherits SCFramework.Multilanguages

#Region " OVERRIDES "

    Public Overrides Function GetTableName() As String
        Return "SYS_TRANSLATIONS"
    End Function

#End Region

#Region " PUBLIC "

    ' Get the translation in language.
    ' This method is same as GetValue and made only for coerence with translation class.
    Public Function GetTranslation(Key As String, Language As String) As String
        Return Me.GetValue(Key, Language)
    End Function

#End Region

End Class
