using System.Threading.Tasks;

namespace RabbitSubscription
{
    public interface IObserver
    {
        public void Update();
    }
}
