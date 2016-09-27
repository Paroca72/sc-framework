'*************************************************************************************************
' 
' [SCFramework]
' Mailer
' di Samuele Carassai
'
' Classi di gestione e spedizione Mail
' Versione 4.0.4
'
'
'*************************************************************************************************


Public Class Mail

    Private _From As String = Nothing
    Private _To As String = Nothing
    Private _Subject As String = Nothing
    Private _Body As String = Nothing
    Private _HTML As String = Nothing
    Private _Attachments As ArrayList = Nothing
    Private _Schedule As Date = Date.MinValue
    Private _Format As Hashtable = Nothing


    Public Sub New()
        Me._Attachments = New ArrayList()
    End Sub

    Public Property From() As String
        Set(value As String)
            Me._From = value
        End Set
        Get
            If String.IsNullOrEmpty(Me._From) Then
                Return Configuration.Instance.GenericMail
            Else
                Return Me._From
            End If
        End Get
    End Property

    Public Property [To]() As String
        Set(value As String)
            Me._To = value
        End Set
        Get
            Return Me._To
        End Get
    End Property

    Public Property Subject() As String
        Set(value As String)
            Me._Subject = value
        End Set
        Get
            Return Me._Subject
        End Get
    End Property

    Public Property Body() As String
        Set(value As String)
            Me._Body = value
        End Set
        Get
            Return Me._Body
        End Get
    End Property

    Public Property HTML() As String
        Set(value As String)
            Me._HTML = value
        End Set
        Get
            Return Me._HTML
        End Get
    End Property

    Public Property Attachments() As ArrayList
        Set(value As ArrayList)
            Me._Attachments = value
        End Set
        Get
            Return Me._Attachments
        End Get
    End Property

    Public Property Schedule() As Date
        Set(value As Date)
            Me._Schedule = value
        End Set
        Get
            Return Me._Schedule
        End Get
    End Property

    Public Property Format() As Hashtable
        Set(value As Hashtable)
            Me._Format = value
        End Set
        Get
            Return Me._Format
        End Get
    End Property

End Class

Public Class Mails

#Region " COMMON "

    Private Shared Function GetMailsStructure() As DataTable
        Dim SQL As String = "SELECT * " & _
                            "FROM [SYS_MAILS] " & _
                            "WHERE 1 <> 1"
        Dim Table As DataTable = Bridge.Query.Table(SQL, "SYS_MAILS")
        SCFramework.Utils.SetAutoIncrementColumns(Table, "ID_MAIL")

        Return Table
    End Function

    Private Shared Function SerializeHashTable(Values As Hashtable) As String
        If Values IsNot Nothing AndAlso Values.Count > 0 Then
            Dim Serializer As Script.Serialization.JavaScriptSerializer = New Script.Serialization.JavaScriptSerializer()
            Return Serializer.Serialize(Values)
        Else
            Return String.Empty
        End If
    End Function

    Private Shared Function DeserializeHashTable(Value As String) As Hashtable
        If Not String.IsNullOrEmpty(Value) Then
            Dim Serializer As Script.Serialization.JavaScriptSerializer = New Script.Serialization.JavaScriptSerializer()
            Return Serializer.Deserialize(Of Hashtable)(Value)
        Else
            Return Nothing
        End If
    End Function

#End Region

