namespace DemoProject
{
    public class Program
    {
        static void Main()
        {
            // DEMO001: Methods should have PascalCase names
            var calculator = new Calculator();
            calculator.addNumbers(5, 3); // DEMO001 method invocation

            // DEMO002: Use UtcNow instead of Now
            var currentDate = DateTime.Now; 
        }
    }

    public class Calculator
    {
        // DEMO001: Methods should have PascalCase names
        public int addNumbers(int a, int b) => a + b; // DEMO001 method declaration
    }
}