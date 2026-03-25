namespace CPMCore.Models;

public class EntraLoginViewModel
{
    public string ReturnUrl { get; set; } = "~";
    public LoginType Type { get; set; } = LoginType.Internal;
}

public enum LoginType { Internal, Contractor, Customer }
