using Elearning.Application.Common;

namespace Elearning.Application.Students;

public interface IAdminStudentQueryHandler
{
    Task<CursorPage<StudentListItemDto>> ExecuteAsync(
        ListStudentsQuery request,
        CancellationToken cancellationToken);

    Task<StudentDetailDto> ExecuteAsync(GetStudentQuery query, CancellationToken cancellationToken);
}
