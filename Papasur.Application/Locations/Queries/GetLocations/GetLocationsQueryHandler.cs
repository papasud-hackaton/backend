using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Locations.Ports;

namespace Papasur.Application.Locations.Queries.GetLocations;

public sealed class GetLocationsQueryHandler(IStorageLocationRepository locations)
    : IQueryHandler<GetLocationsQuery, Result<IReadOnlyList<StorageLocationDto>>>
{
    public async Task<Result<IReadOnlyList<StorageLocationDto>>> Handle(
        GetLocationsQuery query,
        CancellationToken cancellationToken)
    {
        var items = await locations.ListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<StorageLocationDto>>(
            [.. items.Select(l => new StorageLocationDto(l.Id, l.Code, l.Name, l.Type, l.TemperatureC))]);
    }
}
