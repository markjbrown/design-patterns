# Preallocation Pattern

## Description

This pattern involved pre-allocating an collection of values in full rather than adding them one at a time later or utilize complex logic to determine a result.

## Details

This pattern tends to go well with collections of dates or representations of values in an array that should be pre-created with a related item.

A couple examples include:

- Hotel room reservations for a room instance
- Available seats in a theatre for a movie instance.

> **NOTE** Cosmos DB has an item size limit of 2MB for an item.  Be sure any document that you model with the pre-allocation pattern stays under this limit, if it goes over this limit, you will need to consider breaking the data into seperate items.

The main components of the models used in the sample include:

- Hotels that contains Rooms
- Reservations that include a RoomId and HotelId
- Rooms can contain AvailableDates

## Setup

- Review the [SETUP.md](SETUP.md) file for details on how to setup and run the sample.

## Non-preallocation

In the non-preallocation pattern, you will see that a hotel is created with 10 rooms.  These rooms have no reservations and the process of checking for reservations would be to query for any existing reservations and then subtracting out all the dates.

- An example hotel:

```json
{
    "id": "hotel_1",
    "EntityType": "hotel",
    "hotelId": "hotel_1",
    "Name": "Microsoft Hotels Inc",
    "City": "Redmond",
    "Address": "1 Microsoft Way",
    "Rating": 0,
    "AvailableRooms": 0,
    "Rooms": [],
    "Reservations": []
}
```

- An example room that does not utilize pre-allocation of available dates:

```json
{
    "id": "room_0",
    "EntityType": "room",
    "hotelId": "hotel_1",
    "Name": null,
    "Type": null,
    "Status": null,
    "NoBeds": 0,
    "SizeInSqFt": 0,
    "Price": 0,
    "Available": false,
    "Description": null,
    "MaximumGuests": 0,
    "Features": [],
    "RoomImages": [],
    "Reviews": []    
}
```

- An example reservation, where the room is a part of the reservation item:

```json
{
    "id": "reservation_room_0_20230213",
    "EntityType": "reservation",
    "LeaseId": null,
    "LeasedUntil": null,
    "IsPaid": false,
    "Status": null,
    "StartDate": "2023-02-13T00:00:00",
    "EndDate": "2023-02-14T00:00:00",
    "CheckIn": "0001-01-01T00:00:00",
    "CheckOut": "0001-01-01T00:00:00",
    "Customer": null,
    "hotelId": "hotel_1",
    "RoomId": "room_0",
    "Room": {
        "id": "room_0",
        "EntityType": "room",
        "LeaseId": null,
        "LeasedUntil": null,
        "hotelId": "hotel_1",
        "Name": null,
        "Type": null,
        "Status": null,
        "NoBeds": 0,
        "SizeInSqFt": 0,
        "Price": 0,
        "Available": false,
        "Description": null,
        "MaximumGuests": 0,
        "AvailableDates": [],
        "Features": [],
        "RoomImages": [],
        "Reviews": []
    }
}
```

## Preallocation

In the following example you will see the reservation dates for a room being pre-allocated in a collection with a simple `IsAvailable` property for each date.  This will then make the process of finding available dates a bit easier when it comes to sending queries to the database.

```csharp
DateTime start = DateTime.Parse(DateTime.Now.ToString("01/01/yyyy"));
DateTime end = DateTime.Parse(DateTime.Now.ToString("12/31/yyyy"));

//add all the days for the year which can be queried later.
foreach (Room r in h.Rooms)
{
    int count = 0;

    while (start.AddDays(count) < end)
    {
        r.AvailableDates.Add(new AvailableDate { Date = start.AddDays(count), IsAvailable = true });

        count++;
    }
}
```

- A pre-allocation room will now look similar to the following:

```json
{
    "id": "room_1",
    "EntityType": "room",
    "LeaseId": null,
    "LeasedUntil": null,
    "hotelId": "hotel_2",
    "Name": null,
    "Type": null,
    "Status": null,
    "NoBeds": 0,
    "SizeInSqFt": 0,
    "Price": 0,
    "Available": false,
    "Description": null,
    "MaximumGuests": 0,
    "AvailableDates": [
        {
            "Date": "2023-01-01T00:00:00",
            "IsAvailable": true
        },
        {
            "Date": "2023-01-02T00:00:00",
            "IsAvailable": true
        }...
    ],
    "Features": [],
    "RoomImages": [],
    "Reviews": []
}
```

## Queries

By pre-allocating the room's reservation days, you can easily run the following query to find available dates for a particular room or set of rooms:

```sql
select c.id, count(ad) from c join ad in c.AvailableDates where ad.Date > '2023-01-01T00:00:00' and ad.Date < '2023-01-05T00:00:00' and c.hotelId = 'hotel_2' group by c.id
```

By not choosing the pre-allocation pattern, the alternative way to find available rooms for a set of dates will be more complex.  For example, without pre-allocation, you would need to query all reservations for a room then build a collection of available dates by substracting the reservation dates.  You can see a subset of this logic available in the `FindAvailableRooms` method of the `Hotel` class.

## Summary

Pre-allocation allows for a much simpler design for queries and logic versus other approaches however it can come at the cost of a larger document in storage and memory given the pre-allocation of the data.
