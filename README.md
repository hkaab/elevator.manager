🏢 Elevator System

This project simulates a basic elevator system, demonstrating how to manage multiple types of elevators, their capacities, and various operational settings. 

The system is designed with configurability in mind, allowing easy adjustments to its behavior without code changes.


✨ Features 

- **Multiple Elevator Types**: Supports Public, Private, and Service elevators.
- **Configurable Settings**: Easily adjust the number of elevators, their capacities, and amenities like music and speakers through a configuration file.
- **Feature Flags**: Enable or disable specific features like camera snapshots and card requirements for elevator access.
- **Logging**: Basic logging setup to monitor application behavior.
- **API Versioning**: Supports versioning for future enhancements and backward compatibility.
- **Floor Management**: Defines the maximum number of floors the elevators can service.

🚀 Getting Started

Prerequisites
.NET SDK (version 8.0)

How to Run

1. Clone the repository (if applicable, otherwise navigate to the project directory):

```

git clone <repo-url>
cd <project-directory>/src/Elevators.Api

```
2. Restore dependencies:
```

dotnet restore

```

3. Run the application:

```

dotnet run

```
The application should start, and you can observe its output or interact with it as designed.

---

⚙️ Configuration

The elevator system's configuration is managed through an `appsettings.json` file, which allows for easy customization of the system's parameters without needing to modify the codebase directly.

```
appsettings.json Example:
{
  "APIVersion": "v1",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "FeatureFlags": {
    "PublicElevators": true,
    "PrivateElevators": true,
    "ServiceElevators": true,
    "CameraSnapshot": true,
    "SwappedCardRequired": false
  },
  "ElevatorSettings": {
    "NumberOfPublicElevators": 4,
    "NumberOfPrivateElevators": 2,
    "NumberOfServiceElevators": 1,
    "MaxFloors": 10,
    "ElevatorCapacity": 5,
    "PublicElevatorHasMusic": true,
    "PublicElevatorHasSpeaker": true,
    "PrivateElevatorHasMusic": false,
    "PrivateElevatorHasSpeaker": true,
    "ServiceElevatorHasMusic": false,
    "ServiceElevatorHasSpeaker": false
  }
}
```

Explanation of Settings:

`NumberOfPublicElevators`: The total count of public-access elevators.

`NumberOfPrivateElevators`: The total count of private elevators (e.g., for executive floors).

`NumberOfServiceElevators`: The total count of service elevators (e.g., for maintenance, deliveries).

`MaxFloors`: The highest floor reachable by any elevator in the system.

`ElevatorCapacity`: The maximum number of people an individual elevator can hold.

`PublicElevatorHasMusic`: true if public elevators play music, false otherwise.

`PublicElevatorHasSpeaker`: true if public elevators have a speaker for announcements, false otherwise.

`PrivateElevatorHasMusic`: true if private elevators play music, false otherwise.

`PrivateElevatorHasSpeaker`: true if private elevators have a speaker, false otherwise.

`ServiceElevatorHasMusic`: true if service elevators play music, false otherwise.

`ServiceElevatorHasSpeaker`: true if service elevators have a speaker, false otherwise.

🔄 Background Service

In an elevator system, a background service (e.g., an IHostedService in .NET) is crucial for managing continuous, long-running operations that don't directly respond to a single HTTP request. Its purpose includes:

Elevator Dispatching Logic: Continuously monitoring call requests (from floors or inside elevators) and dispatching the most suitable elevator to fulfill the request.

* **State Management**: Updating and persisting the current state of each elevator (current floor, direction, passengers) in real-time.

* **Simulation**: Running the elevator movement simulation, including acceleration, deceleration, and door operations, independently of user interactions.

* **Logging and Monitoring**: Recording elevator events, performance metrics, and potential issues for analysis and maintenance.

* **Inter-Elevator Communication**: Coordinating movements between multiple elevators to optimize traffic flow and prevent conflicts.

By offloading these tasks to a background service, the main application (e.g., a Web API) remains responsive to immediate requests (like a user pressing a call button) while the complex, continuous logic runs reliably behind the scenes.

