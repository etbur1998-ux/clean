class SubmissionModel {
  final int? id;
  final int userId;
  final String role;
  final int weredaId;
  final String? weredaName;
  final int mahberatId;
  final String? mahberatName;
  final int? vehicleId;
  final String? vehicleName;
  final String? driverName;
  final double kilogram;
  final double rate;
  final double total;
  final String date;
  final String time;
  final String? imageUrl;
  final String notes;
  final String status;
  final double? latitude;
  final double? longitude;
  final String receiptType;

  SubmissionModel({
    this.id,
    required this.userId,
    required this.role,
    required this.weredaId,
    this.weredaName,
    required this.mahberatId,
    this.mahberatName,
    this.vehicleId,
    this.vehicleName,
    this.driverName,
    required this.kilogram,
    required this.rate,
    required this.total,
    required this.date,
    required this.time,
    this.imageUrl,
    required this.notes,
    this.status = 'Pending',
    this.latitude,
    this.longitude,
    this.receiptType = 'Mahberat',
  });

  factory SubmissionModel.fromJson(Map<String, dynamic> json) {
    return SubmissionModel(
      id: json['id'],
      userId: json['userId'] ?? json['user_id'],
      role: json['role'] ?? '',
      weredaId: json['weredaId'] ?? json['wereda_id'],
      weredaName: json['weredaName'],
      mahberatId: json['mahberatId'] ?? json['mahberat_id'],
      mahberatName: json['mahberatName'],
      vehicleId: json['vehicleId'] ?? json['vehicle_id'],
      vehicleName: json['vehicleName'],
      driverName: json['driverName'],
      kilogram: (json['kilogram'] is int) ? (json['kilogram'] as int).toDouble() : json['kilogram']?.toDouble() ?? 0.0,
      rate: (json['rate'] is int) ? (json['rate'] as int).toDouble() : json['rate']?.toDouble() ?? 0.0,
      total: (json['total'] is int) ? (json['total'] as int).toDouble() : json['total']?.toDouble() ?? 0.0,
      date: json['date'] ?? '',
      time: json['time'] ?? '',
      imageUrl: json['imageUrl'] ?? json['image_path'],
      notes: json['notes'] ?? '',
      status: json['status'] ?? 'Pending',
      latitude: json['latitude']?.toDouble(),
      longitude: json['longitude']?.toDouble(),
      receiptType: json['receiptType'] ?? 'Mahberat',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'userId': userId,
      'role': role,
      'weredaId': weredaId,
      'mahberatId': mahberatId,
      'vehicleId': vehicleId,
      'kilogram': kilogram,
      'rate': rate,
      'total': total,
      'date': date,
      'time': time,
      'imageUrl': imageUrl,
      'notes': notes,
      'status': status,
      'latitude': latitude,
      'longitude': longitude,
      'receiptType': receiptType,
    };
  }
}
