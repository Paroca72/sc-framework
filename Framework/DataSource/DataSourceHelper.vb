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

    ' Static
    Private Const CONCURRENTACCESS_COLUMNNAME As String = "SCFRAMEWORK_SESSIONID"

    ' Holders
    Private mDataSource As DataTable = Nothing
    Private mDataSourceLocker As Object = New Object()

    Private mSubordinates As List(Of DataSourceHelper) = Nothing
    Private mLastClauses As SCFramework.DbClauses = Nothing

    Private mStaticConcurrentAccessSafeMode = False
    Private mSessionID As String = Nothing

    Private mAddNewColumnWhenTranslate As Boolean = True
    Private mTranslatedColumnPrefix As String = "TRANS_"


#Region " CONSTRUCTOR "

    Public Sub New()
        ' Base
        MyBase.New()

        ' Init
        Me.mSubordinates = New List(Of DataSourceHelper)
        If Bridge.Session IsNot Nothing Then Me.mSessionID = Bridge.Session.SessionID
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

#Region " MULTILANGUAGES "

    ' Translate one column of the source
    Private Sub DoTranslateColumn(Source As DataTable, ColumnName As String, Manager As SCFramework.Multilanguages)
        ' Define the destination column name
        Dim DestinationColumn As String = ColumnName

        ' If must be create a new column
        If Me.mAddNewColumnWhenTranslate Then
            ' Create the new destination column name and if not exists create the new column
            DestinationColumn = Me.mTranslatedColumnPrefix & ColumnName
            If Not Source.Columns.Contains(DestinationColumn) Then
                Source.Columns.Add(DestinationColumn)
            End If
        End If

        ' Get all the translations
        ' TODO: check
        Dim Translations As Dictionary(Of String, String) = Manager.GetSource(Bridge.Languages.Current)

        ' Cycle all rows 
        For Each Row As DataRow In Source.Rows
            ' Translate
            Row(DestinationColumn) = Translations(ColumnName)
        Next
    End Sub

    ' Translate many columns of the source
    Private Sub DoTranslateColumns(Source As DataTable, Columns As List(Of String), Manager As SCFramework.Multilanguages)
        ' Cicle all the columns to translate
        For Each Column As String In Columns
            ' Translate the single column
            Me.DoTranslateColumn(Source, Column, Manager)
        Next
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

    ' Check for the static concurrent access safe mode column in the data source
    Private Sub CheckForStaticModeSafeColumn(Source As DataTable)
        ' Check for empty values
        If Source IsNot Nothing Then
            ' Check the state
            Select Case Me.mStaticConcurrentAccessSafeMode
                Case True
                    ' Check if already exists
                    If Not Source.Columns.Contains(DataSourceHelper.CONCURRENTACCESS_COLUMNNAME) Then
                        ' Add the column
                        Source.Columns.Add(DataSourceHelper.CONCURRENTACCESS_COLUMNNAME, GetType(String))
                    End If

                Case False
                    ' Check if exists
                    If Source.Columns.Contains(DataSourceHelper.CONCURRENTACCESS_COLUMNNAME) Then
                        ' Add the column
                        Source.Columns.Remove(DataSourceHelper.CONCURRENTACCESS_COLUMNNAME)
                    End If

            End Select
        End If
    End Sub

    ' Adjust the clauses considering is the static safe is active
    Private Function AdjustClauses(Clauses As SCFramework.DbClauses) As SCFramework.DbClauses
        ' Check for static concurrent access safe is active
        If Me.mStaticConcurrentAccessSafeMode And Me.mSessionID IsNot Nothing Then
            ' Create the session clauses
            Dim SessionClauses As DbClauses = New DbClauses(DataSourceHelper.CONCURRENTACCESS_COLUMNNAME, DbClauses.ComparerType.Equal, Nothing) _
                .Or(DataSourceHelper.CONCURRENTACCESS_COLUMNNAME, DbClauses.ComparerType.Equal, Bridge.Session.SessionID)

            ' Adjust
            Dim AdjustedClauses As SCFramework.DbClauses = DbClauses.Empty _
                .And(Clauses) _
                .And(SessionClauses)

            ' Return
            Return AdjustedClauses

        Else
            ' Return the original one
            Return Clauses
        End If
    End Function

    ' Get the data source holded in memory
    Private Function SelectDataSource(Clauses As SCFramework.DbClauses) As DataTable
        ' If the cluases not changed return the keep in memory source
        If Not Me.ClausesIsChanged(Clauses) And Not Me.mStaticConcurrentAccessSafeMode Then
            ' Return the current data source
            Return Me.mDataSource

        Else
            ' Get the source and check for empty values
            If Me.mDataSource IsNot Nothing Then
                ' Filter the source with the new clauses
                Return Me.mDataSource.AsEnumerable() _
                    .Select(Me.AdjustClauses(Clauses).ForFilter)
            End If
        End If

        ' Nohting
        Return Nothing
    End Function

    ' Create a new data source
    Private Function CreateNewDataSource(Clauses As SCFramework.DbClauses)
        ' Create the sql builder
        Dim SqlBuilder As DbSqlBuilder = New DbSqlBuilder() _
            .Table(Me.GetTableName()) _
            .Where(Clauses) _
            .Order(Me.OrderColumns)

        ' I must create a new datasource with the proper columns settings
        Dim Source As DataTable = Bridge.Query.Table(SqlBuilder.SelectCommand, Me.GetTableName())
        Source.CaseSensitive = False
        Source.Locale = CultureInfo.InvariantCulture

        ' Static safe column
        Me.CheckForStaticModeSafeColumn(Source)

        ' Data source columns settings
        If Me.PrimaryKeys.Count > 0 Then SCFramework.Utils.DataTable.SetPrimaryKeys(Source, Me.PrimaryKeys.ToArray)
        If Me.AutoNumbers.Count > 0 Then SCFramework.Utils.DataTable.SetAutoIncrements(Source, Me.AutoNumbers.ToArray)

        ' Multilanguages columns
        Me.DoTranslateColumns(Source, Me.TranslateColumns, New SCFramework.Translations())
        Me.DoTranslateColumns(Source, Me.ImageColumns, New SCFramework.Files())
        Me.DoTranslateColumns(Source, Me.FileColumns, New SCFramework.Files())

        ' Accept the changes and return the source
        Source.AcceptChanges()
        Return Source
    End Function

    ' Add the session column id information
    Private Sub AddSessionID(Values As IDictionary(Of String, Object))
        ' Check for empty values
        If Me.mSessionID IsNot Nothing And Me.mStaticConcurrentAccessSafeMode And
            Not Values.ContainsKey(DataSourceHelper.CONCURRENTACCESS_COLUMNNAME) Then
            ' Add the session column
            Values.Add(DataSourceHelper.CONCURRENTACCESS_COLUMNNAME, Me.mSessionID)
        End If
    End Sub

    Private Sub AddSessionID(Row As DataRow)
        ' Check for empty values
        If Me.mSessionID IsNot Nothing And Me.mStaticConcurrentAccessSafeMode And
            Row.Table.Columns.Contains(DataSourceHelper.CONCURRENTACCESS_COLUMNNAME) Then
            ' Add the session column
            Row(DataSourceHelper.CONCURRENTACCESS_COLUMNNAME) = Me.mSessionID
        End If
    End Sub

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
            Select Case Me.Query.GetProvider()
                Case "System.Data.OleDb" : AddHandler CType(Adapter, OleDb.OleDbDataAdapter).RowUpdated, AddressOf HandleOldDbRowUpdated
                Case "System.DataSqlClient" : AddHandler CType(Adapter, SqlClient.SqlDataAdapter).RowUpdated, AddressOf HandleSqlRowUpdated
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
            ' Create a temp dataset
            Dim DS As DataSet = New DataSet()
            DS.Tables.Add(Me.mDataSource)

            ' Check
            Return DS.HasChanges
        End Get
    End Property

    ' Return the if memory managed
    Public ReadOnly Property IsMemoryManaged() As Boolean
        Get
            Return Me.mDataSource IsNot Nothing
        End Get
    End Property

    ' Set/get the static concurrent access safe mode to the data source
    Public Property StaticConcurrentAccessSafeMode As Boolean
        Get
            Return Me.mStaticConcurrentAccessSafeMode
        End Get
        Set(value As Boolean)
            Me.mStaticConcurrentAccessSafeMode = value
            Me.CheckForStaticModeSafeColumn(Me.mDataSource)
        End Set
    End Property

