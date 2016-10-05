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

    ' The relative database table name
    Public Const DATABASE_TABLE_NAME As String = "SYS_TRANSLATIONS"
    ' Constant session key for the current user translation
    Private Const CURRENT_USER_TRANSLATIONS_SESSION_KEY As String = "$SCFramework$Translations$List"


#Region " PRIVATE "

    ' Get all translation and store it in the user session
    Private Shared Function GetTranslationsList() As Hashtable
        ' Create the source holder
        Dim Source As Hashtable = Nothing

        ' Check if the session is available
        If Bridge.Session Is Nothing OrElse
            Bridge.Session(Translations.CURRENT_USER_TRANSLATIONS_SESSION_KEY) Is Nothing Then
            ' Get thr current language details
            Dim Current As String = String.Format("[{0}]", Languages.Current)
            Dim [Default] As String = String.Format("[{0}]", Languages.Default)

            ' Create the query
            ' TODO
            'Dim SQL As String = "SELECT [LABEL], " & _
            '                           "(" & SCFramework.DbSqlBuilder.IsNULL(Current, [Default]) & ") AS [VALUE] " & _
            '                    "FROM [" & Translations.DATABASE_TABLE_NAME & "]"
            '' Get the list
            'Source = Bridge.Query.HashTable(SQL, "LABEL", "VALUE")
        End If

        ' Check if the session is available
        If Bridge.Session Is Nothing Then
            Return Source
        Else
            ' Check if the source is loaded
            If Source IsNot Nothing Then
                ' Save the list in the current user session
                Bridge.Session(Translations.CURRENT_USER_TRANSLATIONS_SESSION_KEY) = Source
            End If

            ' Return the list
            Return Bridge.Session(Translations.CURRENT_USER_TRANSLATIONS_SESSION_KEY)
        End If
    End Function


    ' Check if translation already exists
    Private Shared Function Exists(Label As String) As Boolean
        ' Get translations and check
        Dim List As Hashtable = Translations.GetTranslationsList()
        Return List.ContainsKey(Label)
    End Function


    ' Check the label
    Private Shared Sub CheckLabel(Label As String)
        If String.IsNullOrEmpty(Label) Then
            Throw New Exception("The label cannot be empty!")
        End If
    End Sub


    ' Check if some translations is lost
    Private Shared Function CheckForLostTranslation(Details As DataRow)
        ' Check for null value
        If Details IsNot Nothing Then
            ' Cycle all languages code inside the table
            For Each Code As String In Me.AllCodes
                ' Check if the column exists
                If Details.Table.Columns.Contains(Code) Then
                    ' Check if the translation is empty
                    If String.IsNullOrEmpty(Details(Code)) Then
                        ' Some is lost
                        Return True
                    End If
                End If
            Next
        End If

        ' Have all translations
        Return False
    End Function

#End Region

#Region " DATABASE INTERFACE "

    ' Get the datasource from the database table
    Public Shared Function GetDataSource() As DataTable
        ' Create the sql command
        Dim SQL As String = "SELECT * " &
                            "FROM [" & Translations.DATABASE_TABLE_NAME & "] " &
                            "WHERE [INSERT_DATE] IS NULL"
        ' Get the source
        Dim Source As DataTable = Bridge.Query.Table(SQL, Translations.DATABASE_TABLE_NAME)
        ' Fix the primary key
        SCFramework.Utils.SetPrimaryKeyColumns(Source, "LABEL")

        ' Return 
        Return Source
    End Function


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
