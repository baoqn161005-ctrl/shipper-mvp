import 'auth_user.dart';

abstract interface class AuthRepository {
  Future<AuthUser> signIn(String phone, String password);
  Future<AuthUser> signUp(
    String name,
    String phone,
    String password,
    String role,
  );
  Future<AuthUser?> restoreSession();
  Future<void> signOut();
}
