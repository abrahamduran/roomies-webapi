using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Roomies.App.Models;
using Roomies.App.Persistence.Interfaces;

namespace Roomies.WebAPI.HostedService
{
    public class AutocompleteIndexer : BackgroundService
    {
        private readonly ChannelReader<IEnumerable<Autocomplete>> _channel;
        private readonly IAutocompleteRepository _repository;

        public AutocompleteIndexer(Channel<IEnumerable<Autocomplete>> channel, IAutocompleteRepository repository)
        {
            _channel = channel;
            _repository = repository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var items in _channel.ReadAllAsync(stoppingToken).ConfigureAwait(false))
                _repository.Index(items);
        }
    }
}
