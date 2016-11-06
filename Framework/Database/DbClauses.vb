'*************************************************************************************************
' 
' [SCFramework]
' DbClauses  
' by Samuele Carassai
'
' Sql builder manager
' Version 5.0.0
' Created 10/10/2016
' Updated 19/10/2016
'
'*************************************************************************************************


Public Class DbClauses

    ' Enums
    Public Enum ComparerType As Integer
        Equal
        Minor
        MinorOrEqual
        Major
        MajorOrEqual
        Different
        [Like]
        LikeStart
        LikeEnd
    End Enum

    ' Single clause
    Private Class SingleClause

        Public Clauses As DbClauses = Nothing
        Public Column As String = Nothing
        Public Comparer As ComparerType = ComparerType.Equal
        Public Value As Object = Nothing
        Public Sql As String = Nothing
        Public GroupAsAnd As Boolean = True

    End Class

    ' Holder
    Private mClauses As List(Of SingleClause) = New List(Of SingleClause)


#Region " STATIC "

    ' Create an empty clauses
    Public Shared Function Empty() As DbClauses
        Return New SCFramework.DbClauses()
    End Function

    ' Create a false clauses
    Public Shared Function AlwaysFalse() As DbClauses
        Return DbClauses.Empty.Add("1 <> 1")
    End Function

    ' Create a true clauses
    Public Shared Function AlwaysTrue() As DbClauses
        ' Return the new object
        Return DbClauses.Empty.Add("1 = 1")
    End Function

    ' Create a clasuses from a pair values.
    ' The comparison will be as equal.
    Public Shared Function FromPair(Column As String, Value As Object) As DbClauses
        Return DbClauses.Empty.Add(Column, Value)
    End Function

    ' Create a clasuses from range of clauses.
    Public Shared Function FromRange(Clauses As IDictionary(Of String, Object)) As DbClauses
        Return DbClauses.Empty.Add(Clauses)
    End Function

    ' Create a clasuses from another clauses.
    Public Shared Function FromClauses(Clauses As DbClauses) As DbClauses
        Return DbClauses.Empty.Add(Clauses)
    End Function

#End Region

#Region " PUBLIC "

    ' Add a clause
    Public Function Add(Column As String, Comparer As ComparerType, Value As Object,
                        Optional GroupAsAnd As Boolean = True) As DbClauses
        ' Create the single clause
        Dim Clause As SingleClause = New SingleClause()
        Clause.Column = Column
        Clause.Comparer = Comparer
        Clause.Value = Value
        Clause.GroupAsAnd = GroupAsAnd

        ' Add to clauses list
        Me.mClauses.Add(Clause)

        ' Return the class reference
        Return Me
    End Function

    ' Add a sql
    Public Function Add(Sql As String,
                        Optional GroupAsAnd As Boolean = True) As DbClauses
        ' Create the single clause
        Dim Clause As SingleClause = New SingleClause()
        Clause.Sql = Sql
        Clause.GroupAsAnd = GroupAsAnd

        ' Add to clauses list
        Me.mClauses.Add(Clause)

        ' Return the class reference
        Return Me
    End Function

    ' Add a list of clauses defined as key and value pair
    Public Function Add(Clauses As IDictionary(Of String, Object)) As DbClauses
        ' Cycle all clause of list
        For Each Column As String In Clauses.Keys
            ' Add
            Me.Add(Column, ComparerType.Equal, Clauses(Column), True)
        Next

        ' Return the class reference
        Return Me
    End Function

    ' Add a list of clauses
    Public Function Add(Clauses() As DbClauses,
                        Optional GroupAsAnd As Boolean = True) As DbClauses
        ' Cycle all clauses
        For Each CurrentClauses As DbClauses In Clauses
            ' Create the single clause
            Dim Clause As SingleClause = New SingleClause()
            Clause.Clauses = CurrentClauses
            Clause.GroupAsAnd = GroupAsAnd

            ' Add to clauses list
            Me.mClauses.Add(Clause)
        Next

        ' Return the class reference
        Return Me
    End Function

    ' Build the single clause
    Public Function Builder(ForFilter As Boolean) As String
        ' Holder
        Dim Filter As String = String.Empty

        ' Cycle all clauses
        For Each Clause As SingleClause In Me.mClauses
            ' Check all cases
            If Clause.Sql IsNot Nothing Or Clause.Clauses IsNot Nothing Then
                ' Join with the old filter
                If Not SCFramework.Utils.String.IsEmptyOrWhite(Filter) Then Filter &= IIf(Clause.GroupAsAnd, " AND ", " OR ")

                ' Check the case
                If Clause.Sql IsNot Nothing Then Filter &= "(" & Clause.Sql & ")"
                If Clause.Clauses IsNot Nothing Then Filter &= "(" & Clause.Clauses.Builder(ForFilter) & ")"

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
                    Case ComparerType.MajorOrEqual : ComparerToString = ">="
                    Case ComparerType.Minor : ComparerToString = "<"
                    Case ComparerType.MinorOrEqual : ComparerToString = "<="
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

                ' If requested create sql for filter force the provider to OldDb.
                Dim Provider As DbQuery.ProvidersList = IIf(ForFilter, DbQuery.ProvidersList.OleDb, DbQuery.ProvidersList.Undefined)
                Dim SqlBuilder As DbSqlBuilder = New DbSqlBuilder(Provider)

                ' Append the new clause. 
                Filter &= String.Format("{0} {1} {2}",
                                        DbSqlBuilder.Quote(Clause.Column),
                                        ComparerToString,
                                        SqlBuilder.Variant(Clause.Value))
            End If
        Next

        ' Return 
        Return Filter
    End Function

    ' Check if equal to another clauses
    Public Function IsEqual(Clauses As DbClauses) As Boolean
        Return Clauses IsNot Nothing AndAlso Me.ForSql.Equals(Clauses.ForSql)
    End Function

#End Region

#Region " PROPERTIES "

    ' Build the where clauses for sql
    Public ReadOnly Property ForSql As String
        Get
            Return Me.Builder(False)
        End Get
    End Property

    ' Build the where clauses for data filter
    Public ReadOnly Property ForFilter As String
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

#End Region

End Class
