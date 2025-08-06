using Elevators.Core.Constants;
using Elevators.Core.Interfaces;
using Elevators.Core.Models;
using Elevators.Core.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Serilog;

namespace Elevators.Core.Services
{
    public class ElevatorManagerService : IElevatorManagerService
    {
        public List<IElevator> Elevators { get; private set; }
        public List<IFloor> Floors { get; private set; }
        public int MaxFloors { get; private set; }
        public bool FireAlarmActive { get; private set; }
        private int _passengerIdCounter = 0;

        private readonly IFeatureManager _featureManager;
        private readonly IHardwareIntegrationService _hardwareIntegrationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ElevatorSettings _elevatorSettings;
        private TimeSpan _serviceElevatorStartTime;
        private  TimeSpan _serviceElevatorEndTime;
        private  TimeSpan _upDirectionStartTime;
        private  TimeSpan _upDirectionEndTime;

        public ElevatorManagerService(
            IFeatureManager featureManager,
            IHardwareIntegrationService hardwareIntegrationService,
            IConfiguration configuration,
            ILogger logger,
            IOptions<ElevatorSettings> elevatorSettingsOptions
        )
        {
            _featureManager = featureManager;
            _hardwareIntegrationService = hardwareIntegrationService;
            _configuration = configuration;
            _logger = logger;
            _elevatorSettings = elevatorSettingsOptions.Value;

            Elevators = [];
            Floors = [];
            InitializeOperatingHours();
            InitalizeElevators();
            InitializeFloors();

            FireAlarmActive = false;
        }

        // Initializes the floors based on the maximum number of floors specified in the elevator settings.
        // This method creates a list of Floor objects, each representing a floor in the building.
        private void InitializeFloors()
        {
            for (int i = 0; i <= _elevatorSettings.MaxFloors; i++)
            {
                Floors.Add(new Floor(i, _logger));
            }
        }

        // Initializes the elevators based on the settings provided in the configuration.
        private void InitalizeElevators()
        {
            int elevatorIdCounter = 1;
            for (int i = 0; i < _elevatorSettings.NumberOfPublicElevators; i++)
            {
                Elevators.Add(new Elevator(elevatorIdCounter++, _elevatorSettings.ElevatorCapacity, ElevatorType.Public, _elevatorSettings.PublicElevatorHasMusic, _elevatorSettings.PublicElevatorHasSpeaker, _logger));
            }
            for (int i = 0; i < _elevatorSettings.NumberOfPrivateElevators; i++)
            {
                Elevators.Add(new Elevator(elevatorIdCounter++, _elevatorSettings.ElevatorCapacity, ElevatorType.Private, _elevatorSettings.PrivateElevatorHasMusic, _elevatorSettings.PrivateElevatorHasSpeaker, _logger));
            }
            for (int i = 0; i < _elevatorSettings.NumberOfServiceElevators; i++)
            {
                Elevators.Add(new Elevator(elevatorIdCounter++, _elevatorSettings.ElevatorCapacity, ElevatorType.Service, _elevatorSettings.ServiceElevatorHasMusic, _elevatorSettings.ServiceElevatorHasSpeaker, _logger));
            }
        }

        // Initializes the operating hours for service elevators and directional restrictions.
        // This method reads the configuration settings for operating hours and sets the start and end times.
        private void InitializeOperatingHours()
        {
            // Load operating hours from configuration
            var serviceHoursConfig = _configuration.GetSection("ElevatorOperatingRules:ServiceElevatorOperatingHours");
            _serviceElevatorStartTime = TimeSpan.Parse(serviceHoursConfig["Start"] ?? "08:00");
            _serviceElevatorEndTime = TimeSpan.Parse(serviceHoursConfig["End"] ?? "18:00");

            var upDirectionHoursConfig = _configuration.GetSection("ElevatorOperatingRules:UpDirectionOperatingHours");
            _upDirectionStartTime = TimeSpan.Parse(upDirectionHoursConfig["Start"] ?? "08:00");
            _upDirectionEndTime = TimeSpan.Parse(upDirectionHoursConfig["End"] ?? "12:00");
        }

