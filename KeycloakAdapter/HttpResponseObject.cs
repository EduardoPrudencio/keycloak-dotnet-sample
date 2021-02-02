namespace KeycloakAdapter
{
    public class HttpResponseObject<T>
    {
        private int _statusCode;
        private T _object;

        public int StatusCode { get => _statusCode; }
        public T Object { get => _object; }

        public void Create(int statusCode, T item)
        {
            _statusCode = statusCode;
            _object = item;
        }
    }
}
