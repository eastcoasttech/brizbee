using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Core.Models.Accounting;

public class Check
{
    [Required]
    public long BankAccountId { get; set; }

    [ForeignKey("BankAccountId")]
    public virtual Account? BankAccount { get; set; }

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime CreatedAt { get; set; }
    
    [StringLength(60)]
    public string Memo { get; set; } = string.Empty;

    [Required]
    public int VendorId { get; set; }

    [ForeignKey("VendorId")]
    public virtual Vendor? Vendor { get; set; }

    [Required]
    [Column(TypeName = "date")]
    public DateTime EnteredOn { get; set; }

    [Key]
    public long Id { get; set; }

    public virtual ICollection<CheckExpenseLine>? CheckExpenseLines { get; set; }

    [Required]
    [StringLength(20)]
    public string Number { get; set; } = string.Empty;

    [Required]
    public int OrganizationId { get; set; }

    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }

    [Required]
    public decimal TotalAmount { get; set; }

    [Required]
    public long TransactionId { get; set; }

    [ForeignKey("TransactionId")]
    public virtual Transaction? Transaction { get; set; }
}
