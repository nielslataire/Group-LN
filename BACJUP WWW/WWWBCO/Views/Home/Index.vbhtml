@Code
    ViewData("Title") = "BCO - Aannemingen"
    Layout = "~/Views/Shared/_Layout.vbhtml"
End Code
<!-- Current Page CSS -->


<div class="slider-container light rev_slider_wrapper">
    <div id="revolutionSlider" class="slider rev_slider" data-plugin-revolution-slider data-plugin-options='{"gridwidth": 1170, "gridheight": 500, "disableProgressBar": "on"}'>

        <ul>
            @*<li data-transition="fade">

                <img src="@Url.Content("~/content/img/slides/slide1.jpg")"
                     alt="Residentie Scheldepunt - Gent"
                     data-bgposition="right center"
                     data-bgpositionend="center center"
                     data-bgfit="cover"
                     data-bgrepeat="no-repeat"
                     data-kenburns="on"
                     data-duration="3000"
                     data-ease="Linear.easeNone"
                     data-scalestart="100"
                     data-scaleend="100"
                     data-rotatestart="0"
                     data-rotateend="0"
                     data-offsetstart="0 0"
                     data-offsetend="0 0"
                     data-bgparallax="10"
                     class="rev-slidebg">

              

                <div class="tp-caption top-label tp-caption-overlay-opacity"
                     data-x="20"
                     data-y="50"
                     data-start="500"
                     data-transform_in="y:[-300%];opacity:0;s:500;">GENT</div>

               

                <div class="tp-caption main-label tp-caption-overlay-opacity"
                     data-x="20"
                     data-y="80"
                     data-start="1000"
                     data-whitespace="nowrap"
                     data-transform_in="y:[100%];s:500;"
                     data-transform_out="opacity:0;s:500;"
                     data-mask_in="x:0px;y:0px;">Res. Scheldepunt</div>

                <div class="tp-caption bottom-label tp-caption-overlay-opacity"
                     data-x="20"
                     data-y="130"
                     data-start="1500"
                     data-transform_in="y:[100%];opacity:0;s:500;">28 appartementen en 10 lofts met ondergrondse parkeergarage</div>

            </li>*@
            <li data-transition="fade">

                <img src="@Url.Content("~/content/img/slides/slide1.jpg")"
                     alt="Residentie Locanda - Maldegem"
                     data-bgposition="right center"
                     data-bgpositionend="center center"
                     data-bgfit="cover"
                     data-bgrepeat="no-repeat"
                     data-kenburns="on"
                     data-duration="3000"
                     data-ease="Linear.easeNone"
                     data-scalestart="100"
                     data-scaleend="100"
                     data-rotatestart="0"
                     data-rotateend="0"
                     data-offsetstart="0 0"
                     data-offsetend="0 0"
                     data-bgparallax="10"
                     class="rev-slidebg">

                <div class="tp-caption top-label tp-caption-overlay-opacity"
                     data-x="left"
                     data-y="50"
                     data-start="500"
                     data-transform_in="y:[-300%];opacity:0;s:500;">Maldegem</div>

                <div class="tp-caption main-label tp-caption-overlay-opacity"
                     data-x="left"
                     data-y="80"
                     data-start="1000"
                     style="z-index: 5"
                     data-transform_in="y:[100%];s:500;"
                     data-transform_out="opacity:0;s:500;">Res. Locanda</div>

                <div class="tp-caption bottom-label tp-caption-overlay-opacity"
                     data-x="left"
                     data-y="130"
                     data-start="1500"
                     data-transform_in="y:[100%];opacity:0;s:500;">5 appartementen met grote zuidgerichte terrassen en 1 handelspand</div>
               
                <a class="tp-caption btn btn-primary font-weight-bold hidden-xs hidden-sm"
                   href="~/woonprojecten"
                   data-frames='[{"delay":0,"speed":2000,"frame":"0","from":"y:50%;opacity:0;","to":"y:0;o:1;","ease":"Power3.easeInOut"},{"delay":"wait","speed":300,"frame":"999","to":"opacity:0;fb:0;","ease":"Power3.easeInOut"}]'
                   data-x="center" data-hoffset="['90','90','90','165']"
                   data-y="center" data-voffset="['65','65','65','210']"
                   data-paddingtop="['15','15','15','30']"
                   data-paddingbottom="['15','15','15','30']"
                   data-paddingleft="['33','33','33','50']"
                   data-paddingright="['33','33','33','50']"
                   data-fontsize="['13','13','13','25']"
                   data-lineheight="['20','20','20','25']">Ontdek ons aanbod <i class="fa fa-arrow-right ml-1"></i></a>
            </li>
            <li data-transition="fade">

                <img src="@Url.Content("~/content/img/slides/slide2.jpg")"
                     alt="Residentie Wonnegaarde - Otegem"
                     data-bgposition="right center"
                     data-bgpositionend="center center"
                     data-bgfit="cover"
                     data-bgrepeat="no-repeat"
                     data-kenburns="on"
                     data-duration="3000"
                     data-ease="Linear.easeNone"
                     data-scalestart="100"
                     data-scaleend="100"
                     data-rotatestart="0"
                     data-rotateend="0"
                     data-offsetstart="0 0"
                     data-offsetend="0 0"
                     data-bgparallax="10"
                     class="rev-slidebg">

                <div class="tp-caption top-label tp-caption-overlay-opacity"
                     data-x="right"
                     data-y="50"
                     data-start="500"
                     data-transform_in="y:[-300%];opacity:0;s:500;">Otegem</div>

                <div class="tp-caption main-label tp-caption-overlay-opacity"
                     data-x="right"
                     data-y="80"
                     data-start="1000"
                     style="z-index: 5"
                     data-transform_in="y:[100%];s:500;"
                     data-transform_out="opacity:0;s:500;">Res. Wonnegaarde</div>

                <div class="tp-caption bottom-label tp-caption-overlay-opacity"
                     data-x="right"
                     data-y="130"
                     data-start="1500"
                     data-transform_in="y:[100%];opacity:0;s:500;">8 BEN-appartementen</div>
                <a class="tp-caption btn btn-primary font-weight-bold hidden-xs hidden-sm"
                   href="~/woonprojecten"
                   data-frames='[{"delay":0,"speed":2000,"frame":"0","from":"y:50%;opacity:0;","to":"y:0;o:1;","ease":"Power3.easeInOut"},{"delay":"wait","speed":300,"frame":"999","to":"opacity:0;fb:0;","ease":"Power3.easeInOut"}]'
                   data-x="center" data-hoffset="['90','90','90','165']"
                   data-y="center" data-voffset="['65','65','65','210']"
                   data-paddingtop="['15','15','15','30']"
                   data-paddingbottom="['15','15','15','30']"
                   data-paddingleft="['33','33','33','50']"
                   data-paddingright="['33','33','33','50']"
                   data-fontsize="['13','13','13','25']"
                   data-lineheight="['20','20','20','25']">Ontdek ons aanbod <i class="fa fa-arrow-right ml-1"></i></a>
            </li>
            <li data-transition="fade">

                <img src="@Url.Content("~/content/img/slides/slide3.jpg")"
                     alt="Residentie Kervyn - Mariakerke"
                     data-bgposition="right center"
                     data-bgpositionend="center center"
                     data-bgfit="cover"
                     data-bgrepeat="no-repeat"
                     data-kenburns="on"
                     data-duration="3000"
                     data-ease="Linear.easeNone"
                     data-scalestart="100"
                     data-scaleend="100"
                     data-rotatestart="0"
                     data-rotateend="0"
                     data-offsetstart="0 0"
                     data-offsetend="0 0"
                     data-bgparallax="10"
                     class="rev-slidebg">

                <div class="tp-caption top-label tp-caption-overlay-opacity"
                     data-x="left"
                     data-y="50"
                     data-start="500"
                     data-transform_in="y:[-300%];opacity:0;s:500;">MARIAKERKE (GENT)</div>

                <div class="tp-caption main-label tp-caption-overlay-opacity"
                     data-x="left"
                     data-y="80"
                     data-start="1000"
                     style="z-index: 5"
                     data-transform_in="y:[100%];s:500;"
                     data-transform_out="opacity:0;s:500;">Res. Kervyn</div>

                <div class="tp-caption bottom-label tp-caption-overlay-opacity"
                     data-x="left"
                     data-y="130"
                     data-start="1500"
                     data-transform_in="y:[100%];opacity:0;s:500;">9 grondgebonden woningen met parktuin</div>
                <a class="tp-caption btn btn-primary font-weight-bold hidden-xs hidden-sm"
                   href="~/woonprojecten"
                   data-frames='[{"delay":0,"speed":2000,"frame":"0","from":"y:50%;opacity:0;","to":"y:0;o:1;","ease":"Power3.easeInOut"},{"delay":"wait","speed":300,"frame":"999","to":"opacity:0;fb:0;","ease":"Power3.easeInOut"}]'
                   data-x="center" data-hoffset="['90','90','90','165']"
                   data-y="center" data-voffset="['65','65','65','210']"
                   data-paddingtop="['15','15','15','30']"
                   data-paddingbottom="['15','15','15','30']"
                   data-paddingleft="['33','33','33','50']"
                   data-paddingright="['33','33','33','50']"
                   data-fontsize="['13','13','13','25']"
                   data-lineheight="['20','20','20','25']">Ontdek ons aanbod <i class="fa fa-arrow-right ml-1"></i></a>
            </li>
        </ul>
    </div>