        // Sets the fire alarm state and handles the elevator behavior during a fire alarm.
        // If the fire alarm is activated, all elevators will go to the ground floor (Floor 1).
        // If deactivated, elevators will resume normal operation.
        public void SetFireAlarm(bool active)
        {
            FireAlarmActive = active;
            if (active)
            {
                _hardwareIntegrationService.ActivateFireAlarmAsync().Wait();
                _logger.Fatal("\n!!! FIRE ALARM ACTIVATED !!! All elevators going to Floor 1.");
                foreach (var elevator in Elevators)
                {
                    elevator.State = ElevatorState.EmergencyStop;
                    elevator.CurrentDirection = Direction.Down;
                    elevator.SummonRequests.Clear();
                    elevator.SummonRequests.Add(1);
                }
            }
            else
            {
                _hardwareIntegrationService.DeactivateFireAlarmAsync().Wait();
                _logger.Warning("\n!!! FIRE ALARM DEACTIVATED !!! Elevators resuming normal operation.");
                foreach (var elevator in Elevators)
                {
                    if (elevator.State == ElevatorState.EmergencyStop)
                    {
                        elevator.State = ElevatorState.Idle;
                        elevator.CurrentDirection = Direction.None;
                    }
                }
            }
        }

        // Activates an emergency call for a specific elevator.
        // This method checks if the elevator exists and then sends a command to the hardware integration service
        public async Task EmergencyCallAsync(int elevatorId)
        {
            var elevator = Elevators.FirstOrDefault(e => e.Id == elevatorId);
            if (elevator != null)
            {
                bool success = await _hardwareIntegrationService.ActivateEmergencyCallAsync(elevator.Id);
                if (success)
                {
                    elevator.IsEmergencyCallActive = true;
                    _logger.Error("Emergency call activated for Elevator {ElevatorId}. Waiting for response.", elevatorId);
                }
            }
            else
            {
                _logger.Error("Elevator with ID {ElevatorId} not found.", elevatorId);
            }
        }

        // Mocks a camera snapshot for a private elevator at a specific floor.
        // This method simulates capturing an image when a passenger enters or exits a private elevator.
        private async Task MockCameraSnapshot(int elevatorId, int floorNumber)
        {
            if (await _featureManager.IsEnabledAsync(FeatureNames.CameraSnapshot))
            {
                _logger.Information("[CAMERA SNAPSHOT]: Elevator {ElevatorId} (Private) at Floor {FloorNumber} - Image Captured.", elevatorId, floorNumber);
            }
        }

        // Sets an issue for a specific elevator.
        // This method updates the elevator's state and sends a command to the hardware integration service.
        public void SetElevatorIssue(int elevatorId, bool hasIssue)
        {
            var elevator = Elevators.FirstOrDefault(e => e.Id == elevatorId);
            if (elevator != null)
            {
                bool success = _hardwareIntegrationService.SetElevatorIssueAsync(elevator.Id, hasIssue).Result;
                if (success)
                {
                    elevator.SetIssue(hasIssue);
                }
                else
                {
                    _logger.Error("Failed to send issue command to hardware for Elevator {ElevatorId}. State not changed.", elevator.Id);
                }
            }
            else
            {
                _logger.Error("Elevator with ID {ElevatorId} not found.", elevatorId);
            }
        }

