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

#Region " STATIC "

    ' Static instance holder
    Private Shared mInstance As Translations = Nothing

    ' Instance property
    Public Shared ReadOnly Property Instance As Translations
        Get
            ' Check if null
            If Translations.mInstance Is Nothing Then
                Translations.mInstance = New Translations()
            End If

            ' Return the static instance
            Return Translations.mInstance
        End Get
    End Property

#End Region

#Region " CONSTRUCTOR AND OVERRIDES "

    Public Sub New()
        ' Get the datatable
        Me.DataSource = Me.GetSource()
    End Sub

    Public Overrides Function GetTableName() As String
        Return "SYS_TRANSLATIONS"
    End Function

#End Region

#Region " PRIVATE "


#End Region

#Region " DATABASE INTERFACE "

    ' Add a new record in the database
    Private Shared Sub AddToDatabase(Label As String, Values As Hashtable, IsTemporary As Boolean)
        ' Create the fields and values collection
        Dim StrFields As String = String.Empty
        Dim StrValues As String = String.Empty

        For Each Field As String In Values.Keys
            ' Field
            If Not String.IsNullOrEmpty(StrFields) Then StrFields &= ", "
            StrFields &= String.Format("[{0}]", Field)

            ' Value
            If Not String.IsNullOrEmpty(StrValues) Then StrValues &= ", "
            StrValues &= SCFramework.DbSqlBuilder.String(Values(Field))
        Next

        ' If not temporary fix the current date/time
        Dim [Date] As Date = Date.MinValue
        If IsTemporary Then [Date] = Now

        ' Create the query command
        Dim SQL As String = "INSERT INTO [" & Translations.DATABASE_TABLE_NAME & "] (" &
                                "[LABEL], [INSERT_DATE], " & StrFields &
                            ") VALUES (" &
                                SCFramework.DbSqlBuilder.String(Label) & ", " &
                                SCFramework.DbSqlBuilder.Date([Date], True) & ", " &
                                StrValues &
                            ")"
        ' Execute
        SCFramework.Bridge.Query.Exec(SQL)
    End Sub


    ' Update the translations inside database
    Private Shared Sub UpdateDataBase(Label As String, Values As Hashtable)
        ' Create the fields and values collection
        Dim Composite As String = String.Empty

        For Each Field As String In Values.Keys
            ' Create the composite pair
            If Not String.IsNullOrEmpty(Composite) Then Composite &= ", "
            Composite &= String.Format("[{0}] = [{1}]", Field, Values(Field))
        Next

        ' Create the query command
        Dim SQL As String = "UPDATE [" & Translations.DATABASE_TABLE_NAME & "] " &
                            "SET " & Composite & " " &
                            "WHERE [LABEL] = " & SCFramework.DbSqlBuilder.String(Label)
        ' Execute
        SCFramework.Bridge.Query.Exec(SQL)
    End Sub


    ' Delete a record from the database
    Private Shared Sub DeleteFromDatabase(Labels() As String)
        ' Get the source
        Dim Source As DataTable = Translations.GetDataSource()

        ' Cycle all label
        For Each Label As String In Labels
            ' Check the label
            If Not String.IsNullOrEmpty(Label) Then
                ' Find the record inside the data source
                Dim Finded As DataRow = Source.Rows.Find(Label)
                ' Check if exists
                If Finded IsNot Nothing Then
                    ' Delete the record
                    Finded.Delete()
                End If
            End If
        Next

        ' Update the database
        SCFramework.Bridge.Query.UpdateDatabase(Source)
    End Sub


    ' Confirm a translation on the database
    Public Shared Sub Confirm(Label As String)
        ' Create the query command
        Dim SQL As String = "UPDATE [" & Translations.DATABASE_TABLE_NAME & "] " &
                            "SET [INSERT_DATE] = NULL " &
                            "WHERE [LABEL] = " & SCFramework.DbSqlBuilder.String(Label)
        ' Execute
        SCFramework.Bridge.Query.Exec(SQL)
    End Sub


    ' Get the row details
    Private Shared Function GetDatabaseTranslationDetails(Label As String) As DataRow
        ' Create the query command
        Dim SQL As String = "SELECT * " &
                            "FROM [" & Translations.DATABASE_TABLE_NAME & "] " &
                            "WHERE [LABEL] = " & SCFramework.DbSqlBuilder.String(Label)
        ' Execute
        Return SCFramework.Bridge.Query.Row(SQL)
    End Function

    ' Add column language to the table
    Public Sub AddLanguageColumn(LanguageCode As String, Optional Query As SCFramework.DbQuery = Nothing)
        ' Check if the column already exists
        If Not SCFramework.Utils.String.IsEmptyOrWhite(LanguageCode) And
            Not Me.WritableColumns.Contains(LanguageCode) Then
            ' Check for the query manager object
            If Query Is Nothing Then Query = Me.Query

            ' Alter the table and add to the writable columns
            Query.Exec(String.Format("ALTER TABLE [{0}] ADD [{1}] NTEXT", Me.GetTableName(), LanguageCode))
            Me.WritableColumns.Add(LanguageCode)
            ' TODO: reload the datasource
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
            ' TODO: reload the datasource
        End If
    End Sub

