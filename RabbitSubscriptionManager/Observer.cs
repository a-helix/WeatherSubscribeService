using Credentials;
using RabbitChat;
using System;
using System.Threading.Tasks;

namespace RabbitSubscription
{
    public class Observer : IObserver
    {
        private string _userID { get; }
        private string _location { get; }
        private int _responsesPerHour;
        public int _updateTime { get; set; }
        private Consumer _consumer;
        private Publisher _publisher;
        private int _hour = 1000 * 60 * 60;
        private JsonFileContent _configs;

        public Observer(string configPath, string userID, string location, int responsesPerHour, Consumer consumer, Publisher publisher)
        {
            _userID = userID;
            _location = location;
            _configs = new JsonFileContent(configPath);
            _responsesPerHour = responsesPerHour;
            _updateTime = (int)DateTime.UtcNow.Ticks;
            _consumer = consumer;
            _publisher = publisher;

        }

        public void Update()
        {
            
            int now = (int)DateTime.UtcNow.Ticks;
            if ((now-_updateTime) >= (_hour/_responsesPerHour))
            {
                _publisher.SendQueue(_configs.Value("WeatherApiQueueKey").ToString(), _location);
                string response = RabbitFeedback(_location);
                _publisher.SendQueue(_configs.Value("TelegramBotQueueKey").ToString(), response);
                _updateTime = (int)DateTime.UtcNow.Ticks;
            }

        }

        private string RabbitFeedback(string location)
        {
            string response = null;
            int timer = 0;
            while(response == null)
            {
                if (timer >= 1000 * 60)
                    response = "Service unavailable";
                response = _consumer.ReceiveQueue(_location);
                System.Threading.Thread.Sleep(10);
            }
            return response;
            
        }

        public bool SameTo(IObserver observer)
        {
            Observer observ = (Observer) observer;
            if (observ._userID == this._userID && observ._location == this._location)
                return true;
            return false;
        }
    }
}
