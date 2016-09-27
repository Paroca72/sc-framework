'*************************************************************************************************
' 
' [SCFramework]
' Languages, Translations
' di Samuele Carassai
'
' Classe di gestione lingue
' Version 5.0.0
' Created --/--/----
' Updated 29/10/2015
'
'*************************************************************************************************


Public Class Languages

    ' Define the languages code list static
    Private Shared LanguagesCode() As String = Nothing
    ' Define the default language holder static
    Private Shared DefaultLanguage As String = Nothing

    ' Lock the languages code list for the concurrent access
    Private Shared LanguagesCodeLock As Object = New Object()
    ' Lock the default languages code for the concurrent access
    Private Shared DefaultLanguageLock As Object = New Object()

    ' Constant session key for the current user selected language
    Private Const CURRENT_USER_LANGUAGE_SESSION_KEY As String = "$SCFramework$Languages$CurrentCode"
    ' The relative database table name
    Private Const DATABASE_TABLE_NAME As String = "SYS_LANGUAGES"
    ' The user request query-string tag
    Private Const QUERYSTRING_TAG As String = "Language"


#Region " PRIVATE "

    ' Get all the available languages code
    Private Shared Function GetAllLanguagesCode() As String()
        ' Load the data source
        Dim Source As DataView = Languages.GetDataSource()
        ' Convert to array list
        Dim List As ArrayList = SCFramework.Utils.ToArrayList(Source, "CODE")
        ' Convert in array of string
        Return List.ToArray(GetType(System.String))
    End Function


    ' Get the default language code
    Private Shared Function GetDefaultLanguage() As String
        ' Create the query
        Dim SQL As String = "SELECT [CODE] " & _
                            "FROM [" & Languages.DATABASE_TABLE_NAME & "] " & _
                            "WHERE [ISDEFAULT] = " & DbSqlBuilder.Boolean(True)
        ' Execute
        Dim Code As String = CStr("" & Bridge.Query.Value(SQL)).Trim
        ' Check for empty values
        If String.IsNullOrEmpty(Code) Then
            ' Throw an exeception
            Throw New Exception("Define a default language in SYS table")
        Else
            ' Return the default language code
            Return Code
        End If
    End Function


    ' Set the default language on database
    Private Shared Sub SetDefaultLanguage(Code As String)
        ' Reset old selection
        Dim SQL As String = "UPDATE [" & Languages.DATABASE_TABLE_NAME & "] " & _
                            "SET [ISDEFAULT] = " & DbSqlBuilder.Boolean(False)
        Bridge.Query.Exec(SQL)

        ' Set the new
        SQL = "UPDATE [" & Languages.DATABASE_TABLE_NAME & "] " & _
              "SET [ISDEFAULT] = " & DbSqlBuilder.Boolean(True) & " " & _
              "WHERE [CODE] = " & DbSqlBuilder.String(Code)
        Bridge.Query.Exec(SQL)
    End Sub


    ' Fix the language name
    Private Shared Function FixCultureName(Name As String) As String
        Dim Fix As String = Name

        Fix = Fix.Replace("(cyrillic) ", String.Empty)
        Fix = Fix.Replace("(latin) ", String.Empty)
        Fix = Fix.Substring(Fix.IndexOf("(") + 1, Fix.Length - Fix.IndexOf("(") - 2)

        Dim Tokens() As String = Fix.Split(",")
        If Tokens.Length > 1 Then
            Fix = Trim(Tokens(1))
        End If

        Fix = Fix.Replace(" ", "_")
        Fix = Fix.Replace(".", "_")
        Fix = Fix.Replace("'", String.Empty)

        Return Fix
    End Function


    ' Refresh all languages code
    Private Shared Sub RefreshLanguagesCode()
        ' Lock the procedure
        SyncLock LanguagesCodeLock
            ' Retrieve the available languages code
            Languages.LanguagesCode = Languages.GetAllLanguagesCode()
        End SyncLock
    End Sub

#End Region

