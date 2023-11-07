using UploaderTest.Models;

namespace UploaderTest.Helpers
{
    public class VideoUploadQueue
    {
        private readonly Queue<Media> _queue = new Queue<Media>();
        private readonly object _lock = new object();

        public void Enqueue(Media media)
        {
            lock (_lock)
            {
                _queue.Enqueue(media);
            }
        }

        public Media Dequeue()
        {
            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    return _queue.Dequeue();
                }
                return null;
            }
        }
    }

}
