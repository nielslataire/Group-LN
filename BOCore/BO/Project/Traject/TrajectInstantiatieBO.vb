Imports System.ComponentModel.DataAnnotations

''' <summary>Verzoek om een projecttraject te instantiëren uit een sjabloon.</summary>
Public Class TrajectInstantiatieBO

    <Required>
    Public Property ProjectId As Integer

    ''' <summary>Sjabloon om te gebruiken. Leeg = automatisch kiezen op basis van projecttype.</summary>
    Public Property TrajectSjabloonId As Integer?

    ''' <summary>Optionele afwijkende naam voor het traject.</summary>
    <StringLength(200)>
    Public Property Naam As String

    ''' <summary>Ankerdatum (bv. datum aankoopcompromis) van waaruit streefdata berekend worden.</summary>
    <DataType(DataType.Date)>
    Public Property Startdatum As DateOnly?

End Class
