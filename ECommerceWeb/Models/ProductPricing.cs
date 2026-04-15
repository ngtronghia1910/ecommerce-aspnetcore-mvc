namespace ECommerceWeb.Models;

public static class ProductPricing
{
    public static bool HasDiscount(Product p) =>
        p.DiscountPercent.HasValue && p.DiscountPercent.Value > 0;

    public static decimal GetEffectiveUnitPrice(Product p)
    {
        if (!HasDiscount(p))
            return p.Price;
        var pct = Math.Clamp(p.DiscountPercent!.Value, 0, 100);
        var raw = p.Price * (100 - pct) / 100;
        return Math.Max(0, Math.Round(raw, 0, MidpointRounding.AwayFromZero));
    }

    public static int GetDiscountPercentDisplay(Product p) =>
        HasDiscount(p) ? (int)Math.Floor(p.DiscountPercent!.Value) : 0;
}
