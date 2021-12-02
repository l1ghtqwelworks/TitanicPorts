using System.Text;

namespace Titanic
{
    class Program
    {
        static void Main(string[] args)
        {
            ExecuteArgs(args);
        }

        static void ExecuteArgs(string[] args)
        {
            Console.WriteLine("Sailing Titanic");
            string command = args[0];
            switch (command)
            {
                case "get":
                    string target = args[1];
                    switch (target)
                    {
                        case "all":
                            Console.WriteLine("All");
                            break;
                    }
                    break;
            }
        }
    }
}