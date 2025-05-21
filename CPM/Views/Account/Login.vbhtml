@ModelType LoginViewModel
@Code
    ViewBag.Title = "Login"
    Layout = "~/Views/Shared/_Base.vbhtml"
End Code

<section class="body-sign">
    <div class="center-sign">
        <a href="/" class="logo pull-left">
            <img src="~/img/logo.png" height="54" alt="CPM" />
        </a>

        <div class="panel panel-sign">
            <div class="panel-title-sign mt-xl text-right">
                <h2 class="title text-uppercase text-weight-bold m-none"><i class="fa fa-user mr-xs"></i> Inloggen</h2>
            </div>
            <div class="panel-body">
               @Using Html.BeginForm("Login", "Account", New With {.ReturnUrl = ViewBag.ReturnUrl}, FormMethod.Post, New With {.class = "form-horizontal", .role = "form"})
                @Html.AntiForgeryToken()
                    @<text>
                    @Html.ValidationSummary(True, "", New With {.class = "text-danger"})
                    <div class="form-group mb-lg">
                        @Html.LabelFor(Function(m) m.Username)
                        <div class="input-group input-group-icon">
                            @Html.TextBoxFor(Function(m) m.Username, New With {.class = "form-control input-lg"})
                            <span class="input-group-addon">
                                <span class="icon icon-lg">
                                    <i class="fa fa-user"></i>
                                </span>
                            </span>
                        </div>
                            @Html.ValidationMessageFor(Function(m) m.Username, "", New With {.class = "text-danger"})
                    </div>

                    <div class="form-group mb-lg">
                        <div class="clearfix">
                           @Html.LabelFor(Function(m) m.Password, New With {.class = "pull-left"})
                            @Html.ActionLink("Paswoord verloren?", "ForgotPassword", Nothing, New With {.class = "pull-right"})
                            
                        </div>
                        <div class="input-group input-group-icon">
                            @Html.PasswordFor(Function(m) m.Password, New With {.class = "form-control input-lg"})
                            <span class="input-group-addon">
                                <span class="icon icon-lg">
                                    <i class="fa fa-lock"></i>
                                </span>
                            </span>
                        </div>
                        @Html.ValidationMessageFor(Function(m) m.Password, "", New With {.class = "text-danger"})
                    </div>

                    <div class="form-group mb-lg">
                        <!--<div class="col-sm-8">-->
                            @*<div class="checkbox-custom checkbox-default">
                                @Html.CheckBoxFor(Function(m) m.RememberMe)
                                @Html.LabelFor(Function(m) m.RememberMe)
                            </div>*@
                        <!--</div>
                        <div class="col-sm-4 text-right mr-none ">-->
                            <button type="submit" class="btn btn-primary btn-block hidden-xs">Inloggen</button>
                            <button type="submit" class="btn btn-primary btn-block btn-lg visible-xs mt-lg">Inloggen</button>
                        @*</div>*@
                    </div>
                </text>
               End Using
            </div>
        </div>

        <p class="text-center text-muted mt-md mb-md">&copy; Copyright @Date.Now().Year . All Rights Reserved.</p>
    </div>
