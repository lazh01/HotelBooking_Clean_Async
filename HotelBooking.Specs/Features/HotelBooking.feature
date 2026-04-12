Feature: Create Booking
  A hotel room can be booked for a future period,
  provided it is not already booked during the desired period.

  # --- Date validation ---

  Scenario: Booking with valid future date is created
    Given there are no existing bookings
    And there are 1 available rooms
    When I try to book from "tomorrow" to in 5 days
    Then the booking should return true

  Scenario: Booking with start date in the past throws exception
    Given there are no existing bookings
    And there are 1 available rooms
    When I try to book from "yesterday" to in 5 days
    Then an ArgumentException should be thrown

  Scenario: Booking with start date today throws exception
    Given there are no existing bookings
    And there are 1 available rooms
    When I try to book from "today" to in 5 days
    Then an ArgumentException should be thrown

  Scenario: Booking where end date is before start date throws exception
    Given there are no existing bookings
    And there are 1 available rooms
    When I try to book from in 5 days to in 2 days
    Then an ArgumentException should be thrown

  Scenario: Single day booking is created
    Given there are no existing bookings
    And there are 1 available rooms
    When I try to book from "tomorrow" to "tomorrow"
    Then the booking should return true

  # --- Overlap types ---

  Scenario: Booking with no overlap is created
    Given there are 2 available rooms
    And a room is booked from in 1 day to in 3 days
    When I try to book from in 5 days to in 8 days
    Then the booking should return true

  Scenario: Booking with partial overlap is rejected
    Given there are 1 available rooms
    And a room is booked from in 3 days to in 7 days
    When I try to book from in 1 day to in 5 days
    Then the booking should return false

  Scenario: New period fully covered by existing booking is rejected
    Given there are 1 available rooms
    And a room is booked from in 2 days to in 4 days
    When I try to book from in 1 day to in 5 days
    Then the booking should return false

  Scenario: New period fully covers existing booking is rejected
    Given there are 1 available rooms
    And a room is booked from in 1 day to in 5 days
    When I try to book from in 2 days to in 4 days
    Then the booking should return false

  # --- Boundary values ---

  Scenario: New period ends the day before existing booking starts
    Given there are 1 available rooms
    And a room is booked from in 5 days to in 10 days
    When I try to book from in 2 days to in 4 days
    Then the booking should return true

  Scenario: New period ends on the same day existing booking starts
    Given there are 1 available rooms
    And a room is booked from in 5 days to in 10 days
    When I try to book from in 2 days to in 5 days
    Then the booking should return false

  Scenario: New period starts on the same day existing booking ends
    Given there are 1 available rooms
    And a room is booked from in 5 days to in 10 days
    When I try to book from in 10 days to in 13 days
    Then the booking should return false

  Scenario: New period starts the day after existing booking ends
    Given there are 1 available rooms
    And a room is booked from in 5 days to in 10 days
    When I try to book from in 11 days to in 13 days
    Then the booking should return true

  # --- All rooms occupied ---

  Scenario: All rooms occupied in the period rejects booking
    Given there are 2 available rooms
    And all rooms are booked from in 1 day to in 5 days
    When I try to book from in 1 day to in 5 days
    Then the booking should return false