#Region " SEND "

    Private Shared Function FormatText(Value As String, List As Hashtable) As String
        If Not String.IsNullOrEmpty(Value) Then
            For Each Key As String In List.Keys
                Value = Value.Replace(Key, List(Key))
            Next
        End If
        Return Value
    End Function

    Public Shared Function DirectSend(Mail As SCFramework.Mail) As String
        Try
            Dim Message As MailMessage = New MailMessage(Mail.From, Mail.[To], Mail.Subject, Mail.Body)

            ' Format
            Dim Body As String = Mail.Body
            Dim HTML As String = Mail.HTML

            If Mail.Format IsNot Nothing Then
                Body = Mails.FormatText(Body, Mail.Format)
                HTML = Mails.FormatText(HTML, Mail.Format)
            End If

            ' Body
            If Not String.IsNullOrEmpty(Mail.HTML) And Not String.IsNullOrEmpty(Mail.Body) Then
                Dim AVText As AlternateView = AlternateView.CreateAlternateViewFromString(Mail.Body, Nothing, "text/plain")
                Dim AVHTML As AlternateView = AlternateView.CreateAlternateViewFromString(Mail.HTML, Nothing, "text/html")

                Message.AlternateViews.Add(AVText)
                Message.AlternateViews.Add(AVHTML)
            Else
                If Not String.IsNullOrEmpty(Mail.Body) Then
                    Message.Body = Body
                    Message.IsBodyHtml = False
                Else
                    Message.Body = HTML
                    Message.IsBodyHtml = True
                End If
            End If

            ' Attachments
            If Mail.Attachments.Count > 0 Then
                For Each FileName As String In Mail.Attachments
                    If FileName.Trim <> String.Empty AndAlso IO.File.Exists(FileName) Then
                        Message.Attachments.Add(New Attachment(FileName))
                    End If
                Next
            End If

            ' Credential
            Dim Credential As NetworkCredential = CredentialCache.DefaultNetworkCredentials
            Dim SMTP As String = Configuration.Instance.SMTP

            If String.IsNullOrEmpty(SMTP) Then SMTP = "127.0.0.1"
            If SMTP.IndexOf("@") <> -1 Then
                Dim Params As String = SMTP.Split("|")(1)
                Dim Login As String = Params.Split(":")(0)
                Dim Psw As String = Params.Split(":")(1)

                Credential = New NetworkCredential
                Credential.UserName = Login
                Credential.Password = Psw

                SMTP = SMTP.Split("|")(0)
            End If

            ' Send
            Dim Client As SmtpClient = New SmtpClient(SMTP)
            Client.Credentials = Credential
            Client.Send(Message)

            Return Nothing
        Catch ex As Exception
            Return ex.Message
        End Try
    End Function

    Public Shared Function Send(Mail As SCFramework.Mail) As String
        If Not Configuration.Instance.AsyncMailer Then
            Return Mails.DirectSend(Mail)
        Else
            Mails.Add(Mail)
            Return String.Empty
        End If
    End Function

    Public Shared Function Send(Optional Number As Integer = 1) As Integer
        ' Check
        If Number < 0 Then Number = 1

        ' Get source
        Dim SQL As String = "SELECT TOP " & Number & " " & _
                                   "[A].*, " & _
                                   "[B].[CONTENT] AS [REF_SUBJECT], " & _
                                   "[C].[CONTENT] AS [REF_BODY], " & _
                                   "[D].[CONTENT] AS [REF_HTML], " & _
                                   "[E].[CONTENT] AS [REF_ATTACHMENT] " & _
                            "FROM (((([SYS_MAILS] [A] " & _
                                "LEFT OUTER JOIN [SYS_MAILS_CONTENT] [B] ON [A].[SUBJECT] = [B].[ID_CONTENT]) " & _
                                "LEFT OUTER JOIN [SYS_MAILS_CONTENT] [C] ON [A].[BODY] = [C].[ID_CONTENT]) " & _
                                "LEFT OUTER JOIN [SYS_MAILS_CONTENT] [D] ON [A].[HTML] = [D].[ID_CONTENT]) " & _
                                "LEFT OUTER JOIN [SYS_MAILS_CONTENT] [E] ON [A].[ATTACHMENT] = [E].[ID_CONTENT]) " & _
                            "WHERE [A].[ERROR] IS NULL AND " & _
                                  "[A].[SCHEDULE] < " & DbSqlBuilder.Date(Now, True) & " " & _
                            "ORDER BY [A].[ID_MAIL]"
        Dim Table As DataTable = Bridge.Query.Table(SQL)
        Dim SendedCounter As Integer = 0

        ' Cycle
        For Index As Integer = Table.Rows.Count - 1 To 0 Step -1
            Dim Row As DataRow = Table.Rows(Index)

            ' Create mail
            Dim Mail As SCFramework.Mail = New SCFramework.Mail()

            ' Set generic
            Mail.From = "" & Row!FROM
            Mail.To = "" & Row!TO
            Mail.Subject = "" & Row!REF_SUBJECT

            ' Set body
            Mail.Body = "" & Row!REF_BODY
            Mail.HTML = "" & Row!REF_HTML

            If Not String.IsNullOrEmpty(Mail.Body) Then Mail.Body = ZipHelper.Uncompress(Mail.Body)
            If Not String.IsNullOrEmpty(Mail.HTML) Then Mail.HTML = ZipHelper.Uncompress(Mail.HTML)

            ' Set format
            Mail.Format = Mails.DeserializeHashTable("" & Row!FORMAT)

            ' Set attachments
            Dim Attachment As String = "" & Row!REF_ATTACHMENT
            If Not String.IsNullOrEmpty(Attachment) Then
                Mail.Attachments = New ArrayList(Attachment.Split(","))
            End If

            ' Send
            Dim [Error] As String = Mails.DirectSend(Mail)
            If String.IsNullOrEmpty([Error]) Then
                SendedCounter += 1
                Row.Delete()
            Else
                Row!ERROR = [Error]
            End If
        Next

        ' Update
        Bridge.Query.UpdateDatabase(Table, "SYS_MAILS")

        ' Return
        Return SendedCounter
    End Function

