//
//  QuickBooksService.cs
//  BRIZBEE Integration Utility
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
//
//  This file is part of BRIZBEE Database Management.
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

using Brizbee.Common.Models;
using Brizbee.Common.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Brizbee.Integration.Utility.Services
{
    public class QuickBooksService
    {
        public void BuildInventoryItemQueryRq(XmlDocument doc, XmlElement parent)
        {
            XmlElement request = doc.CreateElement("ItemInventoryQueryRq");
            parent.AppendChild(request);

            // ------------------------------------------------------------
            // ItemInventoryQueryRq > MaxReturned
            // ------------------------------------------------------------

            request.AppendChild(MakeSimpleElement(doc, "MaxReturned", "500"));

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
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            XmlNodeList queryResults = responseXmlDoc.GetElementsByTagName("ItemInventoryQueryRs");
            XmlNode firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            //Check the status code, info, and severity
            XmlAttributeCollection rsAttributes = firstQueryResult.Attributes;
            string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            int iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var items = new List<QBDInventoryItem>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var inventoryItem = new QBDInventoryItem();

                    foreach (var node in queryResult.ChildNodes)
                    {
                        XmlNode xmlNode = node as XmlNode;

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
                                    XmlNode xmlInnerNode = innerNode as XmlNode;
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
                                    XmlNode xmlInnerNode = innerNode as XmlNode;
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
            XmlElement request = doc.CreateElement("SalesReceiptAddRq");
            parent.AppendChild(request);

            foreach (var consumption in consumptions)
            {
                // ------------------------------------------------------------
                // SalesReceiptAddRq > SalesReceiptAdd
                // ------------------------------------------------------------

                XmlElement salesReceipt = doc.CreateElement("SalesReceiptAdd");
                request.AppendChild(salesReceipt);

                // ------------------------------------------------------------
                // SalesReceiptAdd > CustomerRef
                // ------------------------------------------------------------

                if (!string.IsNullOrEmpty(consumption.Task.Job.QuickBooksCustomerJob))
                {
                    XmlElement customerRef = doc.CreateElement("CustomerRef");
                    salesReceipt.AppendChild(customerRef);

                    customerRef.AppendChild(MakeSimpleElement(doc, "FullName", consumption.Task.Job.QuickBooksCustomerJob));
                }

                // ------------------------------------------------------------
                // SalesReceiptAdd > ClassRef
                // ------------------------------------------------------------

                if (!string.IsNullOrEmpty(consumption.Task.Job.QuickBooksClass))
                {
                    XmlElement classRef = doc.CreateElement("ClassRef");
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

                XmlElement line = doc.CreateElement("SalesReceiptLineAdd");
                salesReceipt.AppendChild(line);

                // ------------------------------------------------------------
                // SalesReceiptLineAdd > ItemRef
                // ------------------------------------------------------------

                XmlElement itemRef = doc.CreateElement("ItemRef");
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
                    XmlElement inventorySiteRef = doc.CreateElement("InventorySiteRef");
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
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            XmlNodeList queryResults = responseXmlDoc.GetElementsByTagName("SalesReceiptAddRs");
            XmlNode firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            //Check the status code, info, and severity
            XmlAttributeCollection rsAttributes = firstQueryResult.Attributes;
            string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            int iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var ids = new List<string>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var id = "";

                    foreach (var node in queryResult.ChildNodes)
                    {
                        XmlNode xmlNode = node as XmlNode;

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
            XmlElement request = doc.CreateElement("InventoryAdjustmentAddRq");
            parent.AppendChild(request);

            foreach (var consumption in consumptions)
            {
                // ------------------------------------------------------------
                // InventoryAdjustmentAddRq > InventoryAdjustmentAdd
                // ------------------------------------------------------------

                XmlElement adjustment = doc.CreateElement("InventoryAdjustmentAdd");
                request.AppendChild(adjustment);

                // ------------------------------------------------------------
                // InventoryAdjustmentAdd > AccountRef
                // ------------------------------------------------------------

                XmlElement accountRef = doc.CreateElement("AccountRef");
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
                    XmlElement customerRef = doc.CreateElement("CustomerRef");
                    adjustment.AppendChild(customerRef);

                    customerRef.AppendChild(MakeSimpleElement(doc, "FullName", consumption.Task.Job.QuickBooksCustomerJob));
                }

                // ------------------------------------------------------------
                // InventoryAdjustmentAdd > ClassRef
                // ------------------------------------------------------------

                if (!string.IsNullOrEmpty(consumption.Task.Job.QuickBooksClass))
                {
                    XmlElement classRef = doc.CreateElement("ClassRef");
                    adjustment.AppendChild(classRef);

                    classRef.AppendChild(MakeSimpleElement(doc, "FullName", consumption.Task.Job.QuickBooksClass));
                }

                // ------------------------------------------------------------
                // InventoryAdjustmentAdd > InventoryAdjustmentLineAdd
                // ------------------------------------------------------------

                XmlElement line = doc.CreateElement("InventoryAdjustmentLineAdd");
                adjustment.AppendChild(line);

                // ------------------------------------------------------------
                // InventoryAdjustmentLineAdd > ItemRef
                // ------------------------------------------------------------

                XmlElement itemRef = doc.CreateElement("ItemRef");
                line.AppendChild(itemRef);

                itemRef.AppendChild(MakeSimpleElement(doc, "ListID", consumption.QBDInventoryItem.ListId));

                // ------------------------------------------------------------
                // InventoryAdjustmentLineAdd > QuantityAdjustment
                // ------------------------------------------------------------

                XmlElement quantity = doc.CreateElement("QuantityAdjustment");
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
                    XmlElement inventorySiteRef = doc.CreateElement("InventorySiteRef");
                    line.AppendChild(inventorySiteRef);

                    inventorySiteRef.AppendChild(MakeSimpleElement(doc, "ListID", consumption.QBDInventorySite.ListId));
                }
            }
        }

        public (bool, string, List<string>) WalkInventoryAdjustmentAddRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            XmlNodeList queryResults = responseXmlDoc.GetElementsByTagName("InventoryAdjustmentAddRs");
            XmlNode firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            //Check the status code, info, and severity
            XmlAttributeCollection rsAttributes = firstQueryResult.Attributes;
            string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            int iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var ids = new List<string>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var id = "";

                    foreach (var node in queryResult.ChildNodes)
                    {
                        XmlNode xmlNode = node as XmlNode;

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
            XmlElement request = doc.CreateElement("UnitOfMeasureSetQueryRq");
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
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            XmlNodeList queryResults = responseXmlDoc.GetElementsByTagName("UnitOfMeasureSetQueryRs");
            XmlNode firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            //Check the status code, info, and severity
            XmlAttributeCollection rsAttributes = firstQueryResult.Attributes;
            string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            int iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var units = new List<QBDUnitOfMeasureSet>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var unit = new QBDUnitOfMeasureSet();

                    foreach (var node in queryResult.ChildNodes)
                    {
                        XmlNode xmlNode = node as XmlNode;

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
                                    XmlNode xmlInnerNode = innerNode as XmlNode;
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
                                var deserializedForBaseUnit = JsonConvert.DeserializeObject<QuickBooksUnitOfMeasures>(unit.UnitNamesAndAbbreviations);
                                deserializedForBaseUnit.BaseUnit = baseUnit;
                                unit.UnitNamesAndAbbreviations = JsonConvert.SerializeObject(deserializedForBaseUnit);

                                break;
                            case "RelatedUnit":

                                // Adding details for a related unit.
                                var relatedUnit = new QuickBooksUnitOfMeasure();
                                foreach (var innerNode in xmlNode.ChildNodes)
                                {
                                    XmlNode xmlInnerNode = innerNode as XmlNode;
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
                                var deserializedForRelatedUnit = JsonConvert.DeserializeObject<QuickBooksUnitOfMeasures>(unit.UnitNamesAndAbbreviations);
                                deserializedForRelatedUnit.RelatedUnits.Add(relatedUnit);
                                unit.UnitNamesAndAbbreviations = JsonConvert.SerializeObject(deserializedForRelatedUnit);

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
            XmlElement request = doc.CreateElement("InventorySiteQueryRq");
            parent.AppendChild(request);

            // Return 500 items at a time.
            request.AppendChild(MakeSimpleElement(doc, "MaxReturned", "500"));

            // Only include certain fields.
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Name"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "ListID"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "IsActive"));
        }

        public (bool, string, List<QBDInventorySite>) WalkInventorySiteQueryRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            XmlNodeList queryResults = responseXmlDoc.GetElementsByTagName("InventorySiteQueryRs");
            XmlNode firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            //Check the status code, info, and severity
            XmlAttributeCollection rsAttributes = firstQueryResult.Attributes;
            string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            int iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var sites = new List<QBDInventorySite>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var inventorySite = new QBDInventorySite();

                    foreach (var node in queryResult.ChildNodes)
                    {
                        XmlNode xmlNode = node as XmlNode;

                        switch (xmlNode.Name)
                        {
                            case "Name":
                                inventorySite.Name = xmlNode.InnerText;
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

        public void BuildCustomerAddRq(XmlDocument doc, XmlElement parent, string name, string parentName = "")
        {
            XmlElement request = doc.CreateElement("CustomerAddRq");
            parent.AppendChild(request);

            // ------------------------------------------------------------
            // CustomerAddRq > CustomerAdd
            // ------------------------------------------------------------

            XmlElement customer = doc.CreateElement("CustomerAdd");
            request.AppendChild(customer);

            // ------------------------------------------------------------
            // CustomerAdd > Name
            // ------------------------------------------------------------

            customer.AppendChild(MakeSimpleElement(doc, "Name", name));

            // ------------------------------------------------------------
            // CustomerAdd > ParentRef
            // ------------------------------------------------------------

            if (!string.IsNullOrEmpty(parentName))
            {
                XmlElement customerRef = doc.CreateElement("ParentRef");
                customer.AppendChild(customerRef);

                customerRef.AppendChild(MakeSimpleElement(doc, "FullName", parentName));
            }

            // ------------------------------------------------------------
            // CustomerAddRq > IncludeRetElement
            // ------------------------------------------------------------

            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Name"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "FullName"));
        }

        public (bool, string, List<string>) WalkCustomerAddRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            XmlNodeList queryResults = responseXmlDoc.GetElementsByTagName("CustomerAddRs");
            XmlNode firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response.", null); }

            //Check the status code, info, and severity
            XmlAttributeCollection rsAttributes = firstQueryResult.Attributes;
            string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            int iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var ids = new List<string>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var id = "";

                    foreach (var node in queryResult.ChildNodes)
                    {
                        XmlNode xmlNode = node as XmlNode;

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

        public void BuildHostQueryRq(XmlDocument doc, XmlElement parent)
        {
            XmlElement HostQuery = doc.CreateElement("HostQueryRq");
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

            //Parse the response XML string into an XmlDocument
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            //Get the response for our request
            XmlNodeList HostQueryRsList = responseXmlDoc.GetElementsByTagName("HostQueryRs");
            foreach (var hostQueryResult in HostQueryRsList)
            {
                XmlNode responseNode = hostQueryResult as XmlNode;

                //Check the status code, info, and severity
                XmlAttributeCollection rsAttributes = responseNode.Attributes;
                string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
                string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
                string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

                int iStatusCode = Convert.ToInt32(statusCode);

                if (iStatusCode == 0)
                {
                    XmlNode hostReturnResult = responseNode.FirstChild as XmlNode;
                    foreach (var node in hostReturnResult.ChildNodes)
                    {
                        XmlNode xmlNode = node as XmlNode;
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
            XmlElement request = doc.CreateElement("TxnDelRq");
            parent.AppendChild(request);

            // ------------------------------------------------------------
            // TxnDelRq > TxnDelType
            // ------------------------------------------------------------

            parent.AppendChild(MakeSimpleElement(doc, "TxnDelType", transactionType));

            // ------------------------------------------------------------
            // TxnDelRq > TxnID
            // ------------------------------------------------------------

            parent.AppendChild(MakeSimpleElement(doc, "TxnID", transactionId));
        }

        public (bool, string) WalkTxnDelRs(string response)
        {
            // Parse the response XML string into an XmlDocument.
            XmlDocument responseXmlDoc = new XmlDocument();
            responseXmlDoc.LoadXml(response);

            // Get the response for our request.
            XmlNodeList queryResults = responseXmlDoc.GetElementsByTagName("TxnDelRs");
            XmlNode firstQueryResult = queryResults.Item(0);

            if (firstQueryResult == null) { return (false, "No items in QuickBooks response."); }

            //Check the status code, info, and severity
            XmlAttributeCollection rsAttributes = firstQueryResult.Attributes;
            string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
            string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
            string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;

            int iStatusCode = Convert.ToInt32(statusCode);

            if (iStatusCode == 0)
            {
                var ids = new List<string>();

                foreach (XmlNode queryResult in firstQueryResult.ChildNodes)
                {
                    var id = "";

                    foreach (var node in queryResult.ChildNodes)
                    {
                        XmlNode xmlNode = node as XmlNode;

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

            XmlElement outer = doc.CreateElement("QBXML");
            doc.AppendChild(outer);

            XmlElement inner = doc.CreateElement("QBXMLMsgsRq");
            outer.AppendChild(inner);
            inner.SetAttribute("onError", "stopOnError");

            return (doc, inner);
        }

        private XmlElement MakeSimpleElement(XmlDocument doc, string tagName, string tagvalue)
        {
            XmlElement element = doc.CreateElement(tagName);
            element.InnerText = tagvalue;
            return element;
        }
    }
}
