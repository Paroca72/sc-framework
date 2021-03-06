﻿'*************************************************************************************************
' 
' [SCFramework]
' Clauses  
' by Samuele Carassai
'
' This classes is specialized to create a database clauses.
' Can be used for create filters for datatable or for example to where condition.
' Have two way to produce the clauses: for SQL (database) or for filter (datatable).
' 
' Sql builder manager
' Version 5.0.0
' Updated 27/11/2016
'
'*************************************************************************************************


' Define the name space
Namespace DB

    ' Class definition
    Public Class Clauses

        ' Enums of the comparator types
        Public Enum Comparer As Integer
            Equal
            Minor
            MinorOrEqual
            Major
            MajorOrEqual
            Different
            [Like]
            LikeStart
            LikeEnd
            [In]
            NotIn
        End Enum

        ' Single clause structure for internal use
        Private Class SingleClause

            Public Clauses As Clauses = Nothing
            Public Column As String = Nothing
            Public Comparer As Comparer = Comparer.Equal
            Public Value As Object = Nothing
            Public Sql As String = Nothing
            Public GroupAsAnd As Boolean = True

        End Class

        ' Holder
        Private mClauses As List(Of SingleClause) = New List(Of SingleClause)


#Region " CONSTRUCTOR "

        Sub New()
        End Sub

        Sub New(Sql As String)
            Me.Add(Sql, True)
        End Sub

        Sub New(Column As String, Comparer As Comparer, Value As Object)
            Me.Add(Column, Comparer, Value, True)
        End Sub

        Sub New(Clauses As Clauses)
            Me.Add(Clauses, True)
        End Sub

#End Region

#Region " STATIC "

        ' Create an empty clauses
        Public Shared Function Empty() As Clauses
            Return New Clauses()
        End Function


        ' Create a false clauses
        Public Shared Function AlwaysFalse() As Clauses
            Return New Clauses("1 <> 1")
        End Function


        ' Create a true clauses
        Public Shared Function AlwaysTrue() As Clauses
            ' Return the new object
            Return New Clauses("1 = 1")
        End Function

#End Region

#Region " PRIVATE "

        ' Add a new clauses in many ways only for internal use.
        ' To understand the always present GroupAsAnd, add the new clauses connected with the previous token 
        ' Using an "AND" Else will used an "OR". 
        ' The creation of the clauses will be made only When Call the builder method.
        Private Function Add(Column As String, Comparer As Comparer, Value As Object, GroupAsAnd As Boolean) As Clauses
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

        Private Function Add(Sql As String, GroupAsAnd As Boolean) As Clauses
            ' Create the single clause
            Dim Clause As SingleClause = New SingleClause()
            Clause.Sql = Sql
            Clause.GroupAsAnd = GroupAsAnd

            ' Add to clauses list
            Me.mClauses.Add(Clause)

            ' Return the class reference
            Return Me
        End Function

        Private Function Add(Clauses As IDictionary(Of String, Object), Comparer As Comparer, GroupAsAnd As Boolean) As Clauses
            ' Cycle all clause of list
            For Each Column As String In Clauses.Keys
                ' Add
                Me.Add(Column, Comparer, Clauses(Column), GroupAsAnd)
            Next

            ' Return the class reference
            Return Me
        End Function

        Private Function Add(Clauses As Clauses, GroupAsAnd As Boolean) As Clauses
            ' Create the single clause
            Dim Clause As SingleClause = New SingleClause()
            Clause.Clauses = Clauses
            Clause.GroupAsAnd = GroupAsAnd

            ' Add to clauses list
            Me.mClauses.Add(Clause)

            ' Return the class reference
            Return Me
        End Function


        ' Build the single clause
        Private Function Builder(ForFilter As Boolean) As String
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
                    If Clause.Comparer = Comparer.Like Then
                        Clause.Value = String.Format("%{0}%", Clause.Value.ToString())
                    ElseIf Clause.Comparer = Comparer.LikeStart Then
                        Clause.Value = String.Format("%{0}", Clause.Value.ToString())
                    ElseIf Clause.Comparer = Comparer.LikeEnd Then
                        Clause.Value = String.Format("{0}%", Clause.Value.ToString())
                    End If

                    ' Convert the comparer to its string rappresentation
                    Dim ComparerToString As String = "="
                    Select Case Clause.Comparer
                        Case Comparer.Different : ComparerToString = "<>"
                        Case Comparer.Major : ComparerToString = ">"
                        Case Comparer.MajorOrEqual : ComparerToString = ">="
                        Case Comparer.Minor : ComparerToString = "<"
                        Case Comparer.MinorOrEqual : ComparerToString = "<="
                        Case Comparer.In : ComparerToString = "IN"
                        Case Comparer.NotIn : ComparerToString = "NOT IN"
                    End Select

                    ' Fix the comparer in the case of null values
                    If Clause.Value Is Nothing OrElse IsDBNull(Clause.Value) Then
                        If Clause.Comparer = Comparer.Equal Then ComparerToString = "IS"
                        If Clause.Comparer = Comparer.Different Then ComparerToString = "IS NOT"
                    End If

                    ' Build the group clausole
                    If Not SCFramework.Utils.String.IsEmptyOrWhite(Filter) Then
                        ' Check for AND or OR
                        Filter &= IIf(Clause.GroupAsAnd, " AND ", " OR ")
                    End If

                    ' If requested create sql for filter force the provider to OldDb.
                    Dim Provider As String = IIf(ForFilter, "System.Data.OleDb", Bridge.Query.GetProvider())
                    Dim SqlBuilder As SqlBuilder = New SqlBuilder(Provider)
                    SqlBuilder.StringEmptyIsNULL = False

                    ' Append the new clause by the case
                    If Clause.Comparer = Comparer.In Or Clause.Comparer = Comparer.NotIn Then
                        Filter &= String.Format("{0} {1} ({2})",
                                            SqlBuilder.Quote(Clause.Column),
                                            ComparerToString,
                                            Clause.Value)
                    Else
                        Filter &= String.Format("{0} {1} {2}",
                                            SqlBuilder.Quote(Clause.Column),
                                            ComparerToString,
                                            SqlBuilder.Variant(Clause.Value))
                    End If
                End If
            Next

            ' Return 
            Return Filter
        End Function

#End Region

#Region " PUBLIC "

        ' Add a clauses in AND 
        Public Function [And](Column As String, Comparer As Comparer, Value As Object)
            Return Me.Add(Column, Comparer, Value, True)
        End Function

        Public Function [And](Sql As String)
            Return Me.Add(Sql, True)
        End Function

        Public Function [And](Clauses As Clauses) As Clauses
            Return Me.Add(Clauses, True)
        End Function

        Public Function [And](Clauses As Dictionary(Of String, Object), Comparer As Comparer) As Clauses
            Return Me.Add(Clauses, Comparer, True)
        End Function


        ' Add a clauses in OR 
        Public Function [Or](Column As String, Comparer As Comparer, Value As Object)
            Return Me.Add(Column, Comparer, Value, False)
        End Function

        Public Function [Or](Sql As String)
            Return Me.Add(Sql, False)
        End Function

        Public Function [Or](Clauses As Clauses) As Clauses
            Return Me.Add(Clauses, False)
        End Function

        Public Function [Or](Clauses As Dictionary(Of String, Object), Comparer As Comparer) As Clauses
            Return Me.Add(Clauses, Comparer, False)
        End Function


        ' Check if this clauses equal to another clauses
        Public Function IsEqual(Clauses As Clauses) As Boolean
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

End Namespace
