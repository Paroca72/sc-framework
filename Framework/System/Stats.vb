'*************************************************************************************************
' 
' [SCFramework]
' Stats
' by Samuele Carassai
'
' Base statistic manager.
' This classes inherits from DataSourceHelper but is ALWAYS memory managed. Some base methods are 
' shadowed or overridden to avoid to change the management way.
' Offer a very basic function and was created only for give the basis for a future improvement.
' 
' Version 5.0.0
' Created 30/10/2015
' Updated 20/10/2016
'
'*************************************************************************************************


Public Class Stats
    Inherits DataSourceHelper

#Region " CONSTRUCTOR "

    Public Sub New()
        ' Base methods
        MyBase.New()

        ' Define the order fields
        Me.OrderColumns.Clear()
        Me.OrderColumns.Add("DAY")

        ' Load the table in memory
        MyBase.GetSource(Nothing, True)
    End Sub

#End Region

#Region " MUST OVERRIDES "

    ' Define the linked databse table name
    Public Overrides Function Name() As String
        Return "SYS_STATS"
    End Function

#End Region

#Region " PRIVATE "

    ' Fill the missing day with empty value
    Private Sub FillDayGaps(Source As DataTable)
        ' Check for empty data
        If Source IsNot Nothing AndAlso Source.Rows.Count > 0 Then
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

    ' Get source filtered by date
    Private Function GetFilteredSource([Date] As Date) As DataTable
        ' Create the clauses and filter
        Return Me.GetSource(New SCFramework.DB.Clauses("DAY", SCFramework.DB.Clauses.Comparer.MinorOrEqual, [Date]))
    End Function

#End Region

#Region " PUBLIC "

    ' Get the data source
    Public Shadows Function GetSource(Optional Clauses As DB.Clauses = Nothing) As DataTable
        ' Get the data source
        Dim Source As DataTable = MyBase.GetSource(Clauses, False)

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
        ' TODO: create a static languages manager
        Dim LanguageManager As Languages = New Languages()
        Dim Culture As Globalization.CultureInfo = Globalization.CultureInfo.CreateSpecificCulture(LanguageManager.Current)

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

    ' Get the last week
    Public Function GetLastWeek() As DataTable
        Return Me.GetFilteredSource(Today.AddDays(-7))
    End Function

    ' Get the last month
    Public Function GetLastMonth() As DataTable
        Return Me.GetFilteredSource(Today.AddMonths(-1))
    End Function

    ' Get the last year
    Public Function GetLastYear() As DataTable
        Return Me.GetFilteredSource(Today.AddYears(-1))
    End Function

    ' Increase the current day access counter
    Public Sub IncreaseCounter(Optional Day As Date = Nothing)
        ' Check for empty date
        If Day = Date.MinValue Then Day = Date.Today

        ' Create the clauses for a single day
        Dim Clauses As DB.Clauses = DB.Clauses.Empty _
            .And("YEAR_NUMBER", DB.Clauses.Comparer.Equal, Day.Year) _
            .and("MONTH_NUMBER", DB.Clauses.Comparer.Equal, Day.Month) _
            .and("DAY_NUMBER", DB.Clauses.Comparer.Equal, Day.Day)

        ' Get the data source and check if exists
        Dim Today As DataRow = Me.GetSource(Clauses).AsEnumerable().FirstOrDefault
        If Today IsNot Nothing Then
            ' If exists increase of one
            Today!COUNTER = CInt(Today!COUNTER) + 1

        Else
            ' If not exists create the values list
            Dim Values As Dictionary(Of String, Object) = New Dictionary(Of String, Object)
            Values.Add("DAY", Day)
            Values.Add("COUNTER", 1)

            ' Insert the new record
            Me.Insert(Values)
        End If

        ' Is memory managed so I must to update the database manually
        Me.AcceptChanges()
    End Sub

    ' Force to reload data source using the last clauses at the next source access
    Public Overrides Sub CleanDataSouce()
        MyBase.GetSource(Nothing, True)
    End Sub

#End Region

End Class
