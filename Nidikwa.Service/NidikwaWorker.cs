using Microsoft.Extensions.Options;

namespace Nidikwa.Service
{
    public class NidikwaWorkerConfiguration
    {
        public CancellationTokenSource AppStopper { get; set; } = default!;
    }

    internal class NidikwaWorker : BackgroundService
    {
        private readonly IAudioService _audioService;
        private readonly IOptions<NidikwaWorkerConfiguration> _configuration;

        public NidikwaWorker(IAudioService audioService, IOptions<NidikwaWorkerConfiguration> configuration)
        {
            _audioService = audioService;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // TODO: find a way to listen to requests and start recording

            _configuration.Value.AppStopper.Cancel();
        }
    }
}
