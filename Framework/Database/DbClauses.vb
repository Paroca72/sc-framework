'*************************************************************************************************
' 
' [SCFramework]
' DbClauses  
' by Samuele Carassai
'
' Sql builder manager
' Version 5.0.0
' Created 10/10/2016
' Updated 10/10/2016
'
'*************************************************************************************************


Public Class DbClauses

    ' Enums
    Public Enum ComparerType As Integer
        Equal
        Minor
        MinorAndEqual
        Major
        MajorAndEqual
        Different
        [Like]
        LikeStart
        LikeEnd
    End Enum

    ' Single clause
    Private Class SingleClause

        Public Clauses As SCFramework.DbClauses = Nothing
        Public Column As String = Nothing
        Public Comparer As ComparerType = ComparerType.Equal
        Public Value As Object = Nothing
        Public GroupAsAnd As Boolean = True

    End Class

    ' Holder
    Private mClauses As List(Of SingleClause) = New List(Of SingleClause)


    ' Contructor
    Public Sub New()
    End Sub

    Public Sub New(Column As String, Value As Object)
        Me.Add(Column, Value)
    End Sub

    Public Sub New(Clauses As IDictionary(Of String, Object))
        Me.Add(Clauses)
    End Sub

    ' Add a clause
    Public Sub Add(Column As String, Comparer As ComparerType, Value As Object, GroupAsAnd As Boolean)
        ' Create the single clause
        Dim Clause As SingleClause = New SingleClause()
        Clause.Column = Column
        Clause.Comparer = Comparer
        Clause.Value = Value
        Clause.GroupAsAnd = GroupAsAnd

        ' Add to clauses list
        Me.mClauses.Add(Clause)
    End Sub

    ' Add one clause using the default parameters
    Public Sub Add(Column As String, Value As Object)
        Me.Add(Column, ComparerType.Equal, Value, True)
    End Sub

    ' Add a list of clauses defined as key and value pair
    Public Sub Add(Clauses As IDictionary(Of String, Object))
        ' Cycle all clause of list
        For Each Column As String In Clauses.Keys
            ' Add
            Me.Add(Column, ComparerType.Equal, Clauses(Column), True)
        Next
    End Sub

    ' Add a list of clauses
    Public Sub Add(Clauses() As SCFramework.DbClauses, GroupAsAnd As Boolean)
        ' Cycle all clauses
        For Each CurrentClauses As SCFramework.DbClauses In Clauses
            ' Create the single clause
            Dim Clause As SingleClause = New SingleClause()
            Clause.Clauses = CurrentClauses
            Clause.GroupAsAnd = GroupAsAnd

            ' Add to clauses list
            Me.mClauses.Add(Clause)
        Next
    End Sub

    ' Build the single clause
    Public Function Builder(ForFilter As Boolean) As String
        ' Holder
        Dim Filter As String = String.Empty

        ' Cycle all clauses
        For Each Clause As SingleClause In Me.mClauses
            ' Check if the clause is an object 
            If Clause.Clauses IsNot Nothing Then
                ' Add the single clauses
                If Not SCFramework.Utils.String.IsEmptyOrWhite(Filter) Then Filter &= IIf(Clause.GroupAsAnd, " AND ", " OR ")
                Filter &= "(" & Clause.Clauses.Builder(ForFilter) & ")"

            Else
                ' Fix the particular case
                If Clause.Comparer = ComparerType.Like Then
                    Clause.Value = String.Format("%{0}%", Clause.Value.ToString())
                ElseIf Clause.Comparer = ComparerType.LikeStart Then
                    Clause.Value = String.Format("%{0}", Clause.Value.ToString())
                ElseIf Clause.Comparer = ComparerType.LikeEnd Then
                    Clause.Value = String.Format("{0}%", Clause.Value.ToString())
                End If

                ' Convert the comparer to its string rappresentation
                Dim ComparerToString As String = "="
                Select Case Clause.Comparer
                    Case ComparerType.Different : ComparerToString = "<>"
                    Case ComparerType.Major : ComparerToString = ">"
                    Case ComparerType.MajorAndEqual : ComparerToString = ">="
                    Case ComparerType.Minor : ComparerToString = "<"
                    Case ComparerType.MinorAndEqual : ComparerToString = "<="
                End Select

                ' Fix the comparer in the case of null values
                If Clause.Value Is Nothing OrElse IsDBNull(Clause.Value) Then
                    If Clause.Comparer = ComparerType.Equal Then ComparerToString = "IS"
                    If Clause.Comparer = ComparerType.Different Then ComparerToString = "IS NOT"
                End If

                ' Build the group clausole
                If Not SCFramework.Utils.String.IsEmptyOrWhite(Filter) Then
                    ' Check for AND or OR
                    Filter &= IIf(Clause.GroupAsAnd, " AND ", " OR ")
                End If

                ' Append the new clause. If must create sql for filter force the provider to OldDb.
                Dim Provider As DbQuery.ProvidersList = IIf(ForFilter, DbQuery.ProvidersList.OleDb, DbQuery.ProvidersList.Undefined)
                Filter &= String.Format("{0} {1} {2}",
                                        DbSqlBuilder.Quote(Clause.Column), ComparerToString,
                                        DbSqlBuilder.Variant(Clause.Value, Provider))
            End If
        Next

        ' Return 
        Return Filter
    End Function

    ' Build the where clauses for sql
    Public ReadOnly Property ForSql As Boolean
        Get
            Return Me.Builder(False)
        End Get
    End Property

    ' Build the where clauses for data filter
    Public ReadOnly Property ForFilter As Boolean
        Get
            Return Me.Builder(True)
        End Get
    End Property

    ' True if is empty
    Public ReadOnly Property IsEmpty As Boolean
        Get
            Return Me.mClauses.Count = 0
        End Get
    End Property

End Class
