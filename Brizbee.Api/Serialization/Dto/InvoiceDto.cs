//
//  InvoiceDto.cs
//  BRIZBEE API
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE API.
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

namespace Brizbee.Api.Serialization.Dto
{
    public class InvoiceDto
    {
        // Invoice Details

        public DateTime Invoice_CreatedAt { get; set; }
        
        public int Invoice_CustomerId { get; set; }
        
        public DateTime Invoice_EnteredOn { get; set; }
        
        public long Invoice_Id { get; set; }

        public string Invoice_Number { get; set; } = string.Empty;
        
        public int Invoice_OrganizationId { get; set; }
        
        public decimal Invoice_TotalAmount { get; set; }
        
        public long Invoice_TransactionId { get; set; }


        // Customer Details
        
        public DateTime Customer_CreatedAt { get; set; }
        
        public string Customer_Description { get; set; } = string.Empty;

        public int Customer_Id { get; set; }

        public string Customer_Name { get; set; } = string.Empty;

        public string Customer_Number { get; set; } = string.Empty;

        public int Customer_OrganizationId { get; set; }
    }
}