#End Region

#Region " ERROR "

    Public Shared Sub DeleteInError()
        If Configuration.Instance.DaysToDeleteInErrorMails > 0 Then
            ' Source
            Dim RefDate As Date = Now.AddDays(-Configuration.Instance.DaysToDeleteInErrorMails)
            Dim SQL As String = "DELETE FROM [SYS_MAILS] " & _
                                "WHERE [ERROR] IS NOT NULL AND " & _
                                      "[SCHEDULE] < " & SCFramework.DbSqlBuilder.Date(RefDate, True)
            Dim Query As SCFramework.DbQuery = New SCFramework.DbQuery()
            Query.Exec(SQL)
        End If
    End Sub

#End Region

#Region " CONTENTS "

    Private Shared Function GetContents() As DataTable
        Dim SQL As String = "SELECT * " & _
                            "FROM [SYS_MAILS_CONTENT]"
        Dim Table As DataTable = Bridge.Query.Table(SQL, "SYS_MAILS_CONTENT")
        SCFramework.Utils.SetAutoIncrementColumns(Table, "ID_CONTENT")

        Return Table
    End Function

    Private Shared Function GetContentsList() As Hashtable
        Dim Source As DataTable = Mails.GetContents()
        Return Utils.ToHashTable(Source, "ID_CONTENT", "CONTENT")
    End Function

    Private Shared Sub UpdateContents(List As ArrayList)
        ' Get content list
        Dim Source As DataTable = Mails.GetContents()
        Dim Already As ArrayList = SCFramework.Utils.ToArrayList(Source, "CONTENT")
        Dim Contents As ArrayList = New ArrayList()

        ' Cycle
        For Each Item As Object In List
            If TypeOf Item Is SCFramework.Mail Then
                Dim Mail As SCFramework.Mail = CType(Item, SCFramework.Mail)

                Dim Subject As String = Mail.Subject
                Dim Body As String = Mail.Body
                Dim HTML As String = Mail.HTML
                Dim Attachments As String = Nothing

                ' Fix fields
                If Not String.IsNullOrEmpty(Body) Then Body = ZipHelper.Compress(Mail.Body)
                If Not String.IsNullOrEmpty(HTML) Then HTML = ZipHelper.Compress(Mail.HTML)

                If Mail.Attachments.Count > 0 Then Attachments = String.Join(",", Mail.Attachments.ToArray(GetType(String)))
                If Not String.IsNullOrEmpty(Attachments) Then Attachments = ZipHelper.Compress(Attachments)

                ' Check
                If Not String.IsNullOrEmpty(Subject) And _
                   Not Already.Contains(Subject) And Not Contents.Contains(Subject) Then Contents.Add(Subject)
                If Not String.IsNullOrEmpty(Body) And _
                   Not Already.Contains(Body) And Not Contents.Contains(Body) Then Contents.Add(Body)
                If Not String.IsNullOrEmpty(HTML) And _
                   Not Already.Contains(HTML) And Not Contents.Contains(HTML) Then Contents.Add(HTML)
                If Not String.IsNullOrEmpty(Attachments) And _
                   Not Already.Contains(Attachments) And Not Contents.Contains(Attachments) Then Contents.Add(Attachments)
            End If
        Next

        ' Update
        For Each Content As String In Contents
            Dim NewRow As DataRow = Source.NewRow
            NewRow!CONTENT = Content
            Source.Rows.Add(NewRow)
        Next

        ' Save
        SCFramework.Bridge.Query.UpdateDatabase(Source)
    End Sub

    Public Shared Sub CheckContentsIntegrity()
        ' Get content list
        Dim Source As DataTable = Mails.GetQueue(Nothing, Nothing, True)
        Dim Already As ArrayList = New ArrayList()

        For Each Row As DataRow In Source.Rows
            If Not IsDBNull(Row!SUBJECT) AndAlso Not Already.Contains(Row!SUBJECT) Then
                Already.Add(Row!SUBJECT)
            End If

            If Not IsDBNull(Row!BODY) AndAlso Not Already.Contains(Row!BODY) Then
                Already.Add(Row!BODY)
            End If

            If Not IsDBNull(Row!HTML) AndAlso Not Already.Contains(Row!HTML) Then
                Already.Add(Row!HTML)
            End If

            If Not IsDBNull(Row!ATTACHMENT) AndAlso Not Already.Contains(Row!ATTACHMENT) Then
                Already.Add(Row!ATTACHMENT)
            End If
        Next

        ' Compare
        Source = Mails.GetContents()
        For Each Row As DataRow In Source.Rows
            If Not Already.Contains(Row!ID_CONTENT) Then
                Row.Delete()
            End If
        Next
        Bridge.Query.UpdateDatabase(Source, "SYS_MAILS_CONTENT")
    End Sub

