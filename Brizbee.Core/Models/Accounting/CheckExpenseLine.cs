using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Core.Models.Accounting;

public class CheckExpenseLine
{
    [Required]
    public decimal Amount { get; set; }

    [Required]
    public long AccountId { get; set; }

    [ForeignKey("AccountId")]
    public virtual Account? Account { get; set; }

    [Required]
    public long CheckId { get; set; }

    [ForeignKey("CheckId")]
    public virtual Check? Check { get; set; }
    
    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime CreatedAt { get; set; }
    
    [StringLength(60)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public long EntryId { get; set; }

    [ForeignKey("EntryId")]
    public virtual Entry? Entry { get; set; }

    [Key]
    public long Id { get; set; }
}
