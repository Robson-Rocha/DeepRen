using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Xutils.ConsoleApp;

namespace DeepRen
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection()
                           .AddSingleton<IColoredConsole, ColoredConsole>()
                           .BuildServiceProvider();

            var app = new CommandLineApplication<Core>();
            app.Conventions.UseDefaultConventions()
                           .UseConstructorInjection(services);

            try
            {
                app.Execute(args);

            }
            catch (CommandParsingException)
            {
                // swallow
            }        }
    }
}
