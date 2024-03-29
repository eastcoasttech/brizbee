﻿//
//  QuickBooksService.cs
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

using Brizbee.Integration.Utility.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml;
using Brizbee.Core.Models;
using Brizbee.Core.Serialization;

namespace Brizbee.Integration.Utility.Services
{
    public class QuickBooksService
    {
        public void BuildInventoryItemQueryRq(XmlDocument doc, XmlElement parent)
        {
            var request = doc.CreateElement("ItemInventoryQueryRq");
            parent.AppendChild(request);

            // ------------------------------------------------------------
            // ItemInventoryQueryRq > MaxReturned
            // ------------------------------------------------------------

            request.AppendChild(MakeSimpleElement(doc, "MaxReturned", "5000"));

            // ------------------------------------------------------------
            // ItemInventoryQueryRq > IncludeRetElement
            // ------------------------------------------------------------

            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Name"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "FullName"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "BarCodeValue"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "ListID"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "ManufacturerPartNumber"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "SalesDesc"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "SalesPrice"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "PurchaseDesc"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "PurchaseCost"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "UnitOfMeasureSetRef"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "COGSAccountRef"));
        }

        public (bool, string, List<QBDInventoryItem>) WalkInventoryItemQueryRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            var responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            var queryResults = responseXmlDoc.GetElementsByTagName("ItemInventoryQueryRs");
            var firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            // Check the return status.
            var rsAttributes = firstQueryResult.Attributes;
            var statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            var statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            var statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            var iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var items = new List<QBDInventoryItem>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var inventoryItem = new QBDInventoryItem();

                    foreach (var node in queryResult.ChildNodes)
                    {
                        var xmlNode = node as XmlNode;

                        switch (xmlNode.Name)
                        {
                            case "Name":
                                inventoryItem.Name = xmlNode.InnerText;
                                break;
                            case "FullName":
                                inventoryItem.FullName = xmlNode.InnerText;
                                break;
                            case "BarCodeValue":
                                inventoryItem.BarCodeValue = xmlNode.InnerText;
                                break;
                            case "ListID":
                                inventoryItem.ListId = xmlNode.InnerText;
                                break;
                            case "ManufacturerPartNumber":
                                inventoryItem.ManufacturerPartNumber = xmlNode.InnerText;
                                break;
                            case "SalesDesc":
                                if (xmlNode.InnerText.Length > 255)
                                {
                                    inventoryItem.SalesDescription = xmlNode.InnerText.Substring(0, 255);
                                }
                                else
                                {
                                    inventoryItem.SalesDescription = xmlNode.InnerText;
                                }
                                break;
                            case "SalesPrice":
                                inventoryItem.SalesPrice = decimal.Parse(xmlNode.InnerText);
                                break;
                            case "PurchaseDesc":
                                if (xmlNode.InnerText.Length > 255)
                                {
                                    inventoryItem.PurchaseDescription = xmlNode.InnerText.Substring(0, 255);
                                }
                                else
                                {
                                    inventoryItem.PurchaseDescription = xmlNode.InnerText;
                                }
                                break;
                            case "PurchaseCost":
                                inventoryItem.PurchaseCost = decimal.Parse(xmlNode.InnerText);
                                break;
                            case "UnitOfMeasureSetRef":
                                foreach (var innerNode in xmlNode.ChildNodes)
                                {
                                    var xmlInnerNode = innerNode as XmlNode;
                                    if (xmlInnerNode.Name == "FullName")
                                    {
                                        inventoryItem.QBDUnitOfMeasureSetFullName = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "ListID")
                                    {
                                        inventoryItem.QBDUnitOfMeasureSetListId = xmlInnerNode.InnerText;
                                    }
                                }
                                break;
                            case "COGSAccountRef":
                                foreach (var innerNode in xmlNode.ChildNodes)
                                {
                                    var xmlInnerNode = innerNode as XmlNode;
                                    if (xmlInnerNode.Name == "FullName")
                                    {
                                        inventoryItem.QBDCOGSAccountFullName = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "ListID")
                                    {
                                        inventoryItem.QBDCOGSAccountListId = xmlInnerNode.InnerText;
                                    }
                                }
                                break;
                        }
                    }

                    items.Add(inventoryItem);
                }