        // Adds a general passenger request to the elevator system. 
        // This method checks if the requested floors are valid, creates a new passenger, and assigns the request to the best available elevator.
        // If no elevator is available, it logs a warning.
        public async Task AddGeneralPassengerRequest(int currentFloor, int destinationFloor)
        {
            if (currentFloor < 0 || currentFloor > MaxFloors || destinationFloor < 0 || destinationFloor > MaxFloors)
            {
                _logger.Error("Invalid floor number for passenger request.");
                return;
            }

            _passengerIdCounter++;
            var passenger = new Passenger(_passengerIdCounter, currentFloor, destinationFloor);
            Floors[currentFloor].AddPassenger(passenger);
            _logger.Information("System: New summon request at Floor {CurrentFloor} going to {DestinationFloor} from Passenger {PassengerId}.", currentFloor, destinationFloor, passenger.Id);

            IElevator? bestElevator = null;

            if (await _featureManager.IsEnabledAsync(FeatureNames.PublicElevators))
            {
                bestElevator = Elevators.Where(e => e.Type == ElevatorType.Public && e.State != ElevatorState.OutOfService && e.State != ElevatorState.EmergencyStop && !e.IsFull())
                                        .OrderBy(e => Math.Abs(e.CurrentFloor - currentFloor))
                                        .FirstOrDefault();
            }

            if (bestElevator == null && await _featureManager.IsEnabledAsync(FeatureNames.PrivateElevators))
            {
                bestElevator = Elevators.Where(e => e.Type == ElevatorType.Private && e.State != ElevatorState.OutOfService && e.State != ElevatorState.EmergencyStop && !e.IsFull())
                                        .OrderBy(e => Math.Abs(e.CurrentFloor - currentFloor))
                                        .FirstOrDefault();
            }

            if (bestElevator != null)
            {
                if (!bestElevator.SummonRequests.Contains(currentFloor))
                {
                    bestElevator.SummonRequests.Add(currentFloor);
                    bestElevator.SummonRequests.Sort();
                }
                _logger.Information("System: Summon request for Passenger {PassengerId} assigned to Elevator {ElevatorId} ({ElevatorType}).", passenger.Id, bestElevator.Id, bestElevator.Type);
            }
            else
            {
                _logger.Warning("System: No available elevator to handle summon request for Passenger {PassengerId} at Floor {CurrentFloor}.", passenger.Id, currentFloor);
            }

            if (destinationFloor > currentFloor)
            {
                Floors[currentFloor].UpCall = true;
            }
            else if (destinationFloor < currentFloor)
            {
                Floors[currentFloor].DownCall = true;
            }
        }

        // Adds a private elevator request for a specific private elevator.
        public async Task AddPrivateElevatorRequest(int elevatorId, int currentFloor, int destinationFloor)
        {
            if (currentFloor < 0 || currentFloor > MaxFloors || destinationFloor < 0 || destinationFloor > MaxFloors)
            {
                _logger.Error("Invalid floor number for passenger request.");
                return;
            }

            if (!await _featureManager.IsEnabledAsync(FeatureNames.PrivateElevators))
            {
                _logger.Information("Private Elevators feature is currently disabled. Cannot accept specific private elevator requests.");
                return;
            }

            var elevator = Elevators.FirstOrDefault(e => e.Id == elevatorId && e.Type == ElevatorType.Private);
            if (elevator == null)
            {
                _logger.Error("Elevator {ElevatorId} is not a private elevator or does not exist.", elevatorId);
                return;
            }

            if (elevator.State == ElevatorState.OutOfService || elevator.State == ElevatorState.EmergencyStop || elevator.IsFull())
            {
                _logger.Warning("Private Elevator {ElevatorId} cannot accept request: {ElevatorState} or Full.", elevatorId, elevator.State);
                return;
            }

            _passengerIdCounter++;
            var passenger = new Passenger(_passengerIdCounter, currentFloor, destinationFloor);
            Floors[currentFloor].AddPassenger(passenger);
            _logger.Information("System: New summon request at Floor {CurrentFloor} going to {DestinationFloor} from Passenger {PassengerId}.", currentFloor, destinationFloor, passenger.Id);


            if (!elevator.SummonRequests.Contains(currentFloor))
            {
                elevator.SummonRequests.Add(currentFloor);
                elevator.SummonRequests.Sort();
            }

            if (destinationFloor > currentFloor)
            {
                Floors[currentFloor].UpCall = true;
            }
            else if (destinationFloor < currentFloor)
            {
                Floors[currentFloor].DownCall = true;
            }
            _logger.Information("System: Summon request for Passenger {PassengerId} specifically assigned to Private Elevator {ElevatorId}.", passenger.Id, elevatorId);
        }

