class UserModel {
  final int id;
  final String name;
  // Supported mobile roles: 'driver', 'outsource', 'PrivateCompanyRep', 'manager'
  final String role;
  final String phone;
  final int? vehicleId;
  final String? vehicleName;
  final String? companyName;

  UserModel({
    required this.id,
    required this.name,
    required this.role,
    required this.phone,
    this.vehicleId,
    this.vehicleName,
    this.companyName,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: json['id'],
      name: json['name'] ?? '',
      role: json['role'] ?? '',
      phone: json['phone'] ?? '',
      vehicleId: json['vehicleId'],
      vehicleName: json['vehicleName'],
      companyName: json['companyName'],
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'name': name,
    'role': role,
    'phone': phone,
    'vehicleId': vehicleId,
    'vehicleName': vehicleName,
    'companyName': companyName,
  };

  bool get isDriver => role.toLowerCase() == 'driver';
  bool get isOutsource => role.toLowerCase() == 'outsource';
  bool get isPrivateCompanyRep => role.toLowerCase() == 'privatecompanyrep';
  bool get isManager => role.toLowerCase() == 'manager';
}
