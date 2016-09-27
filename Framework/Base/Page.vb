'*************************************************************************************************
' 
' [SCFramework]
' BasePage
' di Samuele Carassai
'
' Definisce l'accesso alle funzioni standard
' Versione 2.3.0
'
'------------------------------------------------------------------------------------------------
' // DIPENDENZE //
'
'   Classi: 
'       SCFramework.ManageLanguages
'       SCFramework.ServerSideViewState
'       SCFramework.Query
'       SCFramework.SystemConfig
'       SCFramework.ManageMails
'       SCFramework.UserInfo
'       SCFramework.HTMLBuilder
'       SCFramework.Generic
'
'
'*************************************************************************************************


Public Class Page
    Inherits System.Web.UI.Page

#Region " PRIVATES "

    Private DBU As SCFramework.DbQuery = Nothing

    Public Sub TraceAction(ByVal Type As String, ByVal Success As Boolean, _
                            Optional ByVal [Alias] As String = Nothing, Optional ByVal Password As String = Nothing)
        Dim Referrer As String = Bridge.Request.UserHostAddress
        Dim SQL As String = "INSERT INTO [SYS_LOGTRACER] (" & _
                                "[TYPE], [RESULT], [ALIAS], [PASSWORD], [REFERRER]" & _
                            ") VALUES (" & _
                                DbSqlBuilder.String(Type) & ", " & _
                                DbSqlBuilder.Boolean(Success) & ", " & _
                                DbSqlBuilder.String([Alias]) & ", " & _
                                DbSqlBuilder.String(Password) & ", " & _
                                DbSqlBuilder.String(Referrer) & _
                            ")"
        Try
            Me.Query.Exec(SQL)
        Catch ex As Exception
            If TypeOf Bridge.Page Is Page Then
                Me.ShowJavaMessage(ex.Message)
            Else
                Throw New Exception(ex.Message)
            End If
        End Try
    End Sub

    Private Sub AddLanguageMeta()
        Dim Meta As HtmlMeta = New HtmlMeta()
        Meta.HttpEquiv = "content-language"
        Meta.Content = System.Threading.Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName

        Me.Header.Controls.Add(Meta)
    End Sub

#End Region

#Region " PROPERTIES "

    Public ReadOnly Property PageName() As String
        Get
            Return Me.Request.AppRelativeCurrentExecutionFilePath()
        End Get
    End Property

    Public ReadOnly Property Query() As SCFramework.DbQuery
        Get
            If IsNothing(Me.DBU) Then
                Me.DBU = New SCFramework.DbQuery()
            End If
            Return Me.DBU
        End Get
    End Property

#End Region

#Region " USER "

    Public Property CurrentUser() As SCFramework.UserInfo
        Get
            If Session("CurrentUser") Is Nothing Then
                Session("CurrentUser") = New SCFramework.UserInfo
            End If
            Return CType(Session("CurrentUser"), SCFramework.UserInfo)
        End Get
        Set(ByVal Value As SCFramework.UserInfo)
            Session("CurrentUser") = Value
        End Set
    End Property

#End Region

#Region " INITIALIZE "

    Private Sub Page_Load(ByVal sender As Object, ByVal e As Global.System.EventArgs) Handles MyBase.Load
        ' Ever
        Me.AddLanguageMeta()

        ' Analyze
        If IsPostBack Then
            Dim Target As String = Request.Form(UI.Page.postEventSourceID)
            Dim Argument As String = Request.Form(UI.Page.postEventArgumentID)
            AnalizePostBack(Target, Argument)
        End If
    End Sub

    Private Sub Page_PreRender(sender As Object, e As System.EventArgs) Handles Me.PreRender
    End Sub

#End Region

#Region " PUBLIC "

    Public Enum LoginResult As Byte
        Success
        EmptyAlias
        EmptyPassword
        WrongFields
        UserNotActive
        UncknowUserLevel
    End Enum

    Public Sub ForceReload(Optional ByVal EndResponse As Boolean = True)
        Response.Redirect(Me.Request.Url.AbsoluteUri, EndResponse)
    End Sub

    Public Function Login(ByVal [Alias] As String, ByVal Password As String) As Byte
        Dim Result As LoginResult = LoginResult.Success

        If String.IsNullOrEmpty([Alias].Trim) Then Result = LoginResult.EmptyAlias
        If String.IsNullOrEmpty(Password.Trim) Then Result = LoginResult.EmptyPassword

        Dim User As SCFramework.UserInfo = New SCFramework.UserInfo(Trim([Alias]), Trim(Password))
        User.UpdateLastAccess()

        If Not User.IsAutenticated Then
            Result = LoginResult.WrongFields
        ElseIf Not User.IsActive Then
            Result = LoginResult.UserNotActive
        Else
            Me.CurrentUser = User
            Select Case User.Level
                Case SCFramework.UserInfo.Levels.Administrator, _
                     SCFramework.UserInfo.Levels.Manager
                    ' OK

                Case SCFramework.UserInfo.Levels.Buyer, _
                     SCFramework.UserInfo.Levels.Dealer, _
                     SCFramework.UserInfo.Levels.Reorder
                    ' OK

                Case SCFramework.UserInfo.Levels.Student, _
                     SCFramework.UserInfo.Levels.Teacher
                    ' OK

                Case SCFramework.UserInfo.Levels.Privileged
                    ' OK

                Case Else
                    Result = LoginResult.UncknowUserLevel
            End Select
        End If

        Me.TraceAction("Login", Result <> LoginResult.Success, [Alias], Password)
        Return Result
    End Function

    Public Sub Logout()
        ' Resetta l'utente
        Me.CurrentUser = New SCFramework.UserInfo()

        ' Ritorna alla pagina base
        Me.Response.Redirect("~/" & Configuration.Instance.BasePage)
    End Sub

    Public Sub MustBeAutenticated(ByVal ParamArray Levels() As Integer)
        Me.MustBeAutenticated()
        If Array.IndexOf(Levels, Me.CurrentUser.Level) <> -1 Then
            Me.Response.Redirect("~/" & Configuration.Instance.BasePage)
        End If
    End Sub

    Public Sub MustBeAutenticated(Level As Integer)
        MustBeAutenticated(New Integer() {Level})
    End Sub

    Public Sub MustBeAutenticated()
        If Not Me.CurrentUser.IsAutenticated Then
            Me.Response.Redirect("~/" & Configuration.Instance.BasePage)
        End If
    End Sub

    Public Sub ShowJavaMessage(ByVal Message As String)
        SCFramework.HTML.ShowJavaMessage(Message, Me)
    End Sub

#End Region

#Region " PROTECTED "

    Protected Overridable Sub AnalizePostBack(ByVal Target As String, ByVal Argument As String)

    End Sub

    Protected Overrides Sub OnInit(ByVal e As Global.System.EventArgs)
        ClientScript.GetPostBackEventReference(Me.Page, "")
        Languages.CheckForUserRequestLanguage()
    End Sub

#End Region

End Class

