//
//  QBDInventoryConsumptionExpanded.cs
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

using System;

namespace Brizbee.Api.Serialization.Expanded
{
    public class QBDInventoryConsumptionExpanded
    {
        // Consumption Details

        public long Consumption_Id { get; set; }

        public int Consumption_Quantity { get; set; }

        public string Consumption_UnitOfMeasure { get; set; }

        public DateTime Consumption_CreatedAt { get; set; }

        public int Consumption_CreatedByUserId { get; set; }

        public string Consumption_Hostname { get; set; }

        public long Consumption_QBDInventoryItemId { get; set; }

        public long Consumption_QBDInventorySiteId { get; set; }

        public int Consumption_OrganizationId { get; set; }

        public long? Consumption_QBDInventoryConsumptionSyncId { get; set; }


        // Items Details

        public long Item_Id { get; set; }

        public string Item_Name { get; set; }

        public string Item_FullName { get; set; }

        public string Item_ManufacturerPartNumber { get; set; }

        public string Item_BarCodeValue { get; set; }

        public string Item_ListId { get; set; }

        public string Item_PurchaseDescription { get; set; }

        public string Item_SalesDescription { get; set; }

        public long Item_QBDInventoryItemSyncId { get; set; }

        public decimal Item_PurchaseCost { get; set; }

        public decimal Item_SalesPrice { get; set; }


        // Task Details

        public string Task_Number { get; set; }

        public string Task_Name { get; set; }


        // Job Details

        public string Job_Number { get; set; }

        public string Job_Name { get; set; }


        // Customer Details

        public string Customer_Number { get; set; }

        public string Customer_Name { get; set; }


        // User Details

        public int User_Id { get; set; }

        public string User_Name { get; set; }
    }
}