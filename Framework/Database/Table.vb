'*************************************************************************************************
' 
' [SCFramework]
' DbHelper  
' by Samuele Carassai
'
' Helper class to link to database (new from version 5.x)
' 
' Version 5.0.0
' Updated 16/10/2016
'
'*************************************************************************************************

Imports System.Data.Common
Imports System.Data.SqlClient


' Define the name space
Namespace DB

    ' Class definition
    Public MustInherit Class Table

        '-------------------------------------------------------------------------------------------
        ' CONSTRUCTOR

        Public Sub New()
            ' Checke for the table name
            If Utils.String.IsEmptyOrWhite(Me.Name) Then
                Throw New Exception("Define the table name at the first step.")
            End If

            'Init internal variables
            Me.mColumns = New List(Of Column)

            ' Analize
            Me.OnAnalizeTable()
        End Sub


        '-------------------------------------------------------------------------------------------
        ' PRIVATES

        ' Define the holders
        Private mQuery As Query = Nothing
        Private mSafety As Boolean = True

        Private mColumns As List(Of Column) = Nothing
        Private mSubordinates As List(Of Table) = Nothing


        ' Create a column and add to it to the list if not already in.
        Private Function FindOrCreateColumn(ColumnName As String, TypeName As String) As ColumnWrapper
            ' Search the column
            FindOrCreateColumn = Me.FindColumn(ColumnName)

            ' Check if already inside the list
            If FindOrCreateColumn Is Nothing Then
                ' Create the new column
                FindOrCreateColumn = New ColumnWrapper()
                FindOrCreateColumn.Name = ColumnName
                FindOrCreateColumn.Alias = ColumnName.Replace(" ", "_")
                FindOrCreateColumn.Type = Type.GetType(TypeName)

                ' Add to the list
                Me.mColumns.Add(FindOrCreateColumn)
            End If
        End Function


        ' OleDb analisys 
        Private Sub OleDbAnalisys(Connection As DbConnection)
            ' Connection
            Dim CustomConnection As OleDb.OleDbConnection = CType(Connection, OleDb.OleDbConnection)

            ' Primary keys
            Dim Table As DataTable = CustomConnection.GetOleDbSchemaTable(OleDb.OleDbSchemaGuid.Primary_Keys,
                                                                          New Object() {Nothing, Nothing, Me.Name()})
            For Each Row As DataRow In Table.Rows
                ' Find the column  and set it as a primary key
                Dim Column As ColumnWrapper = Me.FindOrCreateColumn(Row!COLUMN_NAME, Row!DATA_TYPE)
                Column.IsPrimaryKey = True
            Next

            ' Autonumber and Writable
            Table = CustomConnection.GetOleDbSchemaTable(OleDb.OleDbSchemaGuid.Columns,
                                                         New Object() {Nothing, Nothing, Me.Name(), Nothing})
            For Each Row As DataRow In Table.Rows
                ' Find the column  and set it as a primary key
                Dim Column As ColumnWrapper = Me.FindOrCreateColumn(Row!COLUMN_NAME, Row!DATA_TYPE)

                ' Check the type is an autonumber
                If Row!DATA_TYPE = OleDb.OleDbType.Integer AndAlso
                   Row!COLUMN_FLAGS = 90 Then
                    ' Auto Number
                    Column.IsIdentity = True
                End If
            Next
        End Sub


        ' Sql analisys
        Private Sub SqlAnalisys(Connection As DbConnection)
            ' Define the request for a specific table
            Dim Sql As String = New SqlBuilder(Me.Query.GetProvider()) _
                .Table(Me.Name()) _
                .Where(DB.Clauses.AlwaysFalse) _
                .SelectCommand

            ' Find the reader
            Dim Command As SqlCommand = New SqlCommand(Sql, Connection)
            Dim Reader As SqlDataReader = Command.ExecuteReader(CommandBehavior.KeyInfo)

            ' Find the infos table
            Dim Table As DataTable = Reader.GetSchemaTable()
            For Each Row As DataRow In Table.Rows
                ' Find the column  and set it as a primary key
                Dim Column As ColumnWrapper = Me.FindOrCreateColumn(Row!ColumnName, Row!DataTypeName)

                ' Set the column properties
                Column.IsPrimaryKey = CBool(Row!IsKey) And CBool(Row!IsUnique)
                Column.IsIdentity = CBool(Row!IsIdentity) And CBool(Row!IsAutoIncrement)
            Next
        End Sub


        ' Analize the table and store all usefull data
        Private Sub OnAnalizeTable()
            ' Private holders
            Dim Query As DB.Query = Me.Query
            Dim Connection As DbConnection = Query.GetConnection()

            ' Open
            Dim MustBeOpen As Boolean = (Query.GetConnection().State = ConnectionState.Closed)
            If MustBeOpen Then
                Connection.Open()
            End If

            ' Select the analisys by provider
            Select Case Query.GetProvider()
                Case "System.Data.OleDb" : Me.OleDbAnalisys(Connection)
                Case "System.Data.SqlClient" : Me.SqlAnalisys(Connection)
            End Select

            ' Close the connection to database
            If MustBeOpen Then
                Connection.Close()
            End If

            ' Check if the table exists
            If Me.mColumns.Count = 0 Then
                Throw New Exception("The table not exists.")
            End If
        End Sub


        ' Create a row object starting from a datareader
        Private Function CreateRow(Reader As DbDataReader) As Row
            ' Create the row
            Dim Row As Row = New RowWrapper(Me)

            ' Cycle all columns
            For Index As Integer = 0 To Me.mColumns.Count - 1
                ' Get the column by index.
                ' For work proper all the class columns must always ordered
                ' in the original way.
                Dim Column As Column = Me.mColumns(Index)

                ' Create the cell
                Dim Cell As CellWrapper = New CellWrapper(Column, Row)
                Cell.Value = Reader(Index)
                Cell.AcceptChanges()

                ' Assign the cell to the row alias member.
                ' Since row is a dynamic class if the alias not found in
                ' the members dictionary will be create a new one.
                Row(Column.Alias) = Cell
            Next

            ' Return
            Return Row
        End Function


        '-------------------------------------------------------------------------------------------
        ' PROTECTED

        ' Convert a single value in a clauses using the primary key as reference
        Protected Function ToClauses(Value As Long) As DB.Clauses
            ' Check if have at least one primary key
            Dim Keys() As String = Me.GetColumnsName(Column.Types.PrimaryKey)
            If Keys.Count > 0 Then
                ' Define the clauses
                Return New DB.Clauses(Keys(0), DB.Clauses.Comparer.Equal, Value)

            Else
                ' Else return
                Return Nothing
            End If
        End Function

        ' Convert a pair values in a dictionary
        Protected Function ToValues(Key As String, Value As Object) As Dictionary(Of String, Object)
            ' Create the holder
            ToValues = New Dictionary(Of String, Object)

            ' Check for empty values
            If Not Utils.String.IsEmptyOrWhite(Key) Then
                ' Add
                ToValues(Key, Value)
            End If
        End Function


        '-------------------------------------------------------------------------------------------
        ' PROPERTIES

        ' Get the query class to use
        Public Property Query As DB.Query
            Set(value As DB.Query)
                Me.mQuery = value
            End Set
            Get
                If Me.mQuery IsNot Nothing Then
                    ' Return the global one if the base is not empty
                    Return Me.mQuery

                Else
                    ' Else create a new one
                    Return SCFramework.Bridge.Query
                End If
            End Get
        End Property


        ' Set the safety checker
        Public Property Safety As Boolean
            Set(Value As Boolean)
                Me.mSafety = Value
            End Set
            Get
                Return Me.mSafety
            End Get
        End Property


        '-------------------------------------------------------------------------------------------
        ' PUBLIC

        ' The table name
        Public MustOverride Function Name() As String


        ' Find a column inside the list by the name
        Public Function FindColumn(ColumnName As String) As Column
            Return (From Column As Column In Me.mColumns Where Column.Alias.Equals(ColumnName)).FirstOrDefault
        End Function

        Public Function FindColumn(Index As Integer) As Column
            Return Me.mColumns.Item(Index)
        End Function


        ' Retrieve the list of column name by the column type
        Public Function GetColumnsName(Type As Column.Types) As String()
            ' Select by case
            Select Case Type
                Case Column.Types.PrimaryKey
                    ' Return only the columns marked as primary key
                    Return (From Column In Me.mColumns
                            Where Column.IsPrimaryKey
                            Select Column.Name).ToArray()

                Case Column.Types.Identity
                    ' Return only the columns marked as identity
                    Return (From Column In Me.mColumns
                            Where Column.IsIdentity
                            Select Column.Name).ToArray()

                Case Column.Types.Updatable
                    ' Return only the columns not primary key and not identity
                    Return (From Column In Me.mColumns
                            Where Not Column.IsPrimaryKey And Not Column.IsIdentity
                            Select Column.Name).ToArray()

                Case Column.Types.Writable
                    ' Return only the columns not identity
                    Return (From Column In Me.mColumns
                            Where Not Column.IsIdentity
                            Select Column.Name).ToArray()

                Case Column.Types.MultilanguageFile
                    ' Return only the columns marked as multilanguage file
                    Return (From Column In Me.mColumns
                            Where Not Column.IsMultilanguageFile
                            Select Column.Name).ToArray()

                Case Column.Types.MultilanguageText
                    ' Return only the columns marked as multilanguage text
                    Return (From Column In Me.mColumns
                            Where Not Column.IsMultilanguageText
                            Select Column.Name).ToArray()

                Case Else
                    ' Return all the columns
                    Return (From Column In Me.mColumns
                            Select Column.Name).ToArray()

            End Select
        End Function

        Public Function GetColumnsName() As String()
            Return Me.GetColumnsName(Column.Types.All)
        End Function


        ' Select command
        Public Function [Select](Clauses As DB.Clauses) As List(Of Row)
            ' Trigger for hold the connection status
            Dim MustBeOpen As Boolean = False
            Dim Reader As DbDataReader = Nothing

            Try
                ' Open connection if closed and save the state
                MustBeOpen = (Me.Query.GetConnection().State = ConnectionState.Closed)
                If MustBeOpen Then
                    Me.Query.GetConnection().Open()
                End If

                ' Create the sekect sql command
                Dim Command As String =
                    New SqlBuilder(Me.Query.GetProvider()) _
                        .Table(Me.Name()) _
                        .Where(Clauses) _
                        .SelectCommand

                ' Get the reader
                Reader = Me.Query.Reader(Command)

                ' Try to create the rows list
                [Select] = New List(Of Row)
                If Reader.HasRows Then
                    Do While Reader.Read()
                        ' Create the row and add it to the list
                        [Select].Add(Me.CreateRow(Reader))
                    Loop
                End If

            Catch ex As Exception
                Throw ex

            Finally
                ' Close all
                If Reader IsNot Nothing And Not Reader.IsClosed Then Reader.Close()
                If MustBeOpen Then Me.Query.GetConnection().Close()
            End Try
        End Function

        Public Function [Select]() As List(Of Row)
            Return Me.Select(Clauses.Empty)
        End Function


        ' Delete command
        Public Overridable Function Delete(Clauses As DB.Clauses) As Long
            ' Check for safety
            If (Me.mSafety) And (Clauses Is Nothing OrElse Clauses.IsEmpty) Then
                Throw New Exception("This command will delete all row in the table!")
            End If

            ' Create the command and execute it
            Dim Command As String = New SqlBuilder(Me.Query.GetProvider()) _
                .Table(Me.Name()) _
                .Where(Clauses) _
                .DeleteCommand
            Return Me.Query.Exec(Command, False)
        End Function


        ' Insert command
        Public Overridable Function Insert(Values As Dictionary(Of String, Object)) As Long
            ' Filters for the values can be inserted
            Dim Filtered As Dictionary(Of String, Object) = (From Pair In Values Let Column = Me.FindColumn(Pair.Key)
                                                             Where Column IsNot Nothing AndAlso Not Column.IsIdentity Select Pair) _
                .ToDictionary(Function(Pair) Pair.Key, Function(Pair) Pair.Value)

            ' Create the command and execute it
            Dim Command As String = New SqlBuilder(Me.Query.GetProvider()) _
                .Table(Me.Name()) _
                .Insert(Filtered) _
                .InsertCommand
            Return Me.Query.Exec(Command, True)
        End Function


        ' Update command
        Public Overridable Function Update(Values As Dictionary(Of String, Object), Clauses As DB.Clauses) As Long
            ' Check for safety
            If (Me.Safety) And (Clauses Is Nothing OrElse Clauses.IsEmpty) Then
                Throw New Exception("This command will update all row in the table!")
            End If

            ' Filters for the values can be updated
            Dim Filtered As Dictionary(Of String, Object) = (From Pair In Values Let Column = Me.FindColumn(Pair.Key)
                                                             Where Column IsNot Nothing AndAlso Not Column.IsIdentity AndAlso Not Column.IsPrimaryKey
                                                             Select Pair) _
                .ToDictionary(Function(Pair) Pair.Key, Function(Pair) Pair.Value)

            ' Create the command and execute it
            Dim Command As String = New SqlBuilder(Me.Query.GetProvider()) _
                .Table(Me.Name()) _
                .Update(Filtered) _
                .Where(Clauses) _
                .UpdateCommand
            Return Me.Query.Exec(Command, False)
        End Function


        '------------------------------------------------------------------------------
        ' DEFINE THE INNER COLUMN CLASS

        ' Private wrapper class.
        ' The property may changed after the instance of the class so we need to
        ' shadows the original (readonly) property to permit to the user to change
        ' the values at run-time.
        Private Class ColumnWrapper
            Inherits Column

            '------------------------------------------------------------------------------
            ' PROPERTIES

            ' Get/set the related name field on the database table
            Public Shadows Property Name As String
                Get
                    Return Me.mName
                End Get
                Set(value As String)
                    Me.mName = value
                End Set
            End Property


            ' Get/set the related name field on the database table
            Public Shadows Property Type As Type
                Get
                    Return Me.mType
                End Get
                Set(value As Type)
                    Me.mType = Type
                End Set
            End Property


            ' Get/set if is a primary key
            Public Shadows Property IsPrimaryKey As Boolean
                Get
                    Return Me.mIsPrimaryKey
                End Get
                Set(value As Boolean)
                    Me.mIsPrimaryKey = value
                End Set
            End Property


            ' Get/set if is a identity
            Public Shadows Property IsIdentity As Boolean
                Get
                    Return Me.mIsIdentity
                End Get
                Set(value As Boolean)
                    Me.mIsIdentity = value
                End Set
            End Property

        End Class


        '------------------------------------------------------------------------------
        ' DEFINE THE INNER ROW CLASS

        ' Private wrapper class.
        ' Just redefined the construtor to allow to change some holder values.
        Private Class RowWrapper
            Inherits Row

            Public Sub New(BelongingTable As Table)
                Me.mBelongingTable = BelongingTable
            End Sub

            Public Function CreateCell(Column As Column) As CellWrapper
                Return New CellWrapper(Column, Me)
            End Function

        End Class


        '------------------------------------------------------------------------------
        ' DEFINE THE INNER CELL CLASS

        ' Private wrapper class.
        ' Just redefined the construtor to allow to change some holder values.
        Private Class CellWrapper
            Inherits Cell

            Public Sub New(BelongingColumn As Column, BelongingRow As Row)
                Me.mBelongingColumn = Column
                Me.mBelongingRow = Row
            End Sub

        End Class

    End Class

End Namespace