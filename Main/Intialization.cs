using Credentials;
using DatabaseClient;
using Microsoft.Extensions.Hosting;
using RabbitChat;
using RabbitSubscription;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher
{
    public class Intialization : BackgroundService
    {
        JsonFileContent configContent;
        Consumer consumer;
        Publisher publisher;
        MySqlDatabaseConnection connection;
        MySqlDatabaseClient databaseClient;
        SubscriptionFacade facade;

        public Intialization()
        {
            var configPath = Path.Join(Directory.GetCurrentDirectory(), "configs.json");
            configContent = new JsonFileContent(configPath);
            consumer = new Consumer(
            configContent.Value("RabbitUrl").ToString(),
            configContent.Value("RabbitLogin").ToString(),
            configContent.Value("RabbitPassword").ToString()
            );
            publisher = new Publisher(
                configContent.Value("RabbitUrl").ToString(),
                configContent.Value("RabbitLogin").ToString(),
                configContent.Value("RabbitPassword").ToString()
                );
            connection = new MySqlDatabaseConnection(
                configContent.Value("MySqlServer").ToString(),
                configContent.Value("MySqlDatabase").ToString(),
                configContent.Value("MySqlLogin").ToString(),
                configContent.Value("MySqlPassword").ToString()
                );
            databaseClient = new MySqlDatabaseClient(connection);
            facade = new SubscriptionFacade("configs.json", consumer, publisher, databaseClient);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return facade.Run();
        }
    }
}
