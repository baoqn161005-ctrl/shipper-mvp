import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import 'auth_cubit.dart';

class AuthPage extends StatefulWidget {
  const AuthPage({super.key});
  @override
  State<AuthPage> createState() => _AuthPageState();
}

class _AuthPageState extends State<AuthPage> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _phone = TextEditingController();
  final _password = TextEditingController();
  bool _register = false;
  String _role = 'Customer';

  @override
  void dispose() {
    _name.dispose();
    _phone.dispose();
    _password.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        body: SafeArea(
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 440),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      const Icon(Icons.local_shipping_outlined, size: 56),
                      const SizedBox(height: 12),
                      Text(
                        _register ? 'Tạo tài khoản' : 'Chào mừng trở lại',
                        style: Theme.of(context).textTheme.headlineSmall,
                        textAlign: TextAlign.center,
                      ),
                      const SizedBox(height: 24),
                      if (_register) ...[
                        TextFormField(
                          controller: _name,
                          decoration:
                              const InputDecoration(labelText: 'Họ và tên'),
                          validator: _required,
                        ),
                        const SizedBox(height: 12),
                      ],
                      TextFormField(
                        controller: _phone,
                        keyboardType: TextInputType.phone,
                        decoration: const InputDecoration(
                          labelText: 'Số điện thoại',
                        ),
                        validator: _required,
                      ),
                      const SizedBox(height: 12),
                      TextFormField(
                        controller: _password,
                        obscureText: true,
                        decoration:
                            const InputDecoration(labelText: 'Mật khẩu'),
                        validator: _required,
                      ),
                      if (_register) ...[
                        const SizedBox(height: 12),
                        DropdownButtonFormField<String>(
                          initialValue: _role,
                          decoration:
                              const InputDecoration(labelText: 'Vai trò'),
                          items: const [
                            DropdownMenuItem(
                              value: 'Customer',
                              child: Text('Khách hàng'),
                            ),
                            DropdownMenuItem(
                              value: 'Shipper',
                              child: Text('Shipper'),
                            ),
                          ],
                          onChanged: (value) =>
                              setState(() => _role = value ?? 'Customer'),
                        ),
                      ],
                      const SizedBox(height: 20),
                      BlocBuilder<AuthCubit, AuthState>(
                        builder: (context, state) {
                          if (state is AuthLoading) {
                            return const Center(
                                child: CircularProgressIndicator());
                          }
                          return FilledButton(
                            onPressed: _submit,
                            child: Text(_register ? 'Đăng ký' : 'Đăng nhập'),
                          );
                        },
                      ),
                      BlocBuilder<AuthCubit, AuthState>(
                        builder: (context, state) => state is AuthSignedOut &&
                                state.error != null
                            ? Padding(
                                padding: const EdgeInsets.only(top: 12),
                                child: Text(
                                  state.error!,
                                  style: TextStyle(
                                    color: Theme.of(context).colorScheme.error,
                                  ),
                                ),
                              )
                            : const SizedBox.shrink(),
                      ),
                      TextButton(
                        onPressed: () => setState(() => _register = !_register),
                        child: Text(
                          _register
                              ? 'Đã có tài khoản? Đăng nhập'
                              : 'Chưa có tài khoản? Đăng ký',
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      );

  String? _required(String? value) =>
      value == null || value.trim().isEmpty ? 'Vui lòng nhập thông tin' : null;
  void _submit() {
    if (!_formKey.currentState!.validate()) return;
    final cubit = context.read<AuthCubit>();
    if (_register) {
      cubit.signUp(
        _name.text.trim(),
        _phone.text.trim(),
        _password.text,
        _role,
      );
    } else {
      cubit.signIn(_phone.text.trim(), _password.text);
    }
  }
}
