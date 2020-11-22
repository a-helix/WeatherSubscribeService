using System;
using System.Threading;
using Credentials;
using RabbitChat;
using DatabaseClient;
using Repository;

namespace RabbitSubscription
{
    public class SubscriptionFacade
    {
        private string _configPath;
        private JsonFileContent _configContent;
        private Consumer _consumer;
        private Publisher _publisher;
        private Subject _subject;
        private SubscriptionUnitOfWork _unit;
        private MySqlDatabaseClient _dbClien;
        private string _subscriptionQueueKey;
        private string _subscriptionKey;
        private string _cancelSubscriptionKey;

        public SubscriptionFacade(string configPath, Consumer consumer, Publisher publisher, MySqlDatabaseClient database)
        {
            _configPath = configPath;
            _configContent = new JsonFileContent(configPath);
            _consumer = consumer;
            _publisher = publisher;
            _subject = new Subject();
            _dbClien = database;
            _subscriptionQueueKey = Convert.ToString(_configContent.Value("SubscriotionQueueKey"));
            _subscriptionKey = Convert.ToString(_configContent.Value("SubscriotionKey"));
            _cancelSubscriptionKey = Convert.ToString(_configContent.Value("CanceledSubscriotionKey"));
            _unit = new SubscriptionUnitOfWork(_dbClien);
            Restart();
        }

        public void Run()
        {
            Thread controlSubscriptions = new Thread(ControlSubscriptions);
            controlSubscriptions.Start();
            while (true)
            {
                _subject.Notify();
                Thread.Sleep(1000);
                _unit.Save();
            }
        }

        private void ControlSubscriptions()
        {
            string rabbitFeedback;
            JsonStringContent feedbackContent;
            Observer observer;

            while (true)
            {
                rabbitFeedback = _consumer.ReceiveQueue(_subscriptionQueueKey);
                if (rabbitFeedback == null)
                {
                    Thread.Sleep(100);
                    continue;
                }

                feedbackContent = new JsonStringContent(rabbitFeedback);
                string key = Convert.ToString(feedbackContent.Value("Subscription"));
                observer = new Observer(_configPath,
                        Convert.ToString(feedbackContent.Value("UserID")),
                        Convert.ToString(feedbackContent.Value("Location")),
                        Convert.ToInt32(feedbackContent.Value("ResponsesPerHour")),
                        _consumer,
                        _publisher);

                SubscriptionStrategy strategy;

                if (key.Equals(_subscriptionKey))
                {
                    strategy = new SubscribeStrategy(_unit, _subject, observer);
                    strategy.Execute(feedbackContent);
                    continue;
                }
                if (key.Equals(_cancelSubscriptionKey))
                {
                    strategy = new AbortedSubscriptionStrategy(_unit, _subject, observer);
                    strategy.Execute(feedbackContent);
                    continue;
                }
                throw new ArgumentException($"Invalid value of {key}. Only {_subscriptionKey} and {_cancelSubscriptionKey} are excepted.");
            }
        }

        private void Restart()
        {
            var activeSubscriptions = _dbClien.AllActive();
            Observer observer;
            foreach (var i in activeSubscriptions)
            {
                observer = new Observer(_configPath, i.UserID, i.Location, i.RequestsPerHour, _consumer, _publisher);
                observer._updateTime = Convert.ToInt32(i.LastSent);
                _subject.Attach(observer);
            }
        }
    }
}
