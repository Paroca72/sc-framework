'*************************************************************************************************
' 
' [SCFramework]
' di Samuele Carassai
'
' Users manager (new from the version 5.x)
' Versione 5.0.0
' Created --/--/----
' Created 11/10/2016
'
'*************************************************************************************************


Public Class Users
    Inherits DbHelper

#Region " OVERRIDES "

    Public Overrides Function GetTableName() As String
        Return "SYS_USERS"
    End Function

#End Region

#Region " PRIVATES "

    ' Get the first user if exists
    Public Function GetFirstUser(Source As DataTable) As User
        ' If have one or more rows return the first else null
        If Source.Rows.Count > 0 Then
            Return New User(Source.Rows(0))
        Else
            Return Nothing
        End If
    End Function

    ' Return an array of users
    Private Function GetAllUsers(Source As DataTable) As User()
        Return (From Row In Source.AsEnumerable()
                Select New User(Row)).ToArray()
    End Function

#End Region

#Region " GET INFORMATION "

    ' Get a user details filtered by email
    Public Function GetUser(ID As Long) As User
        ' Create the sql builder
        Dim SqlBuilder As DbSqlBuilder = New DbSqlBuilder() _
            .Table(Me.GetTableName()) _
            .Where(Me.ToClauses(ID))

        ' If have one or more rows return the first else null
        Dim Source As DataTable = Bridge.Query.Table(SqlBuilder.SelectCommand, Me.GetTableName())
        Return Me.GetFirstUser(Source)
    End Function

    ' Get a user details filtered by email
    Public Function GetUser(EMail As String) As User
        ' Create the sql builder
        Dim SqlBuilder As DbSqlBuilder = New DbSqlBuilder() _
            .Table(Me.GetTableName()) _
            .Where(New DbClauses("EMAIL", DbClauses.ComparerType.Equal, EMail))

        ' If have one or more rows return the first else null
        Dim Source As DataTable = Bridge.Query.Table(SqlBuilder.SelectCommand, Me.GetTableName())
        Return Me.GetFirstUser(Source)
    End Function

    ' Get a user details filtered by login and password
    Public Function GetUser(Login As String, Password As String) As User
        ' Create the clausole
        Dim Clauses As DbClauses = New DbClauses("LOGIN", DbClauses.ComparerType.Equal, Login) _
            .And("PASSWORD", DbClauses.ComparerType.Equal, Password)

        ' Create the sql builder
        Dim SqlBuilder As DbSqlBuilder = New DbSqlBuilder() _
            .Table(Me.GetTableName()) _
            .Where(Clauses)

        ' If have one or more rows return the first else null
        Dim Source As DataTable = Bridge.Query.Table(SqlBuilder.SelectCommand, Me.GetTableName())
        Return Me.GetFirstUser(Source)
    End Function

    ' Get all users list but exclude the root
    Public Function GetUsers() As User()
        ' Create the sql builder
        Dim SqlBuilder As DbSqlBuilder = New DbSqlBuilder() _
            .Table(Me.GetTableName()) _
            .Where(New DbClauses("LOGIN", SCFramework.DbClauses.ComparerType.Different, SCFramework.User.ROOT_PREFIX))

        ' Return the users list
        Dim Source As DataTable = Bridge.Query.Table(SqlBuilder.SelectCommand, Me.GetTableName())
        Return Me.GetAllUsers(Source)
    End Function

    ' Get the users list filtered by level
    Public Function GetUsers(ByVal ParamArray Levels() As Short) As User()
        ' Create the clause for levels by cycle all
        Dim Clauses As DbClauses = DbClauses.Empty
        For Each Level As Short In Levels
            ' Add the condition
            Clauses.Or("LEVEL", SCFramework.DbClauses.ComparerType.Equal, Level)
        Next

        ' Create the sql builder
        Dim SqlBuilder As DbSqlBuilder = New DbSqlBuilder() _
            .Table(Me.GetTableName()) _
            .Where(Clauses)

        ' Return the users list
        Dim Source As DataTable = Bridge.Query.Table(SqlBuilder.SelectCommand, Me.GetTableName())
        Return Me.GetAllUsers(Source)
    End Function

    ' Check if a login already exists
    Public Function LoginExists(Login As String) As Boolean
        ' Create the sql builder
        Dim SqlBuilder As DbSqlBuilder = New DbSqlBuilder() _
            .Table(Me.GetTableName()) _
            .Where(New DbClauses("LOGIN", DbClauses.ComparerType.Equal, Login))

        ' If have one or more rows return the first else null
        Dim Source As DataTable = Bridge.Query.Table(SqlBuilder.SelectCommand, Me.GetTableName())
        Return Source.Rows.Count > 0
    End Function

    ' Check if a email already exists
    Public Function EMailExists(EMail As String) As Boolean
        ' Create the sql builder
        Dim SqlBuilder As DbSqlBuilder = New DbSqlBuilder() _
            .Table(Me.GetTableName()) _
            .Where(New DbClauses("EMAIL", DbClauses.ComparerType.Equal, EMail))

        ' If have one or more rows return the first else null
        Dim Source As DataTable = Bridge.Query.Table(SqlBuilder.SelectCommand, Me.GetTableName())
        Return Source.Rows.Count > 0
    End Function

#End Region

#Region " MANAGE "

    ' Delete one user
    Public Shadows Function Delete(UserID As Long) As Long
        ' Get the user and check for the root.
        Dim User As User = Me.GetUser(UserID)

        ' The root cannot be deleted from the code only manual from the DB.
        If User.IsRoot Then
            ' Throw an exception
            Throw New Exception("The ROOT user cannot be deleted.")

        Else
            ' Create the clause and call the base method
            Return MyBase.Delete(MyBase.ToClauses(UserID))
        End If
    End Function

    ' Save the user
    Public Shadows Function Save(User As User) As Long
        ' Check for empty values
        If IsNothing(User) Then Return -1

        ' The root cannot be deleted from the code only manual from the DB.
        If User.IsRoot Then
            ' Throw an exception
            Throw New Exception("The ROOT user cannot be modified or created.")

        Else
            ' Check the case 
            If User.ID = -1 Then
                ' Create a new user
                Return MyBase.Insert(User.ToDictionary())

            Else
                ' Update the user
                Return MyBase.Update(User.ToDictionary(), Me.ToClauses(User.ID))
            End If
        End If
    End Function

#End Region

End Class
