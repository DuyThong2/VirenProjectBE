using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Domains;

namespace Viren.Services.IntegrationEvents
{
    public interface IEventBusPublisher
    {
        Task PublishAsync(OutboxEvent evt, CancellationToken ct);
    }
}
