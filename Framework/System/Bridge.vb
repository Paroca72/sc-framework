'*************************************************************************************************
' 
' [SCFramework]
' Bridge
' di Samuele Carassai
'
' Interfaccia comune
' Versione 1.4.1
'
'*************************************************************************************************
'
' // DIPENDENZE //
'
'   Classi: 
'       SCFramework.SystemConfig
'
'
'*************************************************************************************************


Public Class Bridge

    Public Shared ReadOnly Property Context() As HttpContext
        Get
            Try
                Return HttpContext.Current()
            Catch ex As Exception
                Return Nothing
            End Try
        End Get
    End Property

    Public Shared ReadOnly Property Request() As HttpRequest
        Get
            If Context Is Nothing Then
                Return Nothing
            Else
                Return Context.Request
            End If
        End Get
    End Property

    Public Shared ReadOnly Property Response() As HttpResponse
        Get
            If Context Is Nothing Then
                Return Nothing
            Else
                Return Context.Response
            End If
        End Get
    End Property

    Public Shared ReadOnly Property Application() As HttpApplicationState
        Get
            If Context Is Nothing Then
                Return Nothing
            Else
                Return Context.Application
            End If
        End Get
    End Property

    Public Shared ReadOnly Property Server() As HttpServerUtility
        Get
            If Context Is Nothing Then
                Return Nothing
            Else
                Return Context.Server
            End If
        End Get
    End Property

    Public Shared ReadOnly Property [Page]() As Page
        Get
            If Context Is Nothing Then
                Return Nothing
            Else
                Return CType(Context.Handler, Page)
            End If
        End Get
    End Property

    Public Shared ReadOnly Property [BasePage]() As Page
        Get
            If (Bridge.Page IsNot Nothing) AndAlso (TypeOf Bridge.Page Is Page) Then
                Return CType(Bridge.Page, Page)
            Else
                Return Nothing
            End If
        End Get
    End Property

    Public Shared ReadOnly Property [Session]() As SessionState.HttpSessionState
        Get
            If Context Is Nothing Then
                Return Nothing
            Else
                Return CType(Context.Session, SessionState.HttpSessionState)
            End If
        End Get
    End Property

    ' ------------------------------------
    ' CREATE IF NOT EXIST

    Public Shared Function Query() As SCFramework.DbQuery
        If Bridge.BasePage IsNot Nothing Then
            Dim [Page] As Page = CType(Bridge.Page, Page)
            Return [Page].Query
        Else
            Dim DB As SCFramework.DbQuery = New SCFramework.DbQuery
            Return DB
        End If
    End Function

    Public Shared Function CurrentUser() As User
        If Bridge.Session Is Nothing OrElse Bridge.Session("CurrentUser") Is Nothing Then
            If Bridge.Session Is Nothing Then
                Return New User()
            Else
                Bridge.Session("CurrentUser") = New User()
                Return Bridge.Session("CurrentUser")
            End If
        Else
            Return CType(Bridge.Session("CurrentUser"), User)
        End If
    End Function

End Class
