'*************************************************************************************************
' 
' [SCFramework]
' DataSourceHelper
' by Samuele Carassai
'
' Data source helper
' Version 5.0.0
' Created 10/10/2016
' Updated 16/10/2016
'
'*************************************************************************************************


Public MustInherit Class DataSourceHelper
    Inherits DbHelperExtended

    ' Holders
    Private mDataSource As DataTable = Nothing
    Private mDataSourceLocker As Object = New Object()
    Private mHasChanges As Boolean = False

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

#Region " DATA MANAGEMENT "

    ' Delete in memory
    Private Function DeleteInMemory(Clauses As DbClauses) As Long
        Try
            ' Get the filtered view
            Dim View As DataView = New DataView(Me.mDataSource)
            View.RowFilter = Clauses.ForFilter

            ' Cycle subortdinates for delete the references
            For Each Subordinate As DataSourceHelper In Me.mSubordinates
                ' Cycle rows and for each row to delete extract the pairs key
                For Each Row As DataRowView In View
                    ' Exctract the current primary keys and delete the items inside the subordinate
                    Subordinate.Delete(New DbClauses(Me.ExtractLocalKeysPairs(Row.Row)))
                Next
            Next

            ' Lock the data source
            SyncLock Me.DataSourceLocker
                ' Delete all row in the view
                For Each Row As DataRowView In View
                    ' Delete and store the current row
                    Row.Delete()
                Next
            End SyncLock

            ' Return the deleted rows count
            Return View.Count

        Catch ex As Exception
            ' If an error roll back and propagate the exception
            Me.RejectChanges()
            Throw ex
        End Try
    End Function

    ' Delete on database
    Private Function DeleteOnDataBase(Clauses As DbClauses) As Long
        ' Get the current query object
        Dim Query As SCFramework.DbQuery = Me.Query
        ' Determine if must manage the transaction
        Dim TransactionOwner As Boolean = Not Query.InTransaction

        Try
            ' Check if not within a transaction
            If TransactionOwner Then Query.StartTransaction()

            ' Get the source by the clauses
            Dim Source As DataTable = Me.GetSource(Clauses)

            ' Cycle subortdinates for delete the references
            For Each Subordinate As DataSourceHelper In Me.mSubordinates
                ' Cycle rows and for each row to delete extract the pairs key
                For Each Row As DataRow In Source.Rows
                    ' Exctract the current primary keys and delete the items inside the subordinate
                    Dim Cluases As SCFramework.DbClauses = New DbClauses(Me.ExtractLocalKeysPairs(Row))
                    Subordinate.Delete(Cluases)
                Next
            Next

            ' Call the base method to delete the records in the current table
            DeleteOnDataBase = MyBase.Delete(Clauses)

            ' Commit the transaction is needed
            If TransactionOwner Then Query.CommitTransaction()

        Catch ex As Exception
            ' Rollback the transaction is needed and propagate the exception
            If TransactionOwner Then Query.RollBackTransaction()
            Throw ex

        End Try
    End Function

    ' Get the autonumber first value 
    Private Function GetIdentityFromMemory(Row As DataRow) As Long
        ' Extract the identity if exists
        Dim Keys As Dictionary(Of String, Object) = Me.ExtractLocalKeysPairs(Row)

        ' Cycle all keys
        For Each Pairs As KeyValuePair(Of String, Object) In Keys
            ' Check for autonumber and is is number
            If Me.AutoNumbers.Contains(Pairs.Key) AndAlso IsNumeric(Pairs.Value) Then
                ' Return
                Return Pairs.Value
            End If
        Next

        ' Else return -1
        Return -1
    End Function

    ' Insert in memory
    Private Function InsertInMemory(Values As IDictionary(Of String, Object)) As Long
        ' Create the new row
        Dim NewRow As DataRow = Me.mDataSource.NewRow

        ' Fill the row cycling all the field inside the values list.
        For Each Field As String In Values.Keys
            ' If the field exists write the value
            If Me.mDataSource.Columns.Contains(Field) Then
                NewRow(Field) = Values(Field)
            End If
        Next

        ' Insert and return the new ID
        Me.mDataSource.Rows.Add(NewRow)
        Return Me.GetIdentityFromMemory(NewRow)
    End Function

    ' Update in memory
    Private Function UpdateInMemory(Values As IDictionary(Of String, Object), Clauses As SCFramework.DbClauses) As Long
        ' Get the filtered view
        Dim View As DataView = New DataView(Me.mDataSource)
        View.RowFilter = Clauses.ForFilter

        ' Lock the data source
        SyncLock Me.DataSourceLocker
            ' Cycle all rows in the view
            For Each Row As DataRowView In View
                ' Fill the row cycling all the field inside the values list.
                For Each Field As String In Values.Keys
                    ' If the field exists write the value
                    If Me.mDataSource.Columns.Contains(Field) Then
                        Row(Field) = Values(Field)
                    End If
                Next
            Next
        End SyncLock

        ' Return the updated rows count
        Return View.Count
    End Function

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
            Return Me.mHasChanges
        End Get
    End Property

