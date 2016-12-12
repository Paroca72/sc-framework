' Define the name space
Namespace DB

    ' Public class
    Public MustInherit Class Column

        '------------------------------------------------------------------------------
        ' PRIVATES / PROTECTED

        ' Define the holders
        Protected mName As String = Nothing
        Protected mType As Type = Nothing
        Protected mIsPrimaryKey As Boolean = False
        Protected mIsIdentity As Boolean = False

        Private mAlias As String = Nothing
        Private mIsMultilanguageFile As Boolean = False
        Private mIsMultilanguageText As Boolean = False


        '------------------------------------------------------------------------------
        ' PUBLIC

        ' The different types of columns.
        ' Useful for filtering.
        Public Enum Types
            All
            Updatable
            Writable
            PrimaryKey
            Identity
            MultilanguageText
            MultilanguageFile
        End Enum


        '------------------------------------------------------------------------------
        ' PROPERTIES

        ' Get the related name field on the database table.
        Public ReadOnly Property Name As String
            Get
                Return Me.mName
            End Get
        End Property


        ' Get the related type if field.
        Public ReadOnly Property Type As Type
            Get
                Return Me.mType
            End Get
        End Property


        ' Get if is a primary key.
        Public ReadOnly Property IsPrimaryKey As Boolean
            Get
                Return Me.mIsPrimaryKey
            End Get
        End Property


        ' Get if is a identity
        Public ReadOnly Property IsIdentity As Boolean
            Get
                Return Me.mIsIdentity
            End Get
        End Property


        ' Get/set the name of the column.
        Public Property [Alias] As String
            Get
                Return IIf(Me.mAlias IsNot Nothing, Me.mAlias, Me.mName)
            End Get
            Set(Value As String)
                Me.mAlias = Value
            End Set
        End Property


        ' Get/set if this column is a multilanguage text.
        ' Note cannot have both image and text to true.
        ' So in case of text true will auto setting file to false.
        Public Property IsMultilanguageText As Boolean
            Get
                Return Me.mIsMultilanguageText
            End Get
            Set(value As Boolean)
                Me.mIsMultilanguageText = value
                If value Then
                    Me.mIsMultilanguageFile = False
                End If
            End Set
        End Property


        ' Get/set if this column is a multilanguage file.
        ' Note cannot have both image and text to true.
        ' So in case of file true will auto setting text to false.
        Public Property IsMultilanguageFile As Boolean
            Get
                Return Me.mIsMultilanguageFile
            End Get
            Set(value As Boolean)
                Me.mIsMultilanguageFile = value
                If value Then
                    Me.mIsMultilanguageText = False
                End If
            End Set
        End Property

    End Class

End Namespace
