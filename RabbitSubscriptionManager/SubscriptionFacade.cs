using Credentials;
using RabbitChat;
using System;
using System.Threading;

namespace RabbitSubscription
{
    public class SubscriptionFacade
    {
        private string _configPath;
        private JsonFileContent _configContent;
        private Consumer _consumer;
        private Publisher _publisher;
        private Subject _subject;

        public SubscriptionFacade(string configPath, Consumer consumer, Publisher publisher)
        {
            _configPath = configPath;
            _configContent = new JsonFileContent(configPath);
            _consumer = consumer;
            _publisher = publisher;
            _subject = new Subject();
        }

        public void Run()
        {
            Thread controlSubscriptions = new Thread(ControlSubscriptions);
            controlSubscriptions.Start();
            while(true)
            {
                _subject.Notify();
                Thread.Sleep(1000);
            }

        }

        private void ControlSubscriptions()
        {
            string rabbitFeedback;
            JsonStringContent feedbackContent;
            Observer observer;
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
                    }
                    else if (key.Equals(cancelSubscriptionKey))
                    {
                         _subject.Detach(observer);
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid value of {key}. Only {subscriptionKey} and {cancelSubscriptionKey} are excepted.");
                    }
                }   
            }
        }
    }
}
