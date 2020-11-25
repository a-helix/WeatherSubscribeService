using Credentials;
using DatabaseClient;
using System;

namespace RabbitSubscription
{
    public abstract class SubscriptionStrategy
    {
        protected SubscriptionUnitOfWork unitOfWork;
        protected Subject subject;
        protected Observer observer;

        public SubscriptionStrategy(SubscriptionUnitOfWork unit, Subject currentSubject, Observer currentObserver)
        {
            unitOfWork = unit;
            subject = currentSubject;
            observer = currentObserver;
        }

        public abstract void Execute(JsonStringContent feedbackContent);
    }

    public class SubscribeStrategy : SubscriptionStrategy
    {
        public SubscribeStrategy(SubscriptionUnitOfWork unit, Subject currentSubject, Observer currentObserver) : base(unit, currentSubject, currentObserver)
        {

        }

        public override void Execute(JsonStringContent feedbackContent)
        {
            subject.Attach(observer);
            Subscription subscription = new Subscription();
            subscription.ID = Convert.ToString(feedbackContent.Value("ID"));
            subscription.UserID = Convert.ToString(feedbackContent.Value("UserID"));
            subscription.Location = Convert.ToString(feedbackContent.Value("Location"));
            subscription.RequestsPerHour = Convert.ToInt32(feedbackContent.Value("ResponsesPerHour"));
            subscription.Active = true;
            subscription.Status = "Started";
            subscription.CreatedAt = DateTime.UtcNow.Ticks;
            subscription.ExpiredAt = 0;
            subscription.LastSent = DateTime.UtcNow.Ticks;
            unitOfWork.Commit();
            unitOfWork.Add(subscription);
        }
    }

    public class AbortedSubscriptionStrategy : SubscriptionStrategy
    {
        public AbortedSubscriptionStrategy(SubscriptionUnitOfWork unit, Subject currentSubject, Observer currentObserver) : base(unit, currentSubject, currentObserver)
        {

        }

        public override void Execute(JsonStringContent feedbackContent)
        {
            Subscription subscription = unitOfWork.GetByID(Convert.ToString(feedbackContent.Value("ID")));
            subscription.Active = false;
            subscription.Status = "Stoped";
            subscription.ExpiredAt = DateTime.UtcNow.Ticks;
            unitOfWork.Remove(Convert.ToString(feedbackContent.Value("ID")));
            unitOfWork.Add(subscription);
            unitOfWork.Commit();
            subject.Detach(observer);
        }
    }
}
