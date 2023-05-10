using AutoMapper;
using SwaggreAnotation.Dtos;
using SwaggreAnotation.Entittes;

namespace SwaggreAnotation
{
    public class ProfileMapper : Profile
    {
        public ProfileMapper() 
        {
            CreateMap<Human, HUmanDto>();
        }
    }


}
