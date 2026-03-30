Public Class ProjectVoortgangBO

    Private _id As Integer
    Public Property Id() As Integer
        Get
            Return _id
        End Get
        Set(ByVal value As Integer)
            _id = value
        End Set
    End Property

    Private _projectId As Integer
    Public Property ProjectId() As Integer
        Get
            Return _projectId
        End Get
        Set(ByVal value As Integer)
            _projectId = value
        End Set
    End Property

    Private _fysiekeVoortgangPct As Decimal
    Public Property FysiekeVoortgangPct() As Decimal
        Get
            Return _fysiekeVoortgangPct
        End Get
        Set(ByVal value As Decimal)
            _fysiekeVoortgangPct = value
        End Set
    End Property

    Private _financieleVoortgangPct As Decimal
    Public Property FinancieleVoortgangPct() As Decimal
        Get
            Return _financieleVoortgangPct
        End Get
        Set(ByVal value As Decimal)
            _financieleVoortgangPct = value
        End Set
    End Property

    Private _contractueleVolwassenheidPct As Decimal
    Public Property ContractueleVolwassenheidPct() As Decimal
        Get
            Return _contractueleVolwassenheidPct
        End Get
        Set(ByVal value As Decimal)
            _contractueleVolwassenheidPct = value
        End Set
    End Property

    Private _fase As VoortgangFase
    Public Property Fase() As VoortgangFase
        Get
            Return _fase
        End Get
        Set(ByVal value As VoortgangFase)
            _fase = value
        End Set
    End Property

    ''' <summary>Pipe-separated warning codes, e.g. "OVERFACTURATIE|ONDERFACTURATIE"</summary>
    Private _warnings As String
    Public Property Warnings() As String
        Get
            Return _warnings
        End Get
        Set(ByVal value As String)
            _warnings = value
        End Set
    End Property

    Private _berekendOp As DateTime
    Public Property BerekendOp() As DateTime
        Get
            Return _berekendOp
        End Get
        Set(ByVal value As DateTime)
            _berekendOp = value
        End Set
    End Property

    Private _manueelAfgesloten As Boolean
    Public Property ManueelAfgesloten() As Boolean
        Get
            Return _manueelAfgesloten
        End Get
        Set(ByVal value As Boolean)
            _manueelAfgesloten = value
        End Set
    End Property

    ''' <summary>Computed from Warnings string — list of warning codes.</summary>
    Public ReadOnly Property WarningList() As List(Of String)
        Get
            If String.IsNullOrWhiteSpace(_warnings) Then Return New List(Of String)()
            Return New List(Of String)(_warnings.Split("|"c))
        End Get
    End Property

End Class
