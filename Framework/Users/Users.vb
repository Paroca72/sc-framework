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

#Region " STATIC "

    ' Static instance holder
    Private Shared mInstance As Users = Nothing

    ' Instance property
    Public Shared ReadOnly Property Instance As Users
        Get
            ' Check if null
            If Users.mInstance Is Nothing Then
                Users.mInstance = New Users()
            End If

            ' Return the static instance
            Return Users.mInstance
        End Get
    End Property

#End Region

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
        ' Create the container
        Dim List As List(Of User) = New List(Of User)

        ' Cycle all rows
        For Each Row As DataRow In Source.Rows
            ' Create the user and insert it inside the list
            List.Add(New User(Row))
        Next

        ' Return
        Return List.ToArray()
    End Function

#End Region

#Region " GET INFORMATION "

    ' Get a user details filtered by email
    Public Function GetUser(ID As Long) As User
        ' Create the clausole
        Dim Clauses As SCFramework.DbClauses = New SCFramework.DbClauses()
        Clauses.Add("ID_USER", ID)

        ' If have one or more rows return the first else null
        Dim Source As DataTable = Bridge.Query.Table(Me.GetTableName(), Nothing, Clauses)
        Return Me.GetFirstUser(Source)
    End Function

    ' Get a user details filtered by email
    Public Function GetUser(EMail As String) As User
        ' Create the clausole
        Dim Clauses As SCFramework.DbClauses = New SCFramework.DbClauses()
        Clauses.Add("EMAIL", EMail)

        ' If have one or more rows return the first else null
        Dim Source As DataTable = Bridge.Query.Table(Me.GetTableName(), Nothing, Clauses)
        Return Me.GetFirstUser(Source)
    End Function

    ' Get a user details filtered by login and password
    Public Function GetUser(Login As String, Password As String) As User
        ' Create the clausole
        Dim Clauses As SCFramework.DbClauses = New SCFramework.DbClauses()
        Clauses.Add("LOGIN", Login)
        Clauses.Add("PASSWORD", Password)

        ' If have one or more rows return the first else null
        Dim Source As DataTable = Bridge.Query.Table(Me.GetTableName(), Nothing, Clauses)
        Return Me.GetFirstUser(Source)
    End Function

    ' Get all users list but exclude the root
    Public Function GetUsers() As User()
        ' Create the clause for levels by cycle all
        Dim Clauses As SCFramework.DbClauses = New SCFramework.DbClauses()
        Clauses.Add("LOGIN", SCFramework.DbClauses.ComparerType.Different, SCFramework.User.ROOT_PREFIX, False)

        ' Return the users list
        Dim Source As DataTable = Bridge.Query.Table(Me.GetTableName(), Nothing, Clauses)
        Return Me.GetAllUsers(Source)
    End Function

    ' Get the users list filtered by level
    Public Function GetUsers(ByVal ParamArray Levels() As Short) As User()
        ' Create the clause for levels by cycle all
        Dim Clauses As SCFramework.DbClauses = New SCFramework.DbClauses()
        For Each Level As Short In Levels
            ' Add the condition
            Clauses.Add("LEVEL", SCFramework.DbClauses.ComparerType.Equal, Level, False)
        Next

        ' Return the users list
        Dim Source As DataTable = Bridge.Query.Table(Me.GetTableName(), Nothing, Clauses)
        Return Me.GetAllUsers(Source)
    End Function

    ' Check if a login already exists
    Public Function LoginExists(Login As String) As Boolean
        ' Create the clausole
        Dim Clauses As SCFramework.DbClauses = New SCFramework.DbClauses()
        Clauses.Add("LOGIN", Login)

        ' If have one or more rows return the first else null
        Dim Source As DataTable = Bridge.Query.Table(Me.GetTableName(), Nothing, Clauses)
        Return Source.Rows.Count > 0
    End Function

    ' Check if a email already exists
    Public Function EMailExists(EMail As String) As Boolean
        ' Create the clausole
        Dim Clauses As SCFramework.DbClauses = New SCFramework.DbClauses()
        Clauses.Add("EMAIL", EMail)

        ' If have one or more rows return the first else null
        Dim Source As DataTable = Bridge.Query.Table(Me.GetTableName(), Nothing, Clauses)
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
                Return MyBase.Insert(User.ToHashTable())

            Else
                ' Update the user
                Return MyBase.Update(User.ToHashTable(), Me.ToClauses(User.ID))
            End If
        End If
    End Function

#End Region

End Class
