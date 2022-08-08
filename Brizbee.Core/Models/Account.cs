﻿//
//  Account.cs
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
    public class Account
    {
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }
        
        [Required]
        [StringLength(120)]
        public string Description { get; set; } = string.Empty;

        [Key]
        public long Id { get; set; }
        
        [Required]
        [StringLength(40)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string Number { get; set; } = string.Empty;

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization? Organization { get; set; }
        
        [Required]
        [StringLength(30)]
        public string Type { get; set; } = string.Empty;

        public string NormalBalance
        {
            get
            {
                switch (Type)
                {
                    case "Bank":
                        return "Debit";
                    case "Accounts Receivable":
                        return "Debit";
                    case "Other Current Asset":
                        return "Debit";
                    case "Fixed Asset":
                        return "Debit";
                    case "Other Asset":
                        return "Debit";
                    case "Expense":
                        return "Debit";
                    case "Other Expense":
                        return "Debit";

                    case "Accounts Payable":
                        return "Credit";
                    case "Credit Card":
                        return "Credit";
                    case "Other Current Liability":
                        return "Credit";
                    case "Long Term Liability":
                        return "Credit";
                    case "Equity":
                        return "Credit";
                    case "Income":
                        return "Credit";
                    case "Cost of Goods Sold":
                        return "Credit";
                    case "Other Income":
                        return "Credit";

                    default:
                        return "";
                }
            }
        }
    }
}