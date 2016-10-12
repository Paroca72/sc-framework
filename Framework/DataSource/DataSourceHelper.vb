'*************************************************************************************************
' 
' [SCFramework]
' DataSourceHelper
' by Samuele Carassai
'
' Data source helper
' Version 5.0.0
' Created 10/10/2016
' Updated 11/10/2016
'
'*************************************************************************************************


Public MustInherit Class DataSourceHelper
    Inherits DbHelper

    ' Holders
    Private mDataSource As DataTable = Nothing
    Private mDataSourceLocker As Object = New Object()
    Private mSubordinates As List(Of DataSourceHelper) = Nothing
    Private mWaitBeforeUpdate As Boolean = False


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

#End Region

#Region " PROPERTIES "

    ' Get the data source locker object
    Public ReadOnly Property DataSourceLocker As Object
        Get
            Return Me.mDataSourceLocker
        End Get
    End Property

    ' Get the data table filtered
    Public ReadOnly Property Source() As DataTable
        Get
            If Me.mDataSource Is Nothing Then Me.SetSource()
            Return Me.mDataSource
        End Get
    End Property

#End Region

#Region " PROTECTED "

    ' Set the data table as a source filtered by where clausole
    Protected Overridable Function SetSource(Optional Clauses As DbClauses = Nothing) As DataTable
        ' Source
        Me.mDataSource = Bridge.Query.Table(Me.GetTableName(), Nothing, Clauses)
        Me.mDataSource.CaseSensitive = False
        Me.mDataSource.Locale = CultureInfo.InvariantCulture

        ' Primary key
        If Me.PrimaryKeys.Count > 0 Then
            SCFramework.Utils.DataTable.SetPrimaryKeys(Me.mDataSource, Me.PrimaryKeys.ToArray)
        End If

        ' Auto number
        If Me.AutoNumbers.Count > 0 Then
            SCFramework.Utils.DataTable.SetAutoIncrements(Me.mDataSource, Me.AutoNumbers.ToArray)
        End If

        ' Return the filtered table
        Return Me.mDataSource
    End Function

    ' Delete command
    Protected Overridable Shadows Function Delete(Clauses As DbClauses) As Long
        ' Check for safety
        If (Me.Safety) And (Clauses Is Nothing OrElse Clauses.IsEmpty) Then
            Throw New Exception("This command will delete all row in the table and related subordinate table items!")
        End If

        ' If the source is nothing load all
        If Me.mDataSource Is Nothing Then Me.SetSource()

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
                    Row.Delete()
                Next
            End SyncLock

            ' Return the number or deleted items
            Return View.Count

        Catch ex As Exception
            ' If an error roll back and propagate the exception
            Me.RejectChanges()
            Throw ex
        End Try
    End Function

    ' Insert command
    Protected Overridable Shadows Function Insert(Values As IDictionary(Of String, Object)) As Long
        ' If the source is nothing load all
        If Me.mDataSource Is Nothing Then Me.SetSource()

        ' Create the new row
        Dim NewRow As DataRow = Me.mDataSource.NewRow

        ' Fill the row cycling all the field inside the values list.
        For Each Field As String In Values.Keys
            ' If the field exists write the value
            If Me.mDataSource.Columns.Contains(Field) Then
                NewRow(Field) = Values(Field)
            End If
        Next

        ' Insert
        Me.mDataSource.Rows.Add(NewRow)
    End Function

    ' Update command
    Protected Overridable Shadows Function Update(Values As IDictionary(Of String, Object), Clauses As SCFramework.DbClauses) As Long
        ' Check for safety
        If (Me.Safety) And (Clauses Is Nothing OrElse Clauses.IsEmpty) Then
            Throw New Exception("This command will update all row in the table!")
        End If

        ' If the source is nothing load all
        If Me.mDataSource Is Nothing Then Me.SetSource()

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

        ' Return the updated items count
        Return View.Count
    End Function

    ' Fix the changes on the database
    Protected Overridable Function UpdateDataBase(Optional Query As SCFramework.DbQuery = Nothing) As Boolean
        ' If the query is nothing create a new one
        Dim Starter As Boolean = Query Is Nothing
        If Starter Then
            Query = New SCFramework.DbQuery()
        End If

        ' If the source is nothing load all
        If Me.mDataSource Is Nothing Then Me.SetSource()

        Try
            ' Start transaction if needed
            If Starter Then Query.StartTransaction()

            ' Lock the data source and try to update
            SyncLock Me.DataSourceLocker
                Query.UpdateDatabase(Me.mDataSource)
            End SyncLock

            ' Cycle subortdinates for update
            For Each Subordinate As DataSourceHelper In Me.mSubordinates
                Subordinate.UpdateDataBase(Query)
            Next

            ' Commit the transaction
            If Starter Then Query.CommitTransaction()

        Catch ex As Exception
            ' Rollback
            If Starter Then Query.RollBackTransaction()
        End Try
    End Function

#End Region

#Region " PUBLIC "

    ' Get the data table filtered by where clausole
    Public Function Filter(Optional Clauses As DbClauses = Nothing) As DataTable
        ' If the source is nothing load all
        If Me.mDataSource Is Nothing Then Me.SetSource()

        ' Filter the source
        Dim View As DataView = New DataView(Me.mDataSource)
        View.RowFilter = Clauses.ForFilter

        ' Return the new datasource
        Return View.ToTable()
    End Function

    ' Reject the soure changes and also on all the subordinates
    Public Sub RejectChanges()
        ' Reject the source changes
        Me.Source.RejectChanges()

        ' Cycle all the subordinates for rejectr the changes
        For Each Subordinate As DataSourceHelper In Me.mSubordinates
            Subordinate.RejectChanges()
        Next
    End Sub

#End Region

End Class
