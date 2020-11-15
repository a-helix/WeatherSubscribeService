using System.Threading.Tasks;

namespace RabbitSubscription
{
    public interface IObserver
    {
        public void Update();

        public bool SameTo(IObserver observer);
    }
}
