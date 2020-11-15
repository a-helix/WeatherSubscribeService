using System.Collections.Generic;


namespace RabbitSubscription
{
    class Subject : ISubject
    {
        private List<IObserver> _buffer;

        public Subject()
        {
            _buffer = new List<IObserver>();
        }

        public void Attach(IObserver observer)
        {
            _buffer.Add(observer);
        }

        public void Detach(IObserver observer)
        {
            foreach(IObserver i in _buffer)
            {
                if (i.SameTo(observer))
                    _buffer.Remove(i);
            }
        }

        public void Notify()
        {
            foreach (IObserver i in _buffer)
            {
                i.Update();
            }
        }
    }
}
