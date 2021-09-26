//
//  QuickBooksCustomer.cs
//  BRIZBEE Integration Utility
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
//
//  This file is part of BRIZBEE Integration Utility.
//
//  This program is free software: you can redistribute
//  it and/or modify it under the terms of the GNU General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will
//  be useful, but WITHOUT ANY WARRANTY; without even the implied
//  warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//  See the GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.
//  If not, see <https://www.gnu.org/licenses/>.
//

namespace Brizbee.Integration.Utility.Serialization
{
    public class QuickBooksCustomer
    {
        public string ListId { get; set; }

        public string Name { get; set; }

        public bool IsActive { get; set; }

        public string CompanyName { get; set; }

        public string Salutation { get; set; }

        public string FirstName { get; set; }

        public string MiddleName { get; set; }

        public string LastName { get; set; }

        public string JobTitle { get; set; }

        public string Phone { get; set; }

        public string AltPhone { get; set; }

        public string Fax { get; set; }

        public string Email { get; set; }

        public string Cc { get; set; }

        public string Contact { get; set; }

        public string AltContact { get; set; }

        public string SalesTaxCountry { get; set; }

        public string ResaleNumber { get; set; }

        public string AccountNumber { get; set; }

        public string CreditLimit { get; set; }

        public string SalesTaxCodeRefListId { get; set; }

        public string ItemSalesTaxRefListId { get; set; }

        public string PreferredPaymentMethodRefListId { get; set; }

        public string BillAddressAddr1 { get; set; }

        public string BillAddressAddr2 { get; set; }

        public string BillAddressAddr3 { get; set; }

        public string BillAddressAddr4 { get; set; }

        public string BillAddressAddr5 { get; set; }

        public string BillAddressCity { get; set; }

        public string BillAddressState { get; set; }

        public string BillAddressPostalCode { get; set; }

        public string BillAddressCountry { get; set; }

        public string BillAddressNote { get; set; }

        public string ShipToAddressName { get; set; }

        public string ShipToAddressAddr1 { get; set; }

        public string ShipToAddressAddr2 { get; set; }

        public string ShipToAddressAddr3 { get; set; }

        public string ShipToAddressAddr4 { get; set; }

        public string ShipToAddressAddr5 { get; set; }

        public string ShipToAddressCity { get; set; }

        public string ShipToAddressState { get; set; }

        public string ShipToAddressPostalCode { get; set; }

        public string ShipToAddressCountry { get; set; }
    }
}