#End Region

#Region " PUBLIC "

    ' Set the data table as a source filtered by where clausole.
    ' If KeepInMemory is true the classes will be managed in mamory and all methods will be applied on the table stored.
    ' Else when you will use a methos as Insert, Delete or Update will have effect directly on the database.
    Public Overridable Function GetSource(Optional Clauses As SCFramework.DbClauses = Nothing,
                                          Optional KeepInMemory As Boolean = False) As DataTable
        ' Hold the source
        If Me.IsMemoryManaged Then
            ' if memory managed select the source from the memory
            GetSource = Me.SelectDataSource(Clauses)

        Else
            ' If NOT memory managed create a new datasource
            GetSource = Me.CreateNewDataSource(Clauses)
        End If

        ' Hold the status is needed 
        If KeepInMemory Then
            Me.mDataSource = GetSource
            Me.mLastClauses = Clauses
        End If
    End Function

    ' Delete command
    Public Overrides Function Delete(Clauses As DbClauses) As Long
        ' Check for safety
        If (Me.Safety) And (Clauses Is Nothing OrElse Clauses.IsEmpty) Then
            Throw New Exception("This command will delete all row in the table and related subordinate table items!")
        End If

        Try
            ' Select the case 
            Select Case Me.IsMemoryManaged
                Case True
                    '--------------------------------------------------------------------------
                    ' MEMORY MANAGED

                    ' Get the filtered source table
                    Dim Source As DataTable = Me.SelectDataSource(Clauses)

                    ' Cycle subortdinates for delete the references
                    For Each Subordinate As DataSourceHelper In Me.mSubordinates
                        ' Cycle rows and for each row to delete extract the pairs key
                        For Each Row As DataRow In Source.Rows
                            ' Exctract the current primary keys and delete the items inside the subordinate
                            Dim CurrentClauses As DbClauses = DbClauses.Empty.And(Me.ExtractLocalKeysPairs(Row), DbClauses.ComparerType.Equal)
                            Subordinate.Delete(CurrentClauses)
                        Next
                    Next

                    ' Lock the data source
                    SyncLock Me.DataSourceLocker
                        ' Delete all row in the view
                        For Each Row As DataRow In Source.Rows
                            ' Delete and store the current row
                            Me.AddSessionID(Row)
                            Row.Delete()
                        Next
                    End SyncLock

                    ' Return the deleted rows count
                    Return Source.Rows.Count

                Case False
                    '--------------------------------------------------------------------------
                    ' DATABASE MANAGED

                    ' Access direct to the database method
                    Return MyBase.Delete(Clauses)

            End Select
        Catch ex As Exception
            ' If an error roll back and propagate the exception
            If Me.IsMemoryManaged Then Me.RejectChanges()
            Throw ex
        End Try
    End Function

    ' Insert command
    Public Overrides Function Insert(Values As Dictionary(Of String, Object)) As Long
        Try
            ' Select the case 
            Select Case Me.IsMemoryManaged
                Case True
                    '--------------------------------------------------------------------------
                    ' MEMORY MANAGED

                    ' Add the session is column to the values list if needed
                    Me.AddSessionID(Values)

                    ' Create the new record from the current datatable structure
                    Dim NewRow As DataRow = Me.mDataSource.NewRow

                    ' Fill the row cycling all the field inside the values list.
                    For Each Field As String In Values.Keys
                        ' If the field exists write the value
                        If Me.mDataSource.Columns.Contains(Field) Then
                            NewRow(Field) = Values(Field)
                        End If
                    Next

                    ' Insert the new ID
                    Me.mDataSource.Rows.Add(NewRow)

                    ' Check if exists an identity field
                    Dim IdentityField As String = Me.ExtractIdentityField(Me.mDataSource)
                    If IdentityField IsNot Nothing AndAlso IsNumeric(NewRow(IdentityField)) Then
                        ' Return the identity field value
                        Return NewRow(IdentityField)

                    Else
                        ' Else return nothing
                        Return -1
                    End If

                Case False
                    '--------------------------------------------------------------------------
                    ' DATABASE MANAGED

                    ' Access direct to the database method
                    Return MyBase.Insert(Values)

            End Select

        Catch ex As Exception
            ' If an error roll back and propagate the exception
            If Me.IsMemoryManaged Then Me.RejectChanges()
            Throw ex
        End Try
    End Function

    ' Update command
    Public Overrides Function Update(Values As Dictionary(Of String, Object), Clauses As SCFramework.DbClauses) As Long
        ' Check for safety
        If (Me.Safety) And (Clauses Is Nothing OrElse Clauses.IsEmpty) Then
            Throw New Exception("This command will update all row in the table!")
        End If

        Try
            ' Select the case 
            Select Case Me.IsMemoryManaged
                Case True
                    '--------------------------------------------------------------------------
                    ' MEMORY MANAGED

                    ' Add the session is column to the values list if needed
                    Me.AddSessionID(Values)

                    ' Get the filtered source table
                    Dim Source As DataTable = Me.SelectDataSource(Clauses)

                    ' Lock the data source
                    SyncLock Me.DataSourceLocker
                        ' Cycle all rows in the view
                        For Each Row As DataRow In Source.Rows
                            ' Fill the row cycling all the field inside the values list.
                            For Each Field As String In Values.Keys
                                ' If the field exists write the value
                                If Source.Columns.Contains(Field) Then
                                    Row(Field) = Values(Field)
                                End If
                            Next
                        Next
                    End SyncLock

                    ' Return the updated rows count
                    Return Source.Rows.Count

                Case False
                    '--------------------------------------------------------------------------
                    ' DATABASE MANAGED

                    ' Access direct to the database method
                    Return MyBase.Update(Values, Clauses)

            End Select

        Catch ex As Exception
            ' If an error roll back and propagate the exception
            If Me.IsMemoryManaged Then Me.RejectChanges()
            Throw ex
        End Try
    End Function

    ' Fix the changes on the database using the data source held in memory
    Public Overridable Sub AcceptChanges()
        ' Only if work in memory
        If Not Me.IsMemoryManaged Then Exit Sub

        ' Get the current query object and determine if must manage the transaction
        Dim Query As SCFramework.DbQuery = Me.Query
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
    End Sub

    ' Reject the soure changes and also on all the subordinates
    Public Overridable Sub RejectChanges()
        ' Only if work in memory
        If Not Me.IsMemoryManaged Then Exit Sub

        ' Cycle all the subordinates for rejectr the changes
        For Each Subordinate As DataSourceHelper In Me.mSubordinates
            Subordinate.RejectChanges()
        Next

        ' Reject the source changes
        Me.mDataSource.RejectChanges()
    End Sub

    ' Force to reload data source using the last clauses at the next source access
    Public Overridable Sub CleanDataSouce()
        Me.mDataSource = Nothing
    End Sub

#End Region

End Class
