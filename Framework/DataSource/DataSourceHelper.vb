'*************************************************************************************************
' 
' [SCFramework]
' DataSourceHelper
' by Samuele Carassai
'
' Data source helper
' Version 5.0.0
' Created 10/10/2016
' Updated 19/10/2016
'
'*************************************************************************************************


Public MustInherit Class DataSourceHelper
    Inherits DbHelperExtended

    ' Holders
    Private mDataSource As DataTable = Nothing
    Private mDataSourceLocker As Object = New Object()

    Private mSubordinates As List(Of DataSourceHelper) = Nothing
    Private mLastClauses As SCFramework.DbClauses = Nothing


#Region " CONSTRUCTOR "

    Public Sub New()
        ' Base class
        MyBase.New()

        ' Init
        Me.mSubordinates = New List(Of DataSourceHelper)
    End Sub

#End Region

#Region " SUBORDINATES "

    ' Find a subortdinate table inside the list
    Public Overridable Function FindSubordinate(TableName As String) As DataSourceHelper
        ' Cycle all subortdinates
        For Each Subordinate As DataSourceHelper In Me.mSubordinates
            ' Compare the table name
            If String.Compare(Subordinate.GetTableName(), TableName, True) Then
                Return Subordinate
            End If
        Next
        ' Not find
        Return Nothing
    End Function

    ' Add a subortdinate DataSourceHelper object
    Public Overridable Sub AddSubordinate(Subordinate As DataSourceHelper)
        Me.mSubordinates.Add(Subordinate)
    End Sub

    ' Remove a subordinate finded by the table name
    Public Overridable Sub RemoveSubordinate(TableName As String)
        ' Find the object
        Dim Subordinate As DataSourceHelper = Me.FindSubordinate(TableName)
        ' Check for the result
        If Subordinate IsNot Nothing Then
            ' Try to remove
            Me.mSubordinates.Remove(Subordinate)
        End If
    End Sub

#End Region

#Region " PRIVATES "

    ' Extract the key pairs from a datarow
    Private Function ExtractLocalKeysPairs(Row As DataRow) As Dictionary(Of String, Object)
        ' Holder
        Dim Pairs As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()

        ' Cycle all primary keys list
        For Each Key As String In Me.PrimaryKeys
            ' If exists add the key and the value at the list
            If Row.Table.Columns.Contains(Key) Then
                Pairs.Add(Key, Row(Key))
            End If
        Next

        ' Return the list
        Return Pairs
    End Function

    ' Check if the clauses is same of the last in memory
    Private Function ClausesIsChanged(NewClauses As SCFramework.DbClauses) As Boolean
        ' Check for null values
        If NewClauses Is Nothing And Me.mLastClauses Is Nothing Then
            Return False
        ElseIf NewClauses IsNot Nothing Then
            ' Compare
            Return NewClauses.IsEqual(Me.mLastClauses)
        Else
            ' It means one is nothing and one no.
            Return True
        End If
    End Function

    ' Return the if memory managed
    Private ReadOnly Property IsMemoryManaged() As Boolean
        Get
            Return Me.mDataSource Is Nothing
        End Get
    End Property

#End Region

#Region " DATABASE "

    ' Holde the identity column name
    Private IdentityColumnName As String = Nothing

    ' Get the first identity column name
    Private Function ExtractIdentityField(Source As DataTable) As String
        ' Cycle all keys
        For Each Column As DataColumn In Source.Columns
            ' Check for autonumber
            If Me.AutoNumbers.Contains(Column.ColumnName) And
                Me.PrimaryKeys.Contains(Column.ColumnName) Then Return Column.ColumnName
        Next
        ' Else return nothing
        Return Nothing
    End Function

    ' Update the identity field
    Private Sub HandleOldDbRowUpdated(ByVal sender As Object, ByVal e As OleDb.OleDbRowUpdatedEventArgs)
        If e.Status = UpdateStatus.Continue AndAlso e.StatementType = StatementType.Insert Then
            ' Get the Identity column value
            e.Row(Me.IdentityColumnName) = Me.Query.Indentity()
            e.Row.AcceptChanges()
        End If
    End Sub

    Private Sub HandleSqlRowUpdated(ByVal sender As Object, ByVal e As SqlClient.SqlRowUpdatedEventArgs)
        If e.Status = UpdateStatus.Continue AndAlso e.StatementType = StatementType.Insert Then
            ' Get the Identity column value
            e.Row(Me.IdentityColumnName) = Me.Query.Indentity()
            e.Row.AcceptChanges()
        End If
    End Sub

    ' Update the database
    Private Sub UpdateDatabase(ByVal Source As DataTable)
        ' Create the adapter and check for empty value
        Dim Adapter As Common.DbDataAdapter = Me.Query.CreateAdapter(Me.GetTableName())
        If Adapter Is Nothing Then Exit Sub

        ' Get the identity column name
        Me.IdentityColumnName = Me.ExtractIdentityField(Source)

        ' Select the case by the provider only if have an identity field to update
        If Me.IdentityColumnName IsNot Nothing Then
            Select Case Me.Query.GetProvider
                Case SCFramework.DbQuery.ProvidersList.OleDb
                    AddHandler CType(Adapter, OleDb.OleDbDataAdapter).RowUpdated, AddressOf HandleOldDbRowUpdated

                Case SCFramework.DbQuery.ProvidersList.SqlClient
                    AddHandler CType(Adapter, SqlClient.SqlDataAdapter).RowUpdated, AddressOf HandleSqlRowUpdated

            End Select
        End If

        ' Set the adapter and call the update
        Adapter.ContinueUpdateOnError = False
        Adapter.Update(Source)
    End Sub