#End Region

#Region " PROTECTED "

    ' Set the data table as a source filtered by where clausole
    Public Overridable Function GetSource(Optional Clauses As DbClauses = Nothing, Optional KeepInMemory As Boolean = False) As DataTable
        ' Check for cache
        If Me.mDataSource IsNot Nothing And
            Not Me.ClausesIsChanged(Clauses) Then Return Me.mDataSource

        ' Store the clauses
        Me.mLastClauses = Clauses

        ' Source
        Dim Source As DataTable = Bridge.Query.Table(Me.GetTableName(), Nothing, Me.mLastClauses)
        Source.CaseSensitive = False
        Source.Locale = CultureInfo.InvariantCulture

        ' Data source columns settings
        If Me.PrimaryKeys.Count > 0 Then SCFramework.Utils.DataTable.SetPrimaryKeys(Source, Me.PrimaryKeys.ToArray)
        If Me.AutoNumbers.Count > 0 Then SCFramework.Utils.DataTable.SetAutoIncrements(Source, Me.AutoNumbers.ToArray)

        ' TODO: all other columns

        ' Hold the status is needed
        If KeepInMemory Then
            Me.mDataSource = Source
            Me.mHasChanges = False
        End If

        ' Return the filtered table
        Return Me.mDataSource
    End Function

    ' Delete command
    Public Overrides Function Delete(Clauses As DbClauses) As Long
        ' Check for safety
        If (Me.Safety) And (Clauses Is Nothing OrElse Clauses.IsEmpty) Then
            Throw New Exception("This command will delete all row in the table and related subordinate table items!")
        End If

        ' Check the case
        Select Case Me.IsMemoryManaged
            Case True
                ' Delete from memeory
                Delete = Me.DeleteInMemory(Clauses)
                Me.mHasChanges = Me.mHasChanges Or Delete > 0

            Case False
                ' Delete directly from database
                Delete = Me.DeleteOnDataBase(Clauses)

        End Select
    End Function

    ' Insert command
    Public Overrides Function Insert(Values As IDictionary(Of String, Object)) As Long
        ' Check the case
        Select Case Me.IsMemoryManaged
            Case True
                ' Delete directly from database
                Insert = Me.InsertInMemory(Values)
                Me.mHasChanges = Me.mHasChanges Or Insert > 0

            Case False
                ' Delete from memeory
                Insert = MyBase.Insert(Values)

        End Select
    End Function

    ' Update command
    Public Overrides Function Update(Values As IDictionary(Of String, Object), Clauses As SCFramework.DbClauses) As Long
        ' Check for safety
        If (Me.Safety) And (Clauses Is Nothing OrElse Clauses.IsEmpty) Then
            Throw New Exception("This command will update all row in the table!")
        End If

        ' Check the case
        Select Case Me.IsMemoryManaged
            Case True
                ' Delete directly from database
                Update = Me.UpdateInMemory(Values, Clauses)
                Me.mHasChanges = Me.mHasChanges Or Update > 0

            Case False
                ' Delete from memeory
                Update = MyBase.Update(Values, Clauses)

        End Select
    End Function

#End Region

#Region " PUBLIC "

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
            Me.mHasChanges = False

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
        Me.mHasChanges = False
    End Sub

    ' Force to reload data source using the last cluases at the next source access
    Public Sub CleanDataSouce()
        Me.mDataSource = Nothing
        Me.mHasChanges = False
    End Sub

#End Region

End Class
