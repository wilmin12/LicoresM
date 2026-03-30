import 'dart:developer' as dev;
import '../core/api_client.dart';
import '../core/storage.dart';
import '../models/auth_models.dart';

class AuthService {
  static Future<LoginResponse> login(String username, String password) async {
    final res = await ApiClient.post(
      '/api/auth/login',
      body: {'username': username, 'password': password},
      auth: false,
    );

    // Debug: log what the API actually returned
    dev.log('Login response: $res', name: 'AuthService');

    if (res == null) {
      throw ApiException('El servidor no devolvió respuesta. Verifica que la API esté corriendo en http://10.0.2.2:54929');
    }

    // API usa PascalCase: Data, Message, Errors, Success
    final data = res['Data'];
    if (data == null) {
      final msg = res['Message'] ?? res['Errors']?.toString() ?? 'Respuesta inesperada del servidor';
      throw ApiException(msg.toString());
    }

    final loginRes = LoginResponse.fromJson(data as Map<String, dynamic>);

    await AppStorage.saveToken(loginRes.token);
    await AppStorage.saveUserInfo(
      fullName: loginRes.user.fullName,
      username: loginRes.user.username,
      roleId: loginRes.user.roleId,
    );
    return loginRes;
  }

  static Future<void> logout() async {
    try {
      await ApiClient.post('/api/auth/logout');
    } catch (_) {}
    await AppStorage.clear();
  }

  static Future<bool> isLoggedIn() async {
    final token = await AppStorage.getToken();
    return token != null && token.isNotEmpty;
  }
}
