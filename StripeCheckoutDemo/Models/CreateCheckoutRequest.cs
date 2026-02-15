using System.ComponentModel.DataAnnotations;

public class CreateCheckoutRequest
{
    [Range(1, long.MaxValue)]
    public long Amount { get; set; }

    [Required]
    public string Currency { get; set; } = "usd";
}
