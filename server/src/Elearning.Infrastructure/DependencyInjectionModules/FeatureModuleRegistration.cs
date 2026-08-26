using Elearning.Application.Courses;
using Elearning.Application.Dashboard;
using Elearning.Application.Enrollments;
using Elearning.Application.Exercises;
using Elearning.Application.Lessons;
using Elearning.Application.Progress;
using Elearning.Application.Students;
using Elearning.Infrastructure.Authorization;
using Elearning.Infrastructure.Courses;
using Elearning.Infrastructure.Dashboard;
using Elearning.Infrastructure.Exercises;
using Elearning.Infrastructure.Lessons;
using Elearning.Infrastructure.Progress;
using Elearning.Infrastructure.Students;
using Microsoft.Extensions.DependencyInjection;

namespace Elearning.Infrastructure.DependencyInjectionModules;

internal static class FeatureModuleRegistration
{
    public static IServiceCollection AddFeatureModules(this IServiceCollection services)
    {
        services.AddScoped<IAdminCourseQueryHandler, AdminCourseQueryHandler>();
        services.AddScoped<IAdminCourseCommandHandler, AdminCourseCommandHandler>();
        services.AddScoped<IStudentCourseCatalogQueryHandler, StudentCourseCatalogQueryHandler>();
        services.AddScoped<IStudentCourseDetailQueryHandler, StudentCourseDetailQueryHandler>();
        services.AddScoped<IAdminLessonQueryHandler, AdminLessonQueryHandler>();
        services.AddScoped<IAdminLessonCommandHandler, AdminLessonCommandHandler>();
        services.AddScoped<IStudentLessonQueryHandler, StudentLessonQueryHandler>();
        services.AddScoped<ILessonProgressCommandHandler, LessonProgressCommandHandler>();
        services.AddScoped<IAdminQuestionQueryHandler, AdminQuestionQueryHandler>();
        services.AddScoped<IAdminQuestionCommandHandler, AdminQuestionCommandHandler>();
        services.AddScoped<ISubmitAnswerCommandHandler, SubmitAnswerCommandHandler>();
        services.AddScoped<StudentAccountStore>();
        services.AddScoped<IAdminStudentQueryHandler, AdminStudentQueryHandler>();
        services.AddScoped<IStudentAccountCommandHandler, StudentAccountCommandHandler>();
        services.AddScoped<IEnrollmentCommandHandler, EnrollmentCommandHandler>();
        services.AddScoped<IAdminProgressQueryHandler, AdminProgressQueryHandler>();
        services.AddScoped<IStudentProgressQueryHandler, StudentProgressQueryHandler>();
        services.AddScoped<IAdminDashboardQueryHandler, AdminDashboardQueryHandler>();
        services.AddScoped<ActiveStudentPolicy>();
        services.AddScoped<StudentLessonAccessPolicy>();
        return services;
    }
}
