import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../providers/task_provider.dart';
import '../providers/auth_provider.dart';
import '../models/task.dart';
import 'map_screen.dart';

class TasksScreen extends StatefulWidget {
  const TasksScreen({super.key});

  @override
  State<TasksScreen> createState() => _TasksScreenState();
}

class _TasksScreenState extends State<TasksScreen> {
  String? _selectedStatus;
  String? _selectedSort;

  @override
  void initState() {
    super.initState();
    _setupNotifications();
    // Load tasks after the widget is built to avoid calling setState during build
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadTasks();
    });
  }

  void _setupNotifications() {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);
    authProvider.setNotificationCallback(_showNotification);
  }

  void _showNotification(String title, String message, String type) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
            Text(message),
          ],
        ),
        duration: const Duration(seconds: 5),
        action: SnackBarAction(
          label: 'OK',
          onPressed: () {
            ScaffoldMessenger.of(context).hideCurrentSnackBar();
          },
        ),
      ),
    );

    // Refresh tasks when receiving task-related notifications
    if (type == 'notification') {
      _loadTasks();
    }
  }

  Future<void> _loadTasks() async {
    final taskProvider = Provider.of<TaskProvider>(context, listen: false);
    await taskProvider.loadTasks(status: _selectedStatus, sortBy: _selectedSort);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Tasks'),
        actions: [
          PopupMenuButton<String>(
            onSelected: (value) {
              setState(() {
                _selectedStatus = value == 'All' ? null : value;
              });
              _loadTasks();
            },
            itemBuilder: (context) => [
              const PopupMenuItem(value: 'All', child: Text('All Tasks')),
              const PopupMenuItem(value: 'Assigned', child: Text('Assigned')),
              const PopupMenuItem(value: 'InProgress', child: Text('In Progress')),
              const PopupMenuItem(value: 'Completed', child: Text('Completed')),
            ],
            icon: const Icon(Icons.filter_list),
          ),
          PopupMenuButton<String>(
            onSelected: (value) {
              setState(() {
                _selectedSort = value;
              });
              _loadTasks();
            },
            itemBuilder: (context) => [
              const PopupMenuItem(value: 'date', child: Text('Sort by Date')),
              const PopupMenuItem(value: 'priority', child: Text('Sort by Priority')),
            ],
            icon: const Icon(Icons.sort),
          ),
        ],
      ),
      body: Consumer<TaskProvider>(
        builder: (context, taskProvider, child) {
          if (taskProvider.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (taskProvider.error != null) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    'Error: ${taskProvider.error}',
                    textAlign: TextAlign.center,
                    style: const TextStyle(color: Colors.red),
                  ),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: _loadTasks,
                    child: const Text('Retry'),
                  ),
                ],
              ),
            );
          }

          if (taskProvider.tasks.isEmpty) {
            return const Center(
              child: Text('No tasks found'),
            );
          }

          return RefreshIndicator(
            onRefresh: _loadTasks,
            child: ListView.builder(
              itemCount: taskProvider.tasks.length,
              itemBuilder: (context, index) {
                final task = taskProvider.tasks[index];
                return TaskCard(
                  task: task,
                  onStatusUpdate: (taskId, status) async {
                    await taskProvider.updateTaskStatus(taskId, status);
                  },
                );
              },
            ),
          );
        },
      ),
    );
  }
}

class TaskCard extends StatelessWidget {
  final Task task;
  final Function(String, String) onStatusUpdate;

  const TaskCard({
    super.key,
    required this.task,
    required this.onStatusUpdate,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    task.referenceCode,
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: task.statusColor.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: task.statusColor),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      if (task.status == 'Completed')
                        const Icon(
                          Icons.check_circle,
                          size: 14,
                          color: Colors.green,
                        ),
                      const SizedBox(width: 4),
                      Text(
                        task.statusDisplay,
                        style: TextStyle(
                          color: task.statusColor,
                          fontSize: 12,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                const Icon(Icons.location_on, size: 16, color: Colors.red),
                const SizedBox(width: 4),
                Expanded(
                  child: Text(
                    'From: ${task.pickupAddress}',
                    style: const TextStyle(fontSize: 14),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 4),
            Row(
              children: [
                const Icon(Icons.location_on, size: 16, color: Colors.green),
                const SizedBox(width: 4),
                Expanded(
                  child: Text(
                    'To: ${task.deliveryAddress}',
                    style: const TextStyle(fontSize: 14),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                const Icon(Icons.access_time, size: 16),
                const SizedBox(width: 4),
                Text(
                  DateFormat('MMM dd, HH:mm').format(task.scheduledPickupTime),
                  style: const TextStyle(fontSize: 14),
                ),
                const Spacer(),
                Text(
                  '${task.distanceKm.toStringAsFixed(1)} km',
                  style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Text(
                  '${task.estimatedPrice.toStringAsFixed(0)} VND',
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: Colors.green,
                  ),
                ),
                const Spacer(),
                if (task.status == 'Assigned')
                  ElevatedButton(
                    onPressed: () async {
                      // Update status to InProgress first
                      await onStatusUpdate(task.id.toString(), 'InProgress');
                      // Navigate to map screen
                      Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (context) => MapScreen(task: task),
                        ),
                      );
                    },
                    child: const Text('Bắt Đầu Vận Chuyển'),
                  )
                else if (task.status == 'InProgress')
                  ElevatedButton(
                    onPressed: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (context) => MapScreen(task: task),
                        ),
                      );
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.orange,
                    ),
                    child: const Text('Tiếp Tục Vận Chuyển'),
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}