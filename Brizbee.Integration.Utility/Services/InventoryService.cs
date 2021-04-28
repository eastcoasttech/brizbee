using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace Brizbee.Integration.Utility.Services
{
    public class InventoryService
    {
        public void BuildInventoryItemQueryRq(XmlDocument doc, XmlElement parent)
        {
            // Create ItemInventoryQueryRq.
            XmlElement request = doc.CreateElement("ItemInventoryQueryRq");
            parent.AppendChild(request);

            // Return 500 items at a time.
            request.AppendChild(MakeSimpleElement(doc, "MaxReturned", "500"));

            // Only include certain fields.
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "Name"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "FullName"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "BarCodeValue"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "ListID"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "ManufacturerPartNumber"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "SalesDesc"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "PurchaseDesc"));
            request.AppendChild(MakeSimpleElement(doc, "IncludeRetElement", "UnitOfMeasureSetRef"));
        }

        public void BuildSalesReceiptAddRq(XmlDocument doc, XmlElement parent, List<QBDInventoryConsumption> consumptions)
        {
            // Create SalesReceiptAddRq.
            XmlElement request = doc.CreateElement("SalesReceiptAddRq");
            parent.AppendChild(request);

            foreach (var consumption in consumptions)
            {
                // Create SalesReceiptAdd.
                XmlElement salesReceipt = doc.CreateElement("SalesReceiptAdd");
                request.AppendChild(salesReceipt);

                // Specify details.
                salesReceipt.AppendChild(MakeSimpleElement(doc, "TxnDate", consumption.CreatedAt.ToString("yyyy-MM-dd")));

                // Create SalesReceiptLineAdd.
                XmlElement line = doc.CreateElement("SalesReceiptLineAdd");
                salesReceipt.AppendChild(line);

                // Inventory Item reference is the ListID.
                XmlElement itemRef = doc.CreateElement("ItemRef");
                line.AppendChild(itemRef);

                itemRef.AppendChild(MakeSimpleElement(doc, "ListID", consumption.QBDInventoryItem.ListId));

                // Specify details (Desc is filled in by the Inventory Item automatically).
                line.AppendChild(MakeSimpleElement(doc, "Quantity", consumption.Quantity.ToString()));
                //line.AppendChild(MakeSimpleElement(doc, "Rate", "1.52"));

                // Unit of Measure is optional because it is not supported in all editions of QuickBooks.
                if (!string.IsNullOrEmpty(consumption.UnitOfMeasure))
                    line.AppendChild(MakeSimpleElement(doc, "UnitOfMeasure", consumption.UnitOfMeasure));

                // Inventory Site is optional because it is not supported in all editions of QuickBooks.
                if (consumption.QBDInventorySiteId.HasValue)
                {
                    // Inventory Site reference is the ListID.
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

        public (bool, string, object) WalkSalesReceiptAddRs(string response)
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
                return (true, "", null);
            }
            else
            {
                return (false, statusMessage, null);
            }
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
                var items = new List<QBDUnitOfMeasureSet>();

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
                            case "BaseUnitName":
                                foreach (var innerNode in xmlNode.ChildNodes)
                                {
                                    XmlNode xmlInnerNode = innerNode as XmlNode;
                                    if (xmlInnerNode.Name == "Name")
                                    {
                                        unit.BaseUnitName = xmlNode.ChildNodes[0].InnerText;
                                    }
                                }
                                break;
                            case "BaseUnitAbbreviation":
                                foreach (var innerNode in xmlNode.ChildNodes)
                                {
                                    XmlNode xmlInnerNode = innerNode as XmlNode;
                                    if (xmlInnerNode.Name == "Abbreviation")
                                    {
                                        unit.BaseUnitAbbreviation = xmlNode.ChildNodes[1].InnerText;
                                    }
                                }
                                break;
                        }
                    }

                    items.Add(unit);
                }

                return (true, "", items);
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
                var items = new List<QBDInventorySite>();

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

                    items.Add(inventorySite);
                }

                return (true, "", items);
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

        private XmlElement MakeSimpleElement(XmlDocument doc, string tagName, string tagvalue)
        {
            XmlElement element = doc.CreateElement(tagName);
            element.InnerText = tagvalue;
            return element;
        }
    }
}