        // Adds a service elevator request for staff members.
        public async Task AddServiceElevatorRequest(int currentFloor, int destinationFloor, bool hasSwappedCard)
        {
            if (currentFloor < 0 || currentFloor > MaxFloors || destinationFloor < 0 || destinationFloor > MaxFloors)
            {
                _logger.Error("Invalid floor number for passenger request.");
                return;
            }

            if (!await _featureManager.IsEnabledAsync(FeatureNames.ServiceElevators))
            {
                _logger.Information("Service Elevators feature is currently disabled. Cannot handle service requests.");
                return;
            }

            if (await _featureManager.IsEnabledAsync(FeatureNames.SwappedCardRequired) && !hasSwappedCard)
            {
                _logger.Warning("Access Denied: A swapped card is required to use a service elevator. Please use a general or private elevator instead.");
                return;
            }

            // Check for time-based service operation
            if (await _featureManager.IsEnabledAsync(FeatureNames.TimeBasedServiceElevatorOperation))
            {
                var now = DateTime.Now.TimeOfDay;
                if (now < _serviceElevatorStartTime || now > _serviceElevatorEndTime)
                {
                    _logger.Information("Access Denied: Service Elevators only operate between {Start} and {End}.", _serviceElevatorStartTime, _serviceElevatorEndTime);
                    return;
                }

                // Check for directional time restriction
                if (await _featureManager.IsEnabledAsync(FeatureNames.DirectionalTimeRestriction))
                {
                    if (destinationFloor > currentFloor) // Is an UP request
                    {
                        if (now < _upDirectionStartTime || now > _upDirectionEndTime)
                        {
                            _logger.Information("Access Denied: Upward travel on Service Elevators is only permitted between {Start} and {End}.", _upDirectionStartTime, _upDirectionEndTime);
                            return;
                        }
                    }
                }
            }

            _passengerIdCounter++;
            var passenger = new Passenger(_passengerIdCounter, currentFloor, destinationFloor, hasSwappedCard);
            Floors[currentFloor].AddPassenger(passenger);
            _logger.Information("System: New summon request at Floor {CurrentFloor} going to {DestinationFloor} from Staff Passenger {PassengerId}.", currentFloor, destinationFloor, passenger.Id);


            var bestElevator = Elevators.Where(e => e.Type == ElevatorType.Service && e.State != ElevatorState.OutOfService && e.State != ElevatorState.EmergencyStop && !e.IsFull())
                                        .OrderBy(e => Math.Abs(e.CurrentFloor - currentFloor))
                                        .FirstOrDefault();

            if (bestElevator != null)
            {
                if (!bestElevator.SummonRequests.Contains(currentFloor))
                {
                    bestElevator.SummonRequests.Add(currentFloor);
                    bestElevator.SummonRequests.Sort();
                }
                _logger.Information("System: Summon request for Staff Passenger {PassengerId} assigned to Service Elevator {ElevatorId}.", passenger.Id, bestElevator.Id);
            }
            else
            {
                _logger.Warning("System: No available service elevator to handle summon request for Staff Passenger {PassengerId} at Floor {CurrentFloor}.", passenger.Id, currentFloor);
            }

            if (destinationFloor > currentFloor)
            {
                Floors[currentFloor].UpCall = true;
            }
            else if (destinationFloor < currentFloor)
            {
                Floors[currentFloor].DownCall = true;
            }
        }

