using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using ECommerceWeb.Options;
using Microsoft.Extensions.Options;

namespace ECommerceWeb.Services;

public interface IVnpayService
{
    bool IsConfigured { get; }
    /// <param name="returnUrl">URL tuyệt đối (ưu tiên nếu appsettings VNPay:ReturnUrl để trống).</param>
    string BuildPaymentUrl(int orderId, decimal amountVnd, string txnRef, string clientIp, string returnUrl);
    bool ValidateSignature(IReadOnlyDictionary<string, string> vnpParams, string secureHashFromGateway);
    string? GetTxnRef(IReadOnlyDictionary<string, string> vnpParams);
    string? GetResponseCode(IReadOnlyDictionary<string, string> vnpParams);
}

public class VnpayService : IVnpayService
{
    private readonly VnpayOptions _opt;

    public VnpayService(IOptions<VnpayOptions> options) => _opt = options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_opt.TmnCode) &&
        !string.IsNullOrWhiteSpace(_opt.HashSecret);

    public string BuildPaymentUrl(int orderId, decimal amountVnd, string txnRef, string clientIp, string returnUrl)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("VNPay chưa được cấu hình (appsettings: VNPay).");

        var effectiveReturn = !string.IsNullOrWhiteSpace(_opt.ReturnUrl) ? _opt.ReturnUrl.Trim() : returnUrl;
        if (string.IsNullOrWhiteSpace(effectiveReturn))
            throw new InvalidOperationException("VNPay cần ReturnUrl (appsettings hoặc URL động từ request).");

        var createDate = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var expireDate = DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var amount = ((long)Math.Round(amountVnd, MidpointRounding.AwayFromZero) * 100).ToString(CultureInfo.InvariantCulture);

        var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _opt.TmnCode,
            ["vnp_Amount"] = amount,
            ["vnp_CreateDate"] = createDate,
            ["vnp_CurrCode"] = "VND",
            ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp,
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = $"Thanh toan don hang #{orderId}",
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = effectiveReturn,
            ["vnp_TxnRef"] = txnRef,
            ["vnp_ExpireDate"] = expireDate
        };

        if (!string.IsNullOrWhiteSpace(_opt.IpnUrl))
            data["vnp_IpnUrl"] = _opt.IpnUrl!;

        var signData = string.Join("&", data.Select(kv => $"{kv.Key}={kv.Value}"));
        var secureHash = HmacSha512(_opt.HashSecret, signData);

        var query = string.Join("&", data.Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));
        return $"{_opt.PaymentUrl.TrimEnd('?')}?{query}&vnp_SecureHash={WebUtility.UrlEncode(secureHash)}";
    }

    public bool ValidateSignature(IReadOnlyDictionary<string, string> vnpParams, string secureHashFromGateway)
    {
        var signData = string.Join("&", vnpParams
            .Where(kv => kv.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
            .Where(kv => !kv.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
                         && !kv.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}"));

        var expected = HmacSha512(_opt.HashSecret, signData);
        return string.Equals(expected, secureHashFromGateway, StringComparison.OrdinalIgnoreCase);
    }

    public string? GetTxnRef(IReadOnlyDictionary<string, string> vnpParams) =>
        vnpParams.TryGetValue("vnp_TxnRef", out var v) ? v : null;

    public string? GetResponseCode(IReadOnlyDictionary<string, string> vnpParams) =>
        vnpParams.TryGetValue("vnp_ResponseCode", out var v) ? v : null;

    private static string HmacSha512(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA512(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
