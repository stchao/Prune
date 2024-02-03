using Microsoft.Extensions.DependencyInjection;
using Prune.Extensions;

namespace Prune
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .ConfigureLogAndServices()
                .BuildServiceProvider();

            Console.WriteLine("Hello, World!");
        }
    }
}
