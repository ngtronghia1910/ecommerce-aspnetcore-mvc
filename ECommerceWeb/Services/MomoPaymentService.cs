using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ECommerceWeb.Options;
using Microsoft.Extensions.Options;

namespace ECommerceWeb.Services;

public interface IMomoPaymentService
{
    bool IsConfigured { get; }
    Task<MomoCreateResult> CreatePaymentAsync(int orderId, decimal amountVnd, string orderTxnId, string returnUrl, string notifyUrl, CancellationToken ct = default);
    bool ValidateReturnSignature(IReadOnlyDictionary<string, string> query);
    bool ValidateNotifySignature(JsonElement root);
}

public sealed record MomoCreateResult(bool Success, string? PayUrl, string? Message, int ResultCode);

public class MomoPaymentService : IMomoPaymentService
{
    private readonly MomoOptions _opt;
    private readonly HttpClient _http;

    public MomoPaymentService(HttpClient http, IOptions<MomoOptions> options)
    {
        _http = http;
        _opt = options.Value;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_opt.PartnerCode) &&
        !string.IsNullOrWhiteSpace(_opt.AccessKey) &&
        !string.IsNullOrWhiteSpace(_opt.SecretKey);

    public async Task<MomoCreateResult> CreatePaymentAsync(int orderId, decimal amountVnd, string orderTxnId, string returnUrl, string notifyUrl, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Momo chưa được cấu hình (appsettings: Momo).");

        var effectiveReturn = !string.IsNullOrWhiteSpace(_opt.ReturnUrl) ? _opt.ReturnUrl.Trim() : returnUrl;
        var effectiveNotify = !string.IsNullOrWhiteSpace(_opt.NotifyUrl) ? _opt.NotifyUrl.Trim() : notifyUrl;
        if (string.IsNullOrWhiteSpace(effectiveReturn) || string.IsNullOrWhiteSpace(effectiveNotify))
            throw new InvalidOperationException("Momo cần ReturnUrl và NotifyUrl (appsettings hoặc URL động).");

        var requestId = $"{orderTxnId}{DateTime.UtcNow:HHmmss}";
        var amount = Math.Round(amountVnd, 0, MidpointRounding.AwayFromZero)
            .ToString("0", CultureInfo.InvariantCulture);
        var orderInfo = $"Thanh toan don hang #{orderId}";
        var extraData = "";
        var raw = $"accessKey={_opt.AccessKey}&amount={amount}&extraData={extraData}&ipnUrl={effectiveNotify}" +
                    $"&orderId={orderTxnId}&orderInfo={orderInfo}&partnerCode={_opt.PartnerCode}" +
                    $"&redirectUrl={effectiveReturn}&requestId={requestId}&requestType=captureWallet";
        var signature = HmacSha256(_opt.SecretKey, raw);

        var body = new MomoCreateBody
        {
            partnerCode = _opt.PartnerCode,
            partnerName = "ECommerceWeb",
            storeId = "ECommerceStore",
            requestId = requestId,
            amount = amount,
            orderId = orderTxnId,
            orderInfo = orderInfo,
            redirectUrl = effectiveReturn,
            ipnUrl = effectiveNotify,
            lang = "vi",
            requestType = "captureWallet",
            extraData = extraData,
            signature = signature
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, _opt.Endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        using var resp = await _http.SendAsync(req, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var resultCode = root.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;
        var message = root.TryGetProperty("message", out var m) ? m.GetString() : null;
        var payUrl = root.TryGetProperty("payUrl", out var p) ? p.GetString() : null;

        return new MomoCreateResult(resultCode == 0, payUrl, message, resultCode);
    }

    public bool ValidateReturnSignature(IReadOnlyDictionary<string, string> q)
    {
        static string G(IReadOnlyDictionary<string, string> d, string k) =>
            d.TryGetValue(k, out var v) ? v : "";

        var raw = $"accessKey={_opt.AccessKey}&amount={G(q, "amount")}&extraData={G(q, "extraData")}" +
                  $"&message={G(q, "message")}&orderId={G(q, "orderId")}&orderInfo={G(q, "orderInfo")}" +
                  $"&orderType={G(q, "orderType")}&partnerCode={G(q, "partnerCode")}&payType={G(q, "payType")}" +
                  $"&requestId={G(q, "requestId")}&responseTime={G(q, "responseTime")}&resultCode={G(q, "resultCode")}" +
                  $"&transId={G(q, "transId")}";
        var sig = G(q, "signature");
        if (string.IsNullOrEmpty(sig))
            return false;
        return string.Equals(HmacSha256(_opt.SecretKey, raw), sig, StringComparison.Ordinal);
    }

    public bool ValidateNotifySignature(JsonElement root)
    {
        static string P(JsonElement e, string name) =>
            e.TryGetProperty(name, out var p) ? p.GetString() ?? "" : "";

        var raw = $"accessKey={P(root, "accessKey")}&amount={P(root, "amount")}&extraData={P(root, "extraData")}" +
                  $"&message={P(root, "message")}&orderId={P(root, "orderId")}&orderInfo={P(root, "orderInfo")}" +
                  $"&orderType={P(root, "orderType")}&partnerCode={P(root, "partnerCode")}&payType={P(root, "payType")}" +
                  $"&requestId={P(root, "requestId")}&responseTime={P(root, "responseTime")}&resultCode={P(root, "resultCode")}" +
                  $"&transId={P(root, "transId")}";
        var sig = P(root, "signature");
        if (string.IsNullOrEmpty(sig))
            return false;
        return string.Equals(HmacSha256(_opt.SecretKey, raw), sig, StringComparison.Ordinal);
    }

    private static string HmacSha256(string key, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private sealed class MomoCreateBody
    {
        public string partnerCode { get; set; } = "";
        public string partnerName { get; set; } = "";
        public string storeId { get; set; } = "";
        public string requestId { get; set; } = "";
        public string amount { get; set; } = "";
        public string orderId { get; set; } = "";
        public string orderInfo { get; set; } = "";
        public string redirectUrl { get; set; } = "";
        public string ipnUrl { get; set; } = "";
        public string lang { get; set; } = "vi";
        public string requestType { get; set; } = "captureWallet";
        public string extraData { get; set; } = "";
        public string signature { get; set; } = "";
    }
}
