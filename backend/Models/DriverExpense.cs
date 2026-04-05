namespace backend.Models;

public class DriverExpense
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public ApplicationUser? Driver { get; set; }
    public ExpenseType Type { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public string? ReceiptImage { get; set; } // Path to uploaded receipt image
    public ExpenseStatus Status { get; set; } // Pending, Approved, Rejected
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum ExpenseType
{
    Fuel,
    Maintenance,
    Toll,
    Parking,
    Other
}

public enum ExpenseStatus
{
    Pending,
    Approved,
    Rejected
}