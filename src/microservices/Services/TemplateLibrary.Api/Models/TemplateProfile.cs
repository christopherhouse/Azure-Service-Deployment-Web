using AutoMapper;
using AzureDeploymentSaaS.Shared.Contracts.Models;
using Data = AzureDeploymentSaaS.Shared.Infrastructure.Data;

namespace TemplateLibrary.Api.Models;

/// <summary>
/// AutoMapper profile for TemplateLibrary.Api
/// </summary>
public class TemplateProfile : Profile
{
    public TemplateProfile()
    {
        // Map between domain entities and DTOs
        CreateMap<Data.Template, TemplateDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.TemplateContent, opt => opt.MapFrom(src => src.TemplateContent))
            .ForMember(dest => dest.ParametersContent, opt => opt.MapFrom(src => src.ParametersContent))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
            .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => src.ModifiedAt))
            .ForMember(dest => dest.Version, opt => opt.MapFrom(src => src.Version))
            .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => src.IsPublic));

        CreateMap<TemplateDto, Data.Template>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.TemplateContent, opt => opt.MapFrom(src => src.TemplateContent))
            .ForMember(dest => dest.ParametersContent, opt => opt.MapFrom(src => src.ParametersContent))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
            .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => src.ModifiedAt))
            .ForMember(dest => dest.Version, opt => opt.MapFrom(src => src.Version))
            .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => src.IsPublic));
    }
}