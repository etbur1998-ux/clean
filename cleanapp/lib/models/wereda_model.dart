class WeredaModel {
  final int id;
  final String name;
  final String description;
  final String subcity;
  final bool isActive;

  WeredaModel({
    required this.id,
    required this.name,
    required this.description,
    required this.subcity,
    required this.isActive,
  });

  factory WeredaModel.fromJson(Map<String, dynamic> json) {
    return WeredaModel(
      id: json['id'] ?? 0,
      name: json['name'] ?? '',
      description: json['description'] ?? '',
      subcity: json['subcity'] ?? '',
      isActive: json['isActive'] ?? true,
    );
  }
}
