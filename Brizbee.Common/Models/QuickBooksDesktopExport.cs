//
//  QuickBooksDesktopExport.cs
//  BRIZBEE Common Library
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
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

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Common.Models
{
    public class QuickBooksDesktopExport
    {
        /// <summary>
        /// Id for the lock containing the punches.
        /// </summary>
        public int? CommitId { get; set; }

        /// <summary>
        /// The lock containing the punches.
        /// </summary>
        [ForeignKey("CommitId")]
        public virtual Commit Commit { get; set; }

        /// <summary>
        /// Moment when the sync was performed.
        /// </summary>
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Log text from the Integration Utility.
        /// </summary>
        public string Log { get; set; }

        /// <summary>
        /// Id of the sync.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Start of the locked period.
        /// </summary>
        [Column(TypeName = "datetime2")]
        public DateTime? InAt { get; set; }

        /// <summary>
        /// End of the locked period.
        /// </summary>
        [Column(TypeName = "datetime2")]
        public DateTime? OutAt { get; set; }

        /// <summary>
        /// Product name collected from QuickBooks host details.
        /// </summary>
        [Required]
        [StringLength(40)]
        public string QBProductName { get; set; }

        /// <summary>
        /// Major version collected from QuickBooks host details.
        /// </summary>
        [Required]
        [StringLength(10)]
        public string QBMajorVersion { get; set; }

        /// <summary>
        /// Minor version collected from QuickBooks host details.
        /// </summary>
        [Required]
        [StringLength(10)]
        public string QBMinorVersion { get; set; }

        /// <summary>
        /// Country collected from QuickBooks host details.
        /// </summary>
        [Required]
        [StringLength(10)]
        public string QBCountry { get; set; }

        /// <summary>
        /// Supported QBXML version collected from QuickBooks host details.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string QBSupportedQBXMLVersions { get; set; }

        /// <summary>
        /// Id for the user who made the sync.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// The user who made the sync.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        /// <summary>
        /// Comma-separated list of TxnIDs from the added transactions in QuickBooks.
        /// </summary>
        public string TxnIDs { get; set; }
    }
}
