'*************************************************************************************************
' 
' [SCFramework]
' Multilanguages
' di Samuele Carassai
'
' Version 5.0.0
' Updated 27/11/2016
'
'
' Multilanguages manager class provide the methods to manage the framework multilanguages table.
' This classes inherits from DataSourceHelper but is ALWAYS memory managed. 
' Some base methods are shadowed or overridden to avoid to change the management mode.
' Since memory managed you must remember to call the AcceptChanges method to confirm
' every changes.
'
' Tha GetSource method will return a marge of values between the requested language (passed in
' the paramenters) and the default language retrieved from the static Languages class.
'
' The table structure will be very simple:
' - KEY         (PK)
' - LANGUAGES   (PK) 
' - VALUE
' - TO_DELETE
'
' Please note that this class shadows the base delete methods to avoid to the user to delete
' directly the record from the server. For many reason the only way to delete a record is
' throught the DELETE_TO column. Once set the record will be delete after a standard time.
' The main reason of this mode to delete is in case of related table using some columns linked 
' to a multilanguages table and have some errors during the procedure of deleting will permit to 
' the user To rollback to the previous status.
' For example if the multilanguages table is a files table the phisical file will be delete
' posticipate avoiding to lost data in case of roll-back.
' For the same reason the update command will be insert the old value as a new value marked to
' delete and after that will update the old record values.
'
'*************************************************************************************************


Public MustInherit Class Multilanguages
    Inherits DataSourceHelper


#Region " CONSTRUCTOR "

    Sub New()
        ' Start cleaning
        Me.StartCleaning()

        ' Force the class as memory managed
        MyBase.GetSource(Nothing, True)
    End Sub

#End Region

#Region " CLEANER "

    Private Const DELETE_AFTER_MINUTES As Integer = 4 * 60

    Private Shared mCleanerThread As Thread = Nothing
    Private mCleanerInterval As TimeSpan = New TimeSpan(0, 30, 0)


    ' Launch the cleaning cycle
    Private Sub StartCleaning()
        ' Check for empty values
        If Multilanguages.mCleanerThread Is Nothing Then
            Multilanguages.mCleanerThread = New Thread(AddressOf Me.CycleCleaning)
            Multilanguages.mCleanerThread.Priority = ThreadPriority.BelowNormal
            Multilanguages.mCleanerThread.Start()
        End If
    End Sub


    ' Execute the cleaning
    Private Sub CycleCleaning()
        ' Loop
        While Multilanguages.mCleanerThread.ThreadState <> Threading.ThreadState.Stopped
            Try
                ' Clean all cases
                Me.SyncWithLanguagesTable()
                Me.ApplyToDelete()

            Catch ex As ThreadAbortException
                ' Reset the abort
                Thread.ResetAbort()

            Catch ex As Exception
                ' Do nothing

            Finally
                ' Sleep
                Thread.Sleep(Me.mCleanerInterval)
            End Try

        End While
    End Sub


    ' Sync the current languages with the code include inside this table.
    ' All the rows with code not inside the current languages list will be mark as TO_DELETE.
    Private Sub SyncWithLanguagesTable()
        ' Serialize the languages codes and create the clauses
        Dim Codes As String = String.Join(", ", Bridge.Languages.AllCodes(False))
        Dim Clauses As DbClauses = New DbClauses("CODE", DbClauses.ComparerType.NotIn, Codes)

        ' Create the SQL for update the rows not inside the codes and execute it
        Dim SQL As DbSqlBuilder = New DbSqlBuilder() _
            .Table(Me.GetTableName()) _
            .Update(Me.ToValues("TO_DELETE", Now)) _
            .Where(Clauses)
        Me.Query.Exec(SQL.UpdateCommand)
    End Sub


    ' Clean the rows marked as TO_DELETE if needed
    Protected Overridable Function ApplyToDelete() As String()
        ' Create the SQL builder
        Dim Clauses As DbClauses = New DbClauses("TO_DELETE", DbClauses.ComparerType.Minor, Now.AddMinutes(Multilanguages.DELETE_AFTER_MINUTES))
        Dim SQL As DbSqlBuilder = New DbSqlBuilder() _
            .Table(Me.GetTableName()) _
            .Select("VALUE") _
            .Where(Clauses)

        ' Get the list of the values to delete
        Dim Table As DataTable = Me.Query.Table(SQL.SelectCommand)
        ApplyToDelete = (From Row In Table Select CStr(Row!VALUE)).ToArray()

        ' Delete from the database
        Me.Query.Exec(SQL.DeleteCommand)
    End Function

#End Region

