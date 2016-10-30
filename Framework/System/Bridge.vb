'*************************************************************************************************
' 
' [SCFramework]
' Bridge  
' by Samuele Carassai
'
' Define bridge to the application
' Version 5.0.0
' Created 14/10/2016
' Updated --/--/----
'
'*************************************************************************************************


Public Class Bridge

#Region " ACCESS FROM THE CONTEXT "

    ' Get the Context object if exists
    Public Shared ReadOnly Property Context() As Web.HttpContext
        Get
            Try
                Return Web.HttpContext.Current()
            Catch ex As Exception
                Return Nothing
            End Try
        End Get
    End Property

    ' Get the Request object if exists
    Public Shared ReadOnly Property Request() As Web.HttpRequest
        Get
            If Context Is Nothing Then
                Return Nothing
            Else
                Return Context.Request
            End If
        End Get
    End Property

    ' Get the Response object if exists
    Public Shared ReadOnly Property Response() As Web.HttpResponse
        Get
            If Context Is Nothing Then
                Return Nothing
            Else
                Return Context.Response
            End If
        End Get
    End Property

    ' Get the Application object if exists
    Public Shared ReadOnly Property Application() As Web.HttpApplicationState
        Get
            If Context Is Nothing Then
                Return Nothing
            Else
                Return Context.Application
            End If
        End Get
    End Property

    ' Get the Server object if exists
    Public Shared ReadOnly Property Server() As Web.HttpServerUtility
        Get
            If Context Is Nothing Then
                Return Nothing
            Else
                Return Context.Server
            End If
        End Get
    End Property

    ' Get the standard Page object if exists
    Public Shared ReadOnly Property [Page]() As System.Web.UI.Page
        Get
            If Context Is Nothing Then
                Return Nothing
            Else
                Return CType(Context.Handler, System.Web.UI.Page)
            End If
        End Get
    End Property

    ' Get the SCFramework base Page object if exists
    Public Shared ReadOnly Property [BasePage]() As SCFramework.Page
        Get
            If (Bridge.Page IsNot Nothing) AndAlso (TypeOf Bridge.Page Is SCFramework.Page) Then
                Return CType(Bridge.Page, SCFramework.Page)
            Else
                Return Nothing
            End If
        End Get
    End Property

    ' Get the Session object if exists
    Public Shared ReadOnly Property [Session]() As Web.SessionState.HttpSessionState
        Get
            If Context Is Nothing Then
                Return Nothing
            Else
                Return CType(Context.Session, Web.SessionState.HttpSessionState)
            End If
        End Get
    End Property

    ' Get the Query object from the current context page if exists. 
    ' If Not, create a New one.
    Public Shared Function Query() As SCFramework.DbQuery
        If Bridge.BasePage IsNot Nothing Then
            Dim [Page] As Page = CType(Bridge.Page, Page)
            Return [Page].Query
        Else
            Dim DB As SCFramework.DbQuery = New SCFramework.DbQuery
            Return DB
        End If
    End Function

#End Region

#Region " STATIC CLASSES "

    ' Holders
    Private Shared mStats As SCFramework.Stats = Nothing
    Private Shared mConfiguration As SCFramework.Configuration = Nothing


    ' Stats
    Public Shared ReadOnly Property Stats As SCFramework.Stats
        Get
            ' Check if null and return the class static reference
            If Bridge.mStats IsNot Nothing Then Bridge.mStats = New SCFramework.Stats()
            Return Bridge.mStats
        End Get
    End Property

    ' Configuration
    Public Shared ReadOnly Property Configuration As SCFramework.Configuration
        Get
            ' Check if null and return the class static reference
            If Bridge.mConfiguration IsNot Nothing Then Bridge.mConfiguration = New SCFramework.Configuration()
            Return Bridge.mConfiguration
        End Get
    End Property

#End Region

End Class