</div>
<div class="container visible-xs visible-sm background-color-primary text-center pt-sm pb-sm">
    <a href="~/woonprojecten" class="text-color-light font-weight-bold ">Ontdek ons aanbod <i class="fa fa-arrow-right ml-1"></i></a>
</div>
<section class="section section-light m-none">
    <div class="container">
        <div class="row mb-xl">
            <div class="col-md-4">
                <h2 class="mb-xl"><strong>Wie</strong> zijn we</h2>
                <p>BCO is een aannemer vooral actief in de residentiële sector met meer dan 15 jaar ervaring. We zijn vooral gespecialiseerd in nieuwbouwappartementen maar gaan geen enkele uitdaging uit de weg.</p>
                <div>
                    <span class="thumb-info thumb-info-lighten thumb-info-centered-info thumb-info-no-borders mt-lg">
                        <span class="thumb-info-wrapper">
                            <img src="@Url.content("~/content/img/bureel.jpg")" class="img-responsive" alt="">
                           
                        </span>
                    </span>
                    @*<span class="img-thumbnail">
                        <img alt="" class="img-responsive" src="@Url.content("~/content/img/bureel.jpg")">
                    </span>*@
                </div>
                <br />
                @*<a class="btn btn-default mr-xs mb-sm" href="@Url.Action("Index","AboutUs")">Lees Meer</a>*@
            </div>
            <div class="col-md-8">
                <h2 class="mb-xl"><strong>Onze</strong> troeven</h2>

                <div class="row">
                    <div class="col-md-6">
                        <div class="feature-box feature-box-style-2 appear-animation" data-appear-animation="fadeInLeft" data-appear-animation-delay="0">
                            <div class="feature-box-icon">
                                <i class="icon-book-open  icons"></i>
                            </div>
                            <div class="feature-box-info">
                                <h4 class="mb-sm">Kennis</h4>
                                <p class="mb-lg">De jarenlange ervaring van de zaakvoerders als werfleider en projectleider bij diverse aannemingsbedrijven en de daarbij opgedane kennis zijn een garantie voor een kwalitatief afgewerkt project.</p>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="feature-box feature-box-style-2 appear-animation" data-appear-animation="fadeInLeft" data-appear-animation-delay="0">
                            <div class="feature-box-icon">
                                <i class="icon-home icons"></i>
                            </div>
                            <div class="feature-box-info">
                                <h4 class="mb-sm">Functioneel en tijdloos</h4>
                                <p class="mb-lg">Wij werken samen met architecten die uw woonwensen vertalen in een functioneel en tijdloos geheel met aandacht voor kwalitatieve en energie-efficiënte materialen.</p>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="row mt-md">
                    <div class="col-md-6">
                        <div class="feature-box feature-box-style-2 appear-animation" data-appear-animation="fadeInLeft" data-appear-animation-delay="300">
                            <div class="feature-box-icon">
                                <i class="icon-calendar  icons"></i>
                            </div>
                            <div class="feature-box-info">
                                <h4 class="mb-sm">Planning</h4>
                                <p class="mb-lg">Een goede uitvoering van het project, een deskundige selectie van aannemers en een strakke coördinatie resulteren in een korte bouwtermijn, wat een win-win situatie is voor u als klant enerzijds en voor ons als aannemer anderzijds.</p>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="feature-box feature-box-style-2 appear-animation" data-appear-animation="fadeInLeft" data-appear-animation-delay="300">
                            <div class="feature-box-icon">
                                <i class="icon-user icons"></i>
                            </div>
                            <div class="feature-box-info">
                                <h4 class="mb-sm">Eén aanspreekpunt</h4>
                                <p class="mb-lg">Wij verzorgen de coördinatie tussen u als koper(s), de architect, ingenieur, EPB verslaggever, onderaannemers en veiligheidscoördinator. Wij begeleiden u bij het maken van de keuzes in de diverse toonzalen.</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    </section>

@section LatestNews
    <h4>Recente <strong>berichten</strong></h4>

    <ul class="nav nav-list mb-xl">
        @For Each news In ViewData("LatestNews")
            @<text>
                <li><a title="@news.news.TitleNL" href="@Url.Action("News", "Projects", New With {.slug = news.projectslug})">@news.news.TitleNL</a></li>
            </text>

        Next

    </ul>
End Section
@section scripts
    <script>
        $(document).ready(function () {
            //alert(this.location.pathname);
            if (this.location.pathname == '/Home/Index') {
                $('a[href="/"]').parent().addClass('active');
            };
            $('a[href="' + this.location.pathname + '"]').parent().addClass('active');
        });

    </script>
End section
   