#End Region

#Region " SCHEDULE "

    Private Shared Sub UpdateDailyMailsCounterReport(Report As Hashtable, Current As Date)
        If Not Report.ContainsKey(Current) Then
            Report.Add(Current, 1)
        Else
            Report(Current) += 1
        End If
    End Sub

    Private Shared Function CleanDate(Value As Date) As Date
        Return New Date(Value.Year, Value.Month, Value.Day)
    End Function

    Private Shared Function CreateDailyMailsCounterReport() As Hashtable
        Dim SQL As String = "SELECT [SCHEDULE] " & _
                            "FROM [SYS_MAILS]"
        Dim Source As DataTable = Bridge.Query.Table(SQL)
        Dim HT As Hashtable = New Hashtable()

        For Each Row As DataRow In Source.Rows
            Dim Current As Date = Mails.CleanDate(Row!SCHEDULE)
            Mails.UpdateDailyMailsCounterReport(HT, Current)
        Next

        Return HT
    End Function

    Private Shared Function GetNextAvailableDay(Report As Hashtable, StartDay As Date, LimitMailsPerDay As Integer) As Date
        While True
            If StartDay < Now Then StartDay = Now
            Dim Clean As Date = Mails.CleanDate(StartDay)

            If Not Report.ContainsKey(Clean) OrElse Report(Clean) < LimitMailsPerDay Then
                Mails.UpdateDailyMailsCounterReport(Report, Clean)
                Return StartDay
            End If

            StartDay = StartDay.AddDays(1)
        End While
    End Function

    Private Shared Sub AdjustSchedule(List As ArrayList, LimitMailsPerDay As Integer)
        If LimitMailsPerDay > 0 Then
            ' Report
            Dim Report As Hashtable = Mails.CreateDailyMailsCounterReport()

            ' Cycle
            For Each Item As Object In List
                If TypeOf Item Is SCFramework.Mail Then
                    Dim Mail As SCFramework.Mail = CType(Item, SCFramework.Mail)
                    Mail.Schedule = Mails.GetNextAvailableDay(Report, Mail.Schedule, LimitMailsPerDay)
                End If
            Next
        End If
    End Sub

