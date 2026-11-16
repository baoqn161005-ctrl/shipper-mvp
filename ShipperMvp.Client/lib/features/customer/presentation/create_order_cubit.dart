import 'package:flutter_bloc/flutter_bloc.dart';

import '../../shared/domain/order.dart';
import '../domain/customer_repository.dart';

sealed class CreateOrderState {}

class CreateOrderIdle extends CreateOrderState {}

class CreateOrderLoading extends CreateOrderState {}

class CreateOrderSuccess extends CreateOrderState {
  CreateOrderSuccess(this.order);
  final Order order;
}

class CreateOrderFailure extends CreateOrderState {
  CreateOrderFailure(this.message);
  final String message;
}

class CreateOrderCubit extends Cubit<CreateOrderState> {
  CreateOrderCubit(this._repository) : super(CreateOrderIdle());
  final CustomerRepository _repository;

  Future<void> submit({
    required String pickupAddress,
    required String pickupPhone,
    required String deliveryAddress,
    required String receiverName,
    required String receiverPhone,
    required String note,
    required double codAmount,
  }) async {
    emit(CreateOrderLoading());
    try {
      final order = await _repository.createOrder(
        pickupAddress: pickupAddress,
        pickupPhone: pickupPhone,
        deliveryAddress: deliveryAddress,
        receiverName: receiverName,
        receiverPhone: receiverPhone,
        note: note,
        codAmount: codAmount,
      );
      emit(CreateOrderSuccess(order));
    } catch (error) {
      emit(CreateOrderFailure(error.toString()));
    }
  }
}
