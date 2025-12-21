using FluentValidation;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Validators
{
    public class CreateHotelRequestValidator : AbstractValidator<CreateHotelRequest>
    {
        public CreateHotelRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Hotel name is required")
                .MaximumLength(200)
                .WithMessage("Hotel name cannot exceed 200 characters");

            RuleFor(x => x.Address)
                .MaximumLength(500)
                .WithMessage("Address cannot exceed 500 characters");

            RuleFor(x => x.Phone)
                .Matches(@"^[\+]?[1-9][\d]{0,15}$")
                .WithMessage("Please enter a valid phone number")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Please enter a valid email address")
                .MaximumLength(100)
                .WithMessage("Email cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Email));
        }
    }

    public class UpdateHotelRequestValidator : AbstractValidator<UpdateHotelRequest>
    {
        public UpdateHotelRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Hotel name is required")
                .MaximumLength(200)
                .WithMessage("Hotel name cannot exceed 200 characters")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.Address)
                .MaximumLength(500)
                .WithMessage("Address cannot exceed 500 characters");

            RuleFor(x => x.Phone)
                .Matches(@"^[\+]?[1-9][\d]{0,15}$")
                .WithMessage("Please enter a valid phone number")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Please enter a valid email address")
                .MaximumLength(100)
                .WithMessage("Email cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Email));
        }
    }

    public class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
    {
        public CreateRoomRequestValidator()
        {
            RuleFor(x => x.HotelId)
                .GreaterThan(0)
                .WithMessage("Hotel ID must be a valid positive number");

            RuleFor(x => x.RoomNumber)
                .NotEmpty()
                .WithMessage("Room number is required")
                .MaximumLength(10)
                .WithMessage("Room number cannot exceed 10 characters");

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Please select a valid room type");

            RuleFor(x => x.Capacity)
                .GreaterThan(0)
                .WithMessage("Room capacity must be at least 1")
                .LessThanOrEqualTo(20)
                .WithMessage("Room capacity cannot exceed 20");

            RuleFor(x => x.BaseRate)
                .GreaterThan(0)
                .WithMessage("Base rate must be greater than 0")
                .LessThan(10000)
                .WithMessage("Base rate cannot exceed $10,000");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description cannot exceed 1000 characters");
        }
    }

    public class UpdateRoomRequestValidator : AbstractValidator<UpdateRoomRequest>
    {
        public UpdateRoomRequestValidator()
        {
            RuleFor(x => x.RoomNumber)
                .NotEmpty()
                .WithMessage("Room number is required")
                .MaximumLength(10)
                .WithMessage("Room number cannot exceed 10 characters")
                .When(x => !string.IsNullOrEmpty(x.RoomNumber));

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Please select a valid room type");

            RuleFor(x => x.Capacity)
                .GreaterThan(0)
                .WithMessage("Room capacity must be at least 1")
                .LessThanOrEqualTo(20)
                .WithMessage("Room capacity cannot exceed 20");

            RuleFor(x => x.BaseRate)
                .GreaterThan(0)
                .WithMessage("Base rate must be greater than 0")
                .LessThan(10000)
                .WithMessage("Base rate cannot exceed $10,000");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Please select a valid room status");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description cannot exceed 1000 characters");
        }
    }
}