</section>
@*
    <div class="row">
        <div class="col-md-12">

            <div class="featured-boxes">
                <div class="row">
                    <div class="col-sm-3"></div>
                    <div class="col-sm-6">
                        <div class="featured-box featured-box-primary align-left mt-xlg">
                            <div class="box-content">
                                @Using Html.BeginForm("Login", "Account", New With {.ReturnUrl = ViewBag.ReturnUrl}, FormMethod.Post, New With {.class = "form-horizontal", .role = "form"})
                                    @Html.AntiForgeryToken()
                                    @<text>
                                <h4 class="heading-primary text-uppercase mb-md">Meld u hier aan om verder te doen.</h4>
                                @Html.ValidationSummary(True, "", New With {.class = "text-danger"})
                                <form action="/" id="frmSignIn" method="post">
                                    <div class="row">
                                        <div class="form-group">
                                            <div class="col-md-12">
                                                @Html.LabelFor(Function(m) m.Login)
                                                @Html.TextBoxFor(Function(m) m.Login, New With {.class = "form-control input-md"})
                                                @Html.ValidationMessageFor(Function(m) m.Login, "", New With {.class = "text-danger"})
                                                
                                            </div>
                                        </div>
                                    </div>
                                    <div class="row">
                                        <div class="form-group">
                                            <div class="col-md-12">
                                                @Html.ActionLink("Paswoord verloren?", "ForgotPassword", Nothing, New With {.class = "pull-right"})
                                                <a class="pull-right" href="#">(Paswoord verloren?)</a>
                                                @Html.LabelFor(Function(m) m.Password)
                                                @Html.PasswordFor(Function(m) m.Password, New With {.class = "form-control input-md"})
                                                @Html.ValidationMessageFor(Function(m) m.Password, "", New With {.class = "text-danger"})
                                                
                                            </div>
                                        </div>
                                    </div>
                                    <div class="row">
                                        <div class="col-md-6">
                                            <span class="remember-box checkbox">
                                                @Html.CheckBoxFor(Function(m) m.RememberMe)
                                                @Html.LabelFor(Function(m) m.RememberMe)
                                               
                                            </span>
                                        </div>
                                        <div class="col-md-6">
                                            <input type="submit" value="Inloggen" class="btn btn-primary pull-right mb-xl" data-loading-text="Loading...">
                                        </div>
                                    </div>
                                </form>
                            </text>
                                End Using
                            </div>
                        </div>
                    </div>

                </div>

            </div>
        </div>
    </div>
*@

@*<div class="container">
    <div class="row">
        <div class="col-md-8">
            <section id="loginForm">
                @Using Html.BeginForm("Login", "Account", New With {.ReturnUrl = ViewBag.ReturnUrl}, FormMethod.Post, New With {.class = "form-horizontal", .role = "form"})
                    @Html.AntiForgeryToken()
                    @<text>
                        <h4>Use a local account to log in.</h4>
                        <hr />
                        @Html.ValidationSummary(True, "", New With {.class = "text-danger"})
                        <div class="form-group">
                            @Html.LabelFor(Function(m) m.Email, New With {.class = "col-md-2 control-label"})
                            <div class="col-md-10">
                                @Html.TextBoxFor(Function(m) m.Email, New With {.class = "form-control"})
                                @Html.ValidationMessageFor(Function(m) m.Email, "", New With {.class = "text-danger"})
                            </div>
                        </div>
                        <div class="form-group">
                            @Html.LabelFor(Function(m) m.Password, New With {.class = "col-md-2 control-label"})
                            <div class="col-md-10">
                                @Html.PasswordFor(Function(m) m.Password, New With {.class = "form-control"})
                                @Html.ValidationMessageFor(Function(m) m.Password, "", New With {.class = "text-danger"})
                            </div>
                        </div>
                        <div class="form-group">
                            <div class="col-md-offset-2 col-md-10">
                                <div class="checkbox">
                                    @Html.CheckBoxFor(Function(m) m.RememberMe)
                                    @Html.LabelFor(Function(m) m.RememberMe)
                                </div>
                            </div>
                        </div>
                        <div class="form-group">
                            <div class="col-md-offset-2 col-md-10">
                                <input type="submit" value="Log in" class="btn btn-default" />
                            </div>
                        </div>
                        <p>
                            @Html.ActionLink("Register as a new user", "Register")
                        </p>
                        @* Enable this once you have account confirmation enabled for password reset functionality
                            <p>
                                @Html.ActionLink("Forgot your password?", "ForgotPassword")
                            </p>
                    </text>
                End Using
            </section>
        </div>
        <div class="col-md-4">
            <section id="socialLoginForm">
                @Html.Partial("_ExternalLoginsListPartial", New ExternalLoginListViewModel With {.ReturnUrl = ViewBag.ReturnUrl})
            </section>
        </div>
    </div>
</div>*@
@Section Scripts
    @Scripts.Render("~/bundles/jqueryval")
End Section
