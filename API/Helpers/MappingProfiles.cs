using API.Dtos;
using AutoMapper;
using Core.Entities;

namespace API.Helpers
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<Product, ProductToReturnDto>()
                .ForMember(d => d.PictureUrl, o => o.MapFrom<ProductUrlResolver>());

            CreateMap<Core.Entities.Identity.Address, AddressDto>().ReverseMap();
            CreateMap<AddressDto, Core.Entities.OrderAggregate.Address>();

            CreateMap<ProductCreateDto, Product>();
            
            CreateMap<Photo, PhotoToReturnDto>()
                .ForMember(d => d.PictureUrl, 
                    o => o.MapFrom<PhotoUrlResolver>());

        }

    }
}
