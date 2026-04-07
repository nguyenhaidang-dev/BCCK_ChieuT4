import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../providers/earnings_provider.dart';

class EarningsScreen extends StatefulWidget {
  const EarningsScreen({super.key});

  @override
  State<EarningsScreen> createState() => _EarningsScreenState();
}

class _EarningsScreenState extends State<EarningsScreen> {
  bool _isMonthly = true;

  @override
  void initState() {
    super.initState();
    // Load earnings after the widget is built
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadEarnings();
    });
  }

  Future<void> _loadEarnings() async {
    final earningsProvider = context.read<EarningsProvider>();
    if (_isMonthly) {
      await earningsProvider.loadMonthlyEarnings();
    } else {
      await earningsProvider.loadWeeklyEarnings();
    }
  }

  void _togglePeriod() {
    setState(() {
      _isMonthly = !_isMonthly;
    });
    _loadEarnings();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Thu nhập'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadEarnings,
          ),
        ],
      ),
      body: Consumer<EarningsProvider>(
        builder: (context, earningsProvider, child) {
          if (earningsProvider.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (earningsProvider.error != null) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text('Lỗi: ${earningsProvider.error}'),
                  ElevatedButton(
                    onPressed: _loadEarnings,
                    child: const Text('Thử lại'),
                  ),
                ],
              ),
            );
          }

          final currencyFormat = NumberFormat.currency(locale: 'vi_VN', symbol: '₫');

          return Column(
            children: [
              // Header with total earnings
              Container(
                padding: const EdgeInsets.all(20),
                color: Theme.of(context).primaryColor,
                child: Column(
                  children: [
                    const Text(
                      'Tổng thu nhập',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 16,
                      ),
                    ),
                    const SizedBox(height: 10),
                    Text(
                      currencyFormat.format(earningsProvider.monthlyEarnings),
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 32,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ],
                ),
              ),

              // Filter buttons
              Padding(
                padding: const EdgeInsets.all(16),
                child: Row(
                  children: [
                    Expanded(
                      child: ElevatedButton(
                        onPressed: _isMonthly ? null : _togglePeriod,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: _isMonthly ? Theme.of(context).primaryColor : Colors.grey,
                        ),
                        child: const Text('Tháng'),
                      ),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: ElevatedButton(
                        onPressed: !_isMonthly ? null : _togglePeriod,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: !_isMonthly ? Theme.of(context).primaryColor : Colors.grey,
                        ),
                        child: const Text('Tuần'),
                      ),
                    ),
                  ],
                ),
              ),

              // Earnings history list
              Expanded(
                child: earningsProvider.earningsHistory.isEmpty
                  ? const Center(
                      child: Text('Không có dữ liệu thu nhập'),
                    )
                  : ListView.builder(
                      itemCount: earningsProvider.earningsHistory.length,
                      itemBuilder: (context, index) {
                        final item = earningsProvider.earningsHistory[index];
                        final date = item['date'] != null
                          ? DateTime.parse(item['date'])
                          : DateTime.now();
                        final amount = item['amount'] ?? 0.0;
                        final description = item['description'] ?? 'Giao dịch';

                        return ListTile(
                          leading: CircleAvatar(
                            backgroundColor: amount > 0 ? Colors.green : Colors.red,
                            child: Icon(
                              amount > 0 ? Icons.arrow_upward : Icons.arrow_downward,
                              color: Colors.white,
                            ),
                          ),
                          title: Text(description),
                          subtitle: Text(
                            DateFormat('dd/MM/yyyy HH:mm').format(date),
                          ),
                          trailing: Text(
                            currencyFormat.format(amount),
                            style: TextStyle(
                              color: amount > 0 ? Colors.green : Colors.red,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        );
                      },
                    ),
              ),
            ],
          );
        },
      ),
    );
  }
}
// Improved earnings chart UI.
