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
        private IUnitOfWork<Subscription> _databaseClient;
        private string _subscriptionQueueKey;
        private string _subscriptionKey;
        private string _cancelSubscriptionKey;

        public SubscriptionFacade(string configPath, Consumer consumer, Publisher publisher, IUnitOfWork<Subscription> database)
        {
            _configPath = configPath;
            _configContent = new JsonFileContent(configPath);
            _consumer = consumer;
            _publisher = publisher;
            _subject = new Subject();
            _databaseClient = database;
            _subscriptionQueueKey = _configContent.Value("SubscriotionQueueKey").ToString();
            _subscriptionKey = _configContent.Value("SubscriotionKey").ToString();
            _cancelSubscriptionKey = _configContent.Value("CanceledSubscriotionKey").ToString();
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
                _databaseClient.Save();
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
                string key = feedbackContent.Value("Subscription").ToString();
                observer = new Observer(_configPath,
                        feedbackContent.Value("UserID").ToString(),
                        feedbackContent.Value("Location").ToString(),
                        (int)feedbackContent.Value("ResponsesPerHour"),
                        _consumer,
                        _publisher);

                SubscriptionStrategy strategy;
                if (key.Equals(_subscriptionKey))
                {
                    strategy = new SubscribeStrategy(_databaseClient);
                    strategy.Execute(feedbackContent);
                    _subject.Attach(observer);
                    continue;
                }
                if (key.Equals(_cancelSubscriptionKey))
                {
                    strategy = new AbortedSubscriptionStrategy(_databaseClient);
                    strategy.Execute(feedbackContent);
                    _subject.Detach(observer);
                    continue;
                }
                throw new ArgumentException($"Invalid value of {key}. Only {_subscriptionKey} and {_cancelSubscriptionKey} are excepted.");
            }
        }

        private void Restart()
        {
            var activeSubscriptions = _databaseClient.AllActiveSubscriptions();
            Observer observer;
            foreach (var i in activeSubscriptions)
            {
                observer = new Observer(_configPath, i.UserID, i.Location, i.RequestsPerHour, _consumer, _publisher);
                observer._updateTime = (int)i.LastSent;
                _subject.Attach(observer);
            }
        }
    }


    public abstract class SubscriptionStrategy
    {
        protected IUnitOfWork<Subscription> unitOfWork;

        public SubscriptionStrategy(IUnitOfWork<Subscription> unit)
        {
            unitOfWork = unit;
        }

        public abstract void Execute(JsonStringContent feedbackContent);
    }

    public class SubscribeStrategy : SubscriptionStrategy
    {
        public SubscribeStrategy(IUnitOfWork<Subscription> unit) : base(unit)
        {

        }

        public override void Execute(JsonStringContent feedbackContent)
        {
            Subscription subscription = new Subscription();
            subscription.ID = feedbackContent.Value("ID").ToString();
            subscription.UserID = feedbackContent.Value("UserID").ToString();
            subscription.Location = feedbackContent.Value("Location").ToString();
            subscription.RequestsPerHour = (int)feedbackContent.Value("ResponsesPerHour");
            subscription.Active = true;
            subscription.Status = "Started";
            subscription.CreatedAt = DateTime.UtcNow.Ticks;
            subscription.ExpiredAt = 0;
            subscription.LastSent = DateTime.UtcNow.Ticks;
            unitOfWork.Create(subscription);
        }
    }

    public class AbortedSubscriptionStrategy : SubscriptionStrategy
    {
        public AbortedSubscriptionStrategy(IUnitOfWork<Subscription> unit) : base(unit)
        {

        }

        public override void Execute(JsonStringContent feedbackContent)
        {
            Subscription subscription = new Subscription();
            subscription = unitOfWork.Read(feedbackContent.Value("ID").ToString());
            subscription.Active = false;
            subscription.Status = "Stoped";
            subscription.ExpiredAt = DateTime.UtcNow.Ticks;
            unitOfWork.Delete(feedbackContent.Value("ID").ToString());
            unitOfWork.Create(subscription);
        }
    }
}
