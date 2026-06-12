namespace FirstApi.Services
{
    public class LoadBalancerService
    {
        private readonly string[] _servers = {
            "http://localhost:8000",
            "http://localhost:8001",
            "http://localhost:8002"
        };

        private int _currentIndex = 0;

        public string GetNextServer()
        {
            var server = _servers[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _servers.Length;
            return server;
        }
    }
}