#End Region

#Region " PUBLIC "

    ' Store the translation
    Public Shared Sub Store(Label As String, Values As Hashtable, Optional IsTemporary As Boolean = False)
        ' Check the label
        Translations.CheckLabel(Label)

        ' Get the list
        Dim List As Hashtable = Translations.GetTranslationsList()

        ' Check if the translation already exists
        If Not Translations.Exists(Label) Then
            ' Add a new translation to the database
            Translations.AddToDatabase(Label, Values, IsTemporary)
            ' Add a new translation to the session
            List.Add(Label, Values(Languages.Current))
        Else
            ' Update the translation inside the database
            Translations.UpdateDataBase(Label, Values)
            ' Update the current translation into the session
            List(Label) = Values(Languages.Current)
        End If
    End Sub


    ' Delete a translation 
    Public Shared Sub Delete(ParamArray Labels() As String)
        ' Delete from database
        Translations.DeleteFromDatabase(Labels)

        ' Get the list
        Dim List As Hashtable = Translations.GetTranslationsList()

        ' Delete from user session
        For Each Label As String In Labels
            List.Remove(Label)
        Next
    End Sub


    ' Get a translation
    Public Shared Function Translate(Label As String) As String
        ' Get the list and return the value
        Dim List As Hashtable = Translations.GetTranslationsList()
        Return List(Label)
    End Function


    ' Get a translation in whole available languages
    Public Shared Function TranslateInAllLanguages(Label As String) As Hashtable
        ' Translation details
        Dim Details As DataRow = Translations.GetDatabaseTranslationDetails(Label)

        ' Check for exists
        If Details Is Nothing Then
            ' Return nothing
            Return Nothing
        Else
            ' Create a hash table
            Dim HT As Hashtable = New Hashtable()
            ' Cycle all language code
            For Each Code As String In Languages.AllCodes
                ' Check if exists
                If Details.Table.Columns.Contains(Code) Then
                    ' Save the code and value pair
                    HT.Add(Code, Details(Code))
                End If
            Next
            ' Return
            Return HT
        End If
    End Function


    ' Translate a datatable column in the current language
    Public Shared Sub TranslateColumn(Source As DataTable, ColumnName As String, Optional NewColumnName As String = "")
        ' Check if have the column to translate
        If Not Source.Columns.Contains(ColumnName) Then
            Return
        End If

        ' Fix the destination column name
        Dim DestColumnName As String = ColumnName
        If Not String.IsNullOrEmpty(NewColumnName) Then
            DestColumnName = NewColumnName
        End If

        ' Check if need to be created
        If Not Source.Columns.Contains(DestColumnName) Then
            Source.Columns.Add(DestColumnName, GetType(System.String))
        End If

        ' Cycle all rows
        For Each Row As DataRow In Source.Rows
            ' Translate and save the value inside the destination column
            Row(DestColumnName) = Translations.Translate(Row(ColumnName))
        Next
    End Sub


    ' Add to datatable a boolean column that show if have lost some translation for each row
    Public Shared Sub AddLanguageLessColumn(ByVal Source As DataTable, ByVal ColumnName As String, ByVal ParamArray ColumnsToCheck() As String)
        ' Add the column if not exists
        If Not Source.Columns.Contains(ColumnName) Then
            Source.Columns.Add(ColumnName, GetType(System.Boolean))
        End If

        ' Get the translation data source
        Dim TranslationSource As DataTable = Translations.GetDataSource()

        ' Cycle all source rows
        For Each Row As DataRow In Source.Rows
            ' Cycle all columns to check
            For Each ColumnToCheck As String In ColumnsToCheck
                ' Find the translation details
                Dim TranslationDetails As DataRow = TranslationSource.Rows.Find(ColumnToCheck)
                Row(ColumnName) = Translations.CheckForLostTranslation(TranslationDetails)
            Next
        Next
        ' Accept changes
        Source.AcceptChanges()
    End Sub


    ' Clean the temporaries translation
    Public Shared Sub Clean()
        ' Clean database
        Dim SQL As String = "DELETE FROM [" & Translations.DATABASE_TABLE_NAME & "] " &
                            "WHERE [INSERT_DATE] <" & DbSqlBuilder.Date(Now.AddHours(-4))
        Bridge.Query.Exec(SQL)
    End Sub

#End Region

End Class
