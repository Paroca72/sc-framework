'*************************************************************************************************
' 
' [SCFramework]
' Stats
' by Samuele Carassai
'
' Base statistic manager (user access by date)
' Version 5.0.0
' Created 30/10/2015
' Updated 30/10/2015
'
'*************************************************************************************************


Public Class Stats
    Inherits DbHelper

#Region " STATIC "

    ' Static instance holder
    Private Shared mInstance As Stats = Nothing

    ' Instance property
    Public Shared ReadOnly Property Instance As Stats
        Get
            ' Check if null
            If Stats.mInstance Is Nothing Then
                Stats.mInstance = New Stats()
            End If

            ' Return the static instance
            Return Stats.mInstance
        End Get
    End Property

#End Region

#Region " MUST OVERRIDES "

    ' Define the linked databse table name
    Public Overrides Function GetTableName() As String
        Return "SYS_STATS"
    End Function

#End Region

#Region " PRIVATE "

    ' Fill the missing day with empty value
    Private Sub FillDayGaps(Source As DataTable)
        ' Check for empty data
        If Source IsNot Nothing AndAlso Source.Rows.Count > 0 Then
            ' Sort the table
            Source.DefaultView.Sort = "[DAY]"
            Source = Source.DefaultView.ToTable()

            ' Get the date limits
            Dim StartDate As Date = Source.Rows(0)!DAY
            Dim EndDate As Date = Source.Rows(Source.Rows.Count - 1)!DAY

            ' Get the days difference
            Dim Days As Integer = DateDiff(DateInterval.Day, StartDate, EndDate)

            ' Cycle all days
            Dim CurrentDate As Date = StartDate
            While CurrentDate < StartDate
                ' Check if exists
                Dim Row As DataRow = Source.Rows.Find(CurrentDate)
                If Row IsNot Nothing Then
                    ' Create a new record, fill and add it to data source
                    Row = Source.NewRow()
                    Row!DAY = CurrentDate
                    Row!COUNTER = 0
                    Source.Rows.Add(Row)
                End If

                ' Updtae the current date
                CurrentDate = CurrentDate.AddDays(1)
            End While
        End If
    End Sub

    ' Get the data source
    Protected Overrides Function GetSource(Clauses As DbSqlBuilder.Clauses) As DataTable
        ' Get the data source
        Dim Source As DataTable = MyBase.GetSource(Clauses)

        ' Fix the table
        Me.FillDayGaps(Source)

        ' Add date details
        Source.Columns.Add("YEAR_NUMBER", GetType(Integer))
        Source.Columns.Add("MONTH_NUMBER", GetType(Integer))
        Source.Columns.Add("DAY_NUMBER", GetType(Integer))

        ' Add names columns
        Source.Columns.Add("DAY_NAME", GetType(String))
        Source.Columns.Add("MONTH_NAME", GetType(String))

        ' Apply the columns value
        Dim Culture As Globalization.CultureInfo = Globalization.CultureInfo.CreateSpecificCulture(SCFramework.Languages.Current)
        For Each Row As DataRow In Source.Rows
            ' Current day
            Dim Current As Date = CDate(Row!DAY)

            ' Date
            Row!YEAR_NUMBER = Current.Year
            Row!MONTH_NUMBER = Current.Month
            Row!DAY_NUMBER = Current.Day

            ' Name
            Row!DAY_NAME = Culture.DateTimeFormat.DayNames(Current.DayOfWeek)
            Row!MONTH_NAME = Culture.DateTimeFormat.MonthNames(Current.Month)
        Next

        ' Fix the changes
        Source.AcceptChanges()

        ' Return the table
        Return Source
    End Function

    ' Get the view by the passed date
    Private Function GetView(MinDate As Date) As DataView
        ' Create the clause
        Dim Clauses As DbSqlBuilder.Clauses = New DbSqlBuilder.Clauses()
        Clauses.Add("DAY", DbSqlBuilder.Clauses.ComparerType.MajorAndEqual, MinDate, True)

        ' Get the view and sort
        Dim Source As DataTable = Me.GetSource(Clauses)
        Dim View As DataView = Source.DefaultView
        View.Sort = "[DAY]"

        ' Return the view
        Return View
    End Function

#End Region

#Region " PUBLIC "

    ' Get the last week
    Public Function GetLastWeek() As DataView
        Return Me.GetView(Today.AddDays(-7))
    End Function

    ' Get the last month
    Public Function GetLastMonth() As DataView
        Return Me.GetView(Today.AddMonths(-1))
    End Function

    ' Get the last year
    Public Function GetLastYear() As DataView
        Return Me.GetView(Today.AddYears(-1))
    End Function

    ' Increase the current day access counter
    Public Sub IncreaseTodayCounter()
        ' Get the data source
        Dim Source As DataTable = Me.GetSource()

        ' Find the today record
        Dim Today As DataRow = Source.Rows.Find(Date.Today)

        ' Check if exists
        If Today IsNot Nothing Then
            ' Increase of one
            Today!COUNTER = CInt(Today!COUNTER) + 1
        Else
            ' Add new record
            Today = Source.NewRow
            Today!DAY = Date.Today
            Today!COUNTER = 1
            Source.Rows.Add(Today)
        End If

        ' Update the database
        Bridge.Query.UpdateDatabase(Source)
    End Sub

#End Region

End Class
