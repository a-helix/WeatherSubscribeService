using Credentials;
using DatabaseClient;
using Microsoft.Extensions.Hosting;
using RabbitChat;
using RabbitSubscription;
using System.Threading;
using System.Threading.Tasks;

namespace Main
{
    public class Intialization : BackgroundService
    {
        JsonFileContent configContent;
        Consumer consumer;
        Publisher publisher;
        MySqlDatabaseClient databaseClient;
        SubscriptionFacade facade;

        public Intialization()
        {
            configContent = new JsonFileContent("configs.json");
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
            databaseClient = new MySqlDatabaseClient(
                configContent.Value("MySqlServer").ToString(),
                configContent.Value("MySqlDatabase").ToString(),
                configContent.Value("MySqlLogin").ToString(),
                configContent.Value("MySqlPassword").ToString()
                );
            facade = new SubscriptionFacade("configs.json", consumer, publisher, databaseClient);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return facade.Run();
        }
    }
}
