namespace Brizbee.Core.Transforms.Queries;

public class JobWithCustomer
{
    public int Job_Id { get; set; }

    public DateTime Job_CreatedAt { get; set; }

    public string Job_Name { get; set; }

    public string Job_Number { get; set; }

    public string Job_Description { get; set; }

    public string Job_QuickBooksCustomerJob { get; set; }

    public string Job_QuoteNumber { get; set; }

    public int Job_CustomerId { get; set; }

    public string Job_Status { get; set; }

    public int Customer_Id { get; set; }

    public DateTime Customer_CreatedAt { get; set; }

    public string Customer_Name { get; set; }

    public string Customer_Number { get; set; }

    public string Customer_Description { get; set; }

    public int Customer_OrganizationId { get; set; }
}
