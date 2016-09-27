'*************************************************************************************************
' 
' [SCFramework]
' di Samuele Carassai
'
' User details
' Versione 5.0.0
' Created --/--/----
' Updated 25/09/2016
'
'*************************************************************************************************
'
' // DIPENDENZE //
'
'   Classi: 
'       SCFramework.Bridge
'
'
'*************************************************************************************************


Public Class UserInfo

    ' Constants
    Private Const CRYPT_KEY = "{caneuva#123456789}"

    ' Define all properties of user
    Private mID As Long = -1
    Private mGroup As Long = -1
    Private mLevel As Short = Levels.Buyer
    Private mLogin As String = Nothing
    Private mPassword As String = Nothing
    Private mPriceList As Integer = -1
    Private mSignup As Date = Date.MinValue
    Private mLastAccess As Date = Date.MinValue
    Private mLanguage As String = Nothing
    Private mCorporate As String = Nothing
    Private mEMail As String = Nothing
    Private mURL As String = Nothing
    Private mSkype As String = Nothing
    Private mFacebook As String = Nothing
    Private mTwitter As String = Nothing
    Private mPhone As String = Nothing
    Private mFax As String = Nothing
    Private mInfo As String = Nothing
    Private mVAT As String = Nothing
    Private mFiscalCode As String = Nothing
    Private mErpCode As String = Nothing
    Private mLogo As Long = -1
    Private mBirthDate As Date = Date.MinValue
    Private mGender As Short = Genders.Unknown

    Private mExtraNumber As Long = Long.MinValue
    Private mExtraString As String = Nothing

    Private mSpeditionHeader As String = Nothing
    Private mBillingAddress As UserInfo.Address = Nothing
    Private mSendingAddress As UserInfo.Address = Nothing

    Private mStatus As Boolean = False
    Private mIsApplicationUser As Boolean = False

    Private mMyBag As ArrayList = Nothing
    Private mMyWish As ArrayList = Nothing

    ' The address structure
    Public Class Address
        Public FirstName As String = Nothing
        Public LastName As String = Nothing
        Public Address As String = Nothing
        Public Province As String = Nothing
        Public City As String = Nothing
        Public PostalCode As String = Nothing
        Public Country As String = Nothing
        Public Phone As String = Nothing
    End Class

    ' The levels structure
    Public Enum Levels As Short
        Unknown = -1
        Administrator = 0
        Manager = 1
        Buyer = 2
        Dealer = 3
        Reorder = 4
        Privileged = 5
        Student = 6
        Teacher = 7
        Owner = 8
    End Enum

    ' The gender structure
    Public Enum Genders As Short
        Unknown = -1
        Male = 1
        Female = 2
    End Enum


