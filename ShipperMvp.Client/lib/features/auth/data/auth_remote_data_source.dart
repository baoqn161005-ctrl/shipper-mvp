import '../../../core/network/api_client.dart';

class AuthRemoteDataSource {
  AuthRemoteDataSource(this._client);
  final ApiClient _client;

  Future<Map<String, dynamic>> login(String phone, String password) async =>
      (await _client.dio.post<Map<String, dynamic>>(
        '/api/auth/login',
        data: {'phoneNumber': phone, 'password': password},
      ))
          .data!;

  Future<Map<String, dynamic>> register(
    String name,
    String phone,
    String password,
    String role,
  ) async =>
      (await _client.dio.post<Map<String, dynamic>>(
        '/api/auth/register',
        data: {
          'fullName': name,
          'phoneNumber': phone,
          'password': password,
          'role': role,
        },
      ))
          .data!;
}
