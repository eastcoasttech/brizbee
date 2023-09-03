namespace Brizbee.Api.Serialization
{
    public class VoucherTypes
    {
        public static readonly List<VoucherType> All = new()
        {
            new VoucherType
            {
                Name = "Deposit",
                Abbreviation = "DEP"
            }
        };
    }
}
