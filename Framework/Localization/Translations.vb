'*************************************************************************************************
' 
' [SCFramework]
' Translations
' di Samuele Carassai
'
' Class to manage the translations.
' Inherits from the Multilanguages class and following that concept in particular this is 
' specialized to manage translation in multilanguages, different text value by the language code.
'
' Version 5.0.0
' Updated 29/10/2015
'
'*************************************************************************************************


Public Class Translations
    Inherits SCFramework.Multilanguages

#Region " OVERRIDES "

    Public Overrides Function Name() As String
        Return "SYS_TRANSLATIONS"
    End Function

#End Region

#Region " PUBLIC "

    ' Get the translation in language.
    ' This method is same as GetValue and made only for coerence with translation class.
    Public Function GetTranslation(Key As String, Language As String) As String
        ' Filter the source by the key and language
        Return Me.GetValue(Key, Language)
    End Function

    ' Get all the translations in language.
    ' This method is same as GetValues and made only for coerence with translation class.
    Public Function GetTranslations(Key As String) As Dictionary(Of String, String)
        ' Filter the source by the key
        Return Me.GetValues(Key)
    End Function

#End Region

End Class
