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
    Inherits SCFramework.DataSourceHelper

#Region " OVERRIDES "

    Public Overrides Function GetTableName() As String
        Return "SYS_TRANSLATIONS"
    End Function

#End Region

#Region " PRIVATE "


#End Region

#Region " DATABASE INTERFACE "

    ' Add column language to the table
    Public Sub AddLanguageColumn(LanguageCode As String, Optional Query As SCFramework.DbQuery = Nothing)
        ' Check if the column already exists
        If Not SCFramework.Utils.String.IsEmptyOrWhite(LanguageCode) And
            Not Me.WritableColumns.Contains(LanguageCode) Then
            ' Check for the query manager object
            If Query Is Nothing Then Query = Me.Query

            ' Alter the table and add to the writable columns
            Query.Exec(String.Format("ALTER TABLE [{0}] ADD [{1}] NVARCHAR(MAX)", Me.GetTableName(), LanguageCode))
            Me.WritableColumns.Add(LanguageCode)
        End If
    End Sub

    ' Add column language to the table
    Public Sub DropLanguageColumn(LanguageCode As String, Optional Query As SCFramework.DbQuery = Nothing)
        ' Check if the column exists
        If Not SCFramework.Utils.String.IsEmptyOrWhite(LanguageCode) And
            Me.WritableColumns.Contains(LanguageCode) Then
            ' Check for the query manager object
            If Query Is Nothing Then Query = Me.Query

            ' Alter the table and add to the writable columns
            Query.Exec(String.Format("ALTER TABLE [{0}] DROP COLUMN [{1}]", Me.GetTableName(), LanguageCode))
            Me.WritableColumns.Remove(LanguageCode)
        End If
    End Sub

#End Region

End Class
