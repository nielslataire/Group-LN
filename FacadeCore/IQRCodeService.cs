namespace FacadeCore;

public interface IQRCodeService
{
    byte[]? GeneratePlaceholderQr(string content);
}