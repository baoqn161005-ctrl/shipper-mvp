import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../auth/presentation/auth_cubit.dart';
import 'create_order_cubit.dart';

class CreateOrderPage extends StatefulWidget {
  const CreateOrderPage({super.key});
  @override
  State<CreateOrderPage> createState() => _CreateOrderPageState();
}

class _CreateOrderPageState extends State<CreateOrderPage> {
  final _form = GlobalKey<FormState>();
  final _pickup = TextEditingController();
  final _pickupPhone = TextEditingController();
  final _delivery = TextEditingController();
  final _receiver = TextEditingController();
  final _receiverPhone = TextEditingController();
  final _note = TextEditingController();
  final _cod = TextEditingController(text: '0');

  @override
  void dispose() {
    for (final c in [
      _pickup,
      _pickupPhone,
      _delivery,
      _receiver,
      _receiverPhone,
      _note,
      _cod,
    ]) {
      c.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(
          title: const Text('Tạo đơn hàng'),
          actions: [
            IconButton(
              onPressed: () => context.read<AuthCubit>().signOut(),
              icon: const Icon(Icons.logout),
            ),
          ],
        ),
        body: SafeArea(
          child: Form(
            key: _form,
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                _field(_pickup, 'Điểm lấy hàng'),
                const SizedBox(height: 12),
                _field(
                  _pickupPhone,
                  'Số điện thoại người gửi',
                  keyboard: TextInputType.phone,
                ),
                const SizedBox(height: 12),
                _field(_delivery, 'Điểm giao hàng'),
                const SizedBox(height: 12),
                _field(_receiver, 'Tên người nhận'),
                const SizedBox(height: 12),
                _field(
                  _receiverPhone,
                  'Số điện thoại người nhận',
                  keyboard: TextInputType.phone,
                ),
                const SizedBox(height: 12),
                _field(_note, 'Ghi chú / mô tả hàng hóa', maxLines: 2),
                const SizedBox(height: 12),
                _field(
                  _cod,
                  'Tiền thu hộ COD (₫)',
                  keyboard:
                      const TextInputType.numberWithOptions(decimal: false),
                ),
                const SizedBox(height: 18),
                BlocConsumer<CreateOrderCubit, CreateOrderState>(
                  listener: (context, state) {
                    if (state is CreateOrderSuccess) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text('Đã tạo đơn ${state.order.orderCode}'),
                        ),
                      );
                    }
                    if (state is CreateOrderFailure) {
                      ScaffoldMessenger.of(
                        context,
                      ).showSnackBar(SnackBar(content: Text(state.message)));
                    }
                  },
                  builder: (context, state) => FilledButton.icon(
                    onPressed: state is CreateOrderLoading ? null : _submit,
                    icon: state is CreateOrderLoading
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Icon(Icons.add),
                    label: const Text('Tạo đơn'),
                  ),
                ),
              ],
            ),
          ),
        ),
      );

  Widget _field(
    TextEditingController controller,
    String label, {
    TextInputType? keyboard,
    int maxLines = 1,
  }) =>
      TextFormField(
        controller: controller,
        keyboardType: keyboard,
        maxLines: maxLines,
        decoration: InputDecoration(labelText: label),
        validator: (value) => value == null || value.trim().isEmpty
            ? 'Vui lòng nhập $label'
            : null,
      );

  void _submit() {
    if (!_form.currentState!.validate()) return;
    final amount = double.tryParse(
      _cod.text.replaceAll(',', '').replaceAll('.', ''),
    );
    if (amount == null || amount < 0) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('COD phải là số không âm')));
      return;
    }
    context.read<CreateOrderCubit>().submit(
          pickupAddress: _pickup.text.trim(),
          pickupPhone: _pickupPhone.text.trim(),
          deliveryAddress: _delivery.text.trim(),
          receiverName: _receiver.text.trim(),
          receiverPhone: _receiverPhone.text.trim(),
          note: _note.text.trim(),
          codAmount: amount,
        );
  }
}
