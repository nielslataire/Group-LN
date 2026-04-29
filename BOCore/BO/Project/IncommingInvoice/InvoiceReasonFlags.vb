<Flags>
Public Enum InvoiceReasonFlags As Long
    None = 0L
    MissingDueDate = 1L
    MissingLines = 2L
    ProjectUncertain = 4L
    ContractUncertain = 8L
    SupplierUncertain = 16L
    GeneralCostSuggested = 32L
    DuplicateSuspected = 64L
    AmountMismatch = 128L
    LinesRecoveredFromPdf = 256L
    PdfTextUnavailable = 512L
    AzureSkippedNoConfig = 1024L
    OcrUsed = 2048L
    UserCorrected = 4096L
    MultipleProjectCandidates = 8192L
    MultipleContractCandidates = 16384L
    MissingInvoiceNumber = 32768L
    BtwnumberMismatch = 131072L
    ContractInactive = 262144L
    InvoiceDateOutsideProjectPeriod = 524288L
End Enum
