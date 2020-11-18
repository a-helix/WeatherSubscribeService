using Credentials;
using DatabaseClient;
using RabbitChat;
using RabbitSubscription;

namespace WeatherSubscribeService
{
    public class Program
    {
        static JsonFileContent configContent = new JsonFileContent("configs.json");
        static Consumer consumer = new Consumer(
            configContent.Value("RabbitUrl").ToString(),
            configContent.Value("RabbitLogin").ToString(),
            configContent.Value("RabbitPassword").ToString()
            );
        static Publisher publisher = new Publisher(
            configContent.Value("RabbitUrl").ToString(),
            configContent.Value("RabbitLogin").ToString(),
            configContent.Value("RabbitPassword").ToString()
            );
        static MySqlDatabaseClient databaseClient = new MySqlDatabaseClient(
            configContent.Value("MySqlServer").ToString(),
            configContent.Value("MySqlDatabase").ToString(),
            configContent.Value("MySqlLogin").ToString(),
            configContent.Value("MySqlPassword").ToString()
            );
        static SubscriptionFacade facade = new SubscriptionFacade("configs.json", consumer, publisher, databaseClient);

        public static void Main(string[] args)
        {
            facade.Run();
        }
    }
}
