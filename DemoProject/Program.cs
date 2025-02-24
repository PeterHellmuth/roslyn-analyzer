namespace DemoProject
{
    public class Program
    {
        static void Main()
        {
            // DEMO002: Use UtcNow instead of Now
            var currentDate = DateTime.Now; // This should trigger a diagnostic

            // Examples of null operators
            string? nullableString = null;
            string nonNullableString = "Hello";

            // DEMO003: Null-coalescing operator (??)
            var result = nullableString ?? "Default value"; // This should trigger a diagnostic

            // DEMO004: Null-coalescing assignment operator (??=)
            nullableString ??= "Assigned value"; // This should trigger a diagnostic

            // DEMO005: Null-forgiving operator (!)
            var length = nullableString!.Length; // This should trigger a diagnostic
        }
    }
}