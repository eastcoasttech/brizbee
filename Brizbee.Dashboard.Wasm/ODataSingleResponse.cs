namespace Brizbee.Dashboard.Wasm
{
    public class ODataSingleResponse<T> where T : class
    {
        public T Value { get; set; }
    }
}