        // Processes elevator commands based on the current state of each elevator and the building's floors.
        // This method iterates through each elevator, checks its state, and applies the necessary rules for movement, passenger entry/exit, and music playback.
        // It handles special cases such as fire alarms, mechanical issues, and time-based service operations.
        // The method also manages active summons and ensures that elevators respond appropriately to passenger requests.
        public async Task ProcessElevatorCommands()
        {
            foreach (var elevator in Elevators)
            {
                // Skip elevators that are out of service or in emergency stop state
                if (elevator.HasMechanicalIssue)
                {
                    _logger.Debug("Elevator {ElevatorId} ({ElevatorType}) is out of service due to an issue.", elevator.Id, elevator.Type);
                    continue;
                }

                // Check if the elevator is currently in an emergency state
                if (FireAlarmActive)
                {
                    await ApplyFireAlarmRules(elevator);
                    continue;
                }

                // Time-based logic for Service Elevators
                // Check if the elevator is a service elevator and if time-based operation is enabled
                if (elevator.Type == ElevatorType.Service && await _featureManager.IsEnabledAsync(FeatureNames.TimeBasedServiceElevatorOperation))
                {
                    bool flowControl = await ApplyTimeBasedServiceRules(elevator);
                    if (!flowControl)
                    {
                        continue;
                    }
                }

                //Check if the elevator is currently at a floor with requests
                IFloor currentFloor = Floors[elevator.CurrentFloor];

                bool needsToStop = elevator.ShouldStop(currentFloor);

                if (needsToStop)
                    await ApplyStopMusicRule(elevator);

                // Apply exit and passenger rules
                await ApplyExitPassengersRules(elevator);

                // Apply enter passengers rules
                await ApplyEnterPassengersRules(elevator, currentFloor);


                if (elevator.Passengers.Count != 0 || elevator.SummonRequests.Count != 0)
                {
                    int nextInternalDestination = elevator.GetNextDestination(MaxFloors);

                    // Directional time restriction logic
                    if (await _featureManager.IsEnabledAsync(FeatureNames.DirectionalTimeRestriction) && elevator.Type == ElevatorType.Service)
                    {
                        nextInternalDestination = ApplyDirectionalTimeRestrictionRule(elevator, nextInternalDestination);
                    }

                    if (nextInternalDestination != elevator.CurrentFloor)
                    {
                        Direction moveDirection = (nextInternalDestination > elevator.CurrentFloor) ? Direction.Up : Direction.Down;

                        if (elevator.HasMusic && !elevator.IsMusicPlaying)
                        {
                            bool musicStarted = await _hardwareIntegrationService.PlayMusicAsync(elevator.Id);
                            if (musicStarted) elevator.IsMusicPlaying = true;
                        }

                        bool moved = await _hardwareIntegrationService.MoveElevatorAsync(elevator.Id, elevator.CurrentFloor, nextInternalDestination, moveDirection);
                        if (moved)
                        {
                            elevator.CurrentFloor = nextInternalDestination;
                            elevator.CurrentDirection = moveDirection;
                            elevator.State = (moveDirection == Direction.Up) ? ElevatorState.MovingUp : ElevatorState.MovingDown;
                        }
                        else
                        {
                            _logger.Error("Elevator {ElevatorId} failed to move from {CurrentFloor} to {NextDestination}. Remaining at {CurrentFloor}.", elevator.Id, elevator.CurrentFloor, nextInternalDestination, elevator.CurrentFloor);
                            elevator.State = ElevatorState.Idle;
                            await ApplyStopMusicRule(elevator);
                        }
                        continue;
                    }
                }
                else
                {
                    await ApplyStopMusicRule(elevator);
                }

                var activeSummons = Floors.Where(f => f.UpCall || f.DownCall).ToList();
                if (activeSummons.Count != 0)
                {
                    await ProcessActiveSummons(elevator, activeSummons);
                }
                else
                {
                    elevator.State = ElevatorState.Idle;
                    elevator.CurrentDirection = Direction.None;
                }
            }
        }

