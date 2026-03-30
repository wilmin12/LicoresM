import 'dart:convert';
import 'dart:developer' as dev;
import 'dart:io';
import 'package:http/http.dart' as http;
import 'storage.dart';

class ApiException implements Exception {
  final String message;
  final int? statusCode;
  ApiException(this.message, {this.statusCode});

  @override
  String toString() => message;
}

class ApiClient {
  // Android emulator → 10.0.2.2 apunta al localhost de tu PC
  // Dispositivo físico en la misma red → usar IP local de tu PC (ej: 192.168.1.x)
  // iOS simulator / Windows desktop → localhost
  static const String baseUrl = 'http://10.0.2.2:54929';

  static Future<Map<String, String>> _headers({bool auth = true}) async {
    final headers = {'Content-Type': 'application/json'};
    if (auth) {
      final token = await AppStorage.getToken();
      if (token != null && token.isNotEmpty) {
        headers['Authorization'] = 'Bearer $token';
      }
    }
    return headers;
  }

  static Uri _uri(String path, [Map<String, dynamic>? params]) {
    final uri = Uri.parse('$baseUrl$path');
    if (params == null || params.isEmpty) return uri;
    final stringParams = params.map((k, v) => MapEntry(k, v.toString()));
    return uri.replace(queryParameters: stringParams);
  }

  static dynamic _handleResponse(http.Response res) {
    // Debug log — visible en flutter run / logcat
    dev.log('[${res.statusCode}] ${res.request?.url}', name: 'ApiClient');
    dev.log('Body: ${res.body.length > 500 ? res.body.substring(0, 500) : res.body}', name: 'ApiClient');

    // Parse body — handle empty responses gracefully
    dynamic body;
    final rawBody = res.body.trim();
    if (rawBody.isNotEmpty) {
      try {
        body = jsonDecode(rawBody);
      } catch (_) {
        // Body exists but is not JSON (HTML error page, etc.)
        body = null;
      }
    }

    if (res.statusCode == 401) {
      throw ApiException(
        'Sesión expirada. Por favor inicia sesión de nuevo.',
        statusCode: 401,
      );
    }

    if (res.statusCode >= 400) {
      String msg = 'Error del servidor (${res.statusCode})';
      if (body is Map) {
        // API usa PascalCase: Errors, Message
        final errors = (body['Errors'] ?? body['errors']) as List?;
        msg = (errors != null && errors.isNotEmpty)
            ? errors.first.toString()
            : (body['Message'] ?? body['message'])?.toString() ?? msg;
      }
      throw ApiException(msg, statusCode: res.statusCode);
    }

    return body;
  }

  static Future<dynamic> get(String path,
      {Map<String, dynamic>? params}) async {
    try {
      final res = await http
          .get(_uri(path, params), headers: await _headers())
          .timeout(const Duration(seconds: 30));
      return _handleResponse(res);
    } on SocketException {
      throw ApiException(
          'No se puede conectar al servidor. Verifica que la API esté corriendo.');
    } on ApiException {
      rethrow;
    } catch (e) {
      throw ApiException('Error de conexión: $e');
    }
  }

  static Future<dynamic> post(String path,
      {Map<String, dynamic>? body, bool auth = true}) async {
    try {
      final res = await http
          .post(
            _uri(path),
            headers: await _headers(auth: auth),
            body: body != null ? jsonEncode(body) : null,
          )
          .timeout(const Duration(seconds: 30));
      return _handleResponse(res);
    } on SocketException {
      throw ApiException(
          'No se puede conectar al servidor. Verifica que la API esté corriendo.');
    } on ApiException {
      rethrow;
    } catch (e) {
      throw ApiException('Error de conexión: $e');
    }
  }

  static Future<dynamic> put(String path,
      {Map<String, dynamic>? body}) async {
    try {
      final res = await http
          .put(
            _uri(path),
            headers: await _headers(),
            body: body != null ? jsonEncode(body) : null,
          )
          .timeout(const Duration(seconds: 30));
      return _handleResponse(res);
    } on SocketException {
      throw ApiException(
          'No se puede conectar al servidor. Verifica que la API esté corriendo.');
    } on ApiException {
      rethrow;
    } catch (e) {
      throw ApiException('Error de conexión: $e');
    }
  }
}