#Region " PUBLIC "

    ' Get all languages code and store it permanently at application level
    Public Shared ReadOnly Property AllCodes() As String()
        Get
            ' Check if already loaded
            If Languages.LanguagesCode Is Nothing Then
                ' Lock the procedure
                SyncLock LanguagesCodeLock
                    ' Retrieve the available languages code
                    Languages.LanguagesCode = Languages.GetAllLanguagesCode()
                End SyncLock
            End If
            ' Return the list
            Return Languages.LanguagesCode
        End Get
    End Property


    ' Get all languages code and store it permanently at application level
    Public Shared Property [Default]() As String
        ' Getter
        Get
            ' Check if already loaded
            If Languages.LanguagesCode Is Nothing Then
                ' Lock the procedure
                SyncLock DefaultLanguageLock
                    ' Retrieve the default languages code
                    Languages.DefaultLanguage = Languages.GetDefaultLanguage()
                End SyncLock
            End If
            ' Return the list
            Return Languages.DefaultLanguage
        End Get
        ' Setter
        Set(Value As String)
            ' Store the default language on database
            Languages.SetDefaultLanguage(Value)

            ' Lock the procedure
            SyncLock DefaultLanguageLock
                ' Need to reload the default language
                Languages.DefaultLanguage = Nothing
            End SyncLock
        End Set
    End Property


    ' Hold the current user language at session level
    Public Shared Property Current() As String
        ' Getter
        Get
            ' Check for session and for current user details
            If (Bridge.Session IsNot Nothing AndAlso Bridge.Session(Languages.CURRENT_USER_LANGUAGE_SESSION_KEY) Is Nothing) And _
               (Bridge.Request IsNot Nothing AndAlso Bridge.Request.UserLanguages IsNot Nothing) Then
                ' If the current user is not a robot
                If Bridge.Request.UserLanguages.Length > 0 Then
                    ' Get the first language available
                    Dim Language As String = Bridge.Request.UserLanguages(0)
                    ' Create a specific culture by the user language and store the name
                    Dim Culture As Global.System.Globalization.CultureInfo = Global.System.Globalization.CultureInfo.CreateSpecificCulture(Language)
                    Languages.Current = Culture.Name
                End If
            End If

            ' Hold the current user language in the session onyl if the session enviroment is available
            If Bridge.Session(Languages.CURRENT_USER_LANGUAGE_SESSION_KEY) Is Nothing Then
                Languages.Current = Languages.Default
            End If

            ' Return the current user language
            Return Bridge.Session(Languages.CURRENT_USER_LANGUAGE_SESSION_KEY)
        End Get
        ' Setter
        Set(value As String)
            ' Check if this language code is available in the database language list
            If Languages.Exists(value) Then
                ' Set the user session language
                Global.System.Threading.Thread.CurrentThread.CurrentUICulture = Global.System.Globalization.CultureInfo.CreateSpecificCulture(value)
                Global.System.Threading.Thread.CurrentThread.CurrentCulture = Global.System.Threading.Thread.CurrentThread.CurrentUICulture

                ' Hold the language
                Bridge.Session(Languages.CURRENT_USER_LANGUAGE_SESSION_KEY) = value
            End If
        End Set
    End Property


    ' Retrieve the short code rappresentation of the current language
    Public Shared ReadOnly Property CurrentShort() As String
        Get
            ' Get the current culture info
            Dim Infos As Globalization.CultureInfo = Globalization.CultureInfo.CreateSpecificCulture(Languages.Current)
            ' Return the short code
            Return Infos.TwoLetterISOLanguageName
        End Get
    End Property


    ' Get the image that rappresenting the language
    Public Shared Function GetFlagBitmap(Code As String) As Bitmap
        ' Find the name of the language
        Dim Culture As Globalization.CultureInfo = Globalization.CultureInfo.CreateSpecificCulture(Code)
        Dim LongName As String = Culture.EnglishName.ToLower

        ' By the name create a relation name with the resource file
        Dim ResourceName As String = String.Format("flag_{0}", FixCultureName(LongName))
        ' Retrieve from resources the bitmap object
        Return (My.Resources.ResourceManager.GetObject(ResourceName))
    End Function


    ' Get the server available culture list
    Public Shared Function GetAllCulturesCode() As ArrayList
        ' Get all cultures installed on server
        Dim AllCulture As Globalization.CultureInfo() = Globalization.CultureInfo.GetCultures(CultureTypes.AllCultures)

        ' Convert the list in an array list
        Dim List As ArrayList = New ArrayList
        ' Cycle all cultures
        For Each Culture As Globalization.CultureInfo In AllCulture
            ' Check if the name is empty
            If Not String.IsNullOrEmpty(Culture.Name) Then
                ' If the list still not contain the name
                If Not List.Contains(Culture.TextInfo.CultureName) Then
                    ' Add the name to the list
                    List.Add(Culture.TextInfo.CultureName)
                End If
            End If
        Next
        ' Sort the list
        List.Sort()

        ' Retrun
        Return List
    End Function


    ' Check if the code exists in my database
    Public Shared Function Exists(Code As String) As Boolean
        Return New ArrayList(Languages.AllCodes).Contains(Code)
    End Function


    ' Check if have a user request to change the current language
    Public Shared Sub CheckForUserRequestLanguage()
        Try
            ' Check in the query string if have the tag < language >
            Dim Language As String = Trim("" & Bridge.Page.Request.QueryString(Languages.QUERYSTRING_TAG))
            If Not String.IsNullOrEmpty(Language) Then
                ' Set the current language
                Languages.Current = Language
            End If

        Catch ex As Exception
            ' Do nothing
        End Try
    End Sub

#End Region

