@modeltype Integer
<li>
    <a href="@Url.Action("Detail", "Projecten", New With {.projectid = Model})" class="menu-item"> Project</a>

</li>
<li>
    <a href="@Url.Action("Recalculation", "Projecten", New With {.projectid = Model})" class="menu-item">Nacalculatie</a>
</li>
<li>
    <a href="@Url.Action("DetailContracts", "Projecten", New With {.projectid = Model})" class="menu-item">Contracten</a>
</li>
<li>
    <a href="@Url.Action("ProjectVacationDays", "Projecten", New With {.projectid = Model})" class="menu-item">Verlofdagen project</a>
</li>
<li>
    <a href="@Url.Action("DetailPhotos", "Projecten", New With {.projectid = Model})" class="menu-item"> Foto's</a>
</li>
<li>
    <a href="@Url.Action("DetailNews", "Projecten", New With {.projectid = Model})" class="menu-item">Nieuws</a>
</li>
<li>
    <a href="@Url.Action("DetailClients", "Projecten", New With {.projectid = Model})" class="menu-item">Klanten</a>
</li>
<li>
    <a href="@Url.Action("DetailUnits", "Projecten", New With {.projectid = Model})" class="menu-item">Eenheden</a>
</li>
<li>
    <a href="@Url.Action("Sales", "Projecten", New With {.projectid = Model})" class="menu-item">Verkoop</a>
</li>
<li>
    <a href="@Url.Action("Invoicing", "Projecten", New With {.projectid = Model})" class="menu-item">Facturatie</a>
</li>
<li>
    <a href="@Url.Action("DetailDocs", "Projecten", New With {.projectid = Model})" class="menu-item">Documenten</a>
</li>
<li>
    <a href="@Url.Action("DetailInsurances", "Projecten", New With {.projectid = Model})" class="menu-item">Vezekeringen</a>
</li>