#Region " PUBLIC "

    ' Get the source filtered by the language
    Public Shadows Function GetSource(Language As String) As Dictionary(Of String, String)
        ' Get the source
        Dim Clauses As DbClauses = New DbClauses("TO_DELETE", DbClauses.ComparerType.Equal, Nothing) _
            .And(New DbClauses("LANGUAGE", DbClauses.ComparerType.Equal, Language).Or("LANGUAGE", DbClauses.ComparerType.Equal, Bridge.Languages.Default))
        Dim Source As DataTable = MyBase.GetSource(Clauses)

        ' Merge the requested language with the default one for fill the empty translations
        ' TODO: Check if work proper
        Return (From Row In Source.AsEnumerable() Where Not IsDBNull(Row!VALUE) Select Row!KEY, Row!VALUE) _
            .OrderBy("[LANGUAGE] " & IIf(Language < Bridge.Languages.Default, "ASC", "DESC")) _
            .GroupBy(Of String)(Function(Pair) Pair.KEY) _
            .ToDictionary(Of String, String)(Function(Pair) Pair.Key, Function(Pair) Pair.First().VALUE)
    End Function


    ' Get the single value in language
    Public Function GetValue(Key As String, Language As String) As String
        ' Create the clauses
        Dim Clauses As DbClauses = New DbClauses()
        Clauses.And("KEY", DbClauses.ComparerType.Equal, Key)
        Clauses.And("LANGUAGE", DbClauses.ComparerType.Equal, Language)
        Clauses.And("TO_DELETE", DbClauses.ComparerType.Equal, Nothing)

        ' Get the filtered source
        Dim Source As DataTable = MyBase.GetSource(Clauses)

        ' Check if have a result
        If Source.Rows.Count > 0 Then
            ' Return the first if not dbnull
            Dim Value As Object = Source.Rows(0)!VALUE
            If Not IsDBNull(Value) Then Return Value
        End If

        ' Else
        Return Nothing
    End Function


    ' Get the value in all languages
    Public Function GetValues(Key As String) As Dictionary(Of String, String)
        ' Create the clauses
        Dim Clauses As DbClauses = New DbClauses()
        Clauses.And("KEY", DbClauses.ComparerType.Equal, Key)
        Clauses.And("TO_DELETE", DbClauses.ComparerType.Equal, Nothing)

        ' Get the filtered source
        Dim Source As DataTable = MyBase.GetSource(Clauses)
        GetValues = New Dictionary(Of String, String)

        ' Check if have a result
        For Each Row As DataRow In Source.Rows
            ' Holders
            Dim CurrentKey As String = Row!LANGUAGE
            Dim CurrentValue As String = String.Empty

            ' Fix the value
            If Not IsDBNull(Row!VALUE) Then
                CurrentValue = Row!VALUE
            End If

            ' Add the value
            GetValues.Add(CurrentKey, CurrentValue)
        Next
    End Function


    ' Insert command
    Public Shadows Sub Insert(Key As String, Value As String, Language As String)
        ' Create the insert values
        Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)
        Values.Add("KEY", Key)
        Values.Add("VALUE", Value)
        Values.Add("LANGUAGE", Language)

        ' Insert calling the base method
        MyBase.Insert(Values)
    End Sub


    ' Delete command
    Public Shadows Sub Delete(Key As String, Language As String)
        ' Set TO_DELETE to now
        ' Create the clauses
        Dim Clauses As DbClauses = DbClauses.Empty
        Clauses.And("KEY", DbClauses.ComparerType.Equal, Key)
        Clauses.And("LANGUAGE", DbClauses.ComparerType.Equal, Language)

        ' Update the table
        MyBase.Update(Me.ToValues("TO_DELETE", Now), Clauses)
    End Sub

    Public Shadows Sub Delete(Key As String)
        ' Update the table
        MyBase.Update(Me.ToValues("TO_DELETE", Now), New DbClauses("KEY", DbClauses.ComparerType.Equal, Key))
    End Sub


    ' Update command
    Public Shadows Function Update(Key As String, Value As String, Language As String) As Boolean
        ' Get the old value if exists
        Dim OldValue As String = Me.GetValue(Key, Language)

        ' Check if exists
        If OldValue IsNot Nothing AndAlso OldValue <> Value Then
            ' Insert a new row with the old value to delete
            Dim NewGuid As String = Utils.GUID.GuidToString
            Me.Insert(NewGuid, OldValue, Language)
            Me.Delete(NewGuid, Language)
        End If

        ' Create the clauses
        Dim Clauses As DbClauses = DbClauses.Empty
        Clauses.And("KEY", DbClauses.ComparerType.Equal, Key)
        Clauses.And("LANGUAGE", DbClauses.ComparerType.Equal, Language)

        ' Call the base method
        Return MyBase.Update(Me.ToValues("VALUE", Value), Clauses) > 0
    End Function

#End Region

End Class
