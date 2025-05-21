@modeltype List(Of BO.ActivityGroupBO)
<ul id="ActivitiesTreeview">
    @For Each Group As BO.ActivityGroupBO In Model
        @<text>
            <li data-jstree='{ "id" : @Group.ID }'>
                Deel @(Group.Lot) - @(Group.Name)
                <ul>

                    @For Each activity As BO.ActivityBO In Group.Activities
                        @<text>
                            <li data-jstree='{ "id" : @activity.ID }' id="@activity.ID">
                                @activity.Name
                            </li>
                        </text>
                    Next
                </ul>
            </li>

        </text>
    Next

</ul>
