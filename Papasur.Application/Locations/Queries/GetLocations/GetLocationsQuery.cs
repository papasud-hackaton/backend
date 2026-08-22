using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Locations.Queries.GetLocations;

public sealed record GetLocationsQuery : IQuery<Result<IReadOnlyList<StorageLocationDto>>>;
