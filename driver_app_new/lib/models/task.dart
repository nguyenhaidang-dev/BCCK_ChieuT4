import 'package:flutter/material.dart';

class Task {
  final int id;
  final String referenceCode;
  final String pickupAddress;
  final double pickupLatitude;
  final double pickupLongitude;
  final String deliveryAddress;
  final double deliveryLatitude;
  final double deliveryLongitude;
  final double distanceKm;
  final double weight;
  final String vehicleType;
  final double estimatedPrice;
  final String status;
  final DateTime scheduledPickupTime;
  final DateTime? actualPickupTime;
  final DateTime? completedTime;
  final DateTime? estimatedArrivalTime;
  final String qrCode;

  Task({
    required this.id,
    required this.referenceCode,
    required this.pickupAddress,
    required this.pickupLatitude,
    required this.pickupLongitude,
    required this.deliveryAddress,
    required this.deliveryLatitude,
    required this.deliveryLongitude,
    required this.distanceKm,
    required this.weight,
    required this.vehicleType,
    required this.estimatedPrice,
    required this.status,
    required this.scheduledPickupTime,
    this.actualPickupTime,
    this.completedTime,
    this.estimatedArrivalTime,
    required this.qrCode,
  });

  factory Task.fromJson(Map<String, dynamic> json) {
    return Task(
      id: json['id'],
      referenceCode: json['referenceCode'],
      pickupAddress: json['pickupAddress'],
      pickupLatitude: (json['pickupLatitude'] as num).toDouble(),
      pickupLongitude: (json['pickupLongitude'] as num).toDouble(),
      deliveryAddress: json['deliveryAddress'],
      deliveryLatitude: (json['deliveryLatitude'] as num).toDouble(),
      deliveryLongitude: (json['deliveryLongitude'] as num).toDouble(),
      distanceKm: (json['distanceKm'] as num).toDouble(),
      weight: (json['weight'] as num).toDouble(),
      vehicleType: json['vehicleType'],
      estimatedPrice: (json['estimatedPrice'] as num).toDouble(),
      status: json['status'],
      scheduledPickupTime: DateTime.parse(json['scheduledPickupTime']),
      actualPickupTime: json['actualPickupTime'] != null
          ? DateTime.parse(json['actualPickupTime'])
          : null,
      completedTime: json['completedTime'] != null
          ? DateTime.parse(json['completedTime'])
          : null,
      estimatedArrivalTime: json['estimatedArrivalTime'] != null
          ? DateTime.parse(json['estimatedArrivalTime'])
          : null,
      qrCode: json['qrCode'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'referenceCode': referenceCode,
      'pickupAddress': pickupAddress,
      'pickupLatitude': pickupLatitude,
      'pickupLongitude': pickupLongitude,
      'deliveryAddress': deliveryAddress,
      'deliveryLatitude': deliveryLatitude,
      'deliveryLongitude': deliveryLongitude,
      'distanceKm': distanceKm,
      'weight': weight,
      'vehicleType': vehicleType,
      'estimatedPrice': estimatedPrice,
      'status': status,
      'scheduledPickupTime': scheduledPickupTime.toIso8601String(),
      'actualPickupTime': actualPickupTime?.toIso8601String(),
      'completedTime': completedTime?.toIso8601String(),
      'estimatedArrivalTime': estimatedArrivalTime?.toIso8601String(),
      'qrCode': qrCode,
    };
  }

  String get statusDisplay {
    switch (status) {
      case 'Unassigned':
        return 'Chưa phân công';
      case 'Assigned':
        return 'Đã phân công';
      case 'InProgress':
        return 'Đang thực hiện';
      case 'Completed':
        return 'Hoàn thành';
      case 'Cancelled':
        return 'Đã hủy';
      default:
        return status;
    }
  }

  Color get statusColor {
    switch (status) {
      case 'Unassigned':
        return Colors.grey;
      case 'Assigned':
        return Colors.orange;
      case 'InProgress':
        return Colors.blue;
      case 'Completed':
        return Colors.green;
      case 'Cancelled':
        return Colors.red;
      default:
        return Colors.grey;
    }
  }

  Task copyWith({
    int? id,
    String? referenceCode,
    String? pickupAddress,
    double? pickupLatitude,
    double? pickupLongitude,
    String? deliveryAddress,
    double? deliveryLatitude,
    double? deliveryLongitude,
    double? distanceKm,
    double? weight,
    String? vehicleType,
    double? estimatedPrice,
    String? status,
    DateTime? scheduledPickupTime,
    DateTime? actualPickupTime,
    DateTime? completedTime,
    DateTime? estimatedArrivalTime,
    String? qrCode,
  }) {
    return Task(
      id: id ?? this.id,
      referenceCode: referenceCode ?? this.referenceCode,
      pickupAddress: pickupAddress ?? this.pickupAddress,
      pickupLatitude: pickupLatitude ?? this.pickupLatitude,
      pickupLongitude: pickupLongitude ?? this.pickupLongitude,
      deliveryAddress: deliveryAddress ?? this.deliveryAddress,
      deliveryLatitude: deliveryLatitude ?? this.deliveryLatitude,
      deliveryLongitude: deliveryLongitude ?? this.deliveryLongitude,
      distanceKm: distanceKm ?? this.distanceKm,
      weight: weight ?? this.weight,
      vehicleType: vehicleType ?? this.vehicleType,
      estimatedPrice: estimatedPrice ?? this.estimatedPrice,
      status: status ?? this.status,
      scheduledPickupTime: scheduledPickupTime ?? this.scheduledPickupTime,
      actualPickupTime: actualPickupTime ?? this.actualPickupTime,
      completedTime: completedTime ?? this.completedTime,
      estimatedArrivalTime: estimatedArrivalTime ?? this.estimatedArrivalTime,
      qrCode: qrCode ?? this.qrCode,
    );
  }
}