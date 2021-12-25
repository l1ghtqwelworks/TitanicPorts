using Open.Nat;
using ShipwreckLib;
using System.Text;
using TitanicLib.Objects;

namespace Titanic
{
    class Program
    {
        static void Main(string[] args)
        {
            ExecuteArgs(String.Join(" ", args)).Wait();
        }

        static async Task ExecuteArgs(string args)
        {
            Console.WriteLine("Executing...");

            var commandManager = new TitanicConsoleCommandManager();
            var result = commandManager[command: args];
            if (result != null && result is Task)
                await (result as Task);
        }
    }
}