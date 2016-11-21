'*************************************************************************************************
' 
' [SCFramework]
' Files management
' by Samuele Carassai
'
' Files management
' Version 5.0.0
' Created 02/11/2015
' Updated 13/10/2016
'
'*************************************************************************************************


Public Class Files
    Inherits SCFramework.Multilanguages


#Region " MUST OVERRIDES "

    ' Define the linked databse table name
    Public Overrides Function GetTableName() As String
        Return "SYS_FILES"
    End Function

#End Region

#Region " PRIVATE "

    ' Delete the file or files from the phisical drive
    Private Sub DeletePhisically(Path As String)
        ' Check for empty values
        If Not SCFramework.Utils.String.IsEmptyOrWhite(Path) Then
            ' Get the file phisical path
            Path = Web.Hosting.HostingEnvironment.MapPath(Path)
            ' Check if file exists
            If IO.File.Exists(Path) Then
                ' Delete 
                IO.File.Delete(Path)
            End If
        End If
    End Sub

    Private Sub DeletePhisically(Source As List(Of DataRow))
        ' Check for empty values
        If Source IsNot Nothing Then
            ' Cycle all rows
            For Each Row As DataRow In Source
                Me.DeletePhisically(Row!VALUE)
            Next
        End If
    End Sub

    ' Check the path field
    Private Sub CheckPath(Path As String)
        ' Check for virtual path
        If Not Path.StartsWith("~/") Then
            Throw New Exception("The file path must be in virtual format.")
        End If

        ' Check if file exists
        Dim PhisicalPath As String = Web.Hosting.HostingEnvironment.MapPath(Path)
        If Not IO.File.Exists(Path) Then
            Throw New Exception("File not exists.")
        End If
    End Sub

#End Region

#Region " PROTECTED "

    Protected Overrides Function ElaborateToDelete(Source As DataTable) As List(Of DataRow)
        ' Call the base methods and get back the list of the file to delete
        Dim ToDelete As List(Of DataRow) = MyBase.ElaborateToDelete(Source)
        Me.DeletePhisically(ToDelete)

        ' Return
        Return ToDelete
    End Function

#End Region

#Region " PUBLIC "

    ' Get the file path in language.
    ' This method is same as GetValue and made only for coerence with file name.
    Public Function GetFilePath(Key As String, Language As String) As String
        Return Me.GetValue(Key, Language)
    End Function

    ' Insert command
    Public Shadows Sub Insert(Key As String, FilePath As String, Language As String)
        ' Check the fields and call the base
        Me.CheckPath(FilePath)
        MyBase.Insert(Key, FilePath, Language)
    End Sub

#End Region

End Class