        // Processes active summons for the elevator.   
        // This method determines the next target floor based on the elevator's current direction and active summons.
        // If the elevator is moving up, it looks for the next floor with an up call.
        // If the elevator is moving down, it looks for the next floor with a down call.
        // If no active summons are found in the current direction, it checks the opposite direction.
        private async Task ProcessActiveSummons(IElevator elevator, List<IFloor> activeSummons)
        {
            IFloor? targetFloor = null;

            // Determine the next target floor based on the elevator's current direction and active summons
            if (elevator.CurrentDirection == Direction.Up)
            {
                targetFloor = activeSummons.Where(f => f.FloorNumber > elevator.CurrentFloor && f.UpCall)
                                        .OrderBy(f => f.FloorNumber)
                                        .FirstOrDefault();
                if (targetFloor == null)
                {
                    targetFloor = activeSummons.Where(f => f.FloorNumber < elevator.CurrentFloor && f.DownCall)
                                            .OrderByDescending(f => f.FloorNumber)
                                            .FirstOrDefault();
                    if (targetFloor != null) elevator.CurrentDirection = Direction.Down;
                }
            }
            else if (elevator.CurrentDirection == Direction.Down)
            {
                targetFloor = activeSummons.Where(f => f.FloorNumber < elevator.CurrentFloor && f.DownCall)
                                        .OrderByDescending(f => f.FloorNumber)
                                        .FirstOrDefault();
                if (targetFloor == null)
                {
                    targetFloor = activeSummons.Where(f => f.FloorNumber > elevator.CurrentFloor && f.UpCall)
                                            .OrderBy(f => f.FloorNumber)
                                            .FirstOrDefault();
                    if (targetFloor != null) elevator.CurrentDirection = Direction.Up;
                }
            }
            else
            {
                targetFloor = activeSummons.OrderBy(f => Math.Abs(f.FloorNumber - elevator.CurrentFloor)).FirstOrDefault();
                if (targetFloor != null)
                {
                    elevator.CurrentDirection = (targetFloor.FloorNumber > elevator.CurrentFloor) ? Direction.Up : Direction.Down;
                }
            }

            if (targetFloor != null && targetFloor.FloorNumber != elevator.CurrentFloor)
            {
                Direction moveDirection = (targetFloor.FloorNumber > elevator.CurrentFloor) ? Direction.Up : Direction.Down;
                if (elevator.HasMusic && !elevator.IsMusicPlaying)
                {
                    bool musicStarted = await _hardwareIntegrationService.PlayMusicAsync(elevator.Id);
                    if (musicStarted) elevator.IsMusicPlaying = true;
                }
                bool moved = await _hardwareIntegrationService.MoveElevatorAsync(elevator.Id, elevator.CurrentFloor, targetFloor.FloorNumber, moveDirection);
                if (moved)
                {
                    elevator.CurrentFloor = targetFloor.FloorNumber;
                    elevator.CurrentDirection = moveDirection;
                    elevator.State = (moveDirection == Direction.Up) ? ElevatorState.MovingUp : ElevatorState.MovingDown;
                }
                else
                {
                    _logger.Error("Elevator {ElevatorId} failed to move to call at {TargetFloor}. Remaining at {CurrentFloor}.", elevator.Id, targetFloor.FloorNumber, elevator.CurrentFloor);
                    elevator.State = ElevatorState.Idle;
                    if (elevator.HasMusic && elevator.IsMusicPlaying)
                    {
                        bool musicStopped = await _hardwareIntegrationService.StopMusicAsync(elevator.Id);
                        if (musicStopped) elevator.IsMusicPlaying = false;
                    }
                }
            }
            else
            {
                elevator.State = ElevatorState.Idle;
                elevator.CurrentDirection = Direction.None;
            }
        }

