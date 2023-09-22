namespace Brizbee.Dashboard.Server
{
    public class ODataSingleResponse<T> where T : class
    {
        public T Value { get; set; }
    }
}
