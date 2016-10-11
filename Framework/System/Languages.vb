'*************************************************************************************************
' 
' [SCFramework]
' Languages
' di Samuele Carassai
'
' Classe di gestione lingue
' Version 5.0.0
' Created --/--/----
' Updated 05/10/2016
'
'*************************************************************************************************


Public Class Languages
    Inherits DbHelper

    ' The user request query-string tag
    Public Const QUERYSTRING_TAG As String = "Language"

    ' Hold the data table source
    Private DataSource As DataTable = Nothing
    ' The data source locker
    Private DataSourceLocker As Object = New Object()


#Region " STATIC "

    ' Static instance holder
    Private Shared mInstance As Languages = Nothing

    ' Instance property
    Public Shared ReadOnly Property Instance As Languages
        Get
            ' Check if null
            If Languages.mInstance Is Nothing Then
                Languages.mInstance = New Languages()
            End If

            ' Return the static instance
            Return Languages.mInstance
        End Get
    End Property

#End Region

#Region " CONSTRUCTOR AND OVERRIDES "

    Public Sub New()
        ' Get the datatable
        Me.DataSource = Me.GetSource()
    End Sub

    Public Overrides Function GetTableName() As String
        Return "SYS_LANGUAGES"
    End Function

#End Region

#Region " PRIVATE "

    ' Get the default language code
    Private Function GetDefaultLanguage() As String
        ' Lock the data source
        SyncLock Me.DataSourceLocker
            ' Find the row
            Dim Row = (From CurrentRow As DataRow In Me.DataSource.AsEnumerable()
                       Where CurrentRow!ISDEFAULT = True And Not IsDBNull(CurrentRow!CODE)
                       Select CurrentRow).FirstOrDefault()

            ' If exists return the code
            If Row IsNot Nothing Then
                Return Row!CODE
            End If
        End SyncLock

        ' If not found throw an exeception
        Throw New Exception("Define a default language in SYS table")
    End Function

    ' Set the default language on database
    Private Sub SetDefaultLanguage(Code As String)
        ' Lock the data source
        SyncLock Me.DataSourceLocker
            ' If not exists exit from the sub
            If Not Me.Exists(Code) Then Exit Sub

            ' Cycle all languages rows and fix the default
            For Each Row As DataRow In Me.DataSource.Rows
                ' Check if default
                Row!ISDEFAULT = Not IsDBNull(Row!CODE) AndAlso CStr(Code).ToLower = Code.ToLower
            Next
        End SyncLock
    End Sub

    ' Fix the language name
    Private Function FixCultureName(Name As String) As String
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

#End Region

#Region " PUBLIC "

    ' Check if the code exists in my database
    Public Function Exists(Code As String) As Boolean
        ' Return true if the code exists in the data source
        Return (From Row As DataRow In Me.DataSource.AsEnumerable
                Where Not IsDBNull(Row!CODE) AndAlso CStr(Code).ToLower = Code.ToLower
                Select Row) _
            .FirstOrDefault Is Nothing
    End Function

    ' Get all languages code
    Public ReadOnly Property AllCodes() As String()
        Get
            Return (From Row As DataRow In Me.DataSource.AsEnumerable
                    Select Row!CODE).ToArray()
        End Get
    End Property

    ' Get all languages code and store it permanently at application level
    Public Property [Default]() As String
        ' Getter
        Get
            Return Me.GetDefaultLanguage()
        End Get
        ' Setter
        Set(Value As String)
            ' Store the default language on database
            Me.SetDefaultLanguage(Value)
            ' Save the changes only if one found
            Bridge.Query.UpdateDatabase(Me.DataSource)
        End Set
    End Property

    ' Hold the current user language at session level
    Public Property Current() As String
        ' Getter
        Get
            ' Check for session and for current user details
            If Bridge.Request IsNot Nothing AndAlso Bridge.Request.UserLanguages IsNot Nothing Then
                ' If the current user is not a robot
                If Bridge.Request.UserLanguages.Length > 0 Then
                    ' Get the first language available
                    Dim Language As String = Bridge.Request.UserLanguages(0)
                    ' Create a specific culture by the user language and store the name
                    Dim Culture As Global.System.Globalization.CultureInfo = Global.System.Globalization.CultureInfo.CreateSpecificCulture(Language)
                    Return Culture.Name
                End If
            End If

            ' Return the default
            Return Me.Default
        End Get
        ' Setter
        Set(value As String)
            ' Check if this language code is available in the database language list
            If Me.Exists(value) Then
                ' Set the user session language
                Global.System.Threading.Thread.CurrentThread.CurrentUICulture = Global.System.Globalization.CultureInfo.CreateSpecificCulture(value)
                Global.System.Threading.Thread.CurrentThread.CurrentCulture = Global.System.Threading.Thread.CurrentThread.CurrentUICulture
            End If
        End Set
    End Property

    ' Retrieve the short code rappresentation of the current language
    Public ReadOnly Property CurrentTwoLetterISO() As String
        Get
            ' Get the current culture info
            Dim Infos As Globalization.CultureInfo = Globalization.CultureInfo.CreateSpecificCulture(Me.Current)
            ' Return the short code
            Return Infos.TwoLetterISOLanguageName
        End Get
    End Property

    ' Get the image that rappresenting the language
    Public Function GetFlag(Code As String) As Bitmap
        ' Find culture object from the code and create a relation name with the resource file
        Dim Culture As Globalization.CultureInfo = Globalization.CultureInfo.CreateSpecificCulture(Code)
        Dim ResourceName As String = String.Format("flag_{0}", Me.FixCultureName(Culture.EnglishName.ToLower))

        ' Retrieve from resources the bitmap object
        Return My.Resources.ResourceManager.GetObject(ResourceName)
    End Function

    ' Get the server available culture list
    Public Shared Function GetAllCulturesCode() As String()
        ' Get all cultures installed on server
        Dim AllCulture As Globalization.CultureInfo() = Globalization.CultureInfo.GetCultures(CultureTypes.AllCultures)

        ' Select all Name sorted and not duplicated and return the list
        Return (From Culture As Globalization.CultureInfo In AllCulture
                Where Not String.IsNullOrEmpty(Culture.Name)
                Order By Culture.Name
                Select Culture.Name).Distinct().ToArray()
    End Function