#End Region

#Region " QUEUE "

    Private Shared Function FindKeyByValue(List As Hashtable, Value As String) As Object
        If Not String.IsNullOrEmpty(Value) Then
            For Each Key As Integer In List.Keys
                If List(Key) = Value Then
                    Return Key
                End If
            Next
        End If
        Return DBNull.Value
    End Function

    Private Shared Sub Add(Source As DataTable, Contents As Hashtable, Mail As SCFramework.Mail, Hook As String)
        ' New row
        Dim NewRow As DataRow = Source.NewRow

        ' Generic
        NewRow!FROM = Mail.From
        NewRow!TO = Mail.To
        NewRow!SUBJECT = Mails.FindKeyByValue(Contents, Mail.Subject)
        NewRow!SCHEDULE = Mail.Schedule

        ' Setting
        NewRow!FORMAT = Mails.SerializeHashTable(Mail.Format)
        NewRow!HOOK = Hook

        ' Compressed fields
        Dim Body As String = Mail.Body
        Dim HTML As String = Mail.HTML
        Dim Attachments As String = Nothing

        If Not String.IsNullOrEmpty(Body) Then
            Body = ZipHelper.Compress(Body)
            NewRow!BODY = Mails.FindKeyByValue(Contents, Body)
        End If

        If Not String.IsNullOrEmpty(HTML) Then
            HTML = ZipHelper.Compress(HTML)
            NewRow!HTML = Mails.FindKeyByValue(Contents, HTML)
        End If

        If Mail.Attachments.Count > 0 Then Attachments = String.Join(",", Mail.Attachments.ToArray(GetType(String)))
        If Not String.IsNullOrEmpty(Attachments) Then
            Attachments = ZipHelper.Compress(Attachments)
            NewRow!ATTACHMENT = Mails.FindKeyByValue(Contents, Attachments)
        End If

        ' Add
        Source.Rows.Add(NewRow)
    End Sub

    Public Shared Sub Add(List As ArrayList, Optional Hook As String = Nothing)
        ' Update contents
        Mails.UpdateContents(List)

        ' Limit
        Mails.AdjustSchedule(List, Configuration.Instance.MailsPerDay)

        ' Get sources
        Dim Source As DataTable = Mails.GetMailsStructure()
        Dim Contents As Hashtable = Mails.GetContentsList()

        ' Cycle mails
        For Each Item As Object In List
            If TypeOf Item Is SCFramework.Mail Then
                Dim Mail As SCFramework.Mail = CType(Item, SCFramework.Mail)
                SCFramework.Mails.Add(Source, Contents, Mail, Hook)
            End If
        Next

        ' Save
        SCFramework.Bridge.Query.UpdateDatabase(Source)
    End Sub

    Public Shared Sub Add(Mails() As SCFramework.Mail, Optional Hook As String = Nothing)
        Dim List As ArrayList = New ArrayList(Mails)
        SCFramework.Mails.Add(List, Hook)
    End Sub

    Public Shared Sub Add(Mail As SCFramework.Mail, Optional Hook As String = Nothing)
        Dim List As ArrayList = New ArrayList()
        List.Add(Mail)

        SCFramework.Mails.Add(List, Hook)
    End Sub

    Public Shared Function GetQueue(Optional Hook As String = Nothing, Optional Schedule As Date = Nothing, Optional ErrorToo As Boolean = False) As DataTable
        Dim SQL As String = "SELECT * " & _
                            "FROM [SYS_MAILS] " & _
                            "WHERE 1 = 1"
        If Not String.IsNullOrEmpty(Hook) Then
            SQL &= " AND [HOOK] = " & DbSqlBuilder.String(Hook)
        End If
        If Schedule > Date.MinValue Then
            SQL &= String.Format(" AND DATEDIFF('s', {0}, SCHEDULE) = 1", SCFramework.DbSqlBuilder.Date(Schedule, True))
        End If
        If Not ErrorToo Then
            SQL &= " AND [ERROR] IS NULL"
        End If

        Return Bridge.Query.Table(SQL)
    End Function

    Public Shared Function IsQueued() As Boolean
        Dim SQL As String = "SELECT COUNT([ID_MAIL]) " & _
                            "FROM [SYS_MAILS] " & _
                            "WHERE [ERROR] IS NULL AND " & _
                                  "[SCHEDULE] <= " & DbSqlBuilder.Date(Now, True)
        Dim Value As Integer = Bridge.Query.Value(SQL)
        Return (Value > 0)
    End Function

