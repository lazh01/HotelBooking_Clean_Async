using System;
using HotelBooking.Core;
// Replaced fakes with Moq in tests
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Moq;


namespace HotelBooking.UnitTests
{
    public class BookingManagerTests
    {
        private IBookingManager bookingManager;
        private Mock<IRepository<Booking>> bookingRepoMock;
        private Mock<IRepository<Room>> roomRepoMock;
        private List<Booking> bookings;
        private List<Room> rooms;

        public BookingManagerTests()
        {
            // Arrange initial booking data that matches previous fakes
            DateTime fullyOccupiedStart = DateTime.Today.AddDays(10);
            DateTime fullyOccupiedEnd = DateTime.Today.AddDays(20);

            bookings = new List<Booking>
            {
                new Booking { Id = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(1), IsActive = true, CustomerId = 1, RoomId = 1 },
                new Booking { Id = 1, StartDate = fullyOccupiedStart, EndDate = fullyOccupiedEnd, IsActive = true, CustomerId = 1, RoomId = 1 },
                new Booking { Id = 2, StartDate = fullyOccupiedStart, EndDate = fullyOccupiedEnd, IsActive = true, CustomerId = 2, RoomId = 2 },
            };

            rooms = new List<Room>
            {
                new Room { Id = 1, Description = "A" },
                new Room { Id = 2, Description = "B" },
            };

            bookingRepoMock = new Mock<IRepository<Booking>>();
            bookingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(() => bookings);
            bookingRepoMock.Setup(r => r.GetAsync(It.IsAny<int>())).ReturnsAsync((int id) => bookings.FirstOrDefault(b => b.Id == id));
            bookingRepoMock.Setup(r => r.AddAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => bookings.Add(b))
                .Returns(Task.CompletedTask);
            bookingRepoMock.Setup(r => r.EditAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
            bookingRepoMock.Setup(r => r.RemoveAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            roomRepoMock = new Mock<IRepository<Room>>();
            roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(() => rooms);
            roomRepoMock.Setup(r => r.GetAsync(It.IsAny<int>())).ReturnsAsync((int id) => rooms.FirstOrDefault(r => r.Id == id));

            bookingManager = new BookingManager(bookingRepoMock.Object, roomRepoMock.Object);
        }

        [Fact]
        public async Task FindAvailableRoom_StartDateNotInTheFuture_ThrowsArgumentException()
        {
            // Arrange
            DateTime date = DateTime.Today;

            // Act
            Task result() => bookingManager.FindAvailableRoom(date, date);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(result);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_RoomIdNotMinusOne()
        {
            // Arrange
            DateTime date = DateTime.Today.AddDays(1);
            // Act
            int roomId = await bookingManager.FindAvailableRoom(date, date);
            // Assert
            Assert.NotEqual(-1, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_ReturnsAvailableRoom()
        {
            // This test was added to satisfy the following test design
            // principle: "Tests should have strong assertions".

            // Arrange
            DateTime date = DateTime.Today.AddDays(1);
            
            // Act
            int roomId = await bookingManager.FindAvailableRoom(date, date);

            var bookingForReturnedRoomId = bookings.
                Where(b => b.RoomId == roomId
                           && b.StartDate <= date
                           && b.EndDate >= date
                           && b.IsActive);
            
            // Assert
            Assert.Empty(bookingForReturnedRoomId);
        }

        public static IEnumerable<object[]> OverlapTestData()
        {
            // startOffset, endOffset, expectAvailable
            return new List<object[]>
            {
                new object[] { 1, 1, true },   // a booking exists only for room 1 on day 1 -> room 2 available
                new object[] { 9, 9, true },   // day before fully occupied period
                new object[] { 10, 10, false },// first day of fully occupied period
                new object[] { 15, 15, false },// inside fully occupied period
                new object[] { 20, 20, false },// last day of fully occupied period
                new object[] { 21, 21, true }, // after fully occupied period
                new object[] { 5, 15, false }, // overlaps fully occupied period
            };
        }

        [Theory]
        [MemberData(nameof(OverlapTestData))]
        public async Task FindAvailableRoom_VariousPeriods_ReturnsExpected(int startOffset, int endOffset, bool expectAvailable)
        {
            DateTime start = DateTime.Today.AddDays(startOffset);
            DateTime end = DateTime.Today.AddDays(endOffset);

            int roomId = await bookingManager.FindAvailableRoom(start, end);

            if (expectAvailable)
                Assert.NotEqual(-1, roomId);
            else
                Assert.Equal(-1, roomId);
        }

        [Fact]
        public async Task CreateBooking_RoomAvailable_AddsBookingAndSetsProperties()
        {
            // Arrange - choose a date after the fully occupied period
            var booking = new Booking { StartDate = DateTime.Today.AddDays(21), EndDate = DateTime.Today.AddDays(21), CustomerId = 99 };

            // Act
            bool created = await bookingManager.CreateBooking(booking);

            // Assert
            Assert.True(created);
            bookingRepoMock.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Once);
            Assert.True(booking.IsActive);
            Assert.True(booking.RoomId >= 0);
        }

        [Fact]
        public async Task CreateBooking_RoomNotAvailable_DoesNotAddBooking()
        {
            // Arrange - choose a date inside the fully occupied period
            var booking = new Booking { StartDate = DateTime.Today.AddDays(12), EndDate = DateTime.Today.AddDays(12), CustomerId = 100 };

            // Act
            bool created = await bookingManager.CreateBooking(booking);

            // Assert
            Assert.False(created);
            bookingRepoMock.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task GetFullyOccupiedDates_ReturnsExpectedDates()
        {
            // Arrange - query a range that covers the fully occupied period
            DateTime rangeStart = DateTime.Today.AddDays(9);
            DateTime rangeEnd = DateTime.Today.AddDays(21);

            // Act
            var fullyOccupied = await bookingManager.GetFullyOccupiedDates(rangeStart, rangeEnd);

            // Assert - should contain dates from day 10 to 20 inclusive
            var expected = new List<DateTime>();
            for (int d = 10; d <= 20; d++) expected.Add(DateTime.Today.AddDays(d));

            Assert.Equal(expected.Count, fullyOccupied.Count);
            Assert.Equal(expected, fullyOccupied);
        }

        [Fact]
        public async Task GetFullyOccupiedDates_StartAfterEnd_ThrowsArgumentException()
        {
            DateTime start = DateTime.Today.AddDays(5);
            DateTime end = DateTime.Today.AddDays(4);

            Task result() => bookingManager.GetFullyOccupiedDates(start, end);

            await Assert.ThrowsAsync<ArgumentException>(result);
        }

        [Fact]
        public async Task CreateBooking_WithMoq_AddsBookingAndSetsProperties()
        {
            // Arrange
            var bookings = new List<Booking>
            {
                new Booking { Id = 1, StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(20), IsActive = true, RoomId = 1 }
            };

            var rooms = new List<Room>
            {
                new Room { Id = 1 }, new Room { Id = 2 }
            };

            var bookingRepoMock = new Mock<IRepository<Booking>>();
            bookingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(bookings);
            bookingRepoMock.Setup(r => r.AddAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);

            var roomRepoMock = new Mock<IRepository<Room>>();
            roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

            var manager = new BookingManager(bookingRepoMock.Object, roomRepoMock.Object);

            var newBooking = new Booking { StartDate = DateTime.Today.AddDays(21), EndDate = DateTime.Today.AddDays(21), CustomerId = 99 };

            // Act
            var result = await manager.CreateBooking(newBooking);

            // Assert
            Assert.True(result);
            bookingRepoMock.Verify(r => r.AddAsync(It.Is<Booking>(b => b.IsActive && b.RoomId >= 0 && b.CustomerId == 99)), Times.Once);
        }

    }
}
