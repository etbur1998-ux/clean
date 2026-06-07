import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import '../models/user_model.dart';
import '../models/submission_model.dart';
import '../models/notification_model.dart';

class ApiService {
  // ── Change this IP to your PC's hotspot/WiFi IP ──────────────
  static const String _serverIp =
      '192.168.137.1'; // PC hotspot IP (device is 192.168.137.104)
  static const String _serverHost = 'localhost';
  static const int _serverPort = 5000;

  static String get _host {
    // Web (Chrome) and Windows desktop use localhost
    if (kIsWeb) return _serverHost;
    return _serverIp; // Physical Android phone on hotspot
  }

  static String get baseUrl => 'http://$_host:$_serverPort/api/mobile';
  static String get transportApiBaseUrl =>
      'http://$_host:$_serverPort/api/transport';

  Future<UserModel?> login(String username, String password) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/login'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'username': username, 'password': password}),
      );

      if (response.statusCode == 200) {
        return UserModel.fromJson(jsonDecode(response.body));
      }
      print('Login failed: ${response.statusCode} - ${response.body}');
      return null;
    } catch (e) {
      print('Login error: $e');
      return null;
    }
  }

  Future<List<Map<String, dynamic>>> getWeredas() async {
    try {
      final response = await http.get(Uri.parse('$baseUrl/weredas'));
      if (response.statusCode == 200) {
        return List<Map<String, dynamic>>.from(jsonDecode(response.body));
      }
      return [];
    } catch (e) {
      print('Get Weredas error: $e');
      return [];
    }
  }

  Future<List<Map<String, dynamic>>> getMahberats() async {
    try {
      final response = await http.get(Uri.parse('$baseUrl/mahberats'));
      if (response.statusCode == 200) {
        return List<Map<String, dynamic>>.from(jsonDecode(response.body));
      }
      return [];
    } catch (e) {
      print('Get Mahberats error: $e');
      return [];
    }
  }

  Future<List<Map<String, dynamic>>> getCompanies() async {
    try {
      final response = await http.get(Uri.parse('$baseUrl/companies'));
      if (response.statusCode == 200) {
        return List<Map<String, dynamic>>.from(jsonDecode(response.body));
      }
      return [];
    } catch (e) {
      print('Get Companies error: $e');
      return [];
    }
  }

  Future<List<Map<String, dynamic>>> getVehicles() async {
    try {
      final response = await http.get(Uri.parse('$baseUrl/vehicles'));
      if (response.statusCode == 200) {
        return List<Map<String, dynamic>>.from(jsonDecode(response.body));
      }
      return [];
    } catch (e) {
      print('Get Vehicles error: $e');
      return [];
    }
  }

  Future<bool> submitWork(SubmissionModel submission) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/submit'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(submission.toJson()),
      );
      return response.statusCode == 200 || response.statusCode == 201;
    } catch (e) {
      print('Submit error (saving offline): $e');
      return false;
    }
  }

  Future<String?> uploadImage(dynamic imageFile) async {
    try {
      var request = http.MultipartRequest(
        'POST',
        Uri.parse('$baseUrl/upload-image'),
      );

      if (imageFile is String) {
        // File path
        request.files.add(await http.MultipartFile.fromPath('file', imageFile));
      } else {
        // XFile - read bytes
        final bytes = await imageFile.readAsBytes();
        final fileName = imageFile.name ?? 'image.jpg';
        request.files.add(
          http.MultipartFile.fromBytes('file', bytes, filename: fileName),
        );
      }

      final streamedResponse = await request.send();
      final response = await http.Response.fromStream(streamedResponse);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        // Return full URL with base
        final serverBase = baseUrl.replaceAll('/api/mobile', '');
        return '$serverBase${data['url']}';
      }
      print('Upload failed: ${response.statusCode} - ${response.body}');
      return null;
    } catch (e) {
      print('Upload image error: $e');
      return null;
    }
  }

  Future<List<SubmissionModel>> getHistory(int userId) async {
    try {
      final response = await http.get(Uri.parse('$baseUrl/history/$userId'));
      if (response.statusCode == 200) {
        List<dynamic> data = jsonDecode(response.body);
        return data.map((json) {
          json['userId'] = 0;
          json['role'] = '';
          json['weredaId'] = 0;
          json['mahberatId'] = 0;
          json['rate'] = 0.0;
          return SubmissionModel.fromJson(json);
        }).toList();
      }
      return [];
    } catch (e) {
      print('Get history error: $e');
      return [];
    }
  }

  Future<List<SubmissionModel>> getPendingSubmissions() async {
    try {
      final response = await http.get(Uri.parse('$baseUrl/pending'));
      if (response.statusCode == 200) {
        List<dynamic> data = jsonDecode(response.body);
        return data.map((json) {
          // Add dummy fields required by model if not returned
          json['userId'] = 0;
          json['role'] = '';
          json['weredaId'] = 0;
          json['mahberatId'] = 0;
          json['rate'] = 0.0;
          return SubmissionModel.fromJson(json);
        }).toList();
      }
      return [];
    } catch (e) {
      print('Get pending error: $e');
      return [];
    }
  }

  Future<bool> updateSubmissionStatus(
    int id,
    String status, {
    String receiptType = 'Mahberat',
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/submissions/$id/status'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'status': status, 'receiptType': receiptType}),
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Update status error: $e');
      return false;
    }
  }

  // ══════════════════════════════════════════════════════════════════════
  // NOTIFICATION APIs
  // ══════════════════════════════════════════════════════════════════════

  Future<List<NotificationModel>> getNotifications(int userId) async {
    try {
      final url = '$transportApiBaseUrl/notifications/$userId';
      print('[ApiService] 🔍 GET $url');
      final response = await http.get(Uri.parse(url));
      print('[ApiService] Status: ${response.statusCode}');
      if (response.statusCode == 200) {
        List<dynamic> data = jsonDecode(response.body);
        print('[ApiService] ✅ Received ${data.length} notifications');
        return data.map((json) => NotificationModel.fromJson(json)).toList();
      } else {
        print('[ApiService] ❌ Error ${response.statusCode}: ${response.body}');
        return [];
      }
    } catch (e) {
      print('[ApiService] ❌ Get notifications error: $e');
      return [];
    }
  }

  Future<bool> markNotificationAsRead(int notificationId) async {
    try {
      final response = await http.post(
        Uri.parse('$transportApiBaseUrl/notifications/$notificationId/read'),
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Mark notification as read error: $e');
      return false;
    }
  }

  // ══════════════════════════════════════════════════════════════════════
  // TRANSPORT REQUEST APIs
  // ══════════════════════════════════════════════════════════════════════

  Future<bool> acceptTransportRequest(
    int requestId, {
    required int driverId,
    required String driverName,
    String? notes,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$transportApiBaseUrl/requests/$requestId/driver-action'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'action': 'Accept',
          'driverId': driverId,
          'driverName': driverName,
          'notes': notes ?? '',
        }),
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Accept transport request error: $e');
      return false;
    }
  }

  Future<bool> rejectTransportRequest(
    int requestId, {
    required int driverId,
    required String driverName,
    String? notes,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$transportApiBaseUrl/requests/$requestId/driver-action'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'action': 'Reject',
          'driverId': driverId,
          'driverName': driverName,
          'notes': notes ?? '',
        }),
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Reject transport request error: $e');
      return false;
    }
  }

  Future<Map<String, dynamic>> getReportData(
    int userId, {
    String? startDate,
    String? endDate,
  }) async {
    try {
      var url = '$baseUrl/reports/$userId';
      if (startDate != null && endDate != null) {
        url += '?startDate=$startDate&endDate=$endDate';
      }
      final response = await http.get(Uri.parse(url));
      if (response.statusCode == 200) {
        return jsonDecode(response.body);
      }
      return {'data': [], 'startDate': '', 'endDate': ''};
    } catch (e) {
      print('Get report data error: $e');
      return {'data': [], 'startDate': '', 'endDate': ''};
    }
  }

  Future<List<Map<String, dynamic>>> getDriverTransportRequests(
    int userId,
  ) async {
    try {
      final response = await http.get(
        Uri.parse('$transportApiBaseUrl/requests?userId=$userId&role=driver'),
      );
      if (response.statusCode == 200) {
        return List<Map<String, dynamic>>.from(jsonDecode(response.body));
      }
      return [];
    } catch (e) {
      print('Get driver transport requests error: $e');
      return [];
    }
  }

  Future<bool> markTransportPickedUp(
    int requestId, {
    required int driverId,
    required String driverName,
    String? notes,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$transportApiBaseUrl/requests/$requestId/pickup'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'driverId': driverId,
          'driverName': driverName,
          'notes': notes ?? '',
        }),
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Mark transport picked up error: $e');
      return false;
    }
  }

  Future<bool> submitTransportReceipt(
    int requestId, {
    required int driverId,
    required String driverName,
    String? receiptPhotoUrl,
    String? notes,
    double? actualKilogram,
    int? weredaId,
    int? mahberatId,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$transportApiBaseUrl/requests/$requestId/submit-receipt'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'driverId': driverId,
          'driverName': driverName,
          'receiptPhotoUrl': receiptPhotoUrl ?? '',
          'notes': notes ?? '',
          'actualKilogram': actualKilogram,
          'weredaId': weredaId,
          'mahberatId': mahberatId,
        }),
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Submit transport receipt error: $e');
      return false;
    }
  }

  // ── Private Company Rep APIs ──────────────────────────────────────────────

  Future<Map<String, dynamic>?> getPrivateCompanyInfo(int userId) async {
    try {
      final r = await http.get(Uri.parse('$baseUrl/private-company/$userId'));
      if (r.statusCode == 200)
        return jsonDecode(r.body) as Map<String, dynamic>;
      return null;
    } catch (e) {
      print('Get private company info error: $e');
      return null;
    }
  }

  Future<bool> submitPrivateReceipt({
    required int userId,
    required int weredaId,
    int? vehicleId,
    required double kilogram,
    required double price,
    required String date,
    required String time,
    String? notes,
    String? imageUrl,
  }) async {
    try {
      final r = await http.post(
        Uri.parse('$baseUrl/submit-private-receipt'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'userId': userId,
          'weredaId': weredaId,
          'vehicleId': vehicleId,
          'kilogram': kilogram,
          'price': price,
          'date': date,
          'time': time,
          'notes': notes ?? '',
          'imageUrl': imageUrl ?? '',
        }),
      );
      return r.statusCode == 200;
    } catch (e) {
      print('Submit private receipt error: $e');
      return false;
    }
  }

  Future<List<Map<String, dynamic>>> getPrivateReceipts(int userId) async {
    try {
      final r = await http.get(Uri.parse('$baseUrl/private-receipts/$userId'));
      if (r.statusCode == 200)
        return List<Map<String, dynamic>>.from(jsonDecode(r.body));
      return [];
    } catch (e) {
      print('Get private receipts error: $e');
      return [];
    }
  }
}
