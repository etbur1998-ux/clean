import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/submission_model.dart';

class LocalDbService {
  static const String _offlineSubmissionsKey = 'offline_submissions';

  Future<void> saveOfflineSubmission(SubmissionModel submission) async {
    final prefs = await SharedPreferences.getInstance();
    List<String> offlineSubmissions = prefs.getStringList(_offlineSubmissionsKey) ?? [];
    
    // Add new submission
    offlineSubmissions.add(jsonEncode(submission.toJson()));
    await prefs.setStringList(_offlineSubmissionsKey, offlineSubmissions);
  }

  Future<List<SubmissionModel>> getOfflineSubmissions() async {
    final prefs = await SharedPreferences.getInstance();
    List<String> offlineSubmissions = prefs.getStringList(_offlineSubmissionsKey) ?? [];
    
    return offlineSubmissions.map((jsonStr) => SubmissionModel.fromJson(jsonDecode(jsonStr))).toList();
  }

  Future<void> clearOfflineSubmissions() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_offlineSubmissionsKey);
  }
}
