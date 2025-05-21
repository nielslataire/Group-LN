@modeltype DetailPhotosModel 

<div class="inner-toolbar clearfix">
    <ul>
        <li>
            <a href="#modaladdphoto" class="btn modal-with-form " type="button" id="btnAddPhoto"><i class="fa fa-plus"></i> Toevoegen</a>
        </li>
        @*<li>
            @Html.HtmlActionLink("<i class='fa fa-remove'></i> Verwijderen</a>", "DeleteSelectedPhotos", "Projecten", New With {.model = Model}, New With {.id = "GeneralDataSave", .class = "btn"})
        </li>*@
           </ul>
</div>
@Using Html.BeginForm("Detail", "Projecten", FormMethod.Post, New With {.id = "FormGeneralData", .class = "form-horizontal"})
    @<text>
@Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "txtProjectId"})

            <div class="row mg-files" data-sort-destination data-sort-id="media-gallery">
                @For Each picture In Model.Photos
                    If Not picture.Type = BO.PictureType.Nieuws Then
                        @<text>
                <div class="isotope-item image col-sm-6 col-md-4 col-lg-3">
                    <div class="thumbnail">
                        <div class="thumb-preview">
                            <a class="thumb-image" href="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/" & picture.Name)">
                                <img src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/447/" & picture.Name)" class="img-responsive" alt="@picture.Caption">
                            </a>
                            <div class="mg-thumb-options">
                                <div class="mg-zoom"><i class="fa fa-search"></i></div>
                                <div class="mg-toolbar">
                                    @*<div class="mg-option checkbox-custom checkbox-inline">
                                        <input type="checkbox" id="check_@picture.Id" value="@picture.Id">
                                        <label for="check_@picture.Id">SELECT</label>
                                    </div>*@
                                    <div class="mg-group pull-right">
                                        <a class="FacebookPhoto" data-id="@picture.Id" href="#ModalDeletePhoto">FACEBOOK</a>
                                        <a class="deletePhoto" data-id="@picture.Id" href="#ModalDeletePhoto">VERWIJDEREN</a>
                                        <button class="dropdown-toggle mg-toggle" type="button" data-toggle="dropdown">
                                            <i class="fa fa-caret-up"></i>
                                        </button>
                                        <ul class="dropdown-menu" role="menu">
                                         @If picture.Type = BO.PictureType.Hoofdfoto Then
                                                                                    @<text>
                                                                                        <li><a href="@Url.Action("UpdatePhotoType", "Projecten", New With {.id = picture.Id, .type = BO.PictureType.Nevenfoto})"><i class="fa fa-check-square"></i> Project</a></li>
                                                                                        <li><a href="@Url.Action("UpdatePhotoType", "Projecten", New With {.id = picture.Id, .type = BO.PictureType.Werffoto})"><i class="fa fa-check-square"></i> Uitvoering</a></li>
                                                                                    </text>

                                         ElseIf picture.Type = BO.PictureType.Nevenfoto Then
                                                                                    @<text>
                                                                                        <li><a href="@Url.Action("UpdatePhotoType", "Projecten", New With {.id = picture.Id, .type = BO.PictureType.Hoofdfoto})" onclick="function"><i class="fa fa-check-square"></i> Hoofd</a></li>
                                                                                        <li><a href="@Url.Action("UpdatePhotoType", "Projecten", New With {.id = picture.Id, .type = BO.PictureType.Werffoto})"><i class="fa fa-check-square"></i> Uitvoering</a></li>
                                                                                    </text>

                                         ElseIf picture.Type = BO.PictureType.Werffoto Then
                                                                                    @<text>
                                                                                        <li><a href="@Url.Action("UpdatePhotoType", "Projecten", New With {.id = picture.Id, .type = BO.PictureType.Hoofdfoto})"><i class="fa fa-check-square"></i> Hoofd</a></li>
                                                                                        <li><a href="@Url.Action("UpdatePhotoType", "Projecten", New With {.id = picture.Id, .type = BO.PictureType.Nevenfoto})"><i class="fa fa-check-square"></i> Project</a></li>
                                                                                    </text>
                                         End If
                                        </ul>
                                    </div>
                                </div>
                            </div>
                        </div>
                        @*<h5 class="mg-title text-weight-semibold visible-xl">@picture.Caption</h5>*@
                        <h5 class="mg-smallertitle visible-md visible-sm visible-xs visible-lg">@picture.Caption</h5>
                        @*<div class="mg-title small ">@picture.Caption </div>*@
                        <div class="mg-description">
                            <small class="pull-left text-muted">@picture.Type</small>
                            <small class="pull-right text-muted">@picture.DateTimeUploaded.ToShortDateString</small>
                        </div>
                    </div>
                </div>
                </text>
                    End If
                Next
            
            </div> 

</text>
                End Using
@section scripts
<script>
  
</script>
End Section