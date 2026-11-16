import '../../shared/domain/order.dart';

class AuthUser {
  const AuthUser({
    required this.id,
    required this.name,
    required this.role,
    required this.token,
  });
  final int id;
  final String name;
  final UserRole role;
  final String token;
}
