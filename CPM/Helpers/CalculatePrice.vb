Imports BO
Public Class CalculatePrice
    'Notariskosten berekenen
    Public Shared Function CalculateNotaryFees(Units As List(Of UnitWithReductionBO), mixedvatregistration As Boolean) As Decimal
        Dim TotalValue As New Decimal
        Dim TotalLandValue As Decimal = CalculateTotalLandValue(Units)
        Dim TotalConstructionValue As Decimal = CalculateTotalConstructionValue(Units)

        If mixedvatregistration = True Then
            TotalValue = TotalLandValue + TotalConstructionValue
        Else
            TotalValue = TotalLandValue + (TotalConstructionValue / 2)
        End If
        'Schijven
        Dim Part1 As Decimal = 7500
        Dim Part2 As Decimal = 10000
        Dim Part3 As Decimal = 12500
        Dim Part4 As Decimal = 15495
        Dim Part5 As Decimal = 18600
        Dim Part6 As Decimal = 186000
        'Percentage schijven
        Dim Perc1 As Decimal = 4.56
        Dim Perc2 As Decimal = 2.85
        Dim Perc3 As Decimal = 2.28
        Dim Perc4 As Decimal = 1.71
        Dim Perc5 As Decimal = 1.14
        Dim Perc6 As Decimal = 0.57
        Dim Perc7 As Decimal = 0.057
        Dim RestValue As New Decimal
        Dim NotaryFees As New Decimal
        If TotalValue > Part1 Then
            NotaryFees = Part1 / 100 * Perc1
            RestValue = TotalValue - Part1
            If RestValue > Part2 Then
                NotaryFees = NotaryFees + (Part2 / 100 * Perc2)
                RestValue = RestValue - Part2
                If RestValue > Part3 Then
                    NotaryFees = NotaryFees + (Part3 / 100 * Perc3)
                    RestValue = RestValue - Part3
                    If RestValue > Part4 Then
                        NotaryFees = NotaryFees + (Part4 / 100 * Perc4)
                        RestValue = RestValue - Part4
                        If RestValue > Part5 Then
                            NotaryFees = NotaryFees + (Part5 / 100 * Perc5)
                            RestValue = RestValue - Part5
                            If RestValue > Part6 Then
                                NotaryFees = NotaryFees + (Part6 / 100 * Perc6)
                                RestValue = RestValue - Part6
                                NotaryFees = NotaryFees + (RestValue / 100 * Perc7)
                            Else
                                NotaryFees = NotaryFees + RestValue / 100 * Perc6
                            End If
                        Else
                            NotaryFees = NotaryFees + RestValue / 100 * Perc5
                        End If
                    Else
                        NotaryFees = NotaryFees + RestValue / 100 * Perc4
                    End If
                Else
                    NotaryFees = NotaryFees + RestValue / 100 * Perc3
                End If
            Else
                NotaryFees = NotaryFees + RestValue / 100 * Perc2
            End If
        Else
            NotaryFees = TotalValue / 100 * Perc1
        End If


        Return NotaryFees
    End Function
    'Totale kostprijs berekenen
    Public Shared Function CalculateTotalPrice(Units As List(Of UnitWithReductionBO), settings As ProjectSalesSettingsBO, unitcount As Integer, Optional meandownhome As Boolean = False) As Decimal
        Dim count As Integer = 1
        If unitcount > 1 Then
            count = unitcount
        End If
        Dim totalprice As Decimal
        Dim registration As Decimal
        Dim notaryfees As Decimal
        Dim vat As Decimal
        Dim landvalue As Decimal = CalculateTotalLandValue(Units)
        Dim constructionvalue As Decimal = CalculateTotalConstructionValue(Units)
        'registratierechten
        If settings.MixedVatRegistration = False Then
            registration = CalculateTotalRegistration(landvalue, settings.RegistrationPercentage, meandownhome)
        End If
        'notariskosten 
        notaryfees = CalculateNotaryFees(Units, settings.MixedVatRegistration)
        vat = CalculateTotalVat(Units, settings)

        totalprice = landvalue + constructionvalue + registration + vat + notaryfees + (settings.ConnectionFees * count) + settings.FixedCertificateCost + (settings.BaseCertificateCost * count) + settings.MortageRegistrationCost + (settings.SurveyorCost * count)
        Return totalprice
    End Function
    Public Shared Function CalculateTotalRegistration(landvalue As Decimal, registrationpercentage As Decimal, meandownhome As Boolean) As Decimal
        If meandownhome = True Then
            Return (landvalue / 100 * registrationpercentage) - (landvalue / 100 * 4)
        Else
            Return landvalue / 100 * registrationpercentage
        End If
    End Function
    Public Shared Function CalculateConstructionPriceVat(constructionvalue As UnitConstructionValueBO) As Decimal
        Dim service = ServiceFactory.GetProjectService
        Dim response = service.GetProjectPaymentGroup(constructionvalue.PaymentGroupId)
        Dim paymentgroup As New ProjectPaymentGroupBO
        If response.Success Then paymentgroup = response.Value
        Return (constructionvalue.Value / 100 * paymentgroup.VatPercentage)
    End Function
    Public Shared Function CalculateTotalVat(Units As List(Of UnitWithReductionBO), salessettings As ProjectSalesSettingsBO) As Decimal
        Dim totalvat As New Decimal
        Dim totalconstructionvalue As New Decimal
        'BTW op bouwwaardes
        For Each unit In Units
            For Each constructionvalue In unit.Base.ConstructionValues
                totalconstructionvalue = totalconstructionvalue + constructionvalue.Value
                totalvat = totalvat + CalculateConstructionPriceVat(constructionvalue)
            Next
        Next
        'BTW op vaste kosten
        totalvat = totalvat + ((salessettings.BaseCertificateCost * Units.Where(Function(m) m.Base.Type.GroupId = 1).Count) / 100 * salessettings.VatPercentage) + (salessettings.FixedCertificateCost / 100 * salessettings.VatPercentage) + ((salessettings.ConnectionFees * Units.Where(Function(m) m.Base.Type.GroupId = 1).Count) / 100 * salessettings.VatPercentage) + ((salessettings.SurveyorCost * Units.Where(Function(m) m.Base.Type.GroupId = 1).Count) / 100 * salessettings.VatPercentage)
        'BTW op grond indien onder btw stelsel
        If salessettings.MixedVatRegistration = True Then
            totalvat = totalvat + (Units.Sum(Function(m) m.Base.LandValue) / 100 * salessettings.VatPercentage)
        End If
        'BTW op notariskosten
        totalvat = totalvat + (CalculateNotaryFees(Units, salessettings.MixedVatRegistration) / 100 * salessettings.VatPercentage)

        Return totalvat
    End Function
    Public Shared Function GetVatPercentage(paymentgroupid As Integer) As Decimal
        Dim service = ServiceFactory.GetProjectService
        Dim response = service.GetProjectPaymentGroup(paymentgroupid)
        Dim paymentgroup As New ProjectPaymentGroupBO
        If response.Success Then paymentgroup = response.Value
        Return paymentgroup.VatPercentage
    End Function
    Private Shared Function CalculateTotalLandValue(units As List(Of UnitWithReductionBO)) As Decimal
        Dim totallandvalue As New Decimal
        For Each unit In units
            totallandvalue += unit.Base.LandValue
        Next
        Return totallandvalue
    End Function
    Private Shared Function CalculateTotalConstructionValue(units As List(Of UnitWithReductionBO)) As Decimal
        Dim totalconstructionvalue As New Decimal
        For Each unit In units
            For Each cv In unit.Base.ConstructionValues
                totalconstructionvalue += cv.Value
            Next
        Next
        Return totalconstructionvalue
    End Function

End Class