#End Region

#Region " DATABASE INTERFACE "

    ' Get the data source of the database languages
    Public Function GetView() As DataView
        ' Get the default view and order it
        Dim View As DataView = New DataView(Me.DataSource)
        View.Sort = "[ISDEFAULT] DESC, [TITLE]"

        ' Return the view
        Return View
    End Function

    ' Add new languages to the database table.
    ' NB: This function alter the structure of the < translations > database table
    Protected Shadows Function Insert(Code As String, Title As String, Visible As Boolean, IsDefault As Boolean) As Long
        ' Check for a valid code
        If String.IsNullOrEmpty(Code) Then
            Throw New Exception("The field < CODE > must be valid.")
        End If

        ' Check for duplicated code
        If Me.Exists(Code) Then
            Throw New Exception("The field < CODE > cannot be duplicated.")
        End If

        ' Create the query manager and start the transaction
        Dim Query As SCFramework.DbQuery = New SCFramework.DbQuery()
        Query.StartTransaction()

        Try
            ' Alter the translation table
            Dim Alter As String = "ALTER TABLE [" & Translations.Instance.GetTableName & "] " &
                                  "ADD [" & Code & "] NTEXT"
            Query.Exec(Alter)

            ' Lock the data source
            SyncLock Me.DataSourceLocker
                ' Insert the new row
                Dim NewRow As DataRow = Me.DataSource.NewRow
                NewRow!CODE = Code
                NewRow!TITLE = Title
                NewRow!VISIBLE = Visible
                Me.DataSource.Rows.Add(NewRow)
            End SyncLock

            ' Define the default language
            If IsDefault Then Me.SetDefaultLanguage(Code)

            ' Save the changes
            Query.UpdateDatabase(Me.DataSource)
            Query.CommitTransaction()

        Catch ex As Exception
            ' Rool back
            Me.DataSource.RejectChanges()
            Query.RollBackTransaction()

            ' Propagate the exception
            Throw ex
        End Try
    End Function

    ' Delete languages from the database table.
    ' NB: This function alter the structure of the < translations > database table
    Public Shadows Sub Delete(Code As String)
        ' Create the query manager and start the transaction
        Dim Query As SCFramework.DbQuery = New SCFramework.DbQuery()
        Query.StartTransaction()

        Try
            ' Create the alter translation table
            Dim Alter As String = "ALTER TABLE [" & Translations.Instance.GetTableName & "] " &
                                  "DROP COLUMN [" & Code & "]"
            Query.Exec(Alter)

            ' Find the row and if exists delete it.
            ' I could use LINQ but in this case is more simple with the standard research because I must find one row using the primary key.
            ' The datatable, by default, was created in ignore case-sensitive for the string comparison so I don't take care of the "code" status.
            Dim Row As DataRow = Me.DataSource.Rows.Find(Code)
            If Row IsNot Nothing Then Row.Delete()

            ' Save the changes
            Query.UpdateDatabase(Me.DataSource)
            Query.CommitTransaction()

        Catch ex As Exception
            ' Rool back
            Me.DataSource.RejectChanges()
            Query.RollBackTransaction()

            ' Propagate the exception
            Throw ex
        End Try
    End Sub

    ' Update a language record
    Public Shadows Sub Update(Code As String, Title As String, Visible As Boolean)
        ' Find the row and if exists delete it
        ' I could use LINQ but in this case is more simple with the standard research because I must find one row using the primary key.
        ' The datatable, by default, was created in ignore case-sensitive for the string comparison so I don't take care of the "code" status.
        Dim Row As DataRow = Me.DataSource.Rows.Find(Code)
        If Row IsNot Nothing Then
            Row!TITLE = Title
            Row!VISIBLE = Visible
        End If

        ' Save the changes
        Bridge.Query.UpdateDatabase(Me.DataSource)
    End Sub


#End Region

End Class

