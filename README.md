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


🚀 Getting Started

Prerequisites
.NET SDK (version 8.0)

How to Run

1. Clone the repository (if applicable, otherwise navigate to the project directory):

```

git clone <repo-url>
cd <project-directory>

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


📄 License
This project is licensed under the Apache-2.0 License - see the LICENSE file for details.