#Region " PROPERTIES "

    ' Calculated

    Public ReadOnly Property IsAutenticated() As Boolean
        Get
            Return (Me.mID <> -1)
        End Get
    End Property

    Public ReadOnly Property IsApplicationUser() As Boolean
        Get
            Return Me.mIsApplicationUser
        End Get
    End Property

    Public ReadOnly Property IsAdministrator() As Boolean
        Get
            Return (Me.mLevel = Levels.Administrator)
        End Get
    End Property

    Public ReadOnly Property IsRoot() As Boolean
        Get
            Return (Me.mLevel = Levels.Administrator) And (UCase(Me.mLogin) = "ROOT")
        End Get
    End Property

    Public ReadOnly Property IsActive() As Boolean
        Get
            Return Me.mStatus
        End Get
    End Property


    ' Readonly

    Public ReadOnly Property ID() As Long
        Get
            Return Me.mID
        End Get
    End Property

    Public ReadOnly Property Signup() As Date
        Get
            Return Me.mSignup
        End Get
    End Property

    Public ReadOnly Property LastAccess() As Date
        Get
            Return Me.mLastAccess
        End Get
    End Property


    ' Tabled

    Public Property Group() As Long
        Get
            Return Me.mGroup
        End Get
        Set(ByVal value As Long)
            Me.mGroup = value
        End Set
    End Property

    Public Property Login() As String
        Get
            Return Me.mLogin
        End Get
        Set(ByVal value As String)
            Me.mLogin = value
        End Set
    End Property

    Public Property Password() As String
        Get
            Return Me.mPassword
        End Get
        Set(ByVal Value As String)
            Me.mPassword = Value
        End Set
    End Property

    Public Property Level() As Short
        Get
            Return Me.mLevel
        End Get
        Set(ByVal value As Short)
            Me.mLevel = value
        End Set
    End Property

    Public Property Status() As Boolean
        Get
            Return Me.mStatus
        End Get
        Set(ByVal value As Boolean)
            Me.mStatus = value
        End Set
    End Property

    Public Property PriceList() As Integer
        Get
            Return Me.mPriceList
        End Get
        Set(ByVal value As Integer)
            Me.mPriceList = value
        End Set
    End Property

    Public Property Language() As String
        Get
            Return Me.mLanguage
        End Get
        Set(ByVal value As String)
            Me.mLanguage = value
        End Set
    End Property

    Public Property Corporate() As String
        Get
            Return Me.mCorporate
        End Get
        Set(ByVal value As String)
            Me.mCorporate = value
        End Set
    End Property

    Public Property EMail() As String
        Get
            Return Me.mEMail
        End Get
        Set(ByVal value As String)
            Me.mEMail = value
        End Set
    End Property

    Public Property Skype() As String
        Get
            Return Me.mSkype
        End Get
        Set(ByVal value As String)
            Me.mSkype = value
        End Set
    End Property

    Public Property FaceBook() As String
        Get
            Return Me.mFacebook
        End Get
        Set(ByVal value As String)
            Me.mFacebook = value
        End Set
    End Property

    Public Property Twitter() As String
        Get
            Return Me.mTwitter
        End Get
        Set(ByVal value As String)
            Me.mTwitter = value
        End Set
    End Property

    Public Property URL() As String
        Get
            Return Me.mURL
        End Get
        Set(ByVal value As String)
            Me.mURL = URL
        End Set
    End Property

    Public Property Phone() As String
        Get
            Return Me.mPhone
        End Get
        Set(ByVal value As String)
            Me.mPhone = value
        End Set
    End Property

    Public Property FAX() As String
        Get
            Return Me.mFax
        End Get
        Set(ByVal value As String)
            Me.mFax = value
        End Set
    End Property

    Public Property Info() As String
        Get
            Return Me.mInfo
        End Get
        Set(ByVal value As String)
            Me.mInfo = value
        End Set
    End Property

    Public Property VAT() As String
        Get
            Return Me.mVAT
        End Get
        Set(ByVal value As String)
            Me.mVAT = value
        End Set
    End Property

    Public Property FiscalCode() As String
        Get
            Return Me.mFiscalCode
        End Get
        Set(ByVal value As String)
            Me.mFiscalCode = value
        End Set
    End Property

    Public Property ERPCode() As String
        Get
            Return Me.mErpCode
        End Get
        Set(ByVal value As String)
            Me.mErpCode = value
        End Set
    End Property

    Public Property Logo() As Long
        Get
            Return Me.mLogo
        End Get
        Set(ByVal value As Long)
            Me.mLogo = value
        End Set
    End Property

    Public Property BirthDate() As Date
        Get
            Return Me.mBirthDate
        End Get
        Set(ByVal value As Date)
            Me.mBirthDate = value
        End Set
    End Property

    Public Property Gender() As Genders
        Get
            Return Me.mGender
        End Get
        Set(ByVal value As Genders)
            Me.mGender = value
        End Set
    End Property


    ' Extra

    Public Property ExtraNumber() As Long
        Get
            Return Me.mExtraNumber
        End Get
        Set(ByVal value As Long)
            Me.mExtraNumber = value
        End Set
    End Property

    Public Property ExtraString() As String
        Get
            Return Me.mExtraString
        End Get
        Set(ByVal value As String)
            Me.mExtraString = value
        End Set
    End Property


    ' Address

    Public Property SpeditionHeader() As String
        Get
            Return mSpeditionHeader
        End Get
        Set(ByVal value As String)
            mSpeditionHeader = value
        End Set
    End Property

    Public Property SendingAddress() As UserInfo.Address
        Get
            Return Me.mSendingAddress
        End Get
        Set(ByVal value As UserInfo.Address)
            Me.mSendingAddress = value
        End Set
    End Property

    Public Property BillingAddress() As UserInfo.Address
        Get
            Return Me.mBillingAddress
        End Get
        Set(ByVal value As UserInfo.Address)
            Me.mBillingAddress = value
        End Set
    End Property


    ' Others

    Public Property MyBag() As ArrayList
        Get
            Return Me.mMyBag
        End Get
        Set(ByVal value As ArrayList)
            Me.mMyBag = value
        End Set
    End Property

    Public Property MyWish() As ArrayList
        Get
            Return Me.mMyWish
        End Get
        Set(ByVal value As ArrayList)
            Me.mMyWish = value
        End Set
    End Property

    Private Function FixPageName(ByVal Value As String) As String
        Value = Value.Replace("\", "|")
        Value = Value.Replace("/", "|")

        If Value.StartsWith("|") Then Value = Right(Value, Value.Length - 2)
        If Value.StartsWith("~") Then Value = Right(Value, Value.Length - 2)

        Return LCase(Value)
    End Function

#End Region

#Region " CONSTRUCTOR "

    Public Sub New()
        ResetData()
    End Sub

    Public Sub New(ByVal Login As String, ByVal Password As String)
        Dim User As Integer = Me.GetUser(Login, Password)

        ResetData()
        TrySystemUser(User)
    End Sub

    Public Sub New(ByVal EMail As String)
        Dim User As Integer = Me.GetUser(Login, Password)

        ResetData()
        TrySystemUser(User)
    End Sub

    Public Sub New(ByVal User As Integer)
        ResetData()
        TrySystemUser(User)
    End Sub

#End Region

#Region " PRIVATE "

    Private Function GetUserByMail(ByVal EMail As String) As Long
        Dim SQL As String = "SELECT [ID_USER] " & _
                            "FROM [SYS_USERS] " & _
                            "WHERE [EMAIL] = " & SCFramework.DbSqlBuilder.String(EMail)
        Dim User As Object = Bridge.Query.Value(SQL)
        If Not User Is Nothing And Not IsDBNull(User) Then
            Return CLng(User)
        Else
            Return -1
        End If
    End Function

    Private Function GetUser(ByVal Login As String, ByVal Password As String) As Integer
        Password = Crypt.MD5.Encrypt(Password, UserInfo.CRYPT_KEY)
        Dim SQL As String = "SELECT [ID_USER] " & _
                            "FROM [SYS_USERS] " & _
                            "WHERE [LOGIN] = " & SCFramework.DbSqlBuilder.String(Login) & " AND " & _
                                  "[PASSWORD] = " & SCFramework.DbSqlBuilder.String(Password)
        Dim Value As Object = Bridge.Query.Value(SQL)
        If Value Is Nothing OrElse IsDBNull(Value) Then
            Return -1
        Else
            Return CInt(Value)
        End If
    End Function

    Private Function GetUserRow(ByVal User As Integer) As DataRow
        Dim SQL As String = "SELECT * " & _
                            "FROM [SYS_USERS] " & _
                            "WHERE [ID_USER] = " & SCFramework.DbSqlBuilder.Numeric(User)
        Return Bridge.Query.Row(SQL)
    End Function

    Private Function TrySystemUser(ByVal User As Integer) As Boolean
        Dim Row As DataRow = Me.GetUserRow(User)
        If Not IsNothing(Row) Then
            ' Global
            If Not IsDBNull(Row!ID_USER) Then Me.mID = Row!ID_USER
            If Not IsDBNull(Row!ID_GROUP) Then Me.mGroup = Row!ID_GROUP

            If Not IsDBNull(Row!EXTRANUMBER) Then Me.mExtraNumber = Row!EXTRANUMBER
            If Not IsDBNull(Row!EXTRASTRING) Then Me.mExtraString = Row!EXTRASTRING

            If Not IsDBNull(Row!LOGIN) Then Me.mLogin = "" & Row!LOGIN
            If Not IsDBNull(Row!LEVEL) Then Me.mLevel = Row!LEVEL
            If Not IsDBNull(Row!STATUS) Then Me.mStatus = Row!STATUS
            If Not IsDBNull(Row!SIGNUP) Then Me.mSignup = Row!SIGNUP
            If Not IsDBNull(Row!LAST_ACCESS) Then Me.mLastAccess = Row!LAST_ACCESS
            If Not IsDBNull(Row!ID_PRICELIST) Then Me.mPriceList = Row!ID_PRICELIST
            If Not IsDBNull(Row!LANGUAGE) Then Me.mLanguage = Row!LANGUAGE
            If Not IsDBNull(Row!CORPORATE) Then Me.mCorporate = Row!CORPORATE
            If Not IsDBNull(Row!EMAIL) Then Me.mEMail = Row!EMAIL
            If Not IsDBNull(Row!SKYPE) Then Me.mSkype = Row!SKYPE
            If Not IsDBNull(Row!FACEBOOK) Then Me.mFacebook = Row!FACEBOOK
            If Not IsDBNull(Row!TWITTER) Then Me.mTwitter = Row!TWITTER
            If Not IsDBNull(Row!URL) Then Me.mURL = Row!URL
            If Not IsDBNull(Row!PHONE) Then Me.mPhone = Row!PHONE
            If Not IsDBNull(Row!FAX) Then Me.mFax = Row!FAX
            If Not IsDBNull(Row!INFO) Then Me.mInfo = Row!INFO
            If Not IsDBNull(Row!VAT) Then Me.mVAT = Row!VAT
            If Not IsDBNull(Row!FISCAL_CODE) Then Me.mFiscalCode = Row!FISCAL_CODE
            If Not IsDBNull(Row!ERP_CODE) Then Me.mErpCode = Row!ERP_CODE
            If Not IsDBNull(Row!LOGO) Then Me.mLogo = Row!LOGO
            If Not IsDBNull(Row!BIRTH_DATE) Then Me.mBirthDate = Row!BIRTH_DATE
            If Not IsDBNull(Row!GENDER) Then Me.mGender = Row!GENDER

            ' Password
            If Not IsDBNull(Row!PASSWORD) Then
                Me.mPassword = Crypt.MD5.Decrypt("" & Row!PASSWORD, UserInfo.CRYPT_KEY)
            End If

            ' Spedition Address
            If Not IsDBNull(Row!SPEDITION_HEADER) Then Me.mSpeditionHeader = Row!SPEDITION_HEADER
            If Not IsDBNull(Row!SEND_FIRSTNAME) Then Me.mSendingAddress.FirstName = Row!SEND_FIRSTNAME
            If Not IsDBNull(Row!SEND_LASTNAME) Then Me.mSendingAddress.LastName = Row!SEND_LASTNAME
            If Not IsDBNull(Row!SEND_ADDRESS) Then Me.mSendingAddress.Address = Row!SEND_ADDRESS
            If Not IsDBNull(Row!SEND_CITY) Then Me.mSendingAddress.City = Row!SEND_CITY
            If Not IsDBNull(Row!SEND_PROVINCE) Then Me.mSendingAddress.Province = Row!SEND_PROVINCE
            If Not IsDBNull(Row!SEND_POSTALCODE) Then Me.mSendingAddress.PostalCode = Row!SEND_POSTALCODE
            If Not IsDBNull(Row!SEND_COUNTRY) Then Me.mSendingAddress.Country = Row!SEND_COUNTRY

            ' Billing Address
            If Not IsDBNull(Row!BILL_FIRSTNAME) Then Me.mBillingAddress.FirstName = Row!BILL_FIRSTNAME
            If Not IsDBNull(Row!BILL_LASTNAME) Then Me.mBillingAddress.LastName = Row!BILL_LASTNAME
            If Not IsDBNull(Row!BILL_ADDRESS) Then Me.mBillingAddress.Address = Row!BILL_ADDRESS
            If Not IsDBNull(Row!BILL_CITY) Then Me.mBillingAddress.City = Row!BILL_CITY
            If Not IsDBNull(Row!BILL_PROVINCE) Then Me.mBillingAddress.Province = Row!BILL_PROVINCE
            If Not IsDBNull(Row!BILL_POSTALCODE) Then Me.mBillingAddress.PostalCode = Row!BILL_POSTALCODE
            If Not IsDBNull(Row!BILL_COUNTRY) Then Me.mBillingAddress.Country = Row!BILL_COUNTRY

            ' Grants
            Me.mIsApplicationUser = True

            Return True
        Else
            Return False
        End If
    End Function

    Private Sub ResetData()
        Me.mID = -1
        Me.mLevel = Levels.Buyer
        Me.mGroup = -1

        Me.mExtraNumber = Long.MinValue
        Me.mExtraString = String.Empty

        Me.mLogin = Nothing
        Me.mPassword = Nothing
        Me.mSignup = Date.MinValue
        Me.mLastAccess = Date.MinValue
        Me.mLanguage = Nothing
        Me.mCorporate = Nothing
        Me.mEMail = Nothing
        Me.mSkype = Nothing
        Me.mFacebook = Nothing
        Me.mTwitter = Nothing
        Me.mURL = Nothing
        Me.mPhone = Nothing
        Me.mFax = Nothing
        Me.mInfo = Nothing
        Me.mVAT = Nothing
        Me.mFiscalCode = Nothing
        Me.mErpCode = Nothing
        Me.mLogo = -1
        Me.mBirthDate = Date.MinValue
        Me.mGender = Genders.Unknown

        Me.mStatus = False
        Me.mIsApplicationUser = False

        Me.mMyBag = New ArrayList
        Me.mMyWish = New ArrayList

        Me.mSpeditionHeader = Nothing
        Me.mBillingAddress = New UserInfo.Address()
        Me.mSendingAddress = New UserInfo.Address()
    End Sub

#End Region

#Region " PUBLIC "

    Public Shared Function GetUsersDataTable() As DataTable
        Return UserInfo.GetUsersDataTable(Nothing)
    End Function

    Public Shared Function GetUsersDataTable(ByVal ParamArray Levels() As Short) As DataTable
        Dim Filters As String = String.Empty
        For Each Level As Short In Levels
            If Filters <> String.Empty Then Filters &= " OR "
            Filters &= "[LEVEL] = " & DbSqlBuilder.Numeric(Level)
        Next

        Dim SQL As String = "SELECT * " & _
                            "FROM [SYS_USERS] " & _
                            "WHERE [LEVEL] <> " & DbSqlBuilder.Numeric(UserInfo.Levels.Administrator)
        If Filters <> String.Empty Then
            SQL &= " AND (" & Filters & ")"
        End If

        Dim Source As DataTable = Bridge.Query.Table(SQL)
        Utils.SetAutoIncrementColumns(Source, "ID_USER")

        Return Source
    End Function

    Public Shared Function CheckForLoginExist(ByVal Login As String, Optional ByVal Exclude As Long = -1) As Boolean
        Dim SQL As String = "SELECT * " & _
                            "FROM [SYS_USERS] " & _
                            "WHERE [LOGIN] = " & SCFramework.DbSqlBuilder.String(Login)
        If Exclude > -1 Then
            SQL &= " AND [ID_USER] <> " & DbSqlBuilder.Numeric(Exclude)
        End If
        Return Bridge.Query.Exists(SQL)
    End Function

    Public Shared Function CheckForEMailExist(ByVal EMail As String) As Boolean
        Dim SQL As String = "SELECT * " & _
                            "FROM [SYS_USERS] " & _
                            "WHERE [EMAIL] = " & SCFramework.DbSqlBuilder.String(EMail)
        Return Bridge.Query.Exists(SQL)
    End Function

    Public Sub UpdateLastAccess()
        If Me.mID <> -1 Then
            Dim SQL As String = "UPDATE [SYS_USERS] " & _
                                "SET [LAST_ACCESS] = " & SCFramework.DbSqlBuilder.Date(Now, True) & " " & _
                                "WHERE [ID_USER] = " & SCFramework.DbSqlBuilder.Numeric(Me.mID)
            Dim Query As SCFramework.DbQuery = New SCFramework.DbQuery
            Query.Exec(SQL)

            Me.mLastAccess = Now
        End If
    End Sub

#End Region

#Region " SAVE AND DELETE "

    ' Delete
    Public Function Delete() As Boolean
        Dim SQL As String = "DELETE FROM [SYS_USERS] " & _
                            "WHERE [ID_USER] = " & SCFramework.DbSqlBuilder.Numeric(Me.mID)
        Me.ResetData()
        Return (Bridge.Query.Exec(SQL) = 1)
    End Function

    ' Save
    Private Function InternalSave(ByVal Password As String) As Boolean
        Dim SQL As String = "INSERT INTO [SYS_USERS] (" & _
                                    "[ID_PRICELIST], [ID_GROUP], " & _
                                    "[EXTRANUMBER], [EXTRASTRING], " & _
                                    "[LOGIN], [PASSWORD], [LEVEL], [STATUS], [LANGUAGE], [LOGO], [BIRTH_DATE], [GENDER], " & _
                                    "[EMAIL], [SKYPE], [FACEBOOK], [TWITTER], " & _
                                    "[CORPORATE], [URL], [PHONE], [FAX], [INFO], [VAT], [FISCAL_CODE], [ERP_CODE], " & _
                                    "[SPEDITION_HEADER], [SEND_FIRSTNAME], [SEND_LASTNAME], [SEND_ADDRESS], " & _
                                    "[SEND_CITY], [SEND_PROVINCE], [SEND_POSTALCODE], [SEND_COUNTRY], " & _
                                    "[BILL_FIRSTNAME], [BILL_LASTNAME], [BILL_ADDRESS], " & _
                                    "[BILL_CITY], [BILL_PROVINCE], [BILL_POSTALCODE], [BILL_COUNTRY]" & _
                                 ") VALUES (" & _
                                    DbSqlBuilder.Numeric(Me.mPriceList) & ", " & _
                                    DbSqlBuilder.Numeric(Me.mGroup) & ", " & _
                                    IIf(Me.mExtraNumber = Long.MinValue, "NULL", DbSqlBuilder.Numeric(Me.mExtraNumber)) & ", " & _
                                    DbSqlBuilder.String(Me.mExtraString) & ", " & _
                                    DbSqlBuilder.String(Me.mLogin) & ", " & _
                                    DbSqlBuilder.String(Password) & ", " & _
                                    DbSqlBuilder.Numeric(Me.mLevel) & ", " & _
                                    DbSqlBuilder.Boolean(Me.mStatus) & ", " & _
                                    DbSqlBuilder.String(Me.mLanguage) & ", " & _
                                    DbSqlBuilder.Numeric(IIf(Me.mLogo = -1, String.Empty, Me.mLogo)) & ", " & _
                                    DbSqlBuilder.Date(Me.mBirthDate) & ", " & _
                                    DbSqlBuilder.Numeric(IIf(Me.mGender = Genders.Unknown, String.Empty, Me.mGender)) & ", " & _
                                    DbSqlBuilder.String(Me.mEMail) & ", " & _
                                    DbSqlBuilder.String(Me.mSkype) & ", " & _
                                    DbSqlBuilder.String(Me.mFacebook) & ", " & _
                                    DbSqlBuilder.String(Me.mTwitter) & ", " & _
                                    DbSqlBuilder.String(Me.mCorporate) & ", " & _
                                    DbSqlBuilder.String(Me.mURL) & ", " & _
                                    DbSqlBuilder.String(Me.mPhone) & ", " & _
                                    DbSqlBuilder.String(Me.mFax) & ", " & _
                                    DbSqlBuilder.String(Me.mInfo) & ", " & _
                                    DbSqlBuilder.String(Me.mVAT) & ", " & _
                                    DbSqlBuilder.String(Me.mFiscalCode) & ", " & _
                                    DbSqlBuilder.String(Me.mErpCode) & ", " & _
                                    DbSqlBuilder.String(Me.mSpeditionHeader) & ", " & _
                                    DbSqlBuilder.String(Me.mSendingAddress.FirstName) & ", " & _
                                    DbSqlBuilder.String(Me.mSendingAddress.LastName) & ", " & _
                                    DbSqlBuilder.String(Me.mSendingAddress.Address) & ", " & _
                                    DbSqlBuilder.String(Me.mSendingAddress.City) & ", " & _
                                    DbSqlBuilder.String(Me.mSendingAddress.Province) & ", " & _
                                    DbSqlBuilder.String(Me.mSendingAddress.PostalCode) & ", " & _
                                    DbSqlBuilder.String(Me.mSendingAddress.Country) & ", " & _
                                    DbSqlBuilder.String(Me.mBillingAddress.FirstName) & ", " & _
                                    DbSqlBuilder.String(Me.mBillingAddress.LastName) & ", " & _
                                    DbSqlBuilder.String(Me.mBillingAddress.Address) & ", " & _
                                    DbSqlBuilder.String(Me.mBillingAddress.City) & ", " & _
                                    DbSqlBuilder.String(Me.mBillingAddress.Province) & ", " & _
                                    DbSqlBuilder.String(Me.mBillingAddress.PostalCode) & ", " & _
                                    DbSqlBuilder.String(Me.mBillingAddress.Country) & _
                               ")"
        Return Bridge.Query.Exec(SQL, True)
    End Function

    Private Function InternalUpdate(ByVal Password As String) As Boolean
        Dim SQL As String = "UPDATE [SYS_USERS] " & _
                            "SET [ID_PRICELIST] = " & DbSqlBuilder.Numeric(Me.mPriceList) & ", " & _
                                "[ID_GROUP] = " & DbSqlBuilder.Numeric(Me.mGroup) & ", " & _
                                "[EXTRANUMBER] = " & IIf(Me.mExtraNumber = Long.MinValue, "NULL", DbSqlBuilder.Numeric(Me.mExtraNumber)) & ", " & _
                                "[EXTRASTRING] = " & DbSqlBuilder.String(Me.mExtraString) & ", " & _
                                "[LOGIN] = " & DbSqlBuilder.String(Me.mLogin) & ", " & _
                                "[PASSWORD] = " & DbSqlBuilder.String(Password) & ", " & _
                                "[LEVEL] = " & DbSqlBuilder.Numeric(Me.mLevel) & ", " & _
                                "[LOGO] = " & DbSqlBuilder.Numeric(IIf(Me.mLogo = -1, String.Empty, Me.mLogo)) & ", " & _
                                "[BIRTH_DATE] = " & DbSqlBuilder.Date(Me.mBirthDate) & ", " & _
                                "[GENDER] = " & DbSqlBuilder.Numeric(IIf(Me.mGender = Genders.Unknown, String.Empty, Me.mGender)) & ", " & _
                                "[STATUS] = " & DbSqlBuilder.Boolean(Me.mStatus) & ", " & _
                                "[CORPORATE] = " & DbSqlBuilder.String(Me.mCorporate) & ", " & _
                                "[EMAIL] = " & DbSqlBuilder.String(Me.mEMail) & ", " & _
                                "[SKYPE] = " & DbSqlBuilder.String(Me.mSkype) & ", " & _
                                "[FACEBOOK] = " & DbSqlBuilder.String(Me.mFacebook) & ", " & _
                                "[TWITTER] = " & DbSqlBuilder.String(Me.mTwitter) & ", " & _
                                "[URL] = " & DbSqlBuilder.String(Me.mURL) & ", " & _
                                "[PHONE] = " & DbSqlBuilder.String(Me.mPhone) & ", " & _
                                "[FAX] = " & DbSqlBuilder.String(Me.mFax) & ", " & _
                                "[INFO] = " & DbSqlBuilder.String(Me.mInfo) & ", " & _
                                "[VAT] = " & DbSqlBuilder.String(Me.mVAT) & ", " & _
                                "[FISCAL_CODE] = " & DbSqlBuilder.String(Me.mFiscalCode) & ", " & _
                                "[ERP_CODE] = " & DbSqlBuilder.String(Me.mErpCode) & ", " & _
                                "[SPEDITION_HEADER] = " & DbSqlBuilder.String(Me.mSpeditionHeader) & ", " & _
                                "[SEND_FIRSTNAME] = " & DbSqlBuilder.String(Me.mSendingAddress.FirstName) & ", " & _
                                "[SEND_LASTNAME] = " & DbSqlBuilder.String(Me.mSendingAddress.LastName) & ", " & _
                                "[SEND_ADDRESS] = " & DbSqlBuilder.String(Me.mSendingAddress.Address) & ", " & _
                                "[SEND_CITY] = " & DbSqlBuilder.String(Me.mSendingAddress.City) & ", " & _
                                "[SEND_PROVINCE] = " & DbSqlBuilder.String(Me.mSendingAddress.Province) & ", " & _
                                "[SEND_POSTALCODE] = " & DbSqlBuilder.String(Me.mSendingAddress.PostalCode) & ", " & _
                                "[SEND_COUNTRY] = " & DbSqlBuilder.String(Me.mSendingAddress.Country) & ", " & _
                                "[BILL_FIRSTNAME] = " & DbSqlBuilder.String(Me.mBillingAddress.FirstName) & ", " & _
                                "[BILL_LASTNAME] = " & DbSqlBuilder.String(Me.mBillingAddress.LastName) & ", " & _
                                "[BILL_ADDRESS] = " & DbSqlBuilder.String(Me.mBillingAddress.Address) & ", " & _
                                "[BILL_CITY] = " & DbSqlBuilder.String(Me.mBillingAddress.City) & ", " & _
                                "[BILL_PROVINCE] = " & DbSqlBuilder.String(Me.mBillingAddress.Province) & ", " & _
                                "[BILL_POSTALCODE] = " & DbSqlBuilder.String(Me.mBillingAddress.PostalCode) & ", " & _
                                "[BILL_COUNTRY] = " & DbSqlBuilder.String(Me.mBillingAddress.Country) & " " & _
                        "WHERE [ID_USER] = " & DbSqlBuilder.Numeric(Me.mID)
        Return (Bridge.Query.Exec(SQL) = 1)
    End Function

    Public Function Save() As Boolean
        Dim Password As String = Me.mPassword
        Password = Crypt.MD5.Encrypt(Password, UserInfo.CRYPT_KEY)

        If Me.mID = -1 Then
            Return Me.InternalSave(Password)
        Else
            Return Me.InternalUpdate(Password)
        End If
    End Function

#End Region

#Region " CRYPT "

    Public Shared Function EncryptAllUserPassword() As Boolean
        Dim SQL As String = "SELECT * " & _
                            "FROM [SYS_USERS] " & _
                            "WHERE [LOGIN] IS NOT NULL AND " & _
                                  "[PASSWORD] IS NOT NULL"
        Dim Source As DataTable = Bridge.Query.Table(SQL)
        Dim [Error] As Boolean = False

        For Each Row As DataRow In Source.Rows
            Try
                Row!PASSWORD = Crypt.MD5.Encrypt("" & Row!PASSWORD, UserInfo.CRYPT_KEY)
            Catch ex As Exception
                [Error] = True
            End Try
        Next

        Bridge.Query.UpdateDatabase(Source, "SYS_USERS")
        Return Not [Error]
    End Function

    Public Shared Function DecryptAllUserPassword() As Boolean
        Dim SQL As String = "SELECT * " & _
                            "FROM [SYS_USERS] " & _
                            "WHERE [LOGIN] IS NOT NULL AND " & _
                                  "[PASSWORD] IS NOT NULL"
        Dim Source As DataTable = Bridge.Query.Table(SQL)
        Dim [Error] As Boolean = False

        For Each Row As DataRow In Source.Rows
            Try
                Row!PASSWORD = Crypt.MD5.Decrypt("" & Row!PASSWORD, UserInfo.CRYPT_KEY)
            Catch ex As Exception
                [Error] = True
            End Try
        Next

        Bridge.Query.UpdateDatabase(Source, "SYS_USERS")
        Return Not [Error]
    End Function

#End Region

End Class