                return (true, "", items);
            }
            else
            {
                return (false, statusMessage, null);
            }
        }

        public void BuildSalesReceiptAddRq(XmlDocument doc, XmlElement parent, List<QBDInventoryConsumption> consumptions, string valueMethod)
        {
            var request = doc.CreateElement("SalesReceiptAddRq");
            parent.AppendChild(request);

            foreach (var consumption in consumptions)
            {
                // ------------------------------------------------------------
                // SalesReceiptAddRq > SalesReceiptAdd
                // ------------------------------------------------------------

                var salesReceipt = doc.CreateElement("SalesReceiptAdd");
                request.AppendChild(salesReceipt);

                // ------------------------------------------------------------
                // SalesReceiptAdd > CustomerRef
                // ------------------------------------------------------------

                if (!string.IsNullOrEmpty(consumption.Task.Job.QuickBooksCustomerJob))
                {
                    var customerRef = doc.CreateElement("CustomerRef");
                    salesReceipt.AppendChild(customerRef);

                    customerRef.AppendChild(MakeSimpleElement(doc, "FullName", consumption.Task.Job.QuickBooksCustomerJob));
                }

                // ------------------------------------------------------------
                // SalesReceiptAdd > ClassRef
                // ------------------------------------------------------------

                if (!string.IsNullOrEmpty(consumption.Task.Job.QuickBooksClass))
                {
                    var classRef = doc.CreateElement("ClassRef");
                    salesReceipt.AppendChild(classRef);

                    classRef.AppendChild(MakeSimpleElement(doc, "FullName", consumption.Task.Job.QuickBooksClass));
                }

                // ------------------------------------------------------------
                // SalesReceiptAdd > TxnDate
                // ------------------------------------------------------------

                salesReceipt.AppendChild(MakeSimpleElement(doc, "TxnDate", consumption.CreatedAt.ToString("yyyy-MM-dd")));

                // ------------------------------------------------------------
                // SalesReceiptAdd > SalesReceiptLineAdd
                // ------------------------------------------------------------

                var line = doc.CreateElement("SalesReceiptLineAdd");
                salesReceipt.AppendChild(line);

                // ------------------------------------------------------------
                // SalesReceiptLineAdd > ItemRef
                // ------------------------------------------------------------

                var itemRef = doc.CreateElement("ItemRef");
                line.AppendChild(itemRef);

                itemRef.AppendChild(MakeSimpleElement(doc, "ListID", consumption.QBDInventoryItem.ListId));

                // ------------------------------------------------------------
                // SalesReceiptLineAdd > Quantity
                // ------------------------------------------------------------

                line.AppendChild(MakeSimpleElement(doc, "Quantity", consumption.Quantity.ToString()));

                // ------------------------------------------------------------
                // SalesReceiptLineAdd > Rate
                // ------------------------------------------------------------

                // Amount is determined by the value method.
                var amount = new decimal(0.00);
                if (valueMethod.ToUpperInvariant() == "PURCHASE COST")
                {
                    amount = consumption.QBDInventoryItem.PurchaseCost;
                }
                else if (valueMethod.ToUpperInvariant() == "SALES PRICE")
                {
                    amount = consumption.QBDInventoryItem.SalesPrice;
                }
                line.AppendChild(MakeSimpleElement(doc, "Rate", amount.ToString()));

                // ------------------------------------------------------------
                // SalesReceiptLineAdd > UnitOfMeasure
                // ------------------------------------------------------------

                if (!string.IsNullOrEmpty(consumption.UnitOfMeasure)) // Optional, not always available
                    line.AppendChild(MakeSimpleElement(doc, "UnitOfMeasure", consumption.UnitOfMeasure));

                // ------------------------------------------------------------
                // SalesReceiptLineAdd > InventorySiteRef
                // ------------------------------------------------------------

                if (consumption.QBDInventorySiteId.HasValue) // Optional, not always available
                {
                    var inventorySiteRef = doc.CreateElement("InventorySiteRef");
                    line.AppendChild(inventorySiteRef);

                    inventorySiteRef.AppendChild(MakeSimpleElement(doc, "ListID", consumption.QBDInventorySite.ListId));

                    // Inventory Site Location reference is the ListID.
                    //XmlElement inventorySiteLocationRef = doc.CreateElement("InventorySiteLocationRef");
                    //line.AppendChild(inventorySiteLocationRef);

                    //inventorySiteLocationRef.AppendChild(MakeSimpleElement(doc, "ListID", ""));
                }
            }
        }

        public (bool, string, List<string>) WalkSalesReceiptAddRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            var responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            var queryResults = responseXmlDoc.GetElementsByTagName("SalesReceiptAddRs");
            var firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            // Check the return status.
            var rsAttributes = firstQueryResult.Attributes;
            var statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            var statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            var statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            var iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var ids = new List<string>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var id = "";

                    foreach (var node in queryResult.ChildNodes)
                    {
                        var xmlNode = node as XmlNode;

                        switch (xmlNode.Name)
                        {
                            case "TxnID":
                                id = xmlNode.InnerText;
                                break;
                        }
                    }

                    ids.Add(id);
                }

                return (true, "", ids);
            }
            else
            {
                return (false, statusMessage, null);
            }
        }

        public void BuildInventoryAdjustmentAddRq(XmlDocument doc, XmlElement parent, List<QBDInventoryConsumption> consumptions, string valueMethod)
        {
            var request = doc.CreateElement("InventoryAdjustmentAddRq");
            parent.AppendChild(request);

            foreach (var consumption in consumptions)
            {
                // ------------------------------------------------------------
                // InventoryAdjustmentAddRq > InventoryAdjustmentAdd
                // ------------------------------------------------------------

                var adjustment = doc.CreateElement("InventoryAdjustmentAdd");
                request.AppendChild(adjustment);

                // ------------------------------------------------------------
                // InventoryAdjustmentAdd > AccountRef
                // ------------------------------------------------------------

                var accountRef = doc.CreateElement("AccountRef");
                adjustment.AppendChild(accountRef);

                accountRef.AppendChild(MakeSimpleElement(doc, "FullName", consumption.QBDInventoryItem.QBDCOGSAccountFullName));

                // ------------------------------------------------------------
                // InventoryAdjustmentAdd > TxnDate
                // ------------------------------------------------------------

                adjustment.AppendChild(MakeSimpleElement(doc, "TxnDate", consumption.CreatedAt.ToString("yyyy-MM-dd")));

                // ------------------------------------------------------------
                // InventoryAdjustmentAdd > CustomerRef
                // ------------------------------------------------------------

                if (!string.IsNullOrEmpty(consumption.Task.Job.QuickBooksCustomerJob))
                {
                    var customerRef = doc.CreateElement("CustomerRef");
                    adjustment.AppendChild(customerRef);

                    customerRef.AppendChild(MakeSimpleElement(doc, "FullName", consumption.Task.Job.QuickBooksCustomerJob));
                }

                // ------------------------------------------------------------
                // InventoryAdjustmentAdd > ClassRef
                // ------------------------------------------------------------

                if (!string.IsNullOrEmpty(consumption.Task.Job.QuickBooksClass))
                {
                    var classRef = doc.CreateElement("ClassRef");
                    adjustment.AppendChild(classRef);

                    classRef.AppendChild(MakeSimpleElement(doc, "FullName", consumption.Task.Job.QuickBooksClass));
                }

                // ------------------------------------------------------------
                // InventoryAdjustmentAdd > InventoryAdjustmentLineAdd
                // ------------------------------------------------------------

                var line = doc.CreateElement("InventoryAdjustmentLineAdd");
                adjustment.AppendChild(line);

                // ------------------------------------------------------------
                // InventoryAdjustmentLineAdd > ItemRef
                // ------------------------------------------------------------

                var itemRef = doc.CreateElement("ItemRef");
                line.AppendChild(itemRef);

                itemRef.AppendChild(MakeSimpleElement(doc, "ListID", consumption.QBDInventoryItem.ListId));

                // ------------------------------------------------------------
                // InventoryAdjustmentLineAdd > QuantityAdjustment
                // ------------------------------------------------------------

                var quantity = doc.CreateElement("QuantityAdjustment");
                line.AppendChild(quantity);

                quantity.AppendChild(MakeSimpleElement(doc, "QuantityDifference", (-consumption.Quantity).ToString()));

                // Amount is determined by the value method.
                //var amount = new decimal(0.00);
                //if (valueMethod.ToUpperInvariant() == "PURCHASE COST")
                //{
                //    amount = -consumption.QBDInventoryItem.PurchaseCost;
                //}
                //else if (valueMethod.ToUpperInvariant() == "SALES PRICE")
                //{
                //    amount = - consumption.QBDInventoryItem.SalesPrice;
                //}
                //quantity.AppendChild(MakeSimpleElement(doc, "ValueDifference", amount.ToString()));

                // ------------------------------------------------------------
                // InventoryAdjustmentLineAdd > InventorySiteRef
                // ------------------------------------------------------------

                if (consumption.QBDInventorySiteId.HasValue)
                {
                    var inventorySiteRef = doc.CreateElement("InventorySiteRef");
                    line.AppendChild(inventorySiteRef);

                    inventorySiteRef.AppendChild(MakeSimpleElement(doc, "ListID", consumption.QBDInventorySite.ListId));
                }
            }
        }

        public (bool, string, List<string>) WalkInventoryAdjustmentAddRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            var responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            var queryResults = responseXmlDoc.GetElementsByTagName("InventoryAdjustmentAddRs");
            var firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            // Check the return status.
            var rsAttributes = firstQueryResult.Attributes;
            var statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            var statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            var statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            var iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var ids = new List<string>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var id = "";

                    foreach (var node in queryResult.ChildNodes)
                    {
                        var xmlNode = node as XmlNode;

                        switch (xmlNode.Name)
                        {
                            case "TxnID":
                                id = xmlNode.InnerText;
                                break;
                        }
                    }

                    ids.Add(id);
                }

                return (true, "", ids);
            }
            else
            {
                return (false, statusMessage, null);
            }
        }

        public void BuildBillAddRq(XmlDocument doc, XmlElement parent, QBDInventoryConsumption consumption, string vendorFullName, string refNumber, string valueMethod)
        {
            var request = doc.CreateElement("BillAddRq");
            parent.AppendChild(request);

            // ------------------------------------------------------------
            // BillAddRq > BillAdd
            // ------------------------------------------------------------

            var bill = doc.CreateElement("BillAdd");
            request.AppendChild(bill);

            // ------------------------------------------------------------
            // BillAdd > VendorRef
            // ------------------------------------------------------------

            var vendorRef = doc.CreateElement("VendorRef");
            bill.AppendChild(vendorRef);

            vendorRef.AppendChild(MakeSimpleElement(doc, "FullName", vendorFullName));

            // ------------------------------------------------------------
            // BillAdd > TxnDate
            // ------------------------------------------------------------

            bill.AppendChild(MakeSimpleElement(doc, "TxnDate", consumption.CreatedAt.ToString("yyyy-MM-dd")));

            // ------------------------------------------------------------
            // BillAdd > DueDate
            // ------------------------------------------------------------

            bill.AppendChild(MakeSimpleElement(doc, "DueDate", consumption.CreatedAt.ToString("yyyy-MM-dd")));

            // ------------------------------------------------------------
            // BillAdd > RefNumber
            // ------------------------------------------------------------

            bill.AppendChild(MakeSimpleElement(doc, "RefNumber", refNumber.ToUpperInvariant()));


            // Determine the cost of the item, which is determined by the value method.
            var cost = new decimal(0.00);
            if (valueMethod.ToUpperInvariant() == "PURCHASE COST")
            {
                cost = consumption.QBDInventoryItem.PurchaseCost;
            }
            else if (valueMethod.ToUpperInvariant() == "SALES PRICE")
            {
                cost = consumption.QBDInventoryItem.SalesPrice;
            }


            // ------------------------------------------------------------
            //
            // Line 1 for adjusting the inventory
            //
            // ------------------------------------------------------------


            // ------------------------------------------------------------
            // BillAdd > ItemLineAdd 1
            // ------------------------------------------------------------

            var line1 = doc.CreateElement("ItemLineAdd");
            bill.AppendChild(line1);

            // ------------------------------------------------------------
            // ItemLineAdd 1 > ItemRef
            // ------------------------------------------------------------

            var itemRef = doc.CreateElement("ItemRef");
            line1.AppendChild(itemRef);

            itemRef.AppendChild(MakeSimpleElement(doc, "ListID", consumption.QBDInventoryItem.ListId));

            // ------------------------------------------------------------
            // ItemLineAdd 1 > InventorySiteRef
            // ------------------------------------------------------------

            if (consumption.QBDInventorySiteId.HasValue && consumption.QBDInventorySiteId != 0) // Optional, not always available
            {
                var inventorySiteRef = doc.CreateElement("InventorySiteRef");
                line1.AppendChild(inventorySiteRef);

                inventorySiteRef.AppendChild(MakeSimpleElement(doc, "ListID", consumption.QBDInventorySite.ListId));
            }

            // ------------------------------------------------------------
            // ItemLineAdd 1 > Quantity
            // ------------------------------------------------------------

            line1.AppendChild(MakeSimpleElement(doc, "Quantity", $"-{consumption.Quantity}"));

            // ------------------------------------------------------------
            // ItemLineAdd 1 > UnitOfMeasure
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(consumption.UnitOfMeasure)) // Optional, not always available
                line1.AppendChild(MakeSimpleElement(doc, "UnitOfMeasure", consumption.UnitOfMeasure));

            // ------------------------------------------------------------
            // ItemLineAdd 1 > Cost
            // ------------------------------------------------------------

            line1.AppendChild(MakeSimpleElement(doc, "Cost", cost.ToString()));


            // ------------------------------------------------------------
            //
            // Line 2 for increasing cost of goods sold
            //
            // ------------------------------------------------------------


            // ------------------------------------------------------------
            // BillAdd > ItemLineAdd 2
            // ------------------------------------------------------------

            var line2 = doc.CreateElement("ItemLineAdd");
            bill.AppendChild(line2);

            // ------------------------------------------------------------
            // ItemLineAdd 2 > ItemRef
            // ------------------------------------------------------------

            var offsetItemRef = doc.CreateElement("ItemRef");
            line2.AppendChild(offsetItemRef);

            offsetItemRef.AppendChild(MakeSimpleElement(doc, "FullName", consumption.QBDInventoryItem.OffsetItemFullName)); // Non-Inventory Part Item

            // ------------------------------------------------------------
            // ItemLineAdd 2 > Desc
            // ------------------------------------------------------------

            line2.AppendChild(MakeSimpleElement(doc, "Desc", consumption.QBDInventoryItem.SalesDescription));

            // ------------------------------------------------------------
            // ItemLineAdd 2 > Quantity
            // ------------------------------------------------------------

            line2.AppendChild(MakeSimpleElement(doc, "Quantity", consumption.Quantity.ToString()));

            // ------------------------------------------------------------
            // ItemLineAdd 2 > Cost
            // ------------------------------------------------------------

            line2.AppendChild(MakeSimpleElement(doc, "Cost", cost.ToString()));

            // ------------------------------------------------------------
            // ItemLineAdd 2 > CustomerRef
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(consumption.Task.Job.QuickBooksCustomerJob))
            {
                var customerRef = doc.CreateElement("CustomerRef");
                line2.AppendChild(customerRef);

                customerRef.AppendChild(MakeSimpleElement(doc, "FullName", consumption.Task.Job.QuickBooksCustomerJob));
            }

            // ------------------------------------------------------------
            // ItemLineAdd 2 > ClassRef
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(consumption.Task.Job.QuickBooksClass))
            {
                var classRef = doc.CreateElement("ClassRef");
                line2.AppendChild(classRef);

                classRef.AppendChild(MakeSimpleElement(doc, "FullName", consumption.Task.Job.QuickBooksClass));
            }

            // ------------------------------------------------------------
            // ItemLineAdd 2 > BillableStatus
            // ------------------------------------------------------------

            line2.AppendChild(MakeSimpleElement(doc, "BillableStatus", "Billable"));

            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "TxnID"));
        }

        public (bool, string, List<string>) WalkBillAddRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            var responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            var queryResults = responseXmlDoc.GetElementsByTagName("BillAddRs");
            var firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            // Check the return status.
            var rsAttributes = firstQueryResult.Attributes;
            var statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            var statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            var statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            var iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var ids = new List<string>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var id = "";

                    foreach (var node in queryResult.ChildNodes)
                    {
                        var xmlNode = node as XmlNode;

                        switch (xmlNode.Name)
                        {
                            case "TxnID":
                                id = xmlNode.InnerText;
                                break;
                        }
                    }

                    ids.Add(id);
                }

                return (true, "", ids);
            }
            else
            {
                return (false, statusMessage, null);
            }
        }

        public void BuildUnitOfMeasureSetQueryRq(XmlDocument doc, XmlElement parent)
        {
            // Create UnitOfMeasureSetQueryRq.
            var request = doc.CreateElement("UnitOfMeasureSetQueryRq");
            parent.AppendChild(request);

            // Return 500 items at a time.
            request.AppendChild(MakeSimpleElement(doc, "MaxReturned", "500"));

            // Only include certain fields.
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Name"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "ListID"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "BaseUnit"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "UnitOfMeasureType"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "IsActive"));
        }

        public (bool, string, List<QBDUnitOfMeasureSet>) WalkUnitOfMeasureSetQueryRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            var responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            var queryResults = responseXmlDoc.GetElementsByTagName("UnitOfMeasureSetQueryRs");
            var firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            // Check the return status.
            var rsAttributes = firstQueryResult.Attributes;
            var statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            var statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            var statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            var iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var units = new List<QBDUnitOfMeasureSet>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var unit = new QBDUnitOfMeasureSet();

                    foreach (var node in queryResult.ChildNodes)
                    {
                        var xmlNode = node as XmlNode;

                        switch (xmlNode.Name)
                        {
                            case "Name":
                                unit.Name = xmlNode.InnerText;
                                break;
                            case "ListID":
                                unit.ListId = xmlNode.InnerText;
                                break;
                            case "UnitOfMeasureType":
                                unit.UnitOfMeasureType = xmlNode.InnerText;
                                break;
                            case "IsActive":
                                unit.IsActive = bool.Parse(xmlNode.InnerText);
                                break;
                            case "BaseUnit":

                                // Adding details for the base unit.
                                var baseUnit = new QuickBooksUnitOfMeasure();
                                foreach (var innerNode in xmlNode.ChildNodes)
                                {
                                    var xmlInnerNode = innerNode as XmlNode;
                                    if (xmlInnerNode.Name == "Name")
                                    {
                                        baseUnit.Name = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "Abbreviation")
                                    {
                                        baseUnit.Abbreviation = xmlInnerNode.InnerText;
                                    }
                                }

                                // Deserialize the existing units, add details, and re-serialize.
                                var deserializedForBaseUnit = new QuickBooksUnitOfMeasures();

                                if (!string.IsNullOrEmpty(unit.UnitNamesAndAbbreviations))
                                {
                                    deserializedForBaseUnit = JsonConvert.DeserializeObject
                                        <QuickBooksUnitOfMeasures>(unit.UnitNamesAndAbbreviations);
                                }

                                deserializedForBaseUnit.BaseUnit = baseUnit;
                                unit.UnitNamesAndAbbreviations =
                                    JsonConvert.SerializeObject(deserializedForBaseUnit);

                                break;
                            case "RelatedUnit":

                                // Adding details for a related unit.
                                var relatedUnit = new QuickBooksUnitOfMeasure();
                                foreach (var innerNode in xmlNode.ChildNodes)
                                {
                                    var xmlInnerNode = innerNode as XmlNode;
                                    if (xmlInnerNode.Name == "Name")
                                    {
                                        relatedUnit.Name = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "Abbreviation")
                                    {
                                        relatedUnit.Abbreviation = xmlInnerNode.InnerText;
                                    }
                                }

                                // Deserialize the existing units, add details, and re-serialize.
                                var deserializedForRelatedUnit = JsonConvert.DeserializeObject
                                    <QuickBooksUnitOfMeasures>(unit.UnitNamesAndAbbreviations);
                                deserializedForRelatedUnit.RelatedUnits.Add(relatedUnit);
                                unit.UnitNamesAndAbbreviations =
                                    JsonConvert.SerializeObject(deserializedForRelatedUnit);

                                break;
                        }
                    }

                    units.Add(unit);
                }

                return (true, "", units);
            }
            else if (iStatusCode == 3250)
            {
                // Feature is not enabled or available
                return (true, "", new List<QBDUnitOfMeasureSet>());
            }
            else
            {
                return (false, statusMessage, null);
            }
        }

        public void BuildInventorySiteQueryRq(XmlDocument doc, XmlElement parent)
        {
            // Create UnitOfMeasureSetQueryRq.
            var request = doc.CreateElement("InventorySiteQueryRq");
            parent.AppendChild(request);

            // Only include certain fields.
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Name"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "ListID"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "IsActive"));
        }

        public (bool, string, List<QBDInventorySite>) WalkInventorySiteQueryRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            var responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            var queryResults = responseXmlDoc.GetElementsByTagName("InventorySiteQueryRs");
            var firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            // Check the return status.
            var rsAttributes = firstQueryResult.Attributes;
            var statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            var statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            var statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            var iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var sites = new List<QBDInventorySite>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var inventorySite = new QBDInventorySite();

                    foreach (var node in queryResult.ChildNodes)
                    {
                        var xmlNode = node as XmlNode;

                        switch (xmlNode.Name)
                        {
                            case "Name":
                                inventorySite.FullName = xmlNode.InnerText;
                                break;
                            case "ListID":
                                inventorySite.ListId = xmlNode.InnerText;
                                break;
                            case "IsActive":
                                inventorySite.IsActive = bool.Parse(xmlNode.InnerText);
                                break;
                        }
                    }

                    sites.Add(inventorySite);
                }

                return (true, "", sites);
            }
            else if (iStatusCode == 3250)
            {
                // Feature is not enabled or available
                return (true, "", new List<QBDInventorySite>());
            }
            else
            {
                return (false, statusMessage, null);
            }
        }

        public void BuildCustomerAddRqForJob(XmlDocument doc, XmlElement parent, QuickBooksCustomer quickBooksCustomer, string parentName)
        {
            var request = doc.CreateElement("CustomerAddRq");
            parent.AppendChild(request);

            // ------------------------------------------------------------
            // CustomerAddRq > CustomerAdd
            // ------------------------------------------------------------

            var customer = doc.CreateElement("CustomerAdd");
            request.AppendChild(customer);

            // ------------------------------------------------------------
            // CustomerAdd > Name
            // ------------------------------------------------------------

            customer.AppendChild(MakeSimpleElement(doc, "Name", quickBooksCustomer.Name));

            // ------------------------------------------------------------
            // CustomerAdd > IsActive
            // ------------------------------------------------------------

            if (!quickBooksCustomer.IsActive)
                customer.AppendChild(MakeSimpleElement(doc, "IsActive", "true"));

            // ------------------------------------------------------------
            // CustomerAdd > ParentRef
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(parentName))
            {
                var customerRef = doc.CreateElement("ParentRef");
                customer.AppendChild(customerRef);

                customerRef.AppendChild(MakeSimpleElement(doc, "FullName", parentName));
            }

            // ------------------------------------------------------------
            // CustomerAdd > CompanyName
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.CompanyName))
                customer.AppendChild(MakeSimpleElement(doc, "CompanyName", quickBooksCustomer.CompanyName));

            // ------------------------------------------------------------
            // CustomerAdd > Salutation
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.Salutation))
                customer.AppendChild(MakeSimpleElement(doc, "Salutation", quickBooksCustomer.Salutation));

            // ------------------------------------------------------------
            // CustomerAdd > FirstName
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.FirstName))
                customer.AppendChild(MakeSimpleElement(doc, "FirstName", quickBooksCustomer.FirstName));

            // ------------------------------------------------------------
            // CustomerAdd > MiddleName
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.MiddleName))
                customer.AppendChild(MakeSimpleElement(doc, "MiddleName", quickBooksCustomer.MiddleName));

            // ------------------------------------------------------------
            // CustomerAdd > LastName
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.LastName))
                customer.AppendChild(MakeSimpleElement(doc, "LastName", quickBooksCustomer.LastName));

            // ------------------------------------------------------------
            // CustomerAdd > JobTitle
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.JobTitle))
                customer.AppendChild(MakeSimpleElement(doc, "JobTitle", quickBooksCustomer.JobTitle));


            // ------------------------------------------------------------
            // CustomerAdd > BillAddress
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.BillAddressAddr1) ||
                !string.IsNullOrEmpty(quickBooksCustomer.BillAddressCity) ||
                !string.IsNullOrEmpty(quickBooksCustomer.BillAddressState) ||
                !string.IsNullOrEmpty(quickBooksCustomer.BillAddressPostalCode))
            {
                var billAddress = doc.CreateElement("BillAddress");
                customer.AppendChild(billAddress);

                billAddress.AppendChild(MakeSimpleElement(doc, "Addr1", quickBooksCustomer.BillAddressAddr1));

                if (!string.IsNullOrEmpty(quickBooksCustomer.BillAddressAddr2))
                    billAddress.AppendChild(MakeSimpleElement(doc, "Addr2", quickBooksCustomer.BillAddressAddr2));

                if (!string.IsNullOrEmpty(quickBooksCustomer.BillAddressAddr3))
                    billAddress.AppendChild(MakeSimpleElement(doc, "Addr3", quickBooksCustomer.BillAddressAddr3));

                billAddress.AppendChild(MakeSimpleElement(doc, "City", quickBooksCustomer.BillAddressCity));
                billAddress.AppendChild(MakeSimpleElement(doc, "State", quickBooksCustomer.BillAddressState));
                billAddress.AppendChild(MakeSimpleElement(doc, "PostalCode", quickBooksCustomer.BillAddressPostalCode));

                if (!string.IsNullOrEmpty(quickBooksCustomer.BillAddressCountry))
                    billAddress.AppendChild(MakeSimpleElement(doc, "Country", quickBooksCustomer.BillAddressCountry));

                if (!string.IsNullOrEmpty(quickBooksCustomer.BillAddressNote))
                    billAddress.AppendChild(MakeSimpleElement(doc, "Note", quickBooksCustomer.BillAddressNote));
            }

            // ------------------------------------------------------------
            // CustomerAdd > ShipToAddress
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.ShipToAddressName))
            {
                var shipToAddress = doc.CreateElement("ShipToAddress");
                customer.AppendChild(shipToAddress);

                shipToAddress.AppendChild(MakeSimpleElement(doc, "Name", quickBooksCustomer.ShipToAddressName));
            }

            // ------------------------------------------------------------
            // CustomerAdd > Phone
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.Phone))
                customer.AppendChild(MakeSimpleElement(doc, "Phone", quickBooksCustomer.Phone));

            // ------------------------------------------------------------
            // CustomerAdd > AltPhone
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.AltPhone))
                customer.AppendChild(MakeSimpleElement(doc, "AltPhone", quickBooksCustomer.AltPhone));

            // ------------------------------------------------------------
            // CustomerAdd > Fax
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.Fax))
                customer.AppendChild(MakeSimpleElement(doc, "Fax", quickBooksCustomer.Fax));

            // ------------------------------------------------------------
            // CustomerAdd > Email
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.Email))
                customer.AppendChild(MakeSimpleElement(doc, "Email", quickBooksCustomer.Email));

            // ------------------------------------------------------------
            // CustomerAdd > Cc
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.Cc))
                customer.AppendChild(MakeSimpleElement(doc, "Cc", quickBooksCustomer.Cc));

            // ------------------------------------------------------------
            // CustomerAdd > Contact
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.Contact))
                customer.AppendChild(MakeSimpleElement(doc, "Contact", quickBooksCustomer.Contact));

            // ------------------------------------------------------------
            // CustomerAdd > AltContact
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.AltContact))
                customer.AppendChild(MakeSimpleElement(doc, "AltContact", quickBooksCustomer.AltContact));

            // ------------------------------------------------------------
            // CustomerAdd > SalesTaxCountry
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.SalesTaxCountry))
                customer.AppendChild(MakeSimpleElement(doc, "SalesTaxCountry", quickBooksCustomer.SalesTaxCountry));

            // ------------------------------------------------------------
            // CustomerAdd > SalesTaxCodeRefListId
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.SalesTaxCodeRefListId))
            {
                var salesTaxCodeRef = doc.CreateElement("SalesTaxCodeRef");
                customer.AppendChild(salesTaxCodeRef);

                salesTaxCodeRef.AppendChild(MakeSimpleElement(doc, "ListID", quickBooksCustomer.SalesTaxCodeRefListId));
            }

            // ------------------------------------------------------------
            // CustomerAdd > ItemSalesTaxRefListId
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.ItemSalesTaxRefListId))
            {
                var itemSalesTaxRef = doc.CreateElement("ItemSalesTaxRef");
                customer.AppendChild(itemSalesTaxRef);

                itemSalesTaxRef.AppendChild(MakeSimpleElement(doc, "ListID", quickBooksCustomer.ItemSalesTaxRefListId));
            }

            // ------------------------------------------------------------
            // CustomerAdd > ResaleNumber
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.ResaleNumber))
                customer.AppendChild(MakeSimpleElement(doc, "ResaleNumber", quickBooksCustomer.ResaleNumber));

            // ------------------------------------------------------------
            // CustomerAdd > AccountNumber
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.AccountNumber))
                customer.AppendChild(MakeSimpleElement(doc, "AccountNumber", quickBooksCustomer.AccountNumber));

            // ------------------------------------------------------------
            // CustomerAdd > CreditLimit
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.CreditLimit))
                customer.AppendChild(MakeSimpleElement(doc, "CreditLimit", quickBooksCustomer.CreditLimit));

            // ------------------------------------------------------------
            // CustomerAdd > PreferredPaymentMethodRefListId
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(quickBooksCustomer.PreferredPaymentMethodRefListId))
            {
                var preferredPaymentMethodRef = doc.CreateElement("PreferredPaymentMethodRef");
                customer.AppendChild(preferredPaymentMethodRef);

                preferredPaymentMethodRef.AppendChild(MakeSimpleElement(doc, "ListID", quickBooksCustomer.PreferredPaymentMethodRefListId));
            }

            // ------------------------------------------------------------
            // CustomerAddRq > IncludeRetElement
            // ------------------------------------------------------------

            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Name"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "FullName"));
        }

        public void BuildCustomerAddRqForCustomer(XmlDocument doc, XmlElement parent, string name)
        {
            var request = doc.CreateElement("CustomerAddRq");
            parent.AppendChild(request);

            // ------------------------------------------------------------
            // CustomerAddRq > CustomerAdd
            // ------------------------------------------------------------

            var customer = doc.CreateElement("CustomerAdd");
            request.AppendChild(customer);

            // ------------------------------------------------------------
            // CustomerAdd > Name
            // ------------------------------------------------------------

            customer.AppendChild(MakeSimpleElement(doc, "Name", name));

            // ------------------------------------------------------------
            // CustomerAddRq > IncludeRetElement
            // ------------------------------------------------------------

            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Name"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "FullName"));
        }

        public (bool, string, List<string>) WalkCustomerAddRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            var responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            var queryResults = responseXmlDoc.GetElementsByTagName("CustomerAddRs");
            var firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            // Check the return status.
            var rsAttributes = firstQueryResult.Attributes;
            var statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            var statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            var statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            var iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var ids = new List<string>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var id = "";

                    foreach (var node in queryResult.ChildNodes)
                    {
                        var xmlNode = node as XmlNode;

                        //switch (xmlNode.Name)
                        //{
                        //    case "TxnID":
                        //        id = xmlNode.InnerText;
                        //        break;
                        //}
                    }

                    ids.Add(id);
                }

                return (true, "", ids);
            }
            else if (iStatusCode == 3100)
            {
                // Customer already exists
                return (true, "", null);
            }
            else
                return (false, statusMessage, null);
        }

        public void BuildCustomerQueryRq(XmlDocument doc, XmlElement parent, string name)
        {
            // Create CustomerQueryRq.
            var request = doc.CreateElement("CustomerQueryRq");
            parent.AppendChild(request);

            // Only include certain fields.
            request.AppendChild(MakeSimpleElement(doc, "FullName", name));

            // Only include certain fields.
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "ListID"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Name"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "FullName"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "IsActive"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "CompanyName"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Salutation"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "FirstName"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "MiddleName"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "LastName"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "JobTitle"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Phone"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "AltPhone"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Fax"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Email"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Cc"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Contact"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "AltContact"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "SalesTaxCountry"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "ResaleNumber"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "AccountNumber"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "CreditLimit"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "SalesTaxCodeRef"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "ItemSalesTaxRef"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "PreferredPaymentMethodRef"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "BillAddress"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "ShipToAddress"));
        }

        public (bool, string, List<QuickBooksCustomer>) WalkCustomerQueryRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            var responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            var queryResults = responseXmlDoc.GetElementsByTagName("CustomerQueryRs");
            var firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            // Check the return status.
            var rsAttributes = firstQueryResult.Attributes;
            var statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            var statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            var statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            var iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var customers = new List<QuickBooksCustomer>(0);

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var customer = new QuickBooksCustomer();

                    foreach (var node in queryResult.ChildNodes)
                    {
                        var xmlNode = node as XmlNode;

                        switch (xmlNode.Name)
                        {
                            case "Name":
                                customer.Name = xmlNode.InnerText;
                                break;
                            case "ListID":
                                customer.ListId = xmlNode.InnerText;
                                break;
                            case "IsActive":
                                customer.IsActive = bool.Parse(xmlNode.InnerText);
                                break;
                            case "CompanyName":
                                customer.CompanyName = xmlNode.InnerText;
                                break;
                            case "Salutation":
                                customer.Salutation = xmlNode.InnerText;
                                break;
                            case "FirstName":
                                customer.FirstName = xmlNode.InnerText;
                                break;
                            case "MiddleName":
                                customer.MiddleName = xmlNode.InnerText;
                                break;
                            case "LastName":
                                customer.LastName = xmlNode.InnerText;
                                break;
                            case "JobTitle":
                                customer.JobTitle = xmlNode.InnerText;
                                break;
                            case "Phone":
                                customer.Phone = xmlNode.InnerText;
                                break;
                            case "AltPhone":
                                customer.AltPhone = xmlNode.InnerText;
                                break;
                            case "Fax":
                                customer.Fax = xmlNode.InnerText;
                                break;
                            case "Email":
                                customer.Email = xmlNode.InnerText;
                                break;
                            case "Cc":
                                customer.Cc = xmlNode.InnerText;
                                break;
                            case "Contact":
                                customer.Contact = xmlNode.InnerText;
                                break;
                            case "AltContact":
                                customer.AltContact = xmlNode.InnerText;
                                break;
                            case "SalesTaxCountry":
                                customer.SalesTaxCountry = xmlNode.InnerText;
                                break;
                            case "ResaleNumber":
                                customer.ResaleNumber = xmlNode.InnerText;
                                break;
                            case "AccountNumber":
                                customer.AccountNumber = xmlNode.InnerText;
                                break;
                            case "CreditLimit":
                                customer.CreditLimit = xmlNode.InnerText;
                                break;
                            case "SalesTaxCodeRef":
                                foreach (var innerNode in xmlNode.ChildNodes)
                                {
                                    var xmlInnerNode = innerNode as XmlNode;
                                    if (xmlInnerNode.Name == "ListID")
                                    {
                                        customer.SalesTaxCodeRefListId = xmlInnerNode.InnerText;
                                    }
                                }
                                break;
                            case "ItemSalesTaxRef":
                                foreach (var innerNode in xmlNode.ChildNodes)
                                {
                                    var xmlInnerNode = innerNode as XmlNode;
                                    if (xmlInnerNode.Name == "ListID")
                                    {
                                        customer.ItemSalesTaxRefListId = xmlInnerNode.InnerText;
                                    }
                                }
                                break;
                            case "PreferredPaymentMethodRef":
                                foreach (var innerNode in xmlNode.ChildNodes)
                                {
                                    var xmlInnerNode = innerNode as XmlNode;
                                    if (xmlInnerNode.Name == "ListID")
                                    {
                                        customer.PreferredPaymentMethodRefListId = xmlInnerNode.InnerText;
                                    }
                                }
                                break;
                            case "BillAddress":
                                foreach (var innerNode in xmlNode.ChildNodes)
                                {
                                    var xmlInnerNode = innerNode as XmlNode;
                                    if (xmlInnerNode.Name == "Addr1")
                                    {
                                        customer.BillAddressAddr1 = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "Addr2")
                                    {
                                        customer.BillAddressAddr2 = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "Addr3")
                                    {
                                        customer.BillAddressAddr3 = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "Addr4")
                                    {
                                        customer.BillAddressAddr4 = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "Addr5")
                                    {
                                        customer.BillAddressAddr5 = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "City")
                                    {
                                        customer.BillAddressCity = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "State")
                                    {
                                        customer.BillAddressState = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "PostalCode")
                                    {
                                        customer.BillAddressPostalCode = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "Country")
                                    {
                                        customer.BillAddressCountry = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "Note")
                                    {
                                        customer.BillAddressNote = xmlInnerNode.InnerText;
                                    }
                                }
                                break;
                            case "ShipToAddress":
                                foreach (var innerNode in xmlNode.ChildNodes)
                                {
                                    var xmlInnerNode = innerNode as XmlNode;
                                    if (xmlInnerNode.Name == "Name")
                                    {
                                        customer.ShipToAddressName = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "Addr1")
                                    {
                                        customer.ShipToAddressAddr1 = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "Addr2")
                                    {
                                        customer.ShipToAddressAddr2 = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "Addr3")
                                    {
                                        customer.ShipToAddressAddr3 = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "Addr4")
                                    {
                                        customer.ShipToAddressAddr4 = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "Addr5")
                                    {
                                        customer.ShipToAddressAddr5 = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "City")
                                    {
                                        customer.ShipToAddressCity = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "State")
                                    {
                                        customer.ShipToAddressState = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "PostalCode")
                                    {
                                        customer.ShipToAddressPostalCode = xmlInnerNode.InnerText;
                                    }
                                    else if (xmlInnerNode.Name == "Country")
                                    {
                                        customer.ShipToAddressCountry = xmlInnerNode.InnerText;
                                    }
                                }
                                break;
                        }
                    }

                    customers.Add(customer);
                }

                return (true, "", customers);
            }
            else
            {
                return (false, statusMessage, null);
            }
        }

        public void BuildHostQueryRq(XmlDocument doc, XmlElement parent)
        {
            var HostQuery = doc.CreateElement("HostQueryRq");
            parent.AppendChild(HostQuery);

            // ------------------------------------------------------------
            // HostQueryRq > IncludeRetElement
            // ------------------------------------------------------------

            HostQuery.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "ProductName"));
            HostQuery.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "MajorVersion"));
            HostQuery.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "MinorVersion"));
            HostQuery.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Country"));
            HostQuery.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "SupportedQBXMLVersion"));
        }

        public (bool, string, QuickBooksHostDetails) WalkHostQueryRsAndParseHostDetails(string response)
        {
            var supportedQBXMLVersions = new List<string>();
            var quickBooksExport = new QuickBooksHostDetails();

            // Parse the response XML string into an XmlDocument.
            var responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            var HostQueryRsList = responseXmlDoc.GetElementsByTagName("HostQueryRs");
            foreach (var hostQueryResult in HostQueryRsList)
            {
                var responseNode = hostQueryResult as XmlNode;

                // Check the return status.
                var rsAttributes = responseNode.Attributes;
                var statusCode = rsAttributes.GetNamedItem("statusCode").Value;
                var statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
                var statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

                var iStatusCode = Convert.ToInt32(statusCode);

                if (iStatusCode == 0)
                {
                    var hostReturnResult = responseNode.FirstChild as XmlNode;
                    foreach (var node in hostReturnResult.ChildNodes)
                    {
                        var xmlNode = node as XmlNode;
                        switch (xmlNode.Name)
                        {
                            case "ProductName":
                                quickBooksExport.QBProductName = xmlNode.InnerText;
                                break;
                            case "MajorVersion":
                                quickBooksExport.QBMajorVersion = xmlNode.InnerText;
                                break;
                            case "MinorVersion":
                                quickBooksExport.QBMinorVersion = xmlNode.InnerText;
                                break;
                            case "Country":
                                quickBooksExport.QBCountry = xmlNode.InnerText;
                                break;
                            case "SupportedQBXMLVersion":
                                supportedQBXMLVersions.Add(xmlNode.InnerText);
                                break;
                        }
                    }

                    quickBooksExport.QBSupportedQBXMLVersions = string.Join(",", supportedQBXMLVersions);
                }
            }

            return (true, "", quickBooksExport);
        }

        public void BuildTxnDelRq(XmlDocument doc, XmlElement parent, string transactionType, string transactionId)
        {
            var request = doc.CreateElement("TxnDelRq");
            parent.AppendChild(request);

            // ------------------------------------------------------------
            // TxnDelRq > TxnDelType
            // ------------------------------------------------------------

            request.AppendChild(MakeSimpleElement(doc, "TxnDelType", transactionType));

            // ------------------------------------------------------------
            // TxnDelRq > TxnID
            // ------------------------------------------------------------

            request.AppendChild(MakeSimpleElement(doc, "TxnID", transactionId));
        }

        public (bool, string) WalkTxnDelRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            var responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            var queryResults = responseXmlDoc.GetElementsByTagName("TxnDelRs");
            var firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response."); }

            // Check the return status.
            var rsAttributes = firstQueryResult.Attributes;
            var statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            var statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            var statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            var iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var ids = new List<string>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var id = "";

                    foreach (var node in queryResult.ChildNodes)
                    {
                        var xmlNode = node as XmlNode;

                        switch (xmlNode.Name)
                        {
                            case "TxnID":
                                id = xmlNode.InnerText;
                                break;
                        }
                    }

                    ids.Add(id);
                }

                return (true, "");
            }
            else
            {
                return (false, statusMessage);
            }
        }

        public (XmlDocument, XmlElement) MakeQBXMLDocument()
        {
            var doc = new XmlDocument();

            doc.AppendChild(doc.CreateXmlDeclaration("1.0", null, null));
            doc.AppendChild(doc.CreateProcessingInstruction("qbxml", "version=\"14.0\""));

            var outer = doc.CreateElement("QBXML");
            doc.AppendChild(outer);

            var inner = doc.CreateElement("QBXMLMsgsRq");
            outer.AppendChild(inner);
            inner.SetAttribute("onError", "stopOnError");

            return (doc, inner);
        }

        private XmlElement MakeSimpleElement(XmlDocument doc, string tagName, string tagvalue)
        {
            var element = doc.CreateElement(tagName);
            element.InnerText = tagvalue;
            return element;
        }
    }
}
