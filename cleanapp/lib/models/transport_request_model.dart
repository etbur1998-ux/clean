class TransportRequestModel {
  final int id;
  final String requestNumber;
  final String? mahberatUserName;
  final String? mahberatName;
  final String pickupLocation;
  final String destination;
  final String passengerItemDetails;
  final String requestedDate;
  final String requestedTime;
  final String? specialInstructions;
  final String? dispatcherName;
  final String? driverName;
  final String? vehiclePlate;
  final double? transportCost;
  final String? transactionNumber;
  final String status;
  final DateTime createdAt;
  final DateTime updatedAt;

  TransportRequestModel({
    required this.id,
    required this.requestNumber,
    this.mahberatUserName,
    this.mahberatName,
    required this.pickupLocation,
    required this.destination,
    required this.passengerItemDetails,
    required this.requestedDate,
    required this.requestedTime,
    this.specialInstructions,
    this.dispatcherName,
    this.driverName,
    this.vehiclePlate,
    this.transportCost,
    this.transactionNumber,
    required this.status,
    required this.createdAt,
    required this.updatedAt,
  });

  factory TransportRequestModel.fromJson(Map<String, dynamic> json) {
    return TransportRequestModel(
      id: json['id'] as int? ?? 0,
      requestNumber: json['request_number'] as String? ?? json['requestNumber'] as String? ?? '',
      mahberatUserName: json['mahberat_user_name'] as String? ?? json['mahberatUserName'] as String?,
      mahberatName: json['mahberat_name'] as String? ?? json['mahberatName'] as String?,
      pickupLocation: json['pickup_location'] as String? ?? json['pickupLocation'] as String? ?? '',
      destination: json['destination'] as String? ?? json['destination'] as String? ?? '',
      passengerItemDetails: json['passenger_item_details'] as String? ?? json['passengerItemDetails'] as String? ?? '',
      requestedDate: json['requested_date'] as String? ?? json['requestedDate'] as String? ?? '',
      requestedTime: json['requested_time'] as String? ?? json['requestedTime'] as String? ?? '',
      specialInstructions: json['special_instructions'] as String? ?? json['specialInstructions'] as String?,
      dispatcherName: json['dispatcher_name'] as String? ?? json['dispatcherName'] as String?,
      driverName: json['driver_name'] as String? ?? json['driverName'] as String?,
      vehiclePlate: json['vehicle_plate'] as String? ?? json['vehiclePlate'] as String?,
      transportCost: (json['transport_cost'] as num?)?.toDouble() ?? (json['transportCost'] as num?)?.toDouble(),
      transactionNumber: json['transaction_number'] as String? ?? json['transactionNumber'] as String?,
      status: json['status'] as String? ?? 'PendingDispatcher',
      createdAt: json['created_at'] != null 
          ? DateTime.parse(json['created_at'] as String) 
          : (json['createdAt'] != null ? DateTime.parse(json['createdAt'] as String) : DateTime.now()),
      updatedAt: json['updated_at'] != null 
          ? DateTime.parse(json['updated_at'] as String) 
          : (json['updatedAt'] != null ? DateTime.parse(json['updatedAt'] as String) : DateTime.now()),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'request_number': requestNumber,
      'mahberat_user_name': mahberatUserName,
      'mahberat_name': mahberatName,
      'pickup_location': pickupLocation,
      'destination': destination,
      'passenger_item_details': passengerItemDetails,
      'requested_date': requestedDate,
      'requested_time': requestedTime,
      'special_instructions': specialInstructions,
      'dispatcher_name': dispatcherName,
      'driver_name': driverName,
      'vehicle_plate': vehiclePlate,
      'transport_cost': transportCost,
      'transaction_number': transactionNumber,
      'status': status,
      'created_at': createdAt.toIso8601String(),
      'updated_at': updatedAt.toIso8601String(),
    };
  }

  String getStatusDisplay() {
    switch (status) {
      case 'PendingDispatcher':
        return 'Pending Dispatcher';
      case 'DriverAssigned':
        return 'Assigned';
      case 'DriverAccepted':
        return 'Accepted';
      case 'PickedUp':
        return 'Picked Up';
      case 'ReceiptSubmitted':
        return 'Receipt Submitted';
      case 'ReceiptVerified':
        return 'Receipt Verified';
      case 'StaffApproved':
        return 'Approved';
      case 'Paid':
        return 'Paid';
      case 'DispatcherRejected':
      case 'StaffRejected':
        return 'Rejected';
      default:
        return status;
    }
  }
}
