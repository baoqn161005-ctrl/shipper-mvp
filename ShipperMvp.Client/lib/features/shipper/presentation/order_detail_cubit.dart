import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:dio/dio.dart';

import '../../shared/domain/order.dart';
import '../domain/shipper_repository.dart';

sealed class OrderDetailState {}

class OrderDetailReady extends OrderDetailState {
  OrderDetailReady(this.order,
      {this.busy = false, this.error, this.acceptConflict = false});
  final Order order;
  final bool busy;
  final String? error;
  final bool acceptConflict;
}

class OrderDetailCubit extends Cubit<OrderDetailState> {
  OrderDetailCubit(this._repository, Order order)
      : super(OrderDetailReady(order));
  final ShipperRepository _repository;

  Future<void> accept() async {
    final order = (state as OrderDetailReady).order;
    await _perform(() => _repository.acceptOrder(order.id), isAccept: true);
  }

  Future<void> advance() async {
    final order = (state as OrderDetailReady).order;
    final next = switch (order.status) {
      OrderStatus.accepted => OrderStatus.pickedUp,
      OrderStatus.pickedUp => OrderStatus.delivering,
      OrderStatus.delivering => OrderStatus.completed,
      _ => null,
    };
    if (next != null) {
      await _perform(() => _repository.updateStatus(order.id, next));
    }
  }

  Future<void> _perform(
    Future<Order> Function() action, {
    bool isAccept = false,
  }) async {
    final order = (state as OrderDetailReady).order;
    emit(OrderDetailReady(order, busy: true));
    try {
      emit(OrderDetailReady(await action()));
    } catch (error) {
      final conflict = isAccept &&
          error is DioException &&
          error.response?.statusCode == 409;
      emit(OrderDetailReady(
        order,
        error: conflict
            ? 'Rất tiếc, đơn hàng này đã có tài xế khác nhanh tay nhận mất rồi!'
            : error.toString(),
        acceptConflict: conflict,
      ));
    }
  }
}