🌐 API Endpoints

The system exposes the following endpoints to monitor and control the elevators. For an interactive experience, see the Swagger Documentation section below.

### Get Elevator Status
Retrieves the current status of all elevators, including their type, current floor, and whether they are in service.
Method: GET
Endpoint: /api/v1/elevator/status
Response (200 OK):
```json

{
  "elevators": [
    {
      "id": 1,
      "type": "Public",
      "currentFloor": 0,
      "state": "Idle",
      "direction": "None",
      "passengers": [],
      "summonRequests": []
    },
    {
      "id": 2,
      "type": "Public",
      "currentFloor": 0,
      "state": "Idle",
      "direction": "None",
      "passengers": [],
      "summonRequests": []
    },
    {
      "id": 3,
      "type": "Private",
      "currentFloor": 0,
      "state": "Idle",
      "direction": "None",
      "passengers": [],
      "summonRequests": []
    },
    {
      "id": 4,
      "type": "Service",
      "currentFloor": 0,
      "state": "Idle",
      "direction": "None",
      "passengers": [],
      "summonRequests": []
    }
  ],
  "floors": [
    {
      "floorNumber": 0,
      "passengers": [],
      "upCall": false,
      "downCall": false
    },
    {
      "floorNumber": 1,
      "passengers": [],
      "upCall": false,
      "downCall": false
    },
    {
      "floorNumber": 2,
      "passengers": [],
      "upCall": false,
      "downCall": false
    },
    {
      "floorNumber": 3,
      "passengers": [],
      "upCall": false,
      "downCall": false
    },
    {
      "floorNumber": 4,
      "passengers": [],
      "upCall": false,
      "downCall": false
    },
    {
      "floorNumber": 5,
      "passengers": [],
      "upCall": false,
      "downCall": false
    },
    {
      "floorNumber": 6,
      "passengers": [],
      "upCall": false,
      "downCall": false
    },
    {
      "floorNumber": 7,
      "passengers": [],
      "upCall": false,
      "downCall": false
    },
    {
      "floorNumber": 8,
      "passengers": [],
      "upCall": false,
      "downCall": false
    },
    {
      "floorNumber": 9,
      "passengers": [],
      "upCall": false,
      "downCall": false
    },
    {
      "floorNumber": 10,
      "passengers": [],
      "upCall": false,
      "downCall": false
    }
  ],
  "fireAlarmActive": false
}

```
### Summon General Elevator
Retrieves detailed information about a specific elevator by its ID, including its type, current floor, capacity, and amenities.
Method: POST
Endpoint: /api/v1/elevator/request/general
Request Body:

```json
{
  "currentFloor": 1,
  "destinationFloor": 9
}
```

Response (200): An empty body indicating the request has been accepted and is being processed.

### Summon Private Elevator
Retrieves detailed information about a specific elevator by its ID, including its type, current floor, capacity, and amenities.
Method: POST
Endpoint: /api/v1/elevator/request/private/1
Request Body:

```json
{
  "currentFloor": 1,
  "destinationFloor": 9
}
```
Response (200): An empty body indicating the request has been accepted and is being processed.

### Summon Service Elevator
Retrieves detailed information about a specific elevator by its ID, including its type, current floor, capacity, and amenities.
Method: POST
Endpoint: /api/v1/elevator/request/service
Request Body:

```json
{
  "currentFloor": 0,
  "destinationFloor": 0,
  "hasSwappedCard": true
}
```
Response (200): An empty body indicating the request has been accepted and is being processed.

📖 API Documentation (Swagger)

This project uses Swagger (OpenAPI) to generate interactive API documentation. 

You can use this interface to view detailed information about each endpoint and test them directly from your browser.

Once the application is running, you can access:

Swagger UI: `http://localhost:<port>/swagger`

Swagger JSON: `http://localhost:<port>/swagger/v1/swagger.json`

Replace `<port>` with the port number your application is running on (e.g., 5001 or 7280).


📄 License
This project is licensed under the Apache-2.0 License - see the LICENSE file for details.
