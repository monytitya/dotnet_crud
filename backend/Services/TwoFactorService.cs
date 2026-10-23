using OtpNet;
using QRCoder;

namespace backend.Services;

public sealed class TwoFactorService
{
    public string GenerateSecretKey() => Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

    public bool VerifyCode(string? secret, string code) =>
        !string.IsNullOrWhiteSpace(secret) &&
        new Totp(Base32Encoding.ToBytes(secret)).VerifyTotp(code, out _, new VerificationWindow(1, 1));

    public string CreateOtpAuthUri(string username, string issuer, string secret) =>
        $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(username)}" +
        $"?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";

    public string CreateQrCodeDataUrl(string otpAuthUri)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(5);
        return "data:image/png;base64," + Convert.ToBase64String(png);
    }
}
