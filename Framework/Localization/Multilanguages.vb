Public MustInherit Class Multilanguages
    Inherits DataSourceHelper


#Region " CONSTRUCTOR "

    Sub New()
        ' Start cleaning
        Me.StartCleaning()
    End Sub

#End Region

#Region " CLEANER "

    Private Shared mCleanerThread As Thread = Nothing
    Private mCleanerInterval As TimeSpan = New TimeSpan(0, 30, 0)
    Private Const DELETE_AFTER As Integer = 4

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
                ' Get the source
                Dim Source As DataTable = MyBase.GetSource().Clone()
                Source.AcceptChanges()

                ' Clean all cases
                Me.SyncWithLanguagesTable(Source)
                Me.ElaborateToDelete(Source)

                ' Save all
                Me.Query.UpdateDatabase(Source)

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
    Private Sub SyncWithLanguagesTable(Source As DataTable)
        ' Extract all the codes
        Dim CodesIn() As String = (From Row In Source.AsEnumerable()
                                   Where IsDBNull(Row!TO_DELETE)
                                   Select Row!LANGUAGE).Distinct()
        ' Start to check
        For Each Code As String In CodesIn
            ' Check if wihtin the languages list
            If Not Bridge.Languages.AllCodes.Contains(Code) Then
                ' Mark all row as to deleted
                Dim Filtered As List(Of DataRow) = (From Row In Source
                                                    Where Row!LANGUAGE = Code
                                                    Select Row).ToList()
                Filtered.ForEach(Sub(Row) Row!TO_DELETE = Now)
            End If
        Next
    End Sub

    ' Clean the rows marked as TO_DELETE
    Protected Overridable Function ElaborateToDelete(Source As DataTable) As List(Of DataRow)
        ' Get all rows to delete and mark delete them
        Dim Filtered As List(Of DataRow) = (From Row In Source
                                            Where Row!TO_DELETE < Now.AddHours(-Multilanguages.DELETE_AFTER)
                                            Select Row).ToList()
        Filtered.ForEach(Sub(Row) Row.Delete())

        ' Return to delete
        Return Filtered
    End Function

#End Region

#Region " PUBLIC "

    ' Get the source
    Public Shadows Function GetSource(Language As String) As Dictionary(Of String, String)
        ' Get the source
        Dim Clauses As DbClauses = New DbClauses("TO_DELETE", DbClauses.ComparerType.Different, Nothing) _
            .And(New DbClauses("LANGUAGE", DbClauses.ComparerType.Equal, Language).Or("LANGUAGE", DbClauses.ComparerType.Equal, Bridge.Languages.Default))
        Dim Source As DataTable = MyBase.GetSource(Clauses, True)

        ' Merge the requested language with the default one for fill the empty translations
        Return (From Row In Source.AsEnumerable() Select Row!KEY, Row!VALUE) _
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
        Clauses.And("TO_DELETE", DbClauses.ComparerType.Different, Nothing)

        ' Get the filtered source
        Dim Source As DataTable = MyBase.GetSource(Clauses)

        ' Check if have a result
        If Source.Rows.Count > 0 Then
            ' Return the first
            Return Source.Rows(0)!VALUE
        Else
            ' Else
            Return Nothing
        End If
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
        Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)
        Values.Add("TO_DELETE", Now)

        ' Create the clauses
        Dim Clauses As DbClauses = DbClauses.Empty
        Clauses.And("KEY", DbClauses.ComparerType.Equal, Key)
        Clauses.And("LANGUAGE", DbClauses.ComparerType.Equal, Language)

        ' Update the table
        MyBase.Update(Values, Clauses)
    End Sub

    ' Update command
    Public Shadows Sub Update(Key As String, Value As String, Language As String)
        ' Get the old value if exists
        Dim OldValue As String = Me.GetValue(Key, Language)

        ' Check if exists
        If OldValue IsNot Nothing Then
            ' Insert a new row with the old value to delete
            Dim NewGuid As String = Utils.GUID.GuidToString
            Me.Insert(NewGuid, OldValue, Language)
            Me.Delete(NewGuid, Language)
        End If

        ' Create the clauses
        Dim Clauses As DbClauses = DbClauses.Empty
        Clauses.And("KEY", DbClauses.ComparerType.Equal, Key)
        Clauses.And("LANGUAGE", DbClauses.ComparerType.Equal, Language)

        ' Create the values to update
        Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)
        Values.Add("VALUE", Value)

        ' Call the base method
        MyBase.Update(Values, Clauses)
    End Sub

#End Region

End Class
