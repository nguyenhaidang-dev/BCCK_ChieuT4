import 'package:signalr_netcore/signalr_client.dart';
import 'package:shared_preferences/shared_preferences.dart';

class SignalRService {
  // FIX 1: Chuyển sang HubConnection? (Nullable)
  HubConnection? _hubConnection;
  final String serverUrl = 'http://10.0.2.2:5278/locationHub'; // Android emulator

  Function(String, Map<String, dynamic>)? onNotificationReceived;
  Function(String, double, double, double?, double?)? onLocationUpdateReceived;

  Future<void> init() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('jwt_token');

    _hubConnection = HubConnectionBuilder()
        .withUrl(serverUrl, options: HttpConnectionOptions(
          accessTokenFactory: () => Future.value(token),
          transport: HttpTransportType.LongPolling,
        ))
        .withAutomaticReconnect()
        .build();

    // FIX 2: Thêm toán tử ! vào các hàm .on()
    // Register event handlers
    _hubConnection!.on('ReceiveNotification', _handleNotification);
    _hubConnection!.on('ReceiveLocationUpdate', _handleLocationUpdate);
    _hubConnection!.on('GeofencingAlert', _handleGeofencingAlert);
  }

  void _handleNotification(List<Object?>? args) {
    if (args != null && args.isNotEmpty) {
      final notification = args[0] as Map<String, dynamic>;
      onNotificationReceived?.call('notification', notification);
    }
  }

  void _handleLocationUpdate(List<Object?>? args) {
    if (args != null && args.length >= 3) {
      final driverId = args[0] as String;
      final latitude = args[1] as double;
      final longitude = args[2] as double;
      final speed = args.length > 3 ? args[3] as double? : null;
      final heading = args.length > 4 ? args[4] as double? : null;

      onLocationUpdateReceived?.call(driverId, latitude, longitude, speed, heading);
    }
  }

  void _handleGeofencingAlert(List<Object?>? args) {
    if (args != null && args.isNotEmpty) {
      final alert = args[0] as Map<String, dynamic>;
      onNotificationReceived?.call('geofencing', alert);
    }
  }

  Future<void> startConnection() async {
    try {
      // FIX 2: Thêm toán tử ! vào .start()
      await _hubConnection!.start();
      print('SignalR connection started');
    } catch (e) {
      print('Error starting SignalR connection: $e');
      // Retry after delay
      Future.delayed(const Duration(seconds: 5), () => startConnection());
    }
  }

  Future<void> stopConnection() async {
    // FIX 3: Thêm kiểm tra null và toán tử ! vào .stop()
    if (_hubConnection != null) {
      await _hubConnection!.stop();
    }
  }

  Future<void> sendLocationUpdate(double latitude, double longitude, {double? speed, double? heading}) async {
    // Kiểm tra _hubConnection != null và xác nhận không null trước khi truy cập state/invoke
    if (_hubConnection != null && _hubConnection!.state == HubConnectionState.Connected) {
      try {
        final args = <Object>[latitude, longitude];
        if (speed != null) args.add(speed);
        if (heading != null) args.add(heading);
        // FIX 2: Thêm toán tử ! vào .invoke()
        await _hubConnection!.invoke('SendLocationUpdate', args: args);
      } catch (e) {
        print('Error sending location update: $e');
      }
    }
  }
  
  // FIX 2: Getter sử dụng toán tử ?. (Null-aware access) an toàn
  HubConnectionState? get connectionState => _hubConnection?.state;

}
// Stabilized socket connection.
