Imports BO
Imports Facade
Imports DAL
Imports Facebook


Public Class FacebookService
    Implements IFacebookService
    Public Function FacebookFeed(feed As FacebookFeedBO, AccessToken As String, AppId As String) As Response Implements IFacebookService.FacebookFeed
        Dim response As New Response
        Try
            Dim fb As New FacebookClient(AccessToken)
            Dim args = New Dictionary(Of String, Object)()
            If Not feed.Name = "" Then args("name") = feed.Name
            If Not feed.Link = "" Then
                args("link") = feed.Link
            End If
            If Not feed.Caption = "" Then args("caption") = feed.Caption
            If Not feed.Description = "" Then args("description") = feed.Description
            If Not feed.Picture = "" Then args("picture") = feed.Picture
            args("message_tags") = feed.MessageTags
            If Not feed.Message = "" Then args("message") = feed.Message
            If Not feed.Action = "" AndAlso Not feed.Link = "" Then args("actions") = New With {.name = feed.Action, .link = feed.Actionlink}

            Dim fbresult = fb.Post("/" & AppId & "/feed", args)
            Dim result As Object = DirectCast(fbresult, IDictionary(Of String, Object))
            Dim msg As New Message
            msg.Type = MessageType.Value
            msg.Message = result.id
            response.Messages.Add(msg)
        Catch ex As Exception
            response.AddError(ex.Message)
        End Try
        Return response
    End Function
    Public Function FacebookPhoto(picture As ProjectPictureBO, FBAlbum As FacebookAlbumBO, AccessToken As String, AppId As String, PicturePath As String) As Response Implements IFacebookService.FacebookPhoto
        Dim response As New Response
        Try
            Dim fb As New FacebookClient(AccessToken)
            Dim media As New FacebookMediaObject
            media.ContentType = "image/jpeg"
            media.FileName = picture.Name
            media.SetValue(System.IO.File.ReadAllBytes(PicturePath & "\" & picture.Name))
            Dim upload As New Dictionary(Of String, Object)
            upload.Add("name", picture.Caption)
            upload.Add("@file.jpg", media)
            Dim result As Object = fb.Post("/" & FBAlbum.Id & "/Photos", upload)
            Dim msg As New Message
            msg.Type = MessageType.Value
            msg.Message = result.id
            response.Messages.Add(msg)
        Catch ex As Exception
            response.AddError(ex.Message)
        End Try
        Return response
    End Function
    Public Function FacebookPhotoDelete(id As Integer, AccessToken As String, AppId As String) As Response Implements IFacebookService.FacebookPhotoDelete
        Dim response As New Response
        Try
            Dim fb As New FacebookClient(AccessToken)
            fb.Delete(id)
        Catch ex As Exception
            response.AddError(ex.Message)

        End Try
        Return response
    End Function
    Public Function FacebookAlbum(fbalbum As FacebookAlbumBO, AccessToken As String, AppId As String) As Response Implements IFacebookService.FacebookAlbum
        Dim response As New Response
        Try
            Dim fb As New FacebookClient(AccessToken)
            If fbalbum.Id Is Nothing Then
                Dim args = New Dictionary(Of String, Object)
                args.Add("name", fbalbum.Name)
                args.Add("message", fbalbum.Description)
                Dim fbresult = fb.Post("/" & AppId & "/albums", args)
                Dim result As Object = DirectCast(fbresult, IDictionary(Of String, Object))
                Dim msg As New Message
                msg.Type = MessageType.Value
                msg.Message = result.id
                response.Messages.Add(msg)
            Else

            End If
        Catch ex As Exception
            response.AddError(ex.Message)
        End Try
        Return response
    End Function
    'Public Function GetFacebookPlacesByName(term As String) As Response Implements IFacebookService.


End Class
