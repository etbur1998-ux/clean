import 'package:flutter/material.dart';
import 'dart:async';
import '../models/notification_model.dart';
import 'api_service.dart';

class NotificationService extends ChangeNotifier {
  final ApiService _apiService = ApiService();
  
  List<NotificationModel> _notifications = [];
  bool _isLoading = false;
  Timer? _pollingTimer;
  
  List<NotificationModel> get notifications => _notifications;
  bool get isLoading => _isLoading;
  
  int get unreadCount => _notifications.where((n) => !n.isRead).length;

  void startPolling(int userId, {Duration interval = const Duration(seconds: 30)}) {
    print('[NotificationService] Starting notification polling for user $userId');
    
    // Cancel existing timer
    _pollingTimer?.cancel();
    
    // Fetch immediately
    fetchNotifications(userId);
    
    // Then poll every interval
    _pollingTimer = Timer.periodic(interval, (_) {
      fetchNotifications(userId);
    });
  }

  void stopPolling() {
    print('[NotificationService] Stopping notification polling');
    _pollingTimer?.cancel();
    _pollingTimer = null;
  }

  Future<void> fetchNotifications(int userId) async {
    try {
      _isLoading = true;
      notifyListeners();
      
      print('[NotificationService] Fetching notifications for user ID: $userId');
      final newNotifications = await _apiService.getNotifications(userId);
      print('[NotificationService] API returned ${newNotifications.length} notifications');
      
      if (newNotifications.isNotEmpty) {
        print('[NotificationService] Sample notification: ${newNotifications[0].title}');
      }
      
      // Check for new notifications by comparing with existing ones
      final unreadBefore = unreadCount;
      _notifications = newNotifications;
      final unreadAfter = unreadCount;
      
      print('[NotificationService] Total notifications: ${_notifications.length}, Unread: $unreadAfter');
      
      if (unreadAfter > unreadBefore) {
        print('[NotificationService] New notifications received: ${unreadAfter - unreadBefore}');
      }
      
      _isLoading = false;
      notifyListeners();
    } catch (e) {
      print('[NotificationService] ❌ Error fetching notifications: $e');
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> markAsRead(NotificationModel notification) async {
    try {
      final success = await _apiService.markNotificationAsRead(notification.id);
      if (success) {
        final index = _notifications.indexOf(notification);
        if (index >= 0) {
          _notifications[index] = notification.copyWith(isRead: true);
          notifyListeners();
        }
      }
    } catch (e) {
      print('[NotificationService] Error marking notification as read: $e');
    }
  }

  void clearNotifications() {
    _notifications.clear();
    notifyListeners();
  }

  @override
  void dispose() {
    stopPolling();
    super.dispose();
  }
}