#End Region

#Region " PROPERTIES "

    ' Get the data source locker object
    Public ReadOnly Property DataSourceLocker As Object
        Get
            Return Me.mDataSourceLocker
        End Get
    End Property

    ' True if has changes
    Public ReadOnly Property HasChanges() As Boolean
        Get
            Return Me.mDataSource IsNot Nothing AndAlso Me.mDataSource.GetChanges().Rows.Count > 0
        End Get
    End Property

#End Region

#Region " PUBLIC "

    ' Set the data table as a source filtered by where clausole
    Public Overridable Function GetSource(Optional Clauses As SCFramework.DbClauses = Nothing,
                                          Optional KeepInMemory As Boolean = False) As DataTable
        ' Hold the source
        Dim Source As DataTable = Me.mDataSource

        ' Check if hove something held in memory
        If Source IsNot Nothing Then
            ' If the cluases not changed return the keep in memory source
            If Not Me.ClausesIsChanged(Clauses) Then
                Return Source

            Else
                ' Filter the source with the new clauses
                Source = Source.AsEnumerable().Select(Clauses.ForFilter)
            End If

        Else
            ' I must create a new datasource with the proper columns settings
            Source = Bridge.Query.Table(Me.GetTableName(), Nothing, Clauses)
            Source.CaseSensitive = False
            Source.Locale = CultureInfo.InvariantCulture

            ' Data source columns settings
            If Me.PrimaryKeys.Count > 0 Then SCFramework.Utils.DataTable.SetPrimaryKeys(Source, Me.PrimaryKeys.ToArray)
            If Me.AutoNumbers.Count > 0 Then SCFramework.Utils.DataTable.SetAutoIncrements(Source, Me.AutoNumbers.ToArray)
            If Me.OrderColumns.Count > 0 Then Source = Source.AsEnumerable().OrderBy("", Me.OrderColumns)

            ' TODO: all other columns
        End If

        ' Hold the status is needed 
        If KeepInMemory Then
            Me.mDataSource = Source
            Me.mLastClauses = Clauses
        End If

        ' Return
        Return Source
    End Function

    ' Delete command
    Public Overrides Function Delete(Clauses As DbClauses) As Long
        ' Check for safety
        If (Me.Safety) And (Clauses Is Nothing OrElse Clauses.IsEmpty) Then
            Throw New Exception("This command will delete all row in the table and related subordinate table items!")
        End If

        Try
            ' Get the filtered table and check for empty values
            Dim Source As DataTable = IIf(Me.IsMemoryManaged, Me.mDataSource, Me.GetSource(Clauses))
            If Source Is Nothing Then Return 0

            ' Cycle subortdinates for delete the references
            For Each Subordinate As DataSourceHelper In Me.mSubordinates
                ' Cycle rows and for each row to delete extract the pairs key
                For Each Row As DataRow In Source.Rows
                    ' Exctract the current primary keys and delete the items inside the subordinate
                    Subordinate.Delete(New DbClauses(Me.ExtractLocalKeysPairs(Row)))
                Next
            Next

            ' Lock the data source
            SyncLock Me.DataSourceLocker
                ' Delete all row in the view
                For Each Row As DataRow In Source.Rows
                    ' Delete and store the current row
                    Row.Delete()
                Next
            End SyncLock

            ' If if not memory managed update the database
            If Not Me.IsMemoryManaged Then
                Me.UpdateDatabase(Source)
            End If

            ' Return the deleted rows count
            Return Source.Rows.Count

        Catch ex As Exception
            ' If an error roll back and propagate the exception
            Me.RejectChanges()
            Throw ex
        End Try
    End Function

    ' Insert command
    Public Overrides Function Insert(Values As IDictionary(Of String, Object)) As Long
        ' Get the filtered table and check for empty values
        Dim Source As DataTable = IIf(Me.IsMemoryManaged,
                                      Me.mDataSource,
                                      Me.GetSource(SCFramework.DbClauses.AlwaysFalse))
        If Source Is Nothing Then Return 0

        ' Create the new row
        Dim NewRow As DataRow = Me.mDataSource.NewRow

        Try
            ' Fill the row cycling all the field inside the values list.
            For Each Field As String In Values.Keys
                ' If the field exists write the value
                If Me.mDataSource.Columns.Contains(Field) Then
                    NewRow(Field) = Values(Field)
                End If
            Next

            ' Insert the new ID
            Me.mDataSource.Rows.Add(NewRow)

            ' If if not memory managed update the database
            If Not Me.IsMemoryManaged Then
                Me.UpdateDatabase(Source)
            End If

            ' Check if exists an identity field
            Dim IdentityField As String = Me.ExtractIdentityField(Source)
            If IdentityField IsNot Nothing And IsNumeric(NewRow(IdentityField)) Then
                ' Return the identity field value
                Return NewRow(IdentityField)

            Else
                ' Else return nothing
                Return -1
            End If

        Catch ex As Exception
            ' If an error roll back and propagate the exception
            Source.RejectChanges()
            Throw ex
        End Try
    End Function

    ' Update command
    Public Overrides Function Update(Values As IDictionary(Of String, Object), Clauses As SCFramework.DbClauses) As Long
        ' Check for safety
        If (Me.Safety) And (Clauses Is Nothing OrElse Clauses.IsEmpty) Then
            Throw New Exception("This command will update all row in the table!")
        End If

        ' Get the filtered table and check for empty values
        Dim Source As DataTable = IIf(Me.IsMemoryManaged, Me.mDataSource, Me.GetSource(Clauses))
        If Source Is Nothing Then Return 0

        Try
            ' Lock the data source
            SyncLock Me.DataSourceLocker
                ' Cycle all rows in the view
                For Each Row As DataRow In Source.Rows
                    ' Fill the row cycling all the field inside the values list.
                    For Each Field As String In Values.Keys
                        ' If the field exists write the value
                        If Me.mDataSource.Columns.Contains(Field) Then
                            Row(Field) = Values(Field)
                        End If
                    Next
                Next
            End SyncLock

            ' If if not memory managed update the database
            If Not Me.IsMemoryManaged Then
                Me.UpdateDatabase(Source)
            End If

            ' Return the updated rows count
            Return Source.Rows.Count

        Catch ex As Exception
            ' If an error roll back and propagate the exception
            Source.RejectChanges()
            Throw ex
        End Try
    End Function

    ' Fix the changes on the database using the data source held in memory
    Public Overridable Function AcceptChanges() As Boolean
        ' Get the current query object
        Dim Query As SCFramework.DbQuery = Me.Query
        ' Determine if must manage the transaction
        Dim TransactionOwner As Boolean = Not Query.InTransaction

        Try
            ' Check if not within a transaction
            If TransactionOwner Then Query.StartTransaction()

            ' Cycle subortdinates for update
            For Each Subordinate As DataSourceHelper In Me.mSubordinates
                Subordinate.AcceptChanges()
            Next

            ' Lock the data source and try to update
            SyncLock Me.DataSourceLocker
                Query.UpdateDatabase(Me.mDataSource)
            End SyncLock

            ' Commit the transaction is needed
            If TransactionOwner Then Query.CommitTransaction()

        Catch ex As Exception
            ' Rollback the transaction is needed and propagate the exception
            If TransactionOwner Then Query.RollBackTransaction()
            Throw ex

        End Try
    End Function

    ' Reject the soure changes and also on all the subordinates
    Public Overridable Sub RejectChanges()
        ' Cycle all the subordinates for rejectr the changes
        For Each Subordinate As DataSourceHelper In Me.mSubordinates
            Subordinate.RejectChanges()
        Next

        ' Reject the source changes
        Me.mDataSource.RejectChanges()
    End Sub

    ' Force to reload data source using the last cluases at the next source access
    Public Sub CleanDataSouce()
        Me.mDataSource = Nothing
    End Sub

#End Region

End Class
