using FacadeCore;

namespace ServiceCore.Stubs;

public class QRCodeServiceStub : IQRCodeService
{
    public byte[]? GeneratePlaceholderQr(string content) => null;
}