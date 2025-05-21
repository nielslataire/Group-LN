Imports BO

Public Class UnitOfWork
    Implements IDisposable

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects).
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
            ' TODO: set large fields to null.
        End If
        Me.disposedValue = True
    End Sub

    ' TODO: override Finalize() only if Dispose(ByVal disposing As Boolean) above has code to free unmanaged resources.
    'Protected Overrides Sub Finalize()
    '    ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
#End Region

    Private _context As testdbEntities

    ''' <summary>
    ''' Detectchanges = true
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New()
        _context = New testdbEntities(True)
    End Sub

    Public Sub New(detectChanges As Boolean)
        _context = New testdbEntities(detectChanges)
    End Sub

    Public Sub New(detectChanges As Boolean, connString As String)
        _context = New testdbEntities(detectChanges, connString)
    End Sub

    Public Function SaveChanges() As List(Of Message)
        Dim messages As New List(Of Message)
        Try
            _context.SaveChanges()

        Catch ex As Exception
            Dim m As New Message
            m.Type = MessageType.Error
            m.Message = ex.Message
            messages.Add(m)
        End Try
        Return messages
    End Function
    Public Function DetachEntity(entity As Object) As List(Of Message)
        Dim messages As New List(Of Message)
        Try
            _context.Entry(entity).State = Data.Entity.EntityState.Detached

        Catch ex As Exception
            Dim m As New Message
            m.Type = MessageType.Error
            m.Message = ex.Message
            messages.Add(m)
        End Try
        Return messages
    End Function
    Private _CompanyContactsDAO As GenericDAO(Of CompanyContacts)
    Public Function GetCompanyContactsDAO() As GenericDAO(Of CompanyContacts)
        If (_CompanyContactsDAO Is Nothing) Then
            _CompanyContactsDAO = New GenericDAO(Of CompanyContacts)
            _CompanyContactsDAO.Context = _context
        End If
        Return _CompanyContactsDAO
    End Function
    Private _CompanyInfoDAO As GenericDAO(Of CompanyInfo)
    Public Function GetCompanyInfoDAO() As GenericDAO(Of CompanyInfo)
        If (_CompanyInfoDAO Is Nothing) Then
            _CompanyInfoDAO = New GenericDAO(Of CompanyInfo)
            _CompanyInfoDAO.Context = _context
        End If
        Return _CompanyInfoDAO
    End Function
    Private _DepartmentDAO As GenericDAO(Of CompanyDepartments)
    Public Function GetDepartmentDAO() As GenericDAO(Of CompanyDepartments)
        If (_DepartmentDAO Is Nothing) Then
            _DepartmentDAO = New GenericDAO(Of CompanyDepartments)
            _DepartmentDAO.Context = _context
        End If
        Return _DepartmentDAO
    End Function
    Private _UserDAO As GenericDAO(Of Users)
    Public Function GetUsersDAO() As GenericDAO(Of Users)
        If (_UserDAO Is Nothing) Then
            _UserDAO = New GenericDAO(Of Users)
            _UserDAO.Context = _context
        End If
        Return _UserDAO
    End Function

    Private _ActivityDAO As GenericDAO(Of Activity)
    Public Function GetActivityDAO() As GenericDAO(Of Activity)
        If (_ActivityDAO Is Nothing) Then
            _ActivityDAO = New GenericDAO(Of Activity)
            _ActivityDAO.Context = _context
        End If
        Return _ActivityDAO
    End Function
    Private _ActivityGroupDAO As GenericDAO(Of ActivityGroup)
    Public Function GetActivityGroupDAO() As GenericDAO(Of ActivityGroup)
        If (_ActivityGroupDAO Is Nothing) Then
            _ActivityGroupDAO = New GenericDAO(Of ActivityGroup)
            _ActivityGroupDAO.Context = _context
        End If
        Return _ActivityGroupDAO
    End Function
    '--------POSTALCODE--------------
    '--------------------------------
    Private _ProvinceDAO As GenericDAO(Of Provincie)
    Public Function GetProvinceDAO() As GenericDAO(Of Provincie)
        If (_ProvinceDAO Is Nothing) Then
            _ProvinceDAO = New GenericDAO(Of Provincie)
            _ProvinceDAO.Context = _context
        End If
        Return _ProvinceDAO
    End Function
    Private _CountryDAO As GenericDAO(Of Country)
    Public Function GetCountryDAO() As GenericDAO(Of Country)
        If (_CountryDAO Is Nothing) Then
            _CountryDAO = New GenericDAO(Of Country)
            _CountryDAO.Context = _context
        End If
        Return _CountryDAO
    End Function
    Private _PostalcodeDAO As GenericDAO(Of PostalCode)
    Public Function GetPostalcodeDAO() As GenericDAO(Of PostalCode)
        If (_PostalcodeDAO Is Nothing) Then
            _PostalcodeDAO = New GenericDAO(Of PostalCode)
            _PostalcodeDAO.Context = _context
        End If
        Return _PostalcodeDAO
    End Function
    '----------PROJECTS---------------
    '---------------------------------
    Private _ProjectDAO As GenericDAO(Of Project)
    Public Function GetProjectDAO() As GenericDAO(Of Project)
        If (_ProjectDAO Is Nothing) Then
            _ProjectDAO = New GenericDAO(Of Project)
            _ProjectDAO.Context = _context
        End If
        Return _ProjectDAO
    End Function
    Private _ProjectStatusDAO As GenericDAO(Of ProjectStatus)
    Public Function GetProjectStatusDAO() As GenericDAO(Of ProjectStatus)
        If (_ProjectStatusDAO Is Nothing) Then
            _ProjectStatusDAO = New GenericDAO(Of ProjectStatus)
            _ProjectStatusDAO.Context = _context
        End If
        Return _ProjectStatusDAO
    End Function
    Private _ProjectPicturesDAO As GenericDAO(Of ProjectPictures)
    Public Function GetProjectPicturesDAO() As GenericDAO(Of ProjectPictures)
        If (_ProjectPicturesDAO Is Nothing) Then
            _ProjectPicturesDAO = New GenericDAO(Of ProjectPictures)
            _ProjectPicturesDAO.Context = _context
        End If
        Return _ProjectPicturesDAO
    End Function
    Private _ProjectNewsDAO As GenericDAO(Of ProjectNews)
    Public Function GetProjectNewsDAO() As GenericDAO(Of ProjectNews)
        If (_ProjectNewsDAO Is Nothing) Then
            _ProjectNewsDAO = New GenericDAO(Of ProjectNews)
            _ProjectNewsDAO.Context = _context
        End If
        Return _ProjectNewsDAO
    End Function
    Private _ProjectSalesSettingsDAO As GenericDAO(Of ProjectSalesSettings)
    Public Function GetProjectSalesSettingsDAO() As GenericDAO(Of ProjectSalesSettings)
        If (_ProjectSalesSettingsDAO Is Nothing) Then
            _ProjectSalesSettingsDAO = New GenericDAO(Of ProjectSalesSettings)
            _ProjectSalesSettingsDAO.Context = _context
        End If
        Return _ProjectSalesSettingsDAO
    End Function
    Private _WheaterstationsDAO As GenericDAO(Of WheaterStations)
    Public Function GetWheaterstationsDAO() As GenericDAO(Of WheaterStations)
        If (_WheaterstationsDAO Is Nothing) Then
            _WheaterstationsDAO = New GenericDAO(Of WheaterStations)
            _WheaterstationsDAO.Context = _context
        End If
        Return _WheaterstationsDAO
    End Function
    Private _BadWeatherDaysDAO As GenericDAO(Of BadWeatherDays)
    Public Function GetBadWeatherDaysDAO() As GenericDAO(Of BadWeatherDays)
        If (_BadWeatherDaysDAO Is Nothing) Then
            _BadWeatherDaysDAO = New GenericDAO(Of BadWeatherDays)
            _BadWeatherDaysDAO.Context = _context
        End If
        Return _BadWeatherDaysDAO
    End Function
    Private _VacationDaysDAO As GenericDAO(Of VacationDays)
    Public Function GetVacationDaysDAO() As GenericDAO(Of VacationDays)
        If (_VacationDaysDAO Is Nothing) Then
            _VacationDaysDAO = New GenericDAO(Of VacationDays)
            _VacationDaysDAO.Context = _context
        End If
        Return _VacationDaysDAO
    End Function
    Private _ProjectLevelsDAO As GenericDAO(Of ProjectLevels)
    Public Function GetProjectLevelsDAO() As GenericDAO(Of ProjectLevels)
        If (_ProjectLevelsDAO Is Nothing) Then
            _ProjectLevelsDAO = New GenericDAO(Of ProjectLevels)
            _ProjectLevelsDAO.Context = _context
        End If
        Return _ProjectLevelsDAO
    End Function
    Private _ProjectDocsDAO As GenericDAO(Of Projectdocs)
    Public Function GetProjectDocsDAO() As GenericDAO(Of ProjectDocs)
        If (_ProjectDocsDAO Is Nothing) Then
            _ProjectDocsDAO = New GenericDAO(Of ProjectDocs)
            _ProjectDocsDAO.Context = _context
        End If
        Return _ProjectDocsDAO
    End Function
    Private _UtilityPercentageDAO As GenericDAO(Of UtilityPercentage)
    Public Function GetUtilityPercentageDAO() As GenericDAO(Of UtilityPercentage)
        If (_UtilityPercentageDAO Is Nothing) Then
            _UtilityPercentageDAO = New GenericDAO(Of UtilityPercentage)
            _UtilityPercentageDAO.Context = _context
        End If
        Return _UtilityPercentageDAO
    End Function

    '-------------INCOMMING INVOICES-----------
    '------------------------------------------

    Private _IncommingInvoices As GenericDAO(Of IncommingInvoices)
    Public Function GetIncommingInvoicesDAO() As GenericDAO(Of IncommingInvoices)
        If (_IncommingInvoices Is Nothing) Then
            _IncommingInvoices = New GenericDAO(Of IncommingInvoices)
            _IncommingInvoices.Context = _context
        End If
        Return _IncommingInvoices
    End Function
    Private _IncommingInvoiceDetail As GenericDAO(Of IncommingInvoiceDetail)
    Public Function GetIncommingInvoicesDetailDAO() As GenericDAO(Of IncommingInvoiceDetail)
        If (_IncommingInvoiceDetail Is Nothing) Then
            _IncommingInvoiceDetail = New GenericDAO(Of IncommingInvoiceDetail)
            _IncommingInvoiceDetail.Context = _context
        End If
        Return _IncommingInvoiceDetail
    End Function

    '-------------INVOICING-----------
    '---------------------------------
    Private _Invoices As GenericDAO(Of Invoices)
    Public Function GetInvoicesDAO() As GenericDAO(Of Invoices)
        If (_Invoices Is Nothing) Then
            _Invoices = New GenericDAO(Of Invoices)
            _Invoices.Context = _context
        End If
        Return _Invoices
    End Function
    Private _InvoicesDetails As GenericDAO(Of InvoicesDetails)
    Public Function GetInvoicesDetailsDAO() As GenericDAO(Of InvoicesDetails)
        If (_InvoicesDetails Is Nothing) Then
            _InvoicesDetails = New GenericDAO(Of InvoicesDetails)
            _InvoicesDetails.Context = _context
        End If
        Return _InvoicesDetails
    End Function
    Private _ProjectPaymentGroups As GenericDAO(Of InvoicingPaymentGroup)
    Public Function GetProjectPaymentGroupsDAO() As GenericDAO(Of InvoicingPaymentGroup)
        If (_ProjectPaymentGroups Is Nothing) Then
            _ProjectPaymentGroups = New GenericDAO(Of InvoicingPaymentGroup)
            _ProjectPaymentGroups.Context = _context
        End If
        Return _ProjectPaymentGroups
    End Function
    Private _ProjectPaymentStages As GenericDAO(Of InvoicingPaymentStages)
    Public Function GetProjectPaymentStagesDAO() As GenericDAO(Of InvoicingPaymentStages)
        If (_ProjectPaymentStages Is Nothing) Then
            _ProjectPaymentStages = New GenericDAO(Of InvoicingPaymentStages)
            _ProjectPaymentStages.Context = _context
        End If
        Return _ProjectPaymentStages
    End Function

    '------------UNITS---------------
    '--------------------------------
    Private _UnitsDAO As GenericDAO(Of Units)
    Public Function GetUnitsDAO() As GenericDAO(Of Units)
        If (_UnitsDAO Is Nothing) Then
            _UnitsDAO = New GenericDAO(Of Units)
            _UnitsDAO.Context = _context
        End If
        Return _UnitsDAO
    End Function
    Private _UnitRoomsDAO As GenericDAO(Of UnitRooms)
    Public Function GetUnitRoomsDAO() As GenericDAO(Of UnitRooms)
        If (_UnitRoomsDAO Is Nothing) Then
            _UnitRoomsDAO = New GenericDAO(Of UnitRooms)
            _UnitRoomsDAO.Context = _context
        End If
        Return _UnitRoomsDAO
    End Function
    Private _UnitTypesDAO As GenericDAO(Of UnitTypes)
    Public Function GetUnitTypesDAO() As GenericDAO(Of UnitTypes)
        If (_UnitTypesDAO Is Nothing) Then
            _UnitTypesDAO = New GenericDAO(Of UnitTypes)
            _UnitTypesDAO.Context = _context
        End If
        Return _UnitTypesDAO
    End Function
    Private _UnitGroupTypesDAO As GenericDAO(Of UnitGroupTypes)
    Public Function GetUnitGroupTypesDAO() As GenericDAO(Of UnitGroupTypes)
        If (_UnitGroupTypesDAO Is Nothing) Then
            _UnitGroupTypesDAO = New GenericDAO(Of UnitGroupTypes)
            _UnitGroupTypesDAO.Context = _context
        End If
        Return _UnitGroupTypesDAO
    End Function
    Private _UnitConstructionValuesDAO As GenericDAO(Of UnitConstructionValue)
    Public Function GetUnitConstructionValuesDAO() As GenericDAO(Of UnitConstructionValue)
        If (_UnitConstructionValuesDAO Is Nothing) Then
            _UnitConstructionValuesDAO = New GenericDAO(Of UnitConstructionValue)
            _UnitConstructionValuesDAO.Context = _context
        End If
        Return _UnitConstructionValuesDAO
    End Function
    '------------CLIENTS--------------
    '---------------------------------
    Private _ClientAccountDAO As GenericDAO(Of ClientAccount)
    Public Function GetClientAccountDAO() As GenericDAO(Of ClientAccount)
        If (_ClientAccountDAO Is Nothing) Then
            _ClientAccountDAO = New GenericDAO(Of ClientAccount)
            _ClientAccountDAO.Context = _context
        End If
        Return _ClientAccountDAO
    End Function
    Private _ClientContactsDAO As GenericDAO(Of ClientContacts)
    Public Function GetClientContactsDAO() As GenericDAO(Of ClientContacts)
        If (_ClientContactsDAO Is Nothing) Then
            _ClientContactsDAO = New GenericDAO(Of ClientContacts)
            _ClientContactsDAO.Context = _context
        End If
        Return _ClientContactsDAO
    End Function
    Private _ClientOwnerTypeDAO As GenericDAO(Of ClientOwnerType)
    Public Function GetClientOwnerTypeDAO() As GenericDAO(Of ClientOwnerType)
        If (_ClientOwnerTypeDAO Is Nothing) Then
            _ClientOwnerTypeDAO = New GenericDAO(Of ClientOwnerType)
            _ClientOwnerTypeDAO.Context = _context
        End If
        Return _ClientOwnerTypeDAO
    End Function
    Private _ClientGiftDAO As GenericDAO(Of ClientGift)
    Public Function GetClientGiftDAO() As GenericDAO(Of ClientGift)
        If (_ClientGiftDAO Is Nothing) Then
            _ClientGiftDAO = New GenericDAO(Of ClientGift)
            _ClientGiftDAO.Context = _context
        End If
        Return _ClientGiftDAO
    End Function
    Private _ClientPoaDAO As GenericDAO(Of ClientPOA)
    Public Function GetClientPoaDAO() As GenericDAO(Of ClientPOA)
        If (_ClientPoaDAO Is Nothing) Then
            _ClientPoaDAO = New GenericDAO(Of ClientPOA)
            _ClientPoaDAO.Context = _context
        End If
        Return _ClientPoaDAO
    End Function
    '------------CONTRACTS------------
    '---------------------------------
    Private _ContractDAO As GenericDAO(Of Contract)
    Public Function GetContractDAO() As GenericDAO(Of Contract)
        If (_ContractDAO Is Nothing) Then
            _ContractDAO = New GenericDAO(Of Contract)
            _ContractDAO.Context = _context
        End If
        Return _ContractDAO
    End Function
    Private _ContractActivityDAO As GenericDAO(Of ContractActivity)
    Public Function GetContractActivityDAO() As GenericDAO(Of ContractActivity)
        If (_ContractActivityDAO Is Nothing) Then
            _ContractActivityDAO = New GenericDAO(Of ContractActivity)
            _ContractActivityDAO.Context = _context
        End If
        Return _ContractActivityDAO
    End Function
    '------------BUDGET---------------
    '---------------------------------
    Private _BudgetDAO As GenericDAO(Of ProjectBudget)
    Public Function GetBudgetDAO() As GenericDAO(Of ProjectBudget)
        If (_BudgetDAO Is Nothing) Then
            _BudgetDAO = New GenericDAO(Of ProjectBudget)
            _BudgetDAO.Context = _context
        End If
        Return _BudgetDAO
    End Function
    '------------CHANGEORDER----------
    '---------------------------------
    Private _ChangeOrderDAO As GenericDAO(Of ChangeOrder)
    Public Function GetChangeOrderDAO() As GenericDAO(Of ChangeOrder)
        If (_ChangeOrderDAO Is Nothing) Then
            _ChangeOrderDAO = New GenericDAO(Of ChangeOrder)
            _ChangeOrderDAO.Context = _context
        End If
        Return _ChangeOrderDAO
    End Function
    Private _ChangeOrderDetailDAO As GenericDAO(Of ChangeOrderDetail)
    Public Function GetChangeOrderDetailDAO() As GenericDAO(Of ChangeOrderDetail)
        If (_ChangeOrderDetailDAO Is Nothing) Then
            _ChangeOrderDetailDAO = New GenericDAO(Of ChangeOrderDetail)
            _ChangeOrderDetailDAO.Context = _context
        End If
        Return _ChangeOrderDetailDAO
    End Function

    '------------INSURANCE----------
    '---------------------------------
    Private _InsuranceDAO As GenericDAO(Of Insurances)
    Public Function GetInsuranceDAO() As GenericDAO(Of Insurances)
        If (_InsuranceDAO Is Nothing) Then
            _InsuranceDAO = New GenericDAO(Of Insurances)
            _InsuranceDAO.Context = _context
        End If
        Return _InsuranceDAO
    End Function
    Private _InsuranceCompaniesDAO As GenericDAO(Of InsuranceCompanies)
    Public Function GetInsuranceCompaniesDAO() As GenericDAO(Of InsuranceCompanies)
        If (_InsuranceCompaniesDAO Is Nothing) Then
            _InsuranceCompaniesDAO = New GenericDAO(Of InsuranceCompanies)
            _InsuranceCompaniesDAO.Context = _context
        End If
        Return _InsuranceCompaniesDAO
    End Function



    '------------LOG------------------
    '---------------------------------
    Private _LogDAO As GenericDAO(Of Loglist)
    Public Function GetLogDAO() As GenericDAO(Of LogList)
        If (_LogDAO Is Nothing) Then
            _LogDAO = New GenericDAO(Of LogList)
            _LogDAO.Context = _context
        End If
        Return _LogDAO
    End Function
End Class
