@modeltype DetailNewsModel 

<div class="inner-toolbar clearfix">
    <ul>
        <li>
            <a href="#modaladdnews" class="btn modal-with-form " type="button" id="btnAddNews"><i class="fa fa-plus"></i> Toevoegen</a>
        </li>
        @*<li>
            @Html.HtmlActionLink("<i class='fa fa-remove'></i> Verwijderen</a>", "DeleteSelectedPhotos", "Projecten", New With {.model = Model}, New With {.id = "GeneralDataSave", .class = "btn"})
        </li>*@
           </ul>
</div>
@Using Html.BeginForm("Detail", "Projecten", FormMethod.Post, New With {.id = "FormGeneralData", .class = "form-horizontal"})
    @<text>
        @Html.HiddenFor(Function(m) m.ProjectId, New With {.id = "txtProjectId"})

        <div class="blog-posts">
                @For Each news In Model.News
                        If news.Picture.Id = 0 Then
                    @<text>
                        <article class="post post-large">

                            <div class="post-date">
                                <span class="day">@news.NewsDate.Day </span>
                                <span class="month">@news.NewsDate.ToString("MMM")</span>
                            </div>
                            <div class="post-content">

                                <h4><a href="blog-post.html">@news.TitleNL </a></h4>
                                <p>@news.TextNL </p>

                            </div>
                            <div class="post-meta">
                                @*<span><i class="fa fa-user"></i> By <a href="#">John Doe</a> </span>
                                <span><i class="fa fa-tag"></i> <a href="#">Duis</a>, <a href="#">News</a> </span>
                                <span><i class="fa fa-comments"></i> <a href="#">12 Comments</a></span>*@
                                <a class="facebookNews" data-id="@news.Id" href="javascript:void(0);"><i class="fa fa-facebook"></i>FACEBOOK</a> | 
                                <a class="editNews" data-id="@news.Id" href="#ModalEditNews"><i class="fa fa-edit"></i>BEWERKEN</a> | 
                                <a class="deleteNews" data-id="@news.Id" href="#ModalDeleteNews"><i class="fa fa-remove"></i> VERWIJDEREN</a>
                            </div>
                        </article>
                    </text>
                Else
                    @<text>
                        <article class="post post-large">
                            <div class="row">
                                <div class="col-md-7">
                                    <div class="post-date">
                                        <span class="day">@news.NewsDate.Day </span>
                                        <span class="month">@news.NewsDate.ToString("MMM")</span>
                                    </div>
                                    <div class="post-content">

                                        <h4><a href="blog-post.html">@news.TitleNL </a></h4>
                                        <p>@news.TextNL </p>

                                    </div>
                                    <div class="post-meta">
                                        <a class="facebookNews" data-id="@news.Id" href="javascript:void(0);"><i class="fa fa-facebook"></i>FACEBOOK</a> | 
                                        <a class="editNews" data-id="@news.Id" href="#ModalEditNews"><i class="fa fa-edit"></i>BEWERKEN</a> | 
                                        <a class="deleteNews" data-id="@news.Id" href="#ModalDeleteNews"><i class="fa fa-remove"></i> VERWIJDEREN</a>
                                        </div>
                                    </div>
                                <div class="col-md-5">
                                    <div class="post-image">
                                        @*<div class="owl-carousel owl-theme" data-plugin-options='{"items":1}'>
                                            <div>*@
                                                <div class="img-thumbnail">
                                                    <img class="img-responsive" src="@Url.Content(System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL") & "pictures/News/" & news.Picture.Name)" alt="">
                                                </div>
                                            @*</div>
                                            <div>
                                                <div class="img-thumbnail">
                                                    <img class="img-responsive" src="img/blog/blog-image-2.jpg" alt="">
                                                </div>
                                            </div>
                                        </div>*@
                                    </div>
                                </div>



                            </div>

                        </article>
                    </text>
                End If

                Next

        </div>

    </text>
End Using
@section scripts


End Section