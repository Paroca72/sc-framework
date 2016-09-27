'*************************************************************************************************
' 
' [SCFramework]
' Configuration
' by Samuele Carassai
'
' Configuration manager
' Version 5.0.0
' Created 03/11/2015
' Updated 03/11/2015
'
'*************************************************************************************************


Public Class Configuration

    ' Constants
    Private Const TABLE_NAME As String = "SYS_CONFIG"

    Private Const APPLICATION_NAME As String = "APPLICATION_NAME"
    Private Const APPLICATION_VERSION As String = "APPLICATION_VERSION"

    Private Const BASE_PAGE As String = "BASE_PAGE"
    Private Const GLOBAL_COUNTER As String = "GLOBAL_COUNTER"
    Private Const ON_LINE As String = "ON_LINE"

    Private Const POSITION_LATITUDE As String = "POSITION_LATITUDE"
    Private Const POSITION_LONGITUDE As String = "POSITION_LONGITUDE"

    Private Const GOOGLE_KEYMAP As String = "GOOGLE_KEYMAP"
    Private Const ANALITYCS_ACCOUNT As String = "ANALITYCS_ACCOUNT"
    Private Const ANALITYCS_DOMAIN As String = "ANALITYCS_DOMAIN"

    Private Const MAILS_SMTP As String = "MAILS_SMTP"
    Private Const MAILS_ADDRESS As String = "MAILS_ADDRESS"
    Private Const MAILS_ASYNC As String = "MAILS_ASYNC"
    Private Const MAILS_DAYSTODELETEINERROR As String = "MAILS_DAYSTODELETEINERROR"
    Private Const MAILS_PERDAYS As String = "MAILS_PERDAYS"
    Private Const MAILS_PERBLOCK As String = "MAILS_PERBLOCK"
    Private Const MAILS_BLOCKDELAY As String = "MAILS_BLOCKDELAY"

    Private Const FOLDER_PUBLIC As String = "FOLDER_PUBLIC"
    Private Const FOLDER_TEMPORARY As String = "FOLDER_TEMPORARY"


    ' Structure
    Private [Structure] As String(,) = {
        {Configuration.APPLICATION_NAME, String.Empty, "The application name"},
        {Configuration.APPLICATION_VERSION, String.Empty, "The application version"},
        {Configuration.BASE_PAGE, "~/index.html", "The default page of web-site"},
        {Configuration.GLOBAL_COUNTER, "0", "The global users access counter"},
        {Configuration.POSITION_LATITUDE, String.Empty, "Reference to the world latitude position"},
        {Configuration.POSITION_LONGITUDE, String.Empty, "Reference to the world longitude position"},
        {Configuration.GOOGLE_KEYMAP, String.Empty, "The google key map"},
        {Configuration.ANALITYCS_ACCOUNT, String.Empty, "The google analitycs account name"},
        {Configuration.ANALITYCS_DOMAIN, String.Empty, "The google analitycs domain name"},
        {Configuration.MAILS_SMTP, "127.0.0.1", "The SMPT server address to send e-mails"},
        {Configuration.MAILS_ASYNC, "false", "Send emails using an async thread"},
        {Configuration.MAILS_DAYSTODELETEINERROR, "30", "Days delay before delete the emails in error"},
        {Configuration.MAILS_PERDAYS, "5000", "Limit the number of mails sended per day"},
        {Configuration.MAILS_PERBLOCK, "10", "Limit the number of mails sended per block"},
        {Configuration.MAILS_BLOCKDELAY, "5", "The second beetwen send two block of mails"},
        {Configuration.FOLDER_PUBLIC, "public", "The application public folder"},
        {Configuration.FOLDER_TEMPORARY, "public/temporary", "The application temporary folders"}
    }


    ' Data holder
    Private mData As DataTable = Nothing


    ' Contructor
    Public Sub New()
        ' Load and fix data
        Me.Load()

        ' Check the directories
        Me.CheckDirectory(Me.PublicPath)
        Me.CheckDirectory(Me.TemporaryPath)
    End Sub


#Region " STATIC "

    ' Static instance holder
    Private Shared mInstance As Configuration = Nothing

    ' Instance property
    Public Shared ReadOnly Property Instance As Configuration
        Get
            ' Check if null
            If Configuration.mInstance Is Nothing Then
                Configuration.mInstance = New Configuration()
            End If

            ' Return the static instance
            Return Configuration.mInstance
        End Get
    End Property

