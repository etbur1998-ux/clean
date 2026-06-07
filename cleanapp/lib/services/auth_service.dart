import 'package:flutter/material.dart';
import '../models/user_model.dart';
import 'api_service.dart';

class AuthService extends ChangeNotifier {
  UserModel? _currentUser;
  final ApiService _apiService = ApiService();
  bool _isLoading = false;

  UserModel? get currentUser => _currentUser;
  bool get isAuthenticated => _currentUser != null;
  bool get isLoading => _isLoading;

  Future<bool> login(String username, String password) async {
    _isLoading = true;
    notifyListeners();

    _currentUser = await _apiService.login(username, password);
    
    _isLoading = false;
    notifyListeners();

    return _currentUser != null;
  }

  void logout() {
    _currentUser = null;
    notifyListeners();
  }
}
