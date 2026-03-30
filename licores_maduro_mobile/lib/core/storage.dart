import 'package:shared_preferences/shared_preferences.dart';

class AppStorage {
  static const _keyToken = 'auth_token';
  static const _keyFullName = 'user_full_name';
  static const _keyUsername = 'user_username';
  static const _keyRoleId = 'user_role_id';

  static Future<void> saveToken(String token) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_keyToken, token);
  }

  static Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_keyToken);
  }

  static Future<void> saveUserInfo({
    required String fullName,
    required String username,
    required int roleId,
  }) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_keyFullName, fullName);
    await prefs.setString(_keyUsername, username);
    await prefs.setInt(_keyRoleId, roleId);
  }

  static Future<Map<String, dynamic>> getUserInfo() async {
    final prefs = await SharedPreferences.getInstance();
    return {
      'fullName': prefs.getString(_keyFullName) ?? '',
      'username': prefs.getString(_keyUsername) ?? '',
      'roleId': prefs.getInt(_keyRoleId) ?? 0,
    };
  }

  static Future<void> clear() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.clear();
  }
}
