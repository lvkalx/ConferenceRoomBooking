using AutoMapper;
using ConferenceRoomBooking.Application.DTOs.Bookings;
using ConferenceRoomBooking.Application.DTOs.Rooms;
using ConferenceRoomBooking.Domain.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ConferenceRoomBooking.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Service, ServiceDto>();

        CreateMap<ConferenceRoom, RoomDto>()
            .ForMember(dest => dest.AvailableServices, opt => opt.MapFrom(src => src.AvailableServices));

        CreateMap<Booking, BookingDto>()
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Room != null ? src.Room.Name : string.Empty))
            .ForMember(dest => dest.SelectedServices, opt => opt.MapFrom(src => src.SelectedServices.Select(s => s.Name)));
    }
}