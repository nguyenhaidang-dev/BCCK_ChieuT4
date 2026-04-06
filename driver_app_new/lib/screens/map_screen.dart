import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:geolocator/geolocator.dart';
import 'package:latlong2/latlong.dart';
import 'package:provider/provider.dart';
import '../providers/task_provider.dart';
import '../services/location_service.dart';
import '../services/signalr_service.dart';
import '../models/task.dart';

class MapScreen extends StatefulWidget {
  final Task? task;

  const MapScreen({super.key, this.task});

  @override
  State<MapScreen> createState() => _MapScreenState();
}

class _MapScreenState extends State<MapScreen> {
  final MapController _mapController = MapController();
  LatLng? _currentLocation;
  List<LatLng> _routePoints = [];
  List<dynamic> _routeSteps = [];
  bool _isLoadingRoute = false;
  StreamSubscription<Position>? _positionStream;
  bool _isMapCentered = false;
  bool _isPickedUp = false;
  bool _showDirections = false;

  @override
  void initState() {
    super.initState();
    // Set initial pickup state based on task status
    if (widget.task != null) {
      _isPickedUp = widget.task!.status == 'PickedUp';
    }
    _determinePosition();
    // Load route after the widget is built
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadTaskRoute();
    });
  }

  @override
  void dispose() {
    _positionStream?.cancel();
    super.dispose();
  }

  Future<void> _determinePosition() async {
    // Cancel existing stream
    await _positionStream?.cancel();
    _positionStream = null;
    _isMapCentered = false;

    bool serviceEnabled;
    LocationPermission permission;

    // Test if location services are enabled.
    serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) {
      // Location services are not enabled don't continue
      // accessing the position and request users of the
      // App to enable the location services.
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Location services are disabled. Please enable the services')),
      );
      return;
    }

    permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied) {
        // Permissions are denied, next time you could try
        // requesting permissions again (this is also where
        // Android's shouldShowRequestPermissionRationale
        // returned true. According to Android guidelines
        // your App should show an explanatory UI now.
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Location permissions are denied')),
        );
        return;
      }
    }

    if (permission == LocationPermission.deniedForever) {
      // Permissions are denied forever, handle appropriately.
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Location permissions are permanently denied, we cannot request permissions.')),
      );
      return;
    }

    // When we reach here, permissions are granted and we can
    // continue accessing the position of the device.
    _positionStream = Geolocator.getPositionStream(
      locationSettings: const LocationSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: 10,
      ),
    ).listen((Position position) {
      setState(() {
        _currentLocation = LatLng(position.latitude, position.longitude);
        if (!_isMapCentered && _currentLocation != null) {
          _mapController.move(_currentLocation!, 15.0);
          _isMapCentered = true;
        }
      });
    });
  }

  Future<void> _loadTaskRoute() async {
    if (widget.task == null) return;

    setState(() {
      _isLoadingRoute = true;
    });

    try {
      final routeData = await context.read<TaskProvider>().getTaskRoute(widget.task!.id.toString());
      setState(() {
        _routePoints = routeData['coordinates'] ?? [];
        _routeSteps = routeData['steps'] ?? [];
        _isLoadingRoute = false;
      });
    } catch (e) {
      setState(() {
        _isLoadingRoute = false;
      });
      print('Error loading route: $e');
    }
  }

  IconData _getDirectionIcon(String type) {
    switch (type) {
      case 'turn':
        return Icons.turn_right;
      case 'new name':
        return Icons.straight;
      case 'depart':
        return Icons.north;
      case 'arrive':
        return Icons.location_on;
      case 'merge':
        return Icons.merge;
      case 'ramp':
        return Icons.ramp_right;
      case 'fork':
        return Icons.fork_right;
      default:
        return Icons.arrow_forward;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.task != null ? 'Vận Chuyển: ${widget.task!.referenceCode}' : 'Map & Tracking'),
        actions: [
          if (_routeSteps.isNotEmpty)
            IconButton(
              icon: Icon(_showDirections ? Icons.map : Icons.directions),
              onPressed: () {
                setState(() {
                  _showDirections = !_showDirections;
                });
              },
            ),
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () {
              _determinePosition();
              _loadTaskRoute();
            },
          ),
        ],
      ),
      body: _showDirections && _routeSteps.isNotEmpty
          ? Row(
              children: [
                // Directions panel
                Container(
                  width: 300,
                  color: Colors.white,
                  child: Column(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(16),
                        color: Colors.blue,
                        child: const Row(
                          children: [
                            Icon(Icons.directions, color: Colors.white),
                            SizedBox(width: 8),
                            Text(
                              'Hướng dẫn đường đi',
                              style: TextStyle(
                                color: Colors.white,
                                fontSize: 18,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ],
                        ),
                      ),
                      Expanded(
                        child: ListView.builder(
                          itemCount: _routeSteps.length,
                          itemBuilder: (context, index) {
                            final step = _routeSteps[index];
                            return ListTile(
                              leading: Icon(
                                _getDirectionIcon(step['type'] ?? 'continue'),
                                color: Colors.blue,
                              ),
                              title: Text(step['instruction'] ?? 'Continue'),
                              subtitle: Text(
                                '${(step['distance'] ?? 0).toString()}m • ${((step['duration'] ?? 0) / 60).round()}min',
                              ),
                            );
                          },
                        ),
                      ),
                    ],
                  ),
                ),
                // Map
                Expanded(
                  child: Stack(
                    children: [
                      FlutterMap(
                        mapController: _mapController,
                        options: MapOptions(
                          center: _currentLocation ?? const LatLng(10.8231, 106.6297),
                          zoom: 13.0,
                        ),
                        children: [
                          TileLayer(
                            urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                            userAgentPackageName: 'com.example.driver_app_new',
                          ),
                          if (_currentLocation != null)
                            MarkerLayer(
                              markers: [
                                Marker(
                                  point: _currentLocation!,
                                  child: const Icon(
                                    Icons.my_location,
                                    color: Colors.blue,
                                    size: 30,
                                  ),
                                ),
                              ],
                            ),
                          if (widget.task != null)
                            MarkerLayer(
                              markers: [
                                if (!_isPickedUp)
                                  Marker(
                                    point: LatLng(widget.task!.pickupLatitude, widget.task!.pickupLongitude),
                                    child: const Icon(
                                      Icons.location_on,
                                      color: Colors.green,
                                      size: 30,
                                    ),
                                  ),
                                if (_isPickedUp)
                                  Marker(
                                    point: LatLng(widget.task!.deliveryLatitude, widget.task!.deliveryLongitude),
                                    child: const Icon(
                                      Icons.location_on,
                                      color: Colors.red,
                                      size: 30,
                                    ),
                                  ),
                              ],
                            ),
                          if (_routePoints.isNotEmpty)
                            PolylineLayer(
                              polylines: [
                                Polyline(
                                  points: _routePoints,
                                  color: Colors.blue,
                                  strokeWidth: 4.0,
                                ),
                              ],
                            ),
                        ],
                      ),
                      // Buttons overlay
                      if (widget.task != null)
                        Positioned(
                          bottom: 20,
                          left: 20,
                          right: 20,
                          child: Row(
                            children: [
                              if (!_isPickedUp)
                                Expanded(
                                  child: ElevatedButton(
                                    onPressed: () async {
                                      final taskProvider = context.read<TaskProvider>();
                                      bool success = await taskProvider.confirmPickup(widget.task!.id.toString());
                                      if (success) {
                                        setState(() {
                                          _isPickedUp = true;
                                        });
                                        _loadTaskRoute();
                                      }
                                    },
                                    style: ElevatedButton.styleFrom(
                                      backgroundColor: Colors.green,
                                      padding: const EdgeInsets.symmetric(vertical: 15),
                                    ),
                                    child: const Text('Đã Lấy Hàng', style: TextStyle(fontSize: 16)),
                                  ),
                                ),
                              if (_isPickedUp)
                                Expanded(
                                  child: ElevatedButton(
                                    onPressed: () async {
                                      final taskProvider = context.read<TaskProvider>();
                                      bool success = await taskProvider.completeTask(widget.task!.id.toString());
                                      if (success) {
                                        Navigator.of(context).pop();
                                      }
                                    },
                                    style: ElevatedButton.styleFrom(
                                      backgroundColor: Colors.blue,
                                      padding: const EdgeInsets.symmetric(vertical: 15),
                                    ),
                                    child: const Text('Hoàn Thành Công Việc', style: TextStyle(fontSize: 16)),
                                  ),
                                ),
                            ],
                          ),
                        ),
                    ],
                  ),
                ),
              ],
            )
          : Stack(
              children: [
                FlutterMap(
                  mapController: _mapController,
                  options: MapOptions(
                    center: _currentLocation ?? const LatLng(10.8231, 106.6297),
                    zoom: 13.0,
                  ),
                  children: [
                    TileLayer(
                      urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                      userAgentPackageName: 'com.example.driver_app_new',
                    ),
                    if (_currentLocation != null)
                      MarkerLayer(
                        markers: [
                          Marker(
                            point: _currentLocation!,
                            child: const Icon(
                              Icons.my_location,
                              color: Colors.blue,
                              size: 30,
                            ),
                          ),
                        ],
                      ),
                    if (widget.task != null)
                      MarkerLayer(
                        markers: [
                          if (!_isPickedUp)
                            Marker(
                              point: LatLng(widget.task!.pickupLatitude, widget.task!.pickupLongitude),
                              child: const Icon(
                                Icons.location_on,
                                color: Colors.green,
                                size: 30,
                              ),
                            ),
                          if (_isPickedUp)
                            Marker(
                              point: LatLng(widget.task!.deliveryLatitude, widget.task!.deliveryLongitude),
                              child: const Icon(
                                Icons.location_on,
                                color: Colors.red,
                                size: 30,
                              ),
                            ),
                        ],
                      ),
                    if (_routePoints.isNotEmpty)
                      PolylineLayer(
                        polylines: [
                          Polyline(
                            points: _routePoints,
                            color: Colors.blue,
                            strokeWidth: 4.0,
                          ),
                        ],
                      ),
                  ],
                ),
                // Buttons overlay
                if (widget.task != null)
                  Positioned(
                    bottom: 20,
                    left: 20,
                    right: 20,
                    child: Row(
                      children: [
                        if (!_isPickedUp)
                          Expanded(
                            child: ElevatedButton(
                              onPressed: () async {
                                final taskProvider = context.read<TaskProvider>();
                                bool success = await taskProvider.confirmPickup(widget.task!.id.toString());
                                if (success) {
                                  setState(() {
                                    _isPickedUp = true;
                                  });
                                  _loadTaskRoute();
                                }
                              },
                              style: ElevatedButton.styleFrom(
                                backgroundColor: Colors.green,
                                padding: const EdgeInsets.symmetric(vertical: 15),
                              ),
                              child: const Text('Đã Lấy Hàng', style: TextStyle(fontSize: 16)),
                            ),
                          ),
                        if (_isPickedUp)
                          Expanded(
                            child: ElevatedButton(
                              onPressed: () async {
                                final taskProvider = context.read<TaskProvider>();
                                bool success = await taskProvider.completeTask(widget.task!.id.toString());
                                if (success) {
                                  Navigator.of(context).pop();
                                }
                              },
                              style: ElevatedButton.styleFrom(
                                backgroundColor: Colors.blue,
                                padding: const EdgeInsets.symmetric(vertical: 15),
                              ),
                              child: const Text('Hoàn Thành Công Việc', style: TextStyle(fontSize: 16)),
                            ),
                          ),
                      ],
                    ),
                  ),
              ],
            ),
    );
  }
}
// Updated widget tree.
