import 'package:flutter/material.dart';
import '../services/api_service.dart';
// ql Doanh Thu
class EarningsProvider with ChangeNotifier {
  double _monthlyEarnings = 0.0;
  List<Map<String, dynamic>> _earningsHistory = [];
  bool _isLoading = false;
  String? _error;

  double get monthlyEarnings => _monthlyEarnings;
  List<Map<String, dynamic>> get earningsHistory => _earningsHistory;
  bool get isLoading => _isLoading;
  String? get error => _error;

  final ApiService _apiService = ApiService();

  Future<void> loadEarnings({DateTime? startDate, DateTime? endDate}) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final tasks = await _apiService.getTaskHistory(startDate: startDate, endDate: endDate);
      _earningsHistory = tasks.map((t) => t.toJson()).toList();
      _monthlyEarnings = tasks.fold(0.0, (sum, t) => sum + t.estimatedPrice);
      _isLoading = false;
      notifyListeners();
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadMonthlyEarnings() async {
    final now = DateTime.now();
    final startOfMonth = DateTime(now.year, now.month, 1);
    final endOfMonth = DateTime(now.year, now.month + 1, 0);

    await loadEarnings(startDate: startOfMonth, endDate: endOfMonth);
  }

  Future<void> loadWeeklyEarnings() async {
    final now = DateTime.now();
    final startOfWeek = now.subtract(Duration(days: now.weekday - 1));
    final endOfWeek = startOfWeek.add(const Duration(days: 6));

    await loadEarnings(startDate: startOfWeek, endDate: endOfWeek);
  }

  void clearError() {
    _error = null;
    notifyListeners();
  }
}