        // Applies directional time restriction rule to the elevator.
        // This method checks if the current time is within the allowed upward travel time window.  
        // If outside the window, it restricts upward travel and sets the next destination accordingly.
        // If there are no downward requests, it defaults to the ground floor (0).  
        private int ApplyDirectionalTimeRestrictionRule(IElevator elevator, int nextInternalDestination)
        {
            var now = DateTime.Now.TimeOfDay;
            if (now < _upDirectionStartTime || now > _upDirectionEndTime)
            {
                // Outside the upward time window, only allow downward travel
                var downwardDestinations = elevator.SummonRequests.Where(d => d < elevator.CurrentFloor).ToList();
                if (downwardDestinations.Count != 0)
                {
                    nextInternalDestination = downwardDestinations.OrderByDescending(d => d).First();
                }
                else
                {
                    // If no downward requests, just go to floor 0 (Ground) if not already there.
                    if (elevator.CurrentFloor != 0)
                    {
                        nextInternalDestination = 0;
                    }
                    else
                    {
                        nextInternalDestination = elevator.CurrentFloor;
                    }
                }

                if (nextInternalDestination > elevator.CurrentFloor)
                {
                    _logger.Information("Elevator {ElevatorId} ({ElevatorType}): Upward travel denied due to time restriction. Remaining at Floor {CurrentFloor}.", elevator.Id, elevator.Type, elevator.CurrentFloor);
                    nextInternalDestination = elevator.CurrentFloor; // Prevent upward movement
                }
            }

            return nextInternalDestination;
        }

        // Applies enter passengers rules to the elevator.
        // This method checks if there are passengers waiting on the current floor who want to enter the elevator.
        // If there are passengers to enter, it opens the doors, announces the floor number if  the elevator has a speaker, and adds the passengers to the elevator.
        // After entering the passengers, it closes the doors.
        private async Task ApplyEnterPassengersRules(IElevator elevator, IFloor currentFloor)
        {
            var passengersToEnter = currentFloor.Passengers
                .Where(p =>
                    (elevator.State == ElevatorState.Idle && (currentFloor.UpCall || currentFloor.DownCall)) ||
                    (elevator.CurrentDirection == Direction.Up && p.DestinationFloor > elevator.CurrentFloor) ||
                    (elevator.CurrentDirection == Direction.Down && p.DestinationFloor < currentFloor.FloorNumber)
                )
                .ToList();

            if (passengersToEnter.Count != 0 && !elevator.IsFull())
            {
                bool opened = await _hardwareIntegrationService.OpenDoorsAsync(elevator.Id, elevator.CurrentFloor);
                if (opened)
                {
                    if (elevator.HasSpeaker)
                    {
                        await _hardwareIntegrationService.SpeakFloorNumberAsync(elevator.Id, elevator.CurrentFloor);
                    }
                    foreach (var passenger in passengersToEnter.ToList())
                    {
                        if (!elevator.IsFull())
                        {
                            elevator.AddPassenger(passenger);
                            currentFloor.Passengers.Remove(passenger);

                            if (elevator.Type == ElevatorType.Private)
                            {
                                await MockCameraSnapshot(elevator.Id, elevator.CurrentFloor);
                            }
                        }
                        else
                        {
                            _logger.Information("Elevator {ElevatorId} ({ElevatorType}) is full, cannot pick up more passengers at Floor {CurrentFloor}.", elevator.Id, elevator.Type, elevator.CurrentFloor);
                            break;
                        }
                    }
                    currentFloor.ClearCalls();
                    await _hardwareIntegrationService.CloseDoorsAsync(elevator.Id, elevator.CurrentFloor);
                }
                else
                {
                    _logger.Error("Elevator {ElevatorId} failed to open doors for pickup at Floor {CurrentFloor}!", elevator.Id, elevator.CurrentFloor);
                }
            }
        }

