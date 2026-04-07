import 'package:flutter/material.dart';
import 'package:latlong2/latlong.dart';
import '../models/task.dart';
import '../services/api_service.dart';
//QL CV
class TaskProvider with ChangeNotifier {
  List<Task> _tasks = [];
  bool _isLoading = false;
  String? _error;

  List<Task> get tasks => _tasks;
  bool get isLoading => _isLoading;
  String? get error => _error;

  final ApiService _apiService = ApiService();

  Future<void> loadTasks({String? status, String? sortBy}) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _tasks = await _apiService.getMyTasks(status: status, sortBy: sortBy);
      _isLoading = false;
      notifyListeners();
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> updateTaskStatus(String taskId, String newStatus) async {
    try {
      // Call API POST to /api/tasks/update-status/{taskId} with newStatus in body
      await _apiService.updateTaskStatus(int.parse(taskId), newStatus);

      // Reload tasks to get updated status
      await loadTasks();

      return true;
    } catch (e) {
      _error = e.toString();
      notifyListeners();
      return false;
    }
  }

  Future<bool> verifyQRCode(String qrCode, String type) async {
    try {
      final result = await _apiService.verifyQRCode(qrCode, type);

      // Reload tasks to get updated status
      await loadTasks();

      return true;
    } catch (e) {
      _error = e.toString();
      notifyListeners();
      return false;
    }
  }

  Future<bool> confirmPickup(String taskId) async {
    try {
      await _apiService.confirmPickup(int.parse(taskId));

      // Reload tasks to get updated status
      await loadTasks();

      return true;
    } catch (e) {
      _error = e.toString();
      notifyListeners();
      return false;
    }
  }

  Future<bool> completeTask(String taskId) async {
    try {
      await _apiService.completeTask(int.parse(taskId));

      // Reload tasks to get updated status
      await loadTasks();

      return true;
    } catch (e) {
      _error = e.toString();
      notifyListeners();
      return false;
    }
  }

  Future<Map<String, dynamic>> getTaskRoute(String taskId) async {
    try {
      final response = await _apiService.getTaskRoute(taskId);
      final List<dynamic> coordinates = response['coordinates'] ?? [];
      final List<dynamic> steps = response['steps'] ?? [];

      return {
        'coordinates': coordinates.map((coord) => LatLng(coord['lat'], coord['lng'])).toList(),
        'steps': steps
      };
    } catch (e) {
      _error = e.toString();
      notifyListeners();
      return {'coordinates': <LatLng>[], 'steps': <dynamic>[]};
    }
  }

  void clearError() {
    _error = null;
    notifyListeners();
  }
}
// Refactored notifyListeners.
