import 'dart:convert';
import 'package:dio/dio.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/task.dart';

class ApiService {
  // ✅ CONFIGURED FOR ANDROID EMULATOR
  // Backend is running on http://0.0.0.0:5278, accessible as 10.0.2.2:5278 from emulator
  static const String baseUrl = 'http://10.0.2.2:5278';


  // Alternative configurations for different environments:
  // static const String baseUrl = 'http://localhost:5000'; // Direct access
  // static const String baseUrl = 'http://10.0.2.2:5278'; // Android emulator with old port
  // static const String baseUrl = 'http://localhost:5278'; // iOS simulator

  static const String apiPrefix = '/api';

  late Dio _dio;

  ApiService() {
    _dio = Dio(BaseOptions(
      baseUrl: baseUrl + apiPrefix,
      connectTimeout: const Duration(seconds: 20),
      receiveTimeout: const Duration(seconds: 30),
    ));

    // Add interceptors for JWT
    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        // Do not add token for login endpoint
        if (!options.path.contains('/Auth/login')) {
          final token = await _getToken();
          if (token != null) {
            options.headers['Authorization'] = 'Bearer $token';
          }
        }
        options.headers['Content-Type'] = 'application/json';
        return handler.next(options);
      },
      onError: (DioException error, handler) async {
        if (error.response?.statusCode == 401 || error.response?.statusCode == 403) {
          // Token is invalid, clear it
          await clearToken();
        }
        return handler.next(error);
      },
    ));
  }

  // Get stored JWT token
  Future<String?> _getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('jwt_token');
  }

  // Set JWT token
  Future<void> _setToken(String token) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('jwt_token', token);
  }

  // Clear stored token
  Future<void> clearToken() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('jwt_token');
  }

  // Login method
  Future<Map<String, dynamic>> login(String email, String password) async {
    try {
      final response = await _dio.post(
        '/Auth/login',
        data: {
          'email': email,
          'password': password,
        },
      );

      final data = response.data;
      if (data['token'] != null) {
        await _setToken(data['token']);
      }
      return data;
    } catch (e) {
      throw Exception('Login failed: ${e.toString()}');
    }
  }

  // Get driver's tasks
  Future<List<Task>> getMyTasks({String? status, String? sortBy}) async {
    try {
      final queryParams = <String, String>{};
      if (status != null) queryParams['status'] = status;
      if (sortBy != null) queryParams['sortBy'] = sortBy;

      final response = await _dio.get(
        '/DriverTasks',
        queryParameters: queryParams,
      );

      final List<dynamic> data = response.data;
      return data.map((json) => Task.fromJson(json)).toList();
    } catch (e) {
      throw Exception('Failed to load tasks: ${e.toString()}');
    }
  }

  // Get task details
  Future<Task> getTask(int id) async {
    final response = await _dio.get('/DriverTasks/$id');
    return Task.fromJson(response.data);
  }

  // Update task status
  Future<void> updateTaskStatus(int id, String status) async {
    await _dio.put('/DriverTasks/$id/status', data: {'status': status});
  }

  // Confirm pickup
  Future<Task> confirmPickup(int id) async {
    final response = await _dio.post('/DriverTasks/pickup/$id');
    return Task.fromJson(response.data);
  }

  // Complete task
  Future<Task> completeTask(int id) async {
    final response = await _dio.post('/DriverTasks/complete/$id');
    return Task.fromJson(response.data);
  }

  // Get task history
  Future<List<Task>> getTaskHistory({DateTime? startDate, DateTime? endDate}) async {
    final queryParams = <String, String>{};
    if (startDate != null) queryParams['startDate'] = startDate.toIso8601String();
    if (endDate != null) queryParams['endDate'] = endDate.toIso8601String();

    final response = await _dio.get(
      '/DriverTasks/history',
      queryParameters: queryParams,
    );

    final List<dynamic> data = response.data;
    return data.map((json) => Task.fromJson(json)).toList();
  }

  // Get earnings
  Future<Map<String, dynamic>> getEarnings({DateTime? startDate, DateTime? endDate}) async {
    final queryParams = <String, String>{};
    if (startDate != null) queryParams['startDate'] = startDate.toIso8601String();
    if (endDate != null) queryParams['endDate'] = endDate.toIso8601String();

    final response = await _dio.get(
      '/DriverTasks/earnings',
      queryParameters: queryParams,
    );

    return response.data;
  }

  // Verify QR code
  Future<Map<String, dynamic>> verifyQRCode(String qrCode, String type) async {
    final response = await _dio.post('/DriverTasks/verify-qr', data: {
      'qrCode': qrCode,
      'type': type,
    });

    return response.data;
  }


  // Get task route
  Future<Map<String, dynamic>> getTaskRoute(String taskId) async {
    final response = await _dio.get('/routes/get-task-route', queryParameters: {'taskId': taskId});
    return response.data;
  }

  // Send SOS signal
  Future<Map<String, dynamic>> sendSosSignal(String emergencyType, String description) async {
    final response = await _dio.post('/DriverTasks/sos', data: {
      'emergencyType': emergencyType,
      'description': description,
    });
    return response.data;
  }

}