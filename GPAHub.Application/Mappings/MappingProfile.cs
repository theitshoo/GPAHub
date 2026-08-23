using AutoMapper;
using GPAHub.Application.DTOs.Course;
using GPAHub.Application.DTOs.GradeScale;
using GPAHub.Application.DTOs.History;
using GPAHub.Application.DTOs.Student;
using GPAHub.Application.DTOs.Subscription;
using GPAHub.Domain.Entities;

namespace GPAHub.Application.Mappings;

public class MappingProfile : Profile
{
    private const int MaxMappingDepth = 32;

    public MappingProfile()
    {
        CreateMap<GPAHub.Domain.ValueObjects.CreditHours, decimal>()
            .ConvertUsing(value => value.Value);

        CreateMap<GradeDefinition, GradeDefinitionItemDto>()
            .MaxDepth(MaxMappingDepth);

        CreateMap<GradeScale, GradeScaleDto>()
            .MaxDepth(MaxMappingDepth);

        CreateMap<Course, CourseDto>()
            .ForMember(d => d.CreditHours, o => o.MapFrom(s => s.CreditHours.Value))
            .MaxDepth(MaxMappingDepth);

        CreateMap<Student, StudentProfileDto>()
            .MaxDepth(MaxMappingDepth);

        CreateMap<GpaRecordCourseLine, GpaRecordLineDto>()
            .MaxDepth(MaxMappingDepth);

        CreateMap<GpaRecord, GpaRecordSummaryDto>()
            .MaxDepth(MaxMappingDepth);

        CreateMap<GpaRecord, GpaRecordDetailDto>()
            .MaxDepth(MaxMappingDepth);

        CreateMap<TargetPlanUpcomingCourse, UpcomingCourseLineDto>()
            .MaxDepth(MaxMappingDepth);

        CreateMap<TargetPlan, TargetPlanSummaryDto>()
            .MaxDepth(MaxMappingDepth);

        CreateMap<TargetPlan, TargetPlanDetailDto>()
            .MaxDepth(MaxMappingDepth);

        CreateMap<Payment, PaymentDto>()
            .MaxDepth(MaxMappingDepth);

        CreateMap<Subscription, SubscriptionDto>()
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActiveAsOf(DateTimeOffset.UtcNow)))
            .MaxDepth(MaxMappingDepth);
    }
}
