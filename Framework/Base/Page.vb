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
    Inherits Web.UI.Page

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
        Dim Meta As Web.UI.HtmlControls.HtmlMeta = New Web.UI.HtmlControls.HtmlMeta()
        Meta.HttpEquiv = "content-language"
        Meta.Content = System.Threading.Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName

        Me.Header.Controls.Add(Meta)
    End Sub

    ' Check if have a user request to change the current language
    Public Sub CheckForUserRequestLanguage()
        Try
            ' Check in the query string if have the tag < language >
            Dim Language As String = Trim("" & Bridge.Page.Request.QueryString(Languages.QUERYSTRING_TAG))
            If Not String.IsNullOrEmpty(Language) Then
                ' Set the current language
                ' TODO: create a static languages manager
                Dim LanguageManager As SCFramework.Languages = New SCFramework.Languages()
                LanguageManager.Current = Language
            End If

        Catch ex As Exception
            ' Do nothing
        End Try
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

    Public Property CurrentUser() As SCFramework.User
        Get
            If Session("CurrentUser") Is Nothing Then
                Session("CurrentUser") = New SCFramework.User
            End If
            Return CType(Session("CurrentUser"), SCFramework.User)
        End Get
        Set(ByVal Value As SCFramework.User)
            Session("CurrentUser") = Value
        End Set
    End Property

#End Region

#Region " INITIALIZE "

    Private Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
        ' Ever
        Me.AddLanguageMeta()

        ' Analyze
        If IsPostBack Then
            Dim Target As String = Request.Form(Web.UI.Page.postEventSourceID)
            Dim Argument As String = Request.Form(Web.UI.Page.postEventArgumentID)
            AnalizePostBack(Target, Argument)
        End If
    End Sub

    Private Sub Page_PreRender(sender As Object, e As EventArgs) Handles Me.PreRender
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
        ' Define the result trigger
        Dim Result As LoginResult = LoginResult.Success

        If String.IsNullOrEmpty([Alias].Trim) Then Result = LoginResult.EmptyAlias
        If String.IsNullOrEmpty(Password.Trim) Then Result = LoginResult.EmptyPassword

        ' Get the user by alias and password
        ' TODO: create a static users manager
        Dim UsersManager As SCFramework.Users = New SCFramework.Users()
        Dim User As SCFramework.User = UsersManager.GetUser(Trim([Alias]), Trim(Password))

        ' Check the access
        If User Is Nothing OrElse Not User.IsAutenticated Then
            Result = LoginResult.WrongFields

        ElseIf Not User.IsActive Then
            Result = LoginResult.UserNotActive

        Else
            Me.CurrentUser = User
            Select Case User.Level
                Case SCFramework.User.Levels.Administrator,
                     SCFramework.User.Levels.Manager
                    ' OK

                Case SCFramework.User.Levels.Buyer,
                     SCFramework.User.Levels.Dealer,
                     SCFramework.User.Levels.Reorder
                    ' OK

                Case SCFramework.User.Levels.Student,
                     SCFramework.User.Levels.Teacher
                    ' OK

                Case SCFramework.User.Levels.Privileged
                    ' OK

                Case Else
                    Result = LoginResult.UncknowUserLevel

            End Select
        End If

        ' Update the last access
        User.LastAccess = Now
        UsersManager.Save(User)

        ' Trace it and return
        Me.TraceAction("Login", Result <> LoginResult.Success, [Alias], Password)
        Return Result
    End Function

    Public Sub Logout()
        ' Resetta l'utente
        Me.CurrentUser = New SCFramework.User()

        ' Ritorna alla pagina base
        Me.Response.Redirect("~/" & Bridge.Configuration.BasePage)
    End Sub

    Public Sub MustBeAutenticated(ByVal ParamArray Levels() As Integer)
        Me.MustBeAutenticated()
        If Array.IndexOf(Levels, Me.CurrentUser.Level) <> -1 Then
            Me.Response.Redirect("~/" & Bridge.Configuration.BasePage)
        End If
    End Sub

    Public Sub MustBeAutenticated(Level As Integer)
        MustBeAutenticated(New Integer() {Level})
    End Sub

    Public Sub MustBeAutenticated()
        If Not Me.CurrentUser.IsAutenticated Then
            Me.Response.Redirect("~/" & Bridge.Configuration.BasePage)
        End If
    End Sub

    Public Sub ShowJavaMessage(ByVal Message As String)
        ' TODO
    End Sub

#End Region

#Region " PROTECTED "

    Protected Overridable Sub AnalizePostBack(ByVal Target As String, ByVal Argument As String)

    End Sub

    Protected Overrides Sub OnInit(ByVal e As Global.System.EventArgs)
        ' Postback
        ClientScript.GetPostBackEventReference(Me.Page, "")
        ' Check if have a user request to change the current language
        Me.CheckForUserRequestLanguage()
    End Sub

#End Region

End Class