#End Region

#Region " PRIVATE "

    ' Load the data and fix if have gaps
    Private Sub Load()
        ' Load
        Me.mData = Bridge.Query.Table(Configuration.TABLE_NAME)

        ' Check
        If Me.mData IsNot Nothing Then
            Throw New Exception("Impossible to read the configuration table!")
        End If

        ' Fill
        For Row As Integer = 0 To Me.Structure.Length - 1
            ' Get the current values
            Dim Key As String = Me.Structure(Row, 0)
            Dim [Default] As String = Me.Structure(Row, 1)
            Dim Description As String = Me.Structure(Row, 2)

            ' Check if exists
            Dim Finded As DataRow = Me.mData.Rows.Find(Key)
            If Finded Is Nothing Then
                ' Create a new row
                Finded = Me.mData.NewRow
                Finded!KEY = Key
                Finded!VALUE = [Default]
                Finded!DESCRIPTION = Description

                ' Add the row to the table and save the changes
                Me.mData.Rows.Add(Finded)
            End If
        Next

        ' Set the value and save the changes
        Bridge.Query.UpdateDatabase(Me.mData)
    End Sub

    ' Get the string value by key
    Private Function GetStringValue(Key As String) As String
        ' Return the value
        Dim Finded As DataRow = Me.mData.Rows.Find(Key)
        Return Finded!VALUE
    End Function

    ' Get the integer value by key
    Private Function GetIntegerValue(Key As String) As Integer
        ' Return the value
        Dim Value As Object = Me.GetStringValue(Key)
        If IsNumeric(Value) Then
            Return CInt(Value)
        Else
            Return 0
        End If
    End Function

    ' Get the integer value by key
    Private Function GetBooleanValue(Key As String) As Boolean
        ' Return the value
        Dim Value As Object = Me.GetStringValue(Key)
        If Value Is Nothing OrElse IsDBNull(Value) Then
            Return False
        Else
            Return Convert.ToBoolean(Value)
        End If
    End Function

    ' Get the string value by key
    Private Sub SetStringValue(Key As String, Value As String)
        ' Find the row and save the value
        Dim Finded As DataRow = Me.mData.Rows.Find(Key)
        Finded!VALUE = Value

        ' Update the database
        Bridge.Query.UpdateDatabase(Me.mData)
    End Sub

    ' Create a directory if not exists
    Private Sub CheckDirectory(Path As String)
        ' Check if exists
        If Not IO.Directory.Exists(Path) Then
            ' Create
            IO.Directory.CreateDirectory(Path)
        End If
    End Sub
#End Region

