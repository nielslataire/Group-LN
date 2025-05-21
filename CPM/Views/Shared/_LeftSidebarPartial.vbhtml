<aside id="sidebar-left" class="sidebar-left">

    <div class="sidebar-header">
        <div class="sidebar-title">
            Navigatie
        </div>
        <div class="sidebar-toggle hidden-xs" data-toggle-class="sidebar-left-collapsed" data-target="html" data-fire-event="sidebar-left-toggle">
            <i class="fa fa-bars" aria-label="Toggle sidebar"></i>
        </div>
    </div>

    <div class="nano">
        <div class="nano-content">
            <nav id="menu" class="nav-main" role="navigation">
                <ul class="nav nav-main">
                    <li class="nav-active">
                        <a href="@Url.Action("Index", "Home")">
                            <i class="fa fa-home" aria-hidden="true"></i>
                            <span>Home</span>
                        </a>
                    </li>

                    <li class="nav-parent">
                        <a>
                            <i class="fa fa-truck" aria-hidden="true"></i>
                            <span>Leveranciers</span>
                        </a>
                        <ul class="nav nav-children">
                            <li>
                                @Html.ActionLink("Zoeken", "Zoeken", "Leveranciers")    
                            </li>
                            <li style="display:none">
                                @Html.ActionLink("Overzicht", "Lijsten", "Leveranciers") 
                            </li>
                            <li>
                                @Html.ActionLink("Toevoegen", "Toevoegen", "Leveranciers") 
                            </li>
                        </ul>
                    </li>
                    <li class="nav-parent" style="display:none">
                        <a>
                            <i class="fa fa-user" aria-hidden="true"></i>
                            <span>Klanten                 </span>
                        </a>
                        <ul class="nav nav-children">
                            <li>
                                <a href="ui-elements-typography.html">
                                    Zoeken
                                </a>
                            </li>
                            <li class="nav-parent">
                                <a>
                                    Lijsten
                                </a>
                            </li>
                            <li>
                                <a href="ui-elements-tabs.html">
                                    Toevoegen
                                </a>
                            </li>

                        </ul>
                    </li>
                    <li class="nav-parent">
                        <a>
                            <i class="fa fa-building" aria-hidden="true"></i>
                            <span>Projecten</span>
                        </a>
                        <ul class="nav nav-children">
                            <li>

                                @Html.ActionLink("Overzicht", "Index", "Projecten")

                            </li>
                            <li>
                                <a href="@(Url.Action("ProjectsByUserId", "Projecten", New With {.UserId = ViewData("UserId").ToString()}))">
                                    Eigen Projecten
                                    </a>
                                    @*@Html.ActionLink("Eigen projecten", "ProjectsByUserId", "Projecten", New With {.UserId = ViewData("UserId").ToString()})*@

                            </li>
                            <li>

                                @Html.ActionLink("Toevoegen", "Toevoegen", "Projecten")


                            </li>
                            <li>

                                @Html.ActionLink("Weerverlet", "Weather", "Projecten")


                            </li>

                        </ul>
                    </li>
                    <li class="nav-parent" style="display:none">
                        <a>
                            <i class="fa fa-link " aria-hidden="true"></i>
                            <span>Websites</span>
                        </a>
                        <ul class="nav nav-children">
                            <li>
                                <a href="ui-elements-typography.html">
                                    Copro
                                </a>
                            </li>
                            <li class="nav-parent">
                                <a>
                                    Bco
                                </a>
                            </li>
                            <li>
                                <a href="ui-elements-tabs.html">
                                    Verspurten
                                </a>
                            </li>

                        </ul>
                    </li>
                    @If User.IsInRole("Boekhouding") Then
                        @<text>
                    <li class="nav-parent" >
                        <a>
                            <i class="fa fa-file-text-o" aria-hidden="true"></i>
                            <span>Facturatie</span>
                        </a>
                        <ul class="nav nav-children">
                            <li>
                                @Html.ActionLink("BCO", "Index", "Facturatie", New With {.company = 1}, "")
                            </li>
                            <li>
                                @Html.ActionLink("Group LN", "Index", "Facturatie", New With {.company = 2}, "")
                            </li>
                        </ul>
                    </li>
                        </text>
                    End If
                    <li class="nav-parent">
                        <a>
                            <i class="fa fa-cog" aria-hidden="true"></i>
                            <span>Admin</span>
                        </a>
                        <ul class="nav nav-children">
                            <li style="display:none">
                                <a href="ui-elements-typography.html">
                                    Gebruikers
                                </a>
                            </li>
                           @If User.IsInRole("Admin") Then
                            @<text>
                                <li>
                                    @Html.ActionLink("Instellingen", "Settings", "Admin")
                                </li>
                            </text>
                           End If
                            <li>
                                @Html.ActionLink("Activiteiten beheren", "Activiteiten", "Admin")       
                            </li>
                            @If User.IsInRole("Admin") Then
                                @<text>
                                    <li>
                                        @Html.ActionLink("Gebruikers beheren", "Index", "Useradmin")
                                    </li>
                                </text>
                            End If
                            @If User.IsInRole("Admin") Then
                                @<text>
                                    <li>
                                        @Html.ActionLink("Verlofdagen - weerverlet", "VacationDays", "Admin")
                                    </li>
                                </text>
                            End If
                        </ul>
                    </li>
                    <li class="nav-parent">
                        <a>
                            <i class="fa fa-external-link" aria-hidden="true"></i>
                            <span>Links</span>
                        </a>
                        <ul class="nav nav-children">
                            <li>
                                <a href="http://www.copro-bouwteam.be" target="_blank">
                                    www.copro-bouwteam.be
                                </a>
                            </li>
                            <li>
                                <a href="http://www.bouwenconstructie.be" target="_blank">
                                    www.bouwenconstructie.be
                                </a>
                            </li>
                            <li>
                                <a href="http://www.verspurten.be" target="_blank">
                                    
                                    www.verspurten.be
                                </a>
                            </li>
                            <li>
                                <a href="http://www.home-estate.be" target="_blank">
                                  
                                    www.home-estate.be
                                </a>
                            </li>
                        </ul>
                    </li>
                </ul>
</nav>

        </div>

    </div>

</aside>
