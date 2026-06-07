class NotificationModel {
  final int id;
  final int? transportRequestId;
  final String? requestNumber;
  final String title;
  final String body;
  final String notificationType; // "Action", "Success", "Warning", "Info"
  final bool isRead;
  final DateTime createdAt;

  NotificationModel({
    required this.id,
    this.transportRequestId,
    this.requestNumber,
    required this.title,
    required this.body,
    required this.notificationType,
    required this.isRead,
    required this.createdAt,
  });

  factory NotificationModel.fromJson(Map<String, dynamic> json) {
    return NotificationModel(
      id: json['id'] as int? ?? 0,
      transportRequestId: json['transport_request_id'] as int?,
      requestNumber: json['request_number'] as String?,
      title: json['title'] as String? ?? '',
      body: json['body'] as String? ?? '',
      notificationType: json['notification_type'] as String? ?? 'Info',
      isRead: (json['is_read'] as int? ?? 0) == 1,
      createdAt: json['created_at'] != null
          ? DateTime.parse(json['created_at'] as String)
          : DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'transport_request_id': transportRequestId,
      'request_number': requestNumber,
      'title': title,
      'body': body,
      'notification_type': notificationType,
      'is_read': isRead ? 1 : 0,
      'created_at': createdAt.toIso8601String(),
    };
  }

  NotificationModel copyWith({
    int? id,
    int? transportRequestId,
    String? requestNumber,
    String? title,
    String? body,
    String? notificationType,
    bool? isRead,
    DateTime? createdAt,
  }) {
    return NotificationModel(
      id: id ?? this.id,
      transportRequestId: transportRequestId ?? this.transportRequestId,
      requestNumber: requestNumber ?? this.requestNumber,
      title: title ?? this.title,
      body: body ?? this.body,
      notificationType: notificationType ?? this.notificationType,
      isRead: isRead ?? this.isRead,
      createdAt: createdAt ?? this.createdAt,
    );
  }
}