#Region " PUBLIC "

    ' Application
    Public Property ApplicationName() As String
        Get
            Return Me.GetStringValue(Configuration.APPLICATION_NAME)
        End Get
        Set(ByVal Value As String)
            Me.SetStringValue(Configuration.APPLICATION_NAME, Value)
        End Set
    End Property

    Public Property ApplicationVersion() As String
        Get
            Return Me.GetStringValue(Configuration.APPLICATION_VERSION)
        End Get
        Set(ByVal Value As String)
            Me.SetStringValue(Configuration.APPLICATION_VERSION, Value)
        End Set
    End Property


    ' Generic
    Public Property BasePage() As String
        Get
            Return Me.GetStringValue(Configuration.BASE_PAGE)
        End Get
        Set(ByVal Value As String)
            Me.SetStringValue(Configuration.BASE_PAGE, Value)
        End Set
    End Property

    Public Property GlobalCounter() As Integer
        Get
            Return Me.GetIntegerValue(Configuration.GLOBAL_COUNTER)
        End Get
        Set(ByVal Value As Integer)
            Me.SetStringValue(Configuration.GLOBAL_COUNTER, Value)
        End Set
    End Property

    Public Property OnLine() As Boolean
        Get
            Return Me.GetBooleanValue(Configuration.ON_LINE)
        End Get
        Set(ByVal Value As Boolean)
            Me.SetStringValue(Configuration.ON_LINE, Value)
        End Set
    End Property


    ' Coordinates
    Public Property Latitude() As String
        Get
            Return Me.GetStringValue(Configuration.POSITION_LATITUDE)
        End Get
        Set(ByVal Value As String)
            Me.SetStringValue(Configuration.POSITION_LATITUDE, Value)
        End Set
    End Property

    Public Property Longitude() As String
        Get
            Return Me.GetStringValue(Configuration.POSITION_LONGITUDE)
        End Get
        Set(ByVal Value As String)
            Me.SetStringValue(Configuration.POSITION_LONGITUDE, Value)
        End Set
    End Property


    ' Google
    Public Property GoogleMapKey() As String
        Get
            Return Me.GetStringValue(Configuration.GOOGLE_KEYMAP)
        End Get
        Set(ByVal Value As String)
            Me.SetStringValue(Configuration.GOOGLE_KEYMAP, Value)
        End Set
    End Property

    Public Property AnalitycsAccount() As String
        Get
            Return Me.GetStringValue(Configuration.ANALITYCS_ACCOUNT)
        End Get
        Set(ByVal Value As String)
            Me.SetStringValue(Configuration.ANALITYCS_ACCOUNT, Value)
        End Set
    End Property

    Public Property AnalitycsDomainName() As String
        Get
            Return Me.GetStringValue(Configuration.ANALITYCS_DOMAIN)
        End Get
        Set(ByVal Value As String)
            Me.SetStringValue(Configuration.ANALITYCS_DOMAIN, Value)
        End Set
    End Property


    ' Mails
    Public Property SMTP() As String
        Get
            Return Me.GetStringValue(Configuration.MAILS_SMTP)
        End Get
        Set(ByVal Value As String)
            Me.SetStringValue(Configuration.MAILS_SMTP, Value)
        End Set
    End Property

    Public Property GenericMail() As String
        Get
            Return Me.GetStringValue(Configuration.MAILS_ADDRESS)
        End Get
        Set(ByVal Value As String)
            Me.SetStringValue(Configuration.MAILS_ADDRESS, Value)
        End Set
    End Property

    Public Property AsyncMailer() As Boolean
        Get
            Return Me.GetBooleanValue(Configuration.MAILS_ASYNC)
        End Get
        Set(ByVal Value As Boolean)
            Me.SetStringValue(Configuration.MAILS_ASYNC, Value)
        End Set
    End Property

    Public Property DaysToDeleteInErrorMails() As Integer
        Get
            Return Me.GetIntegerValue(Configuration.MAILS_DAYSTODELETEINERROR)
        End Get
        Set(ByVal Value As Integer)
            Me.SetStringValue(Configuration.MAILS_DAYSTODELETEINERROR, Value)
        End Set
    End Property

    Public Property MailsPerDay() As Integer
        Get
            Return Me.GetIntegerValue(Configuration.MAILS_PERDAYS)
        End Get
        Set(ByVal Value As Integer)
            Me.SetStringValue(Configuration.MAILS_PERDAYS, Value)
        End Set
    End Property

    Public Property MailPerBlock() As Integer
        Get
            Return Me.GetIntegerValue(Configuration.MAILS_PERBLOCK)
        End Get
        Set(ByVal Value As Integer)
            Me.SetStringValue(Configuration.MAILS_PERBLOCK, Value)
        End Set
    End Property

    Public Property BlockMailsDelay() As Integer
        Get
            Return Me.GetIntegerValue(Configuration.MAILS_BLOCKDELAY)
        End Get
        Set(ByVal Value As Integer)
            Me.SetStringValue(Configuration.MAILS_BLOCKDELAY, Value)
        End Set
    End Property

#End Region

#Region " PATHS "

    ' Generic folders
    Public Property PublicFolder() As String
        Get
            Return Me.GetStringValue(Configuration.FOLDER_PUBLIC)
        End Get
        Set(ByVal Value As String)
            Me.SetStringValue(Configuration.FOLDER_PUBLIC, Value)
        End Set
    End Property

    Public Property TemporaryFolder() As String
        Get
            Return Me.GetStringValue(Configuration.FOLDER_TEMPORARY)
        End Get
        Set(ByVal Value As String)
            Me.SetStringValue(Configuration.FOLDER_TEMPORARY, Value)
        End Set
    End Property

    ' Physical Path
    Public ReadOnly Property PublicPath() As String
        Get
            Return Hosting.HostingEnvironment.MapPath("~/" & Me.PublicFolder)
        End Get
    End Property

    Public ReadOnly Property TemporaryPath() As String
        Get
            Return Hosting.HostingEnvironment.MapPath("~/" & Me.TemporaryFolder)
        End Get
    End Property

#End Region

End Class
