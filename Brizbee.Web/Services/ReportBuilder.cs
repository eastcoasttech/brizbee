using iTextSharp;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;

namespace Brizbee.Services
{
    public class ReportBuilder
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        public byte[] PunchesByUserAsPdf(int[] userIds, DateTime min, DateTime max)
        {
            var buffer = new byte[0];
            var output = new MemoryStream();

            // Create an instance of the document class which represents the PDF document itself.
            using (var document = new Document(PageSize.LETTER, 25, 25, 30, 30))
            {
                var writer = PdfWriter.GetInstance(document, output);

                // Add meta information to the document
                document.AddAuthor("Micke Blomquist");
                document.AddCreator("Sample application using iTextSharp");
                document.AddKeywords("PDF tutorial education");
                document.AddSubject("Document subject - Describing the steps creating a PDF document");
                document.AddTitle("The document title - PDF creation using iTextSharp");

                // Open the document to enable you to write to the document
                document.Open();

                // Build table of punches
                PdfPTable table = new PdfPTable(3);

                var users = db.Users.Where(u => userIds.Contains(u.Id));
                foreach (var user in users)
                {
                    var nameCell = new PdfPCell(new Phrase(user.Name));
                    nameCell.Colspan = 5;
                    table.AddCell(nameCell);

                    var punches = db.Punches.Where(p => p.UserId == user.Id);
                    foreach (var punch in punches)
                    {
                        table.AddCell(punch.InAt.ToString());
                        table.AddCell(punch.OutAt.ToString());
                        table.AddCell(string.Format("{0} - {1}", punch.Task.Number, punch.Task.Name));
                        table.AddCell(string.Format("{0} - {1}", punch.Task.Job.Number, punch.Task.Job.Name));
                        table.AddCell(string.Format("{0} - {1}", punch.Task.Job.Customer.Number, punch.Task.Job.Customer.Name));
                    }

                    // Page break
                    if (users.Last() == user)
                    {
                        document.NewPage();
                    }
                }
                
                // Add a message for no users
                if (users.Count() == 0)
                {
                    document.Add(new Phrase("There are no users with those IDs"));
                }

                document.Add(table);

                // Make sure data has been written
                writer.Flush();

                // Close the document
                document.Close();
            }

            buffer = output.GetBuffer();
            return buffer;
        }
    }
}