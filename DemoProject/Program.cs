namespace DemoProject
{
    public class Program
    {
        static void Main()
        {
            // DEMO002: Use UtcNow instead of Now
            var currentDate = DateTime.UtcNow;

            // Examples of null operators
            string? nullableString = null;
            string nonNullableString = string.Empty;

            // DEMO003: Null-coalescing operator (??)
            var result = nullableString ?? "Default value";

            // DEMO004: Null-coalescing assignment operator (??=)
            nullableString ??= "Assigned value";

            // DEMO005: Null-forgiving operator (!)
            var length = nullableString!.Length;
        }
    }
}