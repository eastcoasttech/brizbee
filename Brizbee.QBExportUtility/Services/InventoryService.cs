using Brizbee.Common.Models;
using Interop.QBXMLRP2;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace Brizbee.QBExportUtility.Services
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
        }

        public void BuildSalesReceiptAddRq(XmlDocument doc, XmlElement parent, List<InventoryAdjustment> adjustments)
        {
            // Create SalesReceiptAddRq.
            XmlElement request = doc.CreateElement("SalesReceiptAddRq");
            parent.AppendChild(request);

            foreach (var adjustment in adjustments)
            {
                // Create SalesReceiptAdd.
                XmlElement salesReceipt = doc.CreateElement("SalesReceiptAdd");
                request.AppendChild(salesReceipt);

                // Specify details.
                salesReceipt.AppendChild(MakeSimpleElement(doc, "TxnDate", adjustment.CreatedAt.ToString("yyyy-MM-dd")));

                // Create SalesReceiptLineAdd.
                XmlElement line = doc.CreateElement("SalesReceiptLineAdd");
                salesReceipt.AppendChild(line);

                // Specify details.
                line.AppendChild(MakeSimpleElement(doc, "Quantity", adjustment.Quantity.ToString()));
                line.AppendChild(MakeSimpleElement(doc, "UnitOfMeasure", adjustment.UnitOfMeasure));

                // Item reference is the ListID.
                XmlElement itemRef = doc.CreateElement("ItemRef");
                line.AppendChild(itemRef);

                itemRef.AppendChild(MakeSimpleElement(doc, "ListID", adjustment.QBDInventoryItem.ListId));

                // Inventory Site reference is the ListID.
                XmlElement inventorySiteRef = doc.CreateElement("InventorySiteRef");
                line.AppendChild(inventorySiteRef);

                inventorySiteRef.AppendChild(MakeSimpleElement(doc, "ListID", ""));

                // Inventory Site Location reference is the ListID.
                XmlElement inventorySiteLocationRef = doc.CreateElement("InventorySiteLocationRef");
                line.AppendChild(inventorySiteLocationRef);

                inventorySiteLocationRef.AppendChild(MakeSimpleElement(doc, "ListID", ""));
            }
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
                                inventoryItem.SalesDescription = xmlNode.InnerText;
                                break;
                            case "PurchaseDesc":
                                inventoryItem.PurchaseDescription = xmlNode.InnerText;
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

        private XmlElement MakeSimpleElement(XmlDocument doc, string tagName, string tagvalue)
        {
            XmlElement element = doc.CreateElement(tagName);
            element.InnerText = tagvalue;
            return element;
        }
    }
}
