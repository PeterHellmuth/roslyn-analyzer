namespace DemoProject
{
    public class Program
    {
        static void Main()
        {
            // DEMO001: Use UtcNow instead of Now
            var currentDate = DateTime.Now;

            // Examples of null operators
            string? nullableString = null;
            string nonNullableString = string.Empty;

            // DEMO002: Null-coalescing operator (??)
            var result = nullableString ?? "Default value";

            // DEMO002: Null-coalescing assignment operator (??=)
            nullableString ??= "Default value";

            // DEMO002: Null-forgiving operator (!)
            var length = nullableString!.Length;
        }
    }
}