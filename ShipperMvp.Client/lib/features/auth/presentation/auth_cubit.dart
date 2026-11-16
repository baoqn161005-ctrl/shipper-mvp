import 'package:flutter_bloc/flutter_bloc.dart';

import '../domain/auth_repository.dart';
import '../domain/auth_user.dart';

sealed class AuthState {}

class AuthInitial extends AuthState {}

class AuthSignedOut extends AuthState {
  AuthSignedOut([this.error]);
  final String? error;
}

class AuthLoading extends AuthState {}

class Authenticated extends AuthState {
  Authenticated(this.user);
  final AuthUser user;
}

class AuthCubit extends Cubit<AuthState> {
  AuthCubit(this._repository) : super(AuthInitial());
  final AuthRepository _repository;

  Future<void> restoreSession() async {
    try {
      final user = await _repository.restoreSession();
      emit(user == null ? AuthSignedOut() : Authenticated(user));
    } catch (_) {
      emit(AuthSignedOut());
    }
  }

  Future<void> signIn(String phone, String password) =>
      _run(() => _repository.signIn(phone, password));
  Future<void> signUp(
    String name,
    String phone,
    String password,
    String role,
  ) =>
      _run(() => _repository.signUp(name, phone, password, role));

  Future<void> _run(Future<AuthUser> Function() action) async {
    emit(AuthLoading());
    try {
      emit(Authenticated(await action()));
    } catch (error) {
      emit(AuthSignedOut(error.toString()));
    }
  }

  Future<void> signOut() async {
    await _repository.signOut();
    emit(AuthSignedOut());
  }
}
