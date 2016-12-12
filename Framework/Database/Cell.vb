
' Define the name space
Namespace DB

    ' TODO: Translation
    Public MustInherit Class Cell

        '------------------------------------------------------------------------------
        ' PRIVATES

        ' Holders
        Private mCurrentValue As Object = Nothing
        Private mPreviousValue As Object = Nothing
        Private mOriginalValue As Object = Nothing
        Private mTranslation As String = Nothing

        Private mHasChanges As Boolean = False
        Private mFirstTimeSetting As Boolean = True

        Protected mBelongingColumn As Column = Nothing
        Protected mBelongingRow As Row = Nothing


        ' Check the belongings of this cell for empty values.
        ' In this case some methods of this class will not work proper and throw an exception.
        Private Sub CheckBelongings()
            ' Check the row
            If Me.Row Is Nothing Then
                Throw New Exception("Cell's row belonging must be defined before use this class.")
            End If

            ' Check the table
            If Me.Table Is Nothing Then
                Throw New Exception("Cell's table belonging must be defined before use this class.")
            End If

            ' Check the column
            If Me.Column Is Nothing Then
                Throw New Exception("Cell's column belonging must be defined before use this class.")
            End If
        End Sub


        ' Given as list of column retrieve a list of pair values.
        Private Function GetColumnValues(ParamArray Columns() As String) As Dictionary(Of String, Object)
            ' Init the dictionary
            GetColumnValues = New Dictionary(Of String, Object)

            ' Cycle all the columns name 
            For Each ColumnName As String In Columns
                ' Try to add the new pair
                If Me.mBelongingRow.HasColumn(ColumnName) Then
                    GetColumnValues.Add(ColumnName, Me.mBelongingRow(ColumnName).Value)
                End If
            Next
        End Function


        ' Try to get the translation considering the actual value.
        Private Function GetTranslation(Key As String) As String
            ' Try with text
            If Me.mBelongingColumn.IsMultilanguageText Then
                Return Bridge.Translations.GetValue(Key, Bridge.Languages.Current)
            End If

            ' Try with image
            If Me.mBelongingColumn.IsMultilanguageFile Then
                Return Bridge.Files.GetValue(Key, Bridge.Languages.Current)
            End If

            ' Else return nothing 
            Return Nothing
        End Function


        '------------------------------------------------------------------------------
        ' PROPERTIES

        ' Getter/setter for the current cell value.
        ' Changing this value will auto-save in the previous value field and mark the
        ' cell as changed.
        Public Property Value As Object
            Get
                ' This dynamic convertion is just a check for the corrected 
                ' Value/ Type column defined.
                Me.CheckBelongings()
                Return CTypeDynamic(Me.mCurrentValue, Me.mBelongingColumn.Type)

            End Get
            Set(Value As Object)
                ' Check if need to be changed.
                ' Noted that nothing cannot be compared with another object so must
                ' be write a more complex if instead a simple compare.
                If Me.mFirstTimeSetting Or Not Value.Equals(Me.mCurrentValue) Then
                    ' This dynamic convertion is just a check for the corrected 
                    ' Value/ Type column defined.
                    Me.CheckBelongings()
                    Value = CTypeDynamic(Value, Me.mBelongingColumn.Type)

                    ' Save the current status and set the trigger as changed.
                    Me.mPreviousValue = Me.mCurrentValue
                    Me.mCurrentValue = Value

                    ' Try to get the translation since the value changed also the 
                    ' Translation should be changed. But only if not the first time 
                    ' setting.
                    If Not Me.mFirstTimeSetting Then
                        Me.mTranslation = Me.GetTranslation(Value)
                    End If

                    ' Save the original value just one time
                    If Me.mOriginalValue Is Nothing Then
                        Me.mOriginalValue = Value
                    End If

                    ' Fix the triggers
                    Me.mFirstTimeSetting = False
                    Me.mHasChanges = True
                End If
            End Set
        End Property


        ' Return the last value after update the current value.
        ' Can be useful when you need to know the last value after updating for example to 
        ' delete a file updated.
        Public ReadOnly Property PreviousValue As Object
            Get
                ' In this case don't need to a dynamic cast since already applied
                ' to the current value and this value is a copy of that.
                Return Me.mCurrentValue
            End Get
        End Property


        ' Get/set the translation.
        Public Property Translation As String
            Get
                Return Me.mTranslation
            End Get
            Set(Value As String)
                ' Check if changed
                If Not Value.Equals(Me.mTranslation) Then
                    ' Holde the value and fix the trigger as changed
                    Me.mTranslation = Value
                    Me.mHasChanges = True
                End If
            End Set
        End Property


        ' Return the table onwer.
        Public ReadOnly Property Table As Table
            Get
                ' Check for empty values
                If Me.mBelongingRow IsNot Nothing Then
                    Return Me.mBelongingRow.Table

                Else
                    Return Nothing
                End If
            End Get
        End Property


        ' Return the row onwer.
        Public ReadOnly Property Row As Row
            Get
                Return Me.mBelongingRow
            End Get
        End Property


        ' Return the column onwer.
        Public ReadOnly Property Column As Column
            Get
                Return Me.mBelongingColumn
            End Get
        End Property


        ' Get if the object has changes.
        Public ReadOnly Property HasChanges() As Boolean
            Get
                Return Me.mHasChanges
            End Get
        End Property


        '------------------------------------------------------------------------------
        ' PUBLIC

        ' Create the sql update command.
        ' This methos attempt to access to the belonging table so before we must
        ' check for empty values. The sql will build on this row schema and will 
        ' not a generic for all objects.
        Public Function UpdateCommand() As String
            ' Recover the privary key column name list from the owner table.
            Me.CheckBelongings()
            Dim PrimaryKeysColumns() As String = Me.Table.GetColumnsName(Column.Types.PrimaryKey)

            ' Once have the name associate the name to the value as we need to create the < where > 
            ' clause for the update sql command. Also we will create the values diction to update.
            Dim PrimaryKeys As Dictionary(Of String, Object) = Me.GetColumnValues(PrimaryKeysColumns)
            Dim Values As Dictionary(Of String, Object) = Me.GetColumnValues(Me.mBelongingColumn.Name)

            ' Create the sql for update command taking the provider from the onwer table.
            Return New SqlBuilder(Me.Table.Query.GetProvider()) _
                .Table(Me.Table.Name) _
                .Update(Values) _
                .Where(Clauses.Empty.And(PrimaryKeys, Clauses.Comparer.Equal)) _
                .UpdateCommand
        End Function


        ' Try to update this object.
        ' This attemp only is some properties is changed and will be referenced
        ' to the owner since we must retrieve the row primary keys. 
        Public Function Update() As Boolean
            ' Update only if need
            If Not Me.mHasChanges Then Return False

            ' Create the sql to update this object and execute it using the query object
            ' store inside the table. Using the query shared object we can using a common  
            ' translaction defined in the owner table.
            Update = Me.Table.Query.Exec(Me.UpdateCommand()) > 0

            ' Reset the changed trigger
            Me.AcceptChanges()
        End Function


        ' Accept the changes.
        ' Just set this state as original state and reset the trigger.
        Public Sub AcceptChanges()
            Me.mOriginalValue = Me.mCurrentValue
            Me.mHasChanges = False
        End Sub


        ' Reject changes.
        ' Hold in mind that the reject changes rool back to the original state and this is
        ' not a undo list function. What happen is currentValue equal To originalValue and 
        ' previousValue equal Nothing.
        Public Sub RejectChanges()
            ' Only if have some changes 
            If Me.mHasChanges Then
                ' Reset to the original state
                Me.mCurrentValue = Me.mOriginalValue
                Me.mPreviousValue = Nothing
                Me.mHasChanges = False
            End If
        End Sub

    End Class

End Namespace
