using AutoMapper;
using GPAHub.Application.DTOs.Course;
using GPAHub.Application.DTOs.GradeScale;
using GPAHub.Application.DTOs.History;
using GPAHub.Application.DTOs.Subscription;
using GPAHub.Domain.Entities;

namespace GPAHub.Application.Mappings;

public class MappingProfile : Profile
{
    private const int MaxMappingDepth = 32;

    public MappingProfile()
    {
        CreateMap<GradeDefinition, GradeDefinitionItemDto>()
            .MaxDepth(MaxMappingDepth);

        CreateMap<GradeScale, GradeScaleDto>()
            .MaxDepth(MaxMappingDepth);

        CreateMap<Course, CourseDto>()
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
