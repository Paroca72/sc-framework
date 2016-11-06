'*************************************************************************************************
' 
' [SCFramework]
' di Samuele Carassai
'
' User details
' Versione 5.0.0
' Created --/--/----
' Updated 11/10/2016
'
'*************************************************************************************************


Public Class User

    ' Constants
    Private Const CRYPT_KEY = "{caneuva#123456789}"
    Public Const ROOT_PREFIX = "ROOT"


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
    Private mBillingAddress As User.Address = Nothing
    Private mSendingAddress As User.Address = Nothing

    Private mActive As Boolean = False

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

    Public ReadOnly Property IsAdministrator() As Boolean
        Get
            Return (Me.mLevel = Levels.Administrator)
        End Get
    End Property

    Public ReadOnly Property IsRoot() As Boolean
        Get
            Return (Me.mLevel = Levels.Administrator) And (UCase(Me.mLogin) = User.ROOT_PREFIX)
        End Get
    End Property

    Public ReadOnly Property IsActive() As Boolean
        Get
            Return Me.mActive
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

    Public Property Active() As Boolean
        Get
            Return Me.mActive
        End Get
        Set(ByVal value As Boolean)
            Me.mActive = value
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

    Public Property LastAccess() As Date
        Get
            Return Me.mLastAccess
        End Get
        Set(value As Date)
            Me.mLastAccess = value
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

    Public Property SendingAddress() As User.Address
        Get
            Return Me.mSendingAddress
        End Get
        Set(ByVal value As User.Address)
            Me.mSendingAddress = value
        End Set
    End Property

    Public Property BillingAddress() As User.Address
        Get
            Return Me.mBillingAddress
        End Get
        Set(ByVal value As User.Address)
            Me.mBillingAddress = value
        End Set
    End Property


    ' Others
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
        Me.ResetData()
    End Sub

    Public Sub New(ByVal Row As DataRow)
        Me.ResetData()
        Me.Fill(Row)
    End Sub

#End Region

#Region " PRIVATES "

    ' Check the DB column for exists and not null
    Private Function CheckColumn(Row As DataRow, Column As String) As Boolean
        Return Row.Table.Columns.Contains(Column) AndAlso Not IsDBNull(Row(Column))
    End Function

    ' Fill the class from a datarow
    Private Sub Fill(ByVal Row As DataRow)
        If Not IsNothing(Row) Then
            ' Global
            If Me.CheckColumn(Row, "ID_USER") Then Me.mID = Row!ID_USER
            If Me.CheckColumn(Row, "ID_GROUP") Then Me.mGroup = Row!ID_GROUP

            If Me.CheckColumn(Row, "EXTRANUMBER") Then Me.mExtraNumber = Row!EXTRANUMBER
            If Me.CheckColumn(Row, "EXTRASTRING") Then Me.mExtraString = Row!EXTRASTRING

            If Me.CheckColumn(Row, "LOGIN") Then Me.mLogin = "" & Row!LOGIN
            If Me.CheckColumn(Row, "LEVEL") Then Me.mLevel = Row!LEVEL
            If Me.CheckColumn(Row, "ACTIVE") Then Me.mActive = Row!ACTIVE
            If Me.CheckColumn(Row, "SIGNUP") Then Me.mSignup = Row!SIGNUP
            If Me.CheckColumn(Row, "LAST_ACCESS") Then Me.mLastAccess = Row!LAST_ACCESS
            If Me.CheckColumn(Row, "ID_PRICELIST") Then Me.mPriceList = Row!ID_PRICELIST
            If Me.CheckColumn(Row, "LANGUAGE") Then Me.mLanguage = Row!LANGUAGE
            If Me.CheckColumn(Row, "CORPORATE") Then Me.mCorporate = Row!CORPORATE
            If Me.CheckColumn(Row, "EMAIL") Then Me.mEMail = Row!EMAIL
            If Me.CheckColumn(Row, "SKYPE") Then Me.mSkype = Row!SKYPE
            If Me.CheckColumn(Row, "FACEBOOK") Then Me.mFacebook = Row!FACEBOOK
            If Me.CheckColumn(Row, "TWITTER") Then Me.mTwitter = Row!TWITTER
            If Me.CheckColumn(Row, "URL") Then Me.mURL = Row!URL
            If Me.CheckColumn(Row, "PHONE") Then Me.mPhone = Row!PHONE
            If Me.CheckColumn(Row, "FAX") Then Me.mFax = Row!FAX
            If Me.CheckColumn(Row, "INFO") Then Me.mInfo = Row!INFO
            If Me.CheckColumn(Row, "VAT") Then Me.mVAT = Row!VAT
            If Me.CheckColumn(Row, "FISCAL_CODE") Then Me.mFiscalCode = Row!FISCAL_CODE
            If Me.CheckColumn(Row, "ERP_CODE") Then Me.mErpCode = Row!ERP_CODE
            If Me.CheckColumn(Row, "LOGO") Then Me.mLogo = Row!LOGO
            If Me.CheckColumn(Row, "BIRTH_DATE") Then Me.mBirthDate = Row!BIRTH_DATE
            If Me.CheckColumn(Row, "GENDER") Then Me.mGender = Row!GENDER

            ' Password
            If Me.CheckColumn(Row, "PASSWORD") Then
                Me.mPassword = Crypt.MD5.Decrypt(Row!PASSWORD, User.CRYPT_KEY)
            End If

            ' Spedition Address
            If Me.CheckColumn(Row, "SPEDITION_HEADER") Then Me.mSpeditionHeader = Row!SPEDITION_HEADER
            If Me.CheckColumn(Row, "SEND_FIRSTNAME") Then Me.mSendingAddress.FirstName = Row!SEND_FIRSTNAME
            If Me.CheckColumn(Row, "SEND_LASTNAME") Then Me.mSendingAddress.LastName = Row!SEND_LASTNAME
            If Me.CheckColumn(Row, "SEND_ADDRESS") Then Me.mSendingAddress.Address = Row!SEND_ADDRESS
            If Me.CheckColumn(Row, "SEND_CITY") Then Me.mSendingAddress.City = Row!SEND_CITY
            If Me.CheckColumn(Row, "SEND_PROVINCE") Then Me.mSendingAddress.Province = Row!SEND_PROVINCE
            If Me.CheckColumn(Row, "SEND_POSTALCODE") Then Me.mSendingAddress.PostalCode = Row!SEND_POSTALCODE
            If Me.CheckColumn(Row, "SEND_COUNTRY") Then Me.mSendingAddress.Country = Row!SEND_COUNTRY

            ' Billing Address
            If Me.CheckColumn(Row, "BILL_FIRSTNAME") Then Me.mBillingAddress.FirstName = Row!BILL_FIRSTNAME
            If Me.CheckColumn(Row, "BILL_LASTNAME") Then Me.mBillingAddress.LastName = Row!BILL_LASTNAME
            If Me.CheckColumn(Row, "BILL_ADDRESS") Then Me.mBillingAddress.Address = Row!BILL_ADDRESS
            If Me.CheckColumn(Row, "BILL_CITY") Then Me.mBillingAddress.City = Row!BILL_CITY
            If Me.CheckColumn(Row, "BILL_PROVINCE") Then Me.mBillingAddress.Province = Row!BILL_PROVINCE
            If Me.CheckColumn(Row, "BILL_POSTALCODE") Then Me.mBillingAddress.PostalCode = Row!BILL_POSTALCODE
            If Me.CheckColumn(Row, "BILL_COUNTRY") Then Me.mBillingAddress.Country = Row!BILL_COUNTRY
        End If
    End Sub

    ' Reset all user fields
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
        Me.mActive = False

        Me.mSpeditionHeader = Nothing
        Me.mBillingAddress = New User.Address()
        Me.mSendingAddress = New User.Address()
    End Sub

