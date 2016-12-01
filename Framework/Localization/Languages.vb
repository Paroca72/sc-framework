'*************************************************************************************************
' 
' [SCFramework]
' Languages
' di Samuele Carassai
'
' Languages manager class provide the methods to manage the framework languages.
' This classes inherits from DataSourceHelper but is ALWAYS memory managed. 
' Some base methods are shadowed or overridden to avoid to change the management way.
' Since memory managed you must remember to call the AcceptChanges method to confirm
' every changes.
'
' The static access to this class can be found in the <SCFramework.Bridge> global class.
'
' Version 5.0.0
' Updated 20/10/2016
'
'*************************************************************************************************


Public Class Languages
    Inherits DataSourceHelper

    ' The user request query-string tag
    Public Const QUERYSTRING_TAG As String = "Language"

    ' Holders
    Private mAllLanguagesCodes() As String = Nothing
    Private mDefaultLanguageCode As String = Nothing


#Region " CONSTRUCTOR "

    Sub New()
        ' Base
        MyBase.New()

        ' Define the order columns
        Me.OrderColumns.Add("[ISDEFAULT]")
        Me.OrderColumns.Add("[TITLE]")

        ' Get the source and keep it in memory
        MyBase.GetSource(Nothing, True)
    End Sub

#End Region

#Region " OVERRIDES "

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
            Dim Row = (From CurrentRow As DataRow In Me.GetSource().AsEnumerable()
                       Where CurrentRow!ISDEFAULT = True And Not IsDBNull(CurrentRow!CODE)
                       Select CurrentRow).FirstOrDefault()

            ' If exists return the code
            If Row IsNot Nothing Then
                Return Row!CODE

            Else
                ' Try to return the first in list
                If Me.GetSource().Rows.Count > 0 Then
                    Return Me.GetSource().Rows(0)!CODE
                End If
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
            For Each Row As DataRow In Me.GetSource().Rows
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
        ' Lock the data source
        SyncLock Me.DataSourceLocker
            ' Return true if the code exists in the data source
            Return (From Row As DataRow In Me.GetSource().AsEnumerable
                    Where Not IsDBNull(Row!CODE) AndAlso CStr(Code).ToLower = Code.ToLower
                    Select Row) _
            .FirstOrDefault Is Nothing
        End SyncLock
    End Function

    ' Get all languages code
    Public ReadOnly Property AllCodes(OnlyVisible As Boolean) As String()
        Get
            ' If the all language codes are not defined
            If Me.mAllLanguagesCodes Is Nothing Then
                ' Lock the data source
                SyncLock Me.DataSourceLocker
                    ' Get the list of all language codes
                    Dim Clauses As DbClauses = New DbClauses("VISIBLE", DbClauses.ComparerType.Equal, True)
                    Dim Rows() As DataRow = Me.GetSource() _
                        .Select(IIf(OnlyVisible, Clauses, String.Empty))

                    ' To array
                    Me.mAllLanguagesCodes = (From Row In Rows Select CStr(Row!CODE)).ToArray()
                End SyncLock
            End If
            ' Return
            Return Me.mAllLanguagesCodes
        End Get
    End Property

    ' Get all languages code and store it permanently at application level
    Public Property [Default]() As String
        ' Getter
        Get
            ' If the default is not defined
            If Me.mDefaultLanguageCode Is Nothing Then
                ' Load and store the default language code
                Me.mDefaultLanguageCode = Me.GetDefaultLanguage()
            End If
            ' Return the default language code
            Return Me.mDefaultLanguageCode
        End Get
        ' Setter
        Set(Value As String)
            ' Store the default language on database
            Me.SetDefaultLanguage(Value)
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
    Public Function GetFlag(Code As String) As Drawing.Image
        Try
            ' Find culture object from the code and create a relation name with the resource file
            Dim Culture As Globalization.CultureInfo = Globalization.CultureInfo.CreateSpecificCulture(Code)
            Dim ResourceName As String = String.Format("flag_{0}", Me.FixCultureName(Culture.EnglishName.ToLower))

            ' Retrieve from resources the bitmap object
            Return My.Resources.ResourceManager.GetObject(ResourceName)

        Catch ex As Exception
            ' Return nothing in error case
            Return Nothing
        End Try
    End Function

    ' Get the server available culture list
    Public Shared Function GetAllSystemCulturesCode() As String()
        ' Get all cultures installed on server
        Dim AllCulture As Globalization.CultureInfo() = Globalization.CultureInfo.GetCultures(CultureTypes.AllCultures)

        ' Select all Name sorted and not duplicated and return the list
        Return (From Culture As Globalization.CultureInfo In AllCulture
                Where Not String.IsNullOrEmpty(Culture.Name)
                Order By Culture.Name
                Select Culture.Name).Distinct().ToArray()
    End Function

    ' Get the source table.
    Public Shadows Function GetSource(Optional Clauses As DbClauses = Nothing) As DataTable
        Return MyBase.GetSource(Clauses)
    End Function

    ' Add a new languages code.
    Public Shadows Sub Insert(Code As String, Title As String, Visible As Boolean, IsDefault As Boolean)
        ' Check for a valid code
        If String.IsNullOrEmpty(Code) Then Throw New Exception("The field < CODE > must be valid.")
        If Me.Exists(Code) Then Throw New Exception("The field < CODE > cannot be duplicated.")

        ' Insert the new row
        Dim Pairs As Dictionary(Of String, Object) = New Dictionary(Of String, Object)
        Pairs.Add("CODE", Code)
        Pairs.Add("TITLE", Title)
        Pairs.Add("VISIBLE", Visible)
        MyBase.Insert(Pairs)

        ' Define the default language
        If IsDefault Then
            Me.SetDefaultLanguage(Code)
        End If

        ' Reset the old code
        Me.mDefaultLanguageCode = Nothing
        Me.mAllLanguagesCodes = Nothing
    End Sub

    ' Delete languages from the database table.
    Public Shadows Sub Delete(Code As String)
        ' Check for empty values
        If Not SCFramework.Utils.String.IsEmptyOrWhite(Code) Then
            ' Call the base method 
            MyBase.Delete(Me.ToClauses(Code))

            ' Reset the codes
            Me.mDefaultLanguageCode = Nothing
            Me.mAllLanguagesCodes = Nothing
        End If
    End Sub

    ' Update a language record
    Public Shadows Sub Update(Code As String, Title As String, Visible As Boolean)
        ' Check for empty values
        If Not SCFramework.Utils.String.IsEmptyOrWhite(Code) Then
            ' Values
            Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)
            Values.Add("TITLE", Title)
            Values.Add("VISIBLE", Visible)

            ' Call the base method
            MyBase.Update(Values, Me.ToClauses(Code))

            ' Reset the codes
            Me.mDefaultLanguageCode = Nothing
            Me.mAllLanguagesCodes = Nothing
        End If
    End Sub

    ' Force to reload data source using the last clauses at the next source access
    Public Overrides Sub CleanDataSouce()
        MyBase.GetSource(Nothing, True)
    End Sub

#End Region

End Class

