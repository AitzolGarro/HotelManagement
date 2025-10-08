using FluentValidation;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Validators
{
    public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
    {
        public CreateReservationRequestValidator()
        {
            RuleFor(x => x.HotelId)
                .GreaterThan(0)
                .WithMessage("Hotel ID must be a valid positive number");

            RuleFor(x => x.RoomId)
                .GreaterThan(0)
                .WithMessage("Room ID must be a valid positive number");

            RuleFor(x => x.CheckInDate)
                .NotEmpty()
                .WithMessage("Check-in date is required")
                .GreaterThanOrEqualTo(DateTime.Today)
                .WithMessage("Check-in date cannot be in the past");

            RuleFor(x => x.CheckOutDate)
                .NotEmpty()
                .WithMessage("Check-out date is required")
                .GreaterThan(x => x.CheckInDate)
                .WithMessage("Check-out date must be after check-in date");

            RuleFor(x => x.NumberOfGuests)
                .GreaterThan(0)
                .WithMessage("Number of guests must be at least 1")
                .LessThanOrEqualTo(10)
                .WithMessage("Number of guests cannot exceed 10");

            RuleFor(x => x.TotalAmount)
                .GreaterThan(0)
                .WithMessage("Total amount must be greater than 0");

            RuleFor(x => x.Guest)
                .NotNull()
                .WithMessage("Guest information is required")
                .SetValidator(new GuestDtoValidator());

            RuleFor(x => x.SpecialRequests)
                .MaximumLength(1000)
                .WithMessage("Special requests cannot exceed 1000 characters");

            RuleFor(x => x.InternalNotes)
                .MaximumLength(1000)
                .WithMessage("Internal notes cannot exceed 1000 characters");
        }
    }

    public class UpdateReservationRequestValidator : AbstractValidator<UpdateReservationRequest>
    {
        public UpdateReservationRequestValidator()
        {
            RuleFor(x => x.CheckInDate)
                .NotEmpty()
                .WithMessage("Check-in date is required")
                .When(x => x.CheckInDate.HasValue);

            RuleFor(x => x.CheckOutDate)
                .NotEmpty()
                .WithMessage("Check-out date is required")
                .GreaterThan(x => x.CheckInDate)
                .WithMessage("Check-out date must be after check-in date")
                .When(x => x.CheckOutDate.HasValue && x.CheckInDate.HasValue);

            RuleFor(x => x.NumberOfGuests)
                .GreaterThan(0)
                .WithMessage("Number of guests must be at least 1")
                .LessThanOrEqualTo(10)
                .WithMessage("Number of guests cannot exceed 10")
                .When(x => x.NumberOfGuests.HasValue);

            RuleFor(x => x.TotalAmount)
                .GreaterThan(0)
                .WithMessage("Total amount must be greater than 0")
                .When(x => x.TotalAmount.HasValue);

            RuleFor(x => x.SpecialRequests)
                .MaximumLength(1000)
                .WithMessage("Special requests cannot exceed 1000 characters");

            RuleFor(x => x.InternalNotes)
                .MaximumLength(1000)
                .WithMessage("Internal notes cannot exceed 1000 characters");
        }
    }

    public class GuestDtoValidator : AbstractValidator<GuestDto>
    {
        public GuestDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("First name is required")
                .MaximumLength(100)
                .WithMessage("First name cannot exceed 100 characters");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Last name is required")
                .MaximumLength(100)
                .WithMessage("Last name cannot exceed 100 characters");

            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Please enter a valid email address")
                .MaximumLength(100)
                .WithMessage("Email cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Phone)
                .Matches(@"^[\+]?[1-9][\d]{0,15}$")
                .WithMessage("Please enter a valid phone number")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Address)
                .MaximumLength(500)
                .WithMessage("Address cannot exceed 500 characters");

            RuleFor(x => x.DocumentNumber)
                .MaximumLength(50)
                .WithMessage("Document number cannot exceed 50 characters");
        }
    }
}