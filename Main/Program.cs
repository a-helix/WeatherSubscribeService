using Credentials;
using DatabaseClient;
using RabbitChat;
using RabbitSubscription;

namespace WeatherSubscribeService
{
    public class Program
    {
        JsonFileContent configContent;
        Consumer consumer;
        Publisher publisher;
        MySqlDatabaseClient databaseClient;
        SubscriptionFacade facade;

        public Program()
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

        public void Start()
        {
            facade.Run();
        }
        
        

        public void Main(string[] args)
        {
            var launcer = new Program();
            launcer.Start();
        }
    }
}