#End Region

#Region " PUBLIC "

    ' Convert the struture in an key/value pair hashtable
    Public Function ToDictionary() As Dictionary(Of String, Object)
        ' Create the container
        Dim List As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()

        ' Fill
        List.Add("ID_USER", Me.mID)
        List.Add("ID_GROUP", Me.mGroup)

        List.Add("EXTRANUMBER", Me.mExtraNumber)
        List.Add("EXTRASTRING", Me.mExtraString)

        List.Add("LOGIN", Me.mLogin)
        List.Add("LEVEL", Me.mLevel)
        List.Add("ACTIVE", Me.mActive)
        List.Add("SIGNUP", Me.mSignup)
        List.Add("LAST_ACCESS", Me.mLastAccess)
        List.Add("ID_PRICELIST", Me.mPriceList)
        List.Add("LANGUAGE", Me.mLanguage)
        List.Add("CORPORATE", Me.mCorporate)
        List.Add("EMAIL", Me.mEMail)
        List.Add("SKYPE", Me.mSkype)
        List.Add("FACEBOOK", Me.mFacebook)
        List.Add("TWITTER", Me.mTwitter)
        List.Add("URL", Me.mURL)
        List.Add("PHONE", Me.mPhone)
        List.Add("FAX", Me.mFax)
        List.Add("INFO", Me.mInfo)
        List.Add("VAT", Me.mVAT)
        List.Add("FISCAL_CODE", Me.mFiscalCode)
        List.Add("ERP_CODE", Me.mErpCode)
        List.Add("LOGO", Me.mLogo)
        List.Add("BIRTH_DATE", Me.mBirthDate)
        List.Add("GENDER", Me.mGender)

        ' Password
        List.Add("PASSWORD", Crypt.MD5.Encrypt(Password, User.CRYPT_KEY))

        ' Spedition Address
        List.Add("SPEDITION_HEADER", Me.mSpeditionHeader)
        List.Add("SEND_FIRSTNAME", Me.mSendingAddress.FirstName)
        List.Add("SEND_LASTNAME", Me.mSendingAddress.LastName)
        List.Add("SEND_ADDRESS", Me.mSendingAddress.Address)
        List.Add("SEND_CITY", Me.mSendingAddress.City)
        List.Add("SEND_PROVINCE", Me.mSendingAddress.Province)
        List.Add("SEND_POSTALCODE", Me.mSendingAddress.PostalCode)
        List.Add("SEND_COUNTRY", Me.mSendingAddress.Country)

        ' Billing Address
        List.Add("BILL_FIRSTNAME", Me.mBillingAddress.FirstName)
        List.Add("BILL_LASTNAME", Me.mBillingAddress.LastName)
        List.Add("BILL_ADDRESS", Me.mBillingAddress.Address)
        List.Add("BILL_CITY", Me.mBillingAddress.City)
        List.Add("BILL_PROVINCE", Me.mBillingAddress.Province)
        List.Add("BILL_POSTALCODE", Me.mBillingAddress.PostalCode)
        List.Add("BILL_COUNTRY", Me.mBillingAddress.Country)

        ' Return
        Return List
    End Function

#End Region

End Class
