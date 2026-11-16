import 'dart:convert';

import '../../../core/storage/token_storage.dart';
import '../../../features/shared/domain/order.dart';
import '../domain/auth_repository.dart';
import '../domain/auth_user.dart';
import 'auth_remote_data_source.dart';

class AuthRepositoryImpl implements AuthRepository {
  AuthRepositoryImpl(this._remote, this._storage);
  final AuthRemoteDataSource _remote;
  final TokenStorage _storage;

  @override
  Future<AuthUser> signIn(String phone, String password) async =>
      await _map(await _remote.login(phone, password));

  @override
  Future<AuthUser> signUp(
    String name,
    String phone,
    String password,
    String role,
  ) async =>
      await _map(await _remote.register(name, phone, password, role));

  Future<AuthUser> _map(Map<String, dynamic> json) async {
    final token = (json['token'] ?? json['accessToken']) as String;
    final roleText = (json['role'] ??
            (json['user'] as Map<String, dynamic>?)?['role']) as String? ??
        'Customer';
    final id = (json['userId'] ??
        (json['user'] as Map<String, dynamic>?)?['id'] ??
        0) as int;
    final name = (json['fullName'] ??
        (json['user'] as Map<String, dynamic>?)?['fullName'] ??
        '') as String;
    final user = AuthUser(
      id: id,
      name: name,
      token: token,
      role: roleText.toLowerCase() == 'shipper'
          ? UserRole.shipper
          : UserRole.customer,
    );
    await _storage.save(token);
    return user;
  }

  @override
  Future<AuthUser?> restoreSession() async {
    final token = await _storage.read();
    if (token == null) return null;
    // API should expose a refresh/me endpoint for reliable session restoration.
    return AuthUser(id: 0, name: '', token: token, role: _roleFromToken(token));
  }

  UserRole _roleFromToken(String token) {
    try {
      final payload = token.split('.')[1];
      final normalized = base64Url.normalize(payload);
      final claims = jsonDecode(utf8.decode(base64Url.decode(normalized)))
          as Map<String, dynamic>;
      final role = claims['role'] ??
          claims[
              'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      return role.toString().toLowerCase() == 'shipper'
          ? UserRole.shipper
          : UserRole.customer;
    } catch (_) {
      return UserRole.customer;
    }
  }

  @override
  Future<void> signOut() => _storage.clear();
}
