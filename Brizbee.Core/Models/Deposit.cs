//
//  Deposit.cs
//  BRIZBEE Common Library
//
//  Copyright (C) 2019-2022 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE Common Library.
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as
//  published by the Free Software Foundation, either version 3 of the
//  License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Core.Models
{
    public class Deposit
    {
        [Required]
        public decimal Amount { get; set; }
        
        [Required]
        public long BankAccountId { get; set; }

        [ForeignKey("BankAccountId")]
        public virtual Account? BankAccount { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime EnteredOn { get; set; }
        
        [Key]
        public long Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string ReferenceNumber { get; set; } = string.Empty;
        
        [Required]
        public long TransactionId { get; set; }

        [ForeignKey("TransactionId")]
        public virtual Transaction? Transaction { get; set; }
    }
}