        // Applies exit passengers rules to the elevator.
        // This method checks if there are passengers whose destination floor matches the current floor of the elevator.
        // If there are passengers to exit, it opens the doors, announces the floor number if the elevator has a speaker, and removes the passengers from the elevator.
        // After exiting the passengers, it closes the doors.
        private async Task ApplyExitPassengersRules(IElevator elevator)
        {
            var passengersToExit = elevator.Passengers.Where(p => p.DestinationFloor == elevator.CurrentFloor).ToList();
            if (passengersToExit.Count != 0)
            {
                bool opened = await _hardwareIntegrationService.OpenDoorsAsync(elevator.Id, elevator.CurrentFloor);
                if (opened)
                {
                    if (elevator.HasSpeaker)
                    {
                        await _hardwareIntegrationService.SpeakFloorNumberAsync(elevator.Id, elevator.CurrentFloor);
                    }
                    foreach (var passenger in passengersToExit.ToList())
                    {
                        elevator.RemovePassenger(passenger);
                    }
                    await _hardwareIntegrationService.CloseDoorsAsync(elevator.Id, elevator.CurrentFloor);
                }
                else
                {
                    _logger.Error("Elevator {ElevatorId} failed to open doors for exit at Floor {CurrentFloor}!", elevator.Id, elevator.CurrentFloor);
                }
            }
        }

        // Applies stop music rule to the elevator.
        // This method checks if the elevator has music and if it is currently playing, then stops  the music using the hardware integration service.
        // If the music is successfully stopped, it updates the elevator's state to reflect that music is no longer playing.
        private async Task ApplyStopMusicRule(IElevator elevator)
        {
            if (elevator.HasMusic && elevator.IsMusicPlaying)
            {
                bool musicStopped = await _hardwareIntegrationService.StopMusicAsync(elevator.Id);
                if (musicStopped) elevator.IsMusicPlaying = false;
            }
        }

        // Applies time-based service rules to the elevator.
        // This method checks the current time against the configured service elevator operating hours. 
        private async Task<bool> ApplyTimeBasedServiceRules(IElevator elevator)
        {
            var now = DateTime.Now.TimeOfDay;
            if (now < _serviceElevatorStartTime || now > _serviceElevatorEndTime)
            {
                // If outside operating hours, prioritize going to the ground floor and become idle
                if (elevator.CurrentFloor > 0)
                {
                    elevator.SummonRequests.Clear();
                    elevator.SummonRequests.Add(0);
                    elevator.State = ElevatorState.MovingDown;
                    _logger.Information("Elevator {ElevatorId} ({ElevatorType}) is outside operating hours. Heading to ground floor (0).", elevator.Id, elevator.Type);
                }
                else
                {
                    elevator.State = ElevatorState.Idle;
                    elevator.CurrentDirection = Direction.None;
                    await ApplyStopMusicRule(elevator);
                }
                return false;
            }

            return true;
        }

        // Applies fire alarm rules to the elevator during a fire alarm situation.
        // This method checks the elevator's current floor and state, and moves it down to the ground floor if necessary.
        // If the elevator is already at the ground floor, it opens the doors and evacuates all passengers.
        // If the elevator fails to move or open doors, it logs an error.
        private async Task ApplyFireAlarmRules(IElevator elevator)
        {
            if (elevator.CurrentFloor > 0)
            {
                bool moved = await _hardwareIntegrationService.MoveElevatorAsync(elevator.Id, elevator.CurrentFloor, elevator.CurrentFloor - 1, Direction.Down);
                if (moved) elevator.CurrentFloor--;
                else _logger.Error("Elevator {ElevatorId} failed to move down during fire alarm!", elevator.Id);
            }
            else if (elevator.CurrentFloor == 0 && elevator.State == ElevatorState.EmergencyStop)
            {
                bool opened = await _hardwareIntegrationService.OpenDoorsAsync(elevator.Id, elevator.CurrentFloor);
                if (opened)
                {
                    foreach (var passenger in elevator.Passengers.ToList())
                    {
                        elevator.RemovePassenger(passenger);
                    }
                    _logger.Information("Elevator {ElevatorId} ({ElevatorType}) has reached ground floor (1) during fire alarm. All passengers evacuated.", elevator.Id, elevator.Type);
                    elevator.State = ElevatorState.Idle;
                }
                else
                {
                    // high emergency alert should be raised if the elevator fails to open doors at ground floor
                    _logger.Error("Elevator {ElevatorId} failed to open doors at ground floor during fire alarm!", elevator.Id);
                }
            }
        }
    }
}
