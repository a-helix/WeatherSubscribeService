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
        private MySqlDatabaseClient _databaseClient;

        public SubscriptionFacade(string configPath, Consumer consumer, Publisher publisher, IRepository<Subscription> database)
        {
            _configPath = configPath;
            _configContent = new JsonFileContent(configPath);
            _consumer = consumer;
            _publisher = publisher;
            _subject = new Subject();
            Restart();
            
        }

        public void Run()
        {
            Thread controlSubscriptions = new Thread(ControlSubscriptions);
            controlSubscriptions.Start();
            while(true)
            {
                _subject.Notify();
                Thread.Sleep(1000);
                _databaseClient.Save();
            }
        }

        private void ControlSubscriptions()
        {
            string rabbitFeedback;
            JsonStringContent feedbackContent;
            Observer observer;
            Subscription subscription;
            string subscriptionQueueKey = _configContent.Value("SubscriotionQueueKey").ToString();
            string subscriptionKey = _configContent.Value("SubscriotionKey").ToString();
            string cancelSubscriptionKey = _configContent.Value("CanceledSubscriotionKey").ToString();
            while (true)
            {
                rabbitFeedback = _consumer.ReceiveQueue(subscriptionQueueKey);
                if(rabbitFeedback == null)
                {
                    Thread.Sleep(100);
                    continue;
                }
                feedbackContent = new JsonStringContent(rabbitFeedback);
                string key = feedbackContent.Value("Subscription").ToString();
                observer = new Observer(_configPath,
                        feedbackContent.Value("UserID").ToString(),
                        feedbackContent.Value("Location").ToString(),
                        (int)feedbackContent.Value("ResponsesPerHour"),
                        _consumer,
                        _publisher);

                lock (_subject)
                {
                    if (key.Equals(subscriptionKey))
                    {
                        _subject.Attach(observer);
                        subscription = new Subscription();
                        subscription.ID = feedbackContent.Value("ID").ToString();
                        subscription.UserID = feedbackContent.Value("UserID").ToString();
                        subscription.Location = feedbackContent.Value("Location").ToString();
                        subscription.RequestsPerHour = (int)feedbackContent.Value("ResponsesPerHour");
                        subscription.Active = true;
                        subscription.Status = "Started";
                        subscription.CreatedAt = DateTime.UtcNow.Ticks;
                        subscription.ExpiredAt = 0;
                        subscription.LastSent = DateTime.UtcNow.Ticks;
                        _databaseClient.Create(subscription);
                    }
                    else if (key.Equals(cancelSubscriptionKey))
                    {
                        subscription = _databaseClient.Read(feedbackContent.Value("ID").ToString());
                        subscription.Active = false;
                        subscription.Status = "Stoped";
                        subscription.ExpiredAt = DateTime.UtcNow.Ticks;
                        _databaseClient.Delete(feedbackContent.Value("ID").ToString());
                        _databaseClient.Create(subscription);
                        _subject.Detach(observer);
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid value of {key}. Only {subscriptionKey} and {cancelSubscriptionKey} are excepted.");
                    }
                }   
            }
        }

        private void Restart()
        {
            var activeSubscriptions = _databaseClient.AllActiveSubscriptions();
            Observer observer;
            foreach(var i in activeSubscriptions)
            {
                observer = new Observer(_configPath,
                                        i.UserID,
                                        i.Location,
                                        i.RequestsPerHour,
                                        _consumer,
                                        _publisher);
                observer._updateTime = (int)i.LastSent;
                _subject.Attach(observer);
            }
        }
    }
}
