using Elearning.Application.Common;

namespace Elearning.Application.Progress;

public interface IAdminProgressQueryHandler
{
    Task<CursorPage<ProgressRowDto>> ExecuteAsync(
        GetAdminProgressQuery request,
        CancellationToken cancellationToken);
}
