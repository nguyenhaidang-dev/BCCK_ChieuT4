import 'dart:async';
import 'package:flutter/scheduler.dart';
import 'package:geolocator/geolocator.dart';
import 'package:permission_handler/permission_handler.dart';
import 'signalr_service.dart';

class LocationService {
  final SignalRService _signalRService;
  Timer? _locationTimer;
  bool _isTracking = false;

  LocationService(this._signalRService);

  Future<bool> requestPermissions() async {
    final locationPermission = await Permission.location.request();
    final locationAlwaysPermission = await Permission.locationAlways.request();

    return locationPermission.isGranted && locationAlwaysPermission.isGranted;
  }

  Future<bool> checkPermissions() async {
    final locationPermission = await Permission.location.status;
    final locationAlwaysPermission = await Permission.locationAlways.status;

    return locationPermission.isGranted && locationAlwaysPermission.isGranted;
  }

  Future<void> startLocationTracking() async {
    if (_isTracking) return;

    final hasPermission = await checkPermissions();
    if (!hasPermission) {
      throw Exception('Location permissions not granted');
    }

    _isTracking = true;

    // Send location updates every 10 seconds using getCurrentPosition to avoid threading issues
    _locationTimer = Timer.periodic(const Duration(seconds: 10), (timer) {
      // Ensure the entire location operation runs on main thread
      Future.microtask(() async {
        try {
          final position = await Geolocator.getCurrentPosition(
            desiredAccuracy: LocationAccuracy.high,
          );
          // Send update on main thread
          SchedulerBinding.instance.addPostFrameCallback((_) {
            _sendLocationUpdate(position);
          });
        } catch (e) {
          print('Error getting current position: $e');
        }
      });
    });
  }

  void _sendLocationUpdate(Position position) {
    _signalRService.sendLocationUpdate(
      position.latitude,
      position.longitude,
      speed: position.speed,
      heading: position.heading,
    );
  }

  Future<void> stopLocationTracking() async {
    _isTracking = false;
    _locationTimer?.cancel();
    _locationTimer = null;
  }

  Future<Position> getCurrentPosition() async {
    final hasPermission = await checkPermissions();
    if (!hasPermission) {
      throw Exception('Location permissions not granted');
    }

    return await Geolocator.getCurrentPosition(
      desiredAccuracy: LocationAccuracy.high,
    );
  }

  bool get isTracking => _isTracking;

  void dispose() {
    stopLocationTracking();
  }
}
// Added try-catch blocks.