#End Region

#Region " REMOVE "

    Public Shared Sub Remove(Mail As Long)
        ' Delete
        Dim SQL As String = "DELETE FROM [SYS_MAILS] " & _
                            "WHERE [ID_MAIL] = " & DbSqlBuilder.Numeric(Mail)
        Bridge.Query.Exec(SQL)
    End Sub

    Public Shared Sub Remove(Hook As String)
        ' Delete
        Dim SQL As String = "DELETE FROM [SYS_MAILS] " & _
                            "WHERE [HOOK] = " & DbSqlBuilder.String(Hook)
        Bridge.Query.Exec(SQL)
    End Sub

#End Region

End Class

Public Class MailsCycle

    Private _Thread As Thread = Nothing
    Private _Timer As TimeSpan = New TimeSpan(0, 0, Configuration.Instance.BlockMailsDelay)
    Private _LogFile As SCFramework.LogFile = Nothing
    Private _MailForSend As Integer = Configuration.Instance.MailPerBlock
    Private _busy As Boolean = False


    Public Sub New(Optional ByVal LogFileName As String = Nothing)
        ' Logs
        If Not LogFileName Is Nothing Then
            Me._LogFile = New SCFramework.LogFile(LogFileName)
        End If

        ' Consistency
        Mails.CheckContentsIntegrity()

        ' THREAD
        Try
            Me._Thread = New Thread(AddressOf Me.ThreadProc)
            Me._Thread.Priority = ThreadPriority.BelowNormal
            Me._Thread.Start()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub WriteLog(ByVal Message As String)
        If Me._LogFile IsNot Nothing Then
            Me._LogFile.Write(Message)
        End If
    End Sub

    Private Sub ThreadProc()
        While Me._Thread.ThreadState <> Threading.ThreadState.Stopped
            Try
                If Not Me._busy AndAlso Mails.IsQueued() Then
                    Me._busy = True

                    ' Send
                    Dim Sended As Integer = Mails.Send(Me._MailForSend)
                    Dim Message As String = String.Format("Sended {0} of {1} mails", Sended, Me._MailForSend)
                    Me.WriteLog(Message)

                    Me._busy = False
                End If

            Catch ex As Exception
                Me._busy = False

            Finally
                Thread.Sleep(Me._Timer)
            End Try
        End While
    End Sub

    Protected Overrides Sub Finalize()
        If Not Me._Thread Is Nothing Then
            Me._Thread.Abort()
            Me._Thread = Nothing
        End If

        MyBase.Finalize()
    End Sub

    Public Sub [Stop]()
        If Me._Thread IsNot Nothing Then
            Me._Thread.Abort()
        End If
    End Sub

End Class

