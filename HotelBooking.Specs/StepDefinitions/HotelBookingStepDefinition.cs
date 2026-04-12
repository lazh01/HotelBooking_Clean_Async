using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Core;
using Moq;
using Reqnroll;
using Xunit;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Core;
using Moq;
using Reqnroll;
using Xunit;

namespace HotelBooking.Specs.StepDefinitions
{
    [Binding]
    public class CreateBookingSteps
    {
        private List<Booking> _bookings = new();
        private List<Room> _rooms = new();
        private bool _result;
        private Exception _thrownException;

        private IBookingManager BuildManager()
        {
            var bookingMock = new Mock<IRepository<Booking>>();
            bookingMock.Setup(r => r.GetAllAsync()).ReturnsAsync(_bookings);
            bookingMock.Setup(r => r.AddAsync(It.IsAny<Booking>()))
                .Returns(Task.CompletedTask);

            var roomMock = new Mock<IRepository<Room>>();
            roomMock.Setup(r => r.GetAllAsync()).ReturnsAsync(_rooms);

            return new BookingManager(bookingMock.Object, roomMock.Object);
        }

        private async Task ExecuteBooking(DateTime start, DateTime end)
        {
            var manager = BuildManager();
            var booking = new Booking { StartDate = start, EndDate = end, CustomerId = 1 };
            try
            {
                _result = await manager.CreateBooking(booking);
            }
            catch (Exception ex)
            {
                _thrownException = ex;
            }
        }

        [Given("there are no existing bookings")]
        public void GivenNoBookings()
        {
            _bookings.Clear();
        }

        [Given("there are {int} available rooms")]
        public void GivenAvailableRooms(int count)
        {
            _rooms.Clear();
            for (int i = 1; i <= count; i++)
                _rooms.Add(new Room { Id = i, Description = $"Room {i}" });
        }

        [Given("a room is booked from in {int} day(s) to in {int} day(s)")]
        public void GivenARoomIsBooked(int startOffset, int endOffset)
        {
            _bookings.Add(new Booking
            {
                Id = _bookings.Count + 1,
                StartDate = DateTime.Today.AddDays(startOffset),
                EndDate = DateTime.Today.AddDays(endOffset),
                IsActive = true,
                RoomId = 1
            });
        }

        [Given("all rooms are booked from in {int} day(s) to in {int} day(s)")]
        public void GivenAllRoomsBooked(int startOffset, int endOffset)
        {
            foreach (var room in _rooms)
            {
                _bookings.Add(new Booking
                {
                    Id = _bookings.Count + 1,
                    StartDate = DateTime.Today.AddDays(startOffset),
                    EndDate = DateTime.Today.AddDays(endOffset),
                    IsActive = true,
                    RoomId = room.Id
                });
            }
        }

        [When("I try to book from {string} to {string}")]
        public async Task WhenITryToBookNamed(string startDesc, string endDesc)
        {
            await ExecuteBooking(ParseDate(startDesc), ParseDate(endDesc));
        }

        [When("I try to book from {string} to in {int} day(s)")]
        public async Task WhenITryToBookNamedToInt(string startDesc, int endOffset)
        {
            await ExecuteBooking(ParseDate(startDesc), DateTime.Today.AddDays(endOffset));
        }

        [When("I try to book from in {int} day(s) to in {int} day(s)")]
        public async Task WhenITryToBookFromInDays(int startOffset, int endOffset)
        {
            await ExecuteBooking(DateTime.Today.AddDays(startOffset), DateTime.Today.AddDays(endOffset));
        }

        [Then("the booking should return true")]
        public void ThenTrue() => Assert.True(_result);

        [Then("the booking should return false")]
        public void ThenFalse() => Assert.False(_result);

        [Then("an ArgumentException should be thrown")]
        public void ThenException() => Assert.IsType<ArgumentException>(_thrownException);

        private static DateTime ParseDate(string desc) => desc switch
        {
            "tomorrow" => DateTime.Today.AddDays(1),
            "yesterday" => DateTime.Today.AddDays(-1),
            "today" => DateTime.Today,
            _ => throw new ArgumentException($"Unknown date descriptor: {desc}")
        };
    }
}