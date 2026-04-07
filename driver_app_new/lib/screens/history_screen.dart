import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../providers/earnings_provider.dart';

class HistoryScreen extends StatefulWidget {
  const HistoryScreen({super.key});

  @override
  State<HistoryScreen> createState() => _HistoryScreenState();
}

class _HistoryScreenState extends State<HistoryScreen> {
  @override
  void initState() {
    super.initState();
    // Load earnings history after the widget is built
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadHistory();
    });
  }

  Future<void> _loadHistory() async {
    final earningsProvider = context.read<EarningsProvider>();
    // Load all time history
    await earningsProvider.loadEarnings();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Lịch sử nhiệm vụ'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadHistory,
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
                    onPressed: _loadHistory,
                    child: const Text('Thử lại'),
                  ),
                ],
              ),
            );
          }

          final currencyFormat = NumberFormat.currency(locale: 'vi_VN', symbol: '₫');

          // Calculate total from history
          final totalEarnings = earningsProvider.earningsHistory.fold<double>(
            0.0,
            (sum, item) => sum + ((item['estimatedPrice'] as num?)?.toDouble() ?? 0.0),
          );

          return Column(
            children: [
              // Total earnings summary
              Container(
                padding: const EdgeInsets.all(20),
                color: Theme.of(context).primaryColor,
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text(
                      'Tổng thu nhập',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    Text(
                      currencyFormat.format(totalEarnings),
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 24,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ],
                ),
              ),

              // Task history list
              Expanded(
                child: earningsProvider.earningsHistory.isEmpty
                  ? const Center(
                      child: Text('Không có lịch sử nhiệm vụ'),
                    )
                  : ListView.builder(
                      itemCount: earningsProvider.earningsHistory.length,
                      itemBuilder: (context, index) {
                        final item = earningsProvider.earningsHistory[index];
                        final date = item['completedTime'] != null
                          ? DateTime.parse(item['completedTime'])
                          : DateTime.now();
                        final amount = (item['estimatedPrice'] as num?)?.toDouble() ?? 0.0;
                        final referenceCode = item['referenceCode'] ?? 'Unknown';

                        return Card(
                          margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                          child: ListTile(
                            leading: CircleAvatar(
                              backgroundColor: Colors.green,
                              child: const Icon(
                                Icons.check_circle,
                                color: Colors.white,
                              ),
                            ),
                            title: Text('Nhiệm vụ $referenceCode'),
                            subtitle: Text(
                              DateFormat('dd/MM/yyyy HH:mm').format(date),
                            ),
                            trailing: Text(
                              currencyFormat.format(amount),
                              style: const TextStyle(
                                color: Colors.green,
                                fontWeight: FontWeight.bold,
                                fontSize: 16,
                              ),
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
// Fixed list rendering bug.
