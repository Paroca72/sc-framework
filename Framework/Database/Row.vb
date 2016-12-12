' Define the name space
Namespace DB

    ' Public class
    Public MustInherit Class Row
        Inherits System.Dynamic.DynamicObject

        '------------------------------------------------------------------------------
        ' CONSTRUCTOR

        Public Sub New()
            ' Init
            Me.mMembers = New Dictionary(Of String, Cell)
        End Sub


        '------------------------------------------------------------------------------
        ' PRIVATES

        ' The holders
        Private mMembers As Dictionary(Of String, Cell) = Nothing
        Protected mBelongingTable As Table = Nothing


        ' Check the belongings of this cell for empty values.
        ' In this case some methods of this class will not work proper and throw an exception.
        Private Sub CheckBelongings()
            ' Check the table
            If Me.Table Is Nothing Then
                Throw New Exception("Row's table belonging must be defined before use this class.")
            End If
        End Sub


        ' Given as list of column retrieve a list of pair values.
        Private Function GetColumnValues(ParamArray Columns() As String) As Dictionary(Of String, Object)
            ' Init the dictionary
            GetColumnValues = New Dictionary(Of String, Object)

            ' Cycle all the columns name 
            For Each ColumnName As String In Columns
                ' If the column is not already inside the disctionary add the
                ' column name and its value.
                If Me.HasColumn(ColumnName) Then
                    GetColumnValues.Add(ColumnName, Me(ColumnName).Value)
                End If
            Next
        End Function


        ' Retrieve the identity column name.
        Private Function GetIdentityColumnName() As String
            ' Check the belonging as after we need to retrieve the information about the onwer
            ' table columns.
            Me.CheckBelongings()

            ' Retrive all the name of identities columns from the owner table and return the first. 
            Dim IdentitiesColumns() As String = Me.Table.GetColumnsName(Column.Types.Identity)
            Return IdentitiesColumns.FirstOrDefault()
        End Function


        '------------------------------------------------------------------------------
        ' SQL BUILDER COMMAND

        ' Create the sql update command.
        ' This methos attempt to access to the belonging table so before we must
        ' check for empty values. The sql will build on this row schema and will 
        ' not a generic for all objects.
        Public Function UpdateCommand() As String
            ' Check the belonging as after we need to retrieve the information about the onwer
            ' table columns.
            Me.CheckBelongings()

            ' Once have the name associate the name to the value as we need to create the < where > 
            ' clause for the update sql command. 
            Dim PrimaryKeysColumns() As String = Me.Table.GetColumnsName(Column.Types.PrimaryKey)
            Dim PrimaryKeys As Dictionary(Of String, Object) = Me.GetColumnValues(PrimaryKeysColumns)

            ' Also we will create the values dictionary of the values to update.
            Dim UpdatableColumns() As String = Me.Table.GetColumnsName(Column.Types.Updatable)
            Dim Values As Dictionary(Of String, Object) = Me.GetColumnValues(UpdatableColumns)

            ' Create the sql for update taking the provider from the onwer table.
            Return New SqlBuilder(Me.Table.Query.GetProvider()) _
                .Table(Me.Table.Name) _
                .Update(Values) _
                .Where(Clauses.Empty.And(PrimaryKeys, Clauses.Comparer.Equal)) _
                .UpdateCommand
        End Function


        ' Create the sql delete command.
        ' This methos attempt to access to the belonging table so before we must
        ' check for empty values. The sql will build on this row schema and will 
        ' not a generic for all objects.
        Public Function DeleteCommand() As String
            ' Check the belonging as after we need to retrieve the information about the onwer
            ' table columns.
            Me.CheckBelongings()

            ' Once have the name associate the name to the value as we need to create the < where > 
            ' clause for the update sql command.
            Dim PrimaryKeysColumns() As String = Me.Table.GetColumnsName(Column.Types.PrimaryKey)
            Dim PrimaryKeys As Dictionary(Of String, Object) = Me.GetColumnValues(PrimaryKeysColumns)

            ' Create the sql for delete taking the provider from the onwer table.
            Return New SqlBuilder(Me.Table.Query.GetProvider()) _
                .Table(Me.Table.Name) _
                .Where(Clauses.Empty.And(PrimaryKeys, Clauses.Comparer.Equal)) _
                .DeleteCommand
        End Function


        ' Create the sql insert command.
        ' This methos attempt to access to the belonging table so before we must
        ' check for empty values. The sql will build on this row schema and will 
        ' not a generic for all objects.
        Public Function InsertCommand() As String
            ' Check the belonging as after we need to retrieve the information about the onwer
            ' table columns.
            Me.CheckBelongings()

            ' Once have the name associate the name to the value as we need but filtering for
            ' have just the writable columns.
            Dim WritableColumns() As String = Me.Table.GetColumnsName(Column.Types.Writable)
            Dim WritableKeys As Dictionary(Of String, Object) = Me.GetColumnValues(WritableColumns)

            ' Create the sql for delete taking the provider from the onwer table.
            Return New SqlBuilder(Me.Table.Query.GetProvider()) _
                .Table(Me.Table.Name) _
                .Insert(WritableKeys) _
                .InsertCommand
        End Function


        '------------------------------------------------------------------------------
        ' PUBLIC

        ' Try to update this object.
        ' This attemp only is some properties is changed and will be referenced
        ' to the owner since we must retrieve the row primary keys. 
        Public Function Update() As Boolean
            ' Update only if need
            If Not Me.HasChanges Then Return False

            ' Create the sql to update this object and execute it using the query object
            ' store inside the table. Using the query shared object we can using a common  
            ' translaction defined in the owner table.
            Update = Me.Table.Query.Exec(Me.UpdateCommand()) > 0

            ' Reset the changed trigger
            Me.AcceptChanges()
        End Function


        ' Try to delete this row from the owner table.
        ' After delete the object will not destroied so you may be insert again.
        Public Function Delete() As Boolean
            ' Create the sql to delete this object and execute it using the query object
            ' store inside the table. Using the query shared object we can using a common  
            ' translaction defined in the owner table.
            Return Me.Table.Query.Exec(Me.DeleteCommand()) > 0
        End Function


        ' Try to insert this row to the owner table.
        ' This methos not consider the changes status, so will attempt to execute it in 
        ' every cases. Note that this methos try to undesrtand if the object contain a 
        ' identity column and if found try to update this value after the insert command.
        Public Function Insert() As Boolean
            ' Try to undertand if need to retrieve the identity from the insert.
            Dim IdentityName As String = Me.GetIdentityColumnName()
            Dim NeedIdentity As Boolean = Utils.String.IsEmptyOrWhite(IdentityName)

            ' Create the sql to delete this object and execute it using the query object
            ' store inside the table. Using the query shared object we can using a common  
            ' translaction defined in the owner table.
            Dim Affected As Integer = Me.Table.Query.Exec(Me.InsertCommand(), NeedIdentity)

            ' Check if need to hold the identity value retrieved from the last insert.
            ' Else return if this methos success.
            If NeedIdentity Then
                Me.mMembers(IdentityName).Value = Affected
                Me.mMembers(IdentityName).AcceptChanges()
                Return True

            Else
                Return Affected > 0
            End If
        End Function


        ' Accept the changes.
        ' Just call the accept method for all cells, by column name, inside this row.
        Public Sub AcceptChanges()
            ' Cycle all cells
            For Each ColumnName As String In Me.mBelongingTable.GetColumnsName()
                ' Get the cell and call the accept changes but before check
                ' if the column in contained inside the members dictionary.
                If Me.HasColumn(ColumnName) Then
                    Me.mMembers(ColumnName).AcceptChanges()
                End If
            Next
        End Sub


        ' Reject changes.
        ' Hold in mind that the reject changes rool back to the original state and this is
        ' not a undo list function.
        Public Sub RejectChanges()
            ' Cycle all cells
            For Each ColumnName As String In Me.mBelongingTable.GetColumnsName()
                ' Get the cell and call the accept changes but before check
                ' if the column in contained inside the members dictionary.
                If Me.HasColumn(ColumnName) Then
                    Me.mMembers(ColumnName).RejectChanges()
                End If
            Next
        End Sub


        ' Check if this row object has a column (by name).
        Public Function HasColumn(ColumnName As String) As Boolean
            Return Me.mMembers.ContainsKey(ColumnName)
        End Function


        '------------------------------------------------------------------------------
        ' DYNAMIC MEMBERS MANAGEMENT

        ' If you try to get a value of a property that is not defined in the class, this method is called.
        Public Overrides Function TryGetMember(ByVal Binder As System.Dynamic.GetMemberBinder, ByRef Result As Object) As Boolean
            ' Converting the property name to lowercase so that property names become case-insensitive.
            Dim Name As String = Binder.Name.ToLower()

            ' If the property name is found in a dictionary, set the result parameter to the 
            ' property value and return true. Otherwise, return false.
            Return Me.mMembers.TryGetValue(Name, Result)
        End Function

        Public Overrides Function TrySetMember(ByVal Binder As System.Dynamic.SetMemberBinder, ByVal Value As Object) As Boolean
            ' Check the value
            If TypeOf Value IsNot Cell Then
                Throw New ArgumentException("Only DB.Table.Cell class is allowed.")
            End If

            ' Converting the property name to lowercase so that property names become case-insensitive.
            Me.mMembers(Binder.Name.ToLower()) = Value

            ' You can always add a value to a dictionary, so this method always returns true.
            Return True
        End Function


        '------------------------------------------------------------------------------
        ' PROPERTIES

        ' Default property.
        ' This property can be addressed to give the table cell directly calling
        ' the column name as an Array.
        Default Public Property Item(ColumnName As String) As Cell
            Get
                Return Me.mMembers(ColumnName)
            End Get
            Set(Value As Cell)
                ' Check if exists
                If Me.mMembers.ContainsKey(ColumnName) Then
                    ' Update
                    Me.mMembers(ColumnName) = Value

                Else
                    ' Insert new
                    Me.mMembers.Add(ColumnName, Value)
                End If
            End Set
        End Property


        ' Get the belonging table.
        ' A row must belong to a table for the proper functionality so this is 
        ' the owner table reference.
        Public ReadOnly Property Table As Table
            Get
                Return Me.mBelongingTable
            End Get
        End Property


        ' Check if the row has changes not confirmed.
        ' Simple cycle all cells, by column name, inside the row and check their status. 
        ' If will found one cell with changes return true.
        Public ReadOnly Property HasChanges As Boolean
            Get
                ' Check the belonging since this method cycle all the
                ' owner table columns and NOT the class members.
                Me.CheckBelongings()

                ' Cycle all memebr in row searching for changes
                For Each ColumnName As String In Me.Table.GetColumnsName()
                    If Me.HasColumn(ColumnName) AndAlso Me(ColumnName).HasChanges Then
                        Return True
                    End If
                Next

                ' If arrived here mean no changes found
                Return False
            End Get
        End Property

    End Class

End Namespace