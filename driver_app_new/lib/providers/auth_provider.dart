import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:jwt_decoder/jwt_decoder.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/user.dart';
import '../services/api_service.dart';
import '../services/signalr_service.dart';
import '../services/location_service.dart';

//QL Dang Nhap

class AuthProvider with ChangeNotifier {
  bool _isLoading = false;
  String? _error;
  bool _isLoggedIn = false;
  User? _currentUser;

  bool get isLoading => _isLoading;
  String? get error => _error;
  bool get isLoggedIn => _isLoggedIn;
  User? get currentUser => _currentUser;

  final ApiService _apiService = ApiService();
  late final SignalRService _signalRService;
  late final LocationService _locationService;

  AuthProvider() {
    _signalRService = SignalRService();
    _locationService = LocationService(_signalRService);

    // Set up SignalR event handlers
    _signalRService.onNotificationReceived = _handleNotification;
  }

  Function(String, String, String)? _notificationCallback;

  void setNotificationCallback(Function(String, String, String) callback) {
    _notificationCallback = callback;
  }

  void _handleNotification(String type, Map<String, dynamic> notification) {
    // Handle real-time notifications
    print('Received $type notification: $notification');

    String title;
    String message;

    switch (type) {
      case 'notification':
        title = 'New Task';
        message = notification['Message'] ?? 'You have a new task assigned';
        break;
      case 'geofencing':
        title = 'Location Alert';
        message = notification['Message'] ?? 'You are near a task location';
        break;
      default:
        title = 'Notification';
        message = notification.toString();
    }

    _notificationCallback?.call(title, message, type);
  }

  Future<bool> login(String email, String password) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final result = await _apiService.login(email, password);
      _isLoggedIn = true;

      // Store user info
      if (result['user'] != null) {
        _currentUser = User.fromJson(result['user']);
        // Save user data to SharedPreferences
        final prefs = await SharedPreferences.getInstance();
        await prefs.setString('user_data', jsonEncode(_currentUser!.toJson()));
      }

      // Initialize and start real-time services
      await _initializeServices();

      _isLoading = false;
      notifyListeners();
      return true;
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  Future<void> _initializeServices() async {
    try {
      // Initialize SignalR
      await _signalRService.init();
      await _signalRService.startConnection();

      // Request location permissions and start tracking
      final hasPermission = await _locationService.requestPermissions();
      if (hasPermission) {
        await _locationService.startLocationTracking();
      } else {
        print('Location permissions denied');
      }
    } catch (e) {
      print('Error initializing services: $e');
    }
  }

  Future<void> logout() async {
    // Stop services
    await _locationService.stopLocationTracking();
    await _signalRService.stopConnection();

    await clearToken();
    notifyListeners();
  }

  Future<bool> validateToken() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final token = prefs.getString('jwt_token');

      if (token != null && token.isNotEmpty) {
        // Check if token is expired using jwt_decoder
        bool isExpired = JwtDecoder.isExpired(token);

        if (!isExpired) {
          _isLoggedIn = true;
          // Load user data from SharedPreferences
          final userDataString = prefs.getString('user_data');
          if (userDataString != null) {
            final userData = jsonDecode(userDataString);
            _currentUser = User.fromJson(userData);
          }
          return true;
        } else {
          // Token is expired, clear it
          await clearToken();
        }
      }
    } catch (e) {
      // Token is invalid or error occurred
      await clearToken();
    }
    return false;
  }

  Future<void> clearToken() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('jwt_token');
    await prefs.remove('user_data');
    _isLoggedIn = false;
    _currentUser = null;
  }

  void clearError() {
    _error = null;
    notifyListeners();
  }

  @override
  void dispose() {
    _locationService.dispose();
    super.dispose();
  }
}
// Better state management.
