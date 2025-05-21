Imports BO
Public Interface IFacebookService
    'Messages
    Function FacebookFeed(Feed As FacebookFeedBO, AccessToken As String, AppId As String) As Response
    'Photos
    Function FacebookPhoto(picture As ProjectPictureBO, FBAlbum As FacebookAlbumBO, AccessToken As String, AppId As String, PicturePath As String) As Response
    Function FacebookPhotoDelete(id As Integer, AccessToken As String, AppId As String) As Response
    Function FacebookAlbum(fbalbum As FacebookAlbumBO, AccessToken As String, AppId As String) As Response
End Interface
