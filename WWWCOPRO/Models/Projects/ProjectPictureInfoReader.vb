Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Web

' Afmetingen + "heeft varianten"-vlag van de foto's van een project (tabel ProjectPictures, door CPMCore beheerd), voor de
' responsieve beelden op de publieke projectpagina (srcset + width/height).
'
' Eén query per project, 5 minuten gecachet. Ontbreekt de kolom HasVariants (migratie 087 nog niet gedraaid), dan lezen we enkel
' de afmetingen en gebruikt de pagina de oude "pictures"-map; bij een databasefout valt alles terug op de oude weergave.
Public Class ProjectPictureInfo
    Public Property Name As String
    Public Property Caption As String
    ' Alt-tekst per foto (ProjectPictures.AltText, CPMCore); leeg = geen eigen alt-tekst
    Public Property AltText As String
    ' Mag de foto publiek getoond worden (ProjectPictures.IsPublic)?
    Public Property IsPublic As Boolean
    ' Zit de foto in de sectie "Galerij" (en is die sectie publiek)? Enkel die foto's komen links in beeld.
    Public Property InGallery As Boolean
    Public Property SortOrder As Integer
    Public Property Type As Integer
    Public Property Uploaded As DateTime
    Public Property Width As Integer
    Public Property Height As Integer
    Public Property HasVariants As Boolean
End Class

Public Class ProjectPictureInfoReader
    Private Const CacheMinutes As Integer = 5

    Public Shared Function Load(projectId As Integer) As Dictionary(Of String, ProjectPictureInfo)
        Dim key As String = "pdv4-pics-" & projectId
        Dim cached = TryCast(HttpRuntime.Cache.Get(key), Dictionary(Of String, ProjectPictureInfo))
        If cached IsNot Nothing Then Return cached

        Dim result As Dictionary(Of String, ProjectPictureInfo)
        Try
            result = Query(projectId, True, True)
        Catch
            Try
                result = Query(projectId, True, False)
            Catch
                Try
                    result = Query(projectId, False, False)
                Catch
                    result = New Dictionary(Of String, ProjectPictureInfo)(StringComparer.OrdinalIgnoreCase)
                End Try
            End Try
        End Try
        HttpRuntime.Cache.Insert(key, result, Nothing, DateTime.UtcNow.AddMinutes(CacheMinutes), System.Web.Caching.Cache.NoSlidingExpiration)
        Return result
    End Function

    Private Shared Function Query(projectId As Integer, withVariants As Boolean, withAlt As Boolean) As Dictionary(Of String, ProjectPictureInfo)
        Dim d As New Dictionary(Of String, ProjectPictureInfo)(StringComparer.OrdinalIgnoreCase)
        ' Publiek-vlag en sectie komen uit ProjectPictures / ProjectMediaSection (CPMCore: Foto's & video's). "Galerij" is de sectie
        ' waarvan de foto's links op de pagina komen; de foto's in een niet-publieke sectie worden nooit getoond.
        Dim sql As String = "SELECT p.Name, p.Caption, p.WidthPx, p.HeightPx, p.IsPublic, p.SortOrder, p.Type, p.Datetimeuploaded, " &
            "CASE WHEN s.Name IN ('Galerij', 'Gallerij') AND s.IsPublic = 1 THEN 1 ELSE 0 END AS InGallery" &
            If(withVariants, ", p.HasVariants", "") & If(withAlt, ", p.AltText", "") &
            " FROM dbo.ProjectPictures p LEFT JOIN dbo.ProjectMediaSection s ON s.Id = p.SectionId" &
            " WHERE p.ProjectId = @id AND p.MediaType = 0"
        Using conn As New SqlConnection(ConfigurationManager.ConnectionStrings("testdbSql").ConnectionString)
            conn.Open()
            Using cmd As New SqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@id", projectId)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        If reader.IsDBNull(0) Then Continue While
                        d(reader.GetString(0)) = New ProjectPictureInfo With {
                            .Name = reader.GetString(0),
                            .Caption = If(reader.IsDBNull(1), Nothing, reader.GetString(1)),
                            .Width = If(reader.IsDBNull(2), 0, reader.GetInt32(2)),
                            .Height = If(reader.IsDBNull(3), 0, reader.GetInt32(3)),
                            .IsPublic = Not reader.IsDBNull(4) AndAlso reader.GetBoolean(4),
                            .SortOrder = If(reader.IsDBNull(5), 0, reader.GetInt32(5)),
                            .Type = If(reader.IsDBNull(6), 0, reader.GetInt32(6)),
                            .Uploaded = If(reader.IsDBNull(7), DateTime.MinValue, reader.GetDateTime(7)),
                            .InGallery = reader.GetInt32(8) = 1,
                            .HasVariants = withVariants AndAlso Not reader.IsDBNull(9) AndAlso reader.GetBoolean(9),
                            .AltText = If(withAlt AndAlso Not reader.IsDBNull(10), reader.GetString(10).Trim(), Nothing)}
                    End While
                End Using
            End Using
        End Using
        Return d
    End Function
End Class
