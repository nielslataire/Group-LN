<nav class="nav-main mega-menu">
    <ul class="nav nav-pills nav-main" id="mainMenu">
        <li class="dropdown active">
            @Html.ActionLink("Home", "Index", "Home", Nothing, New With {Key .[class] = "dropdown-toggle"})
        </li>
    
        <li class="dropdown">
            @Html.ActionLink("Leveranciers", "Index", "Leveranciers", Nothing, New With {Key .[class] = "dropdown-toggle"})

            <ul class="dropdown-menu">
                <li>
                    <a href="#">Zoeken</a>

                </li>
                <li>
                    <a href="#">Lijst</a>

                </li>
                <li>
                    <a href="#">Toevoegen</a>
                </li>

            </ul>
        </li>
        <li class="dropdown">
            @Html.ActionLink("Klanten", "Index", "Klanten", Nothing, New With {Key .[class] = "dropdown-toggle"})


            <ul class="dropdown-menu">
                <li >
                    <a href="#">Zoeken</a>
                    
                </li>
                <li >
                    <a href="#">Lijst</a>
                    
                </li>
                <li>
                    <a href="#">Toevoegen</a>
                </li>
               
            </ul>
        </li>
        <li class="dropdown">
            @Html.ActionLink("Projecten", "Index", "Projecten", Nothing, New With {Key .[class] = "dropdown-toggle"})


            <ul class="dropdown-menu">
                <li>
                    <a href="#">Actief</a>

                </li>
                <li>
                    <a href="#">Opgeleverd</a>

                </li>
                <li>
                    <a href="#">Toevoegen</a>
                </li>

            </ul>
        </li>
        <li class="dropdown">
            @Html.ActionLink("Facturatie", "Index", "Facturatie", Nothing, New With {Key .[class] = "dropdown-toggle"})


            <ul class="dropdown-menu">
                <li>
                    <a href="#">Factuur opmaken</a>

                </li>
                <li>
                    <a href="#">Raadplegen</a>

                </li>
                <li>
                    <a href="#">Zoeken</a>
                </li>
                <li>
                    <a href="#">Openstaande</a>
                </li>

            </ul>
        </li>
        <li class="dropdown">
            @Html.ActionLink("Websites", "Index", "Websites", Nothing, New With {Key .[class] = "dropdown-toggle"})
            <ul class="dropdown-menu">
                <li>@Html.ActionLink("BCO", "Bco", "Websites")</li>
                <li>@Html.ActionLink("Copro", "Copro", "Websites")</li>
                <li>@Html.ActionLink("Verspurten", "Verspurten", "Websites")</li>
            </ul>
        </li>
        <li class="dropdown active">
            @Html.ActionLink("Admin", "Index", "Manage", Nothing, New With {Key .[class] = "dropdown-toggle"})
        </li>
    </ul>
</nav>