#Region " DATABASE INTERFACE "

    ' Get the data source of the database languages
    Public Shared Function GetDataSource() As DataView
        ' Create the SQL command
        Dim SQL As String = "SELECT * " & _
                            "FROM [" & Languages.DATABASE_TABLE_NAME & "]"
        ' Get the data source
        Dim Source As DataTable = Bridge.Query.Table(SQL, Languages.DATABASE_TABLE_NAME)

        ' Get the default view and order it
        Dim View As DataView = Source.DefaultView
        View.Sort = "[ISDEFAULT] DESC, [TITLE]"

        ' Return the view
        Return View
    End Function


    ' Add new languages to the database table.
    ' NB: This function alter the structure of the < translations > database table
    Public Shared Sub Add(Code As String, Title As String, Visible As Boolean)
        ' Create the alter translation table
        Dim Alter As String = "ALTER TABLE [" & Translations.DATABASE_TABLE_NAME & "] " & _
                              "ADD [" & Code & "] NTEXT"
        ' Create the insert command
        Dim Insert As String = "INSERT INTO [" & Languages.DATABASE_TABLE_NAME & "] (" & _
                                    "[CODE], [TITLE], [VISIBLE], [ISDEFAULT]" & _
                               ") VALUES (" & _
                                    DbSqlBuilder.String(Code) & ", " & _
                                    DbSqlBuilder.String(Title) & ", " & _
                                    DbSqlBuilder.Boolean(Visible) & ", " & _
                                    DbSqlBuilder.Boolean(False) & _
                               ")"
        ' Execute all the query
        Bridge.Query.Exec(Alter, Insert)

        ' Need to reload the languages code
        Languages.RefreshLanguagesCode()
    End Sub


    ' Delete languages from the database table.
    ' NB: This function alter the structure of the < translations > database table
    Public Shared Sub Delete(Code As String)
        ' Create the alter translation table
        Dim Alter As String = "ALTER TABLE [" & Translations.DATABASE_TABLE_NAME & "] " & _
                              "DROP COLUMN [" & Code & "]"
        ' Create the delete command
        Dim SQL As String = "DELETE FROM [" & Languages.DATABASE_TABLE_NAME & "] " & _
                            "WHERE [CODE] = " & DbSqlBuilder.String(Code)
        ' Execute all the query
        Bridge.Query.Exec(Alter, SQL)

        ' Need to reload the languages code
        Languages.RefreshLanguagesCode()
    End Sub


    ' Update a language record
    Public Shared Sub Update(Code As String, Title As String, Visible As Boolean)
        ' Create the update command
        Dim SQL As String = "UPDATE [" & Languages.DATABASE_TABLE_NAME & "] " & _
                            "SET [TITLE] = " & DbSqlBuilder.String(Title) & ", " & _
                                "[VISIBLE] = " & DbSqlBuilder.Boolean(Visible) & " " & _
                            "WHERE [CODE] = " & DbSqlBuilder.String(Code)
        ' Execute the command
        Bridge.Query.Exec(SQL)
    End Sub


#End Region

End Class


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
        If Bridge.Session Is Nothing OrElse _
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
            For Each Code As String In Languages.AllCodes
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
        Dim SQL As String = "SELECT * " & _
                            "FROM [" & Translations.DATABASE_TABLE_NAME & "] " & _
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
        Dim SQL As String = "INSERT INTO [" & Translations.DATABASE_TABLE_NAME & "] (" & _
                                "[LABEL], [INSERT_DATE], " & StrFields & _
                            ") VALUES (" & _
                                SCFramework.DbSqlBuilder.String(Label) & ", " & _
                                SCFramework.DbSqlBuilder.Date([Date], True) & ", " & _
                                StrValues & _
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
        Dim SQL As String = "UPDATE [" & Translations.DATABASE_TABLE_NAME & "] " & _
                            "SET " & Composite & " " & _
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
        Dim SQL As String = "UPDATE [" & Translations.DATABASE_TABLE_NAME & "] " & _
                            "SET [INSERT_DATE] = NULL " & _
                            "WHERE [LABEL] = " & SCFramework.DbSqlBuilder.String(Label)
        ' Execute
        SCFramework.Bridge.Query.Exec(SQL)
    End Sub


    ' Get the row details
    Private Shared Function GetDatabaseTranslationDetails(Label As String) As DataRow
        ' Create the query command
        Dim SQL As String = "SELECT * " & _
                            "FROM [" & Translations.DATABASE_TABLE_NAME & "] " & _
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
        Dim SQL As String = "DELETE FROM [" & Translations.DATABASE_TABLE_NAME & "] " & _
                            "WHERE [INSERT_DATE] <" & DbSqlBuilder.Date(Now.AddHours(-4))
        Bridge.Query.Exec(SQL)
    End Sub

#End Region

End Class
