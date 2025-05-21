@modeltype WWWCOPRO.NewsletterModel 
<h4>Nieuwsbrief</h4>
<p>Wilt u op de hoogte blijven van onze nieuwe projecten, de voortgang van onze projecten in uitvoering of van relevant nieuws uit de bouwsector, schrijf u dan in op onze nieuwsbrief.</p>

@Using Html.BeginForm("Index", "Newsletter", FormMethod.Post, New With {.id = "FormNewsletter", .class = "form-horizontal"})
    @<text>
        <div class="input-group">
            @Html.TextBoxFor(Function(m) m.EmailTo, New With {.class = "form-control",.placeholder="Email adres", .name="EmailTo",.id="EmailTo"})
            @*<input class="form-control" placeholder="Email adres" name="newsletterEmail" id="newsletterEmail" type="text">*@
            <span class="input-group-btn">
                <button class="btn btn-default" type="submit">Stuur!</button>
            </span>
        </div>
    </text>
End Using
 
