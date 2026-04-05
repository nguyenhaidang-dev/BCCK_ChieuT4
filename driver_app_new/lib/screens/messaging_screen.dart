import 'package:flutter/material.dart';

class MessagingScreen extends StatelessWidget {
  const MessagingScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Tin nhắn Quản lý'),
      ),
      body: const Center(
        child: Text('Tính năng chat đang được phát triển.'),
      ),
    );
